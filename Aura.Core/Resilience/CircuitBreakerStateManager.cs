using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience;

/// <summary>
/// Manages circuit breaker states across all services and provides health monitoring
/// </summary>
public class CircuitBreakerStateManager
{
    private readonly ILogger<CircuitBreakerStateManager> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _states = new();

    public CircuitBreakerStateManager(ILogger<CircuitBreakerStateManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a circuit breaker state change
    /// </summary>
    public void RecordStateChange(string serviceName, CircuitState state, string? reason = null)
    {
        var now = DateTime.UtcNow;
        
        _states.AddOrUpdate(
            serviceName,
            _ => new CircuitBreakerState
            {
                ServiceName = serviceName,
                State = state,
                LastStateChange = now,
                StateChangedAt = now,
                Reason = reason
            },
            (_, existing) =>
            {
                existing.State = state;
                existing.LastStateChange = now;
                if (state != existing.State)
                {
                    existing.StateChangedAt = now;
                }
                existing.Reason = reason;
                return existing;
            });

        _logger.LogInformation(
            "Circuit breaker state changed: {ServiceName} is now {State} {Reason}",
            serviceName,
            state,
            reason != null ? $"({reason})" : "");
    }

    /// <summary>
    /// Gets the current state of a circuit breaker
    /// </summary>
    public CircuitBreakerState? GetState(string serviceName)
    {
        return _states.TryGetValue(serviceName, out var state) ? state : null;
    }

    /// <summary>
    /// Gets all circuit breaker states
    /// </summary>
    public IReadOnlyDictionary<string, CircuitBreakerState> GetAllStates()
    {
        return _states;
    }

    /// <summary>
    /// Gets services with open or half-open circuits
    /// </summary>
    public IEnumerable<CircuitBreakerState> GetDegradedServices()
    {
        return _states.Values.Where(s => s.State == CircuitState.Open || s.State == CircuitState.HalfOpen);
    }

    /// <summary>
    /// Gets services with closed circuits (healthy)
    /// </summary>
    public IEnumerable<CircuitBreakerState> GetHealthyServices()
    {
        return _states.Values.Where(s => s.State == CircuitState.Closed);
    }

    /// <summary>
    /// Checks if a service is available (circuit not open)
    /// </summary>
    public bool IsServiceAvailable(string serviceName)
    {
        if (!_states.TryGetValue(serviceName, out var state))
        {
            return true; // Unknown services are assumed available
        }

        return state.State != CircuitState.Open;
    }
}

/// <summary>
/// Circuit breaker state information
/// </summary>
public class CircuitBreakerState
{
    public required string ServiceName { get; init; }
    public CircuitState State { get; set; }
    public DateTime LastStateChange { get; set; }
    public DateTime StateChangedAt { get; set; }
    public string? Reason { get; set; }

    public TimeSpan TimeSinceStateChange => DateTime.UtcNow - StateChangedAt;
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitState
{
    Closed,
    Open,
    HalfOpen,
    Isolated
}
