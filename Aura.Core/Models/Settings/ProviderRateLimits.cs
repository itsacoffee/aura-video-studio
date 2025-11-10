using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Settings;

/// <summary>
/// Provider rate limiting and cost management settings
/// </summary>
public class ProviderRateLimits
{
    /// <summary>
    /// Rate limits per provider
    /// </summary>
    public Dictionary<string, ProviderRateLimit> Limits { get; set; } = new();

    /// <summary>
    /// Global rate limit settings
    /// </summary>
    public GlobalRateLimitSettings Global { get; set; } = new();
}

/// <summary>
/// Rate limit configuration for a specific provider
/// </summary>
public class ProviderRateLimit
{
    /// <summary>
    /// Provider name/identifier
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Enable rate limiting for this provider
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum requests per minute (0 = unlimited)
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Maximum requests per hour (0 = unlimited)
    /// </summary>
    public int MaxRequestsPerHour { get; set; } = 1000;

    /// <summary>
    /// Maximum requests per day (0 = unlimited)
    /// </summary>
    public int MaxRequestsPerDay { get; set; } = 10000;

    /// <summary>
    /// Maximum concurrent requests (0 = unlimited)
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// Maximum tokens per request (for LLM providers)
    /// </summary>
    public int MaxTokensPerRequest { get; set; } = 4096;

    /// <summary>
    /// Maximum tokens per minute (for LLM providers)
    /// </summary>
    public int MaxTokensPerMinute { get; set; } = 90000;

    /// <summary>
    /// Daily cost limit in USD (0 = unlimited)
    /// </summary>
    public decimal DailyCostLimit { get; set; } = 0;

    /// <summary>
    /// Monthly cost limit in USD (0 = unlimited)
    /// </summary>
    public decimal MonthlyCostLimit { get; set; } = 0;

    /// <summary>
    /// Behavior when rate limit is exceeded
    /// </summary>
    public RateLimitBehavior ExceededBehavior { get; set; } = RateLimitBehavior.Queue;

    /// <summary>
    /// Priority level for this provider (higher = more important)
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Fallback provider to use when this one is rate limited
    /// </summary>
    public string? FallbackProvider { get; set; }

    /// <summary>
    /// Custom retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum retries before giving up
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Cost warning threshold percentage (0-100)
    /// </summary>
    public int CostWarningThreshold { get; set; } = 80;

    /// <summary>
    /// Send notifications when rate limit is reached
    /// </summary>
    public bool NotifyOnLimitReached { get; set; } = true;
}

/// <summary>
/// Global rate limiting settings
/// </summary>
public class GlobalRateLimitSettings
{
    /// <summary>
    /// Enable global rate limiting across all providers
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum total API calls per minute across all providers
    /// </summary>
    public int MaxTotalRequestsPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum total cost per day across all providers in USD
    /// </summary>
    public decimal MaxTotalDailyCost { get; set; } = 50;

    /// <summary>
    /// Maximum total cost per month across all providers in USD
    /// </summary>
    public decimal MaxTotalMonthlyCost { get; set; } = 500;

    /// <summary>
    /// Behavior when global limit is exceeded
    /// </summary>
    public RateLimitBehavior GlobalExceededBehavior { get; set; } = RateLimitBehavior.Block;

    /// <summary>
    /// Enable circuit breaker pattern for failing providers
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Number of failures before opening circuit
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker timeout in seconds
    /// </summary>
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable intelligent load balancing across providers
    /// </summary>
    public bool EnableLoadBalancing { get; set; } = true;

    /// <summary>
    /// Load balancing strategy
    /// </summary>
    public LoadBalancingStrategy LoadBalancingStrategy { get; set; } = LoadBalancingStrategy.LeastCost;
}

/// <summary>
/// Behavior when rate limit is exceeded
/// </summary>
public enum RateLimitBehavior
{
    /// <summary>Block the request immediately</summary>
    Block,
    
    /// <summary>Queue the request and retry later</summary>
    Queue,
    
    /// <summary>Use fallback provider if available</summary>
    Fallback,
    
    /// <summary>Warn user but allow request</summary>
    Warn
}

/// <summary>
/// Load balancing strategy for provider selection
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>Round robin across providers</summary>
    RoundRobin,
    
    /// <summary>Choose provider with lowest current load</summary>
    LeastLoaded,
    
    /// <summary>Choose provider with lowest cost</summary>
    LeastCost,
    
    /// <summary>Choose provider with best latency</summary>
    LowestLatency,
    
    /// <summary>Choose provider based on priority</summary>
    Priority,
    
    /// <summary>Random selection</summary>
    Random
}

/// <summary>
/// Rate limit status information
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Current requests this minute
    /// </summary>
    public int CurrentMinuteRequests { get; set; }

    /// <summary>
    /// Current requests this hour
    /// </summary>
    public int CurrentHourRequests { get; set; }

    /// <summary>
    /// Current requests today
    /// </summary>
    public int CurrentDayRequests { get; set; }

    /// <summary>
    /// Current concurrent requests
    /// </summary>
    public int CurrentConcurrentRequests { get; set; }

    /// <summary>
    /// Current cost today in USD
    /// </summary>
    public decimal CurrentDayCost { get; set; }

    /// <summary>
    /// Current cost this month in USD
    /// </summary>
    public decimal CurrentMonthCost { get; set; }

    /// <summary>
    /// Is rate limited
    /// </summary>
    public bool IsRateLimited { get; set; }

    /// <summary>
    /// Time until rate limit resets
    /// </summary>
    public TimeSpan? ResetIn { get; set; }

    /// <summary>
    /// Percentage of daily cost limit used (0-100)
    /// </summary>
    public double DailyCostLimitPercentage { get; set; }

    /// <summary>
    /// Percentage of monthly cost limit used (0-100)
    /// </summary>
    public double MonthlyCostLimitPercentage { get; set; }

    /// <summary>
    /// Last request timestamp
    /// </summary>
    public DateTime? LastRequestAt { get; set; }

    /// <summary>
    /// Circuit breaker state
    /// </summary>
    public CircuitBreakerState CircuitState { get; set; } = CircuitBreakerState.Closed;
}

/// <summary>
/// Circuit breaker state
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>Normal operation</summary>
    Closed,
    
    /// <summary>Provider is failing, requests blocked</summary>
    Open,
    
    /// <summary>Testing if provider has recovered</summary>
    HalfOpen
}
