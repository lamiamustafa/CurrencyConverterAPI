using CurrencyConverter.API;
using CurrencyConverter.API.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using CurrencyConverter.API.Services;

public class Program 
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerSupport();

        builder.Host.AddConfiguration();
        builder.Services.AddDIServices(builder.Configuration);

        //builder.Host.AddLogging(builder.Configuration);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("CurrencyConverterApi"))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();
            });

        builder.Services.AddDatabase(builder.Configuration);

        builder.Services.AddAuthentication(builder.Configuration);

        builder.Services.AddExternalApiClient(builder.Configuration);

        builder.Services.AddRateLimiter();

        // This enables {version:apiVersion} in route attributes
        builder.Services.AddRouting(options =>
        {
            options.ConstraintMap.Add("apiVersion", typeof(Microsoft.AspNetCore.Mvc.Routing.ApiVersionRouteConstraint));
        });

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<CustomLoggingMiddleware>();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // TODO: restrict trusted proxy IPs
        });

        app.CreateAndMigrateDB();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRateLimiter();

        app.MapControllers().RequireRateLimiting(RateLimitingService.PolicyName); 

        app.Run();
    }
}
