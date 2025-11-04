using System.Collections.Generic;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Configuration for routing policies per task type.
/// </summary>
public class RoutingPolicy
{
    /// <summary>
    /// Task type this policy applies to.
    /// </summary>
    public TaskType TaskType { get; set; }

    /// <summary>
    /// Ordered list of preferred providers for this task (highest priority first).
    /// </summary>
    public List<ProviderPreference> PreferredProviders { get; set; } = new();

    /// <summary>
    /// Default constraints for this task type.
    /// </summary>
    public RoutingConstraints DefaultConstraints { get; set; } = new();

    /// <summary>
    /// Whether to enable failover to lower-priority providers.
    /// </summary>
    public bool EnableFailover { get; set; } = true;

    /// <summary>
    /// Maximum number of failover attempts.
    /// </summary>
    public int MaxFailoverAttempts { get; set; } = 3;
}

/// <summary>
/// Provider preference configuration for a task type.
/// </summary>
public class ProviderPreference
{
    /// <summary>
    /// Provider name (OpenAI, Ollama, Anthropic, etc.).
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use with this provider.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Priority rank (1 = highest priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Expected quality score for this provider/model combination (0.0 to 1.0).
    /// </summary>
    public double QualityScore { get; set; } = 0.8;

    /// <summary>
    /// Expected cost per request in USD.
    /// </summary>
    public decimal CostPerRequest { get; set; } = 0.01m;

    /// <summary>
    /// Expected latency in milliseconds.
    /// </summary>
    public int ExpectedLatencyMs { get; set; } = 5000;

    /// <summary>
    /// Required context length for this task.
    /// </summary>
    public int ContextLength { get; set; } = 4096;
}

/// <summary>
/// Complete routing configuration for all task types.
/// </summary>
public class RoutingConfiguration
{
    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Health check configuration.
    /// </summary>
    public HealthCheckConfig HealthCheck { get; set; } = new();

    /// <summary>
    /// Cost tracking configuration.
    /// </summary>
    public CostTrackingConfig CostTracking { get; set; } = new();

    /// <summary>
    /// Routing policies per task type.
    /// </summary>
    public List<RoutingPolicy> Policies { get; set; } = new();

    /// <summary>
    /// Whether to enable automatic failover.
    /// </summary>
    public bool EnableFailover { get; set; } = true;

    /// <summary>
    /// Whether to enable cost tracking.
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;
}

/// <summary>
/// Circuit breaker configuration.
/// </summary>
public class CircuitBreakerConfig
{
    /// <summary>
    /// Number of consecutive failures before opening circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep circuit open before attempting reset.
    /// </summary>
    public int OpenDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Success rate threshold to keep circuit closed (percentage).
    /// </summary>
    public double SuccessRateThreshold { get; set; } = 80.0;

    /// <summary>
    /// Minimum number of requests before evaluating success rate.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthCheckConfig
{
    /// <summary>
    /// Interval between health checks in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Timeout for health check requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to run health checks in background.
    /// </summary>
    public bool EnableBackgroundChecks { get; set; } = true;
}

/// <summary>
/// Cost tracking configuration.
/// </summary>
public class CostTrackingConfig
{
    /// <summary>
    /// Maximum cost per request before rejecting.
    /// </summary>
    public decimal MaxCostPerRequest { get; set; } = 0.50m;

    /// <summary>
    /// Maximum total cost per hour.
    /// </summary>
    public decimal MaxCostPerHour { get; set; } = 10.00m;

    /// <summary>
    /// Maximum total cost per day.
    /// </summary>
    public decimal MaxCostPerDay { get; set; } = 50.00m;

    /// <summary>
    /// Whether to enforce budget limits.
    /// </summary>
    public bool EnforceBudgetLimits { get; set; } = true;
}
