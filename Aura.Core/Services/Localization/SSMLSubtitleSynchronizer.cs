using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Service for synchronizing subtitles with SSML timing markers
/// Ensures subtitle timing aligns within ±2% of scene durations
/// </summary>
public class SSMLSubtitleSynchronizer
{
    private readonly ILogger<SSMLSubtitleSynchronizer> _logger;
    private const double DefaultTolerancePercent = 0.02; // ±2%

    public SSMLSubtitleSynchronizer(ILogger<SSMLSubtitleSynchronizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Synchronize subtitle timings with SSML segment results
    /// </summary>
    public SubtitleTimingSyncResult SynchronizeWithSSML(
        IReadOnlyList<SSMLSegmentResult> ssmlSegments,
        IReadOnlyList<ScriptLine> originalLines,
        double tolerancePercent = DefaultTolerancePercent)
    {
        _logger.LogInformation(
            "Synchronizing {Count} subtitle lines with SSML timing markers",
            originalLines.Count);

        var synchronizedLines = new List<ScriptLine>();
        var warnings = new List<string>();
        var adjustments = new List<TimingAdjustmentInfo>();

        var totalOriginalDuration = originalLines.Sum(l => l.Duration.TotalSeconds);
        var totalSSMLDuration = ssmlSegments.Sum(s => s.EstimatedDurationMs / 1000.0);

        var currentStartTime = TimeSpan.Zero;

        for (int i = 0; i < originalLines.Count && i < ssmlSegments.Count; i++)
        {
            var originalLine = originalLines[i];
            var ssmlSegment = ssmlSegments[i];

            var targetDurationSeconds = ssmlSegment.TargetDurationMs / 1000.0;
            var estimatedDurationSeconds = ssmlSegment.EstimatedDurationMs / 1000.0;

            var adjustedDuration = TimeSpan.FromSeconds(estimatedDurationSeconds);
            
            var originalDuration = originalLine.Duration.TotalSeconds;
            var deviation = Math.Abs(estimatedDurationSeconds - targetDurationSeconds) / targetDurationSeconds;

            if (deviation > tolerancePercent)
            {
                var warningMsg = $"Scene {i}: SSML timing deviation {deviation:P2} exceeds tolerance {tolerancePercent:P2} " +
                                $"(Target: {targetDurationSeconds:F2}s, SSML: {estimatedDurationSeconds:F2}s)";
                warnings.Add(warningMsg);
                _logger.LogWarning(warningMsg);
            }

            var adjustmentInfo = new TimingAdjustmentInfo
            {
                SceneIndex = i,
                OriginalStart = originalLine.Start,
                OriginalDuration = originalLine.Duration,
                AdjustedStart = currentStartTime,
                AdjustedDuration = adjustedDuration,
                DeviationFromTarget = deviation,
                WithinTolerance = deviation <= tolerancePercent,
                TimingMarkers = ssmlSegment.TimingMarkers.ToList()
            };

            adjustments.Add(adjustmentInfo);

            var synchronizedLine = new ScriptLine(
                originalLine.SceneIndex,
                originalLine.Text,
                currentStartTime,
                adjustedDuration);

            synchronizedLines.Add(synchronizedLine);
            currentStartTime += adjustedDuration;

            _logger.LogDebug(
                "Scene {Index}: Original={OrigDuration:F2}s, Target={TargetDuration:F2}s, " +
                "SSML={SSMLDuration:F2}s, Deviation={Deviation:P2}",
                i, originalDuration, targetDurationSeconds, estimatedDurationSeconds, deviation);
        }

        var finalDuration = synchronizedLines.Sum(l => l.Duration.TotalSeconds);
        var overallDeviation = Math.Abs(finalDuration - totalOriginalDuration) / totalOriginalDuration;

        _logger.LogInformation(
            "Synchronization complete: Original={Original:F2}s, Final={Final:F2}s, " +
            "SSML={SSML:F2}s, Overall Deviation={Deviation:P2}",
            totalOriginalDuration, finalDuration, totalSSMLDuration, overallDeviation);

        var withinTolerance = adjustments.Count(a => a.WithinTolerance);
        var withinTolerancePercent = (double)withinTolerance / adjustments.Count * 100;

        return new SubtitleTimingSyncResult
        {
            SynchronizedLines = synchronizedLines,
            Adjustments = adjustments,
            Warnings = warnings,
            TotalOriginalDuration = totalOriginalDuration,
            TotalAdjustedDuration = finalDuration,
            TotalSSMLDuration = totalSSMLDuration,
            OverallDeviation = overallDeviation,
            WithinToleranceCount = withinTolerance,
            WithinTolerancePercent = withinTolerancePercent,
            IsValid = overallDeviation <= tolerancePercent
        };
    }

    /// <summary>
    /// Apply timing markers to subtitle lines for fine-grained synchronization
    /// </summary>
    public List<ScriptLine> ApplyTimingMarkers(
        ScriptLine line,
        IReadOnlyList<TimingMarker> markers)
    {
        if (!markers.Any())
        {
            return new List<ScriptLine> { line };
        }

        _logger.LogDebug(
            "Applying {Count} timing markers to scene {Index}",
            markers.Count, line.SceneIndex);

        var segments = new List<ScriptLine>();
        var sortedMarkers = markers.OrderBy(m => m.OffsetMs).ToList();

        var textSegments = SplitTextByMarkers(line.Text, sortedMarkers);

        var currentOffset = line.Start;
        for (int i = 0; i < textSegments.Count; i++)
        {
            var textSegment = textSegments[i];
            
            TimeSpan segmentDuration;
            if (i < sortedMarkers.Count)
            {
                var markerOffset = TimeSpan.FromMilliseconds(sortedMarkers[i].OffsetMs);
                segmentDuration = markerOffset - (currentOffset - line.Start);
            }
            else
            {
                segmentDuration = (line.Start + line.Duration) - currentOffset;
            }

            if (segmentDuration > TimeSpan.Zero && !string.IsNullOrWhiteSpace(textSegment))
            {
                var segment = new ScriptLine(
                    line.SceneIndex,
                    textSegment.Trim(),
                    currentOffset,
                    segmentDuration);

                segments.Add(segment);
                currentOffset += segmentDuration;
            }
        }

        return segments;
    }

    private List<string> SplitTextByMarkers(string text, List<TimingMarker> markers)
    {
        if (markers.Count == 0)
        {
            return new List<string> { text };
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<string>();
        
        var wordsPerMarker = Math.Max(1, words.Length / (markers.Count + 1));
        
        for (int i = 0; i <= markers.Count; i++)
        {
            var startIndex = i * wordsPerMarker;
            var endIndex = (i == markers.Count) ? words.Length : (i + 1) * wordsPerMarker;
            
            if (startIndex < words.Length)
            {
                var segmentWords = words[startIndex..Math.Min(endIndex, words.Length)];
                segments.Add(string.Join(" ", segmentWords));
            }
        }

        return segments;
    }
}

/// <summary>
/// Result of subtitle timing synchronization
/// </summary>
public record SubtitleTimingSyncResult
{
    public required List<ScriptLine> SynchronizedLines { get; init; }
    public required List<TimingAdjustmentInfo> Adjustments { get; init; }
    public List<string> Warnings { get; init; } = new();
    public double TotalOriginalDuration { get; init; }
    public double TotalAdjustedDuration { get; init; }
    public double TotalSSMLDuration { get; init; }
    public double OverallDeviation { get; init; }
    public int WithinToleranceCount { get; init; }
    public double WithinTolerancePercent { get; init; }
    public bool IsValid { get; init; }
}

/// <summary>
/// Information about a single timing adjustment
/// </summary>
public record TimingAdjustmentInfo
{
    public int SceneIndex { get; init; }
    public TimeSpan OriginalStart { get; init; }
    public TimeSpan OriginalDuration { get; init; }
    public TimeSpan AdjustedStart { get; init; }
    public TimeSpan AdjustedDuration { get; init; }
    public double DeviationFromTarget { get; init; }
    public bool WithinTolerance { get; init; }
    public List<TimingMarker> TimingMarkers { get; init; } = new();
}
