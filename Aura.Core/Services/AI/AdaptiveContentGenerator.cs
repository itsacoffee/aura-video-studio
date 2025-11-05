using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.ML;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AI;

/// <summary>
/// Adaptive content generator that wraps LLM providers with ML-driven optimization
/// Applies user-configured enhancements and tracks performance
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class AdaptiveContentGenerator
{
    private readonly ILogger<AdaptiveContentGenerator> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly ContentOptimizationEngine _optimizationEngine;
    private readonly DynamicPromptEnhancer _promptEnhancer;
    private readonly IntelligentContentAdvisor? _contentAdvisor;

    public AdaptiveContentGenerator(
        ILogger<AdaptiveContentGenerator> logger,
        ILlmProvider llmProvider,
        ContentOptimizationEngine optimizationEngine,
        DynamicPromptEnhancer promptEnhancer,
        IntelligentContentAdvisor? contentAdvisor = null,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
        _optimizationEngine = optimizationEngine;
        _promptEnhancer = promptEnhancer;
        _contentAdvisor = contentAdvisor;
    }

    /// <summary>
    /// Generate content with adaptive optimization
    /// </summary>
    public async Task<AdaptiveGenerationResult> GenerateContentAsync(
        Brief brief,
        PlanSpec spec,
        AIOptimizationSettings settings,
        string? profileId = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new AdaptiveGenerationResult
        {
            OriginalBrief = brief,
            OriginalSpec = spec
        };

        try
        {
            // Step 1: Optimize request based on settings
            var optimization = await _optimizationEngine.OptimizeContentRequestAsync(
                brief, spec, settings, profileId, ct);

            result.OptimizationApplied = optimization.Applied;
            result.Prediction = optimization.Prediction;

            // Check if regeneration is required
            if (optimization.RequiresRegeneration)
            {
                _logger.LogWarning("Content requires regeneration: {Reason}", optimization.Reason);
                result.Success = false;
                result.Message = optimization.Reason ?? "Quality threshold not met";
                return result;
            }

            // Use optimized brief and spec
            var workingBrief = optimization.OptimizedBrief;
            var workingSpec = optimization.OptimizedSpec;

            // Step 2: Enhance prompts if optimization is enabled
            string systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
            string userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(workingBrief, workingSpec);
            
            if (settings.Enabled)
            {
                var enhancedUserPrompt = await _promptEnhancer.EnhancePromptAsync(
                    userPrompt, workingBrief, workingSpec, settings, ct: ct);
                
                userPrompt = enhancedUserPrompt.Prompt;
                result.PromptEnhancements = enhancedUserPrompt.Enhancements;
            }

            // Step 3: Generate content using LLM provider
            _logger.LogInformation("Generating content with {Provider}", _llmProvider.GetType().Name);
            
            var generatedContent = await GenerateWithLlmAsync(workingBrief, workingSpec, ct);
            result.GeneratedContent = generatedContent;

            // Step 4: Validate quality if advisor is available
            if (_contentAdvisor != null && settings.Enabled)
            {
                var qualityAnalysis = await _contentAdvisor.AnalyzeContentQualityAsync(
                    generatedContent, workingBrief, workingSpec, ct);

                result.QualityAnalysis = qualityAnalysis;
                result.QualityScore = qualityAnalysis.OverallScore;

                _logger.LogInformation(
                    "Quality analysis: {Score:F1}/100 (threshold: {Threshold})",
                    qualityAnalysis.OverallScore, settings.MinimumQualityThreshold);

                // Check if quality meets threshold
                if (settings.AutoRegenerateIfLowQuality && 
                    qualityAnalysis.OverallScore < settings.MinimumQualityThreshold)
                {
                    _logger.LogWarning(
                        "Generated content quality ({Score:F1}) below threshold ({Threshold})",
                        qualityAnalysis.OverallScore, settings.MinimumQualityThreshold);
                    
                    result.Success = false;
                    result.Message = $"Quality score {qualityAnalysis.OverallScore:F1} below threshold {settings.MinimumQualityThreshold}";
                    result.SuggestRegeneration = true;
                    
                    // Record failure for learning
                    await RecordOutcomeAsync(false, result.QualityScore, stopwatch.Elapsed, settings, ct);
                    
                    return result;
                }
            }
            else
            {
                // No quality check, assume reasonable quality
                result.QualityScore = 75.0;
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = true;
            result.Message = "Content generated successfully";

            // Step 5: Record outcome for learning
            await RecordOutcomeAsync(true, result.QualityScore, stopwatch.Elapsed, settings, ct);

            _logger.LogInformation(
                "Content generation completed in {Duration}ms with quality {Quality:F1}",
                stopwatch.ElapsedMilliseconds, result.QualityScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during adaptive content generation");
            
            stopwatch.Stop();
            result.Success = false;
            result.Message = $"Generation failed: {ex.Message}";
            result.Duration = stopwatch.Elapsed;

            // Record failure
            await RecordOutcomeAsync(false, 0, stopwatch.Elapsed, settings, ct);

            return result;
        }
    }

    /// <summary>
    /// Record generation outcome for learning
    /// </summary>
    private async Task RecordOutcomeAsync(
        bool success,
        double qualityScore,
        TimeSpan duration,
        AIOptimizationSettings settings,
        CancellationToken ct)
    {
        try
        {
            var providerName = _llmProvider.GetType().Name
                .Replace("LlmProvider", "")
                .Replace("Provider", "");

            await _optimizationEngine.RecordGenerationOutcomeAsync(
                providerName,
                "script",
                qualityScore,
                duration,
                success,
                settings,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record generation outcome");
        }
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
        }
    }
}

/// <summary>
/// Result of adaptive content generation
/// </summary>
public record AdaptiveGenerationResult
{
    public Brief OriginalBrief { get; init; } = null!;
    public PlanSpec OriginalSpec { get; init; } = null!;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? GeneratedContent { get; set; }
    public bool OptimizationApplied { get; set; }
    public PredictionResult? Prediction { get; set; }
    public List<string>? PromptEnhancements { get; set; }
    public ContentQualityAnalysis? QualityAnalysis { get; set; }
    public double QualityScore { get; set; }
    public TimeSpan Duration { get; set; }
    public bool SuggestRegeneration { get; set; }
}
