using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware to add appropriate cache-control headers to API responses
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCachingMiddleware> _logger;

    public ResponseCachingMiddleware(RequestDelegate next, ILogger<ResponseCachingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Response.StatusCode == 200)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                var method = context.Request.Method;

                // Determine cache strategy based on endpoint
                var cacheControl = DetermineCacheControl(path, method);
                
                if (!string.IsNullOrEmpty(cacheControl) && 
                    !context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
                {
                    context.Response.Headers[HeaderNames.CacheControl] = cacheControl;
                    _logger.LogDebug("Added Cache-Control header: {CacheControl} for {Path}", cacheControl, path);
                }

                // Add Vary header for content negotiation
                if (!context.Response.Headers.ContainsKey(HeaderNames.Vary))
                {
                    context.Response.Headers[HeaderNames.Vary] = "Accept, Accept-Encoding";
                }
            }

            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }

    private static string DetermineCacheControl(string path, string method)
    {
        // No caching for non-GET requests
        if (method != HttpMethods.Get && method != HttpMethods.Head)
        {
            return "no-store";
        }

        // Configuration endpoints - cache for 5 minutes
        if (path.Contains("/api/configuration") || 
            path.Contains("/api/settings") ||
            path.Contains("/api/system"))
        {
            return "private, max-age=300"; // 5 minutes
        }

        // Provider profiles - cache for 10 minutes
        if (path.Contains("/api/providers") || 
            path.Contains("/api/profiles"))
        {
            return "private, max-age=600"; // 10 minutes
        }

        // Template endpoints - cache for 1 hour
        if (path.Contains("/api/templates"))
        {
            return "private, max-age=3600"; // 1 hour
        }

        // Health checks - cache for 30 seconds
        if (path.Contains("/health") || path.Contains("/api/health"))
        {
            return "public, max-age=30";
        }

        // Static metadata - cache for 24 hours
        if (path.Contains("/api/models") || 
            path.Contains("/api/version"))
        {
            return "public, max-age=86400"; // 24 hours
        }

        // Media library - cache for 5 minutes
        if (path.Contains("/api/media"))
        {
            return "private, max-age=300";
        }

        // Job/queue endpoints - no caching (real-time data)
        if (path.Contains("/api/jobs") || 
            path.Contains("/api/queue") ||
            path.Contains("/api/render") ||
            path.Contains("/api/export"))
        {
            return "no-cache, no-store, must-revalidate";
        }

        // Default: short-lived cache
        return "private, max-age=60"; // 1 minute
    }
}
