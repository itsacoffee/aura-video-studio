using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Aggregates progress information from multiple sources (FFmpeg, TTS, LLM, etc.)
/// Provides unified progress tracking with stage-based weighting
/// Thread-safe for concurrent updates from different pipeline stages
/// </summary>
public class ProgressAggregatorService
{
    private readonly ILogger<ProgressAggregatorService> _logger;
    private readonly ConcurrentDictionary<string, AggregatedProgress> _jobProgress;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public ProgressAggregatorService(ILogger<ProgressAggregatorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jobProgress = new ConcurrentDictionary<string, AggregatedProgress>();
    }

    /// <summary>
    /// Update progress for a specific job and stage
    /// </summary>
    public void UpdateProgress(
        string jobId,
        string stage,
        double stagePercent,
        string message,
        string? correlationId = null,
        string? substageDetail = null,
        int? currentItem = null,
        int? totalItems = null)
    {
        try
        {
            var overallPercent = StageWeights.CalculateOverallProgress(stage, stagePercent);
            var now = DateTime.UtcNow;

            var progress = _jobProgress.AddOrUpdate(
                jobId,
                _ => new AggregatedProgress
                {
                    JobId = jobId,
                    Stage = stage,
                    StagePercent = stagePercent,
                    OverallPercent = overallPercent,
                    Message = message,
                    CorrelationId = correlationId,
                    SubstageDetail = substageDetail,
                    CurrentItem = currentItem,
                    TotalItems = totalItems,
                    LastUpdated = now,
                    StartTime = now
                },
                (_, existing) =>
                {
                    var elapsed = now - existing.StartTime;
                    TimeSpan? eta = null;

                    if (overallPercent > 0 && overallPercent < 100)
                    {
                        var totalEstimated = elapsed.TotalSeconds / (overallPercent / 100.0);
                        var remaining = totalEstimated - elapsed.TotalSeconds;
                        eta = TimeSpan.FromSeconds(Math.Max(0, remaining));
                    }

                    return existing with
                    {
                        Stage = stage,
                        StagePercent = stagePercent,
                        OverallPercent = overallPercent,
                        Message = message,
                        SubstageDetail = substageDetail,
                        CurrentItem = currentItem,
                        TotalItems = totalItems,
                        LastUpdated = now,
                        Eta = eta
                    };
                });

            _logger.LogDebug("Progress updated for job {JobId}: {Stage} {Percent}% - {Message}",
                jobId, stage, (int)overallPercent, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Add a warning for a specific job
    /// </summary>
    public void AddWarning(string jobId, string warning)
    {
        _jobProgress.AddOrUpdate(
            jobId,
            _ => new AggregatedProgress
            {
                JobId = jobId,
                Warnings = new List<string> { warning },
                LastUpdated = DateTime.UtcNow
            },
            (_, existing) =>
            {
                var warnings = new List<string>(existing.Warnings) { warning };
                return existing with { Warnings = warnings, LastUpdated = DateTime.UtcNow };
            });

        _logger.LogWarning("Warning added for job {JobId}: {Warning}", jobId, warning);
    }

    /// <summary>
    /// Get current aggregated progress for a job
    /// </summary>
    public AggregatedProgress? GetProgress(string jobId)
    {
        return _jobProgress.TryGetValue(jobId, out var progress) ? progress : null;
    }

    /// <summary>
    /// Remove progress tracking for a completed or cancelled job
    /// </summary>
    public void RemoveProgress(string jobId)
    {
        _jobProgress.TryRemove(jobId, out _);
        _logger.LogDebug("Progress tracking removed for job {JobId}", jobId);
    }

    /// <summary>
    /// Get all active job progress
    /// </summary>
    public IEnumerable<AggregatedProgress> GetAllProgress()
    {
        return _jobProgress.Values.ToList();
    }

    /// <summary>
    /// Map FFmpeg progress output to standardized progress update
    /// </summary>
    public void UpdateFromFFmpegProgress(
        string jobId,
        int frameNumber,
        int totalFrames,
        double fps,
        TimeSpan time,
        string? correlationId = null)
    {
        var percent = totalFrames > 0 ? (double)frameNumber / totalFrames * 100.0 : 0;
        var message = $"Rendering: frame {frameNumber}/{totalFrames} @ {fps:F1}fps, time={time:hh\\:mm\\:ss}";

        UpdateProgress(
            jobId,
            "Rendering",
            percent,
            message,
            correlationId,
            substageDetail: $"Frame {frameNumber} of {totalFrames}",
            currentItem: frameNumber,
            totalItems: totalFrames);
    }

    /// <summary>
    /// Map TTS provider progress to standardized progress update
    /// </summary>
    public void UpdateFromTtsProgress(
        string jobId,
        int sceneIndex,
        int totalScenes,
        string providerName,
        string? correlationId = null)
    {
        var percent = totalScenes > 0 ? (double)(sceneIndex + 1) / totalScenes * 100.0 : 0;
        var message = $"Synthesizing speech using {providerName}";

        UpdateProgress(
            jobId,
            "TTS",
            percent,
            message,
            correlationId,
            substageDetail: $"Scene {sceneIndex + 1} of {totalScenes}",
            currentItem: sceneIndex + 1,
            totalItems: totalScenes);
    }

    /// <summary>
    /// Map LLM script generation progress to standardized progress update
    /// </summary>
    public void UpdateFromLlmProgress(
        string jobId,
        string phase,
        int? currentChunk = null,
        int? totalChunks = null,
        string? correlationId = null)
    {
        var percent = 0.0;
        var message = $"Generating script: {phase}";

        if (currentChunk.HasValue && totalChunks.HasValue && totalChunks.Value > 0)
        {
            percent = (double)currentChunk.Value / totalChunks.Value * 100.0;
            message = $"Generating script: {phase} (chunk {currentChunk}/{totalChunks})";
        }

        UpdateProgress(
            jobId,
            "Script",
            percent,
            message,
            correlationId,
            substageDetail: currentChunk.HasValue && totalChunks.HasValue
                ? $"Chunk {currentChunk} of {totalChunks}"
                : null,
            currentItem: currentChunk,
            totalItems: totalChunks);
    }

    /// <summary>
    /// Map visual generation progress to standardized progress update
    /// </summary>
    public void UpdateFromVisualProgress(
        string jobId,
        int imageIndex,
        int totalImages,
        string providerName,
        string? correlationId = null)
    {
        var percent = totalImages > 0 ? (double)(imageIndex + 1) / totalImages * 100.0 : 0;
        var message = $"Generating visuals using {providerName}";

        UpdateProgress(
            jobId,
            "Visuals",
            percent,
            message,
            correlationId,
            substageDetail: $"Image {imageIndex + 1} of {totalImages}",
            currentItem: imageIndex + 1,
            totalItems: totalImages);
    }
}

/// <summary>
/// Aggregated progress information from multiple sources
/// </summary>
public record AggregatedProgress
{
    public string JobId { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public double StagePercent { get; init; }
    public double OverallPercent { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? SubstageDetail { get; init; }
    public int? CurrentItem { get; init; }
    public int? TotalItems { get; init; }
    public TimeSpan? Eta { get; init; }
    public List<string> Warnings { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public DateTime StartTime { get; init; }
}
