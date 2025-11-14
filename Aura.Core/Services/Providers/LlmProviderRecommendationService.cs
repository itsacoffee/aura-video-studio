using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models.Providers;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service that provides intelligent LLM provider recommendations based on operation type,
/// quality requirements, cost, latency, availability, and user preferences.
/// Always suggests providers but never forces them - user maintains full control.
/// </summary>
public class LlmProviderRecommendationService
{
    private readonly ILogger<LlmProviderRecommendationService> _logger;
    private readonly ProviderHealthMonitoringService _healthMonitor;
    private readonly ProviderCostTrackingService _costTracker;
    private readonly ProviderSettings _settings;
    private readonly Dictionary<string, ILlmProvider> _availableProviders;

    /// <summary>
    /// Provider characteristics for scoring and recommendations
    /// </summary>
    private static readonly Dictionary<string, ProviderCharacteristics> ProviderSpecs = new()
    {
        ["OpenAI"] = new(QualityScore: 95, CostPer1KTokens: 0.002m, AvgLatencySeconds: 3.5, 
            Strengths: ["Creative narrative", "Structured output", "Consistency"]),
        ["Claude"] = new(QualityScore: 93, CostPer1KTokens: 0.008m, AvgLatencySeconds: 4.0,
            Strengths: ["Self-critique", "Linguistic nuance", "Long-form content"]),
        ["Gemini"] = new(QualityScore: 88, CostPer1KTokens: 0.0005m, AvgLatencySeconds: 2.0,
            Strengths: ["Fast response", "Cost-effective", "Good quality"]),
        ["Ollama"] = new(QualityScore: 75, CostPer1KTokens: 0.0m, AvgLatencySeconds: 8.0,
            Strengths: ["Free", "Local", "Privacy"]),
        ["RuleBased"] = new(QualityScore: 60, CostPer1KTokens: 0.0m, AvgLatencySeconds: 0.5,
            Strengths: ["Always available", "Instant", "Free"])
    };

    public LlmProviderRecommendationService(
        ILogger<LlmProviderRecommendationService> logger,
        ProviderHealthMonitoringService healthMonitor,
        ProviderCostTrackingService costTracker,
        ProviderSettings settings,
        Dictionary<string, ILlmProvider> availableProviders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
        _costTracker = costTracker ?? throw new ArgumentNullException(nameof(costTracker));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _availableProviders = availableProviders ?? throw new ArgumentNullException(nameof(availableProviders));
    }

    /// <summary>
    /// Get provider recommendations for a specific operation type.
    /// Returns a ranked list of suitable providers with reasoning.
    /// Returns empty list if recommendations are disabled.
    /// </summary>
    public async Task<List<ProviderRecommendation>> GetRecommendationsAsync(
        LlmOperationType operationType,
        ProviderPreferences? userPreferences = null,
        int estimatedInputTokens = 1000,
        CancellationToken cancellationToken = default)
    {
        userPreferences ??= LoadUserPreferences();
        
        // If recommendations are completely disabled, return empty list
        if (!userPreferences.EnableRecommendations || userPreferences.AssistanceLevel == AssistanceLevel.Off)
        {
            _logger.LogDebug("Provider recommendations are disabled for {OperationType}", operationType);
            return new List<ProviderRecommendation>();
        }

        var startTime = DateTime.UtcNow;
        _logger.LogDebug("Getting provider recommendations for {OperationType}", operationType);

        var recommendations = new List<ProviderRecommendation>();

        foreach (var (providerName, provider) in _availableProviders)
        {
            if (ShouldExcludeProvider(providerName, userPreferences))
            {
                _logger.LogDebug("Excluding {ProviderName} per user preferences", providerName);
                continue;
            }

            var recommendation = await CreateRecommendationAsync(
                providerName, 
                operationType, 
                estimatedInputTokens,
                userPreferences,
                cancellationToken).ConfigureAwait(false);

            if (recommendation != null)
            {
                recommendations.Add(recommendation);
            }
        }

        var sorted = userPreferences.EnableProfiles 
            ? SortRecommendationsByProfile(recommendations, userPreferences.ActiveProfile, operationType)
            : SortRecommendationsByProfile(recommendations, ProviderProfile.Balanced, operationType);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _logger.LogInformation(
            "Generated {Count} recommendations for {OperationType} in {ElapsedMs}ms (target: <50ms)",
            sorted.Count, operationType, elapsed);

        if (elapsed > 50)
        {
            _logger.LogWarning("Recommendation generation exceeded target latency: {ElapsedMs}ms", elapsed);
        }

        return sorted;
    }

    /// <summary>
    /// Get the best single recommendation based on user preferences and operation type.
    /// Returns null if recommendations are disabled and no manual selection is configured.
    /// </summary>
    public async Task<ProviderRecommendation?> GetBestRecommendationAsync(
        LlmOperationType operationType,
        ProviderPreferences? userPreferences = null,
        int estimatedInputTokens = 1000,
        CancellationToken cancellationToken = default)
    {
        userPreferences ??= LoadUserPreferences();

        // Pinned provider always takes precedence (works even when recommendations disabled)
        if (userPreferences.PinnedProvider != null && 
            _availableProviders.ContainsKey(userPreferences.PinnedProvider))
        {
            _logger.LogInformation(
                "Using pinned provider {ProviderName} for {OperationType}",
                userPreferences.PinnedProvider, operationType);

            return await CreateRecommendationAsync(
                userPreferences.PinnedProvider, 
                operationType, 
                estimatedInputTokens,
                userPreferences,
                cancellationToken).ConfigureAwait(false);
        }

        // Per-operation overrides work regardless of recommendation settings
        if (userPreferences.PerOperationOverrides.TryGetValue(operationType, out var overrideProvider) &&
            _availableProviders.ContainsKey(overrideProvider))
        {
            _logger.LogInformation(
                "Using per-operation override {ProviderName} for {OperationType}",
                overrideProvider, operationType);

            return await CreateRecommendationAsync(
                overrideProvider, 
                operationType, 
                estimatedInputTokens,
                userPreferences,
                cancellationToken).ConfigureAwait(false);
        }

        // Global default works regardless of recommendation settings
        if (userPreferences.AlwaysUseDefault && 
            userPreferences.GlobalDefault != null &&
            _availableProviders.ContainsKey(userPreferences.GlobalDefault))
        {
            _logger.LogInformation(
                "Using global default {ProviderName} for {OperationType}",
                userPreferences.GlobalDefault, operationType);

            return await CreateRecommendationAsync(
                userPreferences.GlobalDefault, 
                operationType, 
                estimatedInputTokens,
                userPreferences,
                cancellationToken).ConfigureAwait(false);
        }

        // If recommendations are disabled, return null (user must configure manual selection)
        if (!userPreferences.EnableRecommendations || userPreferences.AssistanceLevel == AssistanceLevel.Off)
        {
            _logger.LogDebug("No provider recommendation available for {OperationType} (recommendations disabled)", operationType);
            return null;
        }

        // Get intelligent recommendations
        var recommendations = await GetRecommendationsAsync(
            operationType, 
            userPreferences, 
            estimatedInputTokens, 
            cancellationToken).ConfigureAwait(false);

        return recommendations.FirstOrDefault();
    }

    private async Task<ProviderRecommendation?> CreateRecommendationAsync(
        string providerName,
        LlmOperationType operationType,
        int estimatedInputTokens,
        ProviderPreferences userPreferences,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (!ProviderSpecs.TryGetValue(providerName, out var specs))
        {
            _logger.LogWarning("No specifications found for provider {ProviderName}", providerName);
            return null;
        }

        var isAvailable = _availableProviders.ContainsKey(providerName);
        
        // Only get health metrics if health monitoring is enabled
        ProviderHealthMetrics? healthMetrics = null;
        if (userPreferences.EnableHealthMonitoring)
        {
            healthMetrics = _healthMonitor.GetProviderHealth(providerName);
        }
        
        var healthStatus = DetermineHealthStatus(healthMetrics);
        var qualityScore = CalculateQualityScore(specs, operationType, healthMetrics);
        var costEstimate = CalculateCost(specs, estimatedInputTokens, operationType);
        var latency = CalculateExpectedLatency(specs, healthMetrics);
        var reasoning = GenerateReasoning(providerName, operationType, specs, qualityScore, costEstimate, healthStatus, userPreferences.AssistanceLevel);
        var confidence = CalculateConfidence(healthMetrics, isAvailable);

        return new ProviderRecommendation
        {
            ProviderName = providerName,
            Reasoning = reasoning,
            QualityScore = qualityScore,
            EstimatedCost = costEstimate,
            ExpectedLatencySeconds = latency,
            IsAvailable = isAvailable,
            HealthStatus = healthStatus,
            Confidence = confidence
        };
    }

    private int CalculateQualityScore(
        ProviderCharacteristics specs,
        LlmOperationType operationType,
        ProviderHealthMetrics? healthMetrics)
    {
        var baseScore = specs.QualityScore;

        var operationBonus = operationType switch
        {
            LlmOperationType.ScriptGeneration when specs.Strengths.Contains("Creative narrative") => 5,
            LlmOperationType.ScriptRefinement when specs.Strengths.Contains("Self-critique") => 5,
            LlmOperationType.VisualPrompts when specs.Strengths.Contains("Structured output") => 5,
            LlmOperationType.NarrationOptimization when specs.Strengths.Contains("Linguistic nuance") => 5,
            LlmOperationType.QuickOperations when specs.Strengths.Contains("Fast response") => 5,
            _ => 0
        };

        var healthPenalty = healthMetrics?.SuccessRatePercent < 90 ? 
            (int)((90 - healthMetrics.SuccessRatePercent) / 2) : 0;

        return Math.Clamp(baseScore + operationBonus - healthPenalty, 0, 100);
    }

    private decimal CalculateCost(
        ProviderCharacteristics specs,
        int estimatedInputTokens,
        LlmOperationType operationType)
    {
        var estimatedOutputTokens = operationType switch
        {
            LlmOperationType.ScriptGeneration => 1500,
            LlmOperationType.ScriptRefinement => 1000,
            LlmOperationType.VisualPrompts => 500,
            LlmOperationType.NarrationOptimization => 300,
            LlmOperationType.QuickOperations => 200,
            LlmOperationType.SceneAnalysis => 400,
            LlmOperationType.ContentComplexity => 350,
            LlmOperationType.NarrativeValidation => 600,
            _ => 500
        };

        var totalTokens = estimatedInputTokens + estimatedOutputTokens;
        return (totalTokens / 1000m) * specs.CostPer1KTokens;
    }

    private double CalculateExpectedLatency(
        ProviderCharacteristics specs,
        ProviderHealthMetrics? healthMetrics)
    {
        if (healthMetrics?.AverageLatencySeconds > 0)
        {
            return (specs.AvgLatencySeconds + healthMetrics.AverageLatencySeconds) / 2;
        }
        return specs.AvgLatencySeconds;
    }

    private string GenerateReasoning(
        string providerName,
        LlmOperationType operationType,
        ProviderCharacteristics specs,
        int qualityScore,
        decimal cost,
        ProviderHealthStatus healthStatus,
        AssistanceLevel assistanceLevel)
    {
        var reasons = new List<string>();

        // Primary reason based on operation type
        var primaryReason = operationType switch
        {
            LlmOperationType.ScriptGeneration => 
                "excels at creative narrative structure and maintaining consistent tone",
            LlmOperationType.ScriptRefinement => 
                "has strong self-critique capabilities ideal for iterative refinement",
            LlmOperationType.VisualPrompts => 
                "generates highly detailed visual descriptions with excellent composition guidance",
            LlmOperationType.NarrationOptimization => 
                "demonstrates superior linguistic nuance for natural speech patterns",
            LlmOperationType.QuickOperations => 
                "provides fast response times suitable for simple tasks",
            _ => "offers balanced performance for this operation type"
        };

        // At Minimal level, only show provider name
        if (assistanceLevel == AssistanceLevel.Minimal)
        {
            return providerName;
        }

        // At Moderate level, show brief reasoning
        if (assistanceLevel == AssistanceLevel.Moderate)
        {
            reasons.Add($"{providerName} {primaryReason}");
            return string.Join(". ", reasons) + ".";
        }

        // At Full level, show detailed information
        reasons.Add($"{providerName} {primaryReason}");

        if (cost == 0)
        {
            reasons.Add("Free to use");
        }
        else if (cost < 0.01m)
        {
            reasons.Add($"Very cost-effective (${cost:F4} estimated)");
        }
        else
        {
            reasons.Add($"Cost: ${cost:F3} estimated");
        }

        reasons.Add($"Quality score: {qualityScore}/100");

        if (healthStatus == ProviderHealthStatus.Healthy)
        {
            reasons.Add("Provider is healthy and reliable");
        }
        else if (healthStatus == ProviderHealthStatus.Degraded)
        {
            reasons.Add("Provider experiencing some issues");
        }
        else if (healthStatus == ProviderHealthStatus.Unhealthy)
        {
            reasons.Add("Provider currently unhealthy - consider alternative");
        }

        return string.Join(". ", reasons) + ".";
    }

    private ProviderHealthStatus DetermineHealthStatus(ProviderHealthMetrics? metrics)
    {
        if (metrics == null)
        {
            return ProviderHealthStatus.Unknown;
        }

        return metrics.Status;
    }

    private int CalculateConfidence(ProviderHealthMetrics? metrics, bool isAvailable)
    {
        if (!isAvailable)
        {
            return 0;
        }

        if (metrics == null || metrics.TotalRequests < 10)
        {
            return 70;
        }

        if (metrics.Status == ProviderHealthStatus.Healthy)
        {
            return 100;
        }
        else if (metrics.Status == ProviderHealthStatus.Degraded)
        {
            return 60;
        }
        else if (metrics.Status == ProviderHealthStatus.Unhealthy)
        {
            return 30;
        }

        return 50;
    }

    private List<ProviderRecommendation> SortRecommendationsByProfile(
        List<ProviderRecommendation> recommendations,
        ProviderProfile profile,
        LlmOperationType operationType)
    {
        return profile switch
        {
            ProviderProfile.MaximumQuality => recommendations
                .OrderByDescending(r => r.QualityScore)
                .ThenByDescending(r => r.Confidence)
                .ToList(),

            ProviderProfile.BudgetConscious => recommendations
                .OrderBy(r => r.EstimatedCost)
                .ThenByDescending(r => r.QualityScore)
                .ToList(),

            ProviderProfile.SpeedOptimized => recommendations
                .OrderBy(r => r.ExpectedLatencySeconds)
                .ThenByDescending(r => r.QualityScore)
                .ToList(),

            ProviderProfile.LocalOnly => recommendations
                .Where(r => r.EstimatedCost == 0)
                .OrderByDescending(r => r.QualityScore)
                .ToList(),

            ProviderProfile.Balanced or _ => recommendations
                .OrderByDescending(r => CalculateBalancedScore(r))
                .ToList()
        };
    }

    private double CalculateBalancedScore(ProviderRecommendation recommendation)
    {
        var qualityWeight = 0.4;
        var costWeight = 0.3;
        var latencyWeight = 0.2;
        var healthWeight = 0.1;

        var normalizedQuality = recommendation.QualityScore / 100.0;
        
        var normalizedCost = recommendation.EstimatedCost == 0 ? 1.0 : 
            Math.Max(0, 1.0 - (double)(recommendation.EstimatedCost / 0.05m));
        
        var normalizedLatency = Math.Max(0, 1.0 - (recommendation.ExpectedLatencySeconds / 15.0));
        
        var normalizedHealth = recommendation.HealthStatus switch
        {
            ProviderHealthStatus.Healthy => 1.0,
            ProviderHealthStatus.Degraded => 0.6,
            ProviderHealthStatus.Unhealthy => 0.3,
            _ => 0.5
        };

        return (normalizedQuality * qualityWeight) +
               (normalizedCost * costWeight) +
               (normalizedLatency * latencyWeight) +
               (normalizedHealth * healthWeight);
    }

    private bool ShouldExcludeProvider(string providerName, ProviderPreferences preferences)
    {
        return preferences.ExcludedProviders.Contains(providerName);
    }

    private ProviderPreferences LoadUserPreferences()
    {
        // Default preferences: all recommendation features disabled (opt-in model)
        return new ProviderPreferences
        {
            EnableRecommendations = false,
            AssistanceLevel = AssistanceLevel.Off,
            EnableHealthMonitoring = false,
            EnableCostTracking = false,
            EnableLearning = false,
            EnableProfiles = false,
            EnableAutoFallback = false,
            ActiveProfile = ProviderProfile.Balanced,
            AlwaysUseDefault = false,
            PerOperationOverrides = new Dictionary<LlmOperationType, string>(),
            ExcludedProviders = new HashSet<string>(),
            FallbackChains = new Dictionary<LlmOperationType, List<string>>(),
            PerProviderBudgetLimits = new Dictionary<string, decimal>(),
            HardBudgetLimit = false
        };
    }
}

/// <summary>
/// Static characteristics of a provider
/// </summary>
internal record ProviderCharacteristics(
    int QualityScore,
    decimal CostPer1KTokens,
    double AvgLatencySeconds,
    string[] Strengths);
