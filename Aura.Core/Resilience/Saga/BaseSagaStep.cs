using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience.Saga;

/// <summary>
/// Base class for saga steps with common functionality
/// </summary>
public abstract class BaseSagaStep : ISagaStep
{
    protected readonly ILogger Logger;

    protected BaseSagaStep(ILogger logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract string StepId { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual bool CanCompensate => true;

    /// <inheritdoc />
    public virtual Task ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default)
    {
        // This is a base implementation that must be overridden by derived classes
        // or implemented via the generic BaseSagaStep<TResult> pattern
        throw new InvalidOperationException(
            $"ExecuteAsync must be implemented by step {StepId}. " +
            "Either override this method directly or use BaseSagaStep<TResult> to provide typed execution.");
    }

    /// <inheritdoc />
    public abstract Task CompensateAsync(SagaContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores data in the saga context
    /// </summary>
    protected void StoreData<T>(SagaContext context, string key, T value) where T : notnull
    {
        context.Set(key, value);
        Logger.LogDebug(
            "Stored data in saga context: {Key} = {Value}",
            key,
            value);
    }

    /// <summary>
    /// Retrieves data from the saga context
    /// </summary>
    protected T? RetrieveData<T>(SagaContext context, string key)
    {
        var value = context.Get<T>(key);
        Logger.LogDebug(
            "Retrieved data from saga context: {Key} = {Value}",
            key,
            value);
        return value;
    }

    /// <summary>
    /// Retrieves required data from the saga context, throws if not found
    /// </summary>
    protected T RetrieveRequiredData<T>(SagaContext context, string key)
    {
        var value = context.Get<T>(key);
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Required data '{key}' not found in saga context for step {StepId}");
        }
        return value;
    }
}

/// <summary>
/// Base class for saga steps with typed result
/// </summary>
public abstract class BaseSagaStep<TResult> : BaseSagaStep, ISagaStep<TResult> where TResult : notnull
{
    protected BaseSagaStep(ILogger logger) : base(logger)
    {
    }

    /// <inheritdoc />
    public new abstract Task<TResult> ExecuteAsync(SagaContext context, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    async Task ISagaStep.ExecuteAsync(SagaContext context, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        
        // Store the result in the context
        var resultKey = $"{StepId}_result";
        StoreData(context, resultKey, result);
    }

    /// <summary>
    /// Retrieves the result of this step from the context
    /// </summary>
    protected TResult? GetStepResult(SagaContext context)
    {
        var resultKey = $"{StepId}_result";
        return RetrieveData<TResult>(context, resultKey);
    }
}
