using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Providers;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Narrative;
using Aura.Core.Services.PacingServices;
using Aura.Core.Services.Quality;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Health check service for validating pipeline dependencies before execution
/// </summary>
public class PipelineHealthCheck
{
    private readonly ILogger<PipelineHealthCheck> _logger;
    private readonly ILlmProvider? _llmProvider;
    private readonly ITtsProvider? _ttsProvider;
    private readonly IntelligentContentAdvisor? _contentAdvisor;
    private readonly NarrativeFlowAnalyzer? _narrativeAnalyzer;
    private readonly IntelligentPacingOptimizer? _pacingOptimizer;
    private readonly ToneConsistencyEnforcer? _toneEnforcer;
    private readonly VisualPromptGenerationService? _visualPromptService;
    private readonly VisualTextAlignmentService? _visualAlignmentService;
    private readonly NarrationOptimizationService? _narrationOptimizer;
    private readonly ScriptRefinementOrchestrator? _scriptRefinement;

    public PipelineHealthCheck(
        ILogger<PipelineHealthCheck> logger,
        ILlmProvider? llmProvider = null,
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
        _contentAdvisor = contentAdvisor;
        _narrativeAnalyzer = narrativeAnalyzer;
        _pacingOptimizer = pacingOptimizer;
        _toneEnforcer = toneEnforcer;
        _visualPromptService = visualPromptService;
        _visualAlignmentService = visualAlignmentService;
        _narrationOptimizer = narrationOptimizer;
        _scriptRefinement = scriptRefinement;
    }

    /// <summary>
    /// Checks if all required services are available
    /// </summary>
    public async Task<PipelineHealthCheckResult> CheckHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting pipeline health check");

        var availability = new Dictionary<string, bool>();
        var missing = new List<string>();
        var warnings = new List<string>();

        await Task.CompletedTask;

        availability["LlmProvider"] = _llmProvider != null;
        if (_llmProvider == null)
        {
            missing.Add("LlmProvider is required for script generation");
        }

        availability["TtsProvider"] = _ttsProvider != null;
        if (_ttsProvider == null)
        {
            missing.Add("TtsProvider is required for audio generation");
        }

        availability["ContentAdvisor"] = _contentAdvisor != null;
        if (_contentAdvisor == null)
        {
            warnings.Add("IntelligentContentAdvisor not available - quality analysis will be skipped");
        }

        availability["NarrativeAnalyzer"] = _narrativeAnalyzer != null;
        if (_narrativeAnalyzer == null)
        {
            warnings.Add("NarrativeFlowAnalyzer not available - narrative coherence check will be skipped");
        }

        availability["PacingOptimizer"] = _pacingOptimizer != null;
        if (_pacingOptimizer == null)
        {
            warnings.Add("IntelligentPacingOptimizer not available - pacing optimization will be skipped");
        }

        availability["ToneEnforcer"] = _toneEnforcer != null;
        if (_toneEnforcer == null)
        {
            warnings.Add("ToneConsistencyEnforcer not available - tone consistency check will be skipped");
        }

        availability["VisualPromptService"] = _visualPromptService != null;
        if (_visualPromptService == null)
        {
            warnings.Add("VisualPromptGenerationService not available - visual prompts will use basic generation");
        }

        availability["VisualAlignmentService"] = _visualAlignmentService != null;
        if (_visualAlignmentService == null)
        {
            warnings.Add("VisualTextAlignmentService not available - visual-text synchronization will be skipped");
        }

        availability["NarrationOptimizer"] = _narrationOptimizer != null;
        if (_narrationOptimizer == null)
        {
            warnings.Add("NarrationOptimizationService not available - narration optimization will be skipped");
        }

        availability["ScriptRefinement"] = _scriptRefinement != null;
        if (_scriptRefinement == null)
        {
            warnings.Add("ScriptRefinementOrchestrator not available - multi-pass refinement will be skipped");
        }

        var isHealthy = missing.Count == 0;

        _logger.LogInformation(
            "Pipeline health check complete. Healthy: {Healthy}, Available: {Available}/{Total}, Missing: {Missing}",
            isHealthy,
            availability.Count(kvp => kvp.Value),
            availability.Count,
            missing.Count);

        return new PipelineHealthCheckResult
        {
            IsHealthy = isHealthy,
            ServiceAvailability = availability,
            MissingRequiredServices = missing,
            Warnings = warnings
        };
    }
}
