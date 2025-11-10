using Aura.Core.Errors;
using Aura.Core.Resilience.ErrorTracking;
using Aura.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Aura.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions,
/// logs them with correlation IDs, and returns standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ErrorMetricsCollector? _metricsCollector;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        ErrorMetricsCollector? metricsCollector = null)
    {
        _next = next;
        _logger = logger;
        _metricsCollector = metricsCollector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Get correlation ID from context (set by CorrelationIdMiddleware)
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");

        // Record error in metrics collector
        var serviceName = context.Request.Path.ToString().Split('/')[1]; // Extract service from path
        _metricsCollector?.RecordError(serviceName, exception, correlationId);

        // Determine status code and error response based on exception type
        var (statusCode, errorResponse) = MapExceptionToResponse(exception, correlationId);

        // Log the exception with appropriate level
        LogException(exception, correlationId, statusCode);

        // Set response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }

    private (HttpStatusCode, object) MapExceptionToResponse(Exception exception, string correlationId)
    {
        return exception switch
        {
            // Validation errors - 400 Bad Request
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new
                {
                    errorCode = validationEx.ErrorCode,
                    message = validationEx.UserMessage,
                    technicalDetails = validationEx.Message,
                    validationIssues = validationEx.Issues,
                    suggestedActions = validationEx.SuggestedActions,
                    correlationId,
                    timestamp = DateTime.UtcNow
                }),

            // Provider errors - map based on provider exception details
            ProviderException providerEx when providerEx.HttpStatusCode == 429 => (
                HttpStatusCode.TooManyRequests,
                CreateAuraExceptionResponse(providerEx, correlationId)),

            ProviderException providerEx when providerEx.HttpStatusCode == 401 || providerEx.HttpStatusCode == 403 => (
                HttpStatusCode.Unauthorized,
                CreateAuraExceptionResponse(providerEx, correlationId)),

            ProviderException providerEx => (
                providerEx.IsTransient ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.BadGateway,
                CreateAuraExceptionResponse(providerEx, correlationId)),

            // Resource errors - 409 Conflict or 507 Insufficient Storage
            ResourceException resourceEx when resourceEx.ResourceType == ResourceType.DiskSpace => (
                HttpStatusCode.InsufficientStorage,
                CreateAuraExceptionResponse(resourceEx, correlationId)),

            ResourceException resourceEx when resourceEx.ResourceType == ResourceType.FileNotFound => (
                HttpStatusCode.NotFound,
                CreateAuraExceptionResponse(resourceEx, correlationId)),

            ResourceException resourceEx when resourceEx.ResourceType == ResourceType.FileLocked ||
                                              resourceEx.ResourceType == ResourceType.FileAccess ||
                                              resourceEx.ResourceType == ResourceType.DirectoryAccess => (
                HttpStatusCode.Conflict,
                CreateAuraExceptionResponse(resourceEx, correlationId)),

            ResourceException resourceEx => (
                HttpStatusCode.InternalServerError,
                CreateAuraExceptionResponse(resourceEx, correlationId)),

            // Render errors - 500 Internal Server Error or 422 Unprocessable Entity
            RenderException renderEx when renderEx.Category == RenderErrorCategory.InvalidInput => (
                HttpStatusCode.UnprocessableEntity,
                CreateAuraExceptionResponse(renderEx, correlationId)),

            RenderException renderEx when renderEx.Category == RenderErrorCategory.Cancelled => (
                HttpStatusCode.Conflict,
                CreateAuraExceptionResponse(renderEx, correlationId)),

            RenderException renderEx when renderEx.Category == RenderErrorCategory.Timeout => (
                HttpStatusCode.RequestTimeout,
                CreateAuraExceptionResponse(renderEx, correlationId)),

            RenderException renderEx => (
                HttpStatusCode.InternalServerError,
                CreateAuraExceptionResponse(renderEx, correlationId)),

            // FFmpeg errors - 500 Internal Server Error
            FfmpegException ffmpegEx => (
                HttpStatusCode.InternalServerError,
                new
                {
                    errorCode = ffmpegEx.ErrorCode,
                    message = GenerateFfmpegUserMessage(ffmpegEx),
                    technicalDetails = ffmpegEx.Message,
                    category = ffmpegEx.Category.ToString(),
                    suggestedActions = ffmpegEx.SuggestedActions,
                    correlationId,
                    timestamp = DateTime.UtcNow,
                    exitCode = ffmpegEx.ExitCode
                }),

            // Other AuraException types
            AuraException auraEx => (
                auraEx.IsTransient ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.InternalServerError,
                CreateAuraExceptionResponse(auraEx, correlationId)),

            // Operation cancelled - 499 (Client Closed Request - non-standard but widely used)
            OperationCanceledException => (
                (HttpStatusCode)499,
                CreateStandardErrorResponse(
                    "E998",
                    "Operation was cancelled",
                    "The operation was cancelled, typically by the user",
                    new[] { "Retry the operation if it was cancelled unintentionally" },
                    correlationId
                )),

            // Argument exceptions - 400 Bad Request
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateStandardErrorResponse(
                    "E002",
                    "Invalid input provided",
                    argEx.Message,
                    new[] { "Review the request parameters and try again" },
                    correlationId
                )),

            // Not implemented - 501 Not Implemented
            NotImplementedException => (
                HttpStatusCode.NotImplemented,
                CreateStandardErrorResponse(
                    "E997",
                    "This feature is not yet implemented",
                    exception.Message,
                    new[] { "Check for updates or use an alternative feature" },
                    correlationId
                )),

            // Unauthorized access - 403 Forbidden
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                CreateStandardErrorResponse(
                    "E003",
                    "Access denied",
                    exception.Message,
                    new[] { "Check permissions and try again", "Contact an administrator if needed" },
                    correlationId
                )),

            // Default - 500 Internal Server Error
            _ => (
                HttpStatusCode.InternalServerError,
                CreateStandardErrorResponse(
                    "E999",
                    "An unexpected error occurred",
                    exception.Message,
                    new[]
                    {
                        "Try the operation again",
                        "Check application logs for more details",
                        "Contact support if the issue persists"
                    },
                    correlationId
                ))
        };
    }

    private static object CreateAuraExceptionResponse(AuraException auraEx, string correlationId)
    {
        var response = auraEx.ToErrorResponse();
        response["correlationId"] = correlationId;
        response["timestamp"] = DateTime.UtcNow;
        return response;
    }

    private static string GenerateFfmpegUserMessage(FfmpegException ffmpegEx)
    {
        return ffmpegEx.Category switch
        {
            FfmpegErrorCategory.NotFound => "FFmpeg is not installed. Please install it via the Download Center.",
            FfmpegErrorCategory.Corrupted => "FFmpeg binary is corrupted. Please reinstall via the Download Center.",
            FfmpegErrorCategory.EncoderNotFound => "Required video encoder is not available. Try using a different encoder.",
            FfmpegErrorCategory.InvalidInput => "Invalid input provided to video renderer.",
            FfmpegErrorCategory.PermissionDenied => "Permission denied when accessing files for video rendering.",
            FfmpegErrorCategory.Timeout => "Video rendering timed out. Try with shorter content or lower quality.",
            FfmpegErrorCategory.Crashed => "FFmpeg crashed unexpectedly. This may indicate corrupted installation.",
            _ => "Video rendering failed. Please check your settings and input files."
        };
    }

    private void LogException(Exception exception, string correlationId, HttpStatusCode statusCode)
    {
        var logLevel = DetermineLogLevel(exception, statusCode);
        var message = $"Request failed with {statusCode}: {exception.GetType().Name}";

        _logger.Log(logLevel, exception, "{Message} [CorrelationId: {CorrelationId}]", message, correlationId);

        // For certain critical errors, log additional context
        if (logLevel >= LogLevel.Error)
        {
            if (exception is AuraException auraEx && auraEx.Context.Count > 0)
            {
                _logger.LogError("Error context: {@Context}", auraEx.Context);
            }
        }
    }

    private static LogLevel DetermineLogLevel(Exception exception, HttpStatusCode statusCode)
    {
        // Transient/expected errors log as warnings
        if (exception is AuraException auraEx && auraEx.IsTransient)
        {
            return LogLevel.Warning;
        }

        // Client errors (4xx) log as warnings
        if ((int)statusCode >= 400 && (int)statusCode < 500)
        {
            return exception is ValidationException or ArgumentException
                ? LogLevel.Information
                : LogLevel.Warning;
        }

        // Cancellation is informational
        if (exception is OperationCanceledException)
        {
            return LogLevel.Information;
        }

        // Server errors (5xx) log as errors
        return LogLevel.Error;
    }

    /// <summary>
    /// Creates a standard error response object with documentation link
    /// </summary>
    private static object CreateStandardErrorResponse(
        string errorCode,
        string message,
        string technicalDetails,
        string[] suggestedActions,
        string correlationId)
    {
        var response = new Dictionary<string, object>
        {
            ["errorCode"] = errorCode,
            ["message"] = message,
            ["technicalDetails"] = technicalDetails,
            ["suggestedActions"] = suggestedActions,
            ["correlationId"] = correlationId,
            ["timestamp"] = DateTime.UtcNow
        };

        // Add "Learn More" link
        var documentation = ErrorDocumentation.GetDocumentation(errorCode);
        if (documentation != null)
        {
            response["learnMoreUrl"] = documentation.Url;
            response["errorTitle"] = documentation.Title;
        }
        else
        {
            response["learnMoreUrl"] = ErrorDocumentation.GetFallbackUrl();
        }

        return response;
    }
}

/// <summary>
/// Extension methods for registering the ExceptionHandlingMiddleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
