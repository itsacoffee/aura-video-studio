using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Orchestration;
using Microsoft.Extensions.Logging;
using TimelineRecord = Aura.Core.Providers.Timeline;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// Stage 5: Video composition and rendering
/// Combines narration, visuals, and other assets into final video
/// </summary>
public class CompositionStage : PipelineStage
{
    private readonly IVideoComposer _videoComposer;
    private readonly CompositionValidator? _compositionValidator;

    public CompositionStage(
        ILogger<CompositionStage> logger,
        IVideoComposer videoComposer,
        CompositionValidator? compositionValidator = null) : base(logger)
    {
        _videoComposer = videoComposer ?? throw new ArgumentNullException(nameof(videoComposer));
        _compositionValidator = compositionValidator;
    }

    public override string StageName => "Composition";
    public override string DisplayName => "Video Composition";
    public override int ProgressWeight => 20;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(10);
    public override bool SupportsRetry => false; // Video rendering should not be retried automatically

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 5, "Preparing video composition...");

        // Validate all required assets are available
        if (context.ParsedScenes == null || context.ParsedScenes.Count == 0)
        {
            throw new InvalidOperationException("Scenes must be parsed before composition");
        }

        if (string.IsNullOrEmpty(context.NarrationPath))
        {
            throw new InvalidOperationException("Narration must be generated before composition");
        }

        Logger.LogInformation(
            "[{CorrelationId}] Starting video composition: {SceneCount} scenes, Narration: {NarrationPath}",
            context.CorrelationId,
            context.ParsedScenes.Count,
            context.NarrationPath);

        ReportProgress(progress, 10, "Building timeline...");

        // Build timeline from context
        var timeline = new TimelineRecord(
            Scenes: context.ParsedScenes,
            SceneAssets: context.SceneAssets,
            NarrationPath: context.NarrationPath,
            MusicPath: context.MusicPath ?? string.Empty,
            SubtitlesPath: context.SubtitlesPath
        );

        // Validate composition if validator available
        if (_compositionValidator != null)
        {
            ReportProgress(progress, 15, "Validating composition...");

            var validationResult = _compositionValidator.ValidateComposition(
                context.ParsedScenes,
                context.SceneAssets,
                context.NarrationPath,
                context.MusicPath);

            if (validationResult.HasCriticalErrors)
            {
                var criticalErrors = string.Join(", ", 
                    validationResult.Errors
                        .Where(e => e.Severity == Services.Orchestration.ErrorSeverity.Critical)
                        .Select(e => e.Message));

                throw new InvalidOperationException(
                    $"Composition validation failed with critical errors: {criticalErrors}");
            }

            if (validationResult.HasErrors)
            {
                var errors = string.Join(", ",
                    validationResult.Errors
                        .Where(e => e.Severity == Services.Orchestration.ErrorSeverity.Error)
                        .Select(e => e.Message));

                Logger.LogWarning(
                    "[{CorrelationId}] Composition validation found errors: {Errors}",
                    context.CorrelationId,
                    errors);
            }

            if (validationResult.HasWarnings)
            {
                var warnings = string.Join(", ",
                    validationResult.Errors
                        .Where(e => e.Severity == Services.Orchestration.ErrorSeverity.Warning)
                        .Select(e => e.Message));

                Logger.LogInformation(
                    "[{CorrelationId}] Composition validation warnings: {Warnings}",
                    context.CorrelationId,
                    warnings);
            }

            Logger.LogInformation(
                "[{CorrelationId}] Composition validation passed: {ErrorCount} errors, {WarningCount} warnings",
                context.CorrelationId,
                validationResult.ErrorCount,
                validationResult.WarningCount);
        }

        ReportProgress(progress, 20, "Rendering video...");

        // Create progress reporter for render operation
        var renderProgress = new Progress<RenderProgress>(p =>
        {
            // Map render percentage (20-95% of this stage)
            var stagePercent = 20 + (int)(p.Percentage * 0.75);
            ReportProgress(
                progress,
                stagePercent,
                $"Rendering: {p.CurrentStage}");
        });

        // Render the final video
        var outputPath = await _videoComposer.RenderAsync(
            timeline,
            context.RenderSpec,
            renderProgress,
            ct).ConfigureAwait(false);

        if (string.IsNullOrEmpty(outputPath) || !System.IO.File.Exists(outputPath))
        {
            throw new InvalidOperationException("Video rendering failed: output file not found");
        }

        ReportProgress(progress, 95, "Video rendered successfully");

        Logger.LogInformation(
            "[{CorrelationId}] Video composition completed: {OutputPath}",
            context.CorrelationId,
            outputPath);

        // Store final video path in context
        context.FinalVideoPath = outputPath;
        context.SetStageOutput(StageName, new CompositionStageOutput
        {
            VideoPath = outputPath,
            FileSize = new System.IO.FileInfo(outputPath).Length,
            Resolution = $"{context.RenderSpec.Res.Width}x{context.RenderSpec.Res.Height}",
            Fps = context.RenderSpec.Fps,
            Codec = context.RenderSpec.Codec,
            Duration = context.PlanSpec.TargetDuration,
            RenderedAt = DateTime.UtcNow
        });

        ReportProgress(progress, 100, "Composition stage completed");
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        return !string.IsNullOrEmpty(context.FinalVideoPath) &&
               System.IO.File.Exists(context.FinalVideoPath);
    }

    protected override int GetItemsProcessed(PipelineContext context)
    {
        return string.IsNullOrEmpty(context.FinalVideoPath) ? 0 : 1;
    }
}

/// <summary>
/// Output from the Composition stage
/// </summary>
public record CompositionStageOutput
{
    public required string VideoPath { get; init; }
    public required long FileSize { get; init; }
    public required string Resolution { get; init; }
    public required int Fps { get; init; }
    public required string Codec { get; init; }
    public required TimeSpan Duration { get; init; }
    public required DateTime RenderedAt { get; init; }
}
