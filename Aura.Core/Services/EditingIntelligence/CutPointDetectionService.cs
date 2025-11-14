using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Service for detecting optimal cut points in timeline
/// </summary>
public class CutPointDetectionService
{
    private readonly ILogger<CutPointDetectionService> _logger;

    public CutPointDetectionService(ILogger<CutPointDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze timeline and detect optimal cut points
    /// </summary>
    public async Task<IReadOnlyList<CutPoint>> DetectCutPointsAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Analyzing timeline for cut points");
        var cutPoints = new List<CutPoint>();

        foreach (var scene in timeline.Scenes)
        {
            // Detect sentence boundaries
            var sentenceCuts = await DetectSentenceBoundariesAsync(scene).ConfigureAwait(false);
            cutPoints.AddRange(sentenceCuts);

            // Detect natural pauses
            var pauseCuts = DetectNaturalPauses(scene);
            cutPoints.AddRange(pauseCuts);

            // Detect filler content
            var fillerCuts = DetectFillerContent(scene);
            cutPoints.AddRange(fillerCuts);

            // Detect breath points
            var breathCuts = DetectBreathPoints(scene);
            cutPoints.AddRange(breathCuts);
        }

        // Sort by timestamp and remove duplicates
        var uniqueCuts = cutPoints
            .GroupBy(c => c.Timestamp)
            .Select(g => g.OrderByDescending(c => c.Confidence).First())
            .OrderBy(c => c.Timestamp)
            .ToList();

        _logger.LogInformation("Detected {Count} unique cut points", uniqueCuts.Count);
        return uniqueCuts;
    }

    private async Task<List<CutPoint>> DetectSentenceBoundariesAsync(TimelineScene scene)
    {
        var cutPoints = new List<CutPoint>();
        
        // Split script into sentences
        var sentences = scene.Script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length <= 1)
            return cutPoints;

        // Estimate timing based on word count
        var words = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wordsPerSecond = 2.5; // Average speaking rate
        var sceneDuration = scene.Duration.TotalSeconds;
        var actualWordsPerSecond = words.Length / sceneDuration;

        var currentTime = scene.Start;
        var wordIndex = 0;

        foreach (var sentence in sentences.Take(sentences.Length - 1)) // Skip last sentence
        {
            var sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            wordIndex += sentenceWords;
            
            // Calculate timestamp based on word position
            var progress = (double)wordIndex / words.Length;
            var timestamp = scene.Start + TimeSpan.FromSeconds(sceneDuration * progress);

            cutPoints.Add(new CutPoint(
                Timestamp: timestamp,
                Type: CutPointType.SentenceBoundary,
                Confidence: 0.85,
                Reasoning: "Natural sentence boundary - ideal for clean cuts",
                DurationToRemove: null
            ));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return cutPoints;
    }

    private List<CutPoint> DetectNaturalPauses(TimelineScene scene)
    {
        var cutPoints = new List<CutPoint>();

        // Look for common pause indicators in script
        var pauseIndicators = new[] { "...", "—", " - ", ", " };
        var words = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sceneDuration = scene.Duration.TotalSeconds;

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            foreach (var indicator in pauseIndicators)
            {
                if (word.Contains(indicator))
                {
                    var progress = (double)i / words.Length;
                    var timestamp = scene.Start + TimeSpan.FromSeconds(sceneDuration * progress);

                    cutPoints.Add(new CutPoint(
                        Timestamp: timestamp,
                        Type: CutPointType.NaturalPause,
                        Confidence: 0.7,
                        Reasoning: $"Natural pause detected at '{indicator}'",
                        DurationToRemove: TimeSpan.FromSeconds(0.3)
                    ));
                    break;
                }
            }
        }

        return cutPoints;
    }

    private List<CutPoint> DetectFillerContent(TimelineScene scene)
    {
        var cutPoints = new List<CutPoint>();

        // Common filler words and phrases
        var fillerWords = new[] { "um", "uh", "er", "ah", "like", "you know", "basically", "actually" };
        var words = scene.Script.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sceneDuration = scene.Duration.TotalSeconds;

        for (int i = 0; i < words.Length; i++)
        {
            if (fillerWords.Contains(words[i].Trim(',', '.', '!', '?')))
            {
                var progress = (double)i / words.Length;
                var timestamp = scene.Start + TimeSpan.FromSeconds(sceneDuration * progress);

                cutPoints.Add(new CutPoint(
                    Timestamp: timestamp,
                    Type: CutPointType.FillerRemoval,
                    Confidence: 0.75,
                    Reasoning: $"Filler word '{words[i]}' can be removed",
                    DurationToRemove: TimeSpan.FromSeconds(0.5)
                ));
            }
        }

        return cutPoints;
    }

    private List<CutPoint> DetectBreathPoints(TimelineScene scene)
    {
        var cutPoints = new List<CutPoint>();

        // Detect potential breath points based on sentence length
        var sentences = scene.Script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var words = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sceneDuration = scene.Duration.TotalSeconds;

        var wordIndex = 0;
        foreach (var sentence in sentences)
        {
            var sentenceWords = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            wordIndex += sentenceWords.Length;

            // Long sentences need breath points
            if (sentenceWords.Length > 20)
            {
                var progress = (double)wordIndex / words.Length;
                var timestamp = scene.Start + TimeSpan.FromSeconds(sceneDuration * progress);

                cutPoints.Add(new CutPoint(
                    Timestamp: timestamp,
                    Type: CutPointType.BreathPoint,
                    Confidence: 0.65,
                    Reasoning: "Potential breath point after long sentence",
                    DurationToRemove: null
                ));
            }
        }

        return cutPoints;
    }

    /// <summary>
    /// Detect and eliminate awkward pauses
    /// </summary>
    public async Task<IReadOnlyList<CutPoint>> DetectAwkwardPausesAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Detecting awkward pauses");
        var awkwardPauses = new List<CutPoint>();

        // Look for unusually long gaps between scenes
        for (int i = 0; i < timeline.Scenes.Count - 1; i++)
        {
            var currentScene = timeline.Scenes[i];
            var nextScene = timeline.Scenes[i + 1];

            var gap = nextScene.Start - (currentScene.Start + currentScene.Duration);
            if (gap > TimeSpan.FromSeconds(1))
            {
                awkwardPauses.Add(new CutPoint(
                    Timestamp: currentScene.Start + currentScene.Duration,
                    Type: CutPointType.FillerRemoval,
                    Confidence: 0.9,
                    Reasoning: $"Awkward pause of {gap.TotalSeconds:F1}s between scenes",
                    DurationToRemove: gap - TimeSpan.FromSeconds(0.2)
                ));
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return awkwardPauses;
    }
}
