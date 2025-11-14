using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Aura.Api.Middleware;

/// <summary>
/// Middleware to generate and validate ETags for API responses
/// Implements RFC 7232 for conditional requests
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process GET and HEAD requests
        if (context.Request.Method != HttpMethods.Get && context.Request.Method != HttpMethods.Head)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Skip for streaming responses
        if (context.Request.Path.StartsWithSegments("/api/jobs/stream") ||
            context.Request.Path.StartsWithSegments("/api/queue/stream"))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context).ConfigureAwait(false);

        // Only add ETag for successful responses
        if (context.Response.StatusCode == 200 && responseBodyStream.Length > 0)
        {
            var etag = GenerateETag(responseBodyStream);
            context.Response.Headers[HeaderNames.ETag] = etag;

            // Check If-None-Match header
            if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch))
            {
                if (ifNoneMatch.ToString() == etag)
                {
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.ContentLength = 0;
                    context.Response.Body = originalBodyStream;
                    
                    _logger.LogDebug("ETag matched, returning 304 Not Modified for {Path}", context.Request.Path);
                    return;
                }
            }

            // Add cache control headers for cacheable responses
            if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
            {
                context.Response.Headers[HeaderNames.CacheControl] = "private, max-age=60";
            }
        }

        // Copy the response body back to the original stream
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
        context.Response.Body = originalBodyStream;
    }

    private static string GenerateETag(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        var etag = $"\"{Convert.ToBase64String(hash)}\"";
        
        stream.Seek(0, SeekOrigin.Begin);
        return etag;
    }
}
