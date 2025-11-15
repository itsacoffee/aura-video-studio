using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Errors;

/// <summary>
/// Service for mapping exceptions to standardized error responses with user-friendly messages
/// </summary>
public class ErrorMappingService
{
    private readonly ILogger<ErrorMappingService> _logger;

    public ErrorMappingService(ILogger<ErrorMappingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Maps an exception to a structured error response
    /// </summary>
    public StandardErrorResponse MapException(Exception exception, string correlationId)
    {
        _logger.LogDebug("Mapping exception of type {ExceptionType} with correlation {CorrelationId}", 
            exception.GetType().Name, correlationId);

        return exception switch
        {
            // Network exceptions
            NetworkException networkEx => MapNetworkException(networkEx, correlationId),
            HttpRequestException httpEx => MapHttpRequestException(httpEx, correlationId),
            SocketException socketEx => MapSocketException(socketEx, correlationId),
            TimeoutException timeoutEx => MapTimeoutException(timeoutEx, correlationId),
            AuthenticationException authEx => MapAuthenticationException(authEx, correlationId),
            
            // Validation exceptions
            ValidationException validationEx => MapValidationException(validationEx, correlationId),
            ArgumentException argEx => MapArgumentException(argEx, correlationId),
            
            // Provider exceptions
            ProviderException providerEx => MapProviderException(providerEx, correlationId),
            
            // Configuration exceptions
            ConfigurationException configEx => MapConfigurationException(configEx, correlationId),
            
            // Aura exceptions (base class)
            AuraException auraEx => MapAuraException(auraEx, correlationId),
            
            // Fallback for unknown exceptions
            _ => MapUnknownException(exception, correlationId)
        };
    }

    private StandardErrorResponse MapNetworkException(NetworkException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#{ex.ErrorCode.ToLowerInvariant()}";
        
        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Network Error",
            Status: ex.HttpStatusCode ?? (int)HttpStatusCode.ServiceUnavailable,
            Detail: ex.UserMessage,
            CorrelationId: correlationId,
            ErrorCode: ex.ErrorCode,
            HowToFix: ex.HowToFix,
            FieldErrors: null,
            Context: ex.Context
        );
    }

    private StandardErrorResponse MapHttpRequestException(HttpRequestException ex, string correlationId)
    {
        // Determine specific network error type based on inner exception
        var (errorCode, title, detail, howToFix) = ex.InnerException switch
        {
            SocketException socketEx => (
                NetworkErrorCodes.ConnectionRefused,
                "Connection Refused",
                "The server refused the connection. The service may not be running or may be blocking connections.",
                new[]
                {
                    "1. Verify the server is running",
                    "2. Check firewall settings",
                    "3. Verify the port number is correct",
                    "4. Check if another application is using the port"
                }
            ),
            _ when ex.Message.Contains("Name or service not known") || ex.Message.Contains("No such host") => (
                NetworkErrorCodes.DnsResolutionFailed,
                "DNS Resolution Failed",
                "Cannot resolve the hostname. DNS lookup failed.",
                new[]
                {
                    "1. Check your internet connection",
                    "2. Verify the hostname is correct",
                    "3. Try alternative DNS servers (8.8.8.8, 1.1.1.1)",
                    "4. Flush DNS cache"
                }
            ),
            _ when ex.Message.Contains("SSL") || ex.Message.Contains("TLS") || ex.Message.Contains("certificate") => (
                NetworkErrorCodes.TlsHandshakeFailed,
                "TLS/SSL Error",
                "Failed to establish a secure connection. SSL/TLS handshake failed.",
                new[]
                {
                    "1. Update system certificates",
                    "2. Check system date and time",
                    "3. If behind corporate proxy, install corporate certificates",
                    "4. Verify the server's SSL certificate is valid"
                }
            ),
            _ => (
                NetworkErrorCodes.NetworkUnreachable,
                "Network Unreachable",
                "Cannot reach the remote server. The network may be down or the server is unreachable.",
                new[]
                {
                    "1. Check your internet connection",
                    "2. Verify the server address is correct",
                    "3. Check firewall and proxy settings",
                    "4. Try again in a few moments"
                }
            )
        };

        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#{errorCode.ToLowerInvariant()}";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: title,
            Status: (int)HttpStatusCode.ServiceUnavailable,
            Detail: detail,
            CorrelationId: correlationId,
            ErrorCode: errorCode,
            HowToFix: howToFix,
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["originalMessage"] = ex.Message
            }
        );
    }

    private StandardErrorResponse MapSocketException(SocketException ex, string correlationId)
    {
        var (errorCode, title, detail, howToFix) = ex.SocketErrorCode switch
        {
            SocketError.ConnectionRefused => (
                NetworkErrorCodes.ConnectionRefused,
                "Connection Refused",
                "The target server refused the connection.",
                new[]
                {
                    "1. Verify the server is running and accepting connections",
                    "2. Check that the port number is correct",
                    "3. Verify firewall settings allow the connection",
                    "4. Ensure no other application is using the port"
                }
            ),
            SocketError.HostUnreachable or SocketError.NetworkUnreachable => (
                NetworkErrorCodes.NetworkUnreachable,
                "Network Unreachable",
                "Cannot reach the target network or host.",
                new[]
                {
                    "1. Check your network connection",
                    "2. Verify the network adapter is enabled",
                    "3. Check router and network configuration",
                    "4. Verify the target address is correct"
                }
            ),
            SocketError.TimedOut => (
                NetworkErrorCodes.NetworkTimeout,
                "Connection Timed Out",
                "The connection attempt timed out.",
                new[]
                {
                    "1. Check your internet connection speed",
                    "2. Verify the server is responding",
                    "3. Try increasing the timeout value",
                    "4. Check for network congestion"
                }
            ),
            _ => (
                NetworkErrorCodes.NetworkUnreachable,
                "Network Error",
                $"A network error occurred: {ex.SocketErrorCode}",
                new[]
                {
                    "1. Check your network connection",
                    "2. Verify network settings",
                    "3. Try again in a few moments"
                }
            )
        };

        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#{errorCode.ToLowerInvariant()}";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: title,
            Status: (int)HttpStatusCode.ServiceUnavailable,
            Detail: detail,
            CorrelationId: correlationId,
            ErrorCode: errorCode,
            HowToFix: howToFix,
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["socketError"] = ex.SocketErrorCode.ToString()
            }
        );
    }

    private StandardErrorResponse MapTimeoutException(TimeoutException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#{NetworkErrorCodes.NetworkTimeout.ToLowerInvariant()}";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Request Timeout",
            Status: (int)HttpStatusCode.RequestTimeout,
            Detail: "The request timed out. The server did not respond within the expected time.",
            CorrelationId: correlationId,
            ErrorCode: NetworkErrorCodes.NetworkTimeout,
            HowToFix: new[]
            {
                "1. Check your internet connection speed",
                "2. The service may be overloaded - try again in a few minutes",
                "3. Increase timeout setting if the issue persists",
                "4. Check if a VPN or proxy is causing delays"
            },
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["originalMessage"] = ex.Message
            }
        );
    }

    private StandardErrorResponse MapAuthenticationException(AuthenticationException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/network-errors.md#{NetworkErrorCodes.TlsHandshakeFailed.ToLowerInvariant()}";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Authentication Error",
            Status: (int)HttpStatusCode.Unauthorized,
            Detail: "SSL/TLS authentication failed. Cannot establish a secure connection.",
            CorrelationId: correlationId,
            ErrorCode: NetworkErrorCodes.TlsHandshakeFailed,
            HowToFix: new[]
            {
                "1. Update system security certificates",
                "2. Check system date and time are correct",
                "3. If behind corporate proxy, install corporate SSL certificates",
                "4. Contact your network administrator"
            },
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["originalMessage"] = ex.Message
            }
        );
    }

    private StandardErrorResponse MapValidationException(ValidationException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/validation-errors.md#{ex.ErrorCode.ToLowerInvariant()}";

        // Convert FieldErrors from Dictionary<string, string[]> to Dictionary<string, string>
        // by joining multiple messages with semicolon
        Dictionary<string, string>? fieldErrors = null;
        if (ex.FieldErrors.Count > 0)
        {
            fieldErrors = new Dictionary<string, string>();
            foreach (var (field, messages) in ex.FieldErrors)
            {
                fieldErrors[field] = string.Join("; ", messages);
            }
        }

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Validation Error",
            Status: (int)HttpStatusCode.BadRequest,
            Detail: ex.UserMessage,
            CorrelationId: correlationId,
            ErrorCode: ex.ErrorCode,
            HowToFix: null,
            FieldErrors: fieldErrors,
            Context: ex.Context
        );
    }

    private StandardErrorResponse MapArgumentException(ArgumentException ex, string correlationId)
    {
        var docUrl = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/validation-errors.md#invalid-input";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Invalid Input",
            Status: (int)HttpStatusCode.BadRequest,
            Detail: ex.Message,
            CorrelationId: correlationId,
            ErrorCode: ValidationErrorCodes.InvalidInput,
            HowToFix: new[]
            {
                "1. Review the error message for specific field requirements",
                "2. Ensure all required fields are provided",
                "3. Verify field values are in the correct format",
                "4. Check that values are within acceptable ranges"
            },
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["parameterName"] = ex.ParamName ?? "unknown"
            }
        );
    }

    private StandardErrorResponse MapProviderException(ProviderException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/provider-errors.md#{ex.ErrorCode.ToLowerInvariant()}";

        var statusCode = ex.HttpStatusCode switch
        {
            401 or 403 => (int)HttpStatusCode.Unauthorized,
            429 => (int)HttpStatusCode.TooManyRequests,
            >= 500 => (int)HttpStatusCode.BadGateway,
            _ => (int)HttpStatusCode.BadRequest
        };

        return new StandardErrorResponse(
            Type: docUrl,
            Title: $"{ex.ProviderName} Error",
            Status: statusCode,
            Detail: ex.UserMessage,
            CorrelationId: correlationId,
            ErrorCode: ex.ErrorCode,
            HowToFix: ex.SuggestedActions,
            FieldErrors: null,
            Context: ex.Context
        );
    }

    private StandardErrorResponse MapConfigurationException(ConfigurationException ex, string correlationId)
    {
        var docUrl = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/general-errors.md#configuration-errors";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Configuration Error",
            Status: (int)HttpStatusCode.InternalServerError,
            Detail: ex.UserMessage,
            CorrelationId: correlationId,
            ErrorCode: ex.ErrorCode,
            HowToFix: ex.SuggestedActions,
            FieldErrors: null,
            Context: ex.Context
        );
    }

    private StandardErrorResponse MapAuraException(AuraException ex, string correlationId)
    {
        var docUrl = $"https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/general-errors.md#{ex.ErrorCode.ToLowerInvariant()}";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Application Error",
            Status: (int)HttpStatusCode.InternalServerError,
            Detail: ex.UserMessage,
            CorrelationId: correlationId,
            ErrorCode: ex.ErrorCode,
            HowToFix: ex.SuggestedActions,
            FieldErrors: null,
            Context: ex.Context
        );
    }

    private StandardErrorResponse MapUnknownException(Exception ex, string correlationId)
    {
        _logger.LogWarning("Mapping unknown exception type {ExceptionType}", ex.GetType().Name);

        var docUrl = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/troubleshooting/general-errors.md#unknown-errors";

        return new StandardErrorResponse(
            Type: docUrl,
            Title: "Unexpected Error",
            Status: (int)HttpStatusCode.InternalServerError,
            Detail: "An unexpected error occurred. Please try again or contact support if the problem persists.",
            CorrelationId: correlationId,
            ErrorCode: "ERR999_UnknownError",
            HowToFix: new[]
            {
                "1. Try the operation again",
                "2. Check the logs for more details",
                "3. If the problem persists, report it with the correlation ID",
                "4. Include steps to reproduce the error"
            },
            FieldErrors: null,
            Context: new Dictionary<string, object>
            {
                ["exceptionType"] = ex.GetType().Name,
                ["originalMessage"] = ex.Message
            }
        );
    }
}

/// <summary>
/// Standard error response format for all API errors
/// </summary>
public record StandardErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string CorrelationId,
    string ErrorCode,
    string[]? HowToFix,
    Dictionary<string, string>? FieldErrors,
    Dictionary<string, object>? Context
);
