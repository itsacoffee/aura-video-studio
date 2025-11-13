using System;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Persistent provider profile lock that ensures the chosen provider remains active
/// across the entire pipeline. Prevents automatic provider switching unless explicitly
/// requested by the user.
/// </summary>
public sealed class ProviderProfileLock
{
    /// <summary>
    /// Gets the unique identifier for the job or project this lock is associated with
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
    /// Gets whether this ProfileLock is enabled and should be enforced
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the timestamp when the lock was created
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets whether offline mode enforcement is enabled for this lock
    /// </summary>
    public bool OfflineModeEnabled { get; }

    /// <summary>
    /// Gets the stages this lock applies to (empty = all stages)
    /// Stages: "planning", "script_generation", "refinement", "tts", "visual_prompts", "rendering"
    /// </summary>
    public string[] ApplicableStages { get; }

    /// <summary>
    /// Gets additional metadata for the lock (extensible)
    /// </summary>
    public ProviderProfileLockMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new provider profile lock
    /// </summary>
    public ProviderProfileLock(
        string jobId,
        string providerName,
        string providerType,
        bool isEnabled,
        bool offlineModeEnabled = false,
        string[]? applicableStages = null,
        ProviderProfileLockMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or whitespace", nameof(jobId));
        
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));
        
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be null or whitespace", nameof(providerType));

        JobId = jobId;
        ProviderName = providerName;
        ProviderType = providerType;
        IsEnabled = isEnabled;
        CreatedAt = DateTime.UtcNow;
        OfflineModeEnabled = offlineModeEnabled;
        ApplicableStages = applicableStages ?? Array.Empty<string>();
        Metadata = metadata ?? new ProviderProfileLockMetadata();
    }

    /// <summary>
    /// Creates a new ProfileLock with updated properties
    /// </summary>
    public ProviderProfileLock WithEnabled(bool isEnabled)
    {
        return new ProviderProfileLock(
            JobId,
            ProviderName,
            ProviderType,
            isEnabled,
            OfflineModeEnabled,
            ApplicableStages,
            Metadata);
    }

    /// <summary>
    /// Creates a new ProfileLock with updated offline mode setting
    /// </summary>
    public ProviderProfileLock WithOfflineMode(bool offlineModeEnabled)
    {
        return new ProviderProfileLock(
            JobId,
            ProviderName,
            ProviderType,
            IsEnabled,
            offlineModeEnabled,
            ApplicableStages,
            Metadata);
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
    /// Validates that a provider request matches this profile lock
    /// </summary>
    public bool ValidateProvider(string providerName, string stageName)
    {
        if (!IsEnabled)
            return true; // Lock disabled, any provider allowed

        if (!AppliesToStage(stageName))
            return true; // Stage not governed by this lock

        return string.Equals(ProviderName, providerName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a provider is compatible with offline mode settings
    /// </summary>
    public bool IsProviderOfflineCompatible(bool providerRequiresNetwork)
    {
        if (!OfflineModeEnabled)
            return true; // Offline mode not enforced, any provider allowed

        return !providerRequiresNetwork; // Only allow offline-capable providers
    }

    public override string ToString()
    {
        var status = IsEnabled ? "Enabled" : "Disabled";
        var stages = ApplicableStages.Length == 0 ? "All" : string.Join(", ", ApplicableStages);
        var offlineMode = OfflineModeEnabled ? "Offline" : "Online";
        return $"ProviderProfileLock[{ProviderName} ({ProviderType}), {status}, {offlineMode}, Stages: {stages}]";
    }
}

/// <summary>
/// Additional metadata for provider profile locks
/// </summary>
public sealed class ProviderProfileLockMetadata
{
    /// <summary>
    /// Gets or initializes the user who created this lock
    /// </summary>
    public string? CreatedByUser { get; init; }

    /// <summary>
    /// Gets or initializes the reason for creating this lock
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or initializes custom tags for this lock
    /// </summary>
    public string[] Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or initializes the source of this lock (e.g., "User", "Project", "Session", "Preset")
    /// </summary>
    public string Source { get; init; } = "User";

    /// <summary>
    /// Gets or initializes whether manual fallback is allowed
    /// </summary>
    public bool AllowManualFallback { get; init; } = true;

    /// <summary>
    /// Gets or initializes the maximum wait time before offering fallback (seconds)
    /// </summary>
    public int? MaxWaitBeforeFallbackSeconds { get; init; }
}
