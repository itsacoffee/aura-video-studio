using Aura.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for configuring security services
/// </summary>
public static class SecurityServicesExtensions
{
    /// <summary>
    /// Configures all security-related services including authentication, authorization, and Key Vault
    /// </summary>
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure API authentication options
        services.Configure<ApiAuthenticationOptions>(
            configuration.GetSection("Authentication"));

        // Configure Key Vault options
        services.Configure<KeyVaultOptions>(
            configuration.GetSection("KeyVault"));

        // Register Key Vault secret manager
        services.AddSingleton<IKeyVaultSecretManager, KeyVaultSecretManager>();
        services.AddHostedService<KeyVaultRefreshBackgroundService>();

        // Register audit logger
        services.AddSingleton<IAuditLogger, AuditLogger>();

        // Configure JWT authentication
        var jwtSettings = configuration.GetSection("Authentication");
        var jwtSecret = jwtSettings["JwtSecretKey"];
        
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["JwtAudience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<IAuditLogger>();
                        
                        logger.LogAuthenticationFailure(
                            context.Principal?.Identity?.Name ?? "Unknown",
                            context.Exception.Message,
                            context.HttpContext.Connection.RemoteIpAddress?.ToString());
                        
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<IAuditLogger>();
                        
                        logger.LogAuthenticationSuccess(
                            context.Principal?.Identity?.Name ?? "Unknown",
                            context.HttpContext.Connection.RemoteIpAddress?.ToString());
                        
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // Configure authorization policies
        services.ConfigureAuthorizationPolicies();

        // Register authorization handlers
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, RateLimitBypassAuthorizationHandler>();

        // Configure cookie policy for secure cookies
        services.Configure<Microsoft.AspNetCore.Http.CookiePolicyOptions>(options =>
        {
            options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });

        // Configure CORS with security in mind
        services.AddCors(options =>
        {
            options.AddPolicy("SecurePolicy", policy =>
            {
                policy.WithOrigins(
                    configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            });

            // Restrictive policy for sensitive endpoints
            options.AddPolicy("RestrictivePolicy", policy =>
            {
                policy.WithOrigins(
                    configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                    .WithMethods("GET", "POST")
                    .WithHeaders("Content-Type", "Authorization", "X-XSRF-TOKEN")
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the security middleware pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
        // Security headers should be first
        app.UseSecurityHeaders();

        // HTTPS enforcement
        app.UseHttpsEnforcement(enforceHttps: true);

        // Cookie policy
        app.UseCookiePolicy();

        // CORS
        app.UseCors("SecurePolicy");

        // Authentication
        app.UseAuthentication();

        // Authorization
        app.UseAuthorization();

        // CSRF protection (after authentication)
        app.UseCsrfProtection();

        // Audit logging
        app.UseAuditLogging();

        return app;
    }
}
