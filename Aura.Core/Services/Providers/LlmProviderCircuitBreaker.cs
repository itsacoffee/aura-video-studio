using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Circuit breaker pattern implementation for LLM providers
/// Tracks failures and temporarily disables providers that are repeatedly failing
/// </summary>
public class LlmProviderCircuitBreaker
{
    private readonly ILogger<LlmProviderCircuitBreaker> _logger;
    private readonly ConcurrentDictionary<string, LlmCircuitState> _providerStates;
    private readonly TimeSpan _openDuration;
    private readonly int _failureThreshold;

    public LlmProviderCircuitBreaker(
        ILogger<LlmProviderCircuitBreaker> logger,
        TimeSpan? openDuration = null,
        int failureThreshold = 3)
    {
        _logger = logger;
        _providerStates = new ConcurrentDictionary<string, LlmCircuitState>();
        _openDuration = openDuration ?? TimeSpan.FromMinutes(5);
        _failureThreshold = failureThreshold;
    }

    /// <summary>
    /// Check if provider is available (circuit not open)
    /// </summary>
    public bool IsProviderAvailable(string providerName)
    {
        if (!_providerStates.TryGetValue(providerName, out var state))
        {
            return true; // No state = never failed = available
        }

        // If circuit is open and cooldown period has passed, move to half-open
        if (state.Status == CircuitStatus.Open && 
            DateTime.UtcNow >= state.OpenUntil)
        {
            _logger.LogInformation(
                "Circuit for {Provider} moving from Open to HalfOpen (cooldown expired)",
                providerName);
            
            state.Status = CircuitStatus.HalfOpen;
            state.ConsecutiveFailures = 0;
        }

        return state.Status != CircuitStatus.Open;
    }

    /// <summary>
    /// Record successful call - resets failure count
    /// </summary>
    public void RecordSuccess(string providerName)
    {
        if (_providerStates.TryGetValue(providerName, out var state))
        {
            var previousStatus = state.Status;
            
            state.ConsecutiveFailures = 0;
            state.TotalSuccesses++;
            state.LastSuccessTime = DateTime.UtcNow;

            if (previousStatus != CircuitStatus.Closed)
            {
                _logger.LogInformation(
                    "Circuit for {Provider} moving to Closed (success after {Status})",
                    providerName, previousStatus);
                state.Status = CircuitStatus.Closed;
            }
        }
        else
        {
            // First successful call - initialize state
            _providerStates[providerName] = new LlmCircuitState
            {
                Status = CircuitStatus.Closed,
                TotalSuccesses = 1,
                LastSuccessTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Record failed call - may open circuit if threshold exceeded
    /// </summary>
    public void RecordFailure(string providerName, Exception? exception = null)
    {
        var state = _providerStates.GetOrAdd(providerName, _ => new LlmCircuitState());

        state.ConsecutiveFailures++;
        state.TotalFailures++;
        state.LastFailureTime = DateTime.UtcNow;
        state.LastException = exception;

        _logger.LogWarning(
            exception,
            "{Provider} failure recorded ({Consecutive} consecutive, {Total} total)",
            providerName, state.ConsecutiveFailures, state.TotalFailures);

        // Check if we should open the circuit
        if (state.Status == CircuitStatus.Closed && 
            state.ConsecutiveFailures >= _failureThreshold)
        {
            state.Status = CircuitStatus.Open;
            state.OpenUntil = DateTime.UtcNow.Add(_openDuration);

            _logger.LogError(
                "Circuit for {Provider} is now OPEN (threshold: {Threshold}, failures: {Failures}). " +
                "Will retry after {Duration}",
                providerName, _failureThreshold, state.ConsecutiveFailures, _openDuration);
        }
        else if (state.Status == CircuitStatus.HalfOpen)
        {
            // Failure in half-open state means back to open
            state.Status = CircuitStatus.Open;
            state.OpenUntil = DateTime.UtcNow.Add(_openDuration);

            _logger.LogWarning(
                "Circuit for {Provider} moving back to OPEN (failed in HalfOpen state)",
                providerName);
        }
    }

    /// <summary>
    /// Get current state for a provider
    /// </summary>
    public ProviderCircuitInfo GetProviderInfo(string providerName)
    {
        if (!_providerStates.TryGetValue(providerName, out var state))
        {
            return new ProviderCircuitInfo
            {
                ProviderName = providerName,
                Status = CircuitStatus.Closed,
                IsAvailable = true,
                ConsecutiveFailures = 0,
                TotalFailures = 0,
                TotalSuccesses = 0
            };
        }

        return new ProviderCircuitInfo
        {
            ProviderName = providerName,
            Status = state.Status,
            IsAvailable = state.Status != CircuitStatus.Open,
            ConsecutiveFailures = state.ConsecutiveFailures,
            TotalFailures = state.TotalFailures,
            TotalSuccesses = state.TotalSuccesses,
            LastFailureTime = state.LastFailureTime,
            LastSuccessTime = state.LastSuccessTime,
            OpenUntil = state.OpenUntil,
            LastException = state.LastException?.Message
        };
    }

    /// <summary>
    /// Get info for all tracked providers
    /// </summary>
    public ProviderCircuitInfo[] GetAllProviderInfo()
    {
        var result = new ProviderCircuitInfo[_providerStates.Count];
        var index = 0;

        foreach (var kvp in _providerStates)
        {
            result[index++] = GetProviderInfo(kvp.Key);
        }

        return result;
    }

    /// <summary>
    /// Reset circuit state for a provider (manual override)
    /// </summary>
    public void ResetCircuit(string providerName)
    {
        if (_providerStates.TryRemove(providerName, out var state))
        {
            _logger.LogInformation(
                "Circuit for {Provider} manually reset (was: {Status}, failures: {Failures})",
                providerName, state.Status, state.TotalFailures);
        }
    }

    /// <summary>
    /// Reset all circuit states
    /// </summary>
    public void ResetAll()
    {
        var count = _providerStates.Count;
        _providerStates.Clear();
        _logger.LogInformation("All provider circuits reset ({Count} providers)", count);
    }

    /// <summary>
    /// Execute operation with circuit breaker protection
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string providerName,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        if (!IsProviderAvailable(providerName))
        {
            throw new InvalidOperationException(
                $"Provider {providerName} is temporarily unavailable (circuit breaker is open)");
        }

        try
        {
            var result = await operation(ct).ConfigureAwait(false);
            RecordSuccess(providerName);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(providerName, ex);
            throw;
        }
    }
}

/// <summary>
/// Circuit breaker state for a provider
/// </summary>
internal class LlmCircuitState
{
    public CircuitStatus Status { get; set; } = CircuitStatus.Closed;
    public int ConsecutiveFailures { get; set; }
    public int TotalFailures { get; set; }
    public int TotalSuccesses { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? OpenUntil { get; set; }
    public Exception? LastException { get; set; }
}

/// <summary>
/// Circuit breaker status
/// </summary>
public enum CircuitStatus
{
    /// <summary>
    /// Circuit is closed - provider is working normally
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open - provider is temporarily disabled due to failures
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open - testing if provider has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Information about provider circuit breaker state
/// </summary>
public class ProviderCircuitInfo
{
    public required string ProviderName { get; init; }
    public CircuitStatus Status { get; init; }
    public bool IsAvailable { get; init; }
    public int ConsecutiveFailures { get; init; }
    public int TotalFailures { get; init; }
    public int TotalSuccesses { get; init; }
    public DateTime? LastFailureTime { get; init; }
    public DateTime? LastSuccessTime { get; init; }
    public DateTime? OpenUntil { get; init; }
    public string? LastException { get; init; }
}
