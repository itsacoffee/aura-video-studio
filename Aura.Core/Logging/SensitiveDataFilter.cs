using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace Aura.Core.Logging;

/// <summary>
/// Filters sensitive data from log events to prevent PII/credentials leakage
/// </summary>
public partial class SensitiveDataFilter : ILogEventFilter
{
    private static readonly string[] SensitiveKeys = 
    {
        "password", "pwd", "secret", "token", "apikey", "api_key", "authorization",
        "auth", "credential", "credit_card", "creditcard", "ssn", "social_security",
        "api-key", "access_token", "refresh_token", "bearer", "key"
    };

    private static readonly Regex EmailRegex = EmailPattern();
    private static readonly Regex CreditCardRegex = CreditCardPattern();
    private static readonly Regex SsnRegex = SsnPattern();
    private static readonly Regex IpAddressRegex = IpAddressPattern();
    private static readonly Regex JwtRegex = JwtPattern();

    public bool IsEnabled(LogEvent logEvent)
    {
        // Filter the log event properties
        FilterProperties(logEvent);
        return true; // Always return true to allow the event through after filtering
    }

    private static void FilterProperties(LogEvent logEvent)
    {
        var propertiesToRemove = new List<string>();
        var propertiesToAdd = new Dictionary<string, LogEventPropertyValue>();

        foreach (var property in logEvent.Properties)
        {
            var propertyName = property.Key;
            var propertyValue = property.Value;

            // Check if property name contains sensitive keywords
            if (IsSensitiveProperty(propertyName))
            {
                propertiesToRemove.Add(propertyName);
                propertiesToAdd[propertyName] = new ScalarValue("***REDACTED***");
                continue;
            }

            // Check if property value contains sensitive data
            if (propertyValue is ScalarValue { Value: string stringValue })
            {
                var filtered = FilterSensitiveData(stringValue);
                if (filtered != stringValue)
                {
                    propertiesToRemove.Add(propertyName);
                    propertiesToAdd[propertyName] = new ScalarValue(filtered);
                }
            }
        }

        // Remove and add filtered properties
        foreach (var key in propertiesToRemove)
        {
            logEvent.RemovePropertyIfPresent(key);
        }

        foreach (var kvp in propertiesToAdd)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty(kvp.Key, kvp.Value));
        }
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        var lowerName = propertyName.ToLowerInvariant();
        return SensitiveKeys.Any(sensitive => lowerName.Contains(sensitive));
    }

    private static string FilterSensitiveData(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Filter email addresses (partial masking)
        input = EmailRegex.Replace(input, match =>
        {
            var email = match.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex > 2)
            {
                var username = email[..atIndex];
                var domain = email[atIndex..];
                return $"{username[0]}***{username[^1]}{domain}";
            }
            return "***@***";
        });

        // Filter credit card numbers
        input = CreditCardRegex.Replace(input, "****-****-****-****");

        // Filter SSN
        input = SsnRegex.Replace(input, "***-**-****");

        // Filter JWT tokens
        input = JwtRegex.Replace(input, "***JWT_TOKEN***");

        // Optionally filter IP addresses (uncomment if needed)
        // input = IpAddressRegex.Replace(input, "***.***.***.***");

        return input;
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardPattern();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled)]
    private static partial Regex IpAddressPattern();

    [GeneratedRegex(@"eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*", RegexOptions.Compiled)]
    private static partial Regex JwtPattern();
}

/// <summary>
/// Provides methods to safely sanitize log messages
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes a dictionary by redacting sensitive keys
    /// </summary>
    public static Dictionary<string, object> SanitizeDictionary(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var kvp in input)
        {
            var key = kvp.Key;
            var lowerKey = key.ToLowerInvariant();
            
            if (lowerKey.Contains("password") || 
                lowerKey.Contains("secret") || 
                lowerKey.Contains("token") ||
                lowerKey.Contains("apikey") ||
                lowerKey.Contains("credential"))
            {
                result[key] = "***REDACTED***";
            }
            else
            {
                result[key] = kvp.Value;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Masks the middle portion of a string, leaving only first and last characters visible
    /// </summary>
    public static string MaskString(string input, int visibleChars = 2)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= visibleChars * 2)
            return "***";

        return $"{input[..visibleChars]}***{input[^visibleChars..]}";
    }
}
