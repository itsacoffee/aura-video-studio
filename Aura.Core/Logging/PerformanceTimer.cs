using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Aura.Core.Logging;

/// <summary>
/// Provides performance timing capabilities with automatic logging
/// </summary>
public sealed class PerformanceTimer : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly LogLevel _logLevel;
    private readonly Dictionary<string, object> _metadata;
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    private PerformanceTimer(
        ILogger logger,
        string operationName,
        LogLevel logLevel = LogLevel.Information,
        Dictionary<string, object>? metadata = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _logLevel = logLevel;
        _metadata = metadata ?? new Dictionary<string, object>();
        _stopwatch = Stopwatch.StartNew();

        // Log operation start
        _logger.Log(_logLevel, "Starting operation: {OperationName}", _operationName);
    }

    /// <summary>
    /// Creates and starts a new performance timer
    /// </summary>
    public static PerformanceTimer Start(
        ILogger logger,
        string operationName,
        LogLevel logLevel = LogLevel.Information,
        Dictionary<string, object>? metadata = null)
    {
        return new PerformanceTimer(logger, operationName, logLevel, metadata);
    }

    /// <summary>
    /// Records a checkpoint with timing information
    /// </summary>
    public void Checkpoint(string checkpointName)
    {
        var elapsed = _stopwatch.Elapsed;
        _logger.Log(_logLevel,
            "Checkpoint {CheckpointName} for {OperationName}: {Duration}ms",
            checkpointName,
            _operationName,
            elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Adds metadata to be logged when the timer completes
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

    /// <summary>
    /// Stops the timer and logs the result
    /// </summary>
    public void Stop(bool success = true, string? errorMessage = null)
    {
        if (_disposed) return;

        _stopwatch.Stop();
        var duration = _stopwatch.Elapsed;

        var logLevel = DetermineLogLevel(duration, success);
        var statusMessage = success ? "completed successfully" : $"failed: {errorMessage}";

        // Build structured log with all metadata
        var state = new List<KeyValuePair<string, object>>
        {
            new("OperationName", _operationName),
            new("Duration", duration),
            new("DurationMs", duration.TotalMilliseconds),
            new("Success", success)
        };

        foreach (var kvp in _metadata)
        {
            state.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
        }

        if (!success && !string.IsNullOrEmpty(errorMessage))
        {
            state.Add(new KeyValuePair<string, object>("ErrorMessage", errorMessage));
        }

        _logger.Log(
            logLevel,
            "Operation {OperationName} {Status} in {Duration}ms {Metadata}",
            _operationName,
            statusMessage,
            duration.TotalMilliseconds,
            _metadata);

        _disposed = true;
    }

    public void Dispose()
    {
        Stop();
    }

    private LogLevel DetermineLogLevel(TimeSpan duration, bool success)
    {
        if (!success)
            return LogLevel.Error;

        // Warn on slow operations (>5 seconds)
        if (duration.TotalSeconds > 5)
            return LogLevel.Warning;

        return _logLevel;
    }
}

/// <summary>
/// Extension methods for performance timing
/// </summary>
public static class PerformanceTimerExtensions
{
    /// <summary>
    /// Times an operation and logs the result
    /// </summary>
    public static async Task<T> TimeOperationAsync<T>(
        this ILogger logger,
        string operationName,
        Func<Task<T>> operation,
        Dictionary<string, object>? metadata = null)
    {
        using var timer = PerformanceTimer.Start(logger, operationName, LogLevel.Information, metadata);
        
        try
        {
            var result = await operation();
            timer.Stop(success: true);
            return result;
        }
        catch (Exception ex)
        {
            timer.Stop(success: false, errorMessage: ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Times an operation and logs the result
    /// </summary>
    public static async Task TimeOperationAsync(
        this ILogger logger,
        string operationName,
        Func<Task> operation,
        Dictionary<string, object>? metadata = null)
    {
        using var timer = PerformanceTimer.Start(logger, operationName, LogLevel.Information, metadata);
        
        try
        {
            await operation();
            timer.Stop(success: true);
        }
        catch (Exception ex)
        {
            timer.Stop(success: false, errorMessage: ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Times a synchronous operation and logs the result
    /// </summary>
    public static T TimeOperation<T>(
        this ILogger logger,
        string operationName,
        Func<T> operation,
        Dictionary<string, object>? metadata = null)
    {
        using var timer = PerformanceTimer.Start(logger, operationName, LogLevel.Information, metadata);
        
        try
        {
            var result = operation();
            timer.Stop(success: true);
            return result;
        }
        catch (Exception ex)
        {
            timer.Stop(success: false, errorMessage: ex.Message);
            throw;
        }
    }
}
