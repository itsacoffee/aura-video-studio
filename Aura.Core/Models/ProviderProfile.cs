using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Represents a provider profile that defines which providers to use for each stage
/// </summary>
public record ProviderProfile
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, string> Stages { get; init; } = new();

    public static ProviderProfile FreeOnly => new()
    {
        Name = "Free-Only",
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "Free",
            ["TTS"] = "Windows",
            ["Visuals"] = "Stock",
            ["Upload"] = "Off"
        }
    };

    public static ProviderProfile BalancedMix => new()
    {
        Name = "Balanced Mix",
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "ProIfAvailable",
            ["TTS"] = "Windows",
            ["Visuals"] = "StockOrLocal",
            ["Upload"] = "Ask"
        }
    };

    public static ProviderProfile ProMax => new()
    {
        Name = "Pro-Max",
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "Pro",
            ["TTS"] = "Pro",
            ["Visuals"] = "Pro",
            ["Upload"] = "Ask"
        }
    };
}

/// <summary>
/// Configuration for provider mixing and fallback behavior
/// </summary>
public record ProviderMixingConfig
{
    public string ActiveProfile { get; set; } = "Free-Only";
    public List<ProviderProfile> SavedProfiles { get; set; } = new()
    {
        ProviderProfile.FreeOnly,
        ProviderProfile.BalancedMix,
        ProviderProfile.ProMax
    };

    /// <summary>
    /// Whether to automatically fallback to free providers on failure
    /// </summary>
    public bool AutoFallback { get; set; } = true;

    /// <summary>
    /// Whether to log provider selection decisions
    /// </summary>
    public bool LogProviderSelection { get; set; } = true;
}

/// <summary>
/// Result of a provider selection decision
/// </summary>
public record ProviderSelection
{
    public string Stage { get; init; } = string.Empty;
    public string SelectedProvider { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool IsFallback { get; init; }
    public string? FallbackFrom { get; init; }
}
