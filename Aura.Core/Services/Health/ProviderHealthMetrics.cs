using System;

namespace Aura.Core.Services.Health;

/// <summary>
/// Health metrics for a provider
/// </summary>
public record ProviderHealthMetrics
{
    public string ProviderName { get; init; } = string.Empty;
    public bool IsHealthy { get; init; }
    public DateTime LastCheckTime { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public int ConsecutiveFailures { get; init; }
    public string? LastError { get; init; }
    public double SuccessRate { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public CircuitBreakerState CircuitState { get; init; } = CircuitBreakerState.Closed;
    public double FailureRate { get; init; }
    public DateTime? CircuitOpenedAt { get; init; }
}

/// <summary>
/// Internal state for tracking provider health over time
/// </summary>
internal class ProviderHealthState
{
    private readonly CircularBuffer<bool> _recentResults = new(100);
    private readonly CircularBuffer<TimeSpan> _recentResponseTimes = new(100);

    public string ProviderName { get; }
    public DateTime LastCheckTime { get; set; }
    public TimeSpan LastResponseTime { get; set; }
    public int ConsecutiveFailures { get; set; }
    public string? LastError { get; set; }

    public ProviderHealthState(string providerName)
    {
        ProviderName = providerName;
        LastCheckTime = DateTime.MinValue;
    }

    public void RecordSuccess(TimeSpan responseTime)
    {
        _recentResults.Add(true);
        _recentResponseTimes.Add(responseTime);
        LastResponseTime = responseTime;
        ConsecutiveFailures = 0;
        LastError = null;
        LastCheckTime = DateTime.UtcNow;
    }

    public void RecordFailure(string error)
    {
        _recentResults.Add(false);
        ConsecutiveFailures++;
        LastError = error;
        LastCheckTime = DateTime.UtcNow;
    }

    public double GetSuccessRate()
    {
        if (_recentResults.Count == 0) return 0.0;
        
        var successes = 0;
        foreach (var result in _recentResults)
        {
            if (result) successes++;
        }
        
        return (double)successes / _recentResults.Count;
    }

    public TimeSpan GetAverageResponseTime()
    {
        if (_recentResponseTimes.Count == 0) return TimeSpan.Zero;
        
        var totalTicks = 0L;
        foreach (var time in _recentResponseTimes)
        {
            totalTicks += time.Ticks;
        }
        
        return TimeSpan.FromTicks(totalTicks / _recentResponseTimes.Count);
    }

    public ProviderHealthMetrics ToMetrics(CircuitBreaker? circuitBreaker = null)
    {
        return new ProviderHealthMetrics
        {
            ProviderName = ProviderName,
            IsHealthy = ConsecutiveFailures < 3 && (circuitBreaker?.State != CircuitBreakerState.Open),
            LastCheckTime = LastCheckTime,
            ResponseTime = LastResponseTime,
            ConsecutiveFailures = ConsecutiveFailures,
            LastError = LastError,
            SuccessRate = GetSuccessRate(),
            AverageResponseTime = GetAverageResponseTime(),
            CircuitState = circuitBreaker?.State ?? CircuitBreakerState.Closed,
            FailureRate = circuitBreaker?.GetFailureRate() ?? 0.0,
            CircuitOpenedAt = circuitBreaker?.State == CircuitBreakerState.Open ? circuitBreaker.OpenedAt : null
        };
    }
}

/// <summary>
/// Fixed-size circular buffer for storing recent values
/// </summary>
internal class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _count;

    public int Count => _count;
    public int Capacity => _buffer.Length;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        var index = (_start + _count) % _buffer.Length;
        _buffer[index] = item;

        if (_count < _buffer.Length)
        {
            _count++;
        }
        else
        {
            _start = (_start + 1) % _buffer.Length;
        }
    }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _buffer[(_start + i) % _buffer.Length];
        }
    }
}
