namespace Aura.Core.Resilience.Saga;

/// <summary>
/// Represents a single step in a saga transaction pattern
/// </summary>
public interface ISagaStep
{
    /// <summary>
    /// Unique identifier for this step
    /// </summary>
    string StepId { get; }

    /// <summary>
    /// Name of the step for logging
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the forward operation of this step
    /// </summary>
    Task ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the compensation (rollback) operation of this step
    /// </summary>
    Task CompensateAsync(SagaContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether this step can be safely compensated
    /// </summary>
    bool CanCompensate { get; }
}

/// <summary>
/// Generic saga step with typed result
/// </summary>
public interface ISagaStep<TResult> : ISagaStep
{
    /// <summary>
    /// Executes the forward operation and returns a result
    /// </summary>
    new Task<TResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);
}
