using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Memory;

/// <summary>
/// Monitors memory pressure and provides warnings when memory usage is high
/// </summary>
public interface IMemoryPressureMonitor
{
    /// <summary>
    /// Gets the current memory usage in bytes
    /// </summary>
    long GetCurrentMemoryUsage();
    
    /// <summary>
    /// Gets the current GC generation counts
    /// </summary>
    GcStatistics GetGcStatistics();
    
    /// <summary>
    /// Checks if system is under memory pressure
    /// </summary>
    bool IsUnderMemoryPressure();
    
    /// <summary>
    /// Starts monitoring for a specific job
    /// </summary>
    void StartMonitoring(string jobId);
    
    /// <summary>
    /// Stops monitoring for a specific job and returns statistics
    /// </summary>
    MemoryStatistics StopMonitoring(string jobId);
    
    /// <summary>
    /// Forces garbage collection if memory pressure is high
    /// </summary>
    void ForceCollectionIfNeeded();
    
    /// <summary>
    /// Updates peak memory for a job if current memory is higher
    /// </summary>
    void UpdatePeakMemory(string jobId);
}

/// <summary>
/// Implementation of memory pressure monitor
/// </summary>
public class MemoryPressureMonitor : IMemoryPressureMonitor
{
    private readonly ILogger<MemoryPressureMonitor> _logger;
    private readonly ConcurrentDictionary<string, JobMemoryTracking> _jobTracking = new();
    private readonly long _memoryPressureThresholdBytes;
    private readonly double _memoryPressureThresholdPercent;
    
    private const long DefaultMemoryPressureThresholdMb = 2048; // 2GB
    private const double DefaultMemoryPressurePercent = 0.85; // 85%
    
    public MemoryPressureMonitor(
        ILogger<MemoryPressureMonitor> logger,
        long? memoryPressureThresholdMb = null,
        double? memoryPressureThresholdPercent = null)
    {
        _logger = logger;
        _memoryPressureThresholdBytes = (memoryPressureThresholdMb ?? DefaultMemoryPressureThresholdMb) * 1024 * 1024;
        _memoryPressureThresholdPercent = memoryPressureThresholdPercent ?? DefaultMemoryPressurePercent;
    }
    
    public long GetCurrentMemoryUsage()
    {
        return GC.GetTotalMemory(forceFullCollection: false);
    }
    
    public GcStatistics GetGcStatistics()
    {
        return new GcStatistics
        {
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalMemoryBytes = GC.GetTotalMemory(forceFullCollection: false),
            TotalMemoryMb = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0)
        };
    }
    
    public bool IsUnderMemoryPressure()
    {
        var currentMemory = GetCurrentMemoryUsage();
        
        // Check absolute threshold
        if (currentMemory > _memoryPressureThresholdBytes)
        {
            _logger.LogWarning(
                "Memory pressure detected: current usage {CurrentMb} MB exceeds threshold {ThresholdMb} MB",
                currentMemory / (1024.0 * 1024.0),
                _memoryPressureThresholdBytes / (1024.0 * 1024.0));
            return true;
        }
        
        // Check GC pressure (frequent Gen2 collections indicate memory pressure)
        var gcInfo = GC.GetGCMemoryInfo();
        if (gcInfo.HighMemoryLoadThresholdBytes > 0)
        {
            var memoryLoadPercent = (double)gcInfo.MemoryLoadBytes / gcInfo.HighMemoryLoadThresholdBytes;
            if (memoryLoadPercent > _memoryPressureThresholdPercent)
            {
                _logger.LogWarning(
                    "Memory pressure detected: memory load {LoadPercent:F1}% exceeds threshold {ThresholdPercent:F1}%",
                    memoryLoadPercent * 100,
                    _memoryPressureThresholdPercent * 100);
                return true;
            }
        }
        
        return false;
    }
    
    public void StartMonitoring(string jobId)
    {
        var tracking = new JobMemoryTracking
        {
            JobId = jobId,
            StartTime = DateTime.UtcNow,
            StartMemoryBytes = GetCurrentMemoryUsage(),
            StartGcGen0 = GC.CollectionCount(0),
            StartGcGen1 = GC.CollectionCount(1),
            StartGcGen2 = GC.CollectionCount(2)
        };
        
        _jobTracking[jobId] = tracking;
        
        _logger.LogInformation(
            "Started memory monitoring for job {JobId}: initial memory {MemoryMb:F1} MB",
            jobId,
            tracking.StartMemoryBytes / (1024.0 * 1024.0));
    }
    
    public MemoryStatistics StopMonitoring(string jobId)
    {
        if (!_jobTracking.TryRemove(jobId, out var tracking))
        {
            _logger.LogWarning("No memory tracking found for job {JobId}", jobId);
            return new MemoryStatistics
            {
                JobId = jobId,
                StartMemoryMb = 0,
                EndMemoryMb = 0,
                PeakMemoryMb = 0,
                MemoryDeltaMb = 0,
                DurationSeconds = 0,
                Gen0Collections = 0,
                Gen1Collections = 0,
                Gen2Collections = 0
            };
        }
        
        var endMemory = GetCurrentMemoryUsage();
        var endGcGen0 = GC.CollectionCount(0);
        var endGcGen1 = GC.CollectionCount(1);
        var endGcGen2 = GC.CollectionCount(2);
        var duration = DateTime.UtcNow - tracking.StartTime;
        
        var statistics = new MemoryStatistics
        {
            JobId = jobId,
            StartMemoryMb = tracking.StartMemoryBytes / (1024.0 * 1024.0),
            EndMemoryMb = endMemory / (1024.0 * 1024.0),
            PeakMemoryMb = tracking.PeakMemoryBytes / (1024.0 * 1024.0),
            MemoryDeltaMb = (endMemory - tracking.StartMemoryBytes) / (1024.0 * 1024.0),
            DurationSeconds = duration.TotalSeconds,
            Gen0Collections = endGcGen0 - tracking.StartGcGen0,
            Gen1Collections = endGcGen1 - tracking.StartGcGen1,
            Gen2Collections = endGcGen2 - tracking.StartGcGen2
        };
        
        _logger.LogInformation(
            "Memory statistics for job {JobId}: start={StartMb:F1}MB, end={EndMb:F1}MB, peak={PeakMb:F1}MB, delta={DeltaMb:+0.0;-0.0}MB, duration={Duration:F1}s, GC(G0={Gen0},G1={Gen1},G2={Gen2})",
            jobId,
            statistics.StartMemoryMb,
            statistics.EndMemoryMb,
            statistics.PeakMemoryMb,
            statistics.MemoryDeltaMb,
            statistics.DurationSeconds,
            statistics.Gen0Collections,
            statistics.Gen1Collections,
            statistics.Gen2Collections);
        
        return statistics;
    }
    
    public void ForceCollectionIfNeeded()
    {
        if (IsUnderMemoryPressure())
        {
            _logger.LogWarning("Forcing garbage collection due to memory pressure");
            
            var beforeMemory = GetCurrentMemoryUsage();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            var afterMemory = GetCurrentMemoryUsage();
            
            var freedMb = (beforeMemory - afterMemory) / (1024.0 * 1024.0);
            _logger.LogInformation(
                "Garbage collection completed: freed {FreedMb:F1} MB (before={BeforeMb:F1}MB, after={AfterMb:F1}MB)",
                freedMb,
                beforeMemory / (1024.0 * 1024.0),
                afterMemory / (1024.0 * 1024.0));
        }
    }
    
    /// <summary>
    /// Updates peak memory for a job if current memory is higher
    /// </summary>
    public void UpdatePeakMemory(string jobId)
    {
        if (_jobTracking.TryGetValue(jobId, out var tracking))
        {
            var currentMemory = GetCurrentMemoryUsage();
            if (currentMemory > tracking.PeakMemoryBytes)
            {
                tracking.PeakMemoryBytes = currentMemory;
            }
        }
    }
}

/// <summary>
/// GC statistics snapshot
/// </summary>
public record GcStatistics
{
    public required int Gen0Collections { get; init; }
    public required int Gen1Collections { get; init; }
    public required int Gen2Collections { get; init; }
    public required long TotalMemoryBytes { get; init; }
    public required double TotalMemoryMb { get; init; }
}

/// <summary>
/// Memory statistics for a job
/// </summary>
public record MemoryStatistics
{
    public required string JobId { get; init; }
    public required double StartMemoryMb { get; init; }
    public required double EndMemoryMb { get; init; }
    public required double PeakMemoryMb { get; init; }
    public required double MemoryDeltaMb { get; init; }
    public required double DurationSeconds { get; init; }
    public required int Gen0Collections { get; init; }
    public required int Gen1Collections { get; init; }
    public required int Gen2Collections { get; init; }
}

/// <summary>
/// Internal tracking data for a job
/// </summary>
internal class JobMemoryTracking
{
    public required string JobId { get; init; }
    public required DateTime StartTime { get; init; }
    public required long StartMemoryBytes { get; init; }
    public long PeakMemoryBytes { get; set; }
    public required int StartGcGen0 { get; init; }
    public required int StartGcGen1 { get; init; }
    public required int StartGcGen2 { get; init; }
}
