using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// Stage 3: Voice synthesis (TTS)
/// Converts script text to audio narration
/// </summary>
public class VoiceStage : PipelineStage
{
    private readonly ITtsProvider _ttsProvider;
    private readonly TtsOutputValidator _ttsValidator;
    private readonly ProviderRetryWrapper _retryWrapper;
    private readonly ResourceCleanupManager _cleanupManager;

    public VoiceStage(
        ILogger<VoiceStage> logger,
        ITtsProvider ttsProvider,
        TtsOutputValidator ttsValidator,
        ProviderRetryWrapper retryWrapper,
        ResourceCleanupManager cleanupManager) : base(logger)
    {
        _ttsProvider = ttsProvider ?? throw new ArgumentNullException(nameof(ttsProvider));
        _ttsValidator = ttsValidator ?? throw new ArgumentNullException(nameof(ttsValidator));
        _retryWrapper = retryWrapper ?? throw new ArgumentNullException(nameof(retryWrapper));
        _cleanupManager = cleanupManager ?? throw new ArgumentNullException(nameof(cleanupManager));
    }

    public override string StageName => "Voice";
    public override string DisplayName => "Voice Synthesis";
    public override int ProgressWeight => 25;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(3);

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 5, "Preparing script for voice synthesis...");

        // Get script from context
        if (string.IsNullOrEmpty(context.GeneratedScript))
        {
            throw new InvalidOperationException("Script must be generated before voice synthesis");
        }

        // Parse script into scenes
        var scenes = ParseScriptIntoScenes(context.GeneratedScript, context.PlanSpec.TargetDuration);
        context.ParsedScenes = scenes;

        ReportProgress(progress, 10, $"Parsed {scenes.Count} scenes");

        Logger.LogInformation(
            "[{CorrelationId}] Parsed {SceneCount} scenes for TTS",
            context.CorrelationId,
            scenes.Count);

        // Convert scenes to script lines
        var scriptLines = ConvertScenesToScriptLines(scenes);

        ReportProgress(progress, 20, "Synthesizing narration...");

        // Generate audio with retry logic
        var narrationPath = await _retryWrapper.ExecuteWithRetryAsync(
            async (ctRetry) =>
            {
                var audioPath = await _ttsProvider.SynthesizeAsync(
                    scriptLines,
                    context.VoiceSpec,
                    ctRetry).ConfigureAwait(false);

                ReportProgress(progress, 70, "Validating audio output...");

                // Validate audio output
                var minDuration = TimeSpan.FromSeconds(
                    Math.Max(5, context.PlanSpec.TargetDuration.TotalSeconds * 0.3));
                var audioValidation = _ttsValidator.ValidateAudioFile(audioPath, minDuration);

                if (!audioValidation.IsValid)
                {
                    Logger.LogWarning(
                        "[{CorrelationId}] Audio validation failed: {Issues}",
                        context.CorrelationId,
                        string.Join(", ", audioValidation.Issues));
                    
                    throw new Validation.ValidationException(
                        "Audio quality validation failed",
                        audioValidation.Issues);
                }

                // Register for cleanup
                _cleanupManager.RegisterTempFile(audioPath);

                return audioPath;
            },
            "Audio Generation",
            ct
        ).ConfigureAwait(false);

        ReportProgress(progress, 90, "Narration generated successfully");

        Logger.LogInformation(
            "[{CorrelationId}] Narration generated and validated at: {Path}",
            context.CorrelationId,
            narrationPath);

        // Store narration path in context
        context.NarrationPath = narrationPath;
        context.SetStageOutput(StageName, new VoiceStageOutput
        {
            NarrationPath = narrationPath,
            SceneCount = scenes.Count,
            TotalCharacters = scriptLines.Sum(sl => sl.Text?.Length ?? 0),
            Provider = _ttsProvider.GetType().Name,
            VoiceName = context.VoiceSpec.VoiceName,
            GeneratedAt = DateTime.UtcNow
        });

        ReportProgress(progress, 100, "Voice synthesis completed");
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        return !string.IsNullOrEmpty(context.NarrationPath) &&
               System.IO.File.Exists(context.NarrationPath);
    }

    protected override int GetItemsProcessed(PipelineContext context)
    {
        return context.ParsedScenes?.Count ?? 0;
    }

    /// <summary>
    /// Parses the generated script text into Scene objects with timings
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
                    var sceneScript = string.Join("\n", currentScriptLines);
                    scenes.Add(new Scene(sceneIndex++, currentHeading, sceneScript, TimeSpan.Zero, TimeSpan.Zero));
                    currentScriptLines.Clear();
                }
                currentHeading = line.Substring(3).Trim();
            }
            else if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
            {
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
    /// Converts scenes into script lines for TTS synthesis
    /// </summary>
    private List<ScriptLine> ConvertScenesToScriptLines(List<Scene> scenes)
    {
        return scenes.Select(scene => new ScriptLine(
            SceneIndex: scene.Index,
            Text: scene.Script,
            Start: scene.Start,
            Duration: scene.Duration
        )).ToList();
    }

    /// <summary>
    /// Counts the number of words in a text string
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

/// <summary>
/// Output from the Voice stage
/// </summary>
public record VoiceStageOutput
{
    public required string NarrationPath { get; init; }
    public required int SceneCount { get; init; }
    public required int TotalCharacters { get; init; }
    public required string Provider { get; init; }
    public required string? VoiceName { get; init; }
    public required DateTime GeneratedAt { get; init; }
}
