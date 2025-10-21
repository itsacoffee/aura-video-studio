using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Middleware;

public class FirstRunMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirstRunMiddleware> _logger;

    public FirstRunMiddleware(RequestDelegate next, ILogger<FirstRunMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Allow setup API endpoints and setup page
        if (path.StartsWith("/api/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/assets", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Check if setup is completed
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configPath = Path.Combine(localAppData, "Aura", "config.json");

        if (!File.Exists(configPath))
        {
            _logger.LogInformation("Setup not completed, redirecting to setup wizard");
            
            // If it's an API call, return 428 Precondition Required
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 428;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Setup not completed",
                    message = "Please complete the setup wizard before using the application",
                    redirectTo = "/setup"
                }).ConfigureAwait(false);
                return;
            }

            // For page requests, let the SPA router handle the redirect
            await _next(context).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }
}

/// <summary>
/// Extension methods for registering the FirstRunMiddleware
/// </summary>
public static class FirstRunMiddlewareExtensions
{
    public static IApplicationBuilder UseFirstRunCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirstRunMiddleware>();
    }
}
