using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.UserPreferences;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.UserPreferences;

/// <summary>
/// Central service for managing user preferences and custom configurations
/// Provides unified access to all user-defined settings and overrides
/// </summary>
public class UserPreferencesService
{
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly string _preferencesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserPreferencesService(ILogger<UserPreferencesService> logger, string dataDirectory)
    {
        _logger = logger;
        _preferencesDirectory = Path.Combine(dataDirectory, "UserPreferences");
        
        if (!Directory.Exists(_preferencesDirectory))
        {
            Directory.CreateDirectory(_preferencesDirectory);
            _logger.LogInformation("Created user preferences directory: {Directory}", _preferencesDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    // Custom Audience Profiles
    public async Task<List<CustomAudienceProfile>> GetCustomAudienceProfilesAsync(CancellationToken ct = default)
    {
        return await LoadAllFromDirectoryAsync<CustomAudienceProfile>("AudienceProfiles", ct);
    }

    public async Task<CustomAudienceProfile?> GetCustomAudienceProfileAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<CustomAudienceProfile>("AudienceProfiles", id, ct);
    }

    public async Task<CustomAudienceProfile> SaveCustomAudienceProfileAsync(CustomAudienceProfile profile, CancellationToken ct = default)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("AudienceProfiles", profile.Id, profile, ct);
        _logger.LogInformation("Saved custom audience profile: {ProfileId} - {ProfileName}", profile.Id, profile.Name);
        return profile;
    }

    public async Task<bool> DeleteCustomAudienceProfileAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<CustomAudienceProfile>("AudienceProfiles", id, ct);
    }

    // Content Filtering Policies
    public async Task<List<ContentFilteringPolicy>> GetContentFilteringPoliciesAsync(CancellationToken ct = default)
    {
        return await LoadAllFromDirectoryAsync<ContentFilteringPolicy>("FilteringPolicies", ct);
    }

    public async Task<ContentFilteringPolicy?> GetContentFilteringPolicyAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<ContentFilteringPolicy>("FilteringPolicies", id, ct);
    }

    public async Task<ContentFilteringPolicy> SaveContentFilteringPolicyAsync(ContentFilteringPolicy policy, CancellationToken ct = default)
    {
        policy.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("FilteringPolicies", policy.Id, policy, ct);
        _logger.LogInformation("Saved content filtering policy: {PolicyId} - {PolicyName}", policy.Id, policy.Name);
        return policy;
    }

    public async Task<bool> DeleteContentFilteringPolicyAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<ContentFilteringPolicy>("FilteringPolicies", id, ct);
    }

    // AI Behavior Settings
    public async Task<List<AIBehaviorSettings>> GetAIBehaviorSettingsAsync(CancellationToken ct = default)
    {
        return await LoadAllFromDirectoryAsync<AIBehaviorSettings>("AIBehavior", ct);
    }

    public async Task<AIBehaviorSettings?> GetAIBehaviorSettingAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<AIBehaviorSettings>("AIBehavior", id, ct);
    }

    public async Task<AIBehaviorSettings> SaveAIBehaviorSettingsAsync(AIBehaviorSettings settings, CancellationToken ct = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("AIBehavior", settings.Id, settings, ct);
        _logger.LogInformation("Saved AI behavior settings: {SettingsId} - {SettingsName}", settings.Id, settings.Name);
        return settings;
    }

    public async Task<bool> DeleteAIBehaviorSettingsAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<AIBehaviorSettings>("AIBehavior", id, ct);
    }

    public async Task<AIBehaviorSettings> EnsureDefaultAIBehaviorSettingsAsync(CancellationToken ct = default)
    {
        var existingSettings = await GetAIBehaviorSettingsAsync(ct);
        var defaultSetting = existingSettings.FirstOrDefault(s => s.IsDefault);

        if (defaultSetting != null)
        {
            return defaultSetting;
        }

        _logger.LogInformation("Creating default AI behavior settings");

        var newDefault = new AIBehaviorSettings
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Default AI Behavior",
            Description = "Standard AI behavior settings for balanced performance",
            IsDefault = true,
            CreativityVsAdherence = 0.5,
            EnableChainOfThought = false,
            ShowPromptsBeforeSending = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ScriptGeneration = new LLMStageParameters
            {
                StageName = "ScriptGeneration",
                Temperature = 0.7,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.5
            },
            SceneDescription = new LLMStageParameters
            {
                StageName = "SceneDescription",
                Temperature = 0.7,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 1500,
                StrictnessLevel = 0.5
            },
            ContentOptimization = new LLMStageParameters
            {
                StageName = "ContentOptimization",
                Temperature = 0.5,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.7
            },
            Translation = new LLMStageParameters
            {
                StageName = "Translation",
                Temperature = 0.3,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 2000,
                StrictnessLevel = 0.8
            },
            QualityAnalysis = new LLMStageParameters
            {
                StageName = "QualityAnalysis",
                Temperature = 0.2,
                TopP = 0.9,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                MaxTokens = 1000,
                StrictnessLevel = 0.9
            }
        };

        return await SaveAIBehaviorSettingsAsync(newDefault, ct);
    }

    // Custom Prompt Templates
    public async Task<List<CustomPromptTemplate>> GetCustomPromptTemplatesAsync(string? stage = null, CancellationToken ct = default)
    {
        var templates = await LoadAllFromDirectoryAsync<CustomPromptTemplate>("PromptTemplates", ct);
        
        if (!string.IsNullOrEmpty(stage))
        {
            templates = templates.Where(t => t.Stage.Equals(stage, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return templates;
    }

    public async Task<CustomPromptTemplate?> GetCustomPromptTemplateAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<CustomPromptTemplate>("PromptTemplates", id, ct);
    }

    public async Task<CustomPromptTemplate> SaveCustomPromptTemplateAsync(CustomPromptTemplate template, CancellationToken ct = default)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("PromptTemplates", template.Id, template, ct);
        _logger.LogInformation("Saved custom prompt template: {TemplateId} - {TemplateName} for stage {Stage}", 
            template.Id, template.Name, template.Stage);
        return template;
    }

    public async Task<bool> DeleteCustomPromptTemplateAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<CustomPromptTemplate>("PromptTemplates", id, ct);
    }

    // Custom Quality Thresholds
    public async Task<List<CustomQualityThresholds>> GetCustomQualityThresholdsAsync(CancellationToken ct = default)
    {
        return await LoadAllFromDirectoryAsync<CustomQualityThresholds>("QualityThresholds", ct);
    }

    public async Task<CustomQualityThresholds?> GetCustomQualityThresholdAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<CustomQualityThresholds>("QualityThresholds", id, ct);
    }

    public async Task<CustomQualityThresholds> SaveCustomQualityThresholdsAsync(CustomQualityThresholds thresholds, CancellationToken ct = default)
    {
        thresholds.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("QualityThresholds", thresholds.Id, thresholds, ct);
        _logger.LogInformation("Saved custom quality thresholds: {ThresholdsId} - {ThresholdsName}", 
            thresholds.Id, thresholds.Name);
        return thresholds;
    }

    public async Task<bool> DeleteCustomQualityThresholdsAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<CustomQualityThresholds>("QualityThresholds", id, ct);
    }

    // Custom Visual Styles
    public async Task<List<CustomVisualStyle>> GetCustomVisualStylesAsync(CancellationToken ct = default)
    {
        return await LoadAllFromDirectoryAsync<CustomVisualStyle>("VisualStyles", ct);
    }

    public async Task<CustomVisualStyle?> GetCustomVisualStyleAsync(string id, CancellationToken ct = default)
    {
        return await LoadByIdAsync<CustomVisualStyle>("VisualStyles", id, ct);
    }

    public async Task<CustomVisualStyle> SaveCustomVisualStyleAsync(CustomVisualStyle style, CancellationToken ct = default)
    {
        style.UpdatedAt = DateTime.UtcNow;
        await SaveAsync("VisualStyles", style.Id, style, ct);
        _logger.LogInformation("Saved custom visual style: {StyleId} - {StyleName}", style.Id, style.Name);
        return style;
    }

    public async Task<bool> DeleteCustomVisualStyleAsync(string id, CancellationToken ct = default)
    {
        return await DeleteAsync<CustomVisualStyle>("VisualStyles", id, ct);
    }

    // Export/Import Functionality
    public async Task<string> ExportAllPreferencesAsync(CancellationToken ct = default)
    {
        var export = new
        {
            ExportDate = DateTime.UtcNow,
            Version = "1.0",
            AudienceProfiles = await GetCustomAudienceProfilesAsync(ct),
            FilteringPolicies = await GetContentFilteringPoliciesAsync(ct),
            AIBehaviorSettings = await GetAIBehaviorSettingsAsync(ct),
            PromptTemplates = await GetCustomPromptTemplatesAsync(ct: ct),
            QualityThresholds = await GetCustomQualityThresholdsAsync(ct),
            VisualStyles = await GetCustomVisualStylesAsync(ct)
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }

    public async Task ImportPreferencesAsync(string json, CancellationToken ct = default)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("AudienceProfiles", out var audienceProfiles))
        {
            foreach (var profile in audienceProfiles.EnumerateArray())
            {
                var audienceProfile = JsonSerializer.Deserialize<CustomAudienceProfile>(profile.GetRawText(), _jsonOptions);
                if (audienceProfile != null)
                {
                    await SaveCustomAudienceProfileAsync(audienceProfile, ct);
                }
            }
        }

        if (root.TryGetProperty("FilteringPolicies", out var filteringPolicies))
        {
            foreach (var policy in filteringPolicies.EnumerateArray())
            {
                var filteringPolicy = JsonSerializer.Deserialize<ContentFilteringPolicy>(policy.GetRawText(), _jsonOptions);
                if (filteringPolicy != null)
                {
                    await SaveContentFilteringPolicyAsync(filteringPolicy, ct);
                }
            }
        }

        _logger.LogInformation("Imported user preferences successfully");
    }

    // Private Helper Methods
    private async Task<List<T>> LoadAllFromDirectoryAsync<T>(string subdirectory, CancellationToken ct)
    {
        var directory = Path.Combine(_preferencesDirectory, subdirectory);
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            return new List<T>();
        }

        var items = new List<T>();
        var files = Directory.GetFiles(directory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var item = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading {Type} from {File}", typeof(T).Name, file);
            }
        }

        return items;
    }

    private async Task<T?> LoadByIdAsync<T>(string subdirectory, string id, CancellationToken ct) where T : class
    {
        var filePath = GetFilePath(subdirectory, id);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading {Type} with ID {Id}", typeof(T).Name, id);
            return null;
        }
    }

    private async Task SaveAsync<T>(string subdirectory, string id, T item, CancellationToken ct)
    {
        var directory = Path.Combine(_preferencesDirectory, subdirectory);
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var filePath = GetFilePath(subdirectory, id);
        var json = JsonSerializer.Serialize(item, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);
    }

    private async Task<bool> DeleteAsync<T>(string subdirectory, string id, CancellationToken ct)
    {
        var filePath = GetFilePath(subdirectory, id);
        
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            await Task.Run(() => File.Delete(filePath), ct);
            _logger.LogInformation("Deleted {Type} with ID {Id}", typeof(T).Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {Type} with ID {Id}", typeof(T).Name, id);
            return false;
        }
    }

    private string GetFilePath(string subdirectory, string id)
    {
        return Path.Combine(_preferencesDirectory, subdirectory, $"{id}.json");
    }
}
