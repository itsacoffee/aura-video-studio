using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Aura.Api.Security;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware for request-level validation including content length and correlation ID
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;
    private readonly long _maxContentLength;

    public ValidationMiddleware(
        RequestDelegate next,
        ILogger<ValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        
        // Get max content length from configuration (default 10MB)
        _maxContentLength = configuration.GetValue<long>("Validation:MaxContentLengthBytes", 10 * 1024 * 1024);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Validate Content-Length header if present
        if (context.Request.ContentLength.HasValue)
        {
            if (context.Request.ContentLength.Value > _maxContentLength)
            {
                _logger.LogWarning(
                    "Request rejected: Content-Length {ContentLength} exceeds maximum {MaxContentLength}. Path: {Path}, CorrelationId: {CorrelationId}",
                    context.Request.ContentLength.Value,
                    _maxContentLength,
                    context.Request.Path,
                    context.TraceIdentifier);

                context.Response.StatusCode = 413; // Payload Too Large
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://docs.aura.studio/errors/E413",
                    title = "Request Too Large",
                    status = 413,
                    detail = $"Request body exceeds maximum size of {_maxContentLength / (1024 * 1024)} MB",
                    correlationId = context.TraceIdentifier
                });
                return;
            }
        }

        // Validate Correlation ID format if provided by client
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            var correlationIdValue = correlationId.ToString();
            if (!string.IsNullOrWhiteSpace(correlationIdValue) && 
                !InputSanitizer.IsValidGuid(correlationIdValue))
            {
                _logger.LogWarning(
                    "Invalid Correlation ID format provided: {CorrelationId}. Path: {Path}",
                    InputSanitizer.SanitizeForLogging(correlationIdValue),
                    context.Request.Path);

                // Don't reject the request, but log the issue
                // The CorrelationIdMiddleware will generate a new valid one
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering the ValidationMiddleware
/// </summary>
public static class ValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}
