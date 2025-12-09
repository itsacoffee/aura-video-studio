using System;
using System.Collections.Generic;
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
/// Stage 4: Visual asset generation
/// Generates or fetches images/video clips for each scene
/// </summary>
public class VisualsStage : PipelineStage
{
    private const int SceneTimeoutSeconds = 45;

    private readonly IImageProvider? _imageProvider;
    private readonly ImageOutputValidator _imageValidator;
    private readonly ProviderRetryWrapper _retryWrapper;
    private readonly ResourceCleanupManager _cleanupManager;

    public VisualsStage(
        ILogger<VisualsStage> logger,
        ImageOutputValidator imageValidator,
        ProviderRetryWrapper retryWrapper,
        ResourceCleanupManager cleanupManager,
        IImageProvider? imageProvider = null) : base(logger)
    {
        _imageProvider = imageProvider;
        _imageValidator = imageValidator ?? throw new ArgumentNullException(nameof(imageValidator));
        _retryWrapper = retryWrapper ?? throw new ArgumentNullException(nameof(retryWrapper));
        _cleanupManager = cleanupManager ?? throw new ArgumentNullException(nameof(cleanupManager));
    }

    public override string StageName => "Visuals";
    public override string DisplayName => "Visual Generation";
    public override int ProgressWeight => 30;
    public override TimeSpan Timeout => TimeSpan.FromMinutes(5);

    protected override async Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct)
    {
        ReportProgress(progress, 5, "Preparing visual asset generation...");

        // Get parsed scenes from context
        if (context.ParsedScenes == null || context.ParsedScenes.Count == 0)
        {
            throw new InvalidOperationException("Scenes must be parsed before visual generation");
        }

        var scenes = context.ParsedScenes;
        var sceneAssets = new Dictionary<int, IReadOnlyList<Asset>>();

        Logger.LogInformation(
            "[{CorrelationId}] Generating visuals for {SceneCount} scenes using provider: {Provider}",
            context.CorrelationId,
            scenes.Count,
            _imageProvider?.GetType().Name ?? "None");

        // If no image provider is available, skip visual generation with clear message
        if (_imageProvider == null)
        {
            Logger.LogWarning(
                "[{CorrelationId}] No image provider available (IImageProvider not registered). " +
                "This typically means no stock image API keys are configured. " +
                "Configure Pexels, Unsplash, or Pixabay API keys in Settings to enable stock image generation.",
                context.CorrelationId);

            ReportProgress(progress, 100, "No image provider available - configure stock image API keys in Settings");

            context.SceneAssets = sceneAssets;
            context.SetStageOutput(StageName, new VisualsStageOutput
            {
                SceneAssets = sceneAssets,
                TotalAssetsGenerated = 0,
                SkippedScenes = scenes.Count,
                FailureReason = "No image provider available. Configure Pexels, Unsplash, or Pixabay API keys in Settings.",
                GeneratedAt = DateTime.UtcNow
            });

            return;
        }

        // Generate visuals for each scene
        int completedScenes = 0;
        int failedScenes = 0;
        int totalAssets = 0;

        var visualSpec = new VisualSpec(
            context.PlanSpec.Style,
            context.Brief.Aspect,
            Array.Empty<string>());

        foreach (var scene in scenes)
        {
            var scenePercent = (int)((completedScenes / (double)scenes.Count) * 80) + 10;
            ReportProgress(
                progress,
                scenePercent,
                $"Generating visuals for scene {scene.Index + 1}/{scenes.Count}...",
                scene.Index + 1,
                scenes.Count);

            try
            {
                using var sceneCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                sceneCts.CancelAfter(TimeSpan.FromSeconds(SceneTimeoutSeconds));

                var assets = await _retryWrapper.ExecuteWithRetryAsync(
                    async (ctRetry) =>
                    {
                        var generatedAssets = await _imageProvider.FetchOrGenerateAsync(
                            scene,
                            visualSpec,
                            ctRetry).ConfigureAwait(false);

                        // Validate image assets (but be lenient)
                        var imageValidation = _imageValidator.ValidateImageAssets(
                            generatedAssets,
                            expectedMinCount: 1);

                        if (!imageValidation.IsValid)
                        {
                            Logger.LogWarning(
                                "[{CorrelationId}] Image validation failed for scene {SceneIndex}: {Issues}",
                                context.CorrelationId,
                                scene.Index,
                                string.Join(", ", imageValidation.Issues));

                            // For image generation, be lenient and allow empty assets
                            if (generatedAssets.Count == 0)
                            {
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
                    $"Image Generation (Scene {scene.Index})",
                    sceneCts.Token,
                    maxRetries: 2
                ).ConfigureAwait(false);

                sceneAssets[scene.Index] = assets;
                totalAssets += assets.Count;
                completedScenes++;

                Logger.LogDebug(
                    "[{CorrelationId}] Generated {Count} assets for scene {Index}",
                    context.CorrelationId,
                    assets.Count,
                    scene.Index);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException && ct.IsCancellationRequested)
                {
                    throw;
                }

                var timedOut = ex is OperationCanceledException;

                // Log with more context for debugging
                Logger.LogWarning(
                    ex,
                    "[{CorrelationId}] Failed to generate visuals for scene {SceneIndex} (heading: '{Heading}'). " +
                    "Provider: {Provider}. Error: {ErrorMessage}. Continuing with empty assets. TimedOut={TimedOut}",
                    context.CorrelationId,
                    scene.Index,
                    scene.Heading ?? "unknown",
                    _imageProvider?.GetType().Name ?? "null",
                    ex.Message,
                    timedOut);

                sceneAssets[scene.Index] = Array.Empty<Asset>();
                failedScenes++;
                completedScenes++;
            }
        }

        ReportProgress(progress, 95, "Visual generation completed");

        // Provide clear summary of results
        if (failedScenes > 0)
        {
            Logger.LogWarning(
                "[{CorrelationId}] Visual generation completed with {FailedScenes} failed scenes. " +
                "Successfully generated {TotalAssets} assets for {SuccessScenes} scenes. " +
                "Failed scenes will use placeholder images during rendering.",
                context.CorrelationId,
                failedScenes,
                totalAssets,
                completedScenes - failedScenes);
        }
        else
        {
            Logger.LogInformation(
                "[{CorrelationId}] Visual generation completed successfully: {TotalAssets} assets for {CompletedScenes} scenes",
                context.CorrelationId,
                totalAssets,
                completedScenes);
        }

        // Store scene assets in context
        context.SceneAssets = sceneAssets;
        context.SetStageOutput(StageName, new VisualsStageOutput
        {
            SceneAssets = sceneAssets,
            TotalAssetsGenerated = totalAssets,
            SkippedScenes = failedScenes,
            Provider = _imageProvider.GetType().Name,
            GeneratedAt = DateTime.UtcNow
        });

        // Write to channel for downstream consumers
        foreach (var kvp in sceneAssets)
        {
            await context.AssetChannel.Writer.WriteAsync(
                new AssetBatch
                {
                    SceneIndex = kvp.Key,
                    Assets = kvp.Value
                },
                ct).ConfigureAwait(false);
        }
        context.AssetChannel.Writer.Complete();

        ReportProgress(progress, 100, "Visuals stage completed");
    }

    protected override bool CanSkipStage(PipelineContext context)
    {
        return context.SceneAssets != null && context.SceneAssets.Count > 0;
    }

    protected override int GetItemsProcessed(PipelineContext context)
    {
        return context.SceneAssets?.Values.Sum(assets => assets.Count) ?? 0;
    }
}

/// <summary>
/// Output from the Visuals stage
/// </summary>
public record VisualsStageOutput
{
    public required Dictionary<int, IReadOnlyList<Asset>> SceneAssets { get; init; }
    public required int TotalAssetsGenerated { get; init; }
    public required int SkippedScenes { get; init; }
    public string? Provider { get; init; }
    public string? FailureReason { get; init; }
    public required DateTime GeneratedAt { get; init; }
}
