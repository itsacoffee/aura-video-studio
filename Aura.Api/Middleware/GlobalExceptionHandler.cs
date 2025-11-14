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

        // Create ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "An error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = SanitizeExceptionMessage(exception),
            Instance = httpContext.Request.Path
        };

        // Add correlation ID to extensions
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Set response status code
        httpContext.Response.StatusCode = problemDetails.Status.Value;

        // Write ProblemDetails as JSON to response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        // Return true to indicate the exception was handled
        return true;
    }

    /// <summary>
    /// Sanitizes exception messages to avoid exposing sensitive information or stack traces to clients
    /// </summary>
    private static string SanitizeExceptionMessage(Exception exception)
    {
        // In production, we want to hide implementation details
        // But keep useful information for debugging
        
        // For known exception types, we can provide more specific messages
        return exception switch
        {
            ArgumentException => "Invalid input provided. Please check your request and try again.",
            InvalidOperationException => "The requested operation could not be completed.",
            UnauthorizedAccessException => "Access denied. You don't have permission to perform this action.",
            NotImplementedException => "This feature is not yet implemented.",
            OperationCanceledException => "The operation was cancelled.",
            TimeoutException => "The operation timed out. Please try again.",
            _ => "An unexpected error occurred while processing your request."
        };
    }
}
