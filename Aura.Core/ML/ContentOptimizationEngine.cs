using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Aura.Core.Services.Learning;
using Aura.Core.Services.PerformanceAnalytics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.ML;

/// <summary>
/// Main orchestrator for ML-driven content optimization
/// Coordinates prompt enhancement, quality prediction, and provider selection
/// All features are opt-in and user-configurable
/// </summary>
public class ContentOptimizationEngine
{
    private readonly ILogger<ContentOptimizationEngine> _logger;
    private readonly DynamicPromptEnhancer _promptEnhancer;
    private readonly ContentSuccessPredictionModel _predictionModel;
    private readonly ProviderPerformanceTracker _providerTracker;
    private readonly LearningService? _learningService;
    private readonly PerformanceAnalyticsService? _analyticsService;

    public ContentOptimizationEngine(
        ILogger<ContentOptimizationEngine> logger,
        DynamicPromptEnhancer promptEnhancer,
        ContentSuccessPredictionModel predictionModel,
        ProviderPerformanceTracker providerTracker,
        LearningService? learningService = null,
        PerformanceAnalyticsService? analyticsService = null)
    {
        _logger = logger;
        _promptEnhancer = promptEnhancer;
        _predictionModel = predictionModel;
        _providerTracker = providerTracker;
        _learningService = learningService;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Optimize content generation request based on user settings
    /// </summary>
    public async Task<OptimizationResult> OptimizeContentRequestAsync(
        Brief brief,
        PlanSpec spec,
        AIOptimizationSettings settings,
        string? profileId = null,
        CancellationToken ct = default)
    {
        var result = new OptimizationResult
        {
            OriginalBrief = brief,
            OriginalSpec = spec,
            OptimizedBrief = brief,
            OptimizedSpec = spec,
            Settings = settings
        };

        // If optimization is disabled, return original request
        if (!settings.Enabled)
        {
            _logger.LogDebug("AI optimization disabled, using original request");
            result.Applied = false;
            result.Reason = "Optimization disabled by user settings";
            return result;
        }

        _logger.LogInformation("Optimizing content request (level: {Level})", settings.Level);

        try
        {
            // Step 1: Predict content success
            var prediction = await PredictContentSuccessAsync(brief, spec, profileId, ct).ConfigureAwait(false);
            result.Prediction = prediction;

            _logger.LogInformation(
                "Content success prediction: {Score:F1} (confidence: {Confidence:F2})",
                prediction.PredictedScore, prediction.Confidence);

            // Step 2: Check if quality threshold is met
            if (settings.AutoRegenerateIfLowQuality &&
                !prediction.MeetsThreshold(settings.MinimumQualityThreshold))
            {
                _logger.LogWarning(
                    "Predicted quality {Score:F1} below threshold {Threshold}",
                    prediction.PredictedScore, settings.MinimumQualityThreshold);
                
                result.RequiresRegeneration = true;
                result.Reason = $"Predicted quality ({prediction.PredictedScore:F1}) below threshold ({settings.MinimumQualityThreshold})";
            }

            // Step 3: Apply optimizations based on prediction
            if (settings.Level != OptimizationLevel.Conservative)
            {
                // Enhance spec based on prediction insights
                result.OptimizedSpec = EnhanceSpecFromPrediction(spec, prediction, settings);
                result.Applied = true;
            }

            // Step 4: Track decision if learning is enabled
            if (settings.TrackPerformanceData && _learningService != null && !string.IsNullOrEmpty(profileId))
            {
                await TrackOptimizationDecisionAsync(profileId, result, ct).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during content optimization");
            result.Applied = false;
            result.Reason = "Optimization failed, using original request";
            return result;
        }
    }

    /// <summary>
    /// Predict content success using ML model
    /// </summary>
    private async Task<PredictionResult> PredictContentSuccessAsync(
        Brief brief,
        PlanSpec spec,
        string? profileId,
        CancellationToken ct)
    {
        // Extract features from brief and spec
        var features = new ContentFeatures
        {
            Topic = brief.Topic,
            DurationMinutes = spec.TargetDuration.TotalMinutes,
            Pacing = spec.Pacing.ToString(),
            Density = spec.Density.ToString(),
            Tone = brief.Tone,
            Audience = brief.Audience,
            Goal = brief.Goal,
            HistoricalAverageScore = await GetHistoricalAverageScoreAsync(brief.Tone, profileId, ct).ConfigureAwait(false)
        };

        return _predictionModel.PredictSuccess(features);
    }

    /// <summary>
    /// Get historical average score for similar content
    /// </summary>
    private async Task<double> GetHistoricalAverageScoreAsync(
        string tone,
        string? profileId,
        CancellationToken ct)
    {
        if (_analyticsService == null || string.IsNullOrEmpty(profileId))
        {
            return 0;
        }

        try
        {
            // Placeholder for fetching historical data
            // In full implementation, query analytics service for similar content
            await Task.CompletedTask.ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch historical average score");
            return 0;
        }
    }

    /// <summary>
    /// Enhance PlanSpec based on prediction insights
    /// </summary>
    private PlanSpec EnhanceSpecFromPrediction(
        PlanSpec originalSpec,
        PredictionResult prediction,
        AIOptimizationSettings settings)
    {
        // For conservative mode, return original
        if (settings.Level == OptimizationLevel.Conservative)
        {
            return originalSpec;
        }

        // If prediction is already high, minimal changes
        if (prediction.PredictedScore >= 85)
        {
            return originalSpec;
        }

        // Suggest optimizations based on prediction factors
        var styleAdditions = new System.Text.StringBuilder(originalSpec.Style ?? "");

        foreach (var factor in prediction.ContributingFactors)
        {
            if (factor.Contains("may be too long", StringComparison.OrdinalIgnoreCase))
            {
                styleAdditions.AppendLine("\nNote: Keep content concise and engaging throughout.");
            }
            else if (factor.Contains("too complex", StringComparison.OrdinalIgnoreCase))
            {
                styleAdditions.AppendLine("\nNote: Simplify complex concepts for broader accessibility.");
            }
        }

        return originalSpec with
        {
            Style = styleAdditions.Length > (originalSpec.Style?.Length ?? 0)
                ? styleAdditions.ToString()
                : originalSpec.Style ?? string.Empty
        };
    }

    /// <summary>
    /// Track optimization decision for learning
    /// </summary>
    private async Task TrackOptimizationDecisionAsync(
        string profileId,
        OptimizationResult result,
        CancellationToken ct)
    {
        try
        {
            // Record decision in learning service
            // This allows the system to learn from user's response to optimizations
            _logger.LogDebug("Tracking optimization decision for profile {ProfileId}", profileId);
            await Task.CompletedTask.ConfigureAwait(false); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track optimization decision");
        }
    }

    /// <summary>
    /// Select best provider for content generation
    /// </summary>
    public async Task<string?> SelectBestProviderAsync(
        string contentType,
        AIOptimizationSettings settings,
        CancellationToken ct = default)
    {
        if (settings.SelectionMode == ProviderSelectionMode.Manual)
        {
            _logger.LogDebug("Manual provider selection mode, skipping automatic selection");
            return null;
        }

        var availableProviders = settings.EnabledProviders;
        
        if (availableProviders.Count == 0)
        {
            _logger.LogWarning("No providers enabled for optimization");
            return null;
        }

        return await _providerTracker.GetBestProviderAsync(contentType, availableProviders, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Record generation outcome for learning
    /// </summary>
    public async Task RecordGenerationOutcomeAsync(
        string providerName,
        string contentType,
        double qualityScore,
        TimeSpan duration,
        bool success,
        AIOptimizationSettings settings,
        CancellationToken ct = default)
    {
        if (!settings.TrackPerformanceData)
        {
            return;
        }

        await _providerTracker.RecordGenerationAsync(
            providerName,
            contentType,
            qualityScore,
            duration,
            success,
            ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Recorded generation outcome: {Provider}, Quality={Quality:F1}, Success={Success}",
            providerName, qualityScore, success);
    }
}

/// <summary>
/// Result of content optimization
/// </summary>
public record OptimizationResult
{
    public Brief OriginalBrief { get; init; } = null!;
    public PlanSpec OriginalSpec { get; init; } = null!;
    public Brief OptimizedBrief { get; set; } = null!;
    public PlanSpec OptimizedSpec { get; set; } = null!;
    public AIOptimizationSettings Settings { get; init; } = null!;
    public bool Applied { get; set; }
    public bool RequiresRegeneration { get; set; }
    public string? Reason { get; set; }
    public PredictionResult? Prediction { get; set; }
}
