using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Resilience;

/// <summary>
/// Central registry for managing circuit breakers across all provider services.
/// Provides factory methods and state monitoring for circuit breakers.
/// </summary>
public class CircuitBreakerRegistry
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CircuitBreakerRegistry> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new();

    /// <summary>
    /// Creates a new CircuitBreakerRegistry
    /// </summary>
    /// <param name="loggerFactory">Logger factory for creating circuit breaker loggers</param>
    public CircuitBreakerRegistry(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<CircuitBreakerRegistry>();
    }

    /// <summary>
    /// Gets or creates a circuit breaker for the specified service
    /// </summary>
    /// <param name="serviceName">Name of the service (e.g., "OpenAI", "ElevenLabs")</param>
    /// <param name="failureThreshold">Number of failures before opening circuit (default: 5)</param>
    /// <param name="openDuration">How long the circuit stays open (default: 2 minutes)</param>
    /// <returns>The circuit breaker for the service</returns>
    public CircuitBreaker GetOrCreate(
        string serviceName,
        int failureThreshold = 5,
        TimeSpan? openDuration = null)
    {
        return _breakers.GetOrAdd(serviceName, name =>
        {
            var breakerLogger = _loggerFactory.CreateLogger<CircuitBreaker>();
            _logger.LogDebug("Creating circuit breaker for service: {ServiceName}", name);
            return new CircuitBreaker(
                breakerLogger,
                name,
                failureThreshold,
                openDuration ?? TimeSpan.FromMinutes(2));
        });
    }

    /// <summary>
    /// Gets the current state of a circuit breaker for a specific service
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <returns>The circuit state, or Closed if the service has no breaker</returns>
    public CircuitState GetState(string serviceName)
    {
        return _breakers.TryGetValue(serviceName, out var breaker)
            ? breaker.State
            : CircuitState.Closed;
    }

    /// <summary>
    /// Resets the circuit breaker for a specific service
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <returns>True if the breaker was found and reset, false otherwise</returns>
    public bool Reset(string serviceName)
    {
        if (_breakers.TryGetValue(serviceName, out var breaker))
        {
            breaker.Reset();
            _logger.LogInformation("Circuit breaker reset for service: {ServiceName}", serviceName);
            return true;
        }

        _logger.LogWarning("Circuit breaker not found for service: {ServiceName}", serviceName);
        return false;
    }

    /// <summary>
    /// Resets all circuit breakers
    /// </summary>
    public void ResetAll()
    {
        foreach (var breaker in _breakers.Values)
        {
            breaker.Reset();
        }
        _logger.LogInformation("All circuit breakers reset ({Count} total)", _breakers.Count);
    }

    /// <summary>
    /// Gets the states of all registered circuit breakers
    /// </summary>
    /// <returns>Dictionary mapping service names to their circuit states</returns>
    public Dictionary<string, CircuitState> GetAllStates()
    {
        return _breakers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.State);
    }

    /// <summary>
    /// Gets detailed information about all circuit breakers
    /// </summary>
    /// <returns>Dictionary mapping service names to their circuit breaker info</returns>
    public Dictionary<string, CircuitBreakerInfo> GetAllInfo()
    {
        return _breakers.ToDictionary(kvp => kvp.Key, kvp => new CircuitBreakerInfo
        {
            ServiceName = kvp.Key,
            State = kvp.Value.State,
            FailureCount = kvp.Value.FailureCount,
            OpenedAt = kvp.Value.OpenedAt
        });
    }

    /// <summary>
    /// Checks if a specific service's circuit breaker is allowing requests
    /// </summary>
    /// <param name="serviceName">Name of the service</param>
    /// <returns>True if requests are allowed, false if circuit is open</returns>
    public bool IsAllowed(string serviceName)
    {
        if (_breakers.TryGetValue(serviceName, out var breaker))
        {
            return breaker.IsAllowed();
        }
        // No breaker registered means requests are allowed
        return true;
    }

    /// <summary>
    /// Gets the list of all registered service names
    /// </summary>
    public IEnumerable<string> GetRegisteredServices()
    {
        return _breakers.Keys.ToList();
    }
}

/// <summary>
/// Information about a circuit breaker's current state
/// </summary>
public class CircuitBreakerInfo
{
    /// <summary>
    /// Name of the service
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the circuit breaker
    /// </summary>
    public CircuitState State { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// When the circuit was opened (null if closed)
    /// </summary>
    public DateTime? OpenedAt { get; set; }
}
