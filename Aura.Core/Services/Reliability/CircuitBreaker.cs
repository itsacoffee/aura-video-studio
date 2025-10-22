using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Reliability;

/// <summary>
/// Implements the Circuit Breaker pattern to prevent cascading failures
/// </summary>
public class CircuitBreaker
{
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly TimeSpan _halfOpenTestInterval;
    
    private int _consecutiveFailures;
    private DateTime _lastFailureTime;
    private CircuitState _state;
    private readonly object _lock = new object();

    public CircuitBreaker(
        ILogger<CircuitBreaker> logger,
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        TimeSpan? halfOpenTestInterval = null)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromMinutes(1);
        _halfOpenTestInterval = halfOpenTestInterval ?? TimeSpan.FromSeconds(30);
        _state = CircuitState.Closed;
    }

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        // Check if circuit is open
        if (!CanExecute())
        {
            var exception = new InvalidOperationException($"Circuit breaker is {_state} for operation: {operationName}");
            _logger.LogWarning(exception, "Circuit breaker prevented execution of {Operation}", operationName);
            throw exception;
        }

        try
        {
            var result = await operation().ConfigureAwait(false);
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure(ex, operationName);
            throw;
        }
    }

    /// <summary>
    /// Checks if an operation can be executed
    /// </summary>
    private bool CanExecute()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // Check if enough time has passed to try half-open
                    if (DateTime.UtcNow - _lastFailureTime >= _openDuration)
                    {
                        _state = CircuitState.HalfOpen;
                        _logger.LogInformation("Circuit breaker transitioning to HalfOpen state");
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    // Allow one test request in half-open state
                    if (DateTime.UtcNow - _lastFailureTime >= _halfOpenTestInterval)
                    {
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Called when an operation succeeds
    /// </summary>
    private void OnSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation("Circuit breaker test succeeded, closing circuit");
                _state = CircuitState.Closed;
            }
            
            _consecutiveFailures = 0;
        }
    }

    /// <summary>
    /// Called when an operation fails
    /// </summary>
    private void OnFailure(Exception exception, string operationName)
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger.LogWarning(exception, 
                "Circuit breaker recorded failure #{Count} for operation: {Operation}", 
                _consecutiveFailures, operationName);

            if (_state == CircuitState.HalfOpen)
            {
                // Failed during half-open test, go back to open
                _state = CircuitState.Open;
                _logger.LogWarning("Circuit breaker test failed, reopening circuit");
            }
            else if (_consecutiveFailures >= _failureThreshold)
            {
                // Too many failures, open the circuit
                _state = CircuitState.Open;
                _logger.LogError(
                    "Circuit breaker opened after {Count} consecutive failures for operation: {Operation}",
                    _consecutiveFailures, operationName);
            }
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to closed state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _state = CircuitState.Closed;
            _logger.LogInformation("Circuit breaker manually reset to Closed state");
        }
    }

    /// <summary>
    /// Gets statistics about the circuit breaker
    /// </summary>
    public CircuitBreakerStats GetStats()
    {
        lock (_lock)
        {
            return new CircuitBreakerStats
            {
                State = _state,
                ConsecutiveFailures = _consecutiveFailures,
                LastFailureTime = _lastFailureTime,
                CanExecute = CanExecute()
            };
        }
    }
}

/// <summary>
/// States of a circuit breaker
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed, operations flow normally
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, operations are blocked
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, testing if service has recovered
    /// </summary>
    HalfOpen
}

/// <summary>
/// Statistics about a circuit breaker
/// </summary>
public class CircuitBreakerStats
{
    public CircuitState State { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime LastFailureTime { get; set; }
    public bool CanExecute { get; set; }
}
