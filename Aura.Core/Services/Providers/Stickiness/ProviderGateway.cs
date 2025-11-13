using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Gateway that orchestrates provider requests with patience-centric design.
/// Enforces provider stickiness and coordinates heartbeat monitoring and stall detection.
/// </summary>
public sealed class ProviderGateway
{
    private readonly ILogger<ProviderGateway> _logger;
    private readonly StallDetector _stallDetector;
    private readonly ProviderProfileLockService? _profileLockService;
    private readonly ConcurrentDictionary<string, PrimaryProviderLock> _activeLocks;
    private readonly ConcurrentDictionary<string, List<FallbackDecision>> _fallbackHistory;
    private readonly ConcurrentDictionary<string, ProviderState> _activeStates;

    /// <summary>
    /// Event raised when a stall is suspected
    /// </summary>
    public event EventHandler<StallSuspectedEvent>? StallSuspected;

    /// <summary>
    /// Event raised when a fallback decision is made
    /// </summary>
    public event EventHandler<FallbackDecision>? FallbackDecisionMade;

    /// <summary>
    /// Initializes a new provider gateway
    /// </summary>
    public ProviderGateway(
        ILogger<ProviderGateway> logger, 
        StallDetector stallDetector,
        ProviderProfileLockService? profileLockService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stallDetector = stallDetector ?? throw new ArgumentNullException(nameof(stallDetector));
        _profileLockService = profileLockService;
        _activeLocks = new ConcurrentDictionary<string, PrimaryProviderLock>();
        _fallbackHistory = new ConcurrentDictionary<string, List<FallbackDecision>>();
        _activeStates = new ConcurrentDictionary<string, ProviderState>();

        _stallDetector.StallSuspected += OnStallDetected;
    }

    /// <summary>
    /// Creates and locks a provider for a job
    /// </summary>
    public PrimaryProviderLock LockProvider(
        string jobId,
        string providerName,
        string providerType,
        string correlationId,
        bool isOverrideable = true,
        params string[] applicableStages)
    {
        var lock_ = new PrimaryProviderLock(
            jobId,
            providerName,
            providerType,
            correlationId,
            isOverrideable,
            applicableStages);

        if (!_activeLocks.TryAdd(jobId, lock_))
        {
            throw new InvalidOperationException($"Provider lock already exists for job {jobId}");
        }

        _logger.LogInformation(
            "[{CorrelationId}] PROVIDER_LOCK_CREATED Job: {JobId}, Provider: {Provider} ({Type}), " +
            "Overrideable: {Overrideable}, Stages: {Stages}",
            correlationId,
            jobId,
            providerName,
            providerType,
            isOverrideable,
            applicableStages.Length > 0 ? string.Join(", ", applicableStages) : "All");

        return lock_;
    }

    /// <summary>
    /// Gets the active provider lock for a job
    /// </summary>
    public PrimaryProviderLock? GetProviderLock(string jobId)
    {
        _activeLocks.TryGetValue(jobId, out var lock_);
        return lock_;
    }

    /// <summary>
    /// Validates that a provider request matches the locked provider and ProfileLock
    /// </summary>
    public bool ValidateProviderRequest(
        string jobId,
        string providerName,
        string stageName,
        out string? validationError)
    {
        validationError = null;

        // First check ProfileLock if service is available
        if (_profileLockService != null)
        {
            var providerRequiresNetwork = !IsOfflineProvider(providerName);
            var isProfileLockValid = _profileLockService.ValidateProviderRequest(
                jobId,
                providerName,
                stageName,
                providerRequiresNetwork,
                out var profileLockError);

            if (!isProfileLockValid)
            {
                validationError = profileLockError;
                
                _logger.LogWarning(
                    "PROFILE_LOCK_VIOLATION Job: {JobId}, Provider: {Provider}, Stage: {Stage}, Error: {Error}",
                    jobId,
                    providerName,
                    stageName,
                    profileLockError);
                
                return false;
            }
        }

        // Then check PrimaryProviderLock (legacy)
        if (!_activeLocks.TryGetValue(jobId, out var lock_))
        {
            return true; // No legacy lock, validation passes
        }

        if (!lock_.ValidateProvider(providerName, stageName))
        {
            validationError = $"Provider {providerName} does not match locked provider {lock_.ProviderName} for stage {stageName}";
            
            _logger.LogWarning(
                "PRIMARY_LOCK_VIOLATION Job: {JobId}, Provider: {Provider}, Stage: {Stage}",
                jobId,
                providerName,
                stageName);
            
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a provider is offline-compatible
    /// </summary>
    private static bool IsOfflineProvider(string providerName)
    {
        var offlineProviders = new[] { "RuleBased", "Ollama", "Windows", "Piper", "Mimic3", "LocalSD", "Stock" };
        return Array.Exists(offlineProviders, p => 
            string.Equals(p, providerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Executes a provider operation with patience monitoring
    /// </summary>
    public async Task<TResult> ExecuteWithPatienceAsync<TResult>(
        string jobId,
        string providerName,
        string providerType,
        string stageName,
        string correlationId,
        IHeartbeatStrategy heartbeatStrategy,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        if (!ValidateProviderRequest(jobId, providerName, stageName, out var validationError))
        {
            throw new InvalidOperationException(validationError);
        }

        var state = new ProviderState(providerName, providerType, correlationId);
        _activeStates[correlationId] = state;

        _logger.LogInformation(
            "[{CorrelationId}] PROVIDER_REQUEST_START {Provider} for stage {Stage}",
            correlationId,
            providerName,
            stageName);

        var monitoringTask = _stallDetector.MonitorAsync(state, heartbeatStrategy, ct);

        try
        {
            var result = await operation(ct);
            
            state.MarkComplete();
            
            _logger.LogInformation(
                "[{CorrelationId}] PROVIDER_REQUEST_COMPLETE {Provider} - " +
                "Elapsed: {Elapsed}ms, Category: {Category}, Heartbeats: {Heartbeats}",
                correlationId,
                providerName,
                state.ElapsedTime.TotalMilliseconds,
                state.CurrentCategory,
                state.HeartbeatCount);

            return result;
        }
        catch (Exception ex)
        {
            state.MarkError(ex.Message);
            
            _logger.LogError(
                ex,
                "[{CorrelationId}] PROVIDER_HARD_ERROR {Provider} - {Message}",
                correlationId,
                providerName,
                ex.Message);

            throw;
        }
        finally
        {
            await monitoringTask;
            _activeStates.TryRemove(correlationId, out _);
        }
    }

    /// <summary>
    /// Records a user's fallback decision
    /// </summary>
    public void RecordFallbackDecision(FallbackDecision decision)
    {
        if (decision == null)
            throw new ArgumentNullException(nameof(decision));

        var history = _fallbackHistory.GetOrAdd(decision.JobId, _ => new List<FallbackDecision>());
        
        lock (history)
        {
            history.Add(decision);
        }

        _logger.LogInformation(
            "[{CorrelationId}] USER_FALLBACK_INITIATED Job: {JobId}, {From} → {To}, " +
            "Reason: {Reason}, Elapsed: {Elapsed}ms, UserConfirmed: {Confirmed}",
            decision.CorrelationId,
            decision.JobId,
            decision.FromProvider,
            decision.ToProvider,
            decision.ReasonCode,
            decision.ElapsedBeforeSwitchMs,
            decision.UserConfirmed);

        if (_activeLocks.TryGetValue(decision.JobId, out var lock_))
        {
            lock_.TryUnlock(decision.ReasonCode.ToString());
        }

        OnFallbackDecisionMade(decision);
    }

    /// <summary>
    /// Gets the fallback history for a job
    /// </summary>
    public IReadOnlyList<FallbackDecision> GetFallbackHistory(string jobId)
    {
        if (_fallbackHistory.TryGetValue(jobId, out var history))
        {
            lock (history)
            {
                return history.ToArray();
            }
        }

        return Array.Empty<FallbackDecision>();
    }

    /// <summary>
    /// Gets the current state for a provider operation
    /// </summary>
    public ProviderState? GetProviderState(string correlationId)
    {
        _activeStates.TryGetValue(correlationId, out var state);
        return state;
    }

    /// <summary>
    /// Releases the provider lock for a job
    /// </summary>
    public bool ReleaseProviderLock(string jobId)
    {
        if (_activeLocks.TryRemove(jobId, out var lock_))
        {
            _logger.LogInformation(
                "[{CorrelationId}] PROVIDER_LOCK_RELEASED Job: {JobId}, Provider: {Provider}, " +
                "Duration: {Duration}s",
                lock_.CorrelationId,
                jobId,
                lock_.ProviderName,
                lock_.GetLockDuration().TotalSeconds);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets statistics about provider usage
    /// </summary>
    public ProviderGatewayStatistics GetStatistics()
    {
        var totalFallbacks = 0;
        var userRequestedFallbacks = 0;
        var errorFallbacks = 0;

        foreach (var history in _fallbackHistory.Values)
        {
            lock (history)
            {
                totalFallbacks += history.Count;
                userRequestedFallbacks += history.Count(d => d.ReasonCode == FallbackReasonCode.USER_REQUEST);
                errorFallbacks += history.Count(d => d.ReasonCode == FallbackReasonCode.PROVIDER_FATAL_ERROR);
            }
        }

        return new ProviderGatewayStatistics
        {
            ActiveLocks = _activeLocks.Count,
            ActiveOperations = _activeStates.Count,
            TotalFallbackDecisions = totalFallbacks,
            UserRequestedFallbacks = userRequestedFallbacks,
            ErrorTriggeredFallbacks = errorFallbacks
        };
    }

    private void OnStallDetected(object? sender, StallSuspectedEvent e)
    {
        StallSuspected?.Invoke(this, e);
    }

    private void OnFallbackDecisionMade(FallbackDecision decision)
    {
        FallbackDecisionMade?.Invoke(this, decision);
    }
}

/// <summary>
/// Statistics about provider gateway usage
/// </summary>
public sealed class ProviderGatewayStatistics
{
    public int ActiveLocks { get; init; }
    public int ActiveOperations { get; init; }
    public int TotalFallbackDecisions { get; init; }
    public int UserRequestedFallbacks { get; init; }
    public int ErrorTriggeredFallbacks { get; init; }
}
