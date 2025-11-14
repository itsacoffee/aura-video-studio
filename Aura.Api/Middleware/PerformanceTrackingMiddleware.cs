using Aura.Core.Services.Diagnostics;
using System.Diagnostics;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware that tracks request performance and records slow operations
/// </summary>
public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTrackingMiddleware> _logger;
    private readonly PerformanceTrackingService _performanceTracking;
    private readonly TimeSpan _slowRequestThreshold = TimeSpan.FromSeconds(5);

    public PerformanceTrackingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTrackingMiddleware> logger,
        PerformanceTrackingService performanceTracking)
    {
        _next = next;
        _logger = logger;
        _performanceTracking = performanceTracking;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";
        var correlationId = context.Items["CorrelationId"] as string;

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            // Record performance metrics
            var details = new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.ToString(),
                ["statusCode"] = context.Response.StatusCode,
                ["contentLength"] = context.Response.ContentLength ?? 0
            };

            _performanceTracking.RecordOperation(endpoint, duration, correlationId, details);

            // Log slow requests with detailed information
            if (duration >= _slowRequestThreshold)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {Duration}ms (Status: {StatusCode}) [CorrelationId: {CorrelationId}]",
                    context.Request.Method,
                    context.Request.Path,
                    duration.TotalMilliseconds,
                    context.Response.StatusCode,
                    correlationId ?? "N/A");
            }

            // Log all requests with duration for performance tracking
            _logger.LogInformation(
                "Request completed: {Method} {Path} - {StatusCode} in {Duration}ms [CorrelationId: {CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration.TotalMilliseconds,
                correlationId ?? "N/A");
        }
    }
}

/// <summary>
/// Extension methods for registering the PerformanceTrackingMiddleware
/// </summary>
public static class PerformanceTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UsePerformanceTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PerformanceTrackingMiddleware>();
    }
}
