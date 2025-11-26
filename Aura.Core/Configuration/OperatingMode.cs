using System;

namespace Aura.Core.Configuration;

/// <summary>
/// Defines the operating mode of the application - online (cloud+local) or offline (local only)
/// </summary>
public enum OperatingMode
{
    /// <summary>
    /// Online mode - all configured providers available (cloud + local)
    /// Prefers cloud for quality, falls back to local
    /// </summary>
    Online,

    /// <summary>
    /// Offline mode - only local providers available
    /// LLM: Ollama/RuleBased only
    /// TTS: Windows SAPI/Piper/Mimic3 only
    /// Images: Placeholder colors only
    /// </summary>
    Offline
}

/// <summary>
/// Helper class for operating mode related functionality
/// </summary>
public static class OperatingModeHelper
{
    /// <summary>
    /// LLM providers allowed in offline mode
    /// </summary>
    public static readonly string[] OfflineLlmProviders = { "Ollama", "RuleBased" };

    /// <summary>
    /// TTS providers allowed in offline mode
    /// </summary>
    public static readonly string[] OfflineTtsProviders = { "Windows", "WindowsSAPI", "Piper", "Mimic3" };

    /// <summary>
    /// Image providers allowed in offline mode
    /// </summary>
    public static readonly string[] OfflineImageProviders = { "Placeholder" };

    /// <summary>
    /// Checks if a provider is allowed in offline mode
    /// </summary>
    /// <param name="providerName">The provider name to check</param>
    /// <param name="providerType">The type of provider (llm, tts, images)</param>
    /// <returns>True if the provider is allowed in offline mode</returns>
    public static bool IsOfflineProvider(string providerName, string providerType)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        var normalizedName = providerName.Trim();

        return providerType?.ToLowerInvariant() switch
        {
            "llm" or "script" => Array.Exists(OfflineLlmProviders, p =>
                string.Equals(p, normalizedName, StringComparison.OrdinalIgnoreCase)),

            "tts" or "voice" => Array.Exists(OfflineTtsProviders, p =>
                string.Equals(p, normalizedName, StringComparison.OrdinalIgnoreCase)),

            "images" or "visuals" => Array.Exists(OfflineImageProviders, p =>
                string.Equals(p, normalizedName, StringComparison.OrdinalIgnoreCase)),

            _ => false
        };
    }

    /// <summary>
    /// Filters a list of provider names to only those allowed in offline mode
    /// </summary>
    /// <param name="providers">List of provider names</param>
    /// <param name="providerType">The type of providers (llm, tts, images)</param>
    /// <returns>Filtered list of offline-compatible providers</returns>
    public static string[] FilterOfflineProviders(string[] providers, string providerType)
    {
        if (providers == null || providers.Length == 0)
        {
            return Array.Empty<string>();
        }

        return Array.FindAll(providers, p => IsOfflineProvider(p, providerType));
    }

    /// <summary>
    /// Gets the default provider for a given type in offline mode
    /// </summary>
    /// <param name="providerType">The type of provider (llm, tts, images)</param>
    /// <returns>The default offline provider name</returns>
    public static string GetDefaultOfflineProvider(string providerType)
    {
        return providerType?.ToLowerInvariant() switch
        {
            "llm" or "script" => "Ollama",
            "tts" or "voice" => "Windows",
            "images" or "visuals" => "Placeholder",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Determines the appropriate operating mode from settings
    /// </summary>
    /// <param name="offlineModeSetting">The offline mode setting value</param>
    /// <returns>The operating mode</returns>
    public static OperatingMode FromSettings(bool offlineModeSetting)
    {
        return offlineModeSetting ? OperatingMode.Offline : OperatingMode.Online;
    }
}
