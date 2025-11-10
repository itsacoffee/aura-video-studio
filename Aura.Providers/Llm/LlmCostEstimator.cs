using System;
using System.Collections.Generic;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Estimates costs for LLM API calls based on token usage and provider pricing.
/// Updated with current pricing as of late 2024.
/// </summary>
public class LlmCostEstimator
{
    private readonly ILogger<LlmCostEstimator> _logger;
    
    // Pricing per 1M tokens (input / output) in USD
    private static readonly Dictionary<string, (decimal Input, decimal Output)> ModelPricing = new()
    {
        // OpenAI Pricing (as of Nov 2024)
        { "gpt-4o", (2.50m, 10.00m) },
        { "gpt-4o-mini", (0.150m, 0.600m) },
        { "gpt-4-turbo", (10.00m, 30.00m) },
        { "gpt-4", (30.00m, 60.00m) },
        { "gpt-3.5-turbo", (0.50m, 1.50m) },
        { "gpt-3.5-turbo-16k", (3.00m, 4.00m) },
        
        // Anthropic Pricing
        { "claude-3-5-sonnet-20241022", (3.00m, 15.00m) },
        { "claude-3-opus-20240229", (15.00m, 75.00m) },
        { "claude-3-sonnet-20240229", (3.00m, 15.00m) },
        { "claude-3-haiku-20240307", (0.25m, 1.25m) },
        
        // Google Gemini Pricing
        { "gemini-1.5-pro", (1.25m, 5.00m) },
        { "gemini-1.5-flash", (0.075m, 0.30m) },
        { "gemini-pro", (0.50m, 1.50m) },
        
        // Azure OpenAI (same as OpenAI, region-dependent)
        { "azure-gpt-4o", (2.50m, 10.00m) },
        { "azure-gpt-4o-mini", (0.150m, 0.600m) },
        { "azure-gpt-4", (30.00m, 60.00m) },
        { "azure-gpt-35-turbo", (0.50m, 1.50m) },
        
        // Free/Local providers
        { "ollama", (0.00m, 0.00m) },
        { "local", (0.00m, 0.00m) },
        { "rulebased", (0.00m, 0.00m) }
    };

    public LlmCostEstimator(ILogger<LlmCostEstimator> logger)
    {
        _logger = logger;
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
        var normalizedModel = NormalizeModelName(model);
        
        if (!ModelPricing.TryGetValue(normalizedModel, out var pricing))
        {
            _logger.LogWarning("Unknown model {Model}, using gpt-4o-mini pricing as fallback", model);
            pricing = ModelPricing["gpt-4o-mini"];
        }

        var inputCost = (inputTokens / 1_000_000.0m) * pricing.Input;
        var outputCost = (outputTokens / 1_000_000.0m) * pricing.Output;
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
            EstimatedAt = DateTime.UtcNow
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
        return desiredQuality switch
        {
            QualityTier.Budget when maxBudgetPerRequest >= 0.01m => "gpt-4o-mini",
            QualityTier.Budget => "claude-3-haiku-20240307",
            
            QualityTier.Balanced when maxBudgetPerRequest >= 0.05m => "gpt-4o",
            QualityTier.Balanced => "gpt-4o-mini",
            
            QualityTier.Premium when maxBudgetPerRequest >= 0.50m => "gpt-4-turbo",
            QualityTier.Premium when maxBudgetPerRequest >= 0.20m => "claude-3-5-sonnet-20241022",
            QualityTier.Premium => "gpt-4o",
            
            QualityTier.Maximum => "gpt-4-turbo",
            
            _ => "gpt-4o-mini"
        };
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
    /// Normalize model name to match pricing dictionary
    /// </summary>
    private string NormalizeModelName(string model)
    {
        var normalized = model.ToLowerInvariant().Trim();
        
        // Handle common variations
        if (normalized.Contains("gpt-4o-mini") || normalized.Contains("gpt-4o-mini"))
            return "gpt-4o-mini";
        if (normalized.Contains("gpt-4o"))
            return "gpt-4o";
        if (normalized.Contains("gpt-4-turbo"))
            return "gpt-4-turbo";
        if (normalized.Contains("gpt-4"))
            return "gpt-4";
        if (normalized.Contains("gpt-3.5") && normalized.Contains("16k"))
            return "gpt-3.5-turbo-16k";
        if (normalized.Contains("gpt-3.5"))
            return "gpt-3.5-turbo";
        if (normalized.Contains("claude-3-5-sonnet"))
            return "claude-3-5-sonnet-20241022";
        if (normalized.Contains("claude-3-opus"))
            return "claude-3-opus-20240229";
        if (normalized.Contains("claude-3-sonnet"))
            return "claude-3-sonnet-20240229";
        if (normalized.Contains("claude-3-haiku") || normalized.Contains("claude-haiku"))
            return "claude-3-haiku-20240307";
        if (normalized.Contains("gemini-1.5-pro"))
            return "gemini-1.5-pro";
        if (normalized.Contains("gemini-1.5-flash") || normalized.Contains("gemini-flash"))
            return "gemini-1.5-flash";
        if (normalized.Contains("gemini"))
            return "gemini-pro";
        if (normalized.Contains("ollama"))
            return "ollama";
        if (normalized.Contains("local"))
            return "local";
        if (normalized.Contains("rule"))
            return "rulebased";
        
        return normalized;
    }
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
