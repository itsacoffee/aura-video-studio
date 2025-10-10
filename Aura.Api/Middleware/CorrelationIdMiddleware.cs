using Serilog.Context;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware that injects a correlation ID into each HTTP request for tracking and diagnostics.
/// The correlation ID is added to the response headers and logged with all log entries.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);
        
        // Add correlation ID to response headers
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        
        // Push correlation ID into Serilog's LogContext so it's included in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Store in HttpContext.Items for easy access by other middleware/endpoints
            context.Items["CorrelationId"] = correlationId;
            
            await _next(context);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check if client provided a correlation ID
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) 
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Generate a new correlation ID
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>
/// Extension methods for registering the CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
