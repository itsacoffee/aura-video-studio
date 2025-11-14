using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Export metrics record
/// </summary>
public record ExportMetrics(
    string Id,
    DateTime Timestamp,
    ExportPreset Preset,
    TimeSpan Duration,
    long FileSizeBytes,
    double EncodingSpeedFps,
    double RealtimeMultiplier,
    string HardwareUsed,
    bool IsHardwareAccelerated,
    bool Success,
    string? ErrorMessage = null);

/// <summary>
/// Aggregate statistics
/// </summary>
public record RenderStatistics(
    int TotalRenders,
    int SuccessfulRenders,
    int FailedRenders,
    double SuccessRate,
    TimeSpan AverageRenderTime,
    double AverageEncodingSpeed,
    Dictionary<string, int> PresetUsage,
    Dictionary<string, TimeSpan> PresetAverageTime,
    string MostUsedPreset,
    int HardwareAcceleratedCount,
    int SoftwareEncodedCount);

/// <summary>
/// Tracks and analyzes export performance metrics
/// </summary>
public class RenderAnalytics
{
    private readonly ILogger<RenderAnalytics> _logger;
    private readonly string _metricsFile;
    private readonly List<ExportMetrics> _metrics;

    public RenderAnalytics(ILogger<RenderAnalytics> logger, string storageDirectory)
    {
        _logger = logger;
        _metricsFile = Path.Combine(storageDirectory, "render_analytics.json");
        _metrics = new List<ExportMetrics>();

        LoadMetrics();
    }

    /// <summary>
    /// Records export metrics
    /// </summary>
    public async Task RecordExportAsync(
        ExportPreset preset,
        TimeSpan renderDuration,
        long fileSizeBytes,
        double encodingSpeedFps,
        string hardwareUsed,
        bool isHardwareAccelerated,
        bool success,
        string? errorMessage = null)
    {
        var realtimeMultiplier = 0.0;
        
        // Calculate realtime multiplier (how many times faster than realtime playback)
        if (renderDuration.TotalSeconds > 0)
        {
            // Assuming timeline duration matches file duration for now
            var estimatedTimelineDuration = fileSizeBytes / (preset.VideoBitrate * 1000.0 / 8.0);
            realtimeMultiplier = estimatedTimelineDuration / renderDuration.TotalSeconds;
        }

        var metrics = new ExportMetrics(
            Id: Guid.NewGuid().ToString(),
            Timestamp: DateTime.UtcNow,
            Preset: preset,
            Duration: renderDuration,
            FileSizeBytes: fileSizeBytes,
            EncodingSpeedFps: encodingSpeedFps,
            RealtimeMultiplier: realtimeMultiplier,
            HardwareUsed: hardwareUsed,
            IsHardwareAccelerated: isHardwareAccelerated,
            Success: success,
            ErrorMessage: errorMessage
        );

        _metrics.Add(metrics);

        _logger.LogInformation(
            "Recorded export metrics: {Preset}, {Duration}s, {Size} MB, {Fps} FPS, {Hardware} ({HwType}), {Result}",
            preset.Name,
            renderDuration.TotalSeconds,
            fileSizeBytes / 1024.0 / 1024.0,
            encodingSpeedFps,
            hardwareUsed,
            isHardwareAccelerated ? "HW" : "SW",
            success ? "Success" : "Failed"
        );

        await SaveMetricsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets aggregate statistics
    /// </summary>
    public RenderStatistics GetStatistics(DateTime? since = null)
    {
        var filteredMetrics = since.HasValue
            ? _metrics.Where(m => m.Timestamp >= since.Value).ToList()
            : _metrics.ToList();

        if (filteredMetrics.Count == 0)
        {
            return new RenderStatistics(
                TotalRenders: 0,
                SuccessfulRenders: 0,
                FailedRenders: 0,
                SuccessRate: 0,
                AverageRenderTime: TimeSpan.Zero,
                AverageEncodingSpeed: 0,
                PresetUsage: new Dictionary<string, int>(),
                PresetAverageTime: new Dictionary<string, TimeSpan>(),
                MostUsedPreset: "None",
                HardwareAcceleratedCount: 0,
                SoftwareEncodedCount: 0
            );
        }

        var successful = filteredMetrics.Count(m => m.Success);
        var failed = filteredMetrics.Count(m => !m.Success);
        var successRate = (double)successful / filteredMetrics.Count;

        var avgRenderTime = TimeSpan.FromSeconds(
            filteredMetrics.Average(m => m.Duration.TotalSeconds)
        );

        var avgEncodingSpeed = filteredMetrics
            .Where(m => m.EncodingSpeedFps > 0)
            .Average(m => m.EncodingSpeedFps);

        var presetUsage = filteredMetrics
            .GroupBy(m => m.Preset.Name)
            .ToDictionary(g => g.Key, g => g.Count());

        var presetAvgTime = filteredMetrics
            .GroupBy(m => m.Preset.Name)
            .ToDictionary(
                g => g.Key,
                g => TimeSpan.FromSeconds(g.Average(m => m.Duration.TotalSeconds))
            );

        var mostUsed = presetUsage.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None";

        var hwCount = filteredMetrics.Count(m => m.IsHardwareAccelerated);
        var swCount = filteredMetrics.Count(m => !m.IsHardwareAccelerated);

        return new RenderStatistics(
            TotalRenders: filteredMetrics.Count,
            SuccessfulRenders: successful,
            FailedRenders: failed,
            SuccessRate: successRate,
            AverageRenderTime: avgRenderTime,
            AverageEncodingSpeed: avgEncodingSpeed,
            PresetUsage: presetUsage,
            PresetAverageTime: presetAvgTime,
            MostUsedPreset: mostUsed,
            HardwareAcceleratedCount: hwCount,
            SoftwareEncodedCount: swCount
        );
    }

    /// <summary>
    /// Gets performance comparison between hardware and software encoding
    /// </summary>
    public (double hwAvgTime, double swAvgTime, double speedup) GetHardwareSoftwareComparison()
    {
        var hwMetrics = _metrics.Where(m => m.IsHardwareAccelerated && m.Success).ToList();
        var swMetrics = _metrics.Where(m => !m.IsHardwareAccelerated && m.Success).ToList();

        if (hwMetrics.Count == 0 || swMetrics.Count == 0)
        {
            return (0, 0, 0);
        }

        var hwAvg = hwMetrics.Average(m => m.Duration.TotalSeconds);
        var swAvg = swMetrics.Average(m => m.Duration.TotalSeconds);
        var speedup = swAvg / hwAvg;

        _logger.LogDebug(
            "HW vs SW comparison: HW avg={HwAvg}s, SW avg={SwAvg}s, Speedup={Speedup}x",
            hwAvg, swAvg, speedup
        );

        return (hwAvg, swAvg, speedup);
    }

    /// <summary>
    /// Gets metrics for a specific preset
    /// </summary>
    public List<ExportMetrics> GetMetricsByPreset(string presetName)
    {
        return _metrics
            .Where(m => m.Preset.Name == presetName)
            .OrderByDescending(m => m.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Gets recent metrics
    /// </summary>
    public List<ExportMetrics> GetRecentMetrics(int count = 10)
    {
        return _metrics
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Exports analytics data as CSV
    /// </summary>
    public async Task<string> ExportToCsvAsync(string outputPath)
    {
        var lines = new List<string>
        {
            "Timestamp,Preset,Duration_Seconds,FileSize_MB,EncodingSpeed_FPS,Realtime_Multiplier,Hardware,HW_Accelerated,Success,Error"
        };

        foreach (var m in _metrics.OrderBy(m => m.Timestamp))
        {
            var line = string.Join(",",
                m.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                m.Preset.Name,
                m.Duration.TotalSeconds.ToString("F2"),
                (m.FileSizeBytes / 1024.0 / 1024.0).ToString("F2"),
                m.EncodingSpeedFps.ToString("F2"),
                m.RealtimeMultiplier.ToString("F2"),
                m.HardwareUsed,
                m.IsHardwareAccelerated,
                m.Success,
                m.ErrorMessage ?? ""
            );
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(outputPath, lines).ConfigureAwait(false);
        
        _logger.LogInformation("Exported {Count} metrics to {Path}", _metrics.Count, outputPath);
        
        return outputPath;
    }

    /// <summary>
    /// Identifies performance issues
    /// </summary>
    public List<string> IdentifyPerformanceIssues()
    {
        var issues = new List<string>();

        if (_metrics.Count < 5)
        {
            return issues; // Not enough data
        }

        var stats = GetStatistics();

        // Check success rate
        if (stats.SuccessRate < 0.8)
        {
            issues.Add($"Low success rate: {stats.SuccessRate:P0}. Check FFmpeg configuration and system resources.");
        }

        // Check if hardware acceleration is being used
        if (stats.HardwareAcceleratedCount == 0 && stats.TotalRenders > 0)
        {
            issues.Add("Hardware acceleration not being used. Enable GPU encoding in settings for 5-10x faster renders.");
        }

        // Compare hardware vs software performance
        var (hwAvg, swAvg, speedup) = GetHardwareSoftwareComparison();
        if (speedup > 0 && speedup < 2.0)
        {
            issues.Add($"Hardware acceleration speedup is only {speedup:F1}x. GPU may not be properly configured or video is too short to see benefits.");
        }

        // Check for slow renders
        if (stats.AverageEncodingSpeed > 0 && stats.AverageEncodingSpeed < 30)
        {
            issues.Add($"Average encoding speed is low ({stats.AverageEncodingSpeed:F1} FPS). Consider enabling hardware acceleration or reducing quality settings.");
        }

        return issues;
    }

    private void LoadMetrics()
    {
        try
        {
            if (!File.Exists(_metricsFile))
            {
                return;
            }

            var json = File.ReadAllText(_metricsFile);
            var loaded = JsonSerializer.Deserialize<List<ExportMetrics>>(json);

            if (loaded != null)
            {
                _metrics.AddRange(loaded);
                _logger.LogInformation("Loaded {Count} export metrics", loaded.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load export metrics");
        }
    }

    private async Task SaveMetricsAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_metricsFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_metrics, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_metricsFile, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save export metrics");
        }
    }
}
