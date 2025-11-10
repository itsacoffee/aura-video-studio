using System.Collections.Concurrent;

namespace Aura.Core.Resilience.Saga;

/// <summary>
/// Context shared across all steps in a saga
/// </summary>
public class SagaContext
{
    private readonly ConcurrentDictionary<string, object> _data = new();
    private readonly List<string> _completedSteps = new();
    private readonly List<SagaEvent> _events = new();

    /// <summary>
    /// Unique identifier for this saga execution
    /// </summary>
    public string SagaId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Name of the saga
    /// </summary>
    public required string SagaName { get; init; }

    /// <summary>
    /// When the saga started
    /// </summary>
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracking across services
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Current state of the saga
    /// </summary>
    public SagaState State { get; set; } = SagaState.Running;

    /// <summary>
    /// Shared data between saga steps
    /// </summary>
    public ConcurrentDictionary<string, object> Data => _data;

    /// <summary>
    /// Steps that have been completed
    /// </summary>
    public IReadOnlyList<string> CompletedSteps => _completedSteps.AsReadOnly();

    /// <summary>
    /// Events that occurred during saga execution
    /// </summary>
    public IReadOnlyList<SagaEvent> Events => _events.AsReadOnly();

    /// <summary>
    /// Error that caused the saga to fail (if any)
    /// </summary>
    public Exception? FailureException { get; set; }

    /// <summary>
    /// Marks a step as completed
    /// </summary>
    public void MarkStepCompleted(string stepId)
    {
        if (!_completedSteps.Contains(stepId))
        {
            _completedSteps.Add(stepId);
        }
    }

    /// <summary>
    /// Records an event in the saga
    /// </summary>
    public void RecordEvent(string stepId, string eventType, string message, object? data = null)
    {
        _events.Add(new SagaEvent
        {
            StepId = stepId,
            EventType = eventType,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sets a value in the context
    /// </summary>
    public void Set<T>(string key, T value) where T : notnull
    {
        _data[key] = value;
    }

    /// <summary>
    /// Gets a value from the context
    /// </summary>
    public T? Get<T>(string key)
    {
        return _data.TryGetValue(key, out var value) ? (T)value : default;
    }

    /// <summary>
    /// Tries to get a value from the context
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_data.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }
}

/// <summary>
/// State of a saga execution
/// </summary>
public enum SagaState
{
    Running,
    Completed,
    Compensating,
    Compensated,
    Failed
}

/// <summary>
/// An event that occurred during saga execution
/// </summary>
public class SagaEvent
{
    public required string StepId { get; init; }
    public required string EventType { get; init; }
    public required string Message { get; init; }
    public object? Data { get; init; }
    public DateTime Timestamp { get; init; }
}
