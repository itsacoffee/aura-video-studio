using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for tracking performance metrics of operations
/// </summary>
public class PerformanceTrackingService
{
    private readonly ILogger<PerformanceTrackingService> _logger;
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
    private readonly TimeSpan _slowOperationThreshold = TimeSpan.FromSeconds(5);
    private readonly int _maxStoredMetrics = 1000;

    public PerformanceTrackingService(ILogger<PerformanceTrackingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record an operation's performance
    /// </summary>
    public void RecordOperation(string operationName, TimeSpan duration, string? correlationId = null, Dictionary<string, object>? details = null)
    {
        _metrics.AddOrUpdate(
            operationName,
            _ => new PerformanceMetric
            {
                OperationName = operationName,
                Count = 1,
                TotalDuration = duration,
                MinDuration = duration,
                MaxDuration = duration,
                LastExecuted = DateTime.UtcNow,
                Durations = new List<TimeSpan> { duration },
                SlowOperations = duration >= _slowOperationThreshold 
                    ? new List<SlowOperation> { new() { Duration = duration, Timestamp = DateTime.UtcNow, CorrelationId = correlationId, Details = details } }
                    : new List<SlowOperation>()
            },
            (_, existing) =>
            {
                existing.Count++;
                existing.TotalDuration += duration;
                existing.MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration;
                existing.MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration;
                existing.LastExecuted = DateTime.UtcNow;
                
                // Keep last 1000 durations for percentile calculations
                existing.Durations.Add(duration);
                if (existing.Durations.Count > 1000)
                {
                    existing.Durations.RemoveAt(0);
                }

                // Track slow operations (keep last 100)
                if (duration >= _slowOperationThreshold)
                {
                    existing.SlowOperations.Add(new SlowOperation
                    {
                        Duration = duration,
                        Timestamp = DateTime.UtcNow,
                        CorrelationId = correlationId,
                        Details = details
                    });

                    if (existing.SlowOperations.Count > 100)
                    {
                        existing.SlowOperations.RemoveAt(0);
                    }

                    _logger.LogWarning(
                        "Slow operation detected: {Operation} took {Duration}ms (threshold: {Threshold}ms) [CorrelationId: {CorrelationId}]",
                        operationName,
                        duration.TotalMilliseconds,
                        _slowOperationThreshold.TotalMilliseconds,
                        correlationId ?? "N/A");
                }

                return existing;
            });

        // Trim if too many metrics
        if (_metrics.Count > _maxStoredMetrics)
        {
            var oldestMetrics = _metrics
                .OrderBy(kvp => kvp.Value.LastExecuted)
                .Take(_metrics.Count - _maxStoredMetrics)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestMetrics)
            {
                _metrics.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Get performance metrics for all operations
    /// </summary>
    public List<PerformanceMetricSummary> GetMetrics()
    {
        return _metrics.Values
            .Select(m => new PerformanceMetricSummary
            {
                OperationName = m.OperationName,
                Count = m.Count,
                AverageDuration = TimeSpan.FromMilliseconds(m.TotalDuration.TotalMilliseconds / m.Count),
                MinDuration = m.MinDuration,
                MaxDuration = m.MaxDuration,
                LastExecuted = m.LastExecuted,
                P50Duration = CalculatePercentile(m.Durations, 0.50),
                P95Duration = CalculatePercentile(m.Durations, 0.95),
                P99Duration = CalculatePercentile(m.Durations, 0.99),
                SlowOperationCount = m.SlowOperations.Count
            })
            .OrderByDescending(m => m.Count)
            .ToList();
    }

    /// <summary>
    /// Get slow operations across all tracked operations
    /// </summary>
    public List<SlowOperationSummary> GetSlowOperations(int? limit = null)
    {
        IEnumerable<SlowOperationSummary> slowOps = _metrics.Values
            .SelectMany(m => m.SlowOperations.Select(so => new SlowOperationSummary
            {
                OperationName = m.OperationName,
                Duration = so.Duration,
                Timestamp = so.Timestamp,
                CorrelationId = so.CorrelationId,
                Details = so.Details
            }))
            .OrderByDescending(so => so.Duration);

        if (limit.HasValue)
        {
            slowOps = slowOps.Take(limit.Value);
        }

        return slowOps.ToList();
    }

    /// <summary>
    /// Get metrics for a specific operation
    /// </summary>
    public PerformanceMetricSummary? GetOperationMetric(string operationName)
    {
        if (_metrics.TryGetValue(operationName, out var metric))
        {
            return new PerformanceMetricSummary
            {
                OperationName = metric.OperationName,
                Count = metric.Count,
                AverageDuration = TimeSpan.FromMilliseconds(metric.TotalDuration.TotalMilliseconds / metric.Count),
                MinDuration = metric.MinDuration,
                MaxDuration = metric.MaxDuration,
                LastExecuted = metric.LastExecuted,
                P50Duration = CalculatePercentile(metric.Durations, 0.50),
                P95Duration = CalculatePercentile(metric.Durations, 0.95),
                P99Duration = CalculatePercentile(metric.Durations, 0.99),
                SlowOperationCount = metric.SlowOperations.Count
            };
        }

        return null;
    }

    /// <summary>
    /// Clear metrics older than specified time
    /// </summary>
    public int ClearOldMetrics(TimeSpan retentionPeriod)
    {
        var cutoffTime = DateTime.UtcNow - retentionPeriod;
        var keysToRemove = _metrics
            .Where(kvp => kvp.Value.LastExecuted < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        int removed = 0;
        foreach (var key in keysToRemove)
        {
            if (_metrics.TryRemove(key, out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogInformation("Cleared {RemovedCount} old performance metrics beyond retention period", removed);
        }

        return removed;
    }

    /// <summary>
    /// Calculate percentile from duration list
    /// </summary>
    private TimeSpan CalculatePercentile(List<TimeSpan> durations, double percentile)
    {
        if (durations.Count == 0)
            return TimeSpan.Zero;

        var sorted = durations.OrderBy(d => d.TotalMilliseconds).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));
        return sorted[index];
    }
}

/// <summary>
/// Internal performance metric with full data
/// </summary>
internal class PerformanceMetric
{
    public string OperationName { get; set; } = string.Empty;
    public long Count { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public DateTime LastExecuted { get; set; }
    public List<TimeSpan> Durations { get; set; } = new();
    public List<SlowOperation> SlowOperations { get; set; } = new();
}

/// <summary>
/// Summary of performance metrics for an operation
/// </summary>
public class PerformanceMetricSummary
{
    public string OperationName { get; set; } = string.Empty;
    public long Count { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan P50Duration { get; set; }
    public TimeSpan P95Duration { get; set; }
    public TimeSpan P99Duration { get; set; }
    public DateTime LastExecuted { get; set; }
    public int SlowOperationCount { get; set; }
}

/// <summary>
/// Details of a slow operation
/// </summary>
public class SlowOperation
{
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Summary of a slow operation for reporting
/// </summary>
public class SlowOperationSummary
{
    public string OperationName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}
