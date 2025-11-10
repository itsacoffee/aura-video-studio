using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aura.Core.Services.FeatureFlags;

/// <summary>
/// Implementation of feature flag service with Redis caching
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly string _environment;
    private const string CacheKeyPrefix = "feature_flag:";
    private const int CacheExpirationMinutes = 5;
    
    public FeatureFlagService(
        IDistributedCache cache,
        ILogger<FeatureFlagService> logger,
        string environment = "production")
    {
        _cache = cache;
        _logger = logger;
        _environment = environment;
    }
    
    public async Task<bool> IsEnabledAsync(string featureName)
    {
        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag == null)
        {
            _logger.LogWarning("Feature flag {FeatureName} not found, defaulting to disabled", featureName);
            return false;
        }
        
        // Check if enabled for current environment
        if (flag.AllowedEnvironments.Count > 0 && 
            !flag.AllowedEnvironments.Contains(_environment))
        {
            return false;
        }
        
        return flag.IsEnabled;
    }
    
    public async Task<bool> IsEnabledForUserAsync(string featureName, string userId)
    {
        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag == null)
        {
            return false;
        }
        
        // Check allowlist first
        if (flag.AllowedUsers.Contains(userId))
        {
            _logger.LogDebug("Feature {FeatureName} enabled for user {UserId} via allowlist", 
                featureName, userId);
            return true;
        }
        
        // Check global enablement
        if (!flag.IsEnabled)
        {
            return false;
        }
        
        // Check environment
        if (flag.AllowedEnvironments.Count > 0 && 
            !flag.AllowedEnvironments.Contains(_environment))
        {
            return false;
        }
        
        // Check rollout percentage
        if (flag.RolloutPercentage < 100)
        {
            return await IsEnabledWithRolloutAsync(featureName, userId);
        }
        
        return true;
    }
    
    public async Task<bool> IsEnabledWithRolloutAsync(string featureName, string identifier)
    {
        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag == null || !flag.IsEnabled)
        {
            return false;
        }
        
        // Calculate consistent hash for percentage rollout
        var hash = CalculateRolloutHash(featureName, identifier);
        var isEnabled = hash <= flag.RolloutPercentage;
        
        _logger.LogDebug(
            "Feature {FeatureName} rollout check for {Identifier}: hash={Hash}, threshold={Threshold}, enabled={Enabled}",
            featureName, identifier, hash, flag.RolloutPercentage, isEnabled);
        
        return isEnabled;
    }
    
    public async Task<IEnumerable<FeatureFlag>> GetAllFlagsAsync()
    {
        // In production, this would query from a database or feature flag service
        // For now, return from cache or default flags
        var flags = new List<FeatureFlag>
        {
            new() { 
                Name = "advanced_editing", 
                Description = "Advanced video editing features",
                IsEnabled = false,
                RolloutPercentage = 10,
                AllowedEnvironments = new List<string> { "staging", "production" },
                CreatedAt = DateTime.UtcNow
            },
            new() { 
                Name = "ai_voice_generation", 
                Description = "AI-powered voice generation",
                IsEnabled = false,
                RolloutPercentage = 5,
                AllowedEnvironments = new List<string> { "staging" },
                CreatedAt = DateTime.UtcNow
            },
            new() { 
                Name = "cloud_rendering", 
                Description = "Cloud-based video rendering",
                IsEnabled = false,
                RolloutPercentage = 0,
                AllowedEnvironments = new List<string> { "staging" },
                CreatedAt = DateTime.UtcNow
            },
            new() { 
                Name = "enhanced_analytics", 
                Description = "Enhanced cost analytics dashboard",
                IsEnabled = true,
                RolloutPercentage = 100,
                AllowedEnvironments = new List<string> { "staging", "production" },
                CreatedAt = DateTime.UtcNow
            }
        };
        
        return await Task.FromResult(flags);
    }
    
    public async Task EnableFeatureAsync(string featureName)
    {
        var flag = await GetFeatureFlagAsync(featureName) 
            ?? new FeatureFlag { Name = featureName, CreatedAt = DateTime.UtcNow };
        
        flag.IsEnabled = true;
        flag.LastModifiedAt = DateTime.UtcNow;
        
        await SaveFeatureFlagAsync(flag);
        
        _logger.LogInformation("Feature {FeatureName} enabled", featureName);
    }
    
    public async Task DisableFeatureAsync(string featureName)
    {
        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag != null)
        {
            flag.IsEnabled = false;
            flag.LastModifiedAt = DateTime.UtcNow;
            
            await SaveFeatureFlagAsync(flag);
            
            _logger.LogInformation("Feature {FeatureName} disabled", featureName);
        }
    }
    
    public async Task SetRolloutPercentageAsync(string featureName, int percentage)
    {
        if (percentage < 0 || percentage > 100)
        {
            throw new ArgumentException("Percentage must be between 0 and 100", nameof(percentage));
        }
        
        var flag = await GetFeatureFlagAsync(featureName) 
            ?? new FeatureFlag { Name = featureName, CreatedAt = DateTime.UtcNow };
        
        flag.RolloutPercentage = percentage;
        flag.LastModifiedAt = DateTime.UtcNow;
        
        await SaveFeatureFlagAsync(flag);
        
        _logger.LogInformation("Feature {FeatureName} rollout percentage set to {Percentage}%", 
            featureName, percentage);
    }
    
    public async Task AddUserToAllowlistAsync(string featureName, string userId)
    {
        var flag = await GetFeatureFlagAsync(featureName) 
            ?? new FeatureFlag { Name = featureName, CreatedAt = DateTime.UtcNow };
        
        if (!flag.AllowedUsers.Contains(userId))
        {
            flag.AllowedUsers.Add(userId);
            flag.LastModifiedAt = DateTime.UtcNow;
            
            await SaveFeatureFlagAsync(flag);
            
            _logger.LogInformation("User {UserId} added to feature {FeatureName} allowlist", 
                userId, featureName);
        }
    }
    
    public async Task RemoveUserFromAllowlistAsync(string featureName, string userId)
    {
        var flag = await GetFeatureFlagAsync(featureName);
        
        if (flag != null && flag.AllowedUsers.Contains(userId))
        {
            flag.AllowedUsers.Remove(userId);
            flag.LastModifiedAt = DateTime.UtcNow;
            
            await SaveFeatureFlagAsync(flag);
            
            _logger.LogInformation("User {UserId} removed from feature {FeatureName} allowlist", 
                userId, featureName);
        }
    }
    
    private async Task<FeatureFlag?> GetFeatureFlagAsync(string featureName)
    {
        var cacheKey = $"{CacheKeyPrefix}{featureName}";
        
        try
        {
            var cachedValue = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedValue))
            {
                return JsonSerializer.Deserialize<FeatureFlag>(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature flag {FeatureName} from cache", featureName);
        }
        
        // Fallback to default flags
        var allFlags = await GetAllFlagsAsync();
        return allFlags.FirstOrDefault(f => f.Name == featureName);
    }
    
    private async Task SaveFeatureFlagAsync(FeatureFlag flag)
    {
        var cacheKey = $"{CacheKeyPrefix}{flag.Name}";
        
        try
        {
            var serialized = JsonSerializer.Serialize(flag);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            
            await _cache.SetStringAsync(cacheKey, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving feature flag {FeatureName} to cache", flag.Name);
            throw;
        }
    }
    
    private int CalculateRolloutHash(string featureName, string identifier)
    {
        // Create consistent hash for percentage-based rollout
        var input = $"{featureName}:{identifier}";
        var bytes = Encoding.UTF8.GetBytes(input);
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        
        // Convert first 4 bytes to int and normalize to 0-100
        var hashInt = BitConverter.ToInt32(hash, 0);
        return Math.Abs(hashInt % 100);
    }
}
