using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Providers;
using Aura.Core.Services.Generation;
using Aura.Core.Validation;
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
    private readonly VideoGenerationOrchestrator _smartOrchestrator;
    private readonly ResourceMonitor _resourceMonitor;
    private readonly IImageProvider? _imageProvider;
    private readonly PreGenerationValidator _preGenerationValidator;
    private readonly ScriptValidator _scriptValidator;

    public VideoOrchestrator(
        ILogger<VideoOrchestrator> logger,
        ILlmProvider llmProvider,
        ITtsProvider ttsProvider,
        IVideoComposer videoComposer,
        VideoGenerationOrchestrator smartOrchestrator,
        ResourceMonitor resourceMonitor,
        PreGenerationValidator preGenerationValidator,
        ScriptValidator scriptValidator,
        IImageProvider? imageProvider = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _ttsProvider = ttsProvider;
        _videoComposer = videoComposer;
        _smartOrchestrator = smartOrchestrator;
        _resourceMonitor = resourceMonitor;
        _imageProvider = imageProvider;
        _preGenerationValidator = preGenerationValidator;
        _scriptValidator = scriptValidator;
    }

    /// <summary>
    /// Generates a complete video from the provided brief and specifications using smart orchestration.
    /// </summary>
    public async Task<string> GenerateVideoAsync(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        SystemProfile systemProfile,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            // Pre-generation validation
            progress?.Report("Validating system readiness...");
            var validationResult = await _preGenerationValidator.ValidateSystemReadyAsync(brief, planSpec, ct).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var issues = string.Join("\n", validationResult.Issues);
                _logger.LogError("Pre-generation validation failed: {Issues}", issues);
                throw new ValidationException("System validation failed", validationResult.Issues);
            }
            _logger.LogInformation("Pre-generation validation passed");

            progress?.Report("Starting smart video generation pipeline...");
            _logger.LogInformation("Using smart orchestration for topic: {Topic}", brief.Topic);

            // Create task executor that maps generation tasks to providers
            var taskExecutor = CreateTaskExecutor(brief, planSpec, voiceSpec, renderSpec, ct);

            // Map progress events
            var orchestrationProgress = new Progress<OrchestrationProgress>(p =>
            {
                progress?.Report($"{p.CurrentStage}: {p.ProgressPercentage:F1}%");
            });

            // Execute smart orchestration
            var result = await _smartOrchestrator.OrchestrateGenerationAsync(
                brief, planSpec, systemProfile, taskExecutor, orchestrationProgress, ct
            ).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                var reasons = string.Join("; ", result.FailureReasons);
                throw new InvalidOperationException(
                    $"Generation failed: {result.FailedTasks}/{result.TotalTasks} tasks failed. Reasons: {reasons}"
                );
            }

            // Extract final video path from composition task result
            if (!result.TaskResults.TryGetValue("composition", out var compositionTask) || compositionTask.Result == null)
            {
                throw new InvalidOperationException("Composition task did not produce output path");
            }

            var outputPath = compositionTask.Result as string
                ?? throw new InvalidOperationException("Composition result is not a valid path");

            _logger.LogInformation("Smart orchestration completed. Video at: {Path}", outputPath);
            progress?.Report("Video generation complete!");

            return outputPath;
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions without wrapping
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during smart video generation");
            throw;
        }
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
            // Pre-generation validation
            progress?.Report("Validating system readiness...");
            var validationResult = await _preGenerationValidator.ValidateSystemReadyAsync(brief, planSpec, ct).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var issues = string.Join("\n", validationResult.Issues);
                _logger.LogError("Pre-generation validation failed: {Issues}", issues);
                throw new ValidationException("System validation failed", validationResult.Issues);
            }
            _logger.LogInformation("Pre-generation validation passed");

            progress?.Report("Starting video generation pipeline...");

            // Stage 1: Script generation
            progress?.Report("Stage 1/5: Generating script...");
            _logger.LogInformation("Generating script for topic: {Topic}", brief.Topic);
            string script = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            _logger.LogInformation("Script generated: {Length} characters", script.Length);

            // Validate script quality
            var scriptValidation = _scriptValidator.Validate(script, planSpec);
            if (!scriptValidation.IsValid)
            {
                _logger.LogWarning("Script validation failed: {Issues}", string.Join(", ", scriptValidation.Issues));
                _logger.LogInformation("Attempting script regeneration...");
                
                // Try regenerating once
                script = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
                scriptValidation = _scriptValidator.Validate(script, planSpec);
                
                if (!scriptValidation.IsValid)
                {
                    var issues = string.Join("\n", scriptValidation.Issues);
                    _logger.LogError("Script validation failed after retry: {Issues}", issues);
                    throw new ValidationException("Script quality validation failed", scriptValidation.Issues);
                }
            }
            _logger.LogInformation("Script validation passed");

            // Stage 2: Parse script into scenes
            progress?.Report("Stage 2/5: Parsing scenes...");
            var scenes = ParseScriptIntoScenes(script, planSpec.TargetDuration);
            _logger.LogInformation("Parsed {SceneCount} scenes", scenes.Count);

            // Stage 3: Generate narration
            progress?.Report("Stage 3/5: Generating narration...");
            var scriptLines = ConvertScenesToScriptLines(scenes);
            string narrationPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ct).ConfigureAwait(false);
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

            string outputPath = await _videoComposer.RenderAsync(timeline, renderSpec, renderProgress, ct).ConfigureAwait(false);
            _logger.LogInformation("Video rendered to: {Path}", outputPath);

            progress?.Report("Video generation complete!");
            return outputPath;
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions without wrapping
            throw;
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

    /// <summary>
    /// Creates a task executor function that maps generation tasks to the appropriate providers.
    /// </summary>
    private Func<GenerationNode, CancellationToken, Task<object>> CreateTaskExecutor(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        CancellationToken outerCt)
    {
        // Shared state for task results
        string? generatedScript = null;
        List<Scene>? parsedScenes = null;
        string? narrationPath = null;
        Dictionary<int, IReadOnlyList<Asset>> sceneAssets = new();

        return async (node, ct) =>
        {
            _logger.LogDebug("Executing task: {TaskId} ({TaskType})", node.TaskId, node.TaskType);

            switch (node.TaskType)
            {
                case GenerationTaskType.ScriptGeneration:
                    // Generate script using LLM
                    generatedScript = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
                    _logger.LogInformation("Script generated: {Length} characters", generatedScript.Length);
                    
                    // Validate script quality
                    var scriptValidation = _scriptValidator.Validate(generatedScript, planSpec);
                    if (!scriptValidation.IsValid)
                    {
                        _logger.LogWarning("Script validation failed: {Issues}", string.Join(", ", scriptValidation.Issues));
                        _logger.LogInformation("Attempting script regeneration...");
                        
                        // Try regenerating once
                        generatedScript = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
                        scriptValidation = _scriptValidator.Validate(generatedScript, planSpec);
                        
                        if (!scriptValidation.IsValid)
                        {
                            var issues = string.Join("\n", scriptValidation.Issues);
                            _logger.LogError("Script validation failed after retry: {Issues}", issues);
                            throw new ValidationException("Script quality validation failed", scriptValidation.Issues);
                        }
                    }
                    _logger.LogInformation("Script validation passed");
                    
                    // Parse scenes immediately for downstream tasks
                    parsedScenes = ParseScriptIntoScenes(generatedScript, planSpec.TargetDuration);
                    _logger.LogInformation("Parsed {SceneCount} scenes", parsedScenes.Count);
                    
                    return generatedScript;

                case GenerationTaskType.AudioGeneration:
                    // Generate audio from parsed scenes
                    if (parsedScenes == null || generatedScript == null)
                    {
                        throw new InvalidOperationException("Script must be generated before audio");
                    }

                    var scriptLines = ConvertScenesToScriptLines(parsedScenes);
                    narrationPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ct).ConfigureAwait(false);
                    _logger.LogInformation("Narration generated at: {Path}", narrationPath);
                    return narrationPath;

                case GenerationTaskType.ImageGeneration:
                    // Generate images for specific scene
                    if (parsedScenes == null || _imageProvider == null)
                    {
                        // Return empty asset list if no image provider available
                        return Array.Empty<Asset>();
                    }

                    // Extract scene index from task ID (e.g., "visual_0" -> 0)
                    var parts = node.TaskId.Split('_');
                    if (parts.Length < 2 || !int.TryParse(parts[^1], out int sceneIndex))
                    {
                        throw new InvalidOperationException($"Invalid image task ID: {node.TaskId}");
                    }

                    if (sceneIndex >= parsedScenes.Count)
                    {
                        _logger.LogWarning("Scene index {Index} out of range, skipping", sceneIndex);
                        return Array.Empty<Asset>();
                    }

                    var scene = parsedScenes[sceneIndex];
                    var visualSpec = new VisualSpec(planSpec.Style, brief.Aspect, Array.Empty<string>());
                    var assets = await _imageProvider.FetchOrGenerateAsync(scene, visualSpec, ct).ConfigureAwait(false);
                    sceneAssets[sceneIndex] = assets;
                    _logger.LogDebug("Generated {Count} assets for scene {Index}", assets.Count, sceneIndex);
                    return assets;

                case GenerationTaskType.VideoComposition:
                    // Final render combining all assets
                    if (parsedScenes == null || narrationPath == null)
                    {
                        throw new InvalidOperationException("Script and audio must be generated before composition");
                    }

                    var timeline = new Providers.Timeline(
                        Scenes: parsedScenes,
                        SceneAssets: sceneAssets,
                        NarrationPath: narrationPath,
                        MusicPath: string.Empty,
                        SubtitlesPath: null
                    );

                    var renderProgress = new Progress<RenderProgress>(p =>
                    {
                        _logger.LogDebug("Rendering: {Percentage:F1}% - {Stage}", p.Percentage, p.CurrentStage);
                    });

                    var outputPath = await _videoComposer.RenderAsync(timeline, renderSpec, renderProgress, ct).ConfigureAwait(false);
                    _logger.LogInformation("Video rendered to: {Path}", outputPath);
                    return outputPath;

                default:
                    throw new NotSupportedException($"Task type {node.TaskType} not supported");
            }
        };
    }
}
