using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Models;

namespace Aura.Core.Services;

/// <summary>
/// Enhanced key validation service with status tracking and latency-aware validation
/// </summary>
public class EnhancedKeyValidationService
{
    private readonly ILogger<EnhancedKeyValidationService> _logger;
    private readonly IKeyValidationService _baseValidator;
    private readonly ProviderValidationPolicyLoader _policyLoader;
    private readonly ConcurrentDictionary<string, KeyValidationStatusResult> _validationStatus;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeValidations;

    public EnhancedKeyValidationService(
        ILogger<EnhancedKeyValidationService> logger,
        IKeyValidationService baseValidator,
        ProviderValidationPolicyLoader policyLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
        _policyLoader = policyLoader ?? throw new ArgumentNullException(nameof(policyLoader));
        
        _validationStatus = new ConcurrentDictionary<string, KeyValidationStatusResult>();
        _activeValidations = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    /// <summary>
    /// Validates an API key with latency-aware timeouts and progressive status updates
    /// </summary>
    public async Task<KeyValidationStatusResult> ValidateApiKeyAsync(
        string providerName,
        string apiKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        // Cancel any existing validation for this provider
        if (_activeValidations.TryRemove(providerName, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        // Create new cancellation token source for this validation
        var validationCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _activeValidations[providerName] = validationCts;

        // Load validation policy for this provider
        var policies = await _policyLoader.LoadPoliciesAsync(ct).ConfigureAwait(false);
        var policy = policies.GetPolicyForProvider(providerName);

        var statusResult = new KeyValidationStatusResult
        {
            ProviderName = providerName,
            Status = KeyValidationStatus.Validating,
            ValidationStarted = DateTime.UtcNow,
            Message = "Validation in progress...",
            CanRetry = true,
            CanManuallyRevalidate = false
        };

        _validationStatus[providerName] = statusResult;

        try
        {
            _logger.LogInformation(
                "Starting validation for provider {Provider} with policy {Category} (Normal: {Normal}ms, Extended: {Extended}ms, Max: {Max}ms)",
                providerName,
                policy.Category,
                policy.NormalTimeoutMs,
                policy.ExtendedTimeoutMs,
                policy.MaxTimeoutMs);

            var startTime = DateTime.UtcNow;
            var retryCount = 0;
            KeyValidationResult? validationResult = null;

            while (retryCount <= policy.MaxRetries && !validationCts.Token.IsCancellationRequested)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Update status based on elapsed time
                if (elapsed > policy.ExtendedTimeoutMs)
                {
                    statusResult.Status = KeyValidationStatus.ValidatingMaxWait;
                    statusResult.Message = $"Validation taking longer than expected ({elapsed:F0}ms elapsed). Provider may be slow.";
                }
                else if (elapsed > policy.NormalTimeoutMs)
                {
                    statusResult.Status = KeyValidationStatus.ValidatingExtended;
                    statusResult.Message = $"Validation in progress, taking longer than usual ({elapsed:F0}ms elapsed).";
                }

                statusResult.ElapsedMs = (int)elapsed;
                statusResult.RemainingTimeoutMs = Math.Max(0, policy.MaxTimeoutMs - (int)elapsed);
                _validationStatus[providerName] = statusResult;

                // Attempt validation with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(validationCts.Token);
                timeoutCts.CancelAfter(policy.RetryIntervalMs + 5000);

                try
                {
                    validationResult = await _baseValidator.TestApiKeyAsync(
                        providerName,
                        apiKey,
                        timeoutCts.Token).ConfigureAwait(false);

                    if (validationResult.IsValid)
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (elapsed < policy.MaxTimeoutMs)
                {
                    _logger.LogWarning(
                        "Validation attempt {Retry} for {Provider} timed out after {Elapsed}ms",
                        retryCount + 1,
                        providerName,
                        elapsed);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Validation attempt {Retry} for {Provider} failed",
                        retryCount + 1,
                        providerName);
                }

                retryCount++;

                if (retryCount <= policy.MaxRetries && elapsed < policy.MaxTimeoutMs)
                {
                    await Task.Delay(policy.RetryIntervalMs, validationCts.Token).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }

            var totalElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (validationResult != null && validationResult.IsValid)
            {
                statusResult.Status = KeyValidationStatus.Valid;
                statusResult.Message = validationResult.Message;
                statusResult.LastValidated = DateTime.UtcNow;
                statusResult.Details = validationResult.Details;
                statusResult.CanRetry = false;
                statusResult.CanManuallyRevalidate = true;

                _logger.LogInformation(
                    "AUDIT: Validation succeeded for {Provider} after {Elapsed}ms ({Retries} retries)",
                    providerName,
                    totalElapsed,
                    retryCount);
            }
            else if (totalElapsed >= policy.MaxTimeoutMs)
            {
                statusResult.Status = KeyValidationStatus.TimedOut;
                statusResult.Message = $"Validation timed out after {totalElapsed:F0}ms. Provider may be down or unreachable.";
                statusResult.CanRetry = true;
                statusResult.CanManuallyRevalidate = true;

                _logger.LogWarning(
                    "Validation for {Provider} timed out after {Elapsed}ms with {Retries} retries",
                    providerName,
                    totalElapsed,
                    retryCount);
            }
            else if (totalElapsed > policy.ExtendedTimeoutMs && validationResult != null && !validationResult.IsValid)
            {
                statusResult.Status = KeyValidationStatus.SlowButWorking;
                statusResult.Message = $"Provider responded but key is invalid. Slow network detected ({totalElapsed:F0}ms).";
                statusResult.Details = validationResult.Details;
                statusResult.CanRetry = true;
                statusResult.CanManuallyRevalidate = true;
            }
            else
            {
                statusResult.Status = KeyValidationStatus.Invalid;
                statusResult.Message = validationResult?.Message ?? "Validation failed";
                statusResult.Details = validationResult?.Details ?? new Dictionary<string, string>();
                statusResult.CanRetry = true;
                statusResult.CanManuallyRevalidate = true;

                _logger.LogWarning(
                    "Validation for {Provider} failed: {Message}",
                    providerName,
                    statusResult.Message);
            }

            statusResult.ElapsedMs = (int)totalElapsed;
            statusResult.RemainingTimeoutMs = 0;
            _validationStatus[providerName] = statusResult;

            return statusResult;
        }
        catch (OperationCanceledException)
        {
            statusResult.Status = KeyValidationStatus.TimedOut;
            statusResult.Message = "Validation was cancelled";
            statusResult.CanRetry = true;
            _validationStatus[providerName] = statusResult;
            
            _logger.LogInformation("Validation for {Provider} was cancelled", providerName);
            throw;
        }
        catch (Exception ex)
        {
            statusResult.Status = KeyValidationStatus.Invalid;
            statusResult.Message = $"Validation error: {ex.Message}";
            statusResult.CanRetry = true;
            _validationStatus[providerName] = statusResult;
            
            _logger.LogError(ex, "Unexpected error during validation for {Provider}", providerName);
            throw;
        }
        finally
        {
            _activeValidations.TryRemove(providerName, out _);
            validationCts.Dispose();
        }
    }

    /// <summary>
    /// Gets the current validation status for a provider
    /// </summary>
    public KeyValidationStatusResult? GetValidationStatus(string providerName)
    {
        return _validationStatus.TryGetValue(providerName, out var status) ? status : null;
    }

    /// <summary>
    /// Gets validation status for all providers
    /// </summary>
    public Dictionary<string, KeyValidationStatusResult> GetAllValidationStatuses()
    {
        return new Dictionary<string, KeyValidationStatusResult>(_validationStatus);
    }

    /// <summary>
    /// Cancels an active validation for a provider
    /// </summary>
    public bool CancelValidation(string providerName)
    {
        if (_activeValidations.TryRemove(providerName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            
            _logger.LogInformation("Cancelled active validation for {Provider}", providerName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears validation status for a provider (e.g., before revalidation)
    /// </summary>
    public bool ClearValidationStatus(string providerName)
    {
        var removed = _validationStatus.TryRemove(providerName, out _);
        if (removed)
        {
            _logger.LogDebug("Cleared validation status for {Provider}", providerName);
        }
        return removed;
    }
}
