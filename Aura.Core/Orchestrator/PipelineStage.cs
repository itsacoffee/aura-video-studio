using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Base class for all pipeline stages in the video generation orchestration.
/// Provides common functionality for state management, progress reporting, and error handling.
/// </summary>
public abstract class PipelineStage
{
    protected readonly ILogger Logger;

    protected PipelineStage(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the unique name of this stage
    /// </summary>
    public abstract string StageName { get; }

    /// <summary>
    /// Gets the display name for progress reporting
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets the estimated weight of this stage (0-100) for progress calculation
    /// </summary>
    public virtual int ProgressWeight => 20;

    /// <summary>
    /// Indicates if this stage can be skipped if already completed (supports resume)
    /// </summary>
    public virtual bool SupportsResume => true;

    /// <summary>
    /// Indicates if this stage can be retried on failure
    /// </summary>
    public virtual bool SupportsRetry => true;

    /// <summary>
    /// Maximum number of retry attempts for this stage
    /// </summary>
    public virtual int MaxRetryAttempts => 3;

    /// <summary>
    /// Timeout for this stage
    /// </summary>
    public virtual TimeSpan Timeout => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Executes the stage with error handling, retry logic, and progress reporting
    /// </summary>
    public async Task<PipelineStageResult> ExecuteAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startTime = DateTime.UtcNow;
        var attemptNumber = 0;
        Exception? lastException = null;

        Logger.LogInformation(
            "[{CorrelationId}] Starting stage: {StageName}",
            context.CorrelationId,
            StageName);

        context.CurrentStage = StageName;
        
        // Check if stage can be resumed
        if (SupportsResume && CanSkipStage(context))
        {
            Logger.LogInformation(
                "[{CorrelationId}] Skipping stage {StageName} - already completed",
                context.CorrelationId,
                StageName);

            return PipelineStageResult.Success(StageName, TimeSpan.Zero, resumed: true);
        }

        // Execute with retry logic
        while (attemptNumber <= MaxRetryAttempts)
        {
            attemptNumber++;
            
            try
            {
                // Report start
                progress?.Report(new StageProgress
                {
                    StageName = StageName,
                    DisplayName = DisplayName,
                    Percentage = 0,
                    Message = $"Starting {DisplayName}...",
                    AttemptNumber = attemptNumber
                });

                // Create timeout token
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(Timeout);

                // Execute the stage
                await ExecuteStageAsync(context, progress, timeoutCts.Token).ConfigureAwait(false);

                var duration = DateTime.UtcNow - startTime;
                
                // Record success metrics
                var metrics = new PipelineStageMetrics
                {
                    StageName = StageName,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    ItemsProcessed = GetItemsProcessed(context),
                    ItemsFailed = 0,
                    RetryCount = attemptNumber - 1
                };
                context.RecordStageMetrics(StageName, metrics);

                Logger.LogInformation(
                    "[{CorrelationId}] Stage {StageName} completed successfully in {Duration:F2}s (attempt {Attempt})",
                    context.CorrelationId,
                    StageName,
                    duration.TotalSeconds,
                    attemptNumber);

                // Report completion
                progress?.Report(new StageProgress
                {
                    StageName = StageName,
                    DisplayName = DisplayName,
                    Percentage = 100,
                    Message = $"{DisplayName} completed",
                    AttemptNumber = attemptNumber
                });

                return PipelineStageResult.Success(StageName, duration);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Pipeline cancelled by user
                Logger.LogWarning(
                    "[{CorrelationId}] Stage {StageName} cancelled by user",
                    context.CorrelationId,
                    StageName);
                
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                var duration = DateTime.UtcNow - startTime;

                Logger.LogError(
                    ex,
                    "[{CorrelationId}] Stage {StageName} failed (attempt {Attempt}/{MaxAttempts}): {Error}",
                    context.CorrelationId,
                    StageName,
                    attemptNumber,
                    MaxRetryAttempts + 1,
                    ex.Message);

                // Record error
                var isRecoverable = SupportsRetry && attemptNumber <= MaxRetryAttempts;
                context.RecordError(StageName, ex, isRecoverable);

                // Check if we should retry
                if (!SupportsRetry || attemptNumber > MaxRetryAttempts)
                {
                    // Record failure metrics
                    var metrics = new PipelineStageMetrics
                    {
                        StageName = StageName,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        ItemsProcessed = GetItemsProcessed(context),
                        ItemsFailed = 1,
                        RetryCount = attemptNumber - 1
                    };
                    context.RecordStageMetrics(StageName, metrics);

                    return PipelineStageResult.Failure(StageName, ex, duration);
                }

                // Report retry
                progress?.Report(new StageProgress
                {
                    StageName = StageName,
                    DisplayName = DisplayName,
                    Percentage = 0,
                    Message = $"Retrying {DisplayName} (attempt {attemptNumber + 1}/{MaxRetryAttempts + 1})...",
                    AttemptNumber = attemptNumber,
                    ErrorMessage = ex.Message
                });

                // Wait before retry (exponential backoff)
                var delayMs = Math.Min(1000 * (int)Math.Pow(2, attemptNumber - 1), 10000);
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
        }

        // Should not reach here, but handle just in case
        var finalDuration = DateTime.UtcNow - startTime;
        return PipelineStageResult.Failure(StageName, lastException!, finalDuration);
    }

    /// <summary>
    /// Executes the core logic of this stage. Derived classes must implement this.
    /// </summary>
    protected abstract Task ExecuteStageAsync(
        PipelineContext context,
        IProgress<StageProgress>? progress,
        CancellationToken ct);

    /// <summary>
    /// Determines if this stage can be skipped (for resume support)
    /// </summary>
    protected virtual bool CanSkipStage(PipelineContext context)
    {
        // By default, check if stage output exists
        return context.GetStageOutput<object>(StageName) != null;
    }

    /// <summary>
    /// Gets the number of items processed by this stage (for metrics)
    /// </summary>
    protected virtual int GetItemsProcessed(PipelineContext context)
    {
        return 1; // Default to 1 item processed
    }

    /// <summary>
    /// Reports progress for this stage
    /// </summary>
    protected void ReportProgress(
        IProgress<StageProgress>? progress,
        int percentage,
        string message,
        int currentItem = 0,
        int totalItems = 0)
    {
        progress?.Report(new StageProgress
        {
            StageName = StageName,
            DisplayName = DisplayName,
            Percentage = percentage,
            Message = message,
            CurrentItem = currentItem,
            TotalItems = totalItems
        });
    }
}

/// <summary>
/// Progress information for a pipeline stage
/// </summary>
public record StageProgress
{
    public string StageName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int Percentage { get; init; }
    public string Message { get; init; } = string.Empty;
    public int CurrentItem { get; init; }
    public int TotalItems { get; init; }
    public int AttemptNumber { get; init; } = 1;
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of a pipeline stage execution
/// </summary>
public record PipelineStageResult
{
    public required string StageName { get; init; }
    public required bool Succeeded { get; init; }
    public Exception? Exception { get; init; }
    public required TimeSpan Duration { get; init; }
    public bool Resumed { get; init; }
    public string? Message { get; init; }

    public static PipelineStageResult Success(string stageName, TimeSpan duration, bool resumed = false)
    {
        return new PipelineStageResult
        {
            StageName = stageName,
            Succeeded = true,
            Duration = duration,
            Resumed = resumed
        };
    }

    public static PipelineStageResult Failure(string stageName, Exception exception, TimeSpan duration)
    {
        return new PipelineStageResult
        {
            StageName = stageName,
            Succeeded = false,
            Exception = exception,
            Duration = duration,
            Message = exception.Message
        };
    }
}
