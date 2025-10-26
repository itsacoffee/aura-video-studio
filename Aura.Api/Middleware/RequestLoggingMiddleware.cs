using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware for comprehensive request/response logging
/// Logs method, path, status code, duration, user ID, and correlation ID
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var clientIp = GetClientIpAddress(context);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Log request
            _logger.LogInformation(
                "[{CorrelationId}] Request: {Method} {Path}{QueryString} from {ClientIp}",
                correlationId, method, path, queryString, clientIp);

            await _next(context);

            stopwatch.Stop();

            // Log response
            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevelForStatusCode(statusCode);
            
            _logger.Log(logLevel,
                "[{CorrelationId}] Response: {Method} {Path} {StatusCode} in {Duration}ms",
                correlationId, method, path, statusCode, stopwatch.ElapsedMilliseconds);

            // Log slow requests (>5 seconds)
            if (stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] SLOW REQUEST: {Method} {Path} took {Duration}ms",
                    correlationId, method, path, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "[{CorrelationId}] Request failed: {Method} {Path} after {Duration}ms - {ErrorMessage}",
                correlationId, method, path, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }

    private string GetClientIpAddress(HttpContext context)
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

    private LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}

/// <summary>
/// Extension methods for request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
