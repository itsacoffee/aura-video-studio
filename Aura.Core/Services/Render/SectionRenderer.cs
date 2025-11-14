using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Editor;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Time range for export
/// </summary>
public record TimeRange(TimeSpan Start, TimeSpan End, int HandleFrames = 0);

/// <summary>
/// Renders specific sections of a timeline
/// </summary>
public class SectionRenderer
{
    private readonly ILogger<SectionRenderer> _logger;
    private readonly TimelineRenderer _timelineRenderer;

    public SectionRenderer(ILogger<SectionRenderer> logger, TimelineRenderer timelineRenderer)
    {
        _logger = logger;
        _timelineRenderer = timelineRenderer;
    }

    /// <summary>
    /// Exports a specific time range from the timeline
    /// </summary>
    public async Task<string> ExportRangeAsync(
        EditableTimeline timeline,
        TimeRange range,
        ExportPreset preset,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Exporting timeline range {Start} to {End} (Handles: {Handles} frames)",
            range.Start, range.End, range.HandleFrames
        );

        // Validate range
        if (range.Start < TimeSpan.Zero || range.End > timeline.TotalDuration)
        {
            throw new ArgumentException("Range is outside timeline bounds");
        }

        if (range.Start >= range.End)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        // Calculate handle duration (assuming 30fps for frame-to-time conversion)
        var handleDuration = TimeSpan.FromSeconds(range.HandleFrames / 30.0);

        // Adjust range with handles
        var effectiveStart = range.Start - handleDuration;
        var effectiveEnd = range.End + handleDuration;

        // Clamp to timeline bounds
        effectiveStart = effectiveStart < TimeSpan.Zero ? TimeSpan.Zero : effectiveStart;
        effectiveEnd = effectiveEnd > timeline.TotalDuration ? timeline.TotalDuration : effectiveEnd;

        _logger.LogDebug(
            "Effective range with handles: {Start} to {End}",
            effectiveStart, effectiveEnd
        );

        // Create trimmed timeline
        var trimmedTimeline = TrimTimeline(timeline, effectiveStart, effectiveEnd);

        _logger.LogInformation(
            "Trimmed timeline: {Scenes} scenes, duration {Duration}",
            trimmedTimeline.Scenes.Count, trimmedTimeline.TotalDuration
        );

        // Render the trimmed timeline using TimelineRenderer
        var renderSpec = ConvertPresetToRenderSpec(preset);
        await _timelineRenderer.GenerateFinalAsync(
            trimmedTimeline,
            renderSpec,
            outputPath,
            progress,
            cancellationToken).ConfigureAwait(false);

        return outputPath;
    }

    /// <summary>
    /// Converts ExportPreset to RenderSpec for TimelineRenderer
    /// </summary>
    private Models.RenderSpec ConvertPresetToRenderSpec(ExportPreset preset)
    {
        return new Models.RenderSpec(
            Res: new Models.Resolution(preset.Resolution.Width, preset.Resolution.Height),
            Container: preset.Container,
            VideoBitrateK: preset.VideoBitrate / 1000,
            AudioBitrateK: preset.AudioBitrate / 1000,
            Fps: preset.FrameRate,
            Codec: "H264",
            QualityLevel: 75
        );
    }

    /// <summary>
    /// Exports multiple ranges as separate files
    /// </summary>
    public async Task<List<string>> ExportMultipleRangesAsync(
        EditableTimeline timeline,
        List<TimeRange> ranges,
        ExportPreset preset,
        string outputDirectory,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting {Count} timeline ranges", ranges.Count);

        var outputs = new List<string>();
        var totalRanges = ranges.Count;

        for (int i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var outputPath = System.IO.Path.Combine(
                outputDirectory,
                $"clip_{i + 1:D3}_{range.Start.TotalSeconds:F0}s_{range.End.TotalSeconds:F0}s.{preset.Container}"
            );

            _logger.LogInformation(
                "Exporting range {Index}/{Total}: {Start} to {End}",
                i + 1, totalRanges, range.Start, range.End
            );

            await ExportRangeAsync(timeline, range, preset, outputPath, null, cancellationToken).ConfigureAwait(false);
            
            outputs.Add(outputPath);

            // Report overall progress
            var overallProgress = ((i + 1) * 100) / totalRanges;
            progress?.Report(overallProgress);
        }

        _logger.LogInformation("Completed exporting {Count} ranges", ranges.Count);
        return outputs;
    }

    /// <summary>
    /// Trims timeline to specified range
    /// </summary>
    private EditableTimeline TrimTimeline(EditableTimeline timeline, TimeSpan start, TimeSpan end)
    {
        var trimmedTimeline = new EditableTimeline
        {
            BackgroundMusicPath = timeline.BackgroundMusicPath,
            Subtitles = timeline.Subtitles
        };

        var duration = end - start;

        // Find scenes that overlap with the range
        foreach (var scene in timeline.Scenes)
        {
            var sceneStart = scene.Start;
            var sceneEnd = scene.Start + scene.Duration;

            // Check if scene overlaps with range
            if (sceneEnd <= start || sceneStart >= end)
            {
                // Scene is completely outside range
                continue;
            }

            // Calculate trimmed scene timing
            var trimmedStart = sceneStart < start ? TimeSpan.Zero : sceneStart - start;
            var trimmedDuration = scene.Duration;

            // Trim scene start if it begins before range
            if (sceneStart < start)
            {
                var trimAmount = start - sceneStart;
                trimmedDuration -= trimAmount;
            }

            // Trim scene end if it extends beyond range
            if (sceneEnd > end)
            {
                var trimAmount = sceneEnd - end;
                trimmedDuration -= trimAmount;
            }

            // Adjust visual assets
            var trimmedAssets = new List<TimelineAsset>();
            foreach (var asset in scene.VisualAssets)
            {
                var assetStart = scene.Start + asset.Start;
                var assetEnd = assetStart + asset.Duration;

                // Skip assets completely outside range
                if (assetEnd <= start || assetStart >= end)
                {
                    continue;
                }

                // Adjust asset timing relative to trimmed scene
                var adjustedStart = asset.Start;
                var adjustedDuration = asset.Duration;

                if (assetStart < start)
                {
                    var trimAmount = start - assetStart;
                    adjustedStart += trimAmount;
                    adjustedDuration -= trimAmount;
                }

                if (assetEnd > end)
                {
                    var trimAmount = assetEnd - end;
                    adjustedDuration -= trimAmount;
                }

                // Ensure timing is relative to trimmed scene start
                if (sceneStart < start)
                {
                    adjustedStart -= (start - sceneStart);
                }

                var trimmedAsset = asset with
                {
                    Start = adjustedStart >= TimeSpan.Zero ? adjustedStart : TimeSpan.Zero,
                    Duration = adjustedDuration
                };

                trimmedAssets.Add(trimmedAsset);
            }

            // Create trimmed scene
            var trimmedScene = scene with
            {
                Start = trimmedStart,
                Duration = trimmedDuration,
                VisualAssets = trimmedAssets
            };

            trimmedTimeline.AddScene(trimmedScene);
        }

        return trimmedTimeline;
    }
}
