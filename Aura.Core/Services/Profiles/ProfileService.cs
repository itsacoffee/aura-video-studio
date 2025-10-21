using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Profiles;

/// <summary>
/// Manages user profiles and preferences with full CRUD operations
/// </summary>
public class ProfileService
{
    private readonly ILogger<ProfileService> _logger;
    private readonly ProfilePersistence _persistence;
    private readonly Dictionary<string, UserProfile> _profileCache = new();
    private readonly Dictionary<string, ProfilePreferences> _preferencesCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public ProfileService(
        ILogger<ProfileService> logger,
        ProfilePersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    #region Profile CRUD Operations

    /// <summary>
    /// Create a new user profile
    /// </summary>
    public async Task<UserProfile> CreateProfileAsync(
        CreateProfileRequest request,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var profileId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            // Check if this should be the default profile (first profile for user)
            var existingProfiles = await _persistence.LoadUserProfilesAsync(request.UserId, ct);
            var isDefault = existingProfiles.Count == 0;

            var profile = new UserProfile(
                ProfileId: profileId,
                UserId: request.UserId,
                ProfileName: request.ProfileName,
                Description: request.Description,
                IsDefault: isDefault,
                IsActive: isDefault, // First profile is also active
                CreatedAt: now,
                LastUsed: now,
                UpdatedAt: now
            );

            await _persistence.SaveProfileAsync(profile, ct);
            _profileCache[profileId] = profile;

            // Create preferences from template or defaults
            ProfilePreferences preferences;
            if (!string.IsNullOrEmpty(request.FromTemplateId))
            {
                var template = ProfileTemplateService.GetTemplate(request.FromTemplateId);
                if (template != null)
                {
                    preferences = template.DefaultPreferences with { ProfileId = profileId };
                }
                else
                {
                    preferences = await _persistence.LoadPreferencesAsync(profileId, ct) 
                        ?? throw new InvalidOperationException("Failed to create default preferences");
                }
            }
            else
            {
                preferences = await _persistence.LoadPreferencesAsync(profileId, ct)
                    ?? throw new InvalidOperationException("Failed to create default preferences");
            }

            await _persistence.SavePreferencesAsync(preferences, ct);
            _preferencesCache[profileId] = preferences;

            _logger.LogInformation("Created profile {ProfileId} for user {UserId}", profileId, request.UserId);
            return profile;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get a specific profile by ID
    /// </summary>
    public async Task<UserProfile?> GetProfileAsync(string profileId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            if (_profileCache.TryGetValue(profileId, out var cached))
            {
                return cached;
            }

            var profile = await _persistence.LoadProfileAsync(profileId, ct);
            if (profile != null)
            {
                _profileCache[profileId] = profile;
            }

            return profile;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Get all profiles for a user
    /// </summary>
    public async Task<List<UserProfile>> GetUserProfilesAsync(string userId, CancellationToken ct = default)
    {
        var profiles = await _persistence.LoadUserProfilesAsync(userId, ct);
        
        await _cacheLock.WaitAsync(ct);
        try
        {
            foreach (var profile in profiles)
            {
                _profileCache[profile.ProfileId] = profile;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        return profiles;
    }

    /// <summary>
    /// Get the active profile for a user
    /// </summary>
    public async Task<UserProfile?> GetActiveProfileAsync(string userId, CancellationToken ct = default)
    {
        var profiles = await GetUserProfilesAsync(userId, ct);
        return profiles.FirstOrDefault(p => p.IsActive);
    }

    /// <summary>
    /// Update profile metadata
    /// </summary>
    public async Task<UserProfile> UpdateProfileAsync(
        string profileId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var existing = await GetProfileAsync(profileId, ct);
            if (existing == null)
            {
                throw new InvalidOperationException($"Profile {profileId} not found");
            }

            var updated = existing with
            {
                ProfileName = request.ProfileName ?? existing.ProfileName,
                Description = request.Description ?? existing.Description,
                UpdatedAt = DateTime.UtcNow
            };

            await _persistence.SaveProfileAsync(updated, ct);
            _profileCache[profileId] = updated;

            _logger.LogInformation("Updated profile {ProfileId}", profileId);
            return updated;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Delete a profile (cannot delete default or only profile)
    /// </summary>
    public async Task DeleteProfileAsync(string profileId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var profile = await GetProfileAsync(profileId, ct);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile {profileId} not found");
            }

            // Check if this is the only profile
            var userProfiles = await GetUserProfilesAsync(profile.UserId, ct);
            if (userProfiles.Count <= 1)
            {
                throw new InvalidOperationException("Cannot delete the only profile");
            }

            // If deleting the default or active profile, promote another
            if (profile.IsDefault || profile.IsActive)
            {
                var nextProfile = userProfiles.FirstOrDefault(p => p.ProfileId != profileId);
                if (nextProfile != null)
                {
                    var updated = nextProfile with
                    {
                        IsDefault = profile.IsDefault || nextProfile.IsDefault,
                        IsActive = profile.IsActive || nextProfile.IsActive,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _persistence.SaveProfileAsync(updated, ct);
                    _profileCache[nextProfile.ProfileId] = updated;
                }
            }

            await _persistence.DeleteProfileAsync(profileId, ct);
            _profileCache.Remove(profileId);
            _preferencesCache.Remove(profileId);

            _logger.LogInformation("Deleted profile {ProfileId}", profileId);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Set a profile as active for the user
    /// </summary>
    public async Task<UserProfile> ActivateProfileAsync(string profileId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var profile = await GetProfileAsync(profileId, ct);
            if (profile == null)
            {
                throw new InvalidOperationException($"Profile {profileId} not found");
            }

            // Deactivate all other profiles for this user
            var userProfiles = await GetUserProfilesAsync(profile.UserId, ct);
            foreach (var p in userProfiles.Where(p => p.IsActive && p.ProfileId != profileId))
            {
                var deactivated = p with { IsActive = false, UpdatedAt = DateTime.UtcNow };
                await _persistence.SaveProfileAsync(deactivated, ct);
                _profileCache[p.ProfileId] = deactivated;
            }

            // Activate this profile
            var activated = profile with
            {
                IsActive = true,
                LastUsed = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _persistence.SaveProfileAsync(activated, ct);
            _profileCache[profileId] = activated;

            _logger.LogInformation("Activated profile {ProfileId} for user {UserId}", profileId, profile.UserId);
            return activated;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Duplicate an existing profile
    /// </summary>
    public async Task<UserProfile> DuplicateProfileAsync(
        string sourceProfileId,
        string newProfileName,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var source = await GetProfileAsync(sourceProfileId, ct);
            if (source == null)
            {
                throw new InvalidOperationException($"Source profile {sourceProfileId} not found");
            }

            var newProfileId = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            var newProfile = new UserProfile(
                ProfileId: newProfileId,
                UserId: source.UserId,
                ProfileName: newProfileName,
                Description: source.Description,
                IsDefault: false,
                IsActive: false,
                CreatedAt: now,
                LastUsed: now,
                UpdatedAt: now
            );

            await _persistence.SaveProfileAsync(newProfile, ct);
            _profileCache[newProfileId] = newProfile;

            // Copy preferences
            var sourcePreferences = await GetPreferencesAsync(sourceProfileId, ct);
            var newPreferences = sourcePreferences with { ProfileId = newProfileId };
            await _persistence.SavePreferencesAsync(newPreferences, ct);
            _preferencesCache[newProfileId] = newPreferences;

            _logger.LogInformation("Duplicated profile {SourceId} to {NewId}", sourceProfileId, newProfileId);
            return newProfile;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    #endregion

    #region Preferences Operations

    /// <summary>
    /// Get preferences for a profile
    /// </summary>
    public async Task<ProfilePreferences> GetPreferencesAsync(string profileId, CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            if (_preferencesCache.TryGetValue(profileId, out var cached))
            {
                return cached;
            }

            var preferences = await _persistence.LoadPreferencesAsync(profileId, ct);
            if (preferences == null)
            {
                throw new InvalidOperationException($"Preferences not found for profile {profileId}");
            }

            _preferencesCache[profileId] = preferences;
            return preferences;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Update profile preferences
    /// </summary>
    public async Task<ProfilePreferences> UpdatePreferencesAsync(
        string profileId,
        UpdatePreferencesRequest request,
        CancellationToken ct = default)
    {
        await _cacheLock.WaitAsync(ct);
        try
        {
            var existing = await GetPreferencesAsync(profileId, ct);

            var updated = existing with
            {
                ContentType = request.ContentType ?? existing.ContentType,
                Tone = request.Tone ?? existing.Tone,
                Visual = request.Visual ?? existing.Visual,
                Audio = request.Audio ?? existing.Audio,
                Editing = request.Editing ?? existing.Editing,
                Platform = request.Platform ?? existing.Platform,
                AIBehavior = request.AIBehavior ?? existing.AIBehavior
            };

            await _persistence.SavePreferencesAsync(updated, ct);
            _preferencesCache[profileId] = updated;

            // Update profile last modified time
            var profile = await GetProfileAsync(profileId, ct);
            if (profile != null)
            {
                var updatedProfile = profile with { UpdatedAt = DateTime.UtcNow };
                await _persistence.SaveProfileAsync(updatedProfile, ct);
                _profileCache[profileId] = updatedProfile;
            }

            _logger.LogInformation("Updated preferences for profile {ProfileId}", profileId);
            return updated;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    #endregion

    #region Decision Tracking

    /// <summary>
    /// Record a user decision
    /// </summary>
    public async Task RecordDecisionAsync(RecordDecisionRequest request, CancellationToken ct = default)
    {
        var decision = new DecisionRecord(
            RecordId: Guid.NewGuid().ToString("N"),
            ProfileId: request.ProfileId,
            SuggestionType: request.SuggestionType,
            Decision: request.Decision,
            Timestamp: DateTime.UtcNow,
            Context: request.Context
        );

        await _persistence.RecordDecisionAsync(decision, ct);
        _logger.LogInformation("Recorded decision for profile {ProfileId}: {Type} -> {Decision}",
            request.ProfileId, request.SuggestionType, request.Decision);
    }

    /// <summary>
    /// Get decision history for a profile
    /// </summary>
    public async Task<List<DecisionRecord>> GetDecisionHistoryAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadDecisionsAsync(profileId, ct);
    }

    #endregion
}
