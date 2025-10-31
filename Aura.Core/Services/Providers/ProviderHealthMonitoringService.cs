using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Monitors health metrics for all LLM providers including success rate,
/// latency, and consecutive failures. Tracks rolling window of last 100 requests.
/// </summary>
public class ProviderHealthMonitoringService
{
    private readonly ILogger<ProviderHealthMonitoringService> _logger;
    private readonly Dictionary<string, ProviderHealthTracker> _trackers = new();
    private readonly object _lock = new();

    private const int MaxTrackedRequests = 100;

    public ProviderHealthMonitoringService(ILogger<ProviderHealthMonitoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Record a successful provider request
    /// </summary>
    public void RecordSuccess(string providerName, double latencySeconds)
    {
        lock (_lock)
        {
            var tracker = GetOrCreateTracker(providerName);
            tracker.RecordSuccess(latencySeconds);
            
            _logger.LogDebug(
                "Provider {ProviderName} success recorded. Success rate: {SuccessRate:P1}, Avg latency: {LatencySeconds:F2}s",
                providerName, tracker.SuccessRate, tracker.AverageLatency);
        }
    }

    /// <summary>
    /// Record a failed provider request
    /// </summary>
    public void RecordFailure(string providerName, string? errorMessage = null)
    {
        lock (_lock)
        {
            var tracker = GetOrCreateTracker(providerName);
            tracker.RecordFailure(errorMessage);
            
            _logger.LogWarning(
                "Provider {ProviderName} failure recorded. Success rate: {SuccessRate:P1}, Consecutive failures: {ConsecutiveFailures}",
                providerName, tracker.SuccessRate, tracker.ConsecutiveFailures);

            if (tracker.ConsecutiveFailures >= 5)
            {
                _logger.LogError(
                    "Provider {ProviderName} has {ConsecutiveFailures} consecutive failures - recommend switching provider",
                    providerName, tracker.ConsecutiveFailures);
            }
        }
    }

    /// <summary>
    /// Get current health metrics for a provider
    /// </summary>
    public ProviderHealthMetrics? GetProviderHealth(string providerName)
    {
        lock (_lock)
        {
            if (!_trackers.TryGetValue(providerName, out var tracker))
            {
                return null;
            }

            var successRate = tracker.SuccessRate * 100;
            var status = DetermineStatus(successRate, tracker.ConsecutiveFailures);

            return new ProviderHealthMetrics
            {
                ProviderName = providerName,
                SuccessRatePercent = successRate,
                AverageLatencySeconds = tracker.AverageLatency,
                TotalRequests = tracker.TotalRequests,
                ConsecutiveFailures = tracker.ConsecutiveFailures,
                Status = status,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get health metrics for all tracked providers
    /// </summary>
    public List<ProviderHealthMetrics> GetAllProviderHealth()
    {
        lock (_lock)
        {
            return _trackers.Keys
                .Select(GetProviderHealth)
                .Where(m => m != null)
                .Cast<ProviderHealthMetrics>()
                .ToList();
        }
    }

    /// <summary>
    /// Reset health metrics for a provider (useful for testing or after fixing issues)
    /// </summary>
    public void ResetProviderHealth(string providerName)
    {
        lock (_lock)
        {
            if (_trackers.ContainsKey(providerName))
            {
                _trackers.Remove(providerName);
                _logger.LogInformation("Reset health metrics for provider {ProviderName}", providerName);
            }
        }
    }

    /// <summary>
    /// Check if a provider should trigger an alert based on failures
    /// </summary>
    public bool ShouldAlertUser(string providerName)
    {
        lock (_lock)
        {
            if (!_trackers.TryGetValue(providerName, out var tracker))
            {
                return false;
            }

            return tracker.ConsecutiveFailures >= 5;
        }
    }

    private ProviderHealthTracker GetOrCreateTracker(string providerName)
    {
        if (!_trackers.TryGetValue(providerName, out var tracker))
        {
            tracker = new ProviderHealthTracker(MaxTrackedRequests);
            _trackers[providerName] = tracker;
        }
        return tracker;
    }

    private static ProviderHealthStatus DetermineStatus(double successRatePercent, int consecutiveFailures)
    {
        if (consecutiveFailures >= 5)
        {
            return ProviderHealthStatus.Unhealthy;
        }

        if (successRatePercent >= 90)
        {
            return ProviderHealthStatus.Healthy;
        }
        else if (successRatePercent >= 70)
        {
            return ProviderHealthStatus.Degraded;
        }
        else
        {
            return ProviderHealthStatus.Unhealthy;
        }
    }
}

/// <summary>
/// Internal tracker for a single provider's health metrics
/// </summary>
internal class ProviderHealthTracker
{
    private readonly Queue<RequestResult> _requestHistory;
    private readonly int _maxRequests;
    private int _consecutiveFailures;

    public ProviderHealthTracker(int maxRequests)
    {
        _maxRequests = maxRequests;
        _requestHistory = new Queue<RequestResult>(maxRequests);
    }

    public int TotalRequests => _requestHistory.Count;
    public int ConsecutiveFailures => _consecutiveFailures;

    public double SuccessRate
    {
        get
        {
            if (_requestHistory.Count == 0)
            {
                return 1.0;
            }

            var successCount = _requestHistory.Count(r => r.Success);
            return (double)successCount / _requestHistory.Count;
        }
    }

    public double AverageLatency
    {
        get
        {
            var successfulRequests = _requestHistory.Where(r => r.Success && r.LatencySeconds.HasValue).ToList();
            if (successfulRequests.Count == 0)
            {
                return 0;
            }

            return successfulRequests.Average(r => r.LatencySeconds!.Value);
        }
    }

    public void RecordSuccess(double latencySeconds)
    {
        AddRequest(new RequestResult
        {
            Success = true,
            LatencySeconds = latencySeconds,
            Timestamp = DateTime.UtcNow
        });

        _consecutiveFailures = 0;
    }

    public void RecordFailure(string? errorMessage)
    {
        AddRequest(new RequestResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        });

        _consecutiveFailures++;
    }

    private void AddRequest(RequestResult result)
    {
        if (_requestHistory.Count >= _maxRequests)
        {
            _requestHistory.Dequeue();
        }

        _requestHistory.Enqueue(result);
    }
}

/// <summary>
/// Result of a single provider request
/// </summary>
internal class RequestResult
{
    public required bool Success { get; init; }
    public double? LatencySeconds { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; }
}
