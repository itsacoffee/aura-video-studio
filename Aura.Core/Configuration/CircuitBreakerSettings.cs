using System;

namespace Aura.Core.Configuration;

/// <summary>
/// Configuration settings for circuit breaker pattern in provider health monitoring
/// </summary>
public class CircuitBreakerSettings
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit (default: 5)
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Failure rate threshold (0.0-1.0) before opening the circuit (default: 0.5 = 50%)
    /// </summary>
    public double FailureRateThreshold { get; set; } = 0.5;

    /// <summary>
    /// Duration in seconds the circuit stays open before entering half-open state (default: 30)
    /// </summary>
    public int OpenDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for provider health checks (default: 10)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Shorter timeout for health check probes (default: 2 seconds)
    /// </summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 2;

    /// <summary>
    /// Size of the rolling window for calculating failure rate (default: 100 requests)
    /// </summary>
    public int RollingWindowSize { get; set; } = 100;

    /// <summary>
    /// Time span for the rolling window in minutes (default: 5 minutes)
    /// </summary>
    public int RollingWindowMinutes { get; set; } = 5;
}
