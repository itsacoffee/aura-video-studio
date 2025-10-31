using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Generation;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Generation;
using Aura.Core.Services.Orchestration;
using Aura.Core.Services.PacingServices;
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
    private readonly ProviderRetryWrapper _retryWrapper;
    private readonly TtsOutputValidator _ttsValidator;
    private readonly ImageOutputValidator _imageValidator;
    private readonly LlmOutputValidator _llmValidator;
    private readonly ResourceCleanupManager _cleanupManager;
    private readonly Services.PacingServices.IntelligentPacingOptimizer? _pacingOptimizer;
    private readonly Services.PacingServices.PacingApplicationService? _pacingApplicationService;
    private readonly Timeline.TimelineBuilder _timelineBuilder;
    private readonly Configuration.ProviderSettings _providerSettings;
    private readonly NarrationOptimizationService? _narrationOptimizationService;
    private readonly PipelineOrchestrationEngine? _pipelineEngine;

    public VideoOrchestrator(
        ILogger<VideoOrchestrator> logger,
        ILlmProvider llmProvider,
        ITtsProvider ttsProvider,
        IVideoComposer videoComposer,
        VideoGenerationOrchestrator smartOrchestrator,
        ResourceMonitor resourceMonitor,
        PreGenerationValidator preGenerationValidator,
        ScriptValidator scriptValidator,
        ProviderRetryWrapper retryWrapper,
        TtsOutputValidator ttsValidator,
        ImageOutputValidator imageValidator,
        LlmOutputValidator llmValidator,
        ResourceCleanupManager cleanupManager,
        Timeline.TimelineBuilder timelineBuilder,
        Configuration.ProviderSettings providerSettings,
        IImageProvider? imageProvider = null,
        Services.PacingServices.IntelligentPacingOptimizer? pacingOptimizer = null,
        Services.PacingServices.PacingApplicationService? pacingApplicationService = null,
        NarrationOptimizationService? narrationOptimizationService = null,
        PipelineOrchestrationEngine? pipelineEngine = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(llmProvider);
        ArgumentNullException.ThrowIfNull(ttsProvider);
        ArgumentNullException.ThrowIfNull(videoComposer);
        ArgumentNullException.ThrowIfNull(smartOrchestrator);
        ArgumentNullException.ThrowIfNull(resourceMonitor);
        ArgumentNullException.ThrowIfNull(preGenerationValidator);
        ArgumentNullException.ThrowIfNull(scriptValidator);
        ArgumentNullException.ThrowIfNull(retryWrapper);
        ArgumentNullException.ThrowIfNull(ttsValidator);
        ArgumentNullException.ThrowIfNull(imageValidator);
        ArgumentNullException.ThrowIfNull(llmValidator);
        ArgumentNullException.ThrowIfNull(cleanupManager);
        ArgumentNullException.ThrowIfNull(timelineBuilder);
        ArgumentNullException.ThrowIfNull(providerSettings);
        
        _logger = logger;
        _llmProvider = llmProvider;
        _ttsProvider = ttsProvider;
        _videoComposer = videoComposer;
        _smartOrchestrator = smartOrchestrator;
        _resourceMonitor = resourceMonitor;
        _imageProvider = imageProvider;
        _preGenerationValidator = preGenerationValidator;
        _scriptValidator = scriptValidator;
        _retryWrapper = retryWrapper;
        _ttsValidator = ttsValidator;
        _imageValidator = imageValidator;
        _llmValidator = llmValidator;
        _cleanupManager = cleanupManager;
        _pacingOptimizer = pacingOptimizer;
        _pacingApplicationService = pacingApplicationService;
        _timelineBuilder = timelineBuilder;
        _providerSettings = providerSettings;
        _narrationOptimizationService = narrationOptimizationService;
        _pipelineEngine = pipelineEngine;
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
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(planSpec);
        ArgumentNullException.ThrowIfNull(voiceSpec);
        ArgumentNullException.ThrowIfNull(renderSpec);
        ArgumentNullException.ThrowIfNull(systemProfile);
        
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
        finally
        {
            // Clean up temporary resources on completion or failure
            _cleanupManager.CleanupAll();
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
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(planSpec);
        ArgumentNullException.ThrowIfNull(voiceSpec);
        ArgumentNullException.ThrowIfNull(renderSpec);
        
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

            // If pipeline orchestration engine is available, use intelligent orchestration
            if (_pipelineEngine != null)
            {
                _logger.LogInformation("Using intelligent pipeline orchestration for generation");
                return await GenerateVideoWithPipelineAsync(brief, planSpec, voiceSpec, renderSpec, progress, ct).ConfigureAwait(false);
            }

            progress?.Report("Starting video generation pipeline...");

            // Stage 1: Script generation
            progress?.Report("Stage 1/5: Generating script...");
            _logger.LogInformation("Generating script for topic: {Topic}", brief.Topic);
            
            string script = await _retryWrapper.ExecuteWithRetryAsync(
                async (ctRetry) =>
                {
                    var generatedScript = await _llmProvider.DraftScriptAsync(brief, planSpec, ctRetry).ConfigureAwait(false);
                    
                    // Validate script structure and content
                    var structuralValidation = _scriptValidator.Validate(generatedScript, planSpec);
                    var contentValidation = _llmValidator.ValidateScriptContent(generatedScript, planSpec);
                    
                    if (!structuralValidation.IsValid || !contentValidation.IsValid)
                    {
                        var allIssues = structuralValidation.Issues.Concat(contentValidation.Issues).ToList();
                        _logger.LogWarning("Script validation failed: {Issues}", string.Join(", ", allIssues));
                        throw new ValidationException("Script quality validation failed", allIssues);
                    }
                    
                    return generatedScript;
                },
                "Script Generation",
                ct,
                maxRetries: 2
            ).ConfigureAwait(false);
            
            _logger.LogInformation("Script generated and validated: {Length} characters", script.Length);

            // Stage 2: Parse script into scenes
            progress?.Report("Stage 2/5: Parsing scenes...");
            var scenes = ParseScriptIntoScenes(script, planSpec.TargetDuration);
            _logger.LogInformation("Parsed {SceneCount} scenes", scenes.Count);

            // Optional: Apply pacing optimization if enabled
            if (_providerSettings.GetEnablePacingOptimization() && 
                _pacingOptimizer != null && 
                _pacingApplicationService != null &&
                _providerSettings.GetAutoApplyPacingSuggestions())
            {
                try
                {
                    progress?.Report("Analyzing and optimizing pacing...");
                    _logger.LogInformation("Pacing optimization enabled, analyzing scenes");

                    var startTime = DateTime.UtcNow;
                    var pacingResult = await _pacingOptimizer.OptimizePacingAsync(
                        scenes, brief, _llmProvider, true, PacingProfile.BalancedDocumentary, ct).ConfigureAwait(false);
                    
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                    _logger.LogInformation(
                        "Pacing analysis completed in {Elapsed:F2}s. Confidence: {Confidence:F1}%, Retention: {Retention:F1}%",
                        elapsed, pacingResult.ConfidenceScore, pacingResult.PredictedRetentionRate);

                    // Validate suggestions
                    var optimizationLevel = Enum.Parse<Models.Settings.OptimizationLevel>(
                        _providerSettings.GetPacingOptimizationLevel(), ignoreCase: true);
                    var minConfidence = _providerSettings.GetMinimumConfidenceThreshold();

                    var validation = _pacingApplicationService.ValidateSuggestions(
                        pacingResult, scenes, planSpec.TargetDuration, minConfidence);

                    if (validation.IsValid)
                    {
                        // Apply pacing suggestions
                        scenes = _timelineBuilder.ApplyPacingSuggestions(
                            scenes, pacingResult, optimizationLevel, minConfidence).ToList();
                        
                        _logger.LogInformation(
                            "Applied pacing suggestions. New total duration: {Duration}",
                            TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds)));
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Pacing suggestions validation failed: {Issues}",
                            string.Join("; ", validation.Issues));
                    }

                    // Log warnings if any
                    foreach (var warning in validation.Warnings)
                    {
                        _logger.LogWarning("Pacing warning: {Warning}", warning);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Pacing optimization failed, continuing with original timings");
                    // Continue with original scenes if pacing fails
                }
            }
            else
            {
                _logger.LogDebug("Pacing optimization disabled or not available");
            }

            // Stage 3: Generate narration
            progress?.Report("Stage 3/5: Generating narration...");
            var scriptLines = ConvertScenesToScriptLines(scenes);
            
            // Optimize narration for TTS if service is available
            if (_narrationOptimizationService != null)
            {
                try
                {
                    _logger.LogInformation("Optimizing narration for TTS synthesis");
                    var optimizationConfig = new NarrationOptimizationConfig();
                    var optimizationResult = await _narrationOptimizationService.OptimizeForTtsAsync(
                        scriptLines,
                        voiceSpec,
                        null,
                        optimizationConfig,
                        ct
                    ).ConfigureAwait(false);

                    _logger.LogInformation(
                        "Narration optimized. Score: {Score:F1}, Optimizations: {Count}",
                        optimizationResult.OptimizationScore,
                        optimizationResult.OptimizationsApplied
                    );

                    // Use optimized script lines for TTS
                    scriptLines = optimizationResult.OptimizedLines
                        .Select(ol => new ScriptLine(
                            ol.SceneIndex,
                            ol.OptimizedText,
                            ol.Start,
                            ol.Duration
                        ))
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Narration optimization failed, using original text");
                    // Continue with original script lines if optimization fails
                }
            }
            else
            {
                _logger.LogDebug("Narration optimization service not available");
            }
            
            string narrationPath = await _retryWrapper.ExecuteWithRetryAsync(
                async (ctRetry) =>
                {
                    var audioPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ctRetry).ConfigureAwait(false);
                    
                    // Validate audio output
                    var minDuration = TimeSpan.FromSeconds(Math.Max(5, planSpec.TargetDuration.TotalSeconds * 0.3));
                    var audioValidation = _ttsValidator.ValidateAudioFile(audioPath, minDuration);
                    
                    if (!audioValidation.IsValid)
                    {
                        _logger.LogWarning("Audio validation failed: {Issues}", string.Join(", ", audioValidation.Issues));
                        throw new ValidationException("Audio quality validation failed", audioValidation.Issues);
                    }
                    
                    _cleanupManager.RegisterTempFile(audioPath);
                    return audioPath;
                },
                "Audio Generation",
                ct
            ).ConfigureAwait(false);
            
            _logger.LogInformation("Narration generated and validated at: {Path}", narrationPath);

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
        finally
        {
            // Clean up temporary resources on completion or failure
            _cleanupManager.CleanupAll();
        }
    }

    /// <summary>
    /// Generates a complete video using the intelligent pipeline orchestration engine.
    /// Uses dependency-aware service ordering, parallel execution, and smart caching.
    /// </summary>
    private async Task<string> GenerateVideoWithPipelineAsync(
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        if (_pipelineEngine == null)
            throw new InvalidOperationException("Pipeline engine not available");

        progress?.Report("Initializing intelligent pipeline orchestration...");
        _logger.LogInformation("Starting pipeline-based video generation for topic: {Topic}", brief.Topic);

        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = Environment.ProcessorCount,
            PhysicalCores = Math.Max(1, Environment.ProcessorCount / 2),
            RamGB = (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024.0 * 1024 * 1024))
        };

        var pipelineContext = new PipelineExecutionContext
        {
            Brief = brief,
            PlanSpec = planSpec,
            VoiceSpec = voiceSpec,
            RenderSpec = renderSpec,
            SystemProfile = systemProfile
        };

        var pipelineConfig = new PipelineConfiguration
        {
            MaxConcurrentLlmCalls = Math.Max(1, Environment.ProcessorCount / 2),
            EnableCaching = true,
            CacheTtl = TimeSpan.FromHours(1),
            ContinueOnOptionalFailure = true,
            EnableParallelExecution = true
        };

        var pipelineProgress = new Progress<PipelineProgress>(p =>
        {
            var stagePercent = p.PercentComplete;
            progress?.Report($"{p.CurrentStage}: {stagePercent:F1}% ({p.CompletedServices}/{p.TotalServices} services)");
        });

        var result = await _pipelineEngine.ExecutePipelineAsync(
            pipelineContext, pipelineConfig, pipelineProgress, ct
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            var errors = string.Join("; ", result.Errors);
            _logger.LogError("Pipeline orchestration failed: {Errors}", errors);
            throw new InvalidOperationException($"Pipeline generation failed: {errors}");
        }

        _logger.LogInformation(
            "Pipeline orchestration completed successfully. Duration: {Duration}s, Cache hits: {CacheHits}, Parallel executions: {ParallelExecutions}",
            result.TotalExecutionTime.TotalSeconds,
            result.CacheHits,
            result.ParallelExecutions
        );

        if (result.Warnings.Count > 0)
        {
            _logger.LogWarning("Pipeline completed with warnings: {Warnings}", string.Join("; ", result.Warnings));
        }

        string script = pipelineContext.GeneratedScript 
            ?? throw new InvalidOperationException("Pipeline did not generate script");

        progress?.Report("Stage 3/5: Generating narration...");
        var scenes = ParseScriptIntoScenes(script, planSpec.TargetDuration);
        var scriptLines = ConvertScenesToScriptLines(scenes);

        string narrationPath = await _retryWrapper.ExecuteWithRetryAsync(
            async (ctRetry) =>
            {
                var audioPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ctRetry).ConfigureAwait(false);
                
                var minDuration = TimeSpan.FromSeconds(Math.Max(5, planSpec.TargetDuration.TotalSeconds * 0.3));
                var audioValidation = _ttsValidator.ValidateAudioFile(audioPath, minDuration);
                
                if (!audioValidation.IsValid)
                {
                    _logger.LogWarning("Audio validation failed: {Issues}", string.Join(", ", audioValidation.Issues));
                    throw new ValidationException("Audio quality validation failed", audioValidation.Issues);
                }
                
                _cleanupManager.RegisterTempFile(audioPath);
                return audioPath;
            },
            "Audio Generation",
            ct
        ).ConfigureAwait(false);
        
        _logger.LogInformation("Narration generated at: {Path}", narrationPath);

        progress?.Report("Stage 4/5: Building timeline...");
        var timeline = new Providers.Timeline(
            Scenes: scenes,
            SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
            NarrationPath: narrationPath,
            MusicPath: string.Empty,
            SubtitlesPath: null
        );

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
                    // Generate script using LLM with retry logic
                    generatedScript = await _retryWrapper.ExecuteWithRetryAsync(
                        async (ctRetry) =>
                        {
                            var script = await _llmProvider.DraftScriptAsync(brief, planSpec, ctRetry).ConfigureAwait(false);
                            
                            // Validate script structure and content
                            var structuralValidation = _scriptValidator.Validate(script, planSpec);
                            var contentValidation = _llmValidator.ValidateScriptContent(script, planSpec);
                            
                            if (!structuralValidation.IsValid || !contentValidation.IsValid)
                            {
                                var allIssues = structuralValidation.Issues.Concat(contentValidation.Issues).ToList();
                                _logger.LogWarning("Script validation failed: {Issues}", string.Join(", ", allIssues));
                                throw new ValidationException("Script quality validation failed", allIssues);
                            }
                            
                            return script;
                        },
                        "Script Generation",
                        ct,
                        maxRetries: 2 // Try up to 2 times for script generation
                    ).ConfigureAwait(false);
                    
                    _logger.LogInformation("Script generated and validated: {Length} characters", generatedScript.Length);
                    
                    // Parse scenes immediately for downstream tasks
                    parsedScenes = ParseScriptIntoScenes(generatedScript, planSpec.TargetDuration);
                    _logger.LogInformation("Parsed {SceneCount} scenes", parsedScenes.Count);
                    
                    // Optional: Apply pacing optimization if enabled
                    if (_providerSettings.GetEnablePacingOptimization() && 
                        _pacingOptimizer != null && 
                        _pacingApplicationService != null &&
                        _providerSettings.GetAutoApplyPacingSuggestions())
                    {
                        try
                        {
                            _logger.LogInformation("Pacing optimization enabled, analyzing scenes");
                            
                            var startTime = DateTime.UtcNow;
                            var pacingResult = await _pacingOptimizer.OptimizePacingAsync(
                                parsedScenes, brief, _llmProvider, true, PacingProfile.BalancedDocumentary, ct).ConfigureAwait(false);
                            
                            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                            _logger.LogInformation(
                                "Pacing analysis completed in {Elapsed:F2}s. Confidence: {Confidence:F1}%",
                                elapsed, pacingResult.ConfidenceScore);

                            // Validate and apply
                            var optimizationLevel = Enum.Parse<Models.Settings.OptimizationLevel>(
                                _providerSettings.GetPacingOptimizationLevel(), ignoreCase: true);
                            var minConfidence = _providerSettings.GetMinimumConfidenceThreshold();

                            var validation = _pacingApplicationService.ValidateSuggestions(
                                pacingResult, parsedScenes, planSpec.TargetDuration, minConfidence);

                            if (validation.IsValid)
                            {
                                parsedScenes = _timelineBuilder.ApplyPacingSuggestions(
                                    parsedScenes, pacingResult, optimizationLevel, minConfidence).ToList();
                                
                                _logger.LogInformation(
                                    "Applied pacing suggestions. New total duration: {Duration}",
                                    TimeSpan.FromSeconds(parsedScenes.Sum(s => s.Duration.TotalSeconds)));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Pacing optimization failed, continuing with original timings");
                        }
                    }
                    
                    return generatedScript;

                case GenerationTaskType.AudioGeneration:
                    // Generate audio from parsed scenes
                    if (parsedScenes == null || generatedScript == null)
                    {
                        throw new InvalidOperationException("Script must be generated before audio");
                    }

                    var scriptLines = ConvertScenesToScriptLines(parsedScenes);
                    
                    // Generate audio with retry logic and validation
                    narrationPath = await _retryWrapper.ExecuteWithRetryAsync(
                        async (ctRetry) =>
                        {
                            var audioPath = await _ttsProvider.SynthesizeAsync(scriptLines, voiceSpec, ctRetry).ConfigureAwait(false);
                            
                            // Validate audio output
                            var minDuration = TimeSpan.FromSeconds(Math.Max(5, planSpec.TargetDuration.TotalSeconds * 0.3));
                            var audioValidation = _ttsValidator.ValidateAudioFile(audioPath, minDuration);
                            
                            if (!audioValidation.IsValid)
                            {
                                _logger.LogWarning("Audio validation failed: {Issues}", string.Join(", ", audioValidation.Issues));
                                throw new ValidationException("Audio quality validation failed", audioValidation.Issues);
                            }
                            
                            // Register for cleanup (will be promoted to artifact later)
                            _cleanupManager.RegisterTempFile(audioPath);
                            
                            return audioPath;
                        },
                        "Audio Generation",
                        ct
                    ).ConfigureAwait(false);
                    
                    _logger.LogInformation("Narration generated and validated at: {Path}", narrationPath);
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
                    
                    // Generate images with retry logic and validation
                    var assets = await _retryWrapper.ExecuteWithRetryAsync(
                        async (ctRetry) =>
                        {
                            var generatedAssets = await _imageProvider.FetchOrGenerateAsync(scene, visualSpec, ctRetry).ConfigureAwait(false);
                            
                            // Validate image assets
                            var imageValidation = _imageValidator.ValidateImageAssets(generatedAssets, expectedMinCount: 1);
                            
                            if (!imageValidation.IsValid)
                            {
                                _logger.LogWarning("Image validation failed for scene {SceneIndex}: {Issues}", 
                                    sceneIndex, string.Join(", ", imageValidation.Issues));
                                
                                // For image generation, we can be more lenient and return empty if validation fails
                                // This prevents the entire pipeline from failing due to missing stock images
                                if (generatedAssets.Count == 0)
                                {
                                    _logger.LogWarning("No assets generated for scene {SceneIndex}, continuing with empty asset list", sceneIndex);
                                    return Array.Empty<Asset>();
                                }
                            }
                            
                            // Register image files for cleanup (skip URLs)
                            foreach (var asset in generatedAssets)
                            {
                                if (!string.IsNullOrEmpty(asset.PathOrUrl) &&
                                    !asset.PathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                    !asset.PathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                {
                                    _cleanupManager.RegisterTempFile(asset.PathOrUrl);
                                }
                            }
                            
                            return generatedAssets;
                        },
                        $"Image Generation (Scene {sceneIndex})",
                        ct
                    ).ConfigureAwait(false);
                    
                    sceneAssets[sceneIndex] = assets;
                    _logger.LogDebug("Generated and validated {Count} assets for scene {Index}", assets.Count, sceneIndex);
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
