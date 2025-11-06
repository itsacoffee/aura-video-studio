using System;
using System.Collections.Generic;

namespace Aura.Core.Telemetry.Costing;

/// <summary>
/// Versioned pricing table entry with validity window
/// Allows tracking pricing changes over time and ensures cost calculations
/// use the correct pricing data for the time period when work was performed
/// </summary>
public record PricingVersion
{
    /// <summary>
    /// Unique version identifier (e.g., "2024.1", "2024.2")
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// When this pricing version becomes valid
    /// </summary>
    public required DateTime ValidFrom { get; init; }
    
    /// <summary>
    /// When this pricing version expires (null if currently active)
    /// </summary>
    public DateTime? ValidUntil { get; init; }
    
    /// <summary>
    /// Provider name this pricing applies to
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Currency code (ISO 4217, e.g., "USD", "EUR", "GBP")
    /// </summary>
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Cost per 1K input tokens (for LLM providers)
    /// </summary>
    public decimal? CostPer1KInputTokens { get; init; }
    
    /// <summary>
    /// Cost per 1K output tokens (for LLM providers)
    /// </summary>
    public decimal? CostPer1KOutputTokens { get; init; }
    
    /// <summary>
    /// Cost per 1K cached input tokens (if provider supports caching)
    /// </summary>
    public decimal? CostPer1KCachedInputTokens { get; init; }
    
    /// <summary>
    /// Cost per 1K characters (for TTS providers)
    /// </summary>
    public decimal? CostPer1KCharacters { get; init; }
    
    /// <summary>
    /// Cost per image generation
    /// </summary>
    public decimal? CostPerImage { get; init; }
    
    /// <summary>
    /// Cost per second of compute time
    /// </summary>
    public decimal? CostPerComputeSecond { get; init; }
    
    /// <summary>
    /// Whether this provider is free (no cost)
    /// </summary>
    public bool IsFree { get; init; }
    
    /// <summary>
    /// Additional notes about this pricing version (e.g., "Q1 2024 pricing update")
    /// </summary>
    public string? Notes { get; init; }
    
    /// <summary>
    /// When this pricing record was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Check if this pricing version is valid for a given timestamp
    /// </summary>
    public bool IsValidFor(DateTime timestamp)
    {
        if (timestamp < ValidFrom)
            return false;
            
        if (ValidUntil.HasValue && timestamp >= ValidUntil.Value)
            return false;
            
        return true;
    }
}

/// <summary>
/// Collection of pricing versions with query capabilities
/// </summary>
public class PricingVersionTable
{
    private readonly List<PricingVersion> _versions = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Add a new pricing version to the table
    /// </summary>
    public void AddVersion(PricingVersion version)
    {
        lock (_lock)
        {
            _versions.Add(version);
            
            // Sort by ValidFrom descending (newest first)
            _versions.Sort((a, b) => b.ValidFrom.CompareTo(a.ValidFrom));
        }
    }
    
    /// <summary>
    /// Get the pricing version valid for a specific timestamp and provider
    /// </summary>
    public PricingVersion? GetVersionFor(string providerName, DateTime timestamp)
    {
        lock (_lock)
        {
            return _versions.Find(v => 
                v.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) &&
                v.IsValidFor(timestamp));
        }
    }
    
    /// <summary>
    /// Get the current (most recent) pricing version for a provider
    /// </summary>
    public PricingVersion? GetCurrentVersion(string providerName)
    {
        return GetVersionFor(providerName, DateTime.UtcNow);
    }
    
    /// <summary>
    /// Get all versions for a provider, ordered by ValidFrom descending
    /// </summary>
    public List<PricingVersion> GetAllVersions(string providerName)
    {
        lock (_lock)
        {
            return _versions
                .FindAll(v => v.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
    
    /// <summary>
    /// Invalidate a pricing version by setting its ValidUntil date
    /// </summary>
    public void InvalidateVersion(string providerName, string version, DateTime validUntil)
    {
        lock (_lock)
        {
            var existingVersion = _versions.Find(v => 
                v.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) &&
                v.Version == version);
                
            if (existingVersion != null)
            {
                var index = _versions.IndexOf(existingVersion);
                _versions[index] = existingVersion with { ValidUntil = validUntil };
            }
        }
    }
    
    /// <summary>
    /// Get all current pricing versions
    /// </summary>
    public List<PricingVersion> GetAllCurrentVersions()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var currentVersions = new List<PricingVersion>();
            var seenProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var version in _versions)
            {
                if (!seenProviders.Contains(version.ProviderName) && version.IsValidFor(now))
                {
                    currentVersions.Add(version);
                    seenProviders.Add(version.ProviderName);
                }
            }
            
            return currentVersions;
        }
    }
}
