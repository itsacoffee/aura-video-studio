using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Orchestrates the video generation pipeline from brief to final render.
/// Implements the stage-by-stage workflow: Plan → Script → TTS → Assets → Compose → Render.
/// </summary>
public class VideoOrchestrator
{
    private readonly ILogger<VideoOrchestrator> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly ITtsProvider _ttsProvider;
    private readonly IVideoComposer _videoComposer;

    public VideoOrchestrator(
        ILogger<VideoOrchestrator> logger,
        ILlmProvider llmProvider,
        ITtsProvider ttsProvider,
        IVideoComposer videoComposer)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _ttsProvider = ttsProvider;
        _videoComposer = videoComposer;
    }

    /// <summary>
    /// Generates a complete video from the provided brief and specifications.
    /// </summary>
    public async Task<string> GenerateVideoAsync(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            progress?.Report("Starting video generation pipeline...");

            // Stage 1: Script generation
            progress?.Report("Stage 1/5: Generating script...");
            _logger.LogInformation("Generating script for topic: {Topic}", brief.Topic);
            string script = await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
            _logger.LogInformation("Script generated: {Length} characters", script.Length);

            // Stage 2: Parse script into scenes
            progress?.Report("Stage 2/5: Parsing scenes...");
            var scenes = ParseScriptIntoScenes(script, planSpec.TargetDuration);
            _logger.LogInformation("Parsed {SceneCount} scenes", scenes.Count);

            // Stage 3: Generate narration
            progress?.Report("Stage 3/5: Generating narration...");
            var scriptLines = ConvertScenesToScriptLines(scenes);
            string narrationPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ct);
            _logger.LogInformation("Narration generated at: {Path}", narrationPath);

            // Stage 4: Build timeline (placeholder for music/assets)
            progress?.Report("Stage 4/5: Building timeline...");
            var timeline = new Providers.Timeline(
                Scenes: scenes,
                SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
                NarrationPath: narrationPath,
                MusicPath: string.Empty,
                SubtitlesPath: null
            );

            // Stage 5: Render video
            progress?.Report("Stage 5/5: Rendering video...");
            var renderProgress = new Progress<RenderProgress>(p =>
            {
                progress?.Report($"Rendering: {p.Percentage:F1}% - {p.CurrentStage}");
            });

            string outputPath = await _videoComposer.RenderAsync(timeline, renderSpec, renderProgress, ct);
            _logger.LogInformation("Video rendered to: {Path}", outputPath);

            progress?.Report("Video generation complete!");
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video generation");
            throw;
        }
    }

    /// <summary>
    /// Parses the generated script text into Scene objects with timings.
    /// </summary>
    private List<Scene> ParseScriptIntoScenes(string script, TimeSpan targetDuration)
    {
        var scenes = new List<Scene>();
        var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? currentHeading = null;
        var currentScriptLines = new List<string>();
        int sceneIndex = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("## "))
            {
                // Found a new scene heading
                if (currentHeading != null && currentScriptLines.Count > 0)
                {
                    // Save the previous scene
                    var sceneScript = string.Join("\n", currentScriptLines);
                    scenes.Add(new Scene(sceneIndex++, currentHeading, sceneScript, TimeSpan.Zero, TimeSpan.Zero));
                    currentScriptLines.Clear();
                }
                currentHeading = line.Substring(3).Trim();
            }
            else if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
            {
                // Regular content line
                currentScriptLines.Add(line);
            }
        }

        // Add the last scene
        if (currentHeading != null && currentScriptLines.Count > 0)
        {
            var sceneScript = string.Join("\n", currentScriptLines);
            scenes.Add(new Scene(sceneIndex++, currentHeading, sceneScript, TimeSpan.Zero, TimeSpan.Zero));
        }

        // Calculate timings based on word count distribution
        int totalWords = scenes.Sum(s => CountWords(s.Script));
        TimeSpan currentStart = TimeSpan.Zero;

        for (int i = 0; i < scenes.Count; i++)
        {
            int sceneWords = CountWords(scenes[i].Script);
            double proportion = totalWords > 0 ? (double)sceneWords / totalWords : 1.0 / scenes.Count;
            TimeSpan duration = TimeSpan.FromSeconds(targetDuration.TotalSeconds * proportion);

            scenes[i] = scenes[i] with
            {
                Start = currentStart,
                Duration = duration
            };

            currentStart += duration;
        }

        return scenes;
    }

    /// <summary>
    /// Converts scenes into script lines for TTS synthesis.
    /// </summary>
    private List<ScriptLine> ConvertScenesToScriptLines(List<Scene> scenes)
    {
        var scriptLines = new List<ScriptLine>();

        foreach (var scene in scenes)
        {
            scriptLines.Add(new ScriptLine(
                SceneIndex: scene.Index,
                Text: scene.Script,
                Start: scene.Start,
                Duration: scene.Duration
            ));
        }

        return scriptLines;
    }

    /// <summary>
    /// Counts the number of words in a text string.
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
