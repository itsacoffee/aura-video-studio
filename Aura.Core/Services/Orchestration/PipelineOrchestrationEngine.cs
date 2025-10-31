using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Narrative;
using Aura.Core.Services.PacingServices;
using Aura.Core.Services.Quality;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Intelligent pipeline orchestration engine with dependency-aware service ordering,
/// parallel execution, graceful degradation, and smart caching
/// </summary>
public class PipelineOrchestrationEngine
{
    private readonly ILogger<PipelineOrchestrationEngine> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly ITtsProvider? _ttsProvider;
    private readonly PipelineCache _cache;
    private readonly PipelineHealthCheck _healthCheck;
    private readonly SemaphoreSlim _llmSemaphore;
    
    private readonly IntelligentContentAdvisor? _contentAdvisor;
    private readonly NarrativeFlowAnalyzer? _narrativeAnalyzer;
    private readonly IntelligentPacingOptimizer? _pacingOptimizer;
    private readonly ToneConsistencyEnforcer? _toneEnforcer;
    private readonly VisualPromptGenerationService? _visualPromptService;
    private readonly VisualTextAlignmentService? _visualAlignmentService;
    private readonly NarrationOptimizationService? _narrationOptimizer;
    private readonly ScriptRefinementOrchestrator? _scriptRefinement;

    public PipelineOrchestrationEngine(
        ILogger<PipelineOrchestrationEngine> logger,
        ILlmProvider llmProvider,
        PipelineCache cache,
        PipelineHealthCheck healthCheck,
        PipelineConfiguration? config = null,
        ITtsProvider? ttsProvider = null,
        IntelligentContentAdvisor? contentAdvisor = null,
        NarrativeFlowAnalyzer? narrativeAnalyzer = null,
        IntelligentPacingOptimizer? pacingOptimizer = null,
        ToneConsistencyEnforcer? toneEnforcer = null,
        VisualPromptGenerationService? visualPromptService = null,
        VisualTextAlignmentService? visualAlignmentService = null,
        NarrationOptimizationService? narrationOptimizer = null,
        ScriptRefinementOrchestrator? scriptRefinement = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _ttsProvider = ttsProvider;
        _cache = cache;
        _healthCheck = healthCheck;
        _contentAdvisor = contentAdvisor;
        _narrativeAnalyzer = narrativeAnalyzer;
        _pacingOptimizer = pacingOptimizer;
        _toneEnforcer = toneEnforcer;
        _visualPromptService = visualPromptService;
        _visualAlignmentService = visualAlignmentService;
        _narrationOptimizer = narrationOptimizer;
        _scriptRefinement = scriptRefinement;

        config ??= new PipelineConfiguration();
        _llmSemaphore = new SemaphoreSlim(config.MaxConcurrentLlmCalls, config.MaxConcurrentLlmCalls);
    }

    /// <summary>
    /// Orchestrates the entire pipeline with dependency-aware execution
    /// </summary>
    public async Task<PipelineExecutionResult> ExecutePipelineAsync(
        PipelineExecutionContext context,
        PipelineConfiguration config,
        IProgress<PipelineProgress>? progress = null,
        CancellationToken ct = default)
    {
        var overallStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting pipeline orchestration for topic: {Topic}", context.Brief.Topic);

        var serviceResults = new Dictionary<string, ServiceExecutionResult>();
        var stageTimings = new Dictionary<PipelineStage, TimeSpan>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var cacheHits = 0;
        var parallelExecutions = 0;

        try
        {
            var healthCheck = await _healthCheck.CheckHealthAsync(ct).ConfigureAwait(false);
            if (!healthCheck.IsHealthy)
            {
                var errorMsg = $"Pipeline health check failed: {string.Join(", ", healthCheck.MissingRequiredServices)}";
                _logger.LogError("{Error}", errorMsg);
                errors.Add(errorMsg);
                
                return new PipelineExecutionResult
                {
                    Success = false,
                    ServiceResults = serviceResults,
                    TotalExecutionTime = overallStopwatch.Elapsed,
                    StageTimings = stageTimings,
                    Warnings = healthCheck.Warnings,
                    Errors = errors,
                    CacheHits = 0,
                    ParallelExecutions = 0
                };
            }

            warnings.AddRange(healthCheck.Warnings);

            var services = BuildPipelineServices();
            var completedServices = 0;

            foreach (var stage in Enum.GetValues<PipelineStage>())
            {
                var stageStopwatch = Stopwatch.StartNew();
                var stageServices = services.Where(s => s.Stage == stage).ToList();
                
                if (stageServices.Count == 0)
                    continue;

                _logger.LogInformation("Stage {Stage}: Executing {Count} services", stage, stageServices.Count);

                var readyServices = stageServices.Where(s => AreDependenciesMet(s, serviceResults)).ToList();
                var results = await ExecuteServicesAsync(
                    readyServices, context, config, ct).ConfigureAwait(false);

                foreach (var result in results)
                {
                    serviceResults[result.ServiceId] = result;
                    completedServices++;

                    if (result.FromCache)
                        cacheHits++;

                    if (!result.Success)
                    {
                        var service = stageServices.First(s => s.ServiceId == result.ServiceId);
                        if (service.IsRequired)
                        {
                            errors.Add($"Required service {service.Name} failed: {result.ErrorMessage}");
                            _logger.LogError("Required service {Service} failed: {Error}", service.Name, result.ErrorMessage);
                        }
                        else
                        {
                            warnings.Add($"Optional service {service.Name} failed: {result.ErrorMessage}");
                            _logger.LogWarning("Optional service {Service} failed: {Error}", service.Name, result.ErrorMessage);
                        }
                    }

                    progress?.Report(new PipelineProgress
                    {
                        CurrentStage = stage,
                        CurrentService = result.ServiceId,
                        CompletedServices = completedServices,
                        TotalServices = services.Count,
                        PercentComplete = (double)completedServices / services.Count * 100
                    });
                }

                if (results.Count > 1)
                    parallelExecutions += results.Count - 1;

                stageStopwatch.Stop();
                stageTimings[stage] = stageStopwatch.Elapsed;

                _logger.LogInformation(
                    "Stage {Stage} completed in {Duration}ms",
                    stage,
                    stageStopwatch.ElapsedMilliseconds);

                var hasRequiredFailures = results
                    .Where(r => !r.Success)
                    .Any(r => stageServices.First(s => s.ServiceId == r.ServiceId).IsRequired);

                if (hasRequiredFailures && !config.ContinueOnOptionalFailure)
                {
                    _logger.LogError("Required service failure in stage {Stage}, stopping pipeline", stage);
                    break;
                }
            }

            overallStopwatch.Stop();

            var success = errors.Count == 0;
            
            _logger.LogInformation(
                "Pipeline orchestration completed. Success: {Success}, Duration: {Duration}ms, Cache hits: {CacheHits}, Parallel: {Parallel}",
                success,
                overallStopwatch.ElapsedMilliseconds,
                cacheHits,
                parallelExecutions);

            LogPipelineSummary(stageTimings, serviceResults);

            return new PipelineExecutionResult
            {
                Success = success,
                ServiceResults = serviceResults,
                TotalExecutionTime = overallStopwatch.Elapsed,
                StageTimings = stageTimings,
                Warnings = warnings,
                Errors = errors,
                CacheHits = cacheHits,
                ParallelExecutions = parallelExecutions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline orchestration failed with exception");
            errors.Add($"Pipeline exception: {ex.Message}");

            return new PipelineExecutionResult
            {
                Success = false,
                ServiceResults = serviceResults,
                TotalExecutionTime = overallStopwatch.Elapsed,
                StageTimings = stageTimings,
                Warnings = warnings,
                Errors = errors,
                CacheHits = cacheHits,
                ParallelExecutions = parallelExecutions
            };
        }
    }

    /// <summary>
    /// Builds the pipeline service dependency graph
    /// </summary>
    private List<PipelineService> BuildPipelineServices()
    {
        var services = new List<PipelineService>();

        services.Add(new PipelineService
        {
            ServiceId = "script_generation",
            Name = "Script Generation",
            Stage = PipelineStage.ScriptGeneration,
            IsRequired = true,
            DependsOn = new List<string>(),
            Priority = 1
        });

        if (_scriptRefinement != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "script_refinement",
                Name = "Script Refinement (Multi-pass)",
                Stage = PipelineStage.ScriptGeneration,
                IsRequired = false,
                DependsOn = new List<string> { "script_generation" },
                Priority = 2
            });
        }

        services.Add(new PipelineService
        {
            ServiceId = "scene_parsing",
            Name = "Scene Parsing",
            Stage = PipelineStage.ScriptAnalysis,
            IsRequired = true,
            DependsOn = new List<string> { "script_generation" },
            Priority = 1
        });

        if (_contentAdvisor != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "quality_analysis",
                Name = "Content Quality Analysis",
                Stage = PipelineStage.ScriptAnalysis,
                IsRequired = false,
                DependsOn = new List<string> { "scene_parsing" },
                Priority = 2
            });
        }

        if (_narrativeAnalyzer != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "narrative_coherence",
                Name = "Narrative Flow Analysis",
                Stage = PipelineStage.ScriptAnalysis,
                IsRequired = false,
                DependsOn = new List<string> { "scene_parsing" },
                Priority = 2
            });
        }

        services.Add(new PipelineService
        {
            ServiceId = "scene_importance",
            Name = "Scene Importance Analysis",
            Stage = PipelineStage.ScriptOptimization,
            IsRequired = true,
            DependsOn = new List<string> { "scene_parsing" },
            Priority = 1
        });

        if (_pacingOptimizer != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "pacing_optimization",
                Name = "Intelligent Pacing Optimization",
                Stage = PipelineStage.ScriptOptimization,
                IsRequired = false,
                DependsOn = new List<string> { "scene_importance" },
                Priority = 2
            });
        }

        if (_toneEnforcer != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "tone_consistency",
                Name = "Tone Consistency Enforcement",
                Stage = PipelineStage.ScriptOptimization,
                IsRequired = false,
                DependsOn = new List<string> { "scene_importance" },
                Priority = 2
            });
        }

        if (_visualPromptService != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "visual_prompt_generation",
                Name = "Visual Prompt Generation",
                Stage = PipelineStage.VisualPlanning,
                IsRequired = false,
                DependsOn = new List<string> { "script_generation", "scene_importance" },
                Priority = 1
            });
        }

        if (_visualAlignmentService != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "visual_text_alignment",
                Name = "Visual-Text Alignment",
                Stage = PipelineStage.VisualPlanning,
                IsRequired = false,
                DependsOn = new List<string> { "visual_prompt_generation" },
                Priority = 2
            });
        }

        if (_narrationOptimizer != null)
        {
            services.Add(new PipelineService
            {
                ServiceId = "narration_optimization",
                Name = "Narration Optimization",
                Stage = PipelineStage.NarrationOptimization,
                IsRequired = false,
                DependsOn = new List<string> { "script_generation", "visual_prompt_generation" },
                Priority = 1
            });
        }

        return services;
    }

    /// <summary>
    /// Checks if all dependencies for a service are met
    /// </summary>
    private bool AreDependenciesMet(PipelineService service, Dictionary<string, ServiceExecutionResult> results)
    {
        foreach (var dependency in service.DependsOn)
        {
            if (!results.TryGetValue(dependency, out var depResult) || !depResult.Success)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Executes services in parallel where possible
    /// </summary>
    private async Task<List<ServiceExecutionResult>> ExecuteServicesAsync(
        List<PipelineService> services,
        PipelineExecutionContext context,
        PipelineConfiguration config,
        CancellationToken ct)
    {
        if (!config.EnableParallelExecution || services.Count == 1)
        {
            var results = new List<ServiceExecutionResult>();
            foreach (var service in services.OrderBy(s => s.Priority))
            {
                var result = await ExecuteServiceAsync(service, context, config, ct).ConfigureAwait(false);
                results.Add(result);
            }
            return results;
        }

        var tasks = services.Select(s => ExecuteServiceAsync(s, context, config, ct));
        var results2 = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results2.ToList();
    }

    /// <summary>
    /// Executes a single service with caching and error handling
    /// </summary>
    private async Task<ServiceExecutionResult> ExecuteServiceAsync(
        PipelineService service,
        PipelineExecutionContext context,
        PipelineConfiguration config,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        service.Status = ServiceExecutionStatus.Running;
        service.StartTime = DateTime.UtcNow;

        _logger.LogDebug("Executing service: {Service}", service.Name);

        try
        {
            var cacheKey = GenerateCacheKey(service, context);

            if (config.EnableCaching && _cache.TryGet<object>(cacheKey, out var cachedValue))
            {
                stopwatch.Stop();
                service.Status = ServiceExecutionStatus.Completed;
                service.EndTime = DateTime.UtcNow;

                _logger.LogInformation("Service {Service} completed from cache in {Duration}ms",
                    service.Name, stopwatch.ElapsedMilliseconds);

                return new ServiceExecutionResult
                {
                    ServiceId = service.ServiceId,
                    Success = true,
                    Result = cachedValue,
                    ErrorMessage = null,
                    ExecutionTime = stopwatch.Elapsed,
                    FromCache = true
                };
            }

            object? result = service.ServiceId switch
            {
                "script_generation" => await ExecuteScriptGenerationAsync(context, ct).ConfigureAwait(false),
                "script_refinement" => await ExecuteScriptRefinementAsync(context, ct).ConfigureAwait(false),
                "scene_parsing" => await ExecuteSceneParsingAsync(context, ct).ConfigureAwait(false),
                "quality_analysis" => await ExecuteQualityAnalysisAsync(context, ct).ConfigureAwait(false),
                "narrative_coherence" => await ExecuteNarrativeAnalysisAsync(context, ct).ConfigureAwait(false),
                "scene_importance" => await ExecuteSceneImportanceAsync(context, ct).ConfigureAwait(false),
                "pacing_optimization" => await ExecutePacingOptimizationAsync(context, ct).ConfigureAwait(false),
                "tone_consistency" => await ExecuteToneConsistencyAsync(context, ct).ConfigureAwait(false),
                "visual_prompt_generation" => await ExecuteVisualPromptGenerationAsync(context, ct).ConfigureAwait(false),
                "visual_text_alignment" => await ExecuteVisualAlignmentAsync(context, ct).ConfigureAwait(false),
                "narration_optimization" => await ExecuteNarrationOptimizationAsync(context, ct).ConfigureAwait(false),
                _ => throw new NotSupportedException($"Service {service.ServiceId} not implemented")
            };

            if (config.EnableCaching && result != null)
            {
                _cache.Set(cacheKey, result, config.CacheTtl);
            }

            stopwatch.Stop();
            service.Status = ServiceExecutionStatus.Completed;
            service.EndTime = DateTime.UtcNow;

            _logger.LogInformation("Service {Service} completed successfully in {Duration}ms",
                service.Name, stopwatch.ElapsedMilliseconds);

            return new ServiceExecutionResult
            {
                ServiceId = service.ServiceId,
                Success = true,
                Result = result,
                ErrorMessage = null,
                ExecutionTime = stopwatch.Elapsed,
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            service.Status = ServiceExecutionStatus.Failed;
            service.EndTime = DateTime.UtcNow;
            service.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Service {Service} failed after {Duration}ms",
                service.Name, stopwatch.ElapsedMilliseconds);

            return new ServiceExecutionResult
            {
                ServiceId = service.ServiceId,
                Success = false,
                Result = null,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                FromCache = false
            };
        }
    }

    private string GenerateCacheKey(PipelineService service, PipelineExecutionContext context)
    {
        return _cache.GenerateKey(
            service.ServiceId,
            context.Brief.Topic,
            context.Brief.Audience,
            context.Brief.Language,
            context.PlanSpec.TargetDuration.TotalSeconds,
            context.GeneratedScript
        );
    }

    private async Task<string> ExecuteScriptGenerationAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var script = await _llmProvider.DraftScriptAsync(context.Brief, context.PlanSpec, ct).ConfigureAwait(false);
            context.GeneratedScript = script;
            return script;
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private async Task<string> ExecuteScriptRefinementAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_scriptRefinement == null || context.GeneratedScript == null)
            throw new InvalidOperationException("Script refinement not available or no script to refine");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var config = new ScriptRefinementConfig
            {
                MaxRefinementPasses = 2,
                QualityThreshold = 75.0
            };

            var result = await _scriptRefinement.RefineScriptAsync(
                context.Brief, context.PlanSpec, config, ct).ConfigureAwait(false);

            if (result.Success && !string.IsNullOrEmpty(result.FinalScript))
            {
                context.GeneratedScript = result.FinalScript;
                return result.FinalScript;
            }

            return context.GeneratedScript;
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private Task<List<Scene>> ExecuteSceneParsingAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (context.GeneratedScript == null)
            throw new InvalidOperationException("No script available to parse");

        var scenes = ParseScriptIntoScenes(context.GeneratedScript, context.PlanSpec.TargetDuration);
        context.ParsedScenes = scenes;
        return Task.FromResult(scenes);
    }

    private async Task<ContentQualityAnalysis> ExecuteQualityAnalysisAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_contentAdvisor == null || context.GeneratedScript == null)
            throw new InvalidOperationException("Content advisor not available or no script to analyze");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _contentAdvisor.AnalyzeContentQualityAsync(
                context.GeneratedScript, context.Brief, context.PlanSpec, ct).ConfigureAwait(false);
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private async Task<object> ExecuteNarrativeAnalysisAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_narrativeAnalyzer == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Narrative analyzer not available or no scenes to analyze");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _narrativeAnalyzer.AnalyzeNarrativeFlowAsync(
                context.ParsedScenes,
                context.Brief.Goal ?? "informative video",
                context.PlanSpec.Style,
                ct).ConfigureAwait(false);
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private Task<Dictionary<int, double>> ExecuteSceneImportanceAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (context.ParsedScenes == null)
            throw new InvalidOperationException("No scenes available for importance analysis");

        var importance = context.ParsedScenes
            .ToDictionary(s => s.Index, s => 1.0);

        return Task.FromResult(importance);
    }

    private async Task<object> ExecutePacingOptimizationAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_pacingOptimizer == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Pacing optimizer not available or no scenes to optimize");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _pacingOptimizer.OptimizePacingAsync(
                context.ParsedScenes,
                context.Brief,
                _llmProvider,
                true,
                PacingProfile.BalancedDocumentary,
                ct).ConfigureAwait(false);
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private async Task<object> ExecuteToneConsistencyAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_toneEnforcer == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Tone enforcer not available or no scenes to check");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var toneProfile = await _toneEnforcer.ExpandToneProfileAsync(
                context.Brief.Tone,
                ct).ConfigureAwait(false);

            var scores = new List<object>();
            foreach (var scene in context.ParsedScenes)
            {
                var score = await _toneEnforcer.ValidateScriptToneAsync(
                    scene.Script,
                    toneProfile,
                    scene.Index,
                    ct).ConfigureAwait(false);
                scores.Add(score);
            }

            return scores;
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private async Task<object> ExecuteVisualPromptGenerationAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_visualPromptService == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Visual prompt service not available or no scenes");

        await _llmSemaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _visualPromptService.GenerateVisualPromptsAsync(
                context.ParsedScenes,
                context.Brief,
                _llmProvider,
                null,
                ct).ConfigureAwait(false);
        }
        finally
        {
            _llmSemaphore.Release();
        }
    }

    private Task<object> ExecuteVisualAlignmentAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_visualAlignmentService == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Visual alignment service not available or no scenes");

        var result = new { Aligned = true, Scenes = context.ParsedScenes.Count };
        return Task.FromResult<object>(result);
    }

    private async Task<object> ExecuteNarrationOptimizationAsync(PipelineExecutionContext context, CancellationToken ct)
    {
        if (_narrationOptimizer == null || context.ParsedScenes == null)
            throw new InvalidOperationException("Narration optimizer not available or no scenes");

        var scriptLines = context.ParsedScenes.Select(s => new ScriptLine(
            s.Index, s.Script, s.Start, s.Duration)).ToList();

        var config = new Models.Audio.NarrationOptimizationConfig();

        return await _narrationOptimizer.OptimizeForTtsAsync(
            scriptLines,
            context.VoiceSpec,
            null,
            config,
            ct).ConfigureAwait(false);
    }

    private List<Scene> ParseScriptIntoScenes(string script, TimeSpan targetDuration)
    {
        var scenes = new List<Scene>();
        var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? currentHeading = null;
        var currentScriptLines = new List<string>();
        int sceneIndex = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (currentHeading != null && currentScriptLines.Count > 0)
                {
                    var sceneScript = string.Join("\n", currentScriptLines);
                    scenes.Add(new Scene(sceneIndex++, currentHeading, sceneScript, TimeSpan.Zero, TimeSpan.Zero));
                    currentScriptLines.Clear();
                }
                currentHeading = line.Substring(3).Trim();
            }
            else if (!line.StartsWith("#", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(line))
            {
                currentScriptLines.Add(line);
            }
        }

        if (currentHeading != null && currentScriptLines.Count > 0)
        {
            var sceneScript = string.Join("\n", currentScriptLines);
            scenes.Add(new Scene(sceneIndex++, currentHeading, sceneScript, TimeSpan.Zero, TimeSpan.Zero));
        }

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

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private void LogPipelineSummary(
        Dictionary<PipelineStage, TimeSpan> stageTimings,
        Dictionary<string, ServiceExecutionResult> serviceResults)
    {
        _logger.LogInformation("=== Pipeline Execution Summary ===");
        
        foreach (var stage in stageTimings.OrderBy(kvp => kvp.Key))
        {
            _logger.LogInformation("  {Stage}: {Duration}ms", stage.Key, stage.Value.TotalMilliseconds);
        }

        var successCount = serviceResults.Count(kvp => kvp.Value.Success);
        var failureCount = serviceResults.Count - successCount;
        var cacheCount = serviceResults.Count(kvp => kvp.Value.FromCache);

        _logger.LogInformation("Services: {Success} succeeded, {Failed} failed, {Cached} from cache",
            successCount, failureCount, cacheCount);

        var bottlenecks = serviceResults
            .OrderByDescending(kvp => kvp.Value.ExecutionTime)
            .Take(3)
            .ToList();

        _logger.LogInformation("Top 3 slowest services:");
        foreach (var kvp in bottlenecks)
        {
            _logger.LogInformation("  {Service}: {Duration}ms", kvp.Key, kvp.Value.ExecutionTime.TotalMilliseconds);
        }
    }
}
