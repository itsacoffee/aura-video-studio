using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Manages visual continuity across scenes to maintain consistent character appearance,
/// locations, color grading, and time progression
/// </summary>
public class VisualContinuityEngine
{
    private readonly ILogger<VisualContinuityEngine> _logger;

    public VisualContinuityEngine(ILogger<VisualContinuityEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze and create continuity tracking for a scene
    /// </summary>
    public VisualContinuity? AnalyzeContinuity(
        Scene currentScene,
        Scene? previousScene,
        VisualPrompt? previousPrompt,
        List<string>? llmContinuityElements)
    {
        if (previousScene == null || previousPrompt == null)
        {
            return null;
        }

        var characterAppearance = ExtractCharacterElements(currentScene, previousScene, llmContinuityElements);
        var locationDetails = ExtractLocationElements(currentScene, previousScene, llmContinuityElements);
        var colorGrading = previousPrompt.ColorPalette.ToList();
        var timeProgression = DetermineTimeProgression(previousPrompt.Lighting.TimeOfDay, currentScene.Index);

        var similarityScore = CalculateSimilarityScore(
            currentScene,
            previousScene,
            characterAppearance,
            locationDetails,
            llmContinuityElements);

        _logger.LogDebug("Scene {SceneIndex} continuity with previous: {Similarity}%",
            currentScene.Index, similarityScore);

        return new VisualContinuity
        {
            CharacterAppearance = characterAppearance,
            LocationDetails = locationDetails,
            ColorGrading = colorGrading,
            TimeProgression = timeProgression,
            SimilarityScore = similarityScore
        };
    }

    private static List<string> ExtractCharacterElements(
        Scene currentScene,
        Scene previousScene,
        List<string>? llmElements)
    {
        var elements = new List<string>();

        if (llmElements != null && llmElements.Count != 0)
        {
            elements.AddRange(llmElements.Where(e =>
                e.ToLowerInvariant().Contains("character") ||
                e.ToLowerInvariant().Contains("person") ||
                e.ToLowerInvariant().Contains("appearance")));
        }

        var currentWords = currentScene.Script.ToLowerInvariant().Split(' ');
        var previousWords = previousScene.Script.ToLowerInvariant().Split(' ');
        var commonWords = currentWords.Intersect(previousWords)
            .Where(w => w.Length > 4 && !IsStopWord(w))
            .Take(3);

        elements.AddRange(commonWords.Select(w => $"consistent {w}"));

        return elements.Distinct().ToList();
    }

    private static List<string> ExtractLocationElements(
        Scene currentScene,
        Scene previousScene,
        List<string>? llmElements)
    {
        var elements = new List<string>();

        if (llmElements != null && llmElements.Count != 0)
        {
            elements.AddRange(llmElements.Where(e =>
                e.ToLowerInvariant().Contains("location") ||
                e.ToLowerInvariant().Contains("setting") ||
                e.ToLowerInvariant().Contains("environment") ||
                e.ToLowerInvariant().Contains("background")));
        }

        var locationKeywords = new[] { "office", "home", "street", "park", "building", "room", "outdoor", "indoor" };
        var currentLower = currentScene.Script.ToLowerInvariant();
        var previousLower = previousScene.Script.ToLowerInvariant();

        foreach (var keyword in locationKeywords)
        {
            if (currentLower.Contains(keyword) && previousLower.Contains(keyword))
            {
                elements.Add($"same {keyword}");
            }
        }

        return elements.Distinct().ToList();
    }

    private static string DetermineTimeProgression(string previousTimeOfDay, int sceneIndex)
    {
        if (sceneIndex == 1)
        {
            return previousTimeOfDay;
        }

        return previousTimeOfDay switch
        {
            "morning" when sceneIndex % 3 == 0 => "noon",
            "noon" when sceneIndex % 3 == 0 => "afternoon",
            "afternoon" when sceneIndex % 3 == 0 => "evening",
            "evening" when sceneIndex % 3 == 0 => "night",
            "golden hour" => "golden hour",
            _ => previousTimeOfDay
        };
    }

    private static double CalculateSimilarityScore(
        Scene currentScene,
        Scene previousScene,
        List<string> characterElements,
        List<string> locationElements,
        List<string>? llmElements)
    {
        double score = 0.0;

        var currentWords = new HashSet<string>(
            currentScene.Script.ToLowerInvariant()
                .Split(' ')
                .Where(w => w.Length > 3 && !IsStopWord(w)));

        var previousWords = new HashSet<string>(
            previousScene.Script.ToLowerInvariant()
                .Split(' ')
                .Where(w => w.Length > 3 && !IsStopWord(w)));

        if (currentWords.Count > 0 && previousWords.Count > 0)
        {
            var commonWords = currentWords.Intersect(previousWords).Count();
            var totalWords = currentWords.Union(previousWords).Count();
            score += (commonWords / (double)totalWords) * 50.0;
        }

        score += characterElements.Count * 10.0;
        score += locationElements.Count * 10.0;

        if (llmElements != null && llmElements.Count != 0)
        {
            score += 20.0;
        }

        return Math.Min(100.0, score);
    }

    private static bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "and", "but", "for", "with", "from", "this", "that",
            "these", "those", "are", "was", "were", "been", "being",
            "have", "has", "had", "having", "will", "would", "could", "should"
        };

        return stopWords.Contains(word);
    }
}
