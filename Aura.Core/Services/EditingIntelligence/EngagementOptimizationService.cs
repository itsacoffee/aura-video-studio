using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Service for predicting and optimizing viewer engagement
/// </summary>
public class EngagementOptimizationService
{
    private readonly ILogger<EngagementOptimizationService> _logger;

    public EngagementOptimizationService(ILogger<EngagementOptimizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate predicted engagement curve for timeline
    /// </summary>
    public async Task<EngagementCurve> GenerateEngagementCurveAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Generating engagement curve for timeline");
        
        var points = new List<EngagementPoint>();
        var retentionRisks = new List<TimeSpan>();
        var boosterSuggestions = new List<string>();

        // Analyze engagement at key points
        var currentTime = TimeSpan.Zero;
        foreach (var scene in timeline.Scenes)
        {
            // Add engagement points throughout the scene
            var scenePoints = AnalyzeSceneEngagement(scene);
            points.AddRange(scenePoints);

            // Identify retention risks
            var risks = IdentifyRetentionRisks(scene);
            retentionRisks.AddRange(risks);

            currentTime = scene.Start + scene.Duration;
        }

        // Analyze hook and ending
        var hookStrength = AnalyzeHookStrength(timeline);
        var endingImpact = AnalyzeEndingImpact(timeline);

        // Generate booster suggestions
        boosterSuggestions = GenerateBoosterSuggestions(points, retentionRisks, hookStrength);

        var avgEngagement = points.Any() ? points.Average(p => p.PredictedEngagement) : 0.5;

        await Task.CompletedTask;
        return new EngagementCurve(
            Points: points,
            AverageEngagement: avgEngagement,
            RetentionRisks: retentionRisks,
            HookStrength: hookStrength,
            EndingImpact: endingImpact,
            BoosterSuggestions: boosterSuggestions
        );
    }

    private List<EngagementPoint> AnalyzeSceneEngagement(TimelineScene scene)
    {
        var points = new List<EngagementPoint>();
        var sceneDuration = scene.Duration.TotalSeconds;
        var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // Sample engagement at multiple points in the scene
        for (int i = 0; i <= 4; i++)
        {
            var progress = i / 4.0;
            var timestamp = scene.Start + TimeSpan.FromSeconds(sceneDuration * progress);
            var engagement = PredictEngagementAtPoint(scene, progress, wordCount);
            var context = GetContextDescription(scene, progress);

            points.Add(new EngagementPoint(
                Timestamp: timestamp,
                PredictedEngagement: engagement,
                Context: context
            ));
        }

        return points;
    }

    private double PredictEngagementAtPoint(TimelineScene scene, double progress, int wordCount)
    {
        // Base engagement starts high and gradually decreases
        var baseEngagement = 0.85 - (progress * 0.15);

        // Adjust for scene position (first scene gets boost)
        if (scene.Index == 0)
            baseEngagement += 0.1;

        // Adjust for content density
        var wordsPerSecond = wordCount / scene.Duration.TotalSeconds;
        var densityMultiplier = wordsPerSecond switch
        {
            < 1.5 => 0.85,  // Too slow
            < 2.0 => 0.95,
            < 3.0 => 1.0,   // Optimal
            < 4.0 => 0.95,
            _ => 0.8        // Too fast
        };

        // Adjust for visual variety (if assets present)
        var visualMultiplier = scene.VisualAssets?.Count > 0 ? 1.1 : 0.95;

        // Adjust for scene duration (very long scenes lose engagement)
        var durationMultiplier = scene.Duration.TotalSeconds switch
        {
            > 60 => 0.7,
            > 45 => 0.85,
            > 30 => 0.95,
            _ => 1.0
        };

        var engagement = baseEngagement * densityMultiplier * visualMultiplier * durationMultiplier;
        return Math.Clamp(engagement, 0, 1);
    }

    private string GetContextDescription(TimelineScene scene, double progress)
    {
        if (progress < 0.25)
            return $"Scene start: '{scene.Heading}'";
        else if (progress < 0.75)
            return $"Mid-scene: '{scene.Heading}'";
        else
            return $"Scene end: '{scene.Heading}'";
    }

    private List<TimeSpan> IdentifyRetentionRisks(TimelineScene scene)
    {
        var risks = new List<TimeSpan>();

        // Long scenes are retention risks
        if (scene.Duration.TotalSeconds > 45)
        {
            risks.Add(scene.Start + TimeSpan.FromSeconds(30));
        }

        // Scenes without visual variety
        if (scene.VisualAssets == null || scene.VisualAssets.Count == 0)
        {
            risks.Add(scene.Start + scene.Duration / 2);
        }

        // Slow pacing scenes
        var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var wordsPerSecond = wordCount / scene.Duration.TotalSeconds;
        if (wordsPerSecond < 1.5)
        {
            risks.Add(scene.Start);
        }

        return risks;
    }

    private double AnalyzeHookStrength(EditableTimeline timeline)
    {
        if (!timeline.Scenes.Any())
            return 0;

        var firstScene = timeline.Scenes[0];
        var hookStrength = 0.5; // Base score

        // Check for engaging opening words
        var openingWords = firstScene.Script.ToLower().Split(' ').Take(10).ToArray();
        var hookWords = new[] { "imagine", "what if", "discover", "learn", "today", "you", "welcome" };
        
        if (openingWords.Any(w => hookWords.Contains(w.Trim(',', '.', '!', '?'))))
            hookStrength += 0.2;

        // Check for questions (engaging)
        if (firstScene.Script.Contains('?'))
            hookStrength += 0.15;

        // Check for visual variety in first scene
        if (firstScene.VisualAssets?.Count > 0)
            hookStrength += 0.15;

        // Short, punchy opening is better
        if (firstScene.Duration.TotalSeconds < 10)
            hookStrength += 0.1;

        return Math.Clamp(hookStrength, 0, 1);
    }

    private double AnalyzeEndingImpact(EditableTimeline timeline)
    {
        if (!timeline.Scenes.Any())
            return 0;

        var lastScene = timeline.Scenes[^1];
        var impact = 0.5; // Base score

        // Check for call-to-action
        var ctaWords = new[] { "subscribe", "like", "comment", "share", "follow", "visit", "learn more" };
        if (ctaWords.Any(w => lastScene.Script.ToLower().Contains(w)))
            impact += 0.2;

        // Check for summary/conclusion words
        var conclusionWords = new[] { "conclusion", "summary", "remember", "finally", "in summary" };
        if (conclusionWords.Any(w => lastScene.Script.ToLower().Contains(w)))
            impact += 0.15;

        // Strong ending statement (exclamation)
        if (lastScene.Script.Contains('!'))
            impact += 0.15;

        return Math.Clamp(impact, 0, 1);
    }

    private List<string> GenerateBoosterSuggestions(
        List<EngagementPoint> points,
        List<TimeSpan> risks,
        double hookStrength)
    {
        var suggestions = new List<string>();

        // Hook suggestions
        if (hookStrength < 0.6)
        {
            suggestions.Add("Strengthen opening hook with a question or intriguing statement");
            suggestions.Add("Add engaging visual in the first 3 seconds");
        }

        // Engagement dips
        var lowPoints = points.Where(p => p.PredictedEngagement < 0.6).ToList();
        if (lowPoints.Any())
        {
            suggestions.Add($"Add visual variety or pacing change at {lowPoints.Count} low-engagement points");
        }

        // Retention risks
        if (risks.Count > 2)
        {
            suggestions.Add($"Address {risks.Count} retention risk points with pattern interrupts (zoom, cut, visual change)");
        }

        // General suggestions
        var avgEngagement = points.Average(p => p.PredictedEngagement);
        if (avgEngagement < 0.7)
        {
            suggestions.Add("Consider shortening scenes or adding more visual transitions");
            suggestions.Add("Increase content density to maintain viewer attention");
        }

        if (!suggestions.Any())
        {
            suggestions.Add("Engagement looks strong! Consider minor pacing adjustments only.");
        }

        return suggestions;
    }

    /// <summary>
    /// Detect viewer fatigue points
    /// </summary>
    public async Task<IReadOnlyList<(TimeSpan Timestamp, string Remedy)>> DetectFatiguePointsAsync(
        EditableTimeline timeline)
    {
        _logger.LogInformation("Detecting viewer fatigue points");
        var fatiguePoints = new List<(TimeSpan, string)>();

        var cumulativeDuration = TimeSpan.Zero;
        foreach (var scene in timeline.Scenes)
        {
            cumulativeDuration += scene.Duration;

            // Fatigue typically sets in after 30-45 seconds without change
            if (scene.Duration.TotalSeconds > 30)
            {
                fatiguePoints.Add((
                    scene.Start + TimeSpan.FromSeconds(30),
                    "Add B-roll, zoom, or visual change to maintain attention"
                ));
            }

            // Overall video fatigue points
            if (cumulativeDuration.TotalMinutes >= 5 && cumulativeDuration.TotalMinutes % 5 < 0.5)
            {
                fatiguePoints.Add((
                    cumulativeDuration,
                    "Major engagement checkpoint - consider dramatic pacing shift or visual surprise"
                ));
            }
        }

        await Task.CompletedTask;
        return fatiguePoints;
    }
}
