using System;
using System.Text.RegularExpressions;

namespace Aura.Core.Services;

/// <summary>
/// Service for masking secrets in logs and diagnostic output
/// </summary>
public static class SecretMaskingService
{
    private static readonly Regex ApiKeyPattern = new(
        @"(sk-[a-zA-Z0-9]{20,}|sk-ant-[a-zA-Z0-9]{20,}|[a-zA-Z0-9]{32,})",
        RegexOptions.Compiled);

    private static readonly string[] SensitiveKeys = new[]
    {
        "apikey", "api_key", "api-key",
        "secret", "password", "pwd", "pass",
        "token", "auth", "authorization",
        "key", "credential"
    };

    /// <summary>
    /// Mask an API key for display (show first 8 and last 4 characters)
    /// </summary>
    public static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "[not set]";
        }

        if (apiKey.Length <= 12)
        {
            return "***";
        }

        var start = apiKey.Substring(0, 8);
        var end = apiKey.Substring(apiKey.Length - 4);
        return $"{start}...{end}";
    }

    /// <summary>
    /// Mask all API keys found in a string
    /// </summary>
    public static string MaskSecretsInText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return ApiKeyPattern.Replace(text, match =>
        {
            var key = match.Value;
            return MaskApiKey(key);
        });
    }

    /// <summary>
    /// Check if a key name indicates it contains sensitive data
    /// </summary>
    public static bool IsSensitiveKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            return false;
        }

        var lowerKey = keyName.ToLowerInvariant();
        return Array.Exists(SensitiveKeys, sk => lowerKey.Contains(sk));
    }

    /// <summary>
    /// Mask value if key name is sensitive
    /// </summary>
    public static string MaskIfSensitive(string keyName, string value)
    {
        if (IsSensitiveKey(keyName))
        {
            return MaskApiKey(value);
        }

        return value;
    }

    /// <summary>
    /// Create a masked copy of a dictionary for logging
    /// </summary>
    public static System.Collections.Generic.Dictionary<string, string> MaskDictionary(
        System.Collections.Generic.Dictionary<string, string> dict)
    {
        var masked = new System.Collections.Generic.Dictionary<string, string>();
        
        foreach (var kvp in dict)
        {
            masked[kvp.Key] = MaskIfSensitive(kvp.Key, kvp.Value);
        }

        return masked;
    }
}
