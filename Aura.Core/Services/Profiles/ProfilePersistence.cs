using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Profiles;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Profiles;

/// <summary>
/// Manages persistence of user profiles and preferences to disk
/// Follows the same pattern as ContextPersistence for consistency
/// </summary>
public class ProfilePersistence
{
    private readonly ILogger<ProfilePersistence> _logger;
    private readonly string _profilesDirectory;
    private readonly string _preferencesDirectory;
    private readonly string _decisionsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public ProfilePersistence(ILogger<ProfilePersistence> logger, string baseDirectory)
    {
        _logger = logger;
        _profilesDirectory = Path.Combine(baseDirectory, "Profiles");
        _preferencesDirectory = Path.Combine(baseDirectory, "Profiles", "Preferences");
        _decisionsDirectory = Path.Combine(baseDirectory, "Profiles", "Decisions");
        
        // Ensure directories exist
        Directory.CreateDirectory(_profilesDirectory);
        Directory.CreateDirectory(_preferencesDirectory);
        Directory.CreateDirectory(_decisionsDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Profile Operations

    /// <summary>
    /// Save profile metadata to disk
    /// </summary>
    public async Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetProfileFilePath(profile.ProfileId);
            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            
            // Write to temp file first, then rename for atomic operation
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved profile {ProfileId}", profile.ProfileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load profile metadata from disk
    /// </summary>
    public async Task<UserProfile?> LoadProfileAsync(string profileId, CancellationToken ct = default)
    {
        var filePath = GetProfileFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No profile found for {ProfileId}", profileId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var profile = JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
            _logger.LogDebug("Loaded profile {ProfileId}", profileId);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile {ProfileId}", profileId);
            return null;
        }
    }

    /// <summary>
    /// Load all profiles for a user
    /// </summary>
    public async Task<List<UserProfile>> LoadUserProfilesAsync(string userId, CancellationToken ct = default)
    {
        var profiles = new List<UserProfile>();
        
        if (!Directory.Exists(_profilesDirectory))
        {
            return profiles;
        }

        var files = Directory.GetFiles(_profilesDirectory, "*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
                var profile = JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
                
                if (profile != null && profile.UserId == userId)
                {
                    profiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile from {File}", file);
            }
        }
        
        _logger.LogDebug("Loaded {Count} profiles for user {UserId}", profiles.Count, userId);
        return profiles.OrderByDescending(p => p.LastUsed).ToList();
    }

    /// <summary>
    /// Delete profile from disk
    /// </summary>
    public async Task DeleteProfileAsync(string profileId, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var profilePath = GetProfileFilePath(profileId);
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
            }
            
            var preferencesPath = GetPreferencesFilePath(profileId);
            if (File.Exists(preferencesPath))
            {
                File.Delete(preferencesPath);
            }
            
            // Delete decision history for this profile
            var decisionsPath = GetDecisionsFilePath(profileId);
            if (File.Exists(decisionsPath))
            {
                File.Delete(decisionsPath);
            }
            
            _logger.LogInformation("Deleted profile {ProfileId} and associated data", profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    #endregion

    #region Preferences Operations

    /// <summary>
    /// Save profile preferences to disk
    /// </summary>
    public async Task SavePreferencesAsync(ProfilePreferences preferences, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetPreferencesFilePath(preferences.ProfileId);
            var json = JsonSerializer.Serialize(preferences, _jsonOptions);
            
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved preferences for profile {ProfileId}", preferences.ProfileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load profile preferences from disk
    /// </summary>
    public async Task<ProfilePreferences?> LoadPreferencesAsync(string profileId, CancellationToken ct = default)
    {
        var filePath = GetPreferencesFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No preferences found for profile {ProfileId}, using defaults", profileId);
            return CreateDefaultPreferences(profileId);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var preferences = JsonSerializer.Deserialize<ProfilePreferences>(json, _jsonOptions);
            _logger.LogDebug("Loaded preferences for profile {ProfileId}", profileId);
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preferences for profile {ProfileId}", profileId);
            return CreateDefaultPreferences(profileId);
        }
    }

    #endregion

    #region Decision History Operations

    /// <summary>
    /// Record a user decision
    /// </summary>
    public async Task RecordDecisionAsync(DecisionRecord decision, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var decisions = await LoadDecisionsAsync(decision.ProfileId, ct).ConfigureAwait(false);
            decisions.Add(decision);
            
            var filePath = GetDecisionsFilePath(decision.ProfileId);
            var json = JsonSerializer.Serialize(decisions, _jsonOptions);
            
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Recorded decision for profile {ProfileId}: {Type} -> {Decision}",
                decision.ProfileId, decision.SuggestionType, decision.Decision);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load decision history for a profile
    /// </summary>
    public async Task<List<DecisionRecord>> LoadDecisionsAsync(string profileId, CancellationToken ct = default)
    {
        var filePath = GetDecisionsFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            return new List<DecisionRecord>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var decisions = JsonSerializer.Deserialize<List<DecisionRecord>>(json, _jsonOptions);
            return decisions ?? new List<DecisionRecord>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load decisions for profile {ProfileId}", profileId);
            return new List<DecisionRecord>();
        }
    }

    #endregion

    #region Helper Methods

    private string GetProfileFilePath(string profileId)
    {
        return Path.Combine(_profilesDirectory, $"{profileId}.json");
    }

    private string GetPreferencesFilePath(string profileId)
    {
        return Path.Combine(_preferencesDirectory, $"{profileId}.json");
    }

    private string GetDecisionsFilePath(string profileId)
    {
        return Path.Combine(_decisionsDirectory, $"{profileId}.json");
    }

    private static ProfilePreferences CreateDefaultPreferences(string profileId)
    {
        return new ProfilePreferences(
            ProfileId: profileId,
            ContentType: "general",
            Tone: new TonePreferences(
                Formality: 50,
                Energy: 50,
                PersonalityTraits: new List<string> { "friendly" },
                CustomDescription: null
            ),
            Visual: new VisualPreferences(
                Aesthetic: "balanced",
                ColorPalette: "natural",
                ShotTypePreference: "balanced",
                CompositionStyle: "rule of thirds",
                PacingPreference: "moderate",
                BRollUsage: "moderate"
            ),
            Audio: new AudioPreferences(
                MusicGenres: new List<string> { "ambient", "instrumental" },
                MusicEnergy: 50,
                MusicProminence: "balanced",
                SoundEffectsUsage: "minimal",
                VoiceStyle: "warm",
                AudioMixing: "balanced with music"
            ),
            Editing: new EditingPreferences(
                Pacing: 50,
                CutFrequency: 50,
                TransitionStyle: "simple cuts",
                EffectUsage: "subtle",
                SceneDuration: 5,
                EditingPhilosophy: "invisible editing"
            ),
            Platform: new PlatformPreferences(
                PrimaryPlatform: "YouTube",
                SecondaryPlatforms: new List<string>(),
                AspectRatio: "16:9",
                TargetDurationSeconds: 600,
                AudienceDemographic: "general"
            ),
            AIBehavior: new AIBehaviorSettings(
                AssistanceLevel: 50,
                SuggestionVerbosity: "moderate",
                AutoApplySuggestions: false,
                SuggestionFrequency: "moderate",
                CreativityLevel: 50,
                OverridePermissions: new List<string> { "major_changes", "delete_content" }
            )
        );
    }

    #endregion
}
