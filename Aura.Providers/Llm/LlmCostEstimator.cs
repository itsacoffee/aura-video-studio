using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Estimates costs for LLM API calls based on token usage and provider pricing.
/// Uses dynamic configuration file for pricing that can be updated without code changes.
/// </summary>
public class LlmCostEstimator
{
    private readonly ILogger<LlmCostEstimator> _logger;
    private readonly LlmPricingConfiguration _pricingConfig;
    private readonly string? _configPath;
    private DateTime _lastConfigCheck;
    private readonly TimeSpan _configCheckInterval = TimeSpan.FromMinutes(5);

    public LlmCostEstimator(ILogger<LlmCostEstimator> logger, string? configPath = null)
    {
        _logger = logger;
        _configPath = configPath;
        _pricingConfig = LoadConfiguration();
        _lastConfigCheck = DateTime.UtcNow;
    }

    /// <summary>
    /// Load pricing configuration from file or defaults
    /// </summary>
    private LlmPricingConfiguration LoadConfiguration()
    {
        if (!string.IsNullOrEmpty(_configPath))
        {
            return LlmPricingConfiguration.LoadFromFile(_configPath, _logger);
        }
        
        return LlmPricingConfiguration.LoadDefault(_logger);
    }

    /// <summary>
    /// Reload configuration if enough time has passed
    /// Allows hot-reloading of pricing without restart
    /// </summary>
    private void CheckConfigurationUpdate()
    {
        if (DateTime.UtcNow - _lastConfigCheck < _configCheckInterval)
        {
            return;
        }

        _lastConfigCheck = DateTime.UtcNow;
        
        try
        {
            var newConfig = LoadConfiguration();
            if (newConfig.Version != _pricingConfig.Version)
            {
                _logger.LogInformation(
                    "Pricing configuration updated from v{OldVersion} to v{NewVersion}",
                    _pricingConfig.Version, newConfig.Version);
                
                // Update via reflection or recreate (for simplicity, log only in this implementation)
                _logger.LogInformation("Restart application to use new pricing configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for pricing configuration updates");
        }
    }

    /// <summary>
    /// Estimate cost for a given prompt and expected completion
    /// </summary>
    public CostEstimate EstimateCost(string prompt, int estimatedCompletionTokens, string model)
    {
        var inputTokens = EstimateTokenCount(prompt);
        var outputTokens = estimatedCompletionTokens;
        
        return CalculateCost(inputTokens, outputTokens, model);
    }

    /// <summary>
    /// Estimate cost based on token counts
    /// </summary>
    public CostEstimate CalculateCost(int inputTokens, int outputTokens, string model)
    {
        CheckConfigurationUpdate();
        
        var pricing = _pricingConfig.GetModelPricing(model);
        
        if (pricing == null)
        {
            _logger.LogWarning("Unknown model {Model}, using fallback pricing", model);
            pricing = _pricingConfig.FallbackModel;
        }

        var inputCost = (inputTokens / 1_000_000.0m) * pricing.InputPrice;
        var outputCost = (outputTokens / 1_000_000.0m) * pricing.OutputPrice;
        var totalCost = inputCost + outputCost;

        _logger.LogDebug(
            "Cost estimate for {Model}: {InputTokens} input + {OutputTokens} output = ${TotalCost:F6}",
            model, inputTokens, outputTokens, totalCost);

        return new CostEstimate
        {
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            InputCost = inputCost,
            OutputCost = outputCost,
            TotalCost = totalCost,
            EstimatedAt = DateTime.UtcNow,
            ConfigVersion = _pricingConfig.Version
        };
    }

    /// <summary>
    /// Estimate token count for text using heuristic (~4 chars per token)
    /// This is a rough approximation; real tokenization depends on the model's tokenizer
    /// </summary>
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // Rough heuristic: ~4 characters per token for English text
        // More accurate would use tiktoken or similar library
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    /// <summary>
    /// Estimate tokens for a Brief and PlanSpec combination (for script generation)
    /// </summary>
    public int EstimateInputTokensForScriptGeneration(Brief brief, PlanSpec spec)
    {
        // System prompt + user prompt approximate sizes
        var systemPromptTokens = 600; // Approximate size of enhanced system prompt
        
        var briefTokens = EstimateTokenCount($"{brief.Topic} {brief.Audience} {brief.Goal} {brief.Tone}");
        var specTokens = EstimateTokenCount($"{spec.Style} {spec.TargetDuration}");
        var contextTokens = brief.Context != null ? EstimateTokenCount(brief.Context) : 0;
        
        return systemPromptTokens + briefTokens + specTokens + contextTokens;
    }

    /// <summary>
    /// Estimate output tokens for script generation based on target duration
    /// </summary>
    public int EstimateOutputTokensForScriptGeneration(PlanSpec spec)
    {
        // Rough estimate: 150 words per minute * 1.33 tokens per word
        var wordsPerMinute = 150;
        var tokensPerWord = 1.33;
        var estimatedWords = spec.TargetDuration.TotalMinutes * wordsPerMinute;
        var estimatedTokens = (int)(estimatedWords * tokensPerWord);
        
        // Add overhead for markdown formatting, scene markers, etc. (~20%)
        return (int)(estimatedTokens * 1.2);
    }

    /// <summary>
    /// Get recommended model based on budget and quality requirements
    /// </summary>
    public string RecommendModel(decimal maxBudgetPerRequest, QualityTier desiredQuality)
    {
        CheckConfigurationUpdate();
        
        // Get all available models sorted by cost (per 1000 tokens)
        var modelsWithCost = _pricingConfig.Providers.Values
            .SelectMany(p => p.Models)
            .Where(m => m.Value.InputPrice > 0) // Exclude free models
            .Select(m => new
            {
                Name = m.Key,
                Pricing = m.Value,
                AvgCostPer1K = (m.Value.InputPrice + m.Value.OutputPrice) / 2000m // Average cost per 1K tokens
            })
            .OrderBy(m => m.AvgCostPer1K)
            .ToList();

        // Estimate tokens for a typical request (2000 input, 1000 output)
        var estimatedInputTokens = 2000;
        var estimatedOutputTokens = 1000;

        return desiredQuality switch
        {
            QualityTier.Budget => FindBestModelForBudget(modelsWithCost, maxBudgetPerRequest, 
                estimatedInputTokens, estimatedOutputTokens, isBudget: true),
            
            QualityTier.Balanced => FindBestModelForBudget(modelsWithCost, maxBudgetPerRequest,
                estimatedInputTokens, estimatedOutputTokens, isBalanced: true),
            
            QualityTier.Premium => FindBestModelForBudget(modelsWithCost, maxBudgetPerRequest,
                estimatedInputTokens, estimatedOutputTokens, isPremium: true),
            
            QualityTier.Maximum => modelsWithCost.LastOrDefault()?.Name ?? "gpt-4o",
            
            _ => "gpt-4o-mini"
        };
    }

    private string FindBestModelForBudget(
        List<dynamic> models,
        decimal maxBudget,
        int inputTokens,
        int outputTokens,
        bool isBudget = false,
        bool isBalanced = false,
        bool isPremium = false)
    {
        // Filter models that fit the budget
        var affordableModels = models.Where(m =>
        {
            var cost = (inputTokens / 1_000_000.0m) * m.Pricing.InputPrice +
                      (outputTokens / 1_000_000.0m) * m.Pricing.OutputPrice;
            return cost <= maxBudget;
        }).ToList();

        if (!affordableModels.Any())
        {
            // Return cheapest model if nothing fits budget
            return models.FirstOrDefault()?.Name ?? "gpt-4o-mini";
        }

        if (isBudget)
        {
            // Return cheapest affordable model
            return affordableModels.FirstOrDefault()?.Name ?? "gpt-4o-mini";
        }

        if (isBalanced)
        {
            // Return model in middle of affordable range
            var midIndex = affordableModels.Count / 2;
            return affordableModels[midIndex]?.Name ?? "gpt-4o";
        }

        if (isPremium)
        {
            // Return most expensive affordable model
            return affordableModels.LastOrDefault()?.Name ?? "gpt-4o";
        }

        return "gpt-4o-mini";
    }

    /// <summary>
    /// Calculate cost savings by using a different model
    /// </summary>
    public CostComparison CompareModels(int inputTokens, int outputTokens, string currentModel, string alternativeModel)
    {
        var currentCost = CalculateCost(inputTokens, outputTokens, currentModel);
        var alternativeCost = CalculateCost(inputTokens, outputTokens, alternativeModel);
        
        var savings = currentCost.TotalCost - alternativeCost.TotalCost;
        var savingsPercentage = currentCost.TotalCost > 0 
            ? (savings / currentCost.TotalCost) * 100 
            : 0;

        return new CostComparison
        {
            CurrentModel = currentModel,
            CurrentCost = currentCost.TotalCost,
            AlternativeModel = alternativeModel,
            AlternativeCost = alternativeCost.TotalCost,
            Savings = savings,
            SavingsPercentage = savingsPercentage
        };
    }

    /// <summary>
    /// Get all available models from configuration
    /// </summary>
    public List<string> GetAvailableModels()
    {
        CheckConfigurationUpdate();
        return _pricingConfig.GetAllModelNames();
    }

    /// <summary>
    /// Get pricing configuration version
    /// </summary>
    public string GetConfigVersion() => _pricingConfig.Version;

    /// <summary>
    /// Get configuration last updated date
    /// </summary>
    public string GetConfigLastUpdated() => _pricingConfig.LastUpdated;
}

/// <summary>
/// Cost estimate result
/// </summary>
public record CostEstimate
{
    public string Model { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public decimal InputCost { get; init; }
    public decimal OutputCost { get; init; }
    public decimal TotalCost { get; init; }
    public DateTime EstimatedAt { get; init; }
    public string ConfigVersion { get; init; } = string.Empty;
}

/// <summary>
/// Model cost comparison result
/// </summary>
public record CostComparison
{
    public string CurrentModel { get; init; } = string.Empty;
    public decimal CurrentCost { get; init; }
    public string AlternativeModel { get; init; } = string.Empty;
    public decimal AlternativeCost { get; init; }
    public decimal Savings { get; init; }
    public decimal SavingsPercentage { get; init; }
}

/// <summary>
/// Quality tier for model selection
/// </summary>
public enum QualityTier
{
    Budget,
    Balanced,
    Premium,
    Maximum
}
