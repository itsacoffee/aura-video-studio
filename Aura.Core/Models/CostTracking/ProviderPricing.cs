using System;
using System.Collections.Generic;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Pricing information for a provider
/// </summary>
public record ProviderPricing
{
    /// <summary>
    /// Provider name
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Provider type (LLM, TTS, Image, etc.)
    /// </summary>
    public required ProviderType ProviderType { get; init; }
    
    /// <summary>
    /// Whether this provider is free
    /// </summary>
    public bool IsFree { get; init; }
    
    /// <summary>
    /// Cost per 1K tokens (for LLM providers)
    /// </summary>
    public decimal? CostPer1KTokens { get; init; }
    
    /// <summary>
    /// Cost per 1K input tokens (for LLM providers with different input/output rates)
    /// </summary>
    public decimal? CostPer1KInputTokens { get; init; }
    
    /// <summary>
    /// Cost per 1K output tokens (for LLM providers with different input/output rates)
    /// </summary>
    public decimal? CostPer1KOutputTokens { get; init; }
    
    /// <summary>
    /// Cost per character (for TTS providers)
    /// </summary>
    public decimal? CostPerCharacter { get; init; }
    
    /// <summary>
    /// Cost per 1K characters (for TTS providers)
    /// </summary>
    public decimal? CostPer1KCharacters { get; init; }
    
    /// <summary>
    /// Cost per image (for image generation providers)
    /// </summary>
    public decimal? CostPerImage { get; init; }
    
    /// <summary>
    /// Cost per second of compute time (for video/image providers)
    /// </summary>
    public decimal? CostPerComputeSecond { get; init; }
    
    /// <summary>
    /// Whether pricing is manually overridden
    /// </summary>
    public bool IsManualOverride { get; init; }
    
    /// <summary>
    /// Last time pricing was updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Currency for pricing
    /// </summary>
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Additional notes about pricing (e.g., "Enterprise plan", "Volume discount")
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Type of provider for pricing purposes
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// Large Language Model provider
    /// </summary>
    LLM,
    
    /// <summary>
    /// Text-to-Speech provider
    /// </summary>
    TTS,
    
    /// <summary>
    /// Image generation provider
    /// </summary>
    Image,
    
    /// <summary>
    /// Video processing provider
    /// </summary>
    Video,
    
    /// <summary>
    /// Stock media provider
    /// </summary>
    Stock
}
