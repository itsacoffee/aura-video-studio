using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Learning;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Learning;

/// <summary>
/// Persists learning data (patterns, insights, preferences) to disk
/// </summary>
public class LearningPersistence
{
    private readonly ILogger<LearningPersistence> _logger;
    private readonly string _learningDirectory;
    private readonly string _patternsDirectory;
    private readonly string _insightsDirectory;
    private readonly string _preferencesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public LearningPersistence(ILogger<LearningPersistence> logger, string baseDirectory)
    {
        _logger = logger;
        _learningDirectory = Path.Combine(baseDirectory, "Learning");
        _patternsDirectory = Path.Combine(_learningDirectory, "Patterns");
        _insightsDirectory = Path.Combine(_learningDirectory, "Insights");
        _preferencesDirectory = Path.Combine(_learningDirectory, "InferredPreferences");
        
        // Ensure directories exist
        Directory.CreateDirectory(_learningDirectory);
        Directory.CreateDirectory(_patternsDirectory);
        Directory.CreateDirectory(_insightsDirectory);
        Directory.CreateDirectory(_preferencesDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Pattern Operations

    /// <summary>
    /// Save patterns for a profile
    /// </summary>
    public async Task SavePatternsAsync(
        string profileId,
        List<DecisionPattern> patterns,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetPatternsFilePath(profileId);
            var json = JsonSerializer.Serialize(patterns, _jsonOptions);
            
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved {Count} patterns for profile {ProfileId}",
                patterns.Count, profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load patterns for a profile
    /// </summary>
    public async Task<List<DecisionPattern>> LoadPatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var filePath = GetPatternsFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            return new List<DecisionPattern>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var patterns = JsonSerializer.Deserialize<List<DecisionPattern>>(json, _jsonOptions);
            return patterns ?? new List<DecisionPattern>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load patterns for profile {ProfileId}", profileId);
            return new List<DecisionPattern>();
        }
    }

    #endregion

    #region Insight Operations

    /// <summary>
    /// Save insights for a profile
    /// </summary>
    public async Task SaveInsightsAsync(
        string profileId,
        List<LearningInsight> insights,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetInsightsFilePath(profileId);
            var json = JsonSerializer.Serialize(insights, _jsonOptions);
            
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved {Count} insights for profile {ProfileId}",
                insights.Count, profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load insights for a profile
    /// </summary>
    public async Task<List<LearningInsight>> LoadInsightsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var filePath = GetInsightsFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            return new List<LearningInsight>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var insights = JsonSerializer.Deserialize<List<LearningInsight>>(json, _jsonOptions);
            return insights ?? new List<LearningInsight>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load insights for profile {ProfileId}", profileId);
            return new List<LearningInsight>();
        }
    }

    #endregion

    #region Inferred Preference Operations

    /// <summary>
    /// Save inferred preferences for a profile
    /// </summary>
    public async Task SaveInferredPreferencesAsync(
        string profileId,
        List<InferredPreference> preferences,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetInferredPreferencesFilePath(profileId);
            var json = JsonSerializer.Serialize(preferences, _jsonOptions);
            
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved {Count} inferred preferences for profile {ProfileId}",
                preferences.Count, profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load inferred preferences for a profile
    /// </summary>
    public async Task<List<InferredPreference>> LoadInferredPreferencesAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var filePath = GetInferredPreferencesFilePath(profileId);
        
        if (!File.Exists(filePath))
        {
            return new List<InferredPreference>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var preferences = JsonSerializer.Deserialize<List<InferredPreference>>(json, _jsonOptions);
            return preferences ?? new List<InferredPreference>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load inferred preferences for profile {ProfileId}", profileId);
            return new List<InferredPreference>();
        }
    }

    /// <summary>
    /// Update a single inferred preference (e.g., when confirmed)
    /// </summary>
    public async Task UpdateInferredPreferenceAsync(
        string profileId,
        InferredPreference updatedPreference,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var preferences = await LoadInferredPreferencesAsync(profileId, ct).ConfigureAwait(false);
            
            // Find and update the preference
            var index = preferences.FindIndex(p => p.PreferenceId == updatedPreference.PreferenceId);
            if (index >= 0)
            {
                preferences[index] = updatedPreference;
            }
            else
            {
                preferences.Add(updatedPreference);
            }
            
            await SaveInferredPreferencesAsync(profileId, preferences, ct).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    #endregion

    #region Reset Operations

    /// <summary>
    /// Reset all learning data for a profile
    /// </summary>
    public async Task ResetLearningAsync(string profileId, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var patternsPath = GetPatternsFilePath(profileId);
            if (File.Exists(patternsPath))
            {
                File.Delete(patternsPath);
            }
            
            var insightsPath = GetInsightsFilePath(profileId);
            if (File.Exists(insightsPath))
            {
                File.Delete(insightsPath);
            }
            
            var preferencesPath = GetInferredPreferencesFilePath(profileId);
            if (File.Exists(preferencesPath))
            {
                File.Delete(preferencesPath);
            }
            
            _logger.LogInformation("Reset learning data for profile {ProfileId}", profileId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    #endregion

    #region Helper Methods

    private string GetPatternsFilePath(string profileId)
    {
        return Path.Combine(_patternsDirectory, $"{profileId}.json");
    }

    private string GetInsightsFilePath(string profileId)
    {
        return Path.Combine(_insightsDirectory, $"{profileId}.json");
    }

    private string GetInferredPreferencesFilePath(string profileId)
    {
        return Path.Combine(_preferencesDirectory, $"{profileId}.json");
    }

    #endregion
}
