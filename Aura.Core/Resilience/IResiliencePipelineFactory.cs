using Polly;

namespace Aura.Core.Resilience;

/// <summary>
/// Factory for creating resilience pipelines for different service types and providers
/// </summary>
public interface IResiliencePipelineFactory
{
    /// <summary>
    /// Gets a resilience pipeline for a specific provider
    /// </summary>
    ResiliencePipeline<TResult> GetPipeline<TResult>(string providerName);

    /// <summary>
    /// Gets a resilience pipeline for HTTP operations
    /// </summary>
    ResiliencePipeline<HttpResponseMessage> GetHttpPipeline(string serviceName);

    /// <summary>
    /// Creates a custom resilience pipeline with specified options
    /// </summary>
    ResiliencePipeline<TResult> CreateCustomPipeline<TResult>(ResiliencePipelineOptions options);
}

/// <summary>
/// Options for configuring a resilience pipeline
/// </summary>
public class ResiliencePipelineOptions
{
    /// <summary>
    /// Name of the service/provider
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Enable retry policy
    /// </summary>
    public bool EnableRetry { get; init; } = true;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay for retry (will use exponential backoff)
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Enable circuit breaker
    /// </summary>
    public bool EnableCircuitBreaker { get; init; } = true;

    /// <summary>
    /// Failure ratio threshold for circuit breaker (0.0-1.0)
    /// </summary>
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;

    /// <summary>
    /// Minimum throughput before circuit breaker activates
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; init; } = 3;

    /// <summary>
    /// Duration the circuit breaker stays open
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable timeout policy
    /// </summary>
    public bool EnableTimeout { get; init; } = true;

    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable jitter for retry delays
    /// </summary>
    public bool UseJitter { get; init; } = true;
}
