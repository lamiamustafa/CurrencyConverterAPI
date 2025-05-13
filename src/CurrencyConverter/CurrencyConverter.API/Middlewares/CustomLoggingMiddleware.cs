using Azure;
using Serilog.Context;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using static System.Net.WebRequestMethods;

namespace CurrencyConverter.API.Middlewares
{
    public class CustomLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomLoggingMiddleware> _logger;

        public CustomLoggingMiddleware(RequestDelegate next, ILogger<CustomLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Logs each Request enriched with context including: - Client IP,  - Client ID(from JWT),  - HTTP Method & Endpoint, Response Code & Response Time
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var method = context.Request.Method;
            var endpoint = context.Request.Path;
            var clientId = context.User?.FindFirst("Client_Id")?.Value ?? context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                var statusCode = context.Response.StatusCode;
                var responseTimeMs = stopwatch.ElapsedMilliseconds;

                using (LogContext.PushProperty("ClientIP", ipAddress))
                using (LogContext.PushProperty("ClientId", clientId)) //log custom client_id as user's ID from the ASP.NET Identity 
                using (LogContext.PushProperty("HttpMethod", method))
                using (LogContext.PushProperty("Endpoint", endpoint))
                using (LogContext.PushProperty("StatusCode", statusCode))
                using (LogContext.PushProperty("ResponseTimeMs", responseTimeMs))
                {
                    _logger.LogInformation("Handled HTTP request");
                }
                context.Response.Body = originalBodyStream;

            }
        }
    }

}
