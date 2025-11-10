using Aura.Core.Logging;
using Serilog.Context;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware that injects a correlation ID and trace context into each HTTP request for tracking and diagnostics.
/// The correlation ID is added to the response headers and logged with all log entries.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string TraceIdHeaderName = "X-Trace-ID";
    private const string SpanIdHeaderName = "X-Span-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);
        
        // Get or generate trace context
        var traceId = GetOrGenerateTraceId(context);
        var parentSpanId = GetParentSpanId(context);
        
        // Create trace context
        var traceContext = parentSpanId != null 
            ? new TraceContext(traceId, parentSpanId)
            : new TraceContext(traceId);
        
        // Set operation name from route
        traceContext.OperationName = $"{context.Request.Method} {context.Request.Path}";
        
        // Create request context
        var requestContext = new RequestContext
        {
            RequestId = correlationId,
            ClientIp = GetClientIpAddress(context),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            Method = context.Request.Method,
            Path = context.Request.Path.ToString(),
            StartedAt = DateTimeOffset.UtcNow
        };
        
        // Add headers to response
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        context.Response.Headers[TraceIdHeaderName] = traceContext.TraceId;
        context.Response.Headers[SpanIdHeaderName] = traceContext.SpanId;
        
        // Push contexts into async local storage and Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceContext.TraceId))
        using (LogContext.PushProperty("SpanId", traceContext.SpanId))
        using (TraceContext.BeginScope(traceContext))
        {
            // Store in HttpContext.Items for easy access by other middleware/endpoints
            context.Items["CorrelationId"] = correlationId;
            context.Items["TraceContext"] = traceContext;
            RequestContext.Current = requestContext;
            
            _logger.LogDebug(
                "Request started: {Method} {Path} [CorrelationId: {CorrelationId}, TraceId: {TraceId}, SpanId: {SpanId}]",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                traceContext.TraceId,
                traceContext.SpanId);
            
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

    private static string GetOrGenerateTraceId(HttpContext context)
    {
        // Check if client provided a trace ID (W3C Trace Context or custom header)
        if (context.Request.Headers.TryGetValue(TraceIdHeaderName, out var traceId) 
            && !string.IsNullOrWhiteSpace(traceId))
        {
            return traceId.ToString();
        }

        // Check for W3C traceparent header
        if (context.Request.Headers.TryGetValue("traceparent", out var traceparent)
            && !string.IsNullOrWhiteSpace(traceparent))
        {
            var parts = traceparent.ToString().Split('-');
            if (parts.Length >= 2)
            {
                return parts[1]; // trace-id is the second part
            }
        }

        // Use correlation ID as trace ID if not provided
        return GetOrGenerateCorrelationId(context);
    }

    private static string? GetParentSpanId(HttpContext context)
    {
        // Check for custom span ID header
        if (context.Request.Headers.TryGetValue(SpanIdHeaderName, out var spanId)
            && !string.IsNullOrWhiteSpace(spanId))
        {
            return spanId.ToString();
        }

        // Check for W3C traceparent header
        if (context.Request.Headers.TryGetValue("traceparent", out var traceparent)
            && !string.IsNullOrWhiteSpace(traceparent))
        {
            var parts = traceparent.ToString().Split('-');
            if (parts.Length >= 3)
            {
                return parts[2]; // parent-id is the third part
            }
        }

        return null;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (if behind a proxy)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
        {
            return realIp.ToString();
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
