using Aura.Core.Configuration;
using Aura.Core.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Net;

namespace Aura.Core.Resilience;

/// <summary>
/// Factory for creating and caching resilience pipelines for different services
/// </summary>
public class ResiliencePipelineFactory : IResiliencePipelineFactory
{
    private readonly ILogger<ResiliencePipelineFactory> _logger;
    private readonly CircuitBreakerSettings _circuitBreakerSettings;
    private readonly ConcurrentDictionary<string, object> _pipelines = new();

    public ResiliencePipelineFactory(
        ILogger<ResiliencePipelineFactory> logger,
        IOptions<CircuitBreakerSettings> circuitBreakerSettings)
    {
        _logger = logger;
        _circuitBreakerSettings = circuitBreakerSettings.Value;
    }

    /// <inheritdoc />
    public ResiliencePipeline<TResult> GetPipeline<TResult>(string providerName)
    {
        var key = $"{providerName}_{typeof(TResult).Name}";
        
        return (ResiliencePipeline<TResult>)_pipelines.GetOrAdd(key, _ =>
        {
            var options = GetOptionsForProvider(providerName);
            return CreatePipelineInternal<TResult>(options);
        });
    }

    /// <inheritdoc />
    public ResiliencePipeline<HttpResponseMessage> GetHttpPipeline(string serviceName)
    {
        var key = $"http_{serviceName}";
        
        return (ResiliencePipeline<HttpResponseMessage>)_pipelines.GetOrAdd(key, _ =>
        {
            var options = GetOptionsForHttpService(serviceName);
            return CreateHttpPipeline(options);
        });
    }

    /// <inheritdoc />
    public ResiliencePipeline<TResult> CreateCustomPipeline<TResult>(ResiliencePipelineOptions options)
    {
        return CreatePipelineInternal<TResult>(options);
    }

    private ResiliencePipeline<TResult> CreatePipelineInternal<TResult>(ResiliencePipelineOptions options)
    {
        var builder = new ResiliencePipelineBuilder<TResult>();

        // Add retry policy if enabled
        if (options.EnableRetry)
        {
            builder.AddRetry(new RetryStrategyOptions<TResult>
            {
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<ProviderException>(ex => ex.IsTransient)
                    .Handle<OperationCanceledException>(),
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = options.UseJitter,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {AttemptNumber}/{MaxAttempts} for {ServiceName} after {Delay}ms. Reason: {Exception}",
                        args.AttemptNumber,
                        options.MaxRetryAttempts,
                        options.Name,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Transient error");
                    return ValueTask.CompletedTask;
                }
            });
        }

        // Add circuit breaker if enabled
        if (options.EnableCircuitBreaker)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
            {
                FailureRatio = options.CircuitBreakerFailureRatio,
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(_circuitBreakerSettings.RollingWindowMinutes * 60 / 5),
                BreakDuration = options.CircuitBreakerBreakDuration,
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .Handle<ProviderException>(ex => !ex.IsTransient),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker OPENED for {ServiceName}. Service will be unavailable for {BreakDuration}s",
                        options.Name,
                        options.CircuitBreakerBreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker CLOSED for {ServiceName}. Service recovered and is now available",
                        options.Name);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker HALF-OPEN for {ServiceName}. Testing if service has recovered",
                        options.Name);
                    return ValueTask.CompletedTask;
                }
            });
        }

        // Add timeout if enabled
        if (options.EnableTimeout)
        {
            builder.AddTimeout(options.Timeout);
        }

        return builder.Build();
    }

    private ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(ResiliencePipelineOptions options)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        // Add retry policy with HTTP-specific handling
        if (options.EnableRetry)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => ShouldRetryHttpResponse(response)),
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = options.UseJitter,
                OnRetry = args =>
                {
                    var statusCode = args.Outcome.Result?.StatusCode;
                    _logger.LogWarning(
                        "HTTP retry attempt {AttemptNumber}/{MaxAttempts} for {ServiceName} (Status: {StatusCode})",
                        args.AttemptNumber,
                        options.MaxRetryAttempts,
                        options.Name,
                        statusCode);
                    return ValueTask.CompletedTask;
                }
            });
        }

        // Add circuit breaker
        if (options.EnableCircuitBreaker)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = options.CircuitBreakerFailureRatio,
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = options.CircuitBreakerBreakDuration,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                                             response.StatusCode == HttpStatusCode.BadGateway ||
                                             response.StatusCode == HttpStatusCode.GatewayTimeout),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "HTTP Circuit breaker OPENED for {ServiceName}",
                        options.Name);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "HTTP Circuit breaker CLOSED for {ServiceName}",
                        options.Name);
                    return ValueTask.CompletedTask;
                }
            });
        }

        // Add timeout
        if (options.EnableTimeout)
        {
            builder.AddTimeout(options.Timeout);
        }

        return builder.Build();
    }

    private ResiliencePipelineOptions GetOptionsForProvider(string providerName)
    {
        // Customize options based on provider type
        if (providerName.Contains("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            return new ResiliencePipelineOptions
            {
                Name = providerName,
                MaxRetryAttempts = 4,
                RetryDelay = TimeSpan.FromSeconds(2),
                Timeout = TimeSpan.FromSeconds(60),
                CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60)
            };
        }
        
        if (providerName.Contains("Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return new ResiliencePipelineOptions
            {
                Name = providerName,
                MaxRetryAttempts = 4,
                RetryDelay = TimeSpan.FromSeconds(2),
                Timeout = TimeSpan.FromSeconds(60),
                CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60)
            };
        }
        
        if (providerName.Contains("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            return new ResiliencePipelineOptions
            {
                Name = providerName,
                MaxRetryAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(100),
                Timeout = TimeSpan.FromSeconds(30),
                CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
                UseJitter = false
            };
        }

        // Default options for other providers
        return new ResiliencePipelineOptions
        {
            Name = providerName,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private ResiliencePipelineOptions GetOptionsForHttpService(string serviceName)
    {
        return new ResiliencePipelineOptions
        {
            Name = serviceName,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            Timeout = TimeSpan.FromSeconds(30),
            CircuitBreakerMinimumThroughput = 5,
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30)
        };
    }

    private static bool ShouldRetryHttpResponse(HttpResponseMessage response)
    {
        // Retry on transient errors
        return response.StatusCode == HttpStatusCode.RequestTimeout ||
               response.StatusCode == HttpStatusCode.TooManyRequests ||
               response.StatusCode == HttpStatusCode.ServiceUnavailable ||
               response.StatusCode == HttpStatusCode.BadGateway ||
               response.StatusCode == HttpStatusCode.GatewayTimeout ||
               (int)response.StatusCode >= 500;
    }
}
