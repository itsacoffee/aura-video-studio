using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Narrative;

/// <summary>
/// Service for optimizing scene ordering based on narrative coherence analysis
/// Suggests reordering that maintains duration constraints while improving flow
/// </summary>
public class SceneCoherenceOptimizer
{
    private readonly ILogger<SceneCoherenceOptimizer> _logger;
    private const double MaxDurationChangePercent = 5.0;
    private const double MinCoherenceGain = 15.0;

    public SceneCoherenceOptimizer(ILogger<SceneCoherenceOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates scene reordering suggestions to improve narrative coherence
    /// </summary>
    public async Task<SceneReorderingSuggestion?> OptimizeSceneOrderAsync(
        IReadOnlyList<Scene> scenes,
        NarrativeAnalysisResult analysisResult,
        TimeSpan targetDuration,
        CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Analyzing scene order optimization for {SceneCount} scenes", scenes.Count);

        if (scenes.Count < 3)
        {
            _logger.LogInformation("Too few scenes for reordering optimization");
            return null;
        }

        var originalCoherence = analysisResult.OverallCoherenceScore;
        if (originalCoherence >= 85)
        {
            _logger.LogInformation(
                "Current coherence {Score:F2} is already high, no reordering needed",
                originalCoherence);
            return null;
        }

        var originalDuration = CalculateTotalDuration(scenes);
        var originalOrder = Enumerable.Range(0, scenes.Count).ToList();

        var bestSuggestion = FindBestReordering(
            scenes,
            analysisResult,
            originalOrder,
            originalCoherence,
            originalDuration,
            targetDuration);

        if (bestSuggestion != null)
        {
            _logger.LogInformation(
                "Found reordering with coherence gain {Gain:F2} (from {Original:F2} to {Improved:F2}), duration change {DurationChange:F2}%",
                bestSuggestion.CoherenceGain,
                bestSuggestion.OriginalCoherence,
                bestSuggestion.ImprovedCoherence,
                bestSuggestion.DurationChangePercent);
        }
        else
        {
            _logger.LogInformation("No beneficial reordering found");
        }

        return bestSuggestion;
    }

    /// <summary>
    /// Finds the best scene reordering through heuristic optimization
    /// </summary>
    private SceneReorderingSuggestion? FindBestReordering(
        IReadOnlyList<Scene> scenes,
        NarrativeAnalysisResult analysisResult,
        IReadOnlyList<int> originalOrder,
        double originalCoherence,
        TimeSpan originalDuration,
        TimeSpan targetDuration)
    {
        SceneReorderingSuggestion? bestSuggestion = null;
        double bestGain = 0;

        var criticalIssues = analysisResult.ContinuityIssues
            .Where(i => i.Severity == IssueSeverity.Critical && i.SceneIndex >= 0)
            .ToList();

        if (criticalIssues.Count > 0)
        {
            _logger.LogDebug("Attempting to resolve {Count} critical issues through reordering", criticalIssues.Count);

            foreach (var issue in criticalIssues)
            {
                var targetPair = analysisResult.PairwiseCoherence
                    .FirstOrDefault(p => p.FromSceneIndex == issue.SceneIndex);

                if (targetPair != null)
                {
                    var reordering = TrySwapScenes(
                        scenes,
                        originalOrder,
                        targetPair.FromSceneIndex,
                        targetPair.ToSceneIndex);

                    if (reordering != null)
                    {
                        var suggestion = EvaluateReordering(
                            scenes,
                            analysisResult,
                            reordering,
                            originalOrder,
                            originalCoherence,
                            originalDuration,
                            targetDuration);

                        if (suggestion != null && 
                            suggestion.CoherenceGain >= MinCoherenceGain &&
                            suggestion.CoherenceGain > bestGain)
                        {
                            bestSuggestion = suggestion;
                            bestGain = suggestion.CoherenceGain;
                        }
                    }
                }
            }
        }

        if (bestSuggestion == null)
        {
            _logger.LogDebug("Trying greedy optimization for scene ordering");
            bestSuggestion = TryGreedyOptimization(
                scenes,
                analysisResult,
                originalOrder,
                originalCoherence,
                originalDuration,
                targetDuration);
        }

        return bestSuggestion;
    }

    /// <summary>
    /// Tries swapping adjacent problematic scenes
    /// </summary>
    private List<int>? TrySwapScenes(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<int> order,
        int index1,
        int index2)
    {
        if (Math.Abs(index1 - index2) != 1)
        {
            return null;
        }

        var newOrder = order.ToList();
        (newOrder[index1], newOrder[index2]) = (newOrder[index2], newOrder[index1]);
        return newOrder;
    }

    /// <summary>
    /// Tries greedy optimization by moving scenes with strong connections closer
    /// </summary>
    private SceneReorderingSuggestion? TryGreedyOptimization(
        IReadOnlyList<Scene> scenes,
        NarrativeAnalysisResult analysisResult,
        IReadOnlyList<int> originalOrder,
        double originalCoherence,
        TimeSpan originalDuration,
        TimeSpan targetDuration)
    {
        var weakTransitions = analysisResult.PairwiseCoherence
            .Where(p => p.CoherenceScore < 60)
            .OrderBy(p => p.CoherenceScore)
            .Take(3)
            .ToList();

        foreach (var weakPair in weakTransitions)
        {
            for (int newPos = 0; newPos < scenes.Count; newPos++)
            {
                if (newPos == weakPair.ToSceneIndex || 
                    newPos == weakPair.ToSceneIndex - 1 || 
                    newPos == weakPair.ToSceneIndex + 1)
                {
                    continue;
                }

                var newOrder = originalOrder.ToList();
                var sceneToMove = newOrder[weakPair.ToSceneIndex];
                newOrder.RemoveAt(weakPair.ToSceneIndex);
                newOrder.Insert(newPos, sceneToMove);

                var suggestion = EvaluateReordering(
                    scenes,
                    analysisResult,
                    newOrder,
                    originalOrder,
                    originalCoherence,
                    originalDuration,
                    targetDuration);

                if (suggestion != null && suggestion.CoherenceGain >= MinCoherenceGain)
                {
                    return suggestion;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Evaluates a candidate reordering
    /// </summary>
    private SceneReorderingSuggestion? EvaluateReordering(
        IReadOnlyList<Scene> scenes,
        NarrativeAnalysisResult analysisResult,
        IReadOnlyList<int> newOrder,
        IReadOnlyList<int> originalOrder,
        double originalCoherence,
        TimeSpan originalDuration,
        TimeSpan targetDuration)
    {
        var reorderedScenes = newOrder.Select(i => scenes[i]).ToList();
        var newDuration = CalculateTotalDuration(reorderedScenes);

        var durationChangePercent = Math.Abs(
            (newDuration.TotalSeconds - targetDuration.TotalSeconds) / 
            targetDuration.TotalSeconds * 100);

        if (durationChangePercent > MaxDurationChangePercent)
        {
            _logger.LogDebug(
                "Reordering rejected: duration change {Change:F2}% exceeds limit {Limit:F2}%",
                durationChangePercent,
                MaxDurationChangePercent);
            return null;
        }

        var improvedCoherence = EstimateImprovedCoherence(
            reorderedScenes,
            analysisResult.PairwiseCoherence,
            newOrder);

        var coherenceGain = improvedCoherence - originalCoherence;

        if (coherenceGain < MinCoherenceGain)
        {
            _logger.LogDebug(
                "Reordering rejected: coherence gain {Gain:F2} below minimum {Min:F2}",
                coherenceGain,
                MinCoherenceGain);
            return null;
        }

        return new SceneReorderingSuggestion
        {
            OriginalOrder = originalOrder,
            SuggestedOrder = newOrder,
            OriginalCoherence = originalCoherence,
            ImprovedCoherence = improvedCoherence,
            CoherenceGain = coherenceGain,
            OriginalDuration = originalDuration,
            AdjustedDuration = newDuration,
            DurationChangePercent = durationChangePercent,
            Rationale = GenerateReorderingRationale(newOrder, originalOrder, coherenceGain),
            MaintainsDurationConstraint = durationChangePercent <= MaxDurationChangePercent
        };
    }

    /// <summary>
    /// Estimates improved coherence after reordering
    /// </summary>
    private double EstimateImprovedCoherence(
        IReadOnlyList<Scene> reorderedScenes,
        IReadOnlyList<ScenePairCoherence> originalCoherence,
        IReadOnlyList<int> newOrder)
    {
        var scores = new List<double>();

        for (int i = 0; i < reorderedScenes.Count - 1; i++)
        {
            var fromOriginalIndex = newOrder[i];
            var toOriginalIndex = newOrder[i + 1];

            var existingCoherence = originalCoherence.FirstOrDefault(c =>
                c.FromSceneIndex == fromOriginalIndex && c.ToSceneIndex == toOriginalIndex);

            if (existingCoherence != null)
            {
                scores.Add(existingCoherence.CoherenceScore);
            }
            else
            {
                var fromScene = reorderedScenes[i];
                var toScene = reorderedScenes[i + 1];
                scores.Add(EstimateCoherence(fromScene, toScene));
            }
        }

        return scores.Count > 0 ? scores.Average() : 0;
    }

    /// <summary>
    /// Estimates coherence between two scenes based on word overlap
    /// </summary>
    private double EstimateCoherence(Scene fromScene, Scene toScene)
    {
        var fromWords = GetSignificantWords(fromScene.Script);
        var toWords = GetSignificantWords(toScene.Script);
        var commonWords = fromWords.Intersect(toWords, StringComparer.OrdinalIgnoreCase).ToList();
        var overlapRatio = commonWords.Count / (double)Math.Max(fromWords.Count, 1);
        return Math.Clamp(overlapRatio * 100, 0, 100);
    }

    /// <summary>
    /// Generates rationale for reordering suggestion
    /// </summary>
    private string GenerateReorderingRationale(
        IReadOnlyList<int> newOrder,
        IReadOnlyList<int> originalOrder,
        double coherenceGain)
    {
        var changes = new List<string>();
        for (int i = 0; i < newOrder.Count; i++)
        {
            if (newOrder[i] != originalOrder[i])
            {
                changes.Add($"Scene {originalOrder[i]} moved to position {i}");
            }
        }

        return $"Reordering improves coherence by {coherenceGain:F2} points. Changes: {string.Join(", ", changes)}";
    }

    /// <summary>
    /// Calculates total duration of scenes
    /// </summary>
    private TimeSpan CalculateTotalDuration(IReadOnlyList<Scene> scenes)
    {
        return TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds));
    }

    /// <summary>
    /// Extracts significant words from text
    /// </summary>
    private List<string> GetSignificantWords(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
            "this", "that", "these", "those", "we", "you", "they", "it"
        };

        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .ToList();
    }
}
