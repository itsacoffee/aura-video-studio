using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.FrameAnalysis;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VideoOptimization;

/// <summary>
/// Service for A/B testing different pacing strategies
/// </summary>
public class ABTestingService
{
    private readonly ILogger<ABTestingService> _logger;
    private readonly AttentionPredictionService _attentionPredictor;

    public ABTestingService(
        ILogger<ABTestingService> logger,
        AttentionPredictionService attentionPredictor)
    {
        _logger = logger;
        _attentionPredictor = attentionPredictor;
    }

    /// <summary>
    /// Compares multiple pacing strategies and recommends the best one
    /// </summary>
    public async Task<ABTestResult> CompareStrategiesAsync(
        List<PacingStrategy> strategies,
        ABTestOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Comparing {StrategyCount} pacing strategies", strategies.Count);

        if (strategies == null || strategies.Count < 2)
        {
            throw new ArgumentException("At least 2 strategies required for comparison", nameof(strategies));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<StrategyTestResult>();

        foreach (var strategy in strategies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = await EvaluateStrategyAsync(strategy, options, cancellationToken);
            results.Add(result);
        }

        // Determine winner based on composite score
        var winner = results.OrderByDescending(r => r.CompositeScore).First();
        var comparison = GenerateComparison(results);

        return new ABTestResult(
            StrategyResults: results,
            WinningStrategy: winner,
            Comparison: comparison,
            Recommendation: GenerateRecommendation(winner, results)
        );
    }

    /// <summary>
    /// Generates variant pacing strategies from a base timeline
    /// </summary>
    public async Task<List<PacingStrategy>> GenerateVariantsAsync(
        List<Scene> baseScenes,
        VariantGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating pacing variants from {SceneCount} scenes", baseScenes.Count);

        cancellationToken.ThrowIfCancellationRequested();

        var strategies = new List<PacingStrategy>
        {
            // Original strategy
            new PacingStrategy(
                Name: "Original",
                Description: "Original scene timing and pacing",
                Scenes: baseScenes,
                ApproachType: PacingApproach.Balanced
            )
        };

        // Generate fast-paced variant
        if (options.IncludeFastPaced)
        {
            var fastScenes = AdjustScenePacing(baseScenes, 0.85, "Faster");
            strategies.Add(new PacingStrategy(
                Name: "Fast-Paced",
                Description: "15% faster overall pacing for higher energy",
                Scenes: fastScenes,
                ApproachType: PacingApproach.FastPaced
            ));
        }

        // Generate slow-paced variant
        if (options.IncludeSlowPaced)
        {
            var slowScenes = AdjustScenePacing(baseScenes, 1.15, "Slower");
            strategies.Add(new PacingStrategy(
                Name: "Slow-Paced",
                Description: "15% slower pacing for better comprehension",
                Scenes: slowScenes,
                ApproachType: PacingApproach.SlowPaced
            ));
        }

        // Generate dynamic variant (vary pacing throughout)
        if (options.IncludeDynamic)
        {
            var dynamicScenes = CreateDynamicPacing(baseScenes);
            strategies.Add(new PacingStrategy(
                Name: "Dynamic",
                Description: "Variable pacing with faster opening and strategic slowdowns",
                Scenes: dynamicScenes,
                ApproachType: PacingApproach.Dynamic
            ));
        }

        await Task.CompletedTask;
        return strategies;
    }

    private async Task<StrategyTestResult> EvaluateStrategyAsync(
        PacingStrategy strategy,
        ABTestOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Predict attention and engagement
        var attentionPrediction = await _attentionPredictor.PredictAttentionAsync(
            strategy.Scenes,
            new PredictionOptions(),
            cancellationToken);

        // Calculate various metrics
        var totalDuration = strategy.Scenes.Sum(s => s.Duration.TotalSeconds);
        var avgSceneDuration = strategy.Scenes.Average(s => s.Duration.TotalSeconds);
        var paceConsistency = CalculatePaceConsistency(strategy.Scenes);
        var narrativeFlow = CalculateNarrativeFlow(strategy.Scenes);

        // Calculate composite score
        var compositeScore = CalculateCompositeScore(
            attentionPrediction.OverallEngagement,
            attentionPrediction.PredictedRetentionRate,
            paceConsistency,
            narrativeFlow,
            options);

        return new StrategyTestResult(
            Strategy: strategy,
            OverallEngagement: attentionPrediction.OverallEngagement,
            RetentionRate: attentionPrediction.PredictedRetentionRate,
            TotalDuration: TimeSpan.FromSeconds(totalDuration),
            AverageSceneDuration: TimeSpan.FromSeconds(avgSceneDuration),
            PaceConsistency: paceConsistency,
            NarrativeFlow: narrativeFlow,
            EngagementDropCount: attentionPrediction.EngagementDrops.Count,
            CompositeScore: compositeScore
        );
    }

    private List<Scene> AdjustScenePacing(List<Scene> scenes, double multiplier, string modifier)
    {
        var adjustedScenes = new List<Scene>();
        var cumulativeStart = TimeSpan.Zero;

        foreach (var scene in scenes)
        {
            var newDuration = TimeSpan.FromSeconds(scene.Duration.TotalSeconds * multiplier);
            
            adjustedScenes.Add(new Scene(
                Index: scene.Index,
                Heading: $"{scene.Heading} ({modifier})",
                Script: scene.Script,
                Start: cumulativeStart,
                Duration: newDuration
            ));

            cumulativeStart += newDuration;
        }

        return adjustedScenes;
    }

    private List<Scene> CreateDynamicPacing(List<Scene> scenes)
    {
        var dynamicScenes = new List<Scene>();
        var cumulativeStart = TimeSpan.Zero;

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            
            // Opening: faster (0.9x)
            // Middle buildup: gradually slower (1.0x to 1.1x)
            // Climax/payoff: faster (0.85x)
            // Closing: moderate (0.95x)
            var multiplier = CalculateDynamicMultiplier(i, scenes.Count);
            var newDuration = TimeSpan.FromSeconds(scene.Duration.TotalSeconds * multiplier);
            
            dynamicScenes.Add(new Scene(
                Index: scene.Index,
                Heading: $"{scene.Heading} (Dynamic)",
                Script: scene.Script,
                Start: cumulativeStart,
                Duration: newDuration
            ));

            cumulativeStart += newDuration;
        }

        return dynamicScenes;
    }

    private double CalculateDynamicMultiplier(int sceneIndex, int totalScenes)
    {
        var position = (double)sceneIndex / totalScenes;

        return position switch
        {
            < 0.15 => 0.90,  // Fast opening
            < 0.4 => 1.0,    // Normal pace
            < 0.7 => 1.1,    // Slower buildup
            < 0.85 => 0.85,  // Fast climax
            _ => 0.95        // Moderate closing
        };
    }

    private double CalculatePaceConsistency(List<Scene> scenes)
    {
        var paces = scenes.Select(s =>
        {
            var wordCount = s.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return wordCount / s.Duration.TotalSeconds;
        }).ToList();

        if (paces.Count == 0) return 0.0;

        var avgPace = paces.Average();
        var variance = paces.Select(p => Math.Pow(p - avgPace, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        // Lower standard deviation = higher consistency
        // Normalize to 0-1 range (assuming typical stdDev is 0-2)
        return Math.Clamp(1.0 - (stdDev / 2.0), 0.0, 1.0);
    }

    private double CalculateNarrativeFlow(List<Scene> scenes)
    {
        if (scenes.Count < 2) return 1.0;

        var flowScore = 1.0;
        
        // Penalize for abrupt changes in scene duration
        for (int i = 0; i < scenes.Count - 1; i++)
        {
            var durationRatio = scenes[i + 1].Duration.TotalSeconds / scenes[i].Duration.TotalSeconds;
            
            // Optimal ratio is between 0.7 and 1.3 (not too abrupt)
            if (durationRatio < 0.5 || durationRatio > 2.0)
            {
                flowScore -= 0.1;
            }
        }

        return Math.Clamp(flowScore, 0.0, 1.0);
    }

    private double CalculateCompositeScore(
        double engagement,
        double retention,
        double consistency,
        double flow,
        ABTestOptions options)
    {
        var weights = options.ScoringWeights;
        
        return (engagement * weights.EngagementWeight) +
               (retention * weights.RetentionWeight) +
               (consistency * weights.ConsistencyWeight) +
               (flow * weights.FlowWeight);
    }

    private string GenerateComparison(List<StrategyTestResult> results)
    {
        var comparison = "Strategy Comparison:\n";
        
        foreach (var result in results.OrderByDescending(r => r.CompositeScore))
        {
            comparison += $"- {result.Strategy.Name}: Score {result.CompositeScore:F2}, " +
                         $"Engagement {result.OverallEngagement:F2}, " +
                         $"Retention {result.RetentionRate:F2}\n";
        }

        return comparison;
    }

    private string GenerateRecommendation(StrategyTestResult winner, List<StrategyTestResult> allResults)
    {
        var recommendation = $"Recommended strategy: {winner.Strategy.Name}. ";
        
        var runner = allResults.OrderByDescending(r => r.CompositeScore).Skip(1).FirstOrDefault();
        if (runner != null)
        {
            var improvement = ((winner.CompositeScore - runner.CompositeScore) / runner.CompositeScore) * 100;
            recommendation += $"This strategy scores {improvement:F1}% better than the next best option ({runner.Strategy.Name}). ";
        }

        recommendation += $"Expected engagement: {winner.OverallEngagement:P0}, " +
                         $"retention rate: {winner.RetentionRate:P0}.";

        return recommendation;
    }
}

/// <summary>
/// Pacing strategy to test
/// </summary>
public record PacingStrategy(
    string Name,
    string Description,
    List<Scene> Scenes,
    PacingApproach ApproachType
);

/// <summary>
/// Options for A/B testing
/// </summary>
public record ABTestOptions(
    ScoringWeights ScoringWeights
)
{
    public ABTestOptions() : this(new ScoringWeights()) { }
}

/// <summary>
/// Weights for composite scoring
/// </summary>
public record ScoringWeights(
    double EngagementWeight = 0.35,
    double RetentionWeight = 0.35,
    double ConsistencyWeight = 0.15,
    double FlowWeight = 0.15
);

/// <summary>
/// Options for generating variants
/// </summary>
public record VariantGenerationOptions(
    bool IncludeFastPaced = true,
    bool IncludeSlowPaced = true,
    bool IncludeDynamic = true
);

/// <summary>
/// Result of A/B test
/// </summary>
public record ABTestResult(
    List<StrategyTestResult> StrategyResults,
    StrategyTestResult WinningStrategy,
    string Comparison,
    string Recommendation
);

/// <summary>
/// Test result for a single strategy
/// </summary>
public record StrategyTestResult(
    PacingStrategy Strategy,
    double OverallEngagement,
    double RetentionRate,
    TimeSpan TotalDuration,
    TimeSpan AverageSceneDuration,
    double PaceConsistency,
    double NarrativeFlow,
    int EngagementDropCount,
    double CompositeScore
);

/// <summary>
/// Type of pacing approach
/// </summary>
public enum PacingApproach
{
    Balanced,
    FastPaced,
    SlowPaced,
    Dynamic,
    Custom
}
