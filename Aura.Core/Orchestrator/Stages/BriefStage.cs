using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// Stage 1: Brief validation and preprocessing
/// Validates the input brief and prepares it for script generation
/// </summary>
public class BriefStage : PipelineStage
{
    private readonly PreGenerationValidator _validator;

    public BriefStage(
        ILogger<BriefStage> logger,
        PreGenerationValidator validator) : base(logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public override string StageName => "Brief";
    public override string DisplayName => "Brief Validation";
    public override int ProgressWeight => 5;
    public override TimeSpan Timeout => TimeSpan.FromSeconds(30);
    public override bool SupportsRetry => false; // Validation doesn't need retry

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 5, "Starting system validation...");

        Logger.LogInformation(
            "[{CorrelationId}] Validating brief: Topic='{Topic}', Audience='{Audience}'",
            context.CorrelationId,
            context.Brief.Topic,
            context.Brief.Audience);

        // Create progress callback for validation sub-steps
        var validationProgress = new Progress<string>(message =>
        {
            ReportProgress(progress, 10, message);
            Logger.LogDebug("[{CorrelationId}] Validation: {Message}", context.CorrelationId, message);
        });

        // Validate brief with sub-step progress reporting
        var validationResult = await _validator.ValidateSystemReadyAsync(
            context.Brief,
            context.PlanSpec,
            validationProgress,
            ct).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            var issues = string.Join(", ", validationResult.Issues);
            Logger.LogError(
                "[{CorrelationId}] Brief validation failed: {Issues}",
                context.CorrelationId,
                issues);
            
            throw new Validation.ValidationException("Brief validation failed", validationResult.Issues);
        }

        ReportProgress(progress, 80, "Validation complete, finalizing...");

        // Additional brief processing can be done here
        // For example: enriching the brief with context, normalizing inputs, etc.
        
        Logger.LogInformation(
            "[{CorrelationId}] Brief validation passed. Target duration: {Duration}s",
            context.CorrelationId,
            context.PlanSpec.TargetDuration.TotalSeconds);

        ReportProgress(progress, 100, "Brief validated successfully");

        // Store validated brief in context
        context.SetStageOutput(StageName, new BriefStageOutput
        {
            ValidatedBrief = context.Brief,
            ValidationResult = validationResult,
            ProcessedAt = DateTime.UtcNow
        });
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        // Brief validation should always run
        return false;
    }
}

/// <summary>
/// Output from the Brief stage
/// </summary>
public record BriefStageOutput
{
    public required Brief ValidatedBrief { get; init; }
    public required Validation.ValidationResult ValidationResult { get; init; }
    public required DateTime ProcessedAt { get; init; }
}
