using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience.ErrorTracking;

/// <summary>
/// Collects error metrics for monitoring and alerting
/// </summary>
public class ErrorMetricsCollector
{
    private readonly ILogger<ErrorMetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, ErrorMetrics> _serviceMetrics = new();
    private readonly ConcurrentQueue<ErrorEvent> _recentErrors = new();
    private const int MaxRecentErrors = 1000;

    public ErrorMetricsCollector(ILogger<ErrorMetricsCollector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records an error occurrence
    /// </summary>
    public void RecordError(string serviceName, Exception exception, string? correlationId = null)
    {
        var metrics = _serviceMetrics.GetOrAdd(serviceName, _ => new ErrorMetrics { ServiceName = serviceName });

        metrics.TotalErrors++;
        metrics.LastError = exception;
        metrics.LastErrorTime = DateTime.UtcNow;

        // Categorize the error
        var category = CategorizeError(exception);
        metrics.IncrementCategory(category);

        // Record in recent errors
        var errorEvent = new ErrorEvent
        {
            ServiceName = serviceName,
            Exception = exception,
            Category = category,
            Timestamp = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        _recentErrors.Enqueue(errorEvent);
        
        // Keep only the most recent errors
        while (_recentErrors.Count > MaxRecentErrors)
        {
            _recentErrors.TryDequeue(out _);
        }

        // Check for error rate anomalies
        CheckErrorRate(serviceName, metrics);
    }

    /// <summary>
    /// Records a successful operation (for calculating error rates)
    /// </summary>
    public void RecordSuccess(string serviceName)
    {
        var metrics = _serviceMetrics.GetOrAdd(serviceName, _ => new ErrorMetrics { ServiceName = serviceName });
        metrics.TotalSuccesses++;
    }

    /// <summary>
    /// Gets metrics for a specific service
    /// </summary>
    public ErrorMetrics? GetMetrics(string serviceName)
    {
        return _serviceMetrics.TryGetValue(serviceName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets all service metrics
    /// </summary>
    public IReadOnlyDictionary<string, ErrorMetrics> GetAllMetrics()
    {
        return _serviceMetrics;
    }

    /// <summary>
    /// Gets recent errors
    /// </summary>
    public IEnumerable<ErrorEvent> GetRecentErrors(int count = 100)
    {
        return _recentErrors.TakeLast(count);
    }

    /// <summary>
    /// Gets error rate for a service (errors per minute)
    /// </summary>
    public double GetErrorRate(string serviceName, TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        var errors = _recentErrors
            .Where(e => e.ServiceName == serviceName && e.Timestamp >= cutoff)
            .Count();

        return errors / window.TotalMinutes;
    }

    /// <summary>
    /// Resets metrics for a service
    /// </summary>
    public void ResetMetrics(string serviceName)
    {
        _serviceMetrics.TryRemove(serviceName, out _);
        _logger.LogInformation("Reset error metrics for service {ServiceName}", serviceName);
    }

    private ErrorCategory CategorizeError(Exception exception)
    {
        return exception switch
        {
            TimeoutException => ErrorCategory.Timeout,
            OperationCanceledException => ErrorCategory.Cancellation,
            HttpRequestException => ErrorCategory.Network,
            UnauthorizedAccessException => ErrorCategory.Authentication,
            ArgumentException or ArgumentNullException => ErrorCategory.Validation,
            InvalidOperationException => ErrorCategory.InvalidState,
            _ when exception.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) => ErrorCategory.RateLimit,
            _ when exception.Message.Contains("quota", StringComparison.OrdinalIgnoreCase) => ErrorCategory.Quota,
            _ => ErrorCategory.Unknown
        };
    }

    private void CheckErrorRate(string serviceName, ErrorMetrics metrics)
    {
        var errorRate = GetErrorRate(serviceName, TimeSpan.FromMinutes(1));
        
        // Alert if error rate exceeds threshold (e.g., 10 errors per minute)
        if (errorRate > 10)
        {
            _logger.LogWarning(
                "High error rate detected for service {ServiceName}: {ErrorRate} errors/minute",
                serviceName,
                errorRate);
        }

        // Alert if error rate is increasing rapidly
        var recentRate = GetErrorRate(serviceName, TimeSpan.FromMinutes(1));
        var previousRate = GetErrorRate(serviceName, TimeSpan.FromMinutes(5));
        
        if (recentRate > previousRate * 2 && recentRate > 5)
        {
            _logger.LogWarning(
                "Error rate spike detected for service {ServiceName}: {RecentRate} errors/min (was {PreviousRate})",
                serviceName,
                recentRate,
                previousRate);
        }
    }
}

/// <summary>
/// Error metrics for a service
/// </summary>
public class ErrorMetrics
{
    private readonly ConcurrentDictionary<ErrorCategory, long> _categoryCounts = new();

    public required string ServiceName { get; init; }
    public long TotalErrors { get; set; }
    public long TotalSuccesses { get; set; }
    public Exception? LastError { get; set; }
    public DateTime? LastErrorTime { get; set; }

    public double ErrorRate => TotalErrors + TotalSuccesses > 0
        ? (double)TotalErrors / (TotalErrors + TotalSuccesses)
        : 0;

    public IReadOnlyDictionary<ErrorCategory, long> CategoryCounts => _categoryCounts;

    public void IncrementCategory(ErrorCategory category)
    {
        _categoryCounts.AddOrUpdate(category, 1, (_, count) => count + 1);
    }

    public long GetCategoryCount(ErrorCategory category)
    {
        return _categoryCounts.TryGetValue(category, out var count) ? count : 0;
    }
}

/// <summary>
/// An error event with context
/// </summary>
public class ErrorEvent
{
    public required string ServiceName { get; init; }
    public required Exception Exception { get; init; }
    public required ErrorCategory Category { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Error categories for classification
/// </summary>
public enum ErrorCategory
{
    Unknown,
    Timeout,
    Network,
    Authentication,
    Authorization,
    Validation,
    RateLimit,
    Quota,
    InvalidState,
    Cancellation,
    Configuration,
    Dependency
}
