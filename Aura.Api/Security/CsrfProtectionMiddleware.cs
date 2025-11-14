using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Api.Security;

/// <summary>
/// Middleware that implements CSRF (Cross-Site Request Forgery) protection
/// Uses the double-submit cookie pattern with token validation
/// </summary>
public class CsrfProtectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CsrfProtectionMiddleware> _logger;
    
    private const string CsrfTokenCookieName = "XSRF-TOKEN";
    private const string CsrfTokenHeaderName = "X-XSRF-TOKEN";
    private const int TokenExpirationMinutes = 60;

    private static readonly string[] SafeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };
    private static readonly string[] ExemptPaths = { "/health", "/healthz", "/api/health", "/swagger" };

    public CsrfProtectionMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<CsrfProtectionMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();

        // Exempt safe methods and specific paths
        if (SafeMethods.Contains(method) || ExemptPaths.Any(p => path.StartsWith(p)))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // For state-changing requests, validate CSRF token
        var cookieToken = context.Request.Cookies[CsrfTokenCookieName];
        var headerToken = context.Request.Headers[CsrfTokenHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
        {
            _logger.LogWarning(
                "[{CorrelationId}] CSRF token missing for {Method} {Path}",
                context.TraceIdentifier, method, path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E403",
                title = "CSRF Token Missing",
                status = 403,
                detail = "CSRF token is required for this request. Include the XSRF-TOKEN cookie value in the X-XSRF-TOKEN header.",
                correlationId = context.TraceIdentifier
            }).ConfigureAwait(false);
            return;
        }

        // Validate token using constant-time comparison
        if (!ValidateToken(cookieToken, headerToken))
        {
            _logger.LogWarning(
                "[{CorrelationId}] CSRF token validation failed for {Method} {Path}",
                context.TraceIdentifier, method, path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E403",
                title = "CSRF Token Invalid",
                status = 403,
                detail = "CSRF token validation failed. Token may be expired or invalid.",
                correlationId = context.TraceIdentifier
            }).ConfigureAwait(false);
            return;
        }

        // Generate new token for next request (rotation)
        SetCsrfToken(context);

        await _next(context).ConfigureAwait(false);
    }

    private bool ValidateToken(string cookieToken, string headerToken)
    {
        if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
        {
            return false;
        }

        try
        {
            // Constant-time comparison to prevent timing attacks
            var cookieBytes = Encoding.UTF8.GetBytes(cookieToken);
            var headerBytes = Encoding.UTF8.GetBytes(headerToken);

            if (cookieBytes.Length != headerBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
        }
        catch
        {
            return false;
        }
    }

    private void SetCsrfToken(HttpContext context)
    {
        var token = GenerateSecureToken();
        
        context.Response.Cookies.Append(CsrfTokenCookieName, token, new CookieOptions
        {
            HttpOnly = false, // Must be readable by JavaScript
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(TokenExpirationMinutes),
            Path = "/"
        });
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}

/// <summary>
/// Extension methods for CSRF protection middleware
/// </summary>
public static class CsrfProtectionMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfProtectionMiddleware>();
    }
}
