using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Health;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Provides retry logic with exponential backoff for provider operations.
/// Handles transient failures such as rate limits and network issues.
/// </summary>
public class ProviderRetryWrapper
{
    private readonly ILogger<ProviderRetryWrapper> _logger;
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 1000;
    private const int MaxDelayMs = 30000;

    public ProviderRetryWrapper(ILogger<ProviderRetryWrapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes an async operation with exponential backoff retry logic
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="operationName">Name for logging</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="maxRetries">Maximum retry attempts (default: 3)</param>
    /// <returns>Result of the operation</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken ct,
        int maxRetries = MaxRetries)
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

                _logger.LogWarning(ex,
                    "{Operation} failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms. Error: {Error}",
                    operationName, attempt, maxRetries, delayMs, ex.Message);

                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Non-transient error or max retries exceeded
                _logger.LogError(ex, "{Operation} failed permanently after {Attempt} attempts", 
                    operationName, attempt);
                throw;
            }
        }

        // Max retries exceeded
        var finalException = new InvalidOperationException(
            $"{operationName} failed after {maxRetries} attempts. Last error: {lastException?.Message}",
            lastException);
        
        _logger.LogError(finalException, "{Operation} exhausted all retry attempts", operationName);
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
            message.Contains("503"))
        {
            return true;
        }

        return false;
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
