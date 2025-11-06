using System;
using System.IO;
using System.Text.Json;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry.Costing;

/// <summary>
/// Service for estimating costs using versioned pricing data
/// Handles cache hits, retries, and partial scenes appropriately
/// </summary>
public class CostEstimatorService
{
    private readonly ILogger<CostEstimatorService> _logger;
    private readonly PricingVersionTable _pricingTable;
    private readonly string _pricingDataPath;
    
    /// <summary>
    /// Error margin for cost estimates (percentage)
    /// Used to validate that estimates are within acceptable range
    /// </summary>
    public decimal ErrorMarginPercent { get; set; } = 10m;
    
    public CostEstimatorService(
        ILogger<CostEstimatorService> logger,
        string? dataDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pricingTable = new PricingVersionTable();
        
        _pricingDataPath = Path.Combine(
            dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aura"),
            "pricing-versions.json");
        
        InitializeDefaultPricing();
        LoadPricingData();
    }
    
    /// <summary>
    /// Estimate cost for a telemetry record using appropriate pricing version
    /// Accounts for cache hits, retries, and partial operations
    /// </summary>
    public CostEstimate EstimateCost(RunTelemetryRecord record)
    {
        if (record.CostEstimate.HasValue && !string.IsNullOrEmpty(record.PricingVersion))
        {
            // Already has cost estimate with pricing version, return it
            return new CostEstimate
            {
                Amount = record.CostEstimate.Value,
                Currency = record.Currency,
                PricingVersion = record.PricingVersion,
                Confidence = CostConfidence.Exact,
                Notes = "Cost already calculated in telemetry record"
            };
        }
        
        if (string.IsNullOrEmpty(record.Provider))
        {
            return new CostEstimate
            {
                Amount = 0m,
                Currency = record.Currency,
                Confidence = CostConfidence.None,
                Notes = "No provider specified"
            };
        }
        
        var pricing = _pricingTable.GetVersionFor(record.Provider, record.StartedAt);
        
        if (pricing == null)
        {
            _logger.LogWarning(
                "No pricing version found for provider {Provider} at {Timestamp}",
                record.Provider, record.StartedAt);
                
            return new CostEstimate
            {
                Amount = 0m,
                Currency = record.Currency,
                Confidence = CostConfidence.None,
                Notes = $"No pricing data available for {record.Provider}"
            };
        }
        
        if (pricing.IsFree)
        {
            return new CostEstimate
            {
                Amount = 0m,
                Currency = pricing.Currency,
                PricingVersion = pricing.Version,
                Confidence = CostConfidence.Exact,
                Notes = $"{record.Provider} is a free provider"
            };
        }
        
        decimal baseCost = CalculateBaseCost(record, pricing);
        decimal adjustedCost = ApplyCostAdjustments(baseCost, record);
        
        return new CostEstimate
        {
            Amount = adjustedCost,
            Currency = pricing.Currency,
            PricingVersion = pricing.Version,
            Confidence = DetermineConfidence(record),
            Notes = BuildEstimateNotes(record, baseCost, adjustedCost)
        };
    }
    
    /// <summary>
    /// Calculate base cost before adjustments for cache/retries
    /// </summary>
    private decimal CalculateBaseCost(RunTelemetryRecord record, PricingVersion pricing)
    {
        decimal cost = 0m;
        
        // LLM token-based cost
        if (record.TokensIn.HasValue && record.TokensOut.HasValue)
        {
            if (pricing.CostPer1KInputTokens.HasValue && pricing.CostPer1KOutputTokens.HasValue)
            {
                var inputCost = (record.TokensIn.Value / 1000m) * pricing.CostPer1KInputTokens.Value;
                var outputCost = (record.TokensOut.Value / 1000m) * pricing.CostPer1KOutputTokens.Value;
                cost = inputCost + outputCost;
            }
        }
        
        // TTS character-based cost (estimate from record metadata if available)
        if (record.Metadata != null && record.Metadata.TryGetValue("characters", out var charObj))
        {
            if (charObj is int chars && pricing.CostPer1KCharacters.HasValue)
            {
                cost = (chars / 1000m) * pricing.CostPer1KCharacters.Value;
            }
        }
        
        // Image generation cost
        if (record.Stage == RunStage.Visuals && pricing.CostPerImage.HasValue)
        {
            cost = pricing.CostPerImage.Value;
        }
        
        return cost;
    }
    
    /// <summary>
    /// Apply adjustments for cache hits, retries, and partial operations
    /// </summary>
    private decimal ApplyCostAdjustments(decimal baseCost, RunTelemetryRecord record)
    {
        decimal adjustedCost = baseCost;
        
        // Cache hit: typically no cost or heavily reduced cost
        if (record.CacheHit == true)
        {
            var pricing = _pricingTable.GetVersionFor(record.Provider!, record.StartedAt);
            
            // If provider has cached token pricing, use that
            if (pricing?.CostPer1KCachedInputTokens.HasValue == true && record.TokensIn.HasValue)
            {
                adjustedCost = (record.TokensIn.Value / 1000m) * pricing.CostPer1KCachedInputTokens.Value;
            }
            else
            {
                // Otherwise, cache hits are typically free
                adjustedCost = 0m;
            }
        }
        
        // Retries: Cost is already included in the final attempt's token counts
        // No additional adjustment needed - the retry cost is implicit in the tokens used
        
        // Partial scenes: Cost is proportional to work actually done
        // This is already reflected in token counts or operation metadata
        
        return adjustedCost;
    }
    
    /// <summary>
    /// Determine confidence level for the estimate
    /// </summary>
    private CostConfidence DetermineConfidence(RunTelemetryRecord record)
    {
        // Cache hits are exact (0 or known reduced cost)
        if (record.CacheHit == true)
            return CostConfidence.Exact;
        
        // Operations with token counts are high confidence
        if (record.TokensIn.HasValue && record.TokensOut.HasValue)
            return CostConfidence.High;
        
        // Operations with metadata are medium confidence
        if (record.Metadata != null && record.Metadata.Count > 0)
            return CostConfidence.Medium;
        
        // Others are low confidence estimates
        return CostConfidence.Low;
    }
    
    /// <summary>
    /// Build explanatory notes for the estimate
    /// </summary>
    private string BuildEstimateNotes(RunTelemetryRecord record, decimal baseCost, decimal adjustedCost)
    {
        var notes = "";
        
        if (record.CacheHit == true)
        {
            notes += "Cache hit; ";
            if (adjustedCost == 0m)
                notes += "no cost. ";
            else
                notes += $"reduced from {CurrencyFormatter.Format(baseCost)} to {CurrencyFormatter.Format(adjustedCost)}. ";
        }
        
        if (record.Retries > 0)
        {
            notes += $"{record.Retries} retry(ies); cost includes all attempts. ";
        }
        
        if (record.SceneIndex.HasValue)
        {
            notes += $"Scene {record.SceneIndex.Value}. ";
        }
        
        return notes.TrimEnd();
    }
    
    /// <summary>
    /// Initialize default pricing for common providers
    /// </summary>
    private void InitializeDefaultPricing()
    {
        var now = DateTime.UtcNow;
        
        // OpenAI GPT-4 pricing (as of 2024)
        _pricingTable.AddVersion(new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "OpenAI",
            Currency = "USD",
            CostPer1KInputTokens = 0.03m,
            CostPer1KOutputTokens = 0.06m,
            CostPer1KCachedInputTokens = 0.015m, // 50% discount for cached
            Notes = "GPT-4 Turbo pricing"
        });
        
        // Anthropic Claude pricing
        _pricingTable.AddVersion(new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "Anthropic",
            Currency = "USD",
            CostPer1KInputTokens = 0.025m,
            CostPer1KOutputTokens = 0.075m,
            Notes = "Claude 3 Sonnet pricing"
        });
        
        // Google Gemini pricing
        _pricingTable.AddVersion(new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "Gemini",
            Currency = "USD",
            CostPer1KInputTokens = 0.00025m,
            CostPer1KOutputTokens = 0.0005m,
            Notes = "Gemini Pro pricing"
        });
        
        // ElevenLabs TTS pricing
        _pricingTable.AddVersion(new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "ElevenLabs",
            Currency = "USD",
            CostPer1KCharacters = 0.30m,
            Notes = "Creator plan pricing"
        });
        
        // PlayHT TTS pricing
        _pricingTable.AddVersion(new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "PlayHT",
            Currency = "USD",
            CostPer1KCharacters = 0.50m,
            Notes = "Standard pricing"
        });
        
        // Free providers
        var freeProviders = new[] { "Ollama", "Piper", "Mimic3", "RuleBased" };
        foreach (var provider in freeProviders)
        {
            _pricingTable.AddVersion(new PricingVersion
            {
                Version = "2024.1",
                ValidFrom = new DateTime(2024, 1, 1),
                ProviderName = provider,
                Currency = "USD",
                IsFree = true,
                Notes = "Free local/offline provider"
            });
        }
    }
    
    /// <summary>
    /// Load pricing data from persistent storage
    /// </summary>
    private void LoadPricingData()
    {
        if (!File.Exists(_pricingDataPath))
            return;
        
        try
        {
            var json = File.ReadAllText(_pricingDataPath);
            var versions = JsonSerializer.Deserialize<PricingVersion[]>(json);
            
            if (versions != null)
            {
                foreach (var version in versions)
                {
                    _pricingTable.AddVersion(version);
                }
                
                _logger.LogInformation(
                    "Loaded {Count} pricing versions from {Path}",
                    versions.Length, _pricingDataPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pricing data from {Path}", _pricingDataPath);
        }
    }
    
    /// <summary>
    /// Save current pricing data to persistent storage
    /// </summary>
    public void SavePricingData()
    {
        try
        {
            var directory = Path.GetDirectoryName(_pricingDataPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var allVersions = _pricingTable.GetAllCurrentVersions();
            var json = JsonSerializer.Serialize(allVersions, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_pricingDataPath, json);
            
            _logger.LogInformation(
                "Saved {Count} pricing versions to {Path}",
                allVersions.Count, _pricingDataPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pricing data to {Path}", _pricingDataPath);
        }
    }
    
    /// <summary>
    /// Get the pricing version table for direct access
    /// </summary>
    public PricingVersionTable GetPricingTable() => _pricingTable;
}

/// <summary>
/// Result of a cost estimation
/// </summary>
public record CostEstimate
{
    /// <summary>
    /// Estimated cost amount
    /// </summary>
    public required decimal Amount { get; init; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Pricing version used for estimation
    /// </summary>
    public string? PricingVersion { get; init; }
    
    /// <summary>
    /// Confidence level of the estimate
    /// </summary>
    public required CostConfidence Confidence { get; init; }
    
    /// <summary>
    /// Explanatory notes about the estimate
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Confidence level for cost estimates
/// </summary>
public enum CostConfidence
{
    /// <summary>
    /// No cost data available
    /// </summary>
    None,
    
    /// <summary>
    /// Low confidence estimate
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium confidence estimate
    /// </summary>
    Medium,
    
    /// <summary>
    /// High confidence estimate
    /// </summary>
    High,
    
    /// <summary>
    /// Exact/actual cost
    /// </summary>
    Exact
}
