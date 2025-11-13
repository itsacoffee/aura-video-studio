using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Provider validation policy configuration defining timeout thresholds and validation behavior
/// </summary>
public class ProviderValidationPolicy
{
    /// <summary>
    /// Provider category (e.g., "local_llm", "cloud_llm", "tts")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Normal validation timeout in milliseconds (first threshold)
    /// </summary>
    public int NormalTimeoutMs { get; set; } = 15000;

    /// <summary>
    /// Extended validation timeout in milliseconds (for slow providers)
    /// </summary>
    public int ExtendedTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Maximum validation timeout before offering manual decision
    /// </summary>
    public int MaxTimeoutMs { get; set; } = 180000;

    /// <summary>
    /// Interval for retry attempts during validation
    /// </summary>
    public int RetryIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to show user UI during extended wait
    /// </summary>
    public bool ShowProgressDuringExtendedWait { get; set; } = true;

    /// <summary>
    /// Description of the validation policy for UI display
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Container for all provider validation policies loaded from configuration
/// </summary>
public class ProviderValidationPolicySet
{
    /// <summary>
    /// Policies mapped by provider category
    /// </summary>
    public Dictionary<string, ProviderValidationPolicy> Policies { get; set; } = new();

    /// <summary>
    /// Default policy for providers without specific configuration
    /// </summary>
    public ProviderValidationPolicy DefaultPolicy { get; set; } = new()
    {
        Category = "default",
        NormalTimeoutMs = 15000,
        ExtendedTimeoutMs = 60000,
        MaxTimeoutMs = 120000,
        RetryIntervalMs = 5000,
        MaxRetries = 3,
        ShowProgressDuringExtendedWait = true,
        Description = "Default validation policy for providers without specific configuration"
    };

    /// <summary>
    /// Mapping of provider names to categories
    /// </summary>
    public Dictionary<string, string> ProviderCategoryMapping { get; set; } = new();

    /// <summary>
    /// Gets the validation policy for a specific provider
    /// </summary>
    public ProviderValidationPolicy GetPolicyForProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return DefaultPolicy;
        }

        // Try to find category mapping
        if (ProviderCategoryMapping.TryGetValue(providerName, out var category))
        {
            if (Policies.TryGetValue(category, out var policy))
            {
                return policy;
            }
        }

        // Fallback to default
        return DefaultPolicy;
    }
}

/// <summary>
/// Validation status for API keys
/// </summary>
public enum KeyValidationStatus
{
    /// <summary>
    /// Not yet validated
    /// </summary>
    NotValidated,

    /// <summary>
    /// Currently validating (within normal timeout)
    /// </summary>
    Validating,

    /// <summary>
    /// Validation in progress, taking longer than normal (extended timeout)
    /// </summary>
    ValidatingExtended,

    /// <summary>
    /// Validation taking very long, awaiting user decision
    /// </summary>
    ValidatingMaxWait,

    /// <summary>
    /// Validation succeeded
    /// </summary>
    Valid,

    /// <summary>
    /// Validation failed with error
    /// </summary>
    Invalid,

    /// <summary>
    /// Provider is slow but still working (warning state)
    /// </summary>
    SlowButWorking,

    /// <summary>
    /// Validation timed out after max wait
    /// </summary>
    TimedOut
}

/// <summary>
/// Result of key validation with detailed status
/// </summary>
public class KeyValidationStatusResult
{
    public string ProviderName { get; set; } = string.Empty;
    public KeyValidationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? LastValidated { get; set; }
    public DateTime? ValidationStarted { get; set; }
    public int ElapsedMs { get; set; }
    public int RemainingTimeoutMs { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public bool CanRetry { get; set; } = true;
    public bool CanManuallyRevalidate { get; set; } = true;
}
