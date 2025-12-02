using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Models.CostTracking;
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
    /// Estimate the total cost for a video generation based on providers and content
    /// </summary>
    /// <param name="estimatedScriptLength">Script length in characters</param>
    /// <param name="sceneCount">Number of scenes to generate</param>
    /// <param name="llmProvider">LLM provider name (e.g., "OpenAI", "Ollama")</param>
    /// <param name="llmModel">LLM model name (e.g., "gpt-4o-mini")</param>
    /// <param name="ttsProvider">TTS provider name (e.g., "ElevenLabs", "Piper")</param>
    /// <param name="imageProvider">Image provider name (optional)</param>
    /// <returns>Complete cost estimate with breakdown</returns>
    public GenerationCostEstimate EstimateGenerationCost(
        int estimatedScriptLength,
        int sceneCount,
        string llmProvider,
        string llmModel,
        string ttsProvider,
        string? imageProvider)
    {
        var breakdown = new List<CostBreakdownItem>();
        var confidence = CostEstimateConfidence.High;

        // Estimate tokens from script length (rough estimate: 4 chars per token)
        var estimatedInputTokens = Math.Max(100, estimatedScriptLength / 4);
        var estimatedOutputTokens = estimatedInputTokens * 2; // Output typically larger for script generation

        // Calculate LLM cost
        var llmCost = CalculateLlmCost(llmProvider, llmModel, estimatedInputTokens, estimatedOutputTokens, breakdown, ref confidence);

        // Calculate TTS cost (characters to be spoken)
        var ttsCost = CalculateTtsCost(ttsProvider, estimatedScriptLength, breakdown, ref confidence);

        // Calculate image cost
        var imageCost = CalculateImageCost(imageProvider, sceneCount, breakdown, ref confidence);

        var totalCost = llmCost + ttsCost + imageCost;
        var isFreeGeneration = totalCost == 0;

        return new GenerationCostEstimate
        {
            LlmCost = llmCost,
            TtsCost = ttsCost,
            ImageCost = imageCost,
            TotalCost = totalCost,
            Currency = "USD",
            Breakdown = breakdown,
            IsFreeGeneration = isFreeGeneration,
            Confidence = confidence
        };
    }

    private decimal CalculateLlmCost(
        string provider,
        string model,
        int inputTokens,
        int outputTokens,
        List<CostBreakdownItem> breakdown,
        ref CostEstimateConfidence confidence)
    {
        var normalizedProvider = provider.ToLowerInvariant().Trim();

        // Check for free/local providers
        if (IsFreeProvider(normalizedProvider))
        {
            breakdown.Add(new CostBreakdownItem
            {
                Name = "Script Generation",
                Provider = provider,
                Cost = 0,
                IsFree = true,
                Units = inputTokens + outputTokens,
                UnitType = "tokens"
            });
            return 0;
        }

        // Get model pricing
        var modelPricing = GetModelPricing(model);
        if (modelPricing == null)
        {
            // Use fallback pricing
            modelPricing = FallbackModel;
            confidence = CostEstimateConfidence.Medium;
        }

        // Prices in llm-pricing.json are per 1M tokens
        var inputCost = (inputTokens / 1_000_000m) * modelPricing.InputPrice;
        var outputCost = (outputTokens / 1_000_000m) * modelPricing.OutputPrice;
        var totalLlmCost = inputCost + outputCost;

        breakdown.Add(new CostBreakdownItem
        {
            Name = "Script Generation",
            Provider = $"{provider} ({model})",
            Cost = totalLlmCost,
            IsFree = false,
            Units = inputTokens + outputTokens,
            UnitType = "tokens"
        });

        return totalLlmCost;
    }

    private static decimal CalculateTtsCost(
        string provider,
        int characterCount,
        List<CostBreakdownItem> breakdown,
        ref CostEstimateConfidence confidence)
    {
        var normalizedProvider = provider.ToLowerInvariant().Trim();

        // TTS pricing (per 1K characters)
        var ttsPricing = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["elevenlabs"] = 0.30m,
            ["playht"] = 0.20m,
            ["azure"] = 0.016m,
            ["google"] = 0.016m,
            ["aws"] = 0.004m,
            ["amazon"] = 0.004m,
            // Free providers
            ["windows"] = 0,
            ["sapi"] = 0,
            ["piper"] = 0,
            ["mimic3"] = 0,
            ["null"] = 0
        };

        if (!ttsPricing.TryGetValue(normalizedProvider, out var costPer1KChars))
        {
            // Unknown provider - assume it's free (local) but lower confidence
            costPer1KChars = 0;
            confidence = CostEstimateConfidence.Medium;
        }

        var isFree = costPer1KChars == 0;
        var cost = isFree ? 0 : (characterCount / 1000m) * costPer1KChars;

        breakdown.Add(new CostBreakdownItem
        {
            Name = "Text-to-Speech",
            Provider = provider,
            Cost = cost,
            IsFree = isFree,
            Units = characterCount,
            UnitType = "characters"
        });

        return cost;
    }

    private static decimal CalculateImageCost(
        string? provider,
        int imageCount,
        List<CostBreakdownItem> breakdown,
        ref CostEstimateConfidence confidence)
    {
        if (string.IsNullOrEmpty(provider))
        {
            return 0;
        }

        var normalizedProvider = provider.ToLowerInvariant().Trim();

        // Image pricing (per image)
        var imagePricing = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["dalle"] = 0.02m,
            ["dall-e"] = 0.02m,
            ["dall-e-3"] = 0.04m,
            ["dalle-3"] = 0.04m,
            ["midjourney"] = 0.04m,
            ["stability"] = 0.01m,
            ["stabilityai"] = 0.01m,
            ["replicate"] = 0.01m,
            // Free providers
            ["stablediffusion"] = 0,
            ["stable-diffusion"] = 0,
            ["comfyui"] = 0,
            ["stock"] = 0,
            ["pexels"] = 0,
            ["pixabay"] = 0,
            ["unsplash"] = 0,
            ["placeholder"] = 0
        };

        if (!imagePricing.TryGetValue(normalizedProvider, out var costPerImage))
        {
            // Unknown provider - assume it's free (local)
            costPerImage = 0;
            confidence = CostEstimateConfidence.Medium;
        }

        var isFree = costPerImage == 0;
        var cost = isFree ? 0 : imageCount * costPerImage;

        if (imageCount > 0)
        {
            breakdown.Add(new CostBreakdownItem
            {
                Name = "Image Generation",
                Provider = provider,
                Cost = cost,
                IsFree = isFree,
                Units = imageCount,
                UnitType = "images"
            });
        }

        return cost;
    }

    private static bool IsFreeProvider(string provider)
    {
        var freeProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ollama",
            "local",
            "rulebased",
            "rule-based"
        };

        return freeProviders.Contains(provider);
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
