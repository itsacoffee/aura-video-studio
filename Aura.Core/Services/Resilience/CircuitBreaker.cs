using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Resilience;

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    /// <summary>Normal operation, requests allowed</summary>
    Closed,
    /// <summary>Failures exceeded threshold, requests blocked</summary>
    Open,
    /// <summary>Testing if service recovered</summary>
    HalfOpen
}

/// <summary>
/// Circuit breaker implementation for provider API calls.
/// Prevents cascading failures by temporarily blocking requests to failing services.
/// </summary>
public class CircuitBreaker
{
    private readonly ILogger _logger;
    private readonly string _serviceName;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly TimeSpan _halfOpenTestInterval;

    private CircuitState _state = CircuitState.Closed;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private DateTime _openedAt;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new circuit breaker instance
    /// </summary>
    /// <param name="logger">Logger for state transitions</param>
    /// <param name="serviceName">Name of the service this breaker protects</param>
    /// <param name="failureThreshold">Number of failures before opening circuit (default: 5)</param>
    /// <param name="openDuration">How long the circuit stays open (default: 1 minute)</param>
    /// <param name="halfOpenTestInterval">Interval between test requests in half-open state (default: 30 seconds)</param>
    public CircuitBreaker(
        ILogger logger,
        string serviceName,
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        TimeSpan? halfOpenTestInterval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromMinutes(1);
        _halfOpenTestInterval = halfOpenTestInterval ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the current state of the circuit breaker, transitioning from Open to HalfOpen if cooldown expired
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation(
                        "Circuit breaker for {Service} transitioning from Open to HalfOpen",
                        _serviceName);
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets the name of the service this circuit breaker protects
    /// </summary>
    public string ServiceName => _serviceName;

    /// <summary>
    /// Gets the current failure count
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lock)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Gets when the circuit was last opened
    /// </summary>
    public DateTime? OpenedAt
    {
        get
        {
            lock (_lock)
            {
                return _state == CircuitState.Closed ? null : _openedAt;
            }
        }
    }

    /// <summary>
    /// Checks if requests are currently allowed through the circuit
    /// </summary>
    public bool IsAllowed()
    {
        var currentState = State;
        return currentState != CircuitState.Open;
    }

    /// <summary>
    /// Executes an action through the circuit breaker
    /// </summary>
    /// <typeparam name="T">Return type of the action</typeparam>
    /// <param name="action">The action to execute</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The result of the action</returns>
    /// <exception cref="CircuitBreakerOpenException">Thrown when circuit is open</exception>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        if (!IsAllowed())
        {
            var remainingTime = _openDuration - (DateTime.UtcNow - _openedAt);
            throw new CircuitBreakerOpenException(
                $"Circuit breaker for {_serviceName} is open. Retry after {remainingTime.TotalSeconds:F0} seconds.");
        }

        try
        {
            var result = await action(ct).ConfigureAwait(false);
            RecordSuccess();
            return result;
        }
        catch (Exception ex) when (ShouldRecordFailure(ex))
        {
            RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Records a successful operation, potentially closing the circuit
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitState.HalfOpen)
            {
                _logger.LogInformation(
                    "Circuit breaker for {Service} closing after successful test",
                    _serviceName);
            }
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>
    /// Records a failed operation, potentially opening the circuit
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_state == CircuitState.HalfOpen)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning(
                    "Circuit breaker for {Service} re-opened after failed test",
                    _serviceName);
            }
            else if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning(
                    "Circuit breaker for {Service} opened after {FailureCount} failures",
                    _serviceName, _failureCount);
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
            _state = CircuitState.Closed;
            _failureCount = 0;
            _logger.LogInformation("Circuit breaker for {Service} manually reset", _serviceName);
        }
    }

    /// <summary>
    /// Determines if an exception should count as a failure
    /// </summary>
    private static bool ShouldRecordFailure(Exception ex)
    {
        // Don't count user cancellations as failures
        if (ex is OperationCanceledException)
            return false;

        // Count HTTP errors, timeouts, and other transient failures
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is TaskCanceledException;
    }
}

/// <summary>
/// Exception thrown when circuit breaker is open and blocking requests
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Creates a new CircuitBreakerOpenException
    /// </summary>
    public CircuitBreakerOpenException(string message) : base(message) { }

    /// <summary>
    /// Creates a new CircuitBreakerOpenException with inner exception
    /// </summary>
    public CircuitBreakerOpenException(string message, Exception innerException)
        : base(message, innerException) { }
}
