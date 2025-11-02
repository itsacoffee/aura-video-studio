using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Context for executing LLM operations with latency tracking, timeout management, and progress reporting
/// </summary>
public class LlmOperationContext
{
    private readonly ILogger<LlmOperationContext> _logger;
    private readonly LatencyManagementService _latencyService;
    private readonly LatencyTelemetry _telemetry;

    public LlmOperationContext(
        ILogger<LlmOperationContext> logger,
        LatencyManagementService latencyService,
        LatencyTelemetry telemetry)
    {
        _logger = logger;
        _latencyService = latencyService;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Execute an LLM operation with latency tracking and timeout management
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string providerName,
        string operationType,
        Func<CancellationToken, Task<T>> operation,
        int promptTokenCount,
        IProgress<LlmOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var estimate = _latencyService.PredictDuration(providerName, operationType, promptTokenCount);
        var timeoutSeconds = _latencyService.GetTimeoutSeconds(operationType);

        var startTime = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        var hasWarned = false;

        _logger.LogInformation(
            "Starting LLM operation: {Provider} {Operation} (estimate: {Estimate}s, timeout: {Timeout}s)",
            providerName,
            operationType,
            estimate.EstimatedSeconds,
            timeoutSeconds);

        progress?.Report(new LlmOperationProgress
        {
            Stage = operationType,
            Message = $"Generating {operationType}... {estimate.Description}",
            EstimatedSeconds = estimate.EstimatedSeconds,
            ElapsedSeconds = 0,
            IsWarning = false
        });

        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var progressReporter = Task.Run(async () =>
        {
            while (!linkedCts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
                
                var elapsedSeconds = (int)sw.Elapsed.TotalSeconds;
                
                if (!hasWarned && _latencyService.ShouldWarnTimeout(operationType, elapsedSeconds))
                {
                    hasWarned = true;
                    _telemetry.LogTimeoutWarning(providerName, operationType, elapsedSeconds, timeoutSeconds);
                    
                    progress?.Report(new LlmOperationProgress
                    {
                        Stage = operationType,
                        Message = $"{operationType} is taking longer than usual... ({elapsedSeconds}s elapsed)",
                        EstimatedSeconds = estimate.EstimatedSeconds,
                        ElapsedSeconds = elapsedSeconds,
                        IsWarning = true
                    });
                }
                else if (elapsedSeconds % 5 == 0)
                {
                    progress?.Report(new LlmOperationProgress
                    {
                        Stage = operationType,
                        Message = $"Generating {operationType}... ({elapsedSeconds}s elapsed)",
                        EstimatedSeconds = estimate.EstimatedSeconds,
                        ElapsedSeconds = elapsedSeconds,
                        IsWarning = false
                    });
                }
            }
        }, CancellationToken.None);

        T result;
        bool success = false;
        string? errorMessage = null;

        try
        {
            result = await operation(linkedCts.Token).ConfigureAwait(false);
            success = true;
            sw.Stop();

            _logger.LogInformation(
                "LLM operation completed: {Provider} {Operation} in {ElapsedMs}ms",
                providerName,
                operationType,
                sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            errorMessage = $"Operation timed out after {timeoutSeconds} seconds";
            
            _logger.LogError(
                "LLM operation timed out: {Provider} {Operation} after {TimeoutSeconds}s",
                providerName,
                operationType,
                timeoutSeconds);

            var metrics = new LatencyMetrics
            {
                ProviderName = providerName,
                OperationType = operationType,
                PromptTokenCount = promptTokenCount,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Success = success,
                RetryCount = 0,
                Timestamp = startTime,
                Notes = errorMessage
            };
            _latencyService.RecordMetrics(metrics);

            throw new TimeoutException($"{providerName} {operationType} operation timed out after {timeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            sw.Stop();
            errorMessage = ex.Message;
            
            _logger.LogError(ex,
                "LLM operation failed: {Provider} {Operation} after {ElapsedMs}ms",
                providerName,
                operationType,
                sw.ElapsedMilliseconds);

            var metrics = new LatencyMetrics
            {
                ProviderName = providerName,
                OperationType = operationType,
                PromptTokenCount = promptTokenCount,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Success = success,
                RetryCount = 0,
                Timestamp = startTime,
                Notes = errorMessage
            };
            _latencyService.RecordMetrics(metrics);

            throw;
        }
        finally
        {
            linkedCts.Cancel();
            
            try
            {
                await progressReporter.ConfigureAwait(false);
            }
            catch
            {
                // Ignore cancellation exceptions from progress reporter
            }

            if (success)
            {
                var metrics = new LatencyMetrics
                {
                    ProviderName = providerName,
                    OperationType = operationType,
                    PromptTokenCount = promptTokenCount,
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    Success = success,
                    RetryCount = 0,
                    Timestamp = startTime,
                    Notes = errorMessage
                };
                _latencyService.RecordMetrics(metrics);
            }
        }

        return result;
    }
}

/// <summary>
/// Progress information for an LLM operation
/// </summary>
public record LlmOperationProgress
{
    /// <summary>
    /// Current stage/operation name
    /// </summary>
    public string Stage { get; init; } = string.Empty;

    /// <summary>
    /// Progress message to display to user
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Estimated duration in seconds
    /// </summary>
    public int EstimatedSeconds { get; init; }

    /// <summary>
    /// Elapsed time in seconds
    /// </summary>
    public int ElapsedSeconds { get; init; }

    /// <summary>
    /// Whether this is a warning message (timeout threshold exceeded)
    /// </summary>
    public bool IsWarning { get; init; }
}
