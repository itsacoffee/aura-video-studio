using System;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Health status of an LLM provider for circuit breaker pattern.
/// </summary>
public enum ProviderHealthState
{
    /// <summary>
    /// Provider is healthy and available.
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is experiencing degraded performance.
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is unavailable or circuit is open.
    /// </summary>
    Unavailable
}

/// <summary>
/// Health status information for a provider.
/// </summary>
public class ProviderHealthStatus
{
    public string ProviderName { get; set; } = string.Empty;
    public ProviderHealthState State { get; set; }
    public DateTime LastCheckTime { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public DateTime? CircuitOpenedAt { get; set; }
    public TimeSpan? CircuitResetIn { get; set; }

    /// <summary>
    /// Calculate success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalRequests > 0 
        ? (double)SuccessfulRequests / TotalRequests * 100 
        : 0;

    /// <summary>
    /// Health score from 0.0 to 1.0 based on success rate and state.
    /// </summary>
    public double HealthScore
    {
        get
        {
            if (State == ProviderHealthState.Unavailable)
                return 0.0;

            if (State == ProviderHealthState.Degraded)
                return 0.5;

            return Math.Min(1.0, SuccessRate / 100.0);
        }
    }
}

/// <summary>
/// Provider performance metrics for routing decisions.
/// </summary>
public class ProviderMetrics
{
    public string ProviderName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public double AverageLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public decimal AverageCost { get; set; }
    public double QualityScore { get; set; }
    public int RequestCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
