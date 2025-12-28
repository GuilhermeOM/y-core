using Serilog.Context;

namespace Y.Core.Api.Middlewares;
internal sealed class LoggingCorrelationMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public LoggingCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            return _next.Invoke(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName, out var correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
