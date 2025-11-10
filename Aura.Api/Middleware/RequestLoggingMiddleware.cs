using Aura.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware for comprehensive request/response logging with structured logging support
/// Logs method, path, status code, duration, user ID, and correlation ID
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;
    private readonly int _maxBodyLogSize;

    public RequestLoggingMiddleware(
        RequestDelegate next, 
        ILogger<RequestLoggingMiddleware> logger,
        bool logRequestBody = false,
        bool logResponseBody = false,
        int maxBodyLogSize = 4096)
    {
        _next = next;
        _logger = logger;
        _logRequestBody = logRequestBody;
        _logResponseBody = logResponseBody;
        _maxBodyLogSize = maxBodyLogSize;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"] as string ?? context.TraceIdentifier;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var clientIp = GetClientIpAddress(context);

        var stopwatch = Stopwatch.StartNew();
        string? requestBody = null;
        string? responseBody = null;

        try
        {
            // Optionally log request body for debugging
            if (_logRequestBody && context.Request.ContentLength > 0)
            {
                requestBody = await ReadRequestBodyAsync(context.Request);
            }

            // Log incoming request with structured data
            var requestMetadata = new Dictionary<string, object>
            {
                ["Method"] = method,
                ["Path"] = path,
                ["QueryString"] = queryString,
                ["ClientIp"] = clientIp,
                ["UserAgent"] = userAgent,
                ["ContentType"] = context.Request.ContentType ?? "none",
                ["ContentLength"] = context.Request.ContentLength ?? 0
            };

            _logger.LogStructured(
                LogLevel.Information,
                "Incoming request: {Method} {Path}{QueryString} from {ClientIp}",
                requestMetadata);

            // Capture response body if needed
            Stream originalBodyStream = context.Response.Body;
            if (_logResponseBody)
            {
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            else
            {
                await _next(context);
            }

            stopwatch.Stop();

            // Log response with performance metrics
            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevelForStatusCode(statusCode);
            var duration = stopwatch.Elapsed;

            var responseMetadata = new Dictionary<string, object>
            {
                ["Method"] = method,
                ["Path"] = path,
                ["StatusCode"] = statusCode,
                ["Duration"] = duration,
                ["DurationMs"] = duration.TotalMilliseconds,
                ["ResponseContentType"] = context.Response.ContentType ?? "none"
            };

            if (_logResponseBody && !string.IsNullOrEmpty(responseBody))
            {
                responseMetadata["ResponseBody"] = responseBody.Length > _maxBodyLogSize 
                    ? responseBody[.._maxBodyLogSize] + "... (truncated)" 
                    : responseBody;
            }

            _logger.LogStructured(
                logLevel,
                "Request completed: {Method} {Path} - {StatusCode} in {DurationMs}ms",
                responseMetadata);

            // Log slow requests with warning
            if (duration.TotalSeconds > 5)
            {
                _logger.LogWarning(
                    "SLOW REQUEST DETECTED: {Method} {Path} took {DurationMs}ms (Status: {StatusCode})",
                    method, path, duration.TotalMilliseconds, statusCode);
            }

            // Log performance metrics
            _logger.LogPerformance(
                $"{method} {path}",
                duration,
                success: statusCode < 400,
                new Dictionary<string, object>
                {
                    ["StatusCode"] = statusCode,
                    ["ClientIp"] = clientIp
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var errorMetadata = new Dictionary<string, object>
            {
                ["Method"] = method,
                ["Path"] = path,
                ["Duration"] = stopwatch.Elapsed,
                ["DurationMs"] = stopwatch.ElapsedMilliseconds,
                ["ClientIp"] = clientIp,
                ["ExceptionType"] = ex.GetType().Name
            };

            _logger.LogErrorWithContext(
                ex,
                "Request failed: {Method} {Path} after {DurationMs}ms",
                errorMetadata);

            throw;
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        
        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body.Length > _maxBodyLogSize 
            ? body[.._maxBodyLogSize] + "... (truncated)" 
            : body;
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
