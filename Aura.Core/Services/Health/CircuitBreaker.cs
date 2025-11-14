using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Health;

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>Circuit is closed, requests are allowed through</summary>
    Closed,
    
    /// <summary>Circuit is open, requests are blocked</summary>
    Open,
    
    /// <summary>Circuit is half-open, testing if provider has recovered</summary>
    HalfOpen
}

/// <summary>
/// Circuit breaker implementation for provider health management
/// </summary>
public class CircuitBreaker
{
    private readonly string _providerName;
    private readonly CircuitBreakerSettings _settings;
    private readonly ILogger _logger;
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTime _openedAt = DateTime.MinValue;
    private int _consecutiveFailures;
    private readonly ConcurrentQueue<(DateTime Timestamp, bool Success)> _recentAttempts = new();
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    public CircuitBreakerState State => _state;
    public DateTime OpenedAt => _openedAt;
    public int ConsecutiveFailures => _consecutiveFailures;

    public CircuitBreaker(
        string providerName,
        CircuitBreakerSettings settings,
        ILogger logger)
    {
        _providerName = providerName;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Execute an action through the circuit breaker
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        await EnsureStateAsync(ct).ConfigureAwait(false);

        if (_state == CircuitBreakerState.Open)
        {
            _logger.LogWarning(
                "Circuit breaker is OPEN for {ProviderName}, rejecting request",
                _providerName);
            throw new CircuitBreakerOpenException(
                $"Circuit breaker is open for {_providerName}. Provider is unavailable.");
        }

        try
        {
            var result = await action(ct).ConfigureAwait(false);
            await RecordSuccessAsync(ct).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(ex, ct).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Record a successful operation
    /// </summary>
    public async Task RecordSuccessAsync(CancellationToken ct = default)
    {
        await _stateLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _recentAttempts.Enqueue((DateTime.UtcNow, true));
            CleanOldAttempts();
            
            _consecutiveFailures = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogInformation(
                    "Circuit breaker transitioning to CLOSED for {ProviderName} after successful test",
                    _providerName);
                _state = CircuitBreakerState.Closed;
                _openedAt = DateTime.MinValue;
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Record a failed operation
    /// </summary>
    public async Task RecordFailureAsync(Exception exception, CancellationToken ct = default)
    {
        await _stateLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _recentAttempts.Enqueue((DateTime.UtcNow, false));
            CleanOldAttempts();
            
            _consecutiveFailures++;

            _logger.LogWarning(
                exception,
                "Failure recorded for {ProviderName}: {ConsecutiveFailures} consecutive failures",
                _providerName,
                _consecutiveFailures);

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _logger.LogWarning(
                    "Circuit breaker transitioning to OPEN for {ProviderName} after failed test",
                    _providerName);
                _state = CircuitBreakerState.Open;
                _openedAt = DateTime.UtcNow;
            }
            else if (ShouldOpen())
            {
                _logger.LogError(
                    "Circuit breaker transitioning to OPEN for {ProviderName}: {ConsecutiveFailures} consecutive failures, {FailureRate:P0} failure rate",
                    _providerName,
                    _consecutiveFailures,
                    GetFailureRate());
                _state = CircuitBreakerState.Open;
                _openedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Ensure circuit breaker state is up to date
    /// </summary>
    private async Task EnsureStateAsync(CancellationToken ct)
    {
        if (_state != CircuitBreakerState.Open)
            return;

        var openDuration = DateTime.UtcNow - _openedAt;
        if (openDuration.TotalSeconds < _settings.OpenDurationSeconds)
            return;

        await _stateLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_state == CircuitBreakerState.Open &&
                (DateTime.UtcNow - _openedAt).TotalSeconds >= _settings.OpenDurationSeconds)
            {
                _logger.LogInformation(
                    "Circuit breaker transitioning to HALF-OPEN for {ProviderName} after {OpenDurationSeconds}s cooldown",
                    _providerName,
                    _settings.OpenDurationSeconds);
                _state = CircuitBreakerState.HalfOpen;
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Check if circuit should open based on thresholds
    /// </summary>
    private bool ShouldOpen()
    {
        if (_consecutiveFailures >= _settings.FailureThreshold)
            return true;

        var failureRate = GetFailureRate();
        return failureRate >= _settings.FailureRateThreshold;
    }

    /// <summary>
    /// Calculate current failure rate from recent attempts
    /// </summary>
    public double GetFailureRate()
    {
        CleanOldAttempts();

        if (_recentAttempts.IsEmpty)
            return 0.0;

        var failures = 0;
        var total = 0;

        foreach (var (_, success) in _recentAttempts)
        {
            total++;
            if (!success)
                failures++;
        }

        return total > 0 ? (double)failures / total : 0.0;
    }

    /// <summary>
    /// Remove attempts outside the rolling window
    /// </summary>
    private void CleanOldAttempts()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_settings.RollingWindowMinutes);

        while (_recentAttempts.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
        {
            _recentAttempts.TryDequeue(out _);
        }

        // Also limit by size
        while (_recentAttempts.Count > _settings.RollingWindowSize)
        {
            _recentAttempts.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Reset the circuit breaker to closed state
    /// </summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        await _stateLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _logger.LogInformation(
                "Circuit breaker manually reset to CLOSED for {ProviderName}",
                _providerName);
            _state = CircuitBreakerState.Closed;
            _openedAt = DateTime.MinValue;
            _consecutiveFailures = 0;
            
            while (_recentAttempts.TryDequeue(out _)) { }
        }
        finally
        {
            _stateLock.Release();
        }
    }
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    public CircuitBreakerOpenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
