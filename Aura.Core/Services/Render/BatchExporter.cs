using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Export bundle containing multiple presets
/// </summary>
public record ExportBundle(
    string Name,
    string Description,
    List<ExportPreset> Presets);

/// <summary>
/// Batch export result
/// </summary>
public record BatchExportResult(
    string BundleName,
    List<ExportOutput> Outputs,
    TimeSpan TotalTime,
    bool AllSucceeded);

/// <summary>
/// Single export output
/// </summary>
public record ExportOutput(
    ExportPreset Preset,
    string OutputPath,
    bool Succeeded,
    TimeSpan RenderTime,
    long? FileSizeBytes = null,
    string? ErrorMessage = null);

/// <summary>
/// Exports a single timeline to multiple formats simultaneously
/// </summary>
public class BatchExporter
{
    private readonly ILogger<BatchExporter> _logger;
    private readonly RenderQueue _renderQueue;
    private const int MaxParallelExports = 2;

    public BatchExporter(ILogger<BatchExporter> logger, RenderQueue renderQueue)
    {
        _logger = logger;
        _renderQueue = renderQueue;
    }

    /// <summary>
    /// Exports timeline to multiple formats
    /// </summary>
    public async Task<BatchExportResult> ExportToMultipleFormatsAsync(
        EditableTimeline timeline,
        List<ExportPreset> presets,
        string outputDirectory,
        string jobId,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting batch export: {Count} formats to {Directory}",
            presets.Count, outputDirectory
        );

        Directory.CreateDirectory(outputDirectory);

        var startTime = DateTime.UtcNow;
        var outputs = new List<ExportOutput>();
        var queueIds = new List<string>();

        // Add all exports to queue
        foreach (var preset in presets)
        {
            var filename = GenerateFilename(timeline, preset, outputDirectory);
            
            var queueId = await _renderQueue.AddToQueueAsync(
                timeline,
                preset,
                filename,
                jobId,
                QueuePriority.Normal
            ).ConfigureAwait(false);

            queueIds.Add(queueId);
            
            _logger.LogInformation(
                "Added export to queue: {Preset} -> {Filename}",
                preset.Name, Path.GetFileName(filename)
            );
        }

        // Monitor queue progress
        var completed = 0;
        while (completed < queueIds.Count)
        {
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            var items = _renderQueue.GetAllItems()
                .Where(i => queueIds.Contains(i.Id))
                .ToList();

            completed = items.Count(i => 
                i.Status == QueueItemStatus.Complete || 
                i.Status == QueueItemStatus.Failed);

            // Report progress
            var overallProgress = (completed * 100) / queueIds.Count;
            progress?.Report(overallProgress);

            _logger.LogDebug("Batch export progress: {Completed}/{Total}", completed, queueIds.Count);
        }

        // Collect results
        var finalItems = _renderQueue.GetAllItems()
            .Where(i => queueIds.Contains(i.Id))
            .ToList();

        foreach (var item in finalItems)
        {
            var output = new ExportOutput(
                Preset: item.Preset,
                OutputPath: item.OutputPath,
                Succeeded: item.Status == QueueItemStatus.Complete,
                RenderTime: item.RenderTime ?? TimeSpan.Zero,
                FileSizeBytes: item.FileSizeBytes,
                ErrorMessage: item.ErrorMessage
            );

            outputs.Add(output);

            if (output.Succeeded)
            {
                _logger.LogInformation(
                    "Export succeeded: {Preset} ({Size} MB, {Time}s)",
                    output.Preset.Name,
                    output.FileSizeBytes.HasValue ? output.FileSizeBytes.Value / 1024.0 / 1024.0 : 0,
                    output.RenderTime.TotalSeconds
                );
            }
            else
            {
                _logger.LogWarning(
                    "Export failed: {Preset} - {Error}",
                    output.Preset.Name,
                    output.ErrorMessage
                );
            }
        }

        var totalTime = DateTime.UtcNow - startTime;
        var allSucceeded = outputs.All(o => o.Succeeded);

        _logger.LogInformation(
            "Batch export complete: {Succeeded}/{Total} succeeded in {Time}s",
            outputs.Count(o => o.Succeeded),
            outputs.Count,
            totalTime.TotalSeconds
        );

        return new BatchExportResult(
            BundleName: "Custom Bundle",
            Outputs: outputs,
            TotalTime: totalTime,
            AllSucceeded: allSucceeded
        );
    }

    /// <summary>
    /// Exports using a predefined bundle
    /// </summary>
    public async Task<BatchExportResult> ExportBundleAsync(
        EditableTimeline timeline,
        ExportBundle bundle,
        string outputDirectory,
        string jobId,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Exporting bundle '{Bundle}' with {Count} formats",
            bundle.Name, bundle.Presets.Count
        );

        var result = await ExportToMultipleFormatsAsync(
            timeline,
            bundle.Presets,
            outputDirectory,
            jobId,
            progress,
            cancellationToken
        ).ConfigureAwait(false);

        return result with { BundleName = bundle.Name };
    }

    /// <summary>
    /// Gets predefined export bundles
    /// </summary>
    public static List<ExportBundle> GetPredefinedBundles()
    {
        return new List<ExportBundle>
        {
            new ExportBundle(
                Name: "Social Media Pack",
                Description: "YouTube, Instagram Feed, TikTok - covers major platforms",
                Presets: new List<ExportPreset>
                {
                    ExportPresets.YouTube1080p,
                    ExportPresets.InstagramFeed,
                    ExportPresets.TikTok
                }
            ),
            new ExportBundle(
                Name: "Complete Social Suite",
                Description: "All major social media platforms",
                Presets: new List<ExportPreset>
                {
                    ExportPresets.YouTube1080p,
                    ExportPresets.InstagramFeed,
                    ExportPresets.InstagramStory,
                    ExportPresets.TikTok,
                    ExportPresets.Facebook,
                    ExportPresets.Twitter
                }
            ),
            new ExportBundle(
                Name: "YouTube Package",
                Description: "Multiple YouTube qualities",
                Presets: new List<ExportPreset>
                {
                    ExportPresets.YouTube4K,
                    ExportPresets.YouTube1080p
                }
            ),
            new ExportBundle(
                Name: "Instagram Bundle",
                Description: "Feed and Story formats",
                Presets: new List<ExportPreset>
                {
                    ExportPresets.InstagramFeed,
                    ExportPresets.InstagramStory
                }
            ),
            new ExportBundle(
                Name: "Professional Package",
                Description: "High quality for client delivery",
                Presets: new List<ExportPreset>
                {
                    ExportPresets.MasterArchive,
                    ExportPresets.YouTube1080p,
                    ExportPresets.DraftPreview
                }
            )
        };
    }

    private string GenerateFilename(EditableTimeline timeline, ExportPreset preset, string outputDirectory)
    {
        // Generate filename based on preset and timestamp
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var presetSlug = preset.Name.Replace(" ", "_").Replace("/", "_").ToLowerInvariant();
        
        var filename = $"export_{presetSlug}_{timestamp}.{preset.Container}";
        return Path.Combine(outputDirectory, filename);
    }
}
