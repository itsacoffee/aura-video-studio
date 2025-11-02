using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Health;
using Aura.Core.Services.Performance;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Callback for retry notifications
/// </summary>
public delegate void RetryNotificationHandler(string operation, int attempt, int maxAttempts, string reason, int delayMs);

/// <summary>
/// Provides retry logic with exponential backoff for provider operations.
/// Handles transient failures such as rate limits and network issues.
/// </summary>
public class ProviderRetryWrapper
{
    private readonly ILogger<ProviderRetryWrapper> _logger;
    private readonly LatencyTelemetry? _telemetry;
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 1000;
    private const int MaxDelayMs = 30000;

    public ProviderRetryWrapper(ILogger<ProviderRetryWrapper> logger, LatencyTelemetry? telemetry = null)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Executes an async operation with exponential backoff retry logic
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="operationName">Name for logging</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="maxRetries">Maximum retry attempts (default: 3)</param>
    /// <param name="onRetry">Optional callback for retry notifications</param>
    /// <param name="providerName">Optional provider name for telemetry</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken ct,
        int maxRetries = MaxRetries,
        RetryNotificationHandler? onRetry = null,
        string? providerName = null)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                attempt++;
                _logger.LogDebug("Executing {Operation}, attempt {Attempt}/{MaxRetries}", 
                    operationName, attempt, maxRetries);

                var result = await operation(ct).ConfigureAwait(false);
                
                if (attempt > 1)
                {
                    _logger.LogInformation("{Operation} succeeded after {Attempt} attempts", 
                        operationName, attempt);
                    
                    _telemetry?.LogRetrySuccess(providerName ?? "Unknown", operationName, attempt);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                // Don't retry on cancellation
                throw;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                lastException = ex;
                int delayMs = CalculateBackoffDelay(attempt);
                string reason = ClassifyErrorReason(ex);

                _logger.LogWarning(ex,
                    "{Operation} failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    operationName, attempt, maxRetries, delayMs, ex.Message);

                _telemetry?.LogRetryAttempt(providerName ?? "Unknown", operationName, attempt, maxRetries, reason, delayMs);
                
                onRetry?.Invoke(operationName, attempt, maxRetries, reason, delayMs);

                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Non-transient error or max retries exceeded
                _logger.LogError(ex, "{Operation} failed permanently after {Attempt} attempts", 
                    operationName, attempt);
                
                if (attempt >= maxRetries)
                {
                    _telemetry?.LogRetryExhausted(providerName ?? "Unknown", operationName, attempt, ex.Message);
                }
                
                throw;
            }
        }

        // Max retries exceeded
        var finalException = new InvalidOperationException(
            $"{operationName} failed after {maxRetries} attempts. Last error: {lastException?.Message}",
            lastException);
        
        _logger.LogError(finalException, "{Operation} exhausted all retry attempts", operationName);
        _telemetry?.LogRetryExhausted(providerName ?? "Unknown", operationName, maxRetries, lastException?.Message ?? "Unknown error");
        
        throw finalException;
    }

    /// <summary>
    /// Determines if an error is transient and worth retrying
    /// </summary>
    private bool IsTransientError(Exception ex)
    {
        // Network-related errors
        if (ex is HttpRequestException or TaskCanceledException)
        {
            return true;
        }

        // Check message for rate limiting indicators
        var message = ex.Message.ToLowerInvariant();
        if (message.Contains("rate limit") ||
            message.Contains("too many requests") ||
            message.Contains("429") ||
            message.Contains("timeout") ||
            message.Contains("temporarily unavailable") ||
            message.Contains("service unavailable") ||
            message.Contains("503") ||
            message.Contains("502") ||
            message.Contains("504"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Classify the error reason for user-friendly messaging
    /// </summary>
    private string ClassifyErrorReason(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        
        if (message.Contains("rate limit") || message.Contains("429") || message.Contains("too many requests"))
        {
            return "rate limit exceeded";
        }
        
        if (message.Contains("timeout"))
        {
            return "request timeout";
        }
        
        if (message.Contains("503") || message.Contains("service unavailable") || message.Contains("temporarily unavailable"))
        {
            return "service temporarily unavailable";
        }
        
        if (message.Contains("502") || message.Contains("504"))
        {
            return "gateway error";
        }
        
        if (ex is HttpRequestException)
        {
            return "network error";
        }
        
        return "transient error";
    }

    /// <summary>
    /// Calculates exponential backoff delay with jitter
    /// </summary>
    private int CalculateBackoffDelay(int attempt)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s...
        int baseDelay = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
        
        // Cap at max delay
        baseDelay = Math.Min(baseDelay, MaxDelayMs);

        // Add jitter (±20%) to prevent thundering herd
        var jitter = Random.Shared.Next(-baseDelay / 5, baseDelay / 5);
        
        return Math.Max(InitialDelayMs, baseDelay + jitter);
    }
}
