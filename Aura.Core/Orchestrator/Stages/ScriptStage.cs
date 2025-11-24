using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// Stage 2: Script generation and validation
/// Generates the video script using LLM and validates quality
/// Uses ScriptGenerationPipeline for hardened generation with retry and fallback
/// </summary>
public class ScriptStage : PipelineStage
{
    private readonly ScriptGenerationPipeline _pipeline;
    private readonly ScriptValidator _scriptValidator; // Kept for backward compatibility
    private readonly LlmOutputValidator _llmValidator; // Kept for backward compatibility

    public ScriptStage(
        ILogger<ScriptStage> logger,
        ScriptGenerationPipeline pipeline,
        ScriptValidator scriptValidator,
        LlmOutputValidator llmValidator) : base(logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _scriptValidator = scriptValidator ?? throw new ArgumentNullException(nameof(scriptValidator));
        _llmValidator = llmValidator ?? throw new ArgumentNullException(nameof(llmValidator));
    }

    public override string StageName => "Script";
    public override string DisplayName => "Script Generation";
    public override int ProgressWeight => 20;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(2);

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 10, "Generating script...");

        Logger.LogInformation(
            "[{CorrelationId}] Generating script for topic: {Topic}",
            context.CorrelationId,
            context.Brief.Topic);

        // Generate script using hardened pipeline with validation, retry, and fallback
        ReportProgress(progress, 30, "Calling script generation pipeline...");
        
        var result = await _pipeline.GenerateAsync(
            context.Brief,
            context.PlanSpec,
            ct).ConfigureAwait(false);

        ReportProgress(progress, 80, result.UsedFallback ? "Using fallback template..." : "Script generated successfully");

        // Log generation details
        if (result.Success)
        {
            Logger.LogInformation(
                "[{CorrelationId}] Script generated successfully on attempt {Attempt} with quality {Quality:F2}",
                context.CorrelationId,
                result.Attempts.Count,
                result.QualityScore);
        }
        else if (result.UsedFallback)
        {
            Logger.LogWarning(
                "[{CorrelationId}] All LLM attempts failed, used template fallback. Attempts: {Attempts}",
                context.CorrelationId,
                result.Attempts.Count);
        }

        // Log attempt details for observability
        foreach (var attempt in result.Attempts)
        {
            if (attempt.Error != null)
            {
                Logger.LogWarning(
                    "[{CorrelationId}] Attempt {Attempt} failed: {Error} (Duration: {Duration}ms)",
                    context.CorrelationId,
                    attempt.AttemptNumber,
                    attempt.Error,
                    attempt.Duration.TotalMilliseconds);
            }
            else if (attempt.Validation != null && !attempt.Validation.IsValid)
            {
                Logger.LogWarning(
                    "[{CorrelationId}] Attempt {Attempt} validation failed: {Errors} (Quality: {Quality:F2})",
                    context.CorrelationId,
                    attempt.AttemptNumber,
                    string.Join(", ", attempt.Validation.Errors),
                    attempt.Validation.QualityScore);
            }
        }

        // Store script in context
        context.GeneratedScript = result.Script;
        context.SetStageOutput(StageName, new ScriptStageOutput
        {
            Script = result.Script,
            GeneratedAt = DateTime.UtcNow,
            Provider = result.UsedFallback ? "TemplateFallback" : "LLM",
            CharacterCount = result.Script.Length,
            QualityScore = result.QualityScore,
            Metrics = result.Metrics,
            Attempts = result.Attempts.Count,
            UsedFallback = result.UsedFallback
        });

        // Write to channel for downstream consumers
        await context.ScriptChannel.Writer.WriteAsync(result.Script, ct).ConfigureAwait(false);
        context.ScriptChannel.Writer.Complete();

        ReportProgress(progress, 100, "Script stage completed");
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        return !string.IsNullOrEmpty(context.GeneratedScript);
    }

    protected override int GetItemsProcessed(PipelineContext context)
    {
        return string.IsNullOrEmpty(context.GeneratedScript) ? 0 : 1;
    }
}

/// <summary>
/// Output from the Script stage
/// </summary>
public record ScriptStageOutput
{
    public required string Script { get; init; }
    public required DateTime GeneratedAt { get; init; }
    public required string Provider { get; init; }
    public required int CharacterCount { get; init; }
    public double? QualityScore { get; init; }
    public Aura.Core.AI.Validation.ScriptSchemaValidator.ScriptMetrics? Metrics { get; init; }
    public int Attempts { get; init; }
    public bool UsedFallback { get; init; }
}
