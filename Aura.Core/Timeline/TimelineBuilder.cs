using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;

namespace Aura.Core.Timeline;

/// <summary>
/// Builds and manages the video timeline, including scene timings and asset placement.
/// </summary>
public class TimelineBuilder
{
    /// <summary>
    /// Creates a timeline from scenes and narration timing.
    /// </summary>
    public Core.Providers.Timeline BuildTimeline(
        IReadOnlyList<Scene> scenes,
        string narrationPath,
        string? musicPath = null,
        string? subtitlesPath = null)
    {
        var sceneAssets = new Dictionary<int, IReadOnlyList<Asset>>();
        
        // Initialize empty asset collections for each scene
        foreach (var scene in scenes)
        {
            sceneAssets[scene.Index] = new List<Asset>();
        }

        return new Core.Providers.Timeline(
            Scenes: scenes,
            SceneAssets: sceneAssets,
            NarrationPath: narrationPath,
            MusicPath: musicPath ?? string.Empty,
            SubtitlesPath: subtitlesPath
        );
    }

    /// <summary>
    /// Adds assets to a specific scene in the timeline.
    /// </summary>
    public Core.Providers.Timeline AddSceneAssets(
        Core.Providers.Timeline timeline,
        int sceneIndex,
        IReadOnlyList<Asset> assets)
    {
        var updatedAssets = new Dictionary<int, IReadOnlyList<Asset>>(timeline.SceneAssets);
        updatedAssets[sceneIndex] = assets;

        return new Core.Providers.Timeline(
            Scenes: timeline.Scenes,
            SceneAssets: updatedAssets,
            NarrationPath: timeline.NarrationPath,
            MusicPath: timeline.MusicPath,
            SubtitlesPath: timeline.SubtitlesPath
        );
    }

    /// <summary>
    /// Calculates scene timings based on word counts and pacing.
    /// </summary>
    public IReadOnlyList<Scene> CalculateSceneTimings(
        IReadOnlyList<Scene> scenes,
        TimeSpan totalDuration,
        Pacing pacing)
    {
        if (scenes.Count == 0)
            return scenes;

        // Calculate total words across all scenes
        int totalWords = scenes.Sum(s => CountWords(s.Script));
        if (totalWords == 0)
            return scenes;

        // Calculate words per minute based on pacing
        int wpm = pacing switch
        {
            Pacing.Chill => 130,
            Pacing.Conversational => 160,
            Pacing.Fast => 190,
            _ => 160
        };

        // Create new scene list with updated timings
        var updatedScenes = new List<Scene>();
        TimeSpan currentStart = TimeSpan.Zero;

        foreach (var scene in scenes)
        {
            int sceneWords = CountWords(scene.Script);
            double proportion = (double)sceneWords / totalWords;
            TimeSpan sceneDuration = TimeSpan.FromSeconds(totalDuration.TotalSeconds * proportion);

            // Add scene pause/transition time
            TimeSpan transitionTime = TimeSpan.FromSeconds(0.5);
            
            var updatedScene = scene with
            {
                Start = currentStart,
                Duration = sceneDuration
            };

            updatedScenes.Add(updatedScene);
            currentStart += sceneDuration + transitionTime;
        }

        return updatedScenes;
    }

    /// <summary>
    /// Generates subtitle timestamps from scene timings.
    /// </summary>
    public string GenerateSubtitles(IReadOnlyList<Scene> scenes, string format = "SRT")
    {
        if (format.ToUpperInvariant() == "SRT")
        {
            return GenerateSRT(scenes);
        }
        else if (format.ToUpperInvariant() == "VTT")
        {
            return GenerateVTT(scenes);
        }
        
        throw new ArgumentException($"Unsupported subtitle format: {format}");
    }

    private string GenerateSRT(IReadOnlyList<Scene> scenes)
    {
        var srt = new System.Text.StringBuilder();
        
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            srt.AppendLine($"{i + 1}");
            srt.AppendLine($"{FormatSrtTime(scene.Start)} --> {FormatSrtTime(scene.Start + scene.Duration)}");
            srt.AppendLine(scene.Script);
            srt.AppendLine();
        }

        return srt.ToString();
    }

    private string GenerateVTT(IReadOnlyList<Scene> scenes)
    {
        var vtt = new System.Text.StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            vtt.AppendLine($"{FormatVttTime(scene.Start)} --> {FormatVttTime(scene.Start + scene.Duration)}");
            vtt.AppendLine(scene.Script);
            vtt.AppendLine();
        }

        return vtt.ToString();
    }

    private string FormatSrtTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
    }

    private string FormatVttTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Applies pacing suggestions to scenes, updating durations based on analysis
    /// </summary>
    public IReadOnlyList<Scene> ApplyPacingSuggestions(
        IReadOnlyList<Scene> scenes,
        Models.PacingModels.PacingAnalysisResult analysisResult,
        Models.Settings.OptimizationLevel optimizationLevel = Models.Settings.OptimizationLevel.Balanced,
        double minimumConfidenceThreshold = 70.0)
    {
        if (scenes == null || scenes.Count == 0)
            return scenes ?? new List<Scene>();

        if (analysisResult == null || analysisResult.TimingSuggestions.Count == 0)
            return scenes;

        var updatedScenes = new List<Scene>();
        TimeSpan currentStart = TimeSpan.Zero;

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var suggestion = analysisResult.TimingSuggestions.FirstOrDefault(s => s.SceneIndex == i);

            TimeSpan newDuration;
            if (suggestion != null && suggestion.Confidence >= minimumConfidenceThreshold)
            {
                // Apply duration based on optimization level
                newDuration = optimizationLevel switch
                {
                    Models.Settings.OptimizationLevel.Conservative =>
                        TimeSpan.FromSeconds(scene.Duration.TotalSeconds * 0.7 + suggestion.OptimalDuration.TotalSeconds * 0.3),
                    Models.Settings.OptimizationLevel.Balanced =>
                        TimeSpan.FromSeconds(scene.Duration.TotalSeconds * 0.4 + suggestion.OptimalDuration.TotalSeconds * 0.6),
                    Models.Settings.OptimizationLevel.Aggressive =>
                        suggestion.OptimalDuration,
                    _ => scene.Duration
                };

                // Enforce constraints (3-120 seconds per scene)
                newDuration = TimeSpan.FromSeconds(Math.Clamp(newDuration.TotalSeconds, 3.0, 120.0));
            }
            else
            {
                // No suggestion or low confidence, keep current duration
                newDuration = scene.Duration;
            }

            var updatedScene = scene with
            {
                Start = currentStart,
                Duration = newDuration
            };

            updatedScenes.Add(updatedScene);
            currentStart += newDuration;
        }

        return updatedScenes;
    }
}
