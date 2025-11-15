using System;
using System.Collections.Generic;

namespace Aura.Core.Errors;

/// <summary>
/// Exception thrown when network connectivity or communication errors occur
/// </summary>
public class NetworkException : AuraException
{
    /// <summary>
    /// The target endpoint that was being accessed
    /// </summary>
    public string? Endpoint { get; }

    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
    public int? HttpStatusCode { get; }

    /// <summary>
    /// Detailed steps on how to fix the issue
    /// </summary>
    public string[] HowToFix { get; }

    public NetworkException(
        string errorCode,
        string message,
        string? userMessage = null,
        string? endpoint = null,
        int? httpStatusCode = null,
        string[]? howToFix = null,
        string? correlationId = null,
        bool isTransient = true,
        string[]? suggestedActions = null,
        Exception? innerException = null)
        : base(
            message,
            errorCode,
            userMessage ?? message,
            correlationId,
            suggestedActions ?? GenerateDefaultSuggestedActions(errorCode),
            isTransient,
            innerException)
    {
        Endpoint = endpoint;
        HttpStatusCode = httpStatusCode;
        HowToFix = howToFix ?? GenerateDefaultHowToFix(errorCode);

        if (!string.IsNullOrEmpty(endpoint))
        {
            WithContext("endpoint", endpoint);
        }
        if (httpStatusCode.HasValue)
        {
            WithContext("httpStatusCode", httpStatusCode.Value);
        }
    }

    /// <summary>
    /// Creates a BackendUnreachable error
    /// </summary>
    public static NetworkException BackendUnreachable(string endpoint, string? correlationId = null, Exception? innerException = null)
    {
        return new NetworkException(
            NetworkErrorCodes.BackendUnreachable,
            $"Cannot reach backend service at {endpoint}",
            $"Cannot reach backend service. The API server at {endpoint} is not responding.",
            endpoint,
            null,
            new[]
            {
                $"1. Verify the backend service is running on {endpoint}",
                "2. Check that no firewall is blocking the connection",
                "3. If using Electron, ensure the backend process started successfully",
                "4. Check logs for backend startup errors",
                "5. Try restarting the application"
            },
            correlationId,
            isTransient: false,
            suggestedActions: new[]
            {
                "Check if backend service is running",
                "Verify network connectivity",
                "Check firewall settings",
                "Restart the application"
            },
            innerException);
    }

    /// <summary>
    /// Creates a DNS resolution error
    /// </summary>
    public static NetworkException DnsResolutionFailed(string endpoint, string? correlationId = null, Exception? innerException = null)
    {
        return new NetworkException(
            NetworkErrorCodes.DnsResolutionFailed,
            $"Failed to resolve DNS for {endpoint}",
            $"Cannot resolve the hostname {endpoint}. DNS lookup failed.",
            endpoint,
            null,
            new[]
            {
                "1. Check your internet connection",
                "2. Verify the endpoint URL is correct",
                "3. Try using Google DNS (8.8.8.8) or Cloudflare DNS (1.1.1.1)",
                "4. Flush DNS cache: 'ipconfig /flushdns' (Windows) or 'sudo systemd-resolve --flush-caches' (Linux)",
                "5. Check if a VPN is interfering with DNS resolution"
            },
            correlationId,
            isTransient: true,
            suggestedActions: new[]
            {
                "Check internet connection",
                "Verify the URL is correct",
                "Try alternative DNS servers",
                "Flush DNS cache"
            },
            innerException);
    }

    /// <summary>
    /// Creates a TLS handshake error
    /// </summary>
    public static NetworkException TlsHandshakeFailed(string endpoint, string? correlationId = null, Exception? innerException = null)
    {
        return new NetworkException(
            NetworkErrorCodes.TlsHandshakeFailed,
            $"TLS/SSL handshake failed for {endpoint}",
            $"Failed to establish a secure connection to {endpoint}. SSL/TLS error.",
            endpoint,
            null,
            new[]
            {
                "1. Update your operating system and security certificates",
                "2. If behind a corporate proxy, install corporate SSL certificates",
                "3. Check system date and time are correct",
                "4. Temporarily disable SSL inspection in corporate proxies (contact IT)",
                "5. Update .NET runtime to the latest version"
            },
            correlationId,
            isTransient: false,
            suggestedActions: new[]
            {
                "Update system certificates",
                "Check corporate proxy settings",
                "Verify system date/time",
                "Contact IT if on corporate network"
            },
            innerException);
    }

    /// <summary>
    /// Creates a network timeout error
    /// </summary>
    public static NetworkException NetworkTimeout(string endpoint, int timeoutSeconds, string? correlationId = null, Exception? innerException = null)
    {
        return new NetworkException(
            NetworkErrorCodes.NetworkTimeout,
            $"Request to {endpoint} timed out after {timeoutSeconds} seconds",
            $"The request timed out. The server at {endpoint} did not respond within {timeoutSeconds} seconds.",
            endpoint,
            null,
            new[]
            {
                "1. Check your internet connection speed",
                "2. The provider service might be overloaded - try again in a few minutes",
                "3. If the issue persists, increase timeout in settings",
                "4. Try using a wired connection instead of WiFi",
                "5. Check if a VPN is causing delays"
            },
            correlationId,
            isTransient: true,
            suggestedActions: new[]
            {
                "Check internet connection",
                "Wait a few minutes and retry",
                "Increase timeout in settings",
                "Try wired connection"
            },
            innerException);
    }

    /// <summary>
    /// Creates a CORS misconfiguration error
    /// </summary>
    public static NetworkException CorsMisconfigured(string origin, string? correlationId = null)
    {
        return new NetworkException(
            NetworkErrorCodes.CorsMisconfigured,
            $"CORS policy blocked request from origin {origin}",
            $"Cross-Origin Request Blocked: The origin {origin} is not allowed by CORS policy.",
            origin,
            null,
            new[]
            {
                $"1. Check that {origin} is in the backend's AllowedOrigins configuration",
                "2. If running in development, ensure VITE_API_BASE_URL matches backend URL",
                "3. If running in Electron, ensure custom protocol is whitelisted",
                "4. Check appsettings.json CORS configuration in backend",
                "5. Restart both frontend and backend after configuration changes"
            },
            correlationId,
            isTransient: false,
            suggestedActions: new[]
            {
                "Check CORS configuration in backend",
                "Verify API base URL matches",
                "Restart application after config changes"
            });
    }

    /// <summary>
    /// Creates a provider unavailable error
    /// </summary>
    public static NetworkException ProviderUnavailable(string providerName, string endpoint, string? correlationId = null, Exception? innerException = null)
    {
        return new NetworkException(
            NetworkErrorCodes.ProviderUnavailable,
            $"Provider {providerName} is currently unavailable",
            $"Cannot connect to {providerName}. The service at {endpoint} is not responding.",
            endpoint,
            null,
            new[]
            {
                $"1. Check {providerName} status page for outages",
                "2. Verify your API key is valid and has not expired",
                "3. Check if your internet connection can reach the provider",
                "4. Try using a VPN if the provider is blocked in your region",
                "5. Wait a few minutes and retry - the service may be temporarily down",
                "6. Consider configuring a fallback provider in settings"
            },
            correlationId,
            isTransient: true,
            suggestedActions: new[]
            {
                "Check provider status page",
                "Verify API key is valid",
                "Wait and retry in a few minutes",
                "Configure fallback provider"
            },
            innerException);
    }

    private static string[] GenerateDefaultSuggestedActions(string errorCode)
    {
        return errorCode switch
        {
            var code when code.StartsWith("NET") => new[]
            {
                "Check internet connection",
                "Verify endpoint is correct",
                "Check firewall settings",
                "Retry the operation"
            },
            _ => new[] { "Check network connectivity", "Retry the operation" }
        };
    }

    private static string[] GenerateDefaultHowToFix(string errorCode)
    {
        return errorCode switch
        {
            var code when code.StartsWith("NET") => new[]
            {
                "1. Verify your internet connection is working",
                "2. Check firewall and proxy settings",
                "3. Ensure the endpoint URL is correct",
                "4. Try again in a few moments"
            },
            _ => new[] { "1. Check network connectivity", "2. Retry the operation" }
        };
    }

    /// <summary>
    /// Creates a JSON-serializable representation with HowToFix included
    /// </summary>
    public override Dictionary<string, object> ToErrorResponse()
    {
        var response = base.ToErrorResponse();
        response["howToFix"] = HowToFix;
        response["category"] = "Network";
        
        if (!string.IsNullOrEmpty(Endpoint))
        {
            response["endpoint"] = Endpoint;
        }
        
        if (HttpStatusCode.HasValue)
        {
            response["httpStatusCode"] = HttpStatusCode.Value;
        }
        
        return response;
    }
}
