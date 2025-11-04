using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Represents a provider profile that defines which providers to use for each stage
/// </summary>
public record ProviderProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProfileTier Tier { get; init; } = ProfileTier.FreeOnly;
    public Dictionary<string, string> Stages { get; init; } = new();
    public List<string> RequiredApiKeys { get; init; } = new();
    public string UsageNotes { get; init; } = string.Empty;
    public DateTime? LastValidatedAt { get; init; }

    public static ProviderProfile FreeOnly => new()
    {
        Id = "free-only",
        Name = "Free-Only",
        Description = "Uses only free and offline providers. No API keys required. Good for testing and offline use.",
        Tier = ProfileTier.FreeOnly,
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "Free",
            ["TTS"] = "Windows",
            ["Visuals"] = "Stock",
            ["Upload"] = "Off"
        },
        RequiredApiKeys = new List<string>(),
        UsageNotes = "Ideal for development, testing, and offline use. Quality is acceptable for internal videos."
    };

    public static ProviderProfile BalancedMix => new()
    {
        Id = "balanced-mix",
        Name = "Balanced Mix",
        Description = "Combines free and premium providers for good quality at reasonable cost. Requires some API keys.",
        Tier = ProfileTier.BalancedMix,
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "ProIfAvailable",
            ["TTS"] = "Windows",
            ["Visuals"] = "StockOrLocal",
            ["Upload"] = "Ask"
        },
        RequiredApiKeys = new List<string> { "openai" },
        UsageNotes = "Best balance of quality and cost. Uses paid services where they matter most."
    };

    public static ProviderProfile ProMax => new()
    {
        Id = "pro-max",
        Name = "Pro-Max",
        Description = "Premium providers for highest quality. Requires multiple paid API keys. Best for production.",
        Tier = ProfileTier.ProMax,
        Stages = new Dictionary<string, string>
        {
            ["Script"] = "Pro",
            ["TTS"] = "Pro",
            ["Visuals"] = "Pro",
            ["Upload"] = "Ask"
        },
        RequiredApiKeys = new List<string> { "openai", "elevenlabs", "stabilityai" },
        UsageNotes = "Maximum quality for production videos. Higher API costs but best results."
    };
}

/// <summary>
/// Profile tier indicating cost vs quality trade-off
/// </summary>
public enum ProfileTier
{
    FreeOnly,
    BalancedMix,
    ProMax
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
