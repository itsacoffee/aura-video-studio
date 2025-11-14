using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Aura.Api.Security;

/// <summary>
/// Middleware that adds comprehensive security headers to all responses
/// Implements OWASP security best practices
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy - prevents XSS attacks
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // Required for React
            "style-src 'self' 'unsafe-inline'; " + // Required for inline styles
            "img-src 'self' data: blob: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' ws: wss:; " + // Allow WebSocket connections
            "media-src 'self' blob:; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");

        // X-Content-Type-Options - prevents MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options - prevents clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection - enables XSS filter in older browsers
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security - enforces HTTPS
        // Only add if connection is secure
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains; preload");
        }

        // Referrer-Policy - controls referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy - controls browser features
        context.Response.Headers.Append("Permissions-Policy",
            "camera=(), microphone=(), geolocation=(), payment=()");

        // X-Permitted-Cross-Domain-Policies - restricts cross-domain policies
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // Remove server header to avoid information disclosure
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension methods for security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
