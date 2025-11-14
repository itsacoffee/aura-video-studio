using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Optimizes video pacing by analyzing scene durations and speaking rates
/// </summary>
public class PacingOptimizer
{
    private readonly ILogger<PacingOptimizer> _logger;
    private const double IdealWordsPerSecond = 2.5;
    private const double MinWordsPerSecond = 2.0;
    private const double MaxWordsPerSecond = 3.0;
    private const double DurationTolerancePercentage = 20.0;

    public PacingOptimizer(ILogger<PacingOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Optimizes timing for a timeline with scene durations and narration
    /// </summary>
    public async Task<PacingOptimization> OptimizeTimingAsync(
        Aura.Core.Providers.Timeline timeline,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Optimizing pacing for {SceneCount} scenes", timeline.Scenes.Count);

        var suggestions = new List<ScenePacingSuggestion>();

        // Analyze each scene
        for (int i = 0; i < timeline.Scenes.Count; i++)
        {
            var scene = timeline.Scenes[i];
            var suggestion = await AnalyzeScenePacingAsync(scene, i, timeline.Scenes.Count, ct).ConfigureAwait(false);
            
            if (suggestion != null)
            {
                suggestions.Add(suggestion);
            }
        }

        // Generate overall assessment
        var assessment = GenerateOverallAssessment(suggestions, timeline.Scenes.Count);

        _logger.LogInformation("Pacing optimization complete. Generated {Count} suggestions", suggestions.Count);

        return new PacingOptimization(
            Suggestions: suggestions,
            OverallAssessment: assessment
        );
    }

    private async Task<ScenePacingSuggestion?> AnalyzeScenePacingAsync(
        Scene scene,
        int sceneIndex,
        int totalScenes,
        CancellationToken ct)
    {
        await Task.CompletedTask;
        // Calculate words in scene
        var words = Regex.Split(scene.Script, @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToArray();
        var wordCount = words.Length;

        if (wordCount == 0)
        {
            _logger.LogWarning("Scene {Index} has no words in script", sceneIndex);
            return null;
        }

        // Calculate current speaking rate
        var currentDurationSeconds = scene.Duration.TotalSeconds;
        if (currentDurationSeconds <= 0)
        {
            _logger.LogWarning("Scene {Index} has invalid duration", sceneIndex);
            return null;
        }

        var currentWordsPerSecond = wordCount / currentDurationSeconds;

        // Determine ideal pacing based on scene position
        var idealPacing = DetermineIdealPacing(sceneIndex, totalScenes);

        // Calculate suggested duration
        var suggestedDurationSeconds = wordCount / idealPacing;
        var suggestedDuration = TimeSpan.FromSeconds(suggestedDurationSeconds);

        // Calculate difference percentage
        var differencePercentage = Math.Abs(currentDurationSeconds - suggestedDurationSeconds) / currentDurationSeconds * 100;

        // Determine if adjustment is needed
        if (differencePercentage < DurationTolerancePercentage && 
            currentWordsPerSecond >= MinWordsPerSecond && 
            currentWordsPerSecond <= MaxWordsPerSecond)
        {
            // Pacing is acceptable
            return null;
        }

        // Determine priority and reasoning
        var (priority, reasoning) = DeterminePriorityAndReasoning(
            currentWordsPerSecond, 
            idealPacing, 
            differencePercentage,
            sceneIndex,
            totalScenes
        );

        return new ScenePacingSuggestion(
            SceneIndex: sceneIndex,
            CurrentDuration: scene.Duration,
            SuggestedDuration: suggestedDuration,
            Reasoning: reasoning,
            Priority: priority
        );
    }

    private double DetermineIdealPacing(int sceneIndex, int totalScenes)
    {
        // Opening scene should be faster-paced to hook viewer
        if (sceneIndex == 0)
        {
            return 2.8; // Slightly faster
        }

        // Conclusion should pick up pace
        if (sceneIndex == totalScenes - 1)
        {
            return 2.7; // Moderately faster
        }

        // Middle scenes use comfortable pace
        return IdealWordsPerSecond;
    }

    private (PacingPriority priority, string reasoning) DeterminePriorityAndReasoning(
        double currentWps,
        double idealWps,
        double differencePercentage,
        int sceneIndex,
        int totalScenes)
    {
        var reasoning = "";
        var priority = PacingPriority.Optional;

        if (currentWps > MaxWordsPerSecond)
        {
            reasoning = $"Scene is too fast-paced ({currentWps:F2} words/sec). Viewers may struggle to keep up. ";
            priority = PacingPriority.Critical;
        }
        else if (currentWps < MinWordsPerSecond)
        {
            reasoning = $"Scene is too slow-paced ({currentWps:F2} words/sec). May feel dragging with dead air. ";
            priority = PacingPriority.Recommended;
        }
        else if (differencePercentage > 30)
        {
            reasoning = $"Scene duration differs significantly from ideal pacing ({differencePercentage:F1}% difference). ";
            priority = PacingPriority.Recommended;
        }
        else
        {
            reasoning = $"Minor pacing adjustment recommended ({differencePercentage:F1}% difference). ";
            priority = PacingPriority.Optional;
        }

        // Add context-specific reasoning
        if (sceneIndex == 0)
        {
            reasoning += "Opening scene should be engaging and fast-paced to hook viewers.";
        }
        else if (sceneIndex == totalScenes - 1)
        {
            reasoning += "Conclusion should have strong pacing for memorable finish.";
        }
        else
        {
            reasoning += $"Target comfortable pacing of {idealWps:F1} words/sec.";
        }

        return (priority, reasoning);
    }

    private string GenerateOverallAssessment(List<ScenePacingSuggestion> suggestions, int totalScenes)
    {
        if (suggestions.Count == 0)
        {
            return $"Pacing is well-balanced across all {totalScenes} scenes. No adjustments needed.";
        }

        var critical = suggestions.Count(s => s.Priority == PacingPriority.Critical);
        var recommended = suggestions.Count(s => s.Priority == PacingPriority.Recommended);
        var optional = suggestions.Count(s => s.Priority == PacingPriority.Optional);

        var parts = new List<string>();

        if (critical > 0)
        {
            parts.Add($"{critical} critical pacing issue(s) requiring immediate attention");
        }
        if (recommended > 0)
        {
            parts.Add($"{recommended} recommended adjustment(s) to improve flow");
        }
        if (optional > 0)
        {
            parts.Add($"{optional} optional enhancement(s) available");
        }

        var summary = string.Join(", ", parts) + ".";
        
        return $"Analyzed {totalScenes} scenes and found {summary} " +
               $"Adjusting these scenes will improve overall video watchability and viewer retention.";
    }
}
