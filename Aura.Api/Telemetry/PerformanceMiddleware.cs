using System.Diagnostics;

namespace Aura.Api.Telemetry;

/// <summary>
/// Middleware to track request performance and log slow requests
/// </summary>
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly PerformanceMetrics _metrics;
    private readonly IConfiguration _configuration;

    public PerformanceMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMiddleware> logger,
        PerformanceMetrics metrics,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing the request
        var stopwatch = Stopwatch.StartNew();
        
        // Get configuration thresholds
        var slowThreshold = _configuration.GetValue<int>("Performance:SlowRequestThresholdMs", 1000);
        var verySlowThreshold = _configuration.GetValue<int>("Performance:VerySlowRequestThresholdMs", 5000);
        var enableDetailedTelemetry = _configuration.GetValue<bool>("Performance:EnableDetailedTelemetry", true);

        try
        {
            // Execute the request
            await _next(context);
        }
        finally
        {
            // Stop timing
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Get request details
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? string.Empty;
            var statusCode = context.Response.StatusCode;
            var endpoint = $"{method}:{path}";

            // Record metrics
            _metrics.RecordRequest(endpoint, elapsedMs);

            // Log based on performance thresholds
            if (elapsedMs > verySlowThreshold)
            {
                // Very slow request - log as error
                _logger.LogError(
                    "Very slow request detected - {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                    method,
                    path,
                    elapsedMs,
                    statusCode);
            }
            else if (elapsedMs > slowThreshold)
            {
                // Slow request - log as warning
                _logger.LogWarning(
                    "Slow request detected - {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                    method,
                    path,
                    elapsedMs,
                    statusCode);
            }
            else if (enableDetailedTelemetry)
            {
                // Normal request - log as debug
                _logger.LogDebug(
                    "Request completed - {Method} {Path} took {ElapsedMs}ms (Status: {StatusCode})",
                    method,
                    path,
                    elapsedMs,
                    statusCode);
            }

            // Always log structured metrics for aggregation
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestDuration"] = elapsedMs,
                ["Endpoint"] = endpoint,
                ["Method"] = method,
                ["Path"] = path,
                ["StatusCode"] = statusCode
            }))
            {
                _logger.LogInformation(
                    "Performance metric recorded for {Endpoint}",
                    endpoint);
            }
        }
    }
}
