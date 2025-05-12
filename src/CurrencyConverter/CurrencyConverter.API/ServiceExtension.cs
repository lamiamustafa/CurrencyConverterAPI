using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Application.Services;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Persistence;
using CurrencyConverter.Infrastructure.Persistence.SeedData;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.ComponentModel;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;
using CurrencyConverter.Infrastructure.CurrencyProvider;
using System.Threading.RateLimiting;
using CurrencyConverter.API.Services;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace CurrencyConverter.API
{
    public static class ServiceExtension
    {
        /// <summary>
        /// //Register Application Services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddDIServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddLogging();
            services.AddMemoryCache();
            services.AddSingleton<ICurrencyProviderFactory, CurrencyProviderFactory>();
            services.AddSingleton<ICurrencyService, CurrencyService>();
            services.AddTransient<FrankfurterProvider>();
            services.AddHttpClient<FrankfurterProvider>(client =>
            {
                client.BaseAddress = new Uri(config["ExternalApis:Frankfurter:BaseURL"]);
            });
            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string? connString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("The connection string 'DefaultConnection' is not configured.");
            }

            services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer(connString));

            return services;
        }

        /// <summary>
        /// Build the configuration for the service.
        /// </summary>
        public static IHostBuilder AddConfiguration(this IHostBuilder host)
        {
            string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrEmpty(environment))
                environment = "development";

            host.ConfigureAppConfiguration((builderContext, configBuilder) =>
            {
                configBuilder.AddJsonFile(
                    path: "appsettings.json",
                    optional: false,
                    reloadOnChange: true);

                configBuilder.AddJsonFile(
                    path: $"appsettings.{environment.ToLower()}.json",
                    optional: true,
                    reloadOnChange: true);
            });

            return host;
        }

        public static async Task<WebApplication> CreateAndMigrateDB(this WebApplication webApplication)
        {
            using (var scope = webApplication.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
                await UsersSeed.SeedAsync(scope.ServiceProvider);
            }
            return webApplication;
        }

        public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration config)
        {
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;

            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            var key = Encoding.UTF8.GetBytes(config["JwtSettings:Key"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["JwtSettings:Issuer"],
                    ValidAudience = config["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
            return services;
        }

        public static IServiceCollection AddSwaggerSupport(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                // Define the security scheme
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    In = ParameterLocation.Header,
                    Description = "Enter valid JWT token.",
                });

                // Add security requirement
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme, //refer to Bearer scheme from the Security definition
                                Id = "Bearer"
                            }
                        },
                        new string[] {} //This applies to all endpoints
                    }
                });
            });
            return services;
        }

        public static IServiceCollection AddExternalApiClient(this IServiceCollection services, IConfiguration config)
        {

            var externalApisSection = config.GetSection("ExternalApis");

            foreach (var api in externalApisSection.GetChildren())
            {
                string apiName = api.Key;
                string baseUrl = api.GetValue<string>("BaseURL");


                services.AddHttpClient(apiName, client =>
                {
                    client.BaseAddress = new Uri(config[$"ExternalApis:{baseUrl}"]);
                })
                .AddResilienceHandler($"{apiName}Policy", builder =>
                {
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromSeconds(2),
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(response => response.StatusCode == HttpStatusCode.RequestTimeout ||
                                      (int)response.StatusCode >= 500)
                    });
                });
            }


            return services;
        }
    
        public static IServiceCollection AddRateLimiter(this IServiceCollection services)
        {
            /// <summary>
            /// Configure rate limiting middleware and endpoints.
            /// </summary>
            services.AddRateLimiter(limiterOptions =>
            {
                limiterOptions.OnRejected = (context, cancellationToken) =>
                {
                    // In the following block, retry after holding the time to wait before retrying the request
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                    .CreateLogger("Microsoft.AspNetCore.RateLimitingMiddleware").LogWarning("OnRejected");

                    return new ValueTask();
                };

                limiterOptions.AddPolicy<string, RateLimitingService>(RateLimitingService.PolicyName);

                limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    return RateLimitPartition.GetFixedWindowLimiter("global", key => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1)
                    });
                });
            });

            return services;
        }

        public static IServiceCollection AddApiVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0); // Default = v1.0
                options.AssumeDefaultVersionWhenUnspecified = true;// Adds headers to responses
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Version"),
                    new MediaTypeApiVersionReader("ver")
                );
            });

            return services;
        }

    }
}
