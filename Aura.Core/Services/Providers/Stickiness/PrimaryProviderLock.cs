using System;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Immutable lock that ensures a single provider remains active for the entire job duration.
/// Prevents silent provider switching and enforces explicit user control over fallback decisions.
/// </summary>
public sealed class PrimaryProviderLock
{
    /// <summary>
    /// Gets the unique identifier for the job this lock is associated with
    /// </summary>
    public string JobId { get; }

    /// <summary>
    /// Gets the locked provider name (e.g., "Ollama", "OpenAI")
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets the provider type category (e.g., "local_llm", "cloud_llm", "tts")
    /// </summary>
    public string ProviderType { get; }

    /// <summary>
    /// Gets the timestamp when the lock was created
    /// </summary>
    public DateTime LockedAt { get; }

    /// <summary>
    /// Gets whether the lock can be overridden (false means absolute lock)
    /// </summary>
    public bool IsOverrideable { get; }

    /// <summary>
    /// Gets the correlation ID for tracking across distributed systems
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Gets the stages this lock applies to (empty = all stages)
    /// </summary>
    public string[] ApplicableStages { get; }

    /// <summary>
    /// Gets whether this lock has been explicitly unlocked by the user
    /// </summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>
    /// Gets the timestamp when the lock was unlocked (null if still locked)
    /// </summary>
    public DateTime? UnlockedAt { get; private set; }

    /// <summary>
    /// Gets the reason for unlocking (null if still locked)
    /// </summary>
    public string? UnlockReason { get; private set; }

    /// <summary>
    /// Initializes a new provider lock
    /// </summary>
    public PrimaryProviderLock(
        string jobId,
        string providerName,
        string providerType,
        string correlationId,
        bool isOverrideable = false,
        params string[] applicableStages)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or whitespace", nameof(jobId));
        
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));
        
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be null or whitespace", nameof(providerType));

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or whitespace", nameof(correlationId));

        JobId = jobId;
        ProviderName = providerName;
        ProviderType = providerType;
        CorrelationId = correlationId;
        LockedAt = DateTime.UtcNow;
        IsOverrideable = isOverrideable;
        ApplicableStages = applicableStages ?? Array.Empty<string>();
        IsUnlocked = false;
    }

    /// <summary>
    /// Attempts to unlock this provider lock with user consent
    /// </summary>
    /// <param name="reason">The reason for unlocking (e.g., "USER_REQUEST", "PROVIDER_FATAL_ERROR")</param>
    /// <returns>True if successfully unlocked, false if already unlocked or not overrideable</returns>
    public bool TryUnlock(string reason)
    {
        if (IsUnlocked)
            return false;

        if (!IsOverrideable)
            return false;

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Unlock reason cannot be null or whitespace", nameof(reason));

        IsUnlocked = true;
        UnlockedAt = DateTime.UtcNow;
        UnlockReason = reason;
        return true;
    }

    /// <summary>
    /// Checks if this lock applies to a specific stage
    /// </summary>
    public bool AppliesToStage(string stageName)
    {
        if (ApplicableStages.Length == 0)
            return true; // Applies to all stages

        foreach (var stage in ApplicableStages)
        {
            if (string.Equals(stage, stageName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates that a provider request matches this lock
    /// </summary>
    public bool ValidateProvider(string providerName, string stageName)
    {
        if (IsUnlocked)
            return true; // Lock released, any provider allowed

        if (!AppliesToStage(stageName))
            return true; // Stage not governed by this lock

        return string.Equals(ProviderName, providerName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the duration this lock has been active
    /// </summary>
    public TimeSpan GetLockDuration()
    {
        var endTime = IsUnlocked ? UnlockedAt!.Value : DateTime.UtcNow;
        return endTime - LockedAt;
    }

    public override string ToString()
    {
        var status = IsUnlocked ? $"Unlocked at {UnlockedAt}" : "Active";
        var stages = ApplicableStages.Length == 0 ? "All" : string.Join(", ", ApplicableStages);
        return $"ProviderLock[{ProviderName} ({ProviderType}) for Job {JobId}, Stages: {stages}, {status}]";
    }
}
