using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Configuration for LLM pricing loaded from JSON file.
/// Supports dynamic updates without code changes.
/// </summary>
public class LlmPricingConfiguration
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("providers")]
    public Dictionary<string, ProviderPricing> Providers { get; set; } = new();

    [JsonPropertyName("fallbackModel")]
    public ModelPricing FallbackModel { get; set; } = new();

    [JsonPropertyName("updateInstructions")]
    public UpdateInstructions? UpdateInfo { get; set; }

    /// <summary>
    /// Load pricing configuration from JSON file
    /// </summary>
    public static LlmPricingConfiguration LoadFromFile(string filePath, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger?.LogWarning("Pricing configuration file not found at {Path}, using defaults", filePath);
                return GetDefaultConfiguration();
            }

            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<LlmPricingConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (config == null)
            {
                logger?.LogWarning("Failed to deserialize pricing configuration, using defaults");
                return GetDefaultConfiguration();
            }

            logger?.LogInformation(
                "Loaded LLM pricing configuration v{Version} (updated: {LastUpdated}) with {ProviderCount} providers",
                config.Version, config.LastUpdated, config.Providers.Count);

            return config;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error loading pricing configuration from {Path}, using defaults", filePath);
            return GetDefaultConfiguration();
        }
    }

    /// <summary>
    /// Load from embedded resource or default path
    /// </summary>
    public static LlmPricingConfiguration LoadDefault(ILogger? logger = null)
    {
        // Try multiple common paths
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Configuration", "llm-pricing.json"),
            Path.Combine(AppContext.BaseDirectory, "llm-pricing.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "llm-pricing.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "llm-pricing.json"),
            "llm-pricing.json"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                logger?.LogDebug("Found pricing configuration at {Path}", path);
                return LoadFromFile(path, logger);
            }
        }

        logger?.LogWarning("No pricing configuration file found in any standard location, using defaults");
        return GetDefaultConfiguration();
    }

    /// <summary>
    /// Get pricing for a specific model
    /// </summary>
    public ModelPricing? GetModelPricing(string modelName)
    {
        var normalizedName = NormalizeModelName(modelName);

        // Search all providers
        foreach (var provider in Providers.Values)
        {
            if (provider.Models.TryGetValue(normalizedName, out var pricing))
            {
                return pricing;
            }

            // Also check for partial matches
            var partialMatch = provider.Models
                .Where(kv => normalizedName.Contains(kv.Key, StringComparison.OrdinalIgnoreCase) ||
                            kv.Key.Contains(normalizedName, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Value)
                .FirstOrDefault();

            if (partialMatch != null)
            {
                return partialMatch;
            }
        }

        return null;
    }

    /// <summary>
    /// Get all available models across all providers
    /// </summary>
    public List<string> GetAllModelNames()
    {
        return Providers.Values
            .SelectMany(p => p.Models.Keys)
            .OrderBy(m => m)
            .ToList();
    }

    /// <summary>
    /// Normalize model name for consistent lookup
    /// </summary>
    private static string NormalizeModelName(string modelName)
    {
        return modelName.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Get default configuration if file loading fails
    /// </summary>
    private static LlmPricingConfiguration GetDefaultConfiguration()
    {
        return new LlmPricingConfiguration
        {
            Version = "2024.12",
            LastUpdated = "2024-12-01",
            Description = "Default fallback pricing",
            FallbackModel = new ModelPricing
            {
                InputPrice = 0.150m,
                OutputPrice = 0.600m,
                Description = "Default fallback (gpt-4o-mini equivalent)"
            },
            Providers = new Dictionary<string, ProviderPricing>
            {
                ["openai"] = new ProviderPricing
                {
                    Name = "OpenAI",
                    Models = new Dictionary<string, ModelPricing>
                    {
                        ["gpt-4o-mini"] = new ModelPricing
                        {
                            InputPrice = 0.150m,
                            OutputPrice = 0.600m,
                            ContextWindow = 128000,
                            Description = "Fast and affordable"
                        }
                    }
                }
            }
        };
    }
}

/// <summary>
/// Provider-specific pricing information
/// </summary>
public class ProviderPricing
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("models")]
    public Dictionary<string, ModelPricing> Models { get; set; } = new();
}

/// <summary>
/// Model-specific pricing information
/// </summary>
public class ModelPricing
{
    [JsonPropertyName("inputPrice")]
    public decimal InputPrice { get; set; }

    [JsonPropertyName("outputPrice")]
    public decimal OutputPrice { get; set; }

    [JsonPropertyName("contextWindow")]
    public int? ContextWindow { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Update instructions for pricing configuration
/// </summary>
public class UpdateInstructions
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("urls")]
    public Dictionary<string, string> Urls { get; set; } = new();

    [JsonPropertyName("updateFrequency")]
    public string UpdateFrequency { get; set; } = string.Empty;
}
