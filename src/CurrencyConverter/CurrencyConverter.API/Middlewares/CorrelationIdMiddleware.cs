using Serilog.Context;

namespace CurrencyConverter.API.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Enrich logs with ConnectionId
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(CorrelationConstants.Header, out var headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

            context.Items[CorrelationConstants.PropertyName] = correlationId;

            using (LogContext.PushProperty(CorrelationConstants.PropertyName, correlationId))
            {
                context.Response.Headers[CorrelationConstants.Header] = correlationId;
                await _next(context);
            }
        }
    }
    public static class CorrelationConstants
    {
        public const string Header = "X-Correlation-ID";
        public const string PropertyName = "CorrelationId";
    }
}
