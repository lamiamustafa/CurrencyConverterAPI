using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;

namespace CurrencyConverter.API.Services;

/// <summary>
/// Service for rate limiting API requests.
/// </summary>
public class RateLimitingService : IRateLimiterPolicy<string>
{
    public const string PolicyName = "API_RateLimit_perDay";
    private Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingService"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="config"></param>
    public RateLimitingService(ILogger<RateLimitingService> logger, IConfiguration config)
    {
        _onRejected = (ctx, token) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            logger.LogWarning($"Request rejected by {nameof(RateLimitingService)}");
            return ValueTask.CompletedTask;
        };
        _logger = logger;
        _config = config;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => _onRejected;

    // Existing code...

    /// <summary>
    /// Gets the rate limit partition for the specified HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The rate limit partition.</returns>
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        bool isAuth = httpContext.User.Identity?.IsAuthenticated ?? false;
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        string client_Id = httpContext.User.FindFirst("Client_Id")?.Value ?? string.Empty;
        var partitionKey = isAuth ? client_Id : ip;

        if (int.TryParse(_config["RateLimiting:APIRateLimiting:Limit"], out int rateLimit) &&
            int.TryParse(_config["RateLimiting:APIRateLimiting:PeriodInMinutes"], out int periodInMinutes) &&
            rateLimit > 0)
        {
            return RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: partitionKey,
               factory: partition => new FixedWindowRateLimiterOptions
               {
                   AutoReplenishment = true,
                   PermitLimit = rateLimit,
                   QueueLimit = 0,
                   Window = TimeSpan.FromMinutes(periodInMinutes)
               });
        }
        else
        {
            return RateLimitPartition.GetNoLimiter(partitionKey);
        }
    }
}
