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
    private readonly ErrorMappingService _errorMappingService;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        ErrorMappingService errorMappingService,
        ErrorMetricsCollector? metricsCollector = null)
    {
        _next = next;
        _logger = logger;
        _errorMappingService = errorMappingService;
        _metricsCollector = metricsCollector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Get correlation ID from context (set by CorrelationIdMiddleware)
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        // Record error in metrics collector
        var serviceName = context.Request.Path.ToString().Split('/').FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? "unknown";
        _metricsCollector?.RecordError(serviceName, exception, correlationId);

        // Use ErrorMappingService to get standardized error response
        var errorResponse = _errorMappingService.MapException(exception, correlationId);

        // Log the exception with appropriate level
        LogException(exception, correlationId, (HttpStatusCode)errorResponse.Status);

        // Set response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = errorResponse.Status;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options)).ConfigureAwait(false);
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
