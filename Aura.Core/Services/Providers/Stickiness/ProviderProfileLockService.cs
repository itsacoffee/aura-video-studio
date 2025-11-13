using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Configuration;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Service for managing ProviderProfileLock persistence and lifecycle.
/// Handles session-level and project-level locks with proper persistence.
/// </summary>
public sealed class ProviderProfileLockService
{
    private readonly ILogger<ProviderProfileLockService> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly ConcurrentDictionary<string, ProviderProfileLock> _sessionLocks;
    private readonly ConcurrentDictionary<string, ProviderProfileLock> _projectLocks;
    private readonly string _sessionLockPath;

    /// <summary>
    /// Event raised when a profile lock is created or updated
    /// </summary>
    public event EventHandler<ProviderProfileLock>? ProfileLockChanged;

    public ProviderProfileLockService(
        ILogger<ProviderProfileLockService> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
        
        _sessionLocks = new ConcurrentDictionary<string, ProviderProfileLock>();
        _projectLocks = new ConcurrentDictionary<string, ProviderProfileLock>();

        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _sessionLockPath = Path.Combine(auraDataDir, "session-profile-lock.json");

        LoadSessionLock();
    }

    /// <summary>
    /// Sets a profile lock for a specific job or project.
    /// Session-level locks take precedence over project-level locks.
    /// </summary>
    public async Task<ProviderProfileLock> SetProfileLockAsync(
        string jobId,
        string providerName,
        string providerType,
        bool isEnabled,
        bool offlineModeEnabled = false,
        string[]? applicableStages = null,
        ProviderProfileLockMetadata? metadata = null,
        bool isSessionLevel = true,
        CancellationToken ct = default)
    {
        var lock_ = new ProviderProfileLock(
            jobId,
            providerName,
            providerType,
            isEnabled,
            offlineModeEnabled,
            applicableStages,
            metadata);

        if (isSessionLevel)
        {
            _sessionLocks[jobId] = lock_;
            await SaveSessionLockAsync(ct);
            
            _logger.LogInformation(
                "SESSION_PROFILE_LOCK_SET Job: {JobId}, Provider: {Provider} ({Type}), " +
                "Enabled: {Enabled}, Offline: {Offline}, Stages: {Stages}",
                jobId,
                providerName,
                providerType,
                isEnabled,
                offlineModeEnabled,
                applicableStages?.Length > 0 ? string.Join(", ", applicableStages) : "All");
        }
        else
        {
            _projectLocks[jobId] = lock_;
            
            _logger.LogInformation(
                "PROJECT_PROFILE_LOCK_SET Job: {JobId}, Provider: {Provider} ({Type}), " +
                "Enabled: {Enabled}, Offline: {Offline}",
                jobId,
                providerName,
                providerType,
                isEnabled,
                offlineModeEnabled);
        }

        OnProfileLockChanged(lock_);
        return lock_;
    }

    /// <summary>
    /// Gets the active profile lock for a job, checking session first then project
    /// </summary>
    public ProviderProfileLock? GetProfileLock(string jobId)
    {
        if (_sessionLocks.TryGetValue(jobId, out var sessionLock))
        {
            return sessionLock;
        }

        if (_projectLocks.TryGetValue(jobId, out var projectLock))
        {
            return projectLock;
        }

        return null;
    }

    /// <summary>
    /// Checks if a provider request is allowed by the active profile lock
    /// </summary>
    public bool ValidateProviderRequest(
        string jobId,
        string providerName,
        string stageName,
        bool providerRequiresNetwork,
        out string? validationError)
    {
        validationError = null;

        var lock_ = GetProfileLock(jobId);
        if (lock_ == null)
        {
            return true; // No lock, all providers allowed
        }

        if (!lock_.IsEnabled)
        {
            return true; // Lock disabled
        }

        if (!lock_.ValidateProvider(providerName, stageName))
        {
            validationError = $"ProfileLock violation: Provider {providerName} does not match locked provider {lock_.ProviderName} for stage {stageName}. " +
                             $"Use manual fallback to switch providers.";
            return false;
        }

        if (!lock_.IsProviderOfflineCompatible(providerRequiresNetwork))
        {
            validationError = $"Offline mode violation: Provider {providerName} requires network access but offline mode is enforced. " +
                             $"Disable offline mode or switch to an offline-compatible provider.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Unlocks the profile lock for a job, allowing provider switching
    /// </summary>
    public bool UnlockProfileLock(string jobId, bool isSessionLevel = true)
    {
        ProviderProfileLock? lock_ = null;

        if (isSessionLevel && _sessionLocks.TryGetValue(jobId, out var sessionLock))
        {
            lock_ = sessionLock.WithEnabled(false);
            _sessionLocks[jobId] = lock_;
            SaveSessionLockAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        else if (!isSessionLevel && _projectLocks.TryGetValue(jobId, out var projectLock))
        {
            lock_ = projectLock.WithEnabled(false);
            _projectLocks[jobId] = lock_;
        }

        if (lock_ != null)
        {
            _logger.LogInformation(
                "PROFILE_LOCK_UNLOCKED Job: {JobId}, Provider: {Provider}, Level: {Level}",
                jobId,
                lock_.ProviderName,
                isSessionLevel ? "Session" : "Project");

            OnProfileLockChanged(lock_);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a profile lock completely
    /// </summary>
    public bool RemoveProfileLock(string jobId, bool isSessionLevel = true)
    {
        bool removed = false;

        if (isSessionLevel)
        {
            removed = _sessionLocks.TryRemove(jobId, out var lock_);
            if (removed)
            {
                SaveSessionLockAsync(CancellationToken.None).GetAwaiter().GetResult();
                _logger.LogInformation("SESSION_PROFILE_LOCK_REMOVED Job: {JobId}", jobId);
            }
        }
        else
        {
            removed = _projectLocks.TryRemove(jobId, out var lock_);
            if (removed)
            {
                _logger.LogInformation("PROJECT_PROFILE_LOCK_REMOVED Job: {JobId}", jobId);
            }
        }

        return removed;
    }

    /// <summary>
    /// Checks if offline mode is compatible with a provider
    /// </summary>
    public bool IsOfflineCompatible(string providerName, out string? message)
    {
        message = null;

        var offlineProviders = new[] { "RuleBased", "Ollama", "Windows", "Piper", "Mimic3", "LocalSD", "Stock" };
        
        var isCompatible = Array.Exists(offlineProviders, p => 
            string.Equals(p, providerName, StringComparison.OrdinalIgnoreCase));

        if (!isCompatible)
        {
            message = $"Provider {providerName} requires network access and is not compatible with offline mode. " +
                     $"Offline-compatible providers: {string.Join(", ", offlineProviders)}";
        }

        return isCompatible;
    }

    /// <summary>
    /// Loads session lock from persistent storage
    /// </summary>
    private void LoadSessionLock()
    {
        try
        {
            if (File.Exists(_sessionLockPath))
            {
                var json = File.ReadAllText(_sessionLockPath);
                var locks = JsonSerializer.Deserialize<ProviderProfileLock[]>(json);
                
                if (locks != null)
                {
                    foreach (var lock_ in locks)
                    {
                        _sessionLocks[lock_.JobId] = lock_;
                    }
                    
                    _logger.LogInformation("Loaded {Count} session profile locks from disk", locks.Length);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load session profile locks, starting with empty state");
        }
    }

    /// <summary>
    /// Saves session lock to persistent storage
    /// </summary>
    private async Task SaveSessionLockAsync(CancellationToken ct)
    {
        try
        {
            var locks = _sessionLocks.Values;
            var json = JsonSerializer.Serialize(locks, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_sessionLockPath, json, ct);
            _logger.LogDebug("Saved {Count} session profile locks to disk", locks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session profile locks");
        }
    }

    /// <summary>
    /// Raises the ProfileLockChanged event
    /// </summary>
    private void OnProfileLockChanged(ProviderProfileLock lock_)
    {
        ProfileLockChanged?.Invoke(this, lock_);
    }

    /// <summary>
    /// Clears all session locks (e.g., on application shutdown or user logout)
    /// </summary>
    public async Task ClearSessionLocksAsync(CancellationToken ct = default)
    {
        _sessionLocks.Clear();
        await SaveSessionLockAsync(ct);
        _logger.LogInformation("Cleared all session profile locks");
    }

    /// <summary>
    /// Gets statistics about active profile locks
    /// </summary>
    public ProviderProfileLockStatistics GetStatistics()
    {
        var enabledSessionLocks = 0;
        var enabledProjectLocks = 0;
        var offlineModeCount = 0;

        foreach (var lock_ in _sessionLocks.Values)
        {
            if (lock_.IsEnabled)
            {
                enabledSessionLocks++;
                if (lock_.OfflineModeEnabled)
                    offlineModeCount++;
            }
        }

        foreach (var lock_ in _projectLocks.Values)
        {
            if (lock_.IsEnabled)
            {
                enabledProjectLocks++;
                if (lock_.OfflineModeEnabled)
                    offlineModeCount++;
            }
        }

        return new ProviderProfileLockStatistics
        {
            TotalSessionLocks = _sessionLocks.Count,
            TotalProjectLocks = _projectLocks.Count,
            EnabledSessionLocks = enabledSessionLocks,
            EnabledProjectLocks = enabledProjectLocks,
            OfflineModeLocksCount = offlineModeCount
        };
    }
}

/// <summary>
/// Statistics about provider profile locks
/// </summary>
public sealed class ProviderProfileLockStatistics
{
    public int TotalSessionLocks { get; init; }
    public int TotalProjectLocks { get; init; }
    public int EnabledSessionLocks { get; init; }
    public int EnabledProjectLocks { get; init; }
    public int OfflineModeLocksCount { get; init; }
}
