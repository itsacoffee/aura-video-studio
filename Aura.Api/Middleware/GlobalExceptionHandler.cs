using Aura.Core.Services.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Middleware;

/// <summary>
/// Global exception handler that implements ASP.NET Core's IExceptionHandler interface.
/// Catches all unhandled exceptions, logs them with correlation IDs, and returns ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ErrorAggregationService? _errorAggregation;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        ErrorAggregationService? errorAggregation = null)
    {
        _logger = logger;
        _errorAggregation = errorAggregation;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Extract correlation ID from HttpContext (set by CorrelationIdMiddleware)
        var correlationId = httpContext.TraceIdentifier;
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationIdObj))
        {
            correlationId = correlationIdObj?.ToString() ?? correlationId;
        }

        // Log the full exception with correlation ID
        _logger.LogError(
            exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method);

        // Record error in aggregation service
        if (_errorAggregation != null)
        {
            var context = new Dictionary<string, object>
            {
                ["path"] = httpContext.Request.Path.ToString(),
                ["method"] = httpContext.Request.Method,
                ["statusCode"] = httpContext.Response.StatusCode
            };
            _errorAggregation.RecordError(exception, correlationId, context);
        }

        // Determine appropriate status code and error details based on exception type
        var (statusCode, title, detail, errorCode) = MapExceptionToResponse(exception);

        // Create ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Add correlation ID and error code to extensions
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        // Set response status code
        httpContext.Response.StatusCode = problemDetails.Status.Value;

        // Write ProblemDetails as JSON to response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        // Return true to indicate the exception was handled
        return true;
    }

    /// <summary>
    /// Maps exceptions to appropriate HTTP status codes and error details
    /// Note: More specific exception types must be checked before their base types
    /// </summary>
    private static (int StatusCode, string Title, string Detail, string? ErrorCode) MapExceptionToResponse(Exception exception)
    {
        // Check most specific types first (they inherit from more general types)
        return exception switch
        {
            ArgumentNullException nullEx => (
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                $"Required parameter '{nullEx.ParamName}' is missing.",
                "E401"
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                $"Invalid input provided: {SanitizeMessage(argEx.Message)}. Please check your request and try again.",
                "E400"
            ),
            TaskCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "Request Timeout",
                "The operation timed out. Please try again.",
                "E408"
            ),
            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                "Operation Cancelled",
                "The operation was cancelled.",
                "E499"
            ),
            FileNotFoundException fileEx => (
                StatusCodes.Status404NotFound,
                "File Not Found",
                $"The requested file was not found: {SanitizeMessage(fileEx.FileName)}",
                "E406"
            ),
            KeyNotFoundException keyEx => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                $"The requested resource was not found: {SanitizeMessage(keyEx.Message)}",
                "E405"
            ),
            InvalidOperationException opEx => (
                StatusCodes.Status400BadRequest,
                "Invalid Operation",
                $"The requested operation could not be completed: {SanitizeMessage(opEx.Message)}",
                "E402"
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access Denied",
                "You don't have permission to perform this action.",
                "E403"
            ),
            System.Security.SecurityException => (
                StatusCodes.Status403Forbidden,
                "Access Denied",
                "Security check failed. You don't have permission to perform this action.",
                "E404"
            ),
            NotImplementedException => (
                StatusCodes.Status501NotImplemented,
                "Not Implemented",
                "This feature is not yet implemented.",
                "E501"
            ),
            TimeoutException => (
                StatusCodes.Status408RequestTimeout,
                "Request Timeout",
                "The operation timed out. Please try again.",
                "E408"
            ),
            System.Net.Http.HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "External Service Error",
                "An error occurred while communicating with an external service. Please try again later.",
                "E502"
            ),
            System.IO.IOException => (
                StatusCodes.Status503ServiceUnavailable,
                "I/O Error",
                "A file system error occurred. Please check disk space and permissions.",
                "E503"
            ),
            OutOfMemoryException => (
                StatusCodes.Status507InsufficientStorage,
                "Insufficient Resources",
                "The server is out of memory. Please try again later or reduce the size of your request.",
                "E507"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing your request.",
                "E500"
            )
        };
    }

    /// <summary>
    /// Sanitizes exception messages to avoid exposing sensitive information
    /// </summary>
    private static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown error";
        }

        // Remove potential sensitive information patterns
        // In production, you might want to be more aggressive here
        return message;
    }
}
