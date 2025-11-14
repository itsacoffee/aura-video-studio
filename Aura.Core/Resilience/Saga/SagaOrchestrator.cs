using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience.Saga;

/// <summary>
/// Orchestrates saga execution with automatic compensation on failure
/// </summary>
public class SagaOrchestrator
{
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(ILogger<SagaOrchestrator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a saga with automatic compensation on failure
    /// </summary>
    public async Task<SagaResult> ExecuteAsync(
        SagaContext context,
        IEnumerable<ISagaStep> steps,
        CancellationToken cancellationToken = default)
    {
        var stepsList = steps.ToList();
        var executedSteps = new Stack<ISagaStep>();

        _logger.LogInformation(
            "Starting saga {SagaName} (ID: {SagaId}) with {StepCount} steps",
            context.SagaName,
            context.SagaId,
            stepsList.Count);

        context.RecordEvent("saga", "started", $"Saga {context.SagaName} started");

        try
        {
            // Execute each step in sequence
            foreach (var step in stepsList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation(
                    "Executing saga step {StepName} (ID: {StepId}) in saga {SagaId}",
                    step.Name,
                    step.StepId,
                    context.SagaId);

                context.RecordEvent(step.StepId, "executing", $"Executing step {step.Name}");

                try
                {
                    await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                    
                    executedSteps.Push(step);
                    context.MarkStepCompleted(step.StepId);
                    
                    context.RecordEvent(step.StepId, "completed", $"Step {step.Name} completed successfully");
                    
                    _logger.LogInformation(
                        "Saga step {StepName} completed successfully in saga {SagaId}",
                        step.Name,
                        context.SagaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Saga step {StepName} failed in saga {SagaId}: {Error}",
                        step.Name,
                        context.SagaId,
                        ex.Message);

                    context.RecordEvent(step.StepId, "failed", $"Step {step.Name} failed: {ex.Message}", ex);
                    context.FailureException = ex;
                    context.State = SagaState.Compensating;

                    // Start compensation
                    await CompensateAsync(context, executedSteps, cancellationToken).ConfigureAwait(false);

                    return new SagaResult
                    {
                        Success = false,
                        Context = context,
                        FailedAtStep = step.StepId,
                        Error = ex
                    };
                }
            }

            // All steps completed successfully
            context.State = SagaState.Completed;
            context.RecordEvent("saga", "completed", $"Saga {context.SagaName} completed successfully");

            _logger.LogInformation(
                "Saga {SagaName} (ID: {SagaId}) completed successfully in {Duration}ms",
                context.SagaName,
                context.SagaId,
                (DateTime.UtcNow - context.StartedAt).TotalMilliseconds);

            return new SagaResult
            {
                Success = true,
                Context = context
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Saga {SagaName} (ID: {SagaId}) was cancelled",
                context.SagaName,
                context.SagaId);

            context.State = SagaState.Compensating;
            await CompensateAsync(context, executedSteps, CancellationToken.None).ConfigureAwait(false);

            return new SagaResult
            {
                Success = false,
                Context = context,
                Error = new OperationCanceledException("Saga execution was cancelled")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error in saga {SagaName} (ID: {SagaId})",
                context.SagaName,
                context.SagaId);

            context.State = SagaState.Failed;
            context.FailureException = ex;
            
            await CompensateAsync(context, executedSteps, CancellationToken.None).ConfigureAwait(false);

            return new SagaResult
            {
                Success = false,
                Context = context,
                Error = ex
            };
        }
    }

    /// <summary>
    /// Compensates (rolls back) executed steps in reverse order
    /// </summary>
    private async Task CompensateAsync(
        SagaContext context,
        Stack<ISagaStep> executedSteps,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Starting compensation for saga {SagaName} (ID: {SagaId}) - rolling back {StepCount} steps",
            context.SagaName,
            context.SagaId,
            executedSteps.Count);

        context.RecordEvent("saga", "compensating", $"Starting compensation for {executedSteps.Count} steps");

        var compensationErrors = new List<Exception>();

        // Compensate steps in reverse order
        while (executedSteps.Count > 0)
        {
            var step = executedSteps.Pop();

            if (!step.CanCompensate)
            {
                _logger.LogWarning(
                    "Saga step {StepName} (ID: {StepId}) cannot be compensated - skipping",
                    step.Name,
                    step.StepId);
                
                context.RecordEvent(step.StepId, "compensation_skipped", $"Step {step.Name} cannot be compensated");
                continue;
            }

            try
            {
                _logger.LogInformation(
                    "Compensating saga step {StepName} (ID: {StepId})",
                    step.Name,
                    step.StepId);

                context.RecordEvent(step.StepId, "compensating", $"Compensating step {step.Name}");

                await step.CompensateAsync(context, cancellationToken).ConfigureAwait(false);

                context.RecordEvent(step.StepId, "compensated", $"Step {step.Name} compensated successfully");

                _logger.LogInformation(
                    "Successfully compensated saga step {StepName}",
                    step.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to compensate saga step {StepName} (ID: {StepId}): {Error}",
                    step.Name,
                    step.StepId,
                    ex.Message);

                context.RecordEvent(step.StepId, "compensation_failed", $"Failed to compensate step {step.Name}: {ex.Message}", ex);
                
                compensationErrors.Add(ex);
                // Continue compensating other steps even if one fails
            }
        }

        if (compensationErrors.Count != 0)
        {
            context.State = SagaState.Failed;
            
            _logger.LogError(
                "Saga compensation completed with {ErrorCount} errors for saga {SagaId}",
                compensationErrors.Count,
                context.SagaId);
        }
        else
        {
            context.State = SagaState.Compensated;
            
            _logger.LogInformation(
                "Saga {SagaName} (ID: {SagaId}) fully compensated",
                context.SagaName,
                context.SagaId);
        }

        context.RecordEvent("saga", "compensation_completed", 
            $"Compensation completed with {compensationErrors.Count} errors");
    }
}

/// <summary>
/// Result of saga execution
/// </summary>
public class SagaResult
{
    public required bool Success { get; init; }
    public required SagaContext Context { get; init; }
    public string? FailedAtStep { get; init; }
    public Exception? Error { get; init; }
}
