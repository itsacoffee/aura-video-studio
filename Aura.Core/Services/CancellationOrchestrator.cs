using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Orchestrates job cancellation across multiple providers and stages
/// Handles best-effort cancellation with rollback markers and warning collection
/// </summary>
public class CancellationOrchestrator
{
    private readonly ILogger<CancellationOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, CancellationContext> _cancellations;
    private readonly ProgressAggregatorService? _progressAggregator;

    public CancellationOrchestrator(
        ILogger<CancellationOrchestrator> logger,
        ProgressAggregatorService? progressAggregator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _progressAggregator = progressAggregator;
        _cancellations = new ConcurrentDictionary<string, CancellationContext>();
    }

    /// <summary>
    /// Register a provider for a job that may need cancellation
    /// </summary>
    public void RegisterProvider(
        string jobId,
        string providerName,
        string providerType,
        bool supportsCancellation,
        CancellationTokenSource? cts = null)
    {
        var context = _cancellations.GetOrAdd(jobId, _ => new CancellationContext
        {
            JobId = jobId,
            StartedAt = DateTime.UtcNow
        });

        var provider = new ProviderCancellationInfo
        {
            ProviderName = providerName,
            ProviderType = providerType,
            SupportsCancellation = supportsCancellation,
            CancellationTokenSource = cts,
            RegisteredAt = DateTime.UtcNow
        };

        context.Providers[providerName] = provider;

        _logger.LogDebug(
            "Registered provider {ProviderName} ({ProviderType}) for job {JobId}, supports cancellation: {SupportsCancellation}",
            providerName, providerType, jobId, supportsCancellation);
    }

    /// <summary>
    /// Cancel a job and all its associated providers
    /// Returns status of cancellation attempts for each provider
    /// </summary>
    public async Task<CancellationResult> CancelJobAsync(
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting cancellation orchestration for job {JobId}", jobId);

        if (!_cancellations.TryGetValue(jobId, out var context))
        {
            _logger.LogWarning("No cancellation context found for job {JobId}", jobId);
            return new CancellationResult
            {
                JobId = jobId,
                Success = false,
                Message = "No active providers found for this job",
                ProviderStatuses = new List<ProviderCancellationStatus>()
            };
        }

        context.CancellationRequestedAt = DateTime.UtcNow;
        var statuses = new List<ProviderCancellationStatus>();
        var warnings = new List<string>();

        foreach (var (providerName, providerInfo) in context.Providers)
        {
            var status = await CancelProviderAsync(jobId, providerName, providerInfo, ct).ConfigureAwait(false);
            statuses.Add(status);

            if (!string.IsNullOrEmpty(status.Warning))
            {
                warnings.Add(status.Warning);
                _progressAggregator?.AddWarning(jobId, status.Warning);
            }
        }

        var allCancelled = statuses.All(s => s.Status == "Cancelled" || s.Status == "NotSupported");
        var message = allCancelled
            ? "All providers cancelled or reported as not supporting cancellation"
            : "Some providers may not have fully cancelled";

        context.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Cancellation orchestration completed for job {JobId}: {SuccessCount}/{TotalCount} providers cancelled",
            jobId, statuses.Count(s => s.Status == "Cancelled"), statuses.Count);

        return new CancellationResult
        {
            JobId = jobId,
            Success = allCancelled,
            Message = message,
            ProviderStatuses = statuses,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Attempt to cancel a specific provider
    /// </summary>
    private async Task<ProviderCancellationStatus> CancelProviderAsync(
        string jobId,
        string providerName,
        ProviderCancellationInfo providerInfo,
        CancellationToken ct)
    {
        _logger.LogDebug("Attempting to cancel provider {ProviderName} for job {JobId}", providerName, jobId);

        if (!providerInfo.SupportsCancellation)
        {
            var warning = $"Provider {providerName} ({providerInfo.ProviderType}) does not support cancellation. Operation may continue until completion.";
            _logger.LogWarning(warning);

            return new ProviderCancellationStatus
            {
                ProviderName = providerName,
                ProviderType = providerInfo.ProviderType,
                SupportsCancellation = false,
                Status = "NotSupported",
                Warning = warning
            };
        }

        if (providerInfo.CancellationTokenSource == null)
        {
            var warning = $"Provider {providerName} ({providerInfo.ProviderType}) supports cancellation but no CancellationTokenSource was provided.";
            _logger.LogWarning(warning);

            return new ProviderCancellationStatus
            {
                ProviderName = providerName,
                ProviderType = providerInfo.ProviderType,
                SupportsCancellation = true,
                Status = "Failed",
                Warning = warning
            };
        }

        try
        {
            if (!providerInfo.CancellationTokenSource.IsCancellationRequested)
            {
                providerInfo.CancellationTokenSource.Cancel();
            }

            providerInfo.CancelledAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully cancelled provider {ProviderName} for job {JobId}", providerName, jobId);

            return new ProviderCancellationStatus
            {
                ProviderName = providerName,
                ProviderType = providerInfo.ProviderType,
                SupportsCancellation = true,
                Status = "Cancelled"
            };
        }
        catch (Exception ex)
        {
            var warning = $"Error cancelling provider {providerName}: {ex.Message}";
            _logger.LogError(ex, "Error cancelling provider {ProviderName} for job {JobId}", providerName, jobId);

            return new ProviderCancellationStatus
            {
                ProviderName = providerName,
                ProviderType = providerInfo.ProviderType,
                SupportsCancellation = true,
                Status = "Error",
                Warning = warning
            };
        }
    }

    /// <summary>
    /// Mark a stage as completed for rollback purposes
    /// </summary>
    public void MarkStageCompleted(string jobId, string stageName)
    {
        if (_cancellations.TryGetValue(jobId, out var context))
        {
            context.CompletedStages.Add(stageName);
            _logger.LogDebug("Marked stage {StageName} as completed for job {JobId}", stageName, jobId);
        }
    }

    /// <summary>
    /// Get rollback markers for a cancelled job
    /// </summary>
    public List<string> GetCompletedStages(string jobId)
    {
        return _cancellations.TryGetValue(jobId, out var context)
            ? context.CompletedStages.ToList()
            : new List<string>();
    }

    /// <summary>
    /// Clean up cancellation context after job completion
    /// </summary>
    public void CleanupContext(string jobId)
    {
        _cancellations.TryRemove(jobId, out _);
        _logger.LogDebug("Cleaned up cancellation context for job {JobId}", jobId);
    }

    /// <summary>
    /// Get current cancellation status for a job
    /// </summary>
    public CancellationContext? GetContext(string jobId)
    {
        return _cancellations.TryGetValue(jobId, out var context) ? context : null;
    }
}

/// <summary>
/// Context for tracking cancellation state of a job
/// </summary>
public class CancellationContext
{
    public string JobId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CancellationRequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ConcurrentDictionary<string, ProviderCancellationInfo> Providers { get; init; } = new();
    public List<string> CompletedStages { get; init; } = new();
}

/// <summary>
/// Information about a provider's cancellation capability
/// </summary>
public class ProviderCancellationInfo
{
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
    public bool SupportsCancellation { get; init; }
    public CancellationTokenSource? CancellationTokenSource { get; init; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; set; }
}

/// <summary>
/// Result of a cancellation attempt
/// </summary>
public record CancellationResult
{
    public string JobId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ProviderCancellationStatus> ProviderStatuses { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Status of a single provider's cancellation attempt
/// </summary>
public record ProviderCancellationStatus
{
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
    public bool SupportsCancellation { get; init; }
    public string Status { get; init; } = string.Empty; // Cancelled, Failed, NotSupported, Error
    public string? Warning { get; init; }
}
