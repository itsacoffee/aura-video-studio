using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing provider profiles and applying them to the system
/// </summary>
public class ProviderProfileService
{
    private readonly ILogger<ProviderProfileService> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly HttpClient _httpClient;
    private readonly string _profilesPath;

    public ProviderProfileService(
        ILogger<ProviderProfileService> logger,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        HttpClient httpClient)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _httpClient = httpClient;
        
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _profilesPath = Path.Combine(auraDataDir, "provider-profiles.json");
    }

    /// <summary>
    /// Get all available provider profiles (built-in + custom)
    /// </summary>
    public async Task<List<ProviderProfile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        var profiles = new List<ProviderProfile>
        {
            ProviderProfile.FreeOnly,
            ProviderProfile.BalancedMix,
            ProviderProfile.ProMax
        };

        var customProfiles = await LoadCustomProfilesAsync(ct);
        profiles.AddRange(customProfiles);

        return profiles;
    }

    /// <summary>
    /// Get the currently active profile
    /// </summary>
    public async Task<ProviderProfile> GetActiveProfileAsync(CancellationToken ct = default)
    {
        var config = await LoadConfigAsync(ct);
        var allProfiles = await GetAllProfilesAsync(ct);
        
        return allProfiles.FirstOrDefault(p => p.Id == config.ActiveProfile) 
               ?? ProviderProfile.FreeOnly;
    }

    /// <summary>
    /// Set the active provider profile
    /// </summary>
    public async Task<bool> SetActiveProfileAsync(string profileId, CancellationToken ct = default)
    {
        var allProfiles = await GetAllProfilesAsync(ct);
        var profile = allProfiles.FirstOrDefault(p => p.Id == profileId);
        
        if (profile == null)
        {
            _logger.LogWarning("Profile {ProfileId} not found", profileId);
            return false;
        }

        var config = await LoadConfigAsync(ct);
        config.ActiveProfile = profileId;
        await SaveConfigAsync(config, ct);

        _logger.LogInformation("Active profile changed to {ProfileName} ({ProfileId})", 
            profile.Name, profileId);

        return true;
    }

    /// <summary>
    /// Validate a profile by checking if all required API keys and engines are available
    /// </summary>
    public async Task<ProfileValidationResult> ValidateProfileAsync(
        string profileId, 
        CancellationToken ct = default)
    {
        var allProfiles = await GetAllProfilesAsync(ct);
        var profile = allProfiles.FirstOrDefault(p => p.Id == profileId);
        
        if (profile == null)
        {
            return new ProfileValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Profile {profileId} not found" },
                Message = "Profile not found"
            };
        }

        var result = new ProfileValidationResult { IsValid = true };
        var allKeys = _keyStore.GetAllKeys();

        foreach (var requiredKey in profile.RequiredApiKeys)
        {
            if (!allKeys.ContainsKey(requiredKey) || string.IsNullOrWhiteSpace(allKeys[requiredKey]))
            {
                result.IsValid = false;
                result.MissingKeys.Add(requiredKey);
                result.Errors.Add($"Missing required API key: {requiredKey}");
            }
        }

        var engineStatus = await CheckEngineStatusAsync(profile, ct);
        if (!engineStatus.AllEnginesAvailable)
        {
            result.IsValid = false;
            result.Errors.AddRange(engineStatus.MissingEngines);
            
            if (engineStatus.OfflineProvidersNeeded)
            {
                result.Warnings.Add("This profile uses offline providers. Ensure local engines (Ollama, FFmpeg) are installed.");
            }
        }

        if (result.IsValid)
        {
            result.Message = "Profile is valid and ready to use";
        }
        else
        {
            var parts = new List<string>();
            if (result.MissingKeys.Count > 0)
            {
                parts.Add($"{result.MissingKeys.Count} API key(s) missing");
            }
            if (!engineStatus.AllEnginesAvailable)
            {
                parts.Add("some engines unavailable");
            }
            result.Message = $"Profile requires: {string.Join(", ", parts)}";
        }

        return result;
    }

    private async Task<EngineStatusResult> CheckEngineStatusAsync(ProviderProfile profile, CancellationToken ct)
    {
        var result = new EngineStatusResult { AllEnginesAvailable = true };
        var stages = profile.Stages;

        if (stages.ContainsKey("Script") && (stages["Script"].Contains("Ollama") || stages["Script"] == "Free"))
        {
            result.OfflineProvidersNeeded = true;
            var ollamaAvailable = await CheckOllamaAsync(ct);
            if (!ollamaAvailable)
            {
                result.AllEnginesAvailable = false;
                result.MissingEngines.Add("Ollama is not running (required for local LLM)");
            }
        }

        if (stages.ContainsKey("Visuals") && stages["Visuals"].Contains("Local"))
        {
            result.OfflineProvidersNeeded = true;
        }

        var ffmpegAvailable = await CheckFFmpegAsync(ct);
        if (!ffmpegAvailable)
        {
            result.AllEnginesAvailable = false;
            result.MissingEngines.Add("FFmpeg is not available (required for video rendering)");
        }

        return result;
    }

    private async Task<bool> CheckOllamaAsync(CancellationToken ct)
    {
        try
        {
            var ollamaUrl = "http://127.0.0.1:11434/api/tags";
            var response = await _httpClient.GetAsync(ollamaUrl, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckFFmpegAsync(CancellationToken ct)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get provider recommendation based on available API keys and hardware
    /// </summary>
    public async Task<ProviderProfile> GetRecommendedProfileAsync(CancellationToken ct = default)
    {
        var allKeys = _keyStore.GetAllKeys();
        var hasOpenAI = allKeys.ContainsKey("openai") && !string.IsNullOrWhiteSpace(allKeys["openai"]);
        var hasElevenLabs = allKeys.ContainsKey("elevenlabs") && !string.IsNullOrWhiteSpace(allKeys["elevenlabs"]);
        var hasStability = allKeys.ContainsKey("stabilityai") && !string.IsNullOrWhiteSpace(allKeys["stabilityai"]);

        if (hasOpenAI && hasElevenLabs && hasStability)
        {
            _logger.LogInformation("Recommending Pro-Max profile (all premium keys available)");
            return ProviderProfile.ProMax;
        }

        if (hasOpenAI)
        {
            _logger.LogInformation("Recommending Balanced Mix profile (OpenAI key available)");
            return ProviderProfile.BalancedMix;
        }

        _logger.LogInformation("Recommending Free-Only profile (no premium keys available)");
        return ProviderProfile.FreeOnly;
    }

    private async Task<List<ProviderProfile>> LoadCustomProfilesAsync(CancellationToken ct)
    {
        if (!File.Exists(_profilesPath))
        {
            return new List<ProviderProfile>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_profilesPath, ct);
            var profiles = JsonSerializer.Deserialize<List<ProviderProfile>>(json);
            return profiles ?? new List<ProviderProfile>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom profiles");
            return new List<ProviderProfile>();
        }
    }

    private async Task<ProviderMixingConfig> LoadConfigAsync(CancellationToken ct)
    {
        var configPath = Path.Combine(_providerSettings.GetAuraDataDirectory(), "provider-config.json");
        
        if (!File.Exists(configPath))
        {
            return new ProviderMixingConfig { ActiveProfile = "free-only" };
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath, ct);
            var config = JsonSerializer.Deserialize<ProviderMixingConfig>(json);
            return config ?? new ProviderMixingConfig { ActiveProfile = "free-only" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load provider config");
            return new ProviderMixingConfig { ActiveProfile = "free-only" };
        }
    }

    private async Task SaveConfigAsync(ProviderMixingConfig config, CancellationToken ct)
    {
        var configPath = Path.Combine(_providerSettings.GetAuraDataDirectory(), "provider-config.json");
        var directory = Path.GetDirectoryName(configPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        await File.WriteAllTextAsync(configPath, json, ct);
    }
}

/// <summary>
/// Result of profile validation
/// </summary>
public record ProfileValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> MissingKeys { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of engine status check
/// </summary>
internal record EngineStatusResult
{
    public bool AllEnginesAvailable { get; set; }
    public bool OfflineProvidersNeeded { get; set; }
    public List<string> MissingEngines { get; set; } = new();
}
