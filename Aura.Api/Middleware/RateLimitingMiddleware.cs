using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware for rate limiting API requests to prevent abuse
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitConfig> _endpointConfigs;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _endpointConfigs = new ConcurrentDictionary<string, RateLimitConfig>();

        // Configure rate limits for different endpoint patterns
        // General endpoints: 100 requests/minute
        _endpointConfigs["/api/"] = new RateLimitConfig { MaxRequests = 100, WindowSeconds = 60 };
        
        // Export/processing endpoints: 10 requests/minute
        _endpointConfigs["/api/export"] = new RateLimitConfig { MaxRequests = 10, WindowSeconds = 60 };
        _endpointConfigs["/api/render"] = new RateLimitConfig { MaxRequests = 10, WindowSeconds = 60 };
        _endpointConfigs["/api/jobs"] = new RateLimitConfig { MaxRequests = 10, WindowSeconds = 60 };
        _endpointConfigs["/api/quick"] = new RateLimitConfig { MaxRequests = 10, WindowSeconds = 60 };
        
        // Health endpoints: unlimited (needed for monitoring)
        _endpointConfigs["/api/health"] = new RateLimitConfig { MaxRequests = int.MaxValue, WindowSeconds = 60 };
        _endpointConfigs["/healthz"] = new RateLimitConfig { MaxRequests = int.MaxValue, WindowSeconds = 60 };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Find matching rate limit config (most specific match)
        var config = GetRateLimitConfig(path);
        if (config == null)
        {
            // No rate limiting for this endpoint
            await _next(context);
            return;
        }

        var cacheKey = $"ratelimit_{clientId}_{path}_{config.WindowSeconds}";
        var currentCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(config.WindowSeconds);
            return new RateLimitCounter { Count = 0, ResetAt = DateTime.UtcNow.AddSeconds(config.WindowSeconds) };
        });

        if (currentCount == null)
        {
            currentCount = new RateLimitCounter { Count = 0, ResetAt = DateTime.UtcNow.AddSeconds(config.WindowSeconds) };
        }

        currentCount.Count++;
        _cache.Set(cacheKey, currentCount, TimeSpan.FromSeconds(config.WindowSeconds));

        // Add rate limit headers
        context.Response.Headers.Append("X-RateLimit-Limit", config.MaxRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", Math.Max(0, config.MaxRequests - currentCount.Count).ToString());
        context.Response.Headers.Append("X-RateLimit-Reset", new DateTimeOffset(currentCount.ResetAt).ToUnixTimeSeconds().ToString());

        if (currentCount.Count > config.MaxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on path {Path}. Count: {Count}/{MaxRequests}",
                clientId, path, currentCount.Count, config.MaxRequests);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            
            var retryAfter = (int)(currentCount.ResetAt - DateTime.UtcNow).TotalSeconds;
            context.Response.Headers.Append("Retry-After", retryAfter.ToString());

            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E429",
                title = "Rate Limit Exceeded",
                status = 429,
                detail = $"Too many requests. Limit is {config.MaxRequests} requests per {config.WindowSeconds} seconds.",
                retryAfter = retryAfter,
                correlationId = context.TraceIdentifier
            });
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get client ID from various sources
        // In production, you might want to use authenticated user ID instead
        
        // Check for forwarded IP (if behind a proxy)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        // Fall back to connection remote IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return remoteIp;
    }

    private RateLimitConfig? GetRateLimitConfig(string path)
    {
        // Find the most specific matching config
        RateLimitConfig? matchedConfig = null;
        int longestMatch = 0;

        foreach (var kvp in _endpointConfigs)
        {
            if (path.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) && kvp.Key.Length > longestMatch)
            {
                matchedConfig = kvp.Value;
                longestMatch = kvp.Key.Length;
            }
        }

        return matchedConfig;
    }

    private class RateLimitConfig
    {
        public int MaxRequests { get; set; }
        public int WindowSeconds { get; set; }
    }

    private class RateLimitCounter
    {
        public int Count { get; set; }
        public DateTime ResetAt { get; set; }
    }
}

/// <summary>
/// Extension methods for rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
