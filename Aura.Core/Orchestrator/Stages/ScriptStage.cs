using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// Stage 2: Script generation and validation
/// Generates the video script using LLM and validates quality
/// </summary>
public class ScriptStage : PipelineStage
{
    private readonly ILlmProvider _llmProvider;
    private readonly ScriptValidator _scriptValidator;
    private readonly LlmOutputValidator _llmValidator;
    private readonly ProviderRetryWrapper _retryWrapper;

    public ScriptStage(
        ILogger<ScriptStage> logger,
        ILlmProvider llmProvider,
        ScriptValidator scriptValidator,
        LlmOutputValidator llmValidator,
        ProviderRetryWrapper retryWrapper) : base(logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _scriptValidator = scriptValidator ?? throw new ArgumentNullException(nameof(scriptValidator));
        _llmValidator = llmValidator ?? throw new ArgumentNullException(nameof(llmValidator));
        _retryWrapper = retryWrapper ?? throw new ArgumentNullException(nameof(retryWrapper));
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

        // Generate script with retry logic
        var script = await _retryWrapper.ExecuteWithRetryAsync(
            async (ctRetry) =>
            {
                ReportProgress(progress, 40, "Calling LLM provider...");
                
                var generatedScript = await _llmProvider.DraftScriptAsync(
                    context.Brief,
                    context.PlanSpec,
                    ctRetry).ConfigureAwait(false);

                ReportProgress(progress, 60, "Validating script structure...");

                // Validate script structure and content
                var structuralValidation = _scriptValidator.Validate(generatedScript, context.PlanSpec);
                var contentValidation = _llmValidator.ValidateScriptContent(generatedScript, context.PlanSpec);

                if (!structuralValidation.IsValid || !contentValidation.IsValid)
                {
                    var allIssues = structuralValidation.Issues
                        .Concat(contentValidation.Issues)
                        .ToList();
                    
                    Logger.LogWarning(
                        "[{CorrelationId}] Script validation failed: {Issues}",
                        context.CorrelationId,
                        string.Join(", ", allIssues));
                    
                    throw new Validation.ValidationException("Script quality validation failed", allIssues);
                }

                return generatedScript;
            },
            "Script Generation",
            ct,
            maxRetries: 2
        ).ConfigureAwait(false);

        ReportProgress(progress, 80, "Script generated successfully");

        Logger.LogInformation(
            "[{CorrelationId}] Script generated and validated: {Length} characters",
            context.CorrelationId,
            script.Length);

        // Store script in context
        context.GeneratedScript = script;
        context.SetStageOutput(StageName, new ScriptStageOutput
        {
            Script = script,
            GeneratedAt = DateTime.UtcNow,
            Provider = _llmProvider.GetType().Name,
            CharacterCount = script.Length
        });

        // Write to channel for downstream consumers
        await context.ScriptChannel.Writer.WriteAsync(script, ct).ConfigureAwait(false);
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
}
