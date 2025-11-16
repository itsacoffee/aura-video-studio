using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware to monitor and log slow API requests and database queries
/// </summary>
public class QueryPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QueryPerformanceMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;
    private readonly int _verySlowRequestThresholdMs;

    public QueryPerformanceMiddleware(
        RequestDelegate next,
        ILogger<QueryPerformanceMiddleware> logger,
        int slowRequestThresholdMs = 1000,
        int verySlowRequestThresholdMs = 5000)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = slowRequestThresholdMs;
        _verySlowRequestThresholdMs = verySlowRequestThresholdMs;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;

        context.Response.OnStarting(state =>
        {
            var (httpContext, sw) = ((HttpContext Context, Stopwatch Stopwatch))state;
            httpContext.Response.Headers["X-Response-Time-Ms"] = sw.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        }, (context, stopwatch));

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Log slow requests
            if (elapsedMs >= _verySlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Very slow request detected: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms). " +
                    "StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    method, path, elapsedMs, _verySlowRequestThresholdMs,
                    context.Response.StatusCode, context.TraceIdentifier);
            }
            else if (elapsedMs >= _slowRequestThresholdMs)
            {
                _logger.LogInformation(
                    "Slow request: {Method} {Path} took {ElapsedMs}ms (threshold: {Threshold}ms). " +
                    "StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    method, path, elapsedMs, _slowRequestThresholdMs,
                    context.Response.StatusCode, context.TraceIdentifier);
            }
            else if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Request completed: {Method} {Path} in {ElapsedMs}ms",
                    method, path, elapsedMs);
            }
        }
    }
}
