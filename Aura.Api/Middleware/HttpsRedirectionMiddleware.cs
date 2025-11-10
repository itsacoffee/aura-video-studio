using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware that enforces HTTPS connections
/// Redirects HTTP requests to HTTPS and adds security headers
/// </summary>
public class HttpsEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpsEnforcementMiddleware> _logger;
    private readonly bool _enforceHttps;
    private readonly int _httpsPort;

    public HttpsEnforcementMiddleware(
        RequestDelegate next,
        ILogger<HttpsEnforcementMiddleware> logger,
        bool enforceHttps = true,
        int httpsPort = 443)
    {
        _next = next;
        _logger = logger;
        _enforceHttps = enforceHttps;
        _httpsPort = httpsPort;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip enforcement for health check endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/healthz"))
        {
            await _next(context);
            return;
        }

        // Check if request is over HTTPS
        if (_enforceHttps && !context.Request.IsHttps)
        {
            // Check if this is a local development request
            var isLocalhost = context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                            context.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

            if (!isLocalhost)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] HTTP request to {Path} from {Host}. Redirecting to HTTPS.",
                    context.TraceIdentifier, path, context.Request.Host);

                // Build HTTPS URL
                var httpsUrl = $"https://{context.Request.Host.Host}";
                
                // Add port if not default HTTPS port
                if (_httpsPort != 443)
                {
                    httpsUrl += $":{_httpsPort}";
                }
                
                httpsUrl += context.Request.PathBase + context.Request.Path + context.Request.QueryString;

                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = httpsUrl;
                
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E301",
                    title = "HTTPS Required",
                    status = 301,
                    detail = "This service requires HTTPS. Please use HTTPS to access this endpoint.",
                    httpsUrl
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for HTTPS enforcement middleware
/// </summary>
public static class HttpsEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpsEnforcement(
        this IApplicationBuilder builder,
        bool enforceHttps = true,
        int httpsPort = 443)
    {
        return builder.Use((context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<HttpsEnforcementMiddleware>>();
            var middleware = new HttpsEnforcementMiddleware((ctx) => next(), logger, enforceHttps, httpsPort);
            return middleware.InvokeAsync(context);
        });
    }
}
