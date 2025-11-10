using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aura.Core.Services.FeatureFlags;

/// <summary>
/// Service for managing feature flags in production
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName);
    
    /// <summary>
    /// Check if a feature is enabled for a specific user
    /// </summary>
    Task<bool> IsEnabledForUserAsync(string featureName, string userId);
    
    /// <summary>
    /// Check if a feature is enabled with percentage rollout
    /// </summary>
    Task<bool> IsEnabledWithRolloutAsync(string featureName, string identifier);
    
    /// <summary>
    /// Get all feature flags
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetAllFlagsAsync();
    
    /// <summary>
    /// Enable a feature flag
    /// </summary>
    Task EnableFeatureAsync(string featureName);
    
    /// <summary>
    /// Disable a feature flag
    /// </summary>
    Task DisableFeatureAsync(string featureName);
    
    /// <summary>
    /// Set rollout percentage for a feature
    /// </summary>
    Task SetRolloutPercentageAsync(string featureName, int percentage);
    
    /// <summary>
    /// Add a user to feature allowlist
    /// </summary>
    Task AddUserToAllowlistAsync(string featureName, string userId);
    
    /// <summary>
    /// Remove a user from feature allowlist
    /// </summary>
    Task RemoveUserFromAllowlistAsync(string featureName, string userId);
}

/// <summary>
/// Feature flag definition
/// </summary>
public class FeatureFlag
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int RolloutPercentage { get; set; }
    public List<string> AllowedUsers { get; set; } = new();
    public List<string> AllowedEnvironments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
