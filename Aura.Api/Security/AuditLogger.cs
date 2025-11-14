using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;

namespace Aura.Api.Security;

/// <summary>
/// Centralized security audit logging service
/// Logs security-relevant events for compliance and forensics
/// </summary>
public interface IAuditLogger
{
    void LogAuthenticationAttempt(string username, bool success, string? reason = null);
    void LogAuthenticationSuccess(string userId, string? ipAddress = null);
    void LogAuthenticationFailure(string username, string reason, string? ipAddress = null);
    void LogAuthorizationFailure(string userId, string resource, string action, string reason);
    void LogSensitiveDataAccess(string userId, string resource, string action);
    void LogConfigurationChange(string userId, string setting, string oldValue, string newValue);
    void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? additionalData = null);
    void LogApiKeyUsage(string apiKeyId, string endpoint, bool success);
    void LogRateLimitExceeded(string clientId, string endpoint);
    void LogInputValidationFailure(string endpoint, string field, string reason);
    void LogSuspiciousActivity(string description, string? ipAddress = null, Dictionary<string, object>? details = null);
}

public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogAuthenticationAttempt(string username, bool success, string? reason = null)
    {
        using (LogContext.PushProperty("EventType", "Authentication"))
        using (LogContext.PushProperty("Username", SanitizeForLogging(username)))
        using (LogContext.PushProperty("Success", success))
        using (LogContext.PushProperty("Reason", reason ?? "N/A"))
        {
            if (success)
            {
                _logger.LogInformation(
                    "[AUDIT] Authentication successful for user {Username}",
                    SanitizeForLogging(username));
            }
            else
            {
                _logger.LogWarning(
                    "[AUDIT] Authentication failed for user {Username}: {Reason}",
                    SanitizeForLogging(username), reason ?? "Unknown");
            }
        }
    }

    public void LogAuthenticationSuccess(string userId, string? ipAddress = null)
    {
        using (LogContext.PushProperty("EventType", "Authentication"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("IpAddress", ipAddress ?? "Unknown"))
        {
            _logger.LogInformation(
                "[AUDIT] User {UserId} authenticated successfully from {IpAddress}",
                userId, ipAddress ?? "Unknown");
        }
    }

    public void LogAuthenticationFailure(string username, string reason, string? ipAddress = null)
    {
        using (LogContext.PushProperty("EventType", "AuthenticationFailure"))
        using (LogContext.PushProperty("Username", SanitizeForLogging(username)))
        using (LogContext.PushProperty("Reason", reason))
        using (LogContext.PushProperty("IpAddress", ipAddress ?? "Unknown"))
        {
            _logger.LogWarning(
                "[AUDIT] Authentication failed for {Username} from {IpAddress}: {Reason}",
                SanitizeForLogging(username), ipAddress ?? "Unknown", reason);
        }
    }

    public void LogAuthorizationFailure(string userId, string resource, string action, string reason)
    {
        using (LogContext.PushProperty("EventType", "AuthorizationFailure"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Resource", resource))
        using (LogContext.PushProperty("Action", action))
        using (LogContext.PushProperty("Reason", reason))
        {
            _logger.LogWarning(
                "[AUDIT] Authorization failed for user {UserId} accessing {Resource} ({Action}): {Reason}",
                userId, resource, action, reason);
        }
    }

    public void LogSensitiveDataAccess(string userId, string resource, string action)
    {
        using (LogContext.PushProperty("EventType", "SensitiveDataAccess"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Resource", resource))
        using (LogContext.PushProperty("Action", action))
        {
            _logger.LogInformation(
                "[AUDIT] User {UserId} accessed sensitive data: {Resource} ({Action})",
                userId, resource, action);
        }
    }

    public void LogConfigurationChange(string userId, string setting, string oldValue, string newValue)
    {
        using (LogContext.PushProperty("EventType", "ConfigurationChange"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Setting", setting))
        {
            _logger.LogInformation(
                "[AUDIT] Configuration changed by {UserId}: {Setting} changed from '{OldValue}' to '{NewValue}'",
                userId, setting, MaskSensitiveValue(oldValue), MaskSensitiveValue(newValue));
        }
    }

    public void LogSecurityEvent(string eventType, string description, Dictionary<string, object>? additionalData = null)
    {
        using (LogContext.PushProperty("EventType", eventType))
        {
            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    using (LogContext.PushProperty(kvp.Key, kvp.Value))
                    {
                        // Property added to context
                    }
                }
            }

            _logger.LogInformation("[AUDIT] {EventType}: {Description}", eventType, description);
        }
    }

    public void LogApiKeyUsage(string apiKeyId, string endpoint, bool success)
    {
        using (LogContext.PushProperty("EventType", "ApiKeyUsage"))
        using (LogContext.PushProperty("ApiKeyId", apiKeyId))
        using (LogContext.PushProperty("Endpoint", endpoint))
        using (LogContext.PushProperty("Success", success))
        {
            _logger.LogInformation(
                "[AUDIT] API key {ApiKeyId} used for {Endpoint}: {Result}",
                apiKeyId, endpoint, success ? "Success" : "Failed");
        }
    }

    public void LogRateLimitExceeded(string clientId, string endpoint)
    {
        using (LogContext.PushProperty("EventType", "RateLimitExceeded"))
        using (LogContext.PushProperty("ClientId", clientId))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            _logger.LogWarning(
                "[AUDIT] Rate limit exceeded for client {ClientId} on {Endpoint}",
                clientId, endpoint);
        }
    }

    public void LogInputValidationFailure(string endpoint, string field, string reason)
    {
        using (LogContext.PushProperty("EventType", "InputValidationFailure"))
        using (LogContext.PushProperty("Endpoint", endpoint))
        using (LogContext.PushProperty("Field", field))
        using (LogContext.PushProperty("Reason", reason))
        {
            _logger.LogWarning(
                "[AUDIT] Input validation failed for {Endpoint}.{Field}: {Reason}",
                endpoint, field, reason);
        }
    }

    public void LogSuspiciousActivity(string description, string? ipAddress = null, Dictionary<string, object>? details = null)
    {
        using (LogContext.PushProperty("EventType", "SuspiciousActivity"))
        using (LogContext.PushProperty("IpAddress", ipAddress ?? "Unknown"))
        {
            if (details != null)
            {
                foreach (var kvp in details)
                {
                    using (LogContext.PushProperty(kvp.Key, kvp.Value))
                    {
                        // Property added to context
                    }
                }
            }

            _logger.LogWarning(
                "[AUDIT] Suspicious activity detected from {IpAddress}: {Description}",
                ipAddress ?? "Unknown", description);
        }
    }

    private static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Remove newlines and control characters that could be used for log injection
        return input
            .Replace("\r", string.Empty)
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Trim();
    }

    private static string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "[empty]";
        }

        // Mask sensitive values (API keys, passwords, etc.)
        if (value.Length <= 8)
        {
            return "****";
        }

        // Show first 4 and last 4 characters
        return $"{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}";
    }
}

/// <summary>
/// Middleware that logs security-relevant HTTP requests
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogger _auditLogger;

    public AuditLoggingMiddleware(RequestDelegate next, IAuditLogger auditLogger)
    {
        _next = next;
        _auditLogger = auditLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log sensitive endpoint access
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        
        if (IsSensitiveEndpoint(path))
        {
            var userId = context.User?.Identity?.Name ?? "Anonymous";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            _auditLogger.LogSecurityEvent(
                "SensitiveEndpointAccess",
                $"Access to {path}",
                new Dictionary<string, object>
                {
                    ["UserId"] = userId,
                    ["IpAddress"] = ipAddress ?? "Unknown",
                    ["Method"] = context.Request.Method,
                    ["UserAgent"] = context.Request.Headers.UserAgent.ToString() ?? "Unknown"
                });
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitivePaths = new[]
        {
            "/api/settings",
            "/api/providers",
            "/api/keyvault",
            "/api/configuration",
            "/api/users",
            "/api/admin"
        };

        return sensitivePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditLoggingMiddleware>();
    }
}
