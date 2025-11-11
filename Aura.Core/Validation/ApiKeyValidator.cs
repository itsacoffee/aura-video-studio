using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aura.Core.Errors;

namespace Aura.Core.Validation;

/// <summary>
/// Validates API keys for various providers
/// </summary>
public static class ApiKeyValidator
{
    private static readonly Dictionary<string, ApiKeyFormat> _formats = new()
    {
        ["OPENAI_KEY"] = new("sk-[A-Za-z0-9_-]{20,}", "OpenAI API keys start with 'sk-'"),
        ["ANTHROPIC_KEY"] = new("sk-ant-[A-Za-z0-9_-]{20,}", "Anthropic API keys start with 'sk-ant-'"),
        ["ELEVENLABS_KEY"] = new("[A-Za-z0-9]{32}", "ElevenLabs API keys are 32 characters"),
        ["STABILITY_KEY"] = new("sk-[A-Za-z0-9_-]{20,}", "Stability AI keys start with 'sk-'"),
        ["GEMINI_KEY"] = new("AIza[A-Za-z0-9_-]{35}", "Google Gemini keys start with 'AIza'"),
        ["PLAYHT_KEY"] = new("[A-Za-z0-9]{40,}", "PlayHT API keys are at least 40 characters"),
        ["REPLICATE_KEY"] = new("r8_[A-Za-z0-9]{40}", "Replicate keys start with 'r8_'"),
    };

    /// <summary>
    /// Validates an API key format
    /// </summary>
    public static Errors.ValidationResult ValidateKey(string keyName, string? keyValue)
    {
        if (string.IsNullOrWhiteSpace(keyValue))
        {
            return Errors.ValidationResult.Failure(
                $"API key '{keyName}' is required but not provided",
                Errors.ValidationErrorCode.MissingApiKey,
                new[] { $"Add {keyName} in Settings → Providers" }
            );
        }

        // Check if we have format rules for this key
        if (_formats.TryGetValue(keyName, out var format))
        {
            if (!Regex.IsMatch(keyValue, $"^{format.Pattern}$"))
            {
                return Errors.ValidationResult.Failure(
                    $"API key '{keyName}' has invalid format",
                    Errors.ValidationErrorCode.InvalidApiKeyFormat,
                    new[]
                    {
                        format.Hint,
                        "Verify you copied the entire key",
                        "Check for extra spaces or line breaks"
                    }
                );
            }
        }

        // General validation checks
        if (keyValue.Length < 10)
        {
            return Errors.ValidationResult.Failure(
                $"API key '{keyName}' appears too short",
                Errors.ValidationErrorCode.InvalidApiKeyFormat,
                new[] { "API keys are typically at least 10 characters long" }
            );
        }

        // Check for common mistakes
        if (keyValue.Contains(" ") || keyValue.Contains("\n") || keyValue.Contains("\t"))
        {
            return Errors.ValidationResult.Failure(
                $"API key '{keyName}' contains whitespace characters",
                Errors.ValidationErrorCode.InvalidApiKeyFormat,
                new[] { "Remove any spaces, tabs, or line breaks from the key" }
            );
        }

        if (keyValue.StartsWith("Bearer ") || keyValue.StartsWith("bearer "))
        {
            return Errors.ValidationResult.Failure(
                $"API key '{keyName}' should not include 'Bearer' prefix",
                Errors.ValidationErrorCode.InvalidApiKeyFormat,
                new[] { "Remove the 'Bearer ' prefix from the key" }
            );
        }

        return Errors.ValidationResult.Success();
    }

    /// <summary>
    /// Validates multiple API keys at once
    /// </summary>
    public static Dictionary<string, Errors.ValidationResult> ValidateKeys(Dictionary<string, string?> keys)
    {
        var results = new Dictionary<string, Errors.ValidationResult>();

        foreach (var (keyName, keyValue) in keys)
        {
            results[keyName] = ValidateKey(keyName, keyValue);
        }

        return results;
    }

    /// <summary>
    /// Checks if a key is required for a specific provider
    /// </summary>
    public static bool IsKeyRequired(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => true,
            "Anthropic Claude" => true,
            "Google Gemini" => true,
            "ElevenLabs" => true,
            "PlayHT" => true,
            "Stability AI" => true,
            "Replicate" => true,
            "Ollama (Local)" => false,
            "Piper TTS" => false,
            "Mimic3" => false,
            "Windows SAPI" => false,
            "PlaceholderImages" => false,
            "RuleBased" => false,
            _ => false
        };
    }

    /// <summary>
    /// Gets the key name for a provider
    /// </summary>
    public static string? GetKeyNameForProvider(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => "OPENAI_KEY",
            "Anthropic Claude" => "ANTHROPIC_KEY",
            "Google Gemini" => "GEMINI_KEY",
            "ElevenLabs" => "ELEVENLABS_KEY",
            "PlayHT" => "PLAYHT_KEY",
            "Stability AI" => "STABILITY_KEY",
            "Replicate" => "REPLICATE_KEY",
            _ => null
        };
    }

    /// <summary>
    /// Masks an API key for safe display (shows first 4 and last 4 characters)
    /// </summary>
    public static string MaskKey(string keyValue)
    {
        if (string.IsNullOrEmpty(keyValue))
        {
            return string.Empty;
        }

        if (keyValue.Length <= 8)
        {
            return new string('*', keyValue.Length);
        }

        var prefix = keyValue.Substring(0, 4);
        var suffix = keyValue.Substring(keyValue.Length - 4);
        var masked = new string('*', Math.Max(8, keyValue.Length - 8));

        return $"{prefix}{masked}{suffix}";
    }
}

/// <summary>
/// Format information for an API key
/// </summary>
public record ApiKeyFormat(string Pattern, string Hint);

/// <summary>
/// Validation error codes
/// </summary>
public static class ValidationErrorCode
{
    public const string MissingApiKey = "MISSING_API_KEY";
    public const string InvalidApiKeyFormat = "INVALID_API_KEY_FORMAT";
    public const string ExpiredApiKey = "EXPIRED_API_KEY";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
}
