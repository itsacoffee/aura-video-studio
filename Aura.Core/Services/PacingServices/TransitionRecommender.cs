using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Service for recommending optimal transitions between video scenes
/// Analyzes content relationships, emotional flow, and platform requirements
/// </summary>
public class TransitionRecommender
{
    private readonly ILogger<TransitionRecommender> _logger;

    public TransitionRecommender(ILogger<TransitionRecommender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes and recommends transitions for all scene pairs in a video
    /// </summary>
    public async Task<IReadOnlyList<TransitionRecommendation>> RecommendTransitionsAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<SceneAnalysisData>? sceneAnalyses = null,
        Brief? brief = null,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();

        if (scenes.Count < 2)
        {
            _logger.LogDebug("Less than 2 scenes, no transitions to recommend");
            return Array.Empty<TransitionRecommendation>();
        }

        _logger.LogInformation("Analyzing transitions for {SceneCount} scenes", scenes.Count);

        var recommendations = new List<TransitionRecommendation>();
        var platformType = GetPlatformType(brief);

        for (int i = 0; i < scenes.Count - 1; i++)
        {
            ct.ThrowIfCancellationRequested();

            var fromScene = scenes[i];
            var toScene = scenes[i + 1];
            var fromAnalysis = sceneAnalyses?.FirstOrDefault(a => a.SceneIndex == i);
            var toAnalysis = sceneAnalyses?.FirstOrDefault(a => a.SceneIndex == i + 1);

            var recommendation = AnalyzeTransition(
                fromScene, toScene, 
                fromAnalysis, toAnalysis,
                platformType);

            recommendations.Add(recommendation);
        }

        _logger.LogInformation("Generated {Count} transition recommendations", recommendations.Count);
        return recommendations;
    }

    private TransitionRecommendation AnalyzeTransition(
        Scene fromScene,
        Scene toScene,
        SceneAnalysisData? fromAnalysis,
        SceneAnalysisData? toAnalysis,
        PlatformType platformType)
    {
        // Analyze content relationship
        var contentRelationship = AnalyzeContentRelationship(fromScene, toScene);
        
        // Calculate emotional intensity change
        var fromIntensity = fromAnalysis?.EmotionalIntensity ?? 50.0;
        var toIntensity = toAnalysis?.EmotionalIntensity ?? 50.0;
        var intensityChange = toIntensity - fromIntensity;

        // Determine recommended transition based on logic from spec
        var (transitionType, duration, reasoning) = DetermineTransition(
            contentRelationship, 
            intensityChange,
            fromScene,
            toScene,
            platformType);

        // Check if transition might be jarring
        var isJarring = IsTransitionJarring(contentRelationship, intensityChange);

        // Suggest alternative if needed
        var alternativeType = isJarring ? SuggestAlternative(transitionType) : null;

        // Platform-specific optimization
        var platformOptimization = GetPlatformOptimization(transitionType, duration, platformType);

        return new TransitionRecommendation
        {
            FromSceneIndex = fromScene.Index,
            ToSceneIndex = toScene.Index,
            RecommendedType = transitionType,
            DurationSeconds = duration,
            ContentRelationship = contentRelationship,
            EmotionalIntensityChange = intensityChange,
            Confidence = CalculateConfidence(fromAnalysis, toAnalysis),
            Reasoning = reasoning,
            IsJarring = isJarring,
            AlternativeType = alternativeType,
            PlatformOptimization = platformOptimization
        };
    }

    private string AnalyzeContentRelationship(Scene fromScene, Scene toScene)
    {
        var fromWords = GetKeyWords(fromScene.Script);
        var toWords = GetKeyWords(toScene.Script);

        // Check for word overlap
        var commonWords = fromWords.Intersect(toWords, StringComparer.OrdinalIgnoreCase).ToList();
        var overlapRatio = commonWords.Count / (double)Math.Max(fromWords.Count, 1);

        // Detect time/location indicators
        var hasTimeChange = HasTimeIndicators(toScene.Script);
        var hasLocationChange = HasLocationIndicators(toScene.Script);

        if (overlapRatio > 0.4)
            return "directly related";
        if (hasTimeChange || hasLocationChange)
            return "time or location change";
        if (overlapRatio > 0.2)
            return "related theme";
        
        return "topic shift";
    }

    private (TransitionType type, double duration, string reasoning) DetermineTransition(
        string relationship,
        double intensityChange,
        Scene fromScene,
        Scene toScene,
        PlatformType platformType)
    {
        // Implementation of transition logic from spec
        
        // For cliffhangers or reveals, recommend abrupt Cut
        if (IsCliffhangerOrReveal(toScene))
        {
            return (TransitionType.Cut, 0.0, "Abrupt cut for cliffhanger/reveal impact");
        }

        // For reflective moments, recommend gentle Dissolve
        if (IsReflectiveMoment(toScene))
        {
            var duration = platformType == PlatformType.TikTok ? 1.0 : 1.5;
            return (TransitionType.Dissolve, duration, "Gentle dissolve for reflective moment");
        }

        // If emotional intensity increases sharply, recommend quick Cut
        if (intensityChange > 20)
        {
            return (TransitionType.Cut, 0.0, "Quick cut for sharp emotional intensity increase");
        }

        // If emotional intensity decreases, recommend slow Fade
        if (intensityChange < -15)
        {
            var duration = platformType == PlatformType.TikTok ? 0.5 : 0.75;
            return (TransitionType.Fade, duration, "Slow fade for emotional intensity decrease");
        }

        // If scenes are directly related in topic, recommend Cut
        if (relationship == "directly related")
        {
            return (TransitionType.Cut, 0.0, "Cut for content continuity");
        }

        // If time or location changes, recommend Fade
        if (relationship == "time or location change")
        {
            var duration = platformType == PlatformType.TikTok ? 0.5 : 0.75;
            return (TransitionType.Fade, duration, "Fade for time/location change");
        }

        // If topic shifts but related theme, recommend Dissolve
        if (relationship == "related theme")
        {
            var duration = platformType == PlatformType.TikTok ? 1.0 : 1.5;
            return (TransitionType.Dissolve, duration, "Dissolve for related topic shift");
        }

        // Default: dissolve for topic shift
        var defaultDuration = platformType == PlatformType.TikTok ? 1.0 : 1.5;
        return (TransitionType.Dissolve, defaultDuration, "Dissolve for topic change");
    }

    private bool IsTransitionJarring(string relationship, double intensityChange)
    {
        // Transition is jarring if there's a major topic shift with sharp emotional change
        return relationship == "topic shift" && Math.Abs(intensityChange) > 30;
    }

    private TransitionType? SuggestAlternative(TransitionType currentType)
    {
        return currentType switch
        {
            TransitionType.Cut => TransitionType.Fade,
            TransitionType.Fade => TransitionType.Dissolve,
            _ => null
        };
    }

    private string? GetPlatformOptimization(TransitionType type, double duration, PlatformType platform)
    {
        return platform switch
        {
            PlatformType.TikTok => "Faster transition for TikTok rapid pacing",
            PlatformType.YouTubeShorts => "Quick transition for Shorts continuous engagement",
            PlatformType.InstagramReels => "Visual-first transition timing",
            _ => null
        };
    }

    private double CalculateConfidence(SceneAnalysisData? fromAnalysis, SceneAnalysisData? toAnalysis)
    {
        var baseConfidence = 70.0;

        if (fromAnalysis?.AnalyzedWithLlm == true)
            baseConfidence += 10.0;
        
        if (toAnalysis?.AnalyzedWithLlm == true)
            baseConfidence += 10.0;

        return Math.Clamp(baseConfidence, 0, 100);
    }

    private List<string> GetKeyWords(string text)
    {
        // Extract meaningful words (exclude common stop words)
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "is", "are", "was", "were", "be", "been"
        };

        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .ToList();
    }

    private bool HasTimeIndicators(string text)
    {
        var timeWords = new[] { "later", "earlier", "tomorrow", "yesterday", "next", "previous", "after", "before", "then", "now" };
        return timeWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasLocationIndicators(string text)
    {
        var locationWords = new[] { "here", "there", "where", "location", "place", "at the", "in the", "moved to", "went to" };
        return locationWords.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCliffhangerOrReveal(Scene scene)
    {
        var cliffhangerWords = new[] { "reveal", "discover", "surprise", "shocking", "unexpected", "but wait", "however" };
        return cliffhangerWords.Any(word => scene.Script.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsReflectiveMoment(Scene scene)
    {
        var reflectiveWords = new[] { "reflect", "consider", "think about", "remember", "imagine", "contemplate", "ponder" };
        return reflectiveWords.Any(word => scene.Script.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private PlatformType GetPlatformType(Brief? brief)
    {
        if (brief == null)
            return PlatformType.YouTube;

        return brief.Aspect switch
        {
            Aspect.Vertical9x16 => PlatformType.TikTok,
            Aspect.Square1x1 => PlatformType.InstagramReels,
            _ => PlatformType.YouTube
        };
    }
}

/// <summary>
/// Platform types for optimization
/// </summary>
public enum PlatformType
{
    YouTube,
    TikTok,
    InstagramReels,
    YouTubeShorts
}
