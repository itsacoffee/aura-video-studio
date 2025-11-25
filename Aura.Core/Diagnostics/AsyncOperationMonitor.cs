using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Diagnostics;

/// <summary>
/// Monitors async operations for potential deadlocks, timeouts, and performance issues
/// </summary>
public class AsyncOperationMonitor : IDisposable
{
    private readonly ILogger<AsyncOperationMonitor> _logger;
    private readonly ConcurrentDictionary<string, OperationTracker> _activeOperations;
    private readonly Timer? _monitoringTimer;
    private readonly TimeSpan _warningThreshold;
    private readonly TimeSpan _errorThreshold;
    private bool _disposed;

    public AsyncOperationMonitor(
        ILogger<AsyncOperationMonitor> logger,
        TimeSpan? warningThreshold = null,
        TimeSpan? errorThreshold = null)
    {
        _logger = logger;
        _activeOperations = new ConcurrentDictionary<string, OperationTracker>();
        _warningThreshold = warningThreshold ?? TimeSpan.FromSeconds(5);
        _errorThreshold = errorThreshold ?? TimeSpan.FromSeconds(30);
        
        // Start periodic monitoring
        _monitoringTimer = new Timer(CheckForStuckOperations, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Tracks an async operation
    /// </summary>
    public IDisposable TrackOperation(string operationName, string? context = null)
    {
        var tracker = new OperationTracker(operationName, context, Stopwatch.StartNew());
        var key = $"{operationName}_{tracker.Id}";
        
        if (!_activeOperations.TryAdd(key, tracker))
        {
            _logger.LogWarning("Failed to track operation: {OperationName}", operationName);
        }

        return new OperationScope(this, key);
    }

    /// <summary>
    /// Completes tracking for an operation
    /// </summary>
    internal void CompleteOperation(string key)
    {
        if (_activeOperations.TryRemove(key, out var tracker))
        {
            tracker.Stopwatch.Stop();
            var duration = tracker.Stopwatch.Elapsed;

            if (duration > _errorThreshold)
            {
                _logger.LogError(
                    "Async operation {OperationName} took {Duration}ms (exceeded error threshold of {Threshold}ms). Context: {Context}",
                    tracker.OperationName,
                    duration.TotalMilliseconds,
                    _errorThreshold.TotalMilliseconds,
                    tracker.Context);
            }
            else if (duration > _warningThreshold)
            {
                _logger.LogWarning(
                    "Async operation {OperationName} took {Duration}ms (exceeded warning threshold of {Threshold}ms). Context: {Context}",
                    tracker.OperationName,
                    duration.TotalMilliseconds,
                    _warningThreshold.TotalMilliseconds,
                    tracker.Context);
            }
            else
            {
                _logger.LogDebug(
                    "Async operation {OperationName} completed in {Duration}ms. Context: {Context}",
                    tracker.OperationName,
                    duration.TotalMilliseconds,
                    tracker.Context);
            }
        }
    }

    private void CheckForStuckOperations(object? state)
    {
        var now = DateTime.UtcNow;
        var stuckOperations = new List<OperationTracker>();

        foreach (var kvp in _activeOperations)
        {
            var elapsed = kvp.Value.Stopwatch.Elapsed;
            if (elapsed > _errorThreshold)
            {
                stuckOperations.Add(kvp.Value);
            }
        }

        if (stuckOperations.Count > 0)
        {
            _logger.LogError(
                "Detected {Count} potentially stuck async operations: {Operations}",
                stuckOperations.Count,
                string.Join(", ", stuckOperations.Select(o => $"{o.OperationName} ({o.Elapsed.TotalMilliseconds:F0}ms)")));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _monitoringTimer?.Dispose();
        _activeOperations.Clear();
        _disposed = true;
    }

    private class OperationTracker
    {
        public string OperationName { get; }
        public string? Context { get; }
        public Stopwatch Stopwatch { get; }
        public Guid Id { get; }
        public TimeSpan Elapsed => Stopwatch.Elapsed;

        public OperationTracker(string operationName, string? context, Stopwatch stopwatch)
        {
            OperationName = operationName;
            Context = context;
            Stopwatch = stopwatch;
            Id = Guid.NewGuid();
        }
    }

    private class OperationScope : IDisposable
    {
        private readonly AsyncOperationMonitor _monitor;
        private readonly string _key;

        public OperationScope(AsyncOperationMonitor monitor, string key)
        {
            _monitor = monitor;
            _key = key;
        }

        public void Dispose()
        {
            _monitor.CompleteOperation(_key);
        }
    }
}

/// <summary>
/// Extension methods for async operation monitoring
/// </summary>
public static class AsyncOperationMonitorExtensions
{
    /// <summary>
    /// Monitors an async operation
    /// </summary>
    public static async Task<T> MonitorAsync<T>(
        this AsyncOperationMonitor monitor,
        string operationName,
        Func<Task<T>> operation,
        string? context = null)
    {
        using (monitor.TrackOperation(operationName, context))
        {
            return await operation().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Monitors an async operation (void return)
    /// </summary>
    public static async Task MonitorAsync(
        this AsyncOperationMonitor monitor,
        string operationName,
        Func<Task> operation,
        string? context = null)
    {
        using (monitor.TrackOperation(operationName, context))
        {
            await operation().ConfigureAwait(false);
        }
    }
}

