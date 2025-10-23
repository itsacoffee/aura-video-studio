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
/// Service for recommending video transitions based on content analysis
/// </summary>
public class TransitionRecommendationService
{
    private readonly ILogger<TransitionRecommendationService> _logger;

    public TransitionRecommendationService(ILogger<TransitionRecommendationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes scenes and recommends optimal transitions
    /// </summary>
    public async Task<TransitionRecommendations> RecommendTransitionsAsync(
        List<Scene> scenes,
        TransitionAnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing transitions for {SceneCount} scenes", scenes.Count);

        if (scenes == null || scenes.Count == 0)
        {
            throw new ArgumentException("Scenes collection cannot be empty", nameof(scenes));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var recommendations = new List<TransitionSuggestion>();
        
        // Analyze transitions between consecutive scenes
        for (int i = 0; i < scenes.Count - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var currentScene = scenes[i];
            var nextScene = scenes[i + 1];
            
            var suggestion = await AnalyzeSceneTransitionAsync(
                currentScene, 
                nextScene, 
                i, 
                options,
                cancellationToken);
            
            recommendations.Add(suggestion);
        }

        var overallSummary = GenerateOverallSummary(recommendations);

        await Task.CompletedTask;
        return new TransitionRecommendations(
            Suggestions: recommendations,
            Summary: overallSummary,
            TotalTransitions: recommendations.Count
        );
    }

    private async Task<TransitionSuggestion> AnalyzeSceneTransitionAsync(
        Scene currentScene,
        Scene nextScene,
        int transitionIndex,
        TransitionAnalysisOptions options,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();

        // Analyze content similarity and mood
        var contentSimilarity = AnalyzeContentSimilarity(currentScene, nextScene);
        var moodShift = AnalyzeMoodShift(currentScene, nextScene);
        var paceChange = AnalyzePaceChange(currentScene, nextScene);

        // Recommend transition type based on analysis
        var transitionType = DetermineOptimalTransition(
            contentSimilarity, 
            moodShift, 
            paceChange);
        
        var duration = CalculateOptimalDuration(transitionType, paceChange);
        var confidence = CalculateConfidence(contentSimilarity, moodShift, paceChange);

        return new TransitionSuggestion(
            TransitionIndex: transitionIndex,
            FromSceneIndex: currentScene.Index,
            ToSceneIndex: nextScene.Index,
            Timestamp: currentScene.Start + currentScene.Duration,
            RecommendedType: transitionType,
            Duration: duration,
            Confidence: confidence,
            Reasoning: GenerateReasoning(contentSimilarity, moodShift, paceChange, transitionType)
        );
    }

    private double AnalyzeContentSimilarity(Scene scene1, Scene scene2)
    {
        // Simple word overlap analysis
        var words1 = scene1.Script.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = scene2.Script.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (words1.Count == 0 || words2.Count == 0)
            return 0.0;

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private double AnalyzeMoodShift(Scene scene1, Scene scene2)
    {
        // Analyze sentiment/mood change based on heading and content
        // Placeholder: use heading similarity as proxy
        var headingSimilarity = scene1.Heading.Equals(scene2.Heading, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        
        // Invert to get shift magnitude (high similarity = low shift)
        return 1.0 - headingSimilarity;
    }

    private double AnalyzePaceChange(Scene scene1, Scene scene2)
    {
        var pace1 = CalculateScenePace(scene1);
        var pace2 = CalculateScenePace(scene2);

        return Math.Abs(pace1 - pace2);
    }

    private double CalculateScenePace(Scene scene)
    {
        var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var wordsPerSecond = wordCount / scene.Duration.TotalSeconds;
        
        // Normalize to 0-1 range (assuming typical range is 1-4 words per second)
        return Math.Clamp(wordsPerSecond / 4.0, 0.0, 1.0);
    }

    private TransitionType DetermineOptimalTransition(
        double contentSimilarity,
        double moodShift,
        double paceChange)
    {
        // High similarity and low mood shift = simple cut or dissolve
        if (contentSimilarity > 0.5 && moodShift < 0.3)
        {
            return TransitionType.Cut;
        }

        // Medium shift = fade or dissolve
        if (moodShift < 0.6 && paceChange < 0.5)
        {
            return TransitionType.Dissolve;
        }

        // High mood shift = fade through black
        if (moodShift > 0.7)
        {
            return TransitionType.FadeToBlack;
        }

        // Significant pace change = wipe or zoom
        if (paceChange > 0.6)
        {
            return TransitionType.Wipe;
        }

        // Default
        return TransitionType.Dissolve;
    }

    private TimeSpan CalculateOptimalDuration(TransitionType type, double paceChange)
    {
        return type switch
        {
            TransitionType.Cut => TimeSpan.Zero,
            TransitionType.Dissolve => TimeSpan.FromMilliseconds(500 + (paceChange * 500)),
            TransitionType.FadeToBlack => TimeSpan.FromMilliseconds(800),
            TransitionType.Wipe => TimeSpan.FromMilliseconds(600),
            TransitionType.Zoom => TimeSpan.FromMilliseconds(700),
            _ => TimeSpan.FromMilliseconds(500)
        };
    }

    private double CalculateConfidence(double contentSimilarity, double moodShift, double paceChange)
    {
        // Higher confidence when factors are clear and consistent
        var clarity = Math.Abs(contentSimilarity - 0.5) + Math.Abs(moodShift - 0.5);
        return Math.Clamp(clarity, 0.0, 1.0);
    }

    private string GenerateReasoning(
        double contentSimilarity,
        double moodShift,
        double paceChange,
        TransitionType transitionType)
    {
        var reasons = new List<string>();

        if (contentSimilarity > 0.5)
            reasons.Add("scenes have similar content");
        else if (contentSimilarity < 0.2)
            reasons.Add("scenes have very different content");

        if (moodShift > 0.6)
            reasons.Add("significant mood change detected");
        
        if (paceChange > 0.5)
            reasons.Add($"pace changes from {(paceChange > 0.5 ? "fast to slow" : "slow to fast")}");

        var reasoningText = reasons.Count > 0 
            ? string.Join(", ", reasons) 
            : "moderate content and mood transition";

        return $"{transitionType} recommended because {reasoningText}.";
    }

    private string GenerateOverallSummary(List<TransitionSuggestion> recommendations)
    {
        var typeCounts = recommendations
            .GroupBy(r => r.RecommendedType)
            .ToDictionary(g => g.Key, g => g.Count());

        var summary = $"Analyzed {recommendations.Count} transitions. ";
        
        foreach (var (type, count) in typeCounts.OrderByDescending(kvp => kvp.Value))
        {
            summary += $"{count} {type}, ";
        }

        return summary.TrimEnd(',', ' ') + ".";
    }
}

/// <summary>
/// Options for transition analysis
/// </summary>
public record TransitionAnalysisOptions(
    bool PreferSubtleTransitions = true,
    double MinimumConfidenceThreshold = 0.3
);

/// <summary>
/// Results of transition analysis
/// </summary>
public record TransitionRecommendations(
    List<TransitionSuggestion> Suggestions,
    string Summary,
    int TotalTransitions
);

/// <summary>
/// Suggestion for a specific transition
/// </summary>
public record TransitionSuggestion(
    int TransitionIndex,
    int FromSceneIndex,
    int ToSceneIndex,
    TimeSpan Timestamp,
    TransitionType RecommendedType,
    TimeSpan Duration,
    double Confidence,
    string Reasoning
);

/// <summary>
/// Type of video transition
/// </summary>
public enum TransitionType
{
    Cut,
    Dissolve,
    FadeToBlack,
    FadeFromBlack,
    Wipe,
    Zoom,
    Slide
}
