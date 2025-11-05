using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Aura.Core.AI.Validation;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;
using CoreValidationResult = Aura.Core.Validation.ValidationResult;

namespace Aura.Core.Orchestration;

/// <summary>
/// Base class for unified orchestration of all generation stages (LLM, TTS, Visual)
/// Provides common functionality: retries, fallbacks, caching, cost tracking, telemetry
/// </summary>
public abstract class UnifiedGenerationOrchestrator<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    protected readonly ILogger Logger;
    private readonly ILlmCache? _cache;
    private readonly SchemaValidator? _schemaValidator;
    private readonly EnhancedCostTrackingService? _costTrackingService;
    private readonly TokenTrackingService? _tokenTrackingService;

    protected UnifiedGenerationOrchestrator(
        ILogger logger,
        ILlmCache? cache = null,
        SchemaValidator? schemaValidator = null,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache;
        _schemaValidator = schemaValidator;
        _costTrackingService = costTrackingService;
        _tokenTrackingService = tokenTrackingService;
    }

    /// <summary>
    /// Execute a generation operation with full orchestration (retry, fallback, caching, validation)
    /// </summary>
    public async Task<OrchestrationResult<TResponse>> ExecuteAsync(
        TRequest request,
        OrchestrationConfig config,
        CancellationToken ct = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        Logger.LogInformation(
            "Starting orchestration {OperationId}: {Stage}",
            operationId, GetStageName());

        var cacheKey = config.EnableCache ? await GetCacheKeyAsync(request, ct) : null;
        
        if (cacheKey != null && _cache != null)
        {
            var cachedResult = await _cache.GetAsync<TResponse>(cacheKey, ct);
            if (cachedResult != null)
            {
                Logger.LogInformation("Cache hit for operation {OperationId}", operationId);
                return OrchestrationResult<TResponse>.Success(
                    cachedResult,
                    operationId,
                    stopwatch.ElapsedMilliseconds,
                    true);
            }
        }

        var providers = await GetProvidersAsync(config, ct);
        Exception? lastException = null;
        
        foreach (var provider in providers)
        {
            var attempt = 0;
            var maxRetries = config.MaxRetries;

            while (attempt <= maxRetries)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    Logger.LogInformation(
                        "Executing {Stage} with provider {Provider}, attempt {Attempt}/{MaxRetries}",
                        GetStageName(), provider.Name, attempt + 1, maxRetries + 1);

                    var response = await ExecuteProviderAsync(provider, request, config, ct);

                    if (config.ValidateSchema && _schemaValidator != null)
                    {
                        var validationResult = await ValidateResponseAsync(response, ct);
                        if (!validationResult.IsValid)
                        {
                            Logger.LogWarning(
                                "Schema validation failed for {Stage}: {Errors}",
                                GetStageName(), string.Join("; ", validationResult.Issues));
                            
                            if (config.StrictValidation)
                            {
                                throw new InvalidOperationException(
                                    $"Schema validation failed: {string.Join("; ", validationResult.Issues)}");
                            }
                        }
                    }

                    await TrackCostAsync(provider, request, response, ct);

                    if (cacheKey != null && _cache != null)
                    {
                        await _cache.SetAsync(cacheKey, response, config.CacheTtlSeconds, ct);
                    }

                    Logger.LogInformation(
                        "Successfully completed {Stage} with provider {Provider} in {ElapsedMs}ms",
                        GetStageName(), provider.Name, stopwatch.ElapsedMilliseconds);

                    return OrchestrationResult<TResponse>.Success(
                        response,
                        operationId,
                        stopwatch.ElapsedMilliseconds,
                        false,
                        provider.Name);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning("Operation {OperationId} was cancelled", operationId);
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Logger.LogWarning(
                        ex,
                        "Attempt {Attempt}/{MaxRetries} failed for {Stage} with provider {Provider}: {Message}",
                        attempt + 1, maxRetries + 1, GetStageName(), provider.Name, ex.Message);

                    if (attempt < maxRetries)
                    {
                        var delay = CalculateBackoffDelay(attempt, config.BackoffStrategy);
                        Logger.LogInformation("Waiting {DelayMs}ms before retry", delay);
                        await Task.Delay(TimeSpan.FromMilliseconds(delay), ct);
                    }
                    
                    attempt++;
                }
            }

            Logger.LogWarning(
                "All attempts failed for provider {Provider}, trying next provider",
                provider.Name);
        }

        Logger.LogError(
            lastException,
            "All providers failed for {Stage} operation {OperationId}",
            GetStageName(), operationId);

        return OrchestrationResult<TResponse>.Failure(
            operationId,
            stopwatch.ElapsedMilliseconds,
            lastException?.Message ?? "All providers failed");
    }

    /// <summary>
    /// Get the name of this orchestration stage (for logging)
    /// </summary>
    protected abstract string GetStageName();

    /// <summary>
    /// Get ordered list of providers to try
    /// </summary>
    protected abstract Task<ProviderInfo[]> GetProvidersAsync(
        OrchestrationConfig config,
        CancellationToken ct);

    /// <summary>
    /// Execute the actual provider call
    /// </summary>
    protected abstract Task<TResponse> ExecuteProviderAsync(
        ProviderInfo provider,
        TRequest request,
        OrchestrationConfig config,
        CancellationToken ct);

    /// <summary>
    /// Validate the response against schema (optional)
    /// </summary>
    protected virtual Task<CoreValidationResult> ValidateResponseAsync(
        TResponse response,
        CancellationToken ct)
    {
        return Task.FromResult(new CoreValidationResult(true, new List<string>()));
    }

    /// <summary>
    /// Generate cache key for this request (optional)
    /// </summary>
    protected virtual Task<string?> GetCacheKeyAsync(TRequest request, CancellationToken ct)
    {
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Track cost for this operation (optional)
    /// </summary>
    protected virtual Task TrackCostAsync(
        ProviderInfo provider,
        TRequest request,
        TResponse response,
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private int CalculateBackoffDelay(int attempt, BackoffStrategy strategy)
    {
        return strategy switch
        {
            BackoffStrategy.Linear => (attempt + 1) * 1000,
            BackoffStrategy.Exponential => (int)Math.Pow(2, attempt) * 1000,
            BackoffStrategy.Fibonacci => Fibonacci(attempt + 1) * 1000,
            _ => 1000
        };
    }

    private int Fibonacci(int n)
    {
        if (n <= 1) return n;
        int a = 0, b = 1;
        for (int i = 2; i <= n; i++)
        {
            int temp = a + b;
            a = b;
            b = temp;
        }
        return b;
    }
}

/// <summary>
/// Information about a provider
/// </summary>
public record ProviderInfo(
    string Name,
    string Model,
    int Priority,
    object Implementation);

/// <summary>
/// Configuration for orchestration
/// </summary>
public record OrchestrationConfig
{
    public int MaxRetries { get; init; } = 2;
    public BackoffStrategy BackoffStrategy { get; init; } = BackoffStrategy.Exponential;
    public bool EnableCache { get; init; } = true;
    public int CacheTtlSeconds { get; init; } = 3600;
    public bool ValidateSchema { get; init; } = true;
    public bool StrictValidation { get; init; } = false;
    public string[]? ProviderChain { get; init; }
    public bool OfflineOnly { get; init; } = false;
    public string? PreferredTier { get; init; }
}

/// <summary>
/// Backoff strategy for retries
/// </summary>
public enum BackoffStrategy
{
    Linear,
    Exponential,
    Fibonacci
}

/// <summary>
/// Result of an orchestration operation
/// </summary>
public record OrchestrationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string OperationId { get; init; } = string.Empty;
    public long ElapsedMs { get; init; }
    public bool WasCached { get; init; }
    public string? ProviderUsed { get; init; }
    public string? ErrorMessage { get; init; }

    public static OrchestrationResult<T> Success(
        T data,
        string operationId,
        long elapsedMs,
        bool wasCached,
        string? providerUsed = null)
    {
        return new OrchestrationResult<T>
        {
            IsSuccess = true,
            Data = data,
            OperationId = operationId,
            ElapsedMs = elapsedMs,
            WasCached = wasCached,
            ProviderUsed = providerUsed
        };
    }

    public static OrchestrationResult<T> Failure(
        string operationId,
        long elapsedMs,
        string errorMessage)
    {
        return new OrchestrationResult<T>
        {
            IsSuccess = false,
            Data = default,
            OperationId = operationId,
            ElapsedMs = elapsedMs,
            WasCached = false,
            ErrorMessage = errorMessage
        };
    }
}
