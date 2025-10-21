using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.AI.Aesthetics.VisualCoherence;

/// <summary>
/// Analyzes and ensures visual coherence across scenes
/// </summary>
public class CoherenceAnalyzer
{
    /// <summary>
    /// Analyzes style consistency across multiple scenes
    /// </summary>
    public Task<VisualCoherenceReport> AnalyzeCoherenceAsync(
        List<SceneVisualContext> scenes,
        CancellationToken cancellationToken = default)
    {
        var report = new VisualCoherenceReport();

        if (scenes.Count < 2)
        {
            report.OverallCoherenceScore = 1.0f;
            report.StyleConsistencyScore = 1.0f;
            report.ColorConsistencyScore = 1.0f;
            report.LightingConsistencyScore = 1.0f;
            return Task.FromResult(report);
        }

        // Analyze style consistency
        report.StyleConsistencyScore = AnalyzeStyleConsistency(scenes);

        // Analyze color consistency
        report.ColorConsistencyScore = AnalyzeColorConsistency(scenes);

        // Analyze lighting consistency
        report.LightingConsistencyScore = AnalyzeLightingConsistency(scenes);

        // Calculate overall score
        report.OverallCoherenceScore = (
            report.StyleConsistencyScore +
            report.ColorConsistencyScore +
            report.LightingConsistencyScore
        ) / 3.0f;

        // Generate inconsistencies and recommendations
        report.Inconsistencies = DetectInconsistencies(scenes, report);
        report.Recommendations = GenerateRecommendations(report);

        return Task.FromResult(report);
    }

    /// <summary>
    /// Analyzes lighting consistency across scene transitions
    /// </summary>
    public Task<float> AnalyzeLightingConsistencyAsync(
        List<SceneVisualContext> scenes,
        CancellationToken cancellationToken = default)
    {
        var score = AnalyzeLightingConsistency(scenes);
        return Task.FromResult(score);
    }

    /// <summary>
    /// Detects visual theme and provides reinforcement suggestions
    /// </summary>
    public Task<string> DetectVisualThemeAsync(
        List<SceneVisualContext> scenes,
        CancellationToken cancellationToken = default)
    {
        if (scenes.Count == 0)
            return Task.FromResult("Unknown");

        // Find most common mood
        var dominantMood = scenes
            .GroupBy(s => s.DominantMood)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        // Find most common time of day
        var dominantTimeOfDay = scenes
            .GroupBy(s => s.TimeOfDay)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        var theme = $"{dominantMood} with {dominantTimeOfDay} lighting";
        return Task.FromResult(theme);
    }

    private float AnalyzeStyleConsistency(List<SceneVisualContext> scenes)
    {
        // Calculate mood variance
        var moodCounts = scenes
            .GroupBy(s => s.DominantMood)
            .ToDictionary(g => g.Key, g => g.Count());

        var dominantMoodCount = moodCounts.Values.Max();
        var moodConsistency = (float)dominantMoodCount / scenes.Count;

        // Calculate tag overlap
        var allTags = scenes.SelectMany(s => s.Tags).Distinct().ToList();
        var tagOverlap = scenes
            .Select(s => s.Tags.Intersect(allTags).Count() / (float)allTags.Count)
            .Average();

        return (moodConsistency + tagOverlap) / 2.0f;
    }

    private float AnalyzeColorConsistency(List<SceneVisualContext> scenes)
    {
        if (scenes.Count < 2)
            return 1.0f;

        // Calculate color histogram similarity between consecutive scenes
        var similarities = new List<float>();

        for (int i = 0; i < scenes.Count - 1; i++)
        {
            var similarity = CalculateColorHistogramSimilarity(
                scenes[i].ColorHistogram,
                scenes[i + 1].ColorHistogram
            );
            similarities.Add(similarity);
        }

        return similarities.Average();
    }

    private float AnalyzeLightingConsistency(List<SceneVisualContext> scenes)
    {
        if (scenes.Count < 2)
            return 1.0f;

        // Analyze time of day transitions
        var inconsistentTransitions = 0;
        
        for (int i = 0; i < scenes.Count - 1; i++)
        {
            if (!IsReasonableTransition(scenes[i].TimeOfDay, scenes[i + 1].TimeOfDay))
            {
                inconsistentTransitions++;
            }
        }

        var consistency = 1.0f - (inconsistentTransitions / (float)(scenes.Count - 1));
        return Math.Max(0, consistency);
    }

    private float CalculateColorHistogramSimilarity(
        Dictionary<string, float> histogram1,
        Dictionary<string, float> histogram2)
    {
        var allColors = histogram1.Keys.Union(histogram2.Keys).ToList();
        
        if (allColors.Count == 0)
            return 1.0f;

        var totalDifference = allColors.Sum(color =>
        {
            var value1 = histogram1.GetValueOrDefault(color, 0);
            var value2 = histogram2.GetValueOrDefault(color, 0);
            return Math.Abs(value1 - value2);
        });

        return 1.0f - (totalDifference / allColors.Count);
    }

    private bool IsReasonableTransition(TimeOfDay from, TimeOfDay to)
    {
        // Allow natural progression of time
        var timeOrder = new[] {
            TimeOfDay.Dawn,
            TimeOfDay.Morning,
            TimeOfDay.Midday,
            TimeOfDay.Afternoon,
            TimeOfDay.Sunset,
            TimeOfDay.Dusk,
            TimeOfDay.Night
        };

        var fromIndex = Array.IndexOf(timeOrder, from);
        var toIndex = Array.IndexOf(timeOrder, to);

        if (fromIndex == -1 || toIndex == -1)
            return true; // Unknown times are always reasonable

        // Allow forward progression or same time
        return toIndex >= fromIndex || toIndex == 0; // or wrap to dawn
    }

    private List<string> DetectInconsistencies(List<SceneVisualContext> scenes, VisualCoherenceReport report)
    {
        var inconsistencies = new List<string>();

        if (report.StyleConsistencyScore < 0.7f)
        {
            inconsistencies.Add("Multiple visual styles detected across scenes");
        }

        if (report.ColorConsistencyScore < 0.6f)
        {
            inconsistencies.Add("Significant color palette variations between scenes");
        }

        if (report.LightingConsistencyScore < 0.7f)
        {
            inconsistencies.Add("Inconsistent lighting/time-of-day across scenes");
        }

        // Check for jarring transitions
        for (int i = 0; i < scenes.Count - 1; i++)
        {
            if (scenes[i].DominantMood != scenes[i + 1].DominantMood)
            {
                var moodChange = $"{scenes[i].DominantMood} to {scenes[i + 1].DominantMood}";
                if (Math.Abs((int)scenes[i].DominantMood - (int)scenes[i + 1].DominantMood) > 2)
                {
                    inconsistencies.Add($"Abrupt mood change between scenes {i + 1} and {i + 2}: {moodChange}");
                }
            }
        }

        return inconsistencies;
    }

    private List<string> GenerateRecommendations(VisualCoherenceReport report)
    {
        var recommendations = new List<string>();

        if (report.StyleConsistencyScore < 0.8f)
        {
            recommendations.Add("Apply consistent visual style across all scenes");
            recommendations.Add("Use style transfer to match dominant visual theme");
        }

        if (report.ColorConsistencyScore < 0.7f)
        {
            recommendations.Add("Apply consistent color grading across scenes");
            recommendations.Add("Use color matching to ensure smooth transitions");
        }

        if (report.LightingConsistencyScore < 0.8f)
        {
            recommendations.Add("Adjust lighting to create logical temporal progression");
            recommendations.Add("Add transition effects between different lighting conditions");
        }

        if (report.OverallCoherenceScore >= 0.85f)
        {
            recommendations.Add("Visual coherence is excellent across all scenes");
        }

        return recommendations;
    }
}
