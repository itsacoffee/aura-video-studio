using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Loads and manages provider timeout profiles from configuration
/// </summary>
public sealed class ProviderTimeoutProfileLoader
{
    private readonly ILogger<ProviderTimeoutProfileLoader> _logger;
    private readonly string _configPath;
    private ProviderTimeoutConfiguration? _configuration;

    public ProviderTimeoutProfileLoader(
        ILogger<ProviderTimeoutProfileLoader> logger,
        string? configPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configPath = configPath ?? Path.Combine(AppContext.BaseDirectory, "providerTimeoutProfiles.json");
    }

    /// <summary>
    /// Loads the provider timeout configuration from JSON
    /// </summary>
    public ProviderTimeoutConfiguration LoadConfiguration()
    {
        if (_configuration != null)
            return _configuration;

        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning(
                    "Provider timeout configuration not found at {Path}, using defaults",
                    _configPath);
                return GetDefaultConfiguration();
            }

            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<ProviderTimeoutConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (config == null)
            {
                _logger.LogWarning("Failed to parse provider timeout configuration, using defaults");
                return GetDefaultConfiguration();
            }

            _configuration = config;
            
            _logger.LogInformation(
                "Loaded provider timeout configuration with {ProfileCount} profiles and {PatternCount} patience profiles",
                config.Profiles.Count,
                config.PatienceProfiles.Count);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading provider timeout configuration from {Path}, using defaults: {Message}",
                _configPath,
                ex.Message);
            
            return GetDefaultConfiguration();
        }
    }

    /// <summary>
    /// Gets the timeout profile for a specific provider
    /// </summary>
    public ProviderTimeoutProfile GetProfileForProvider(string providerName, string? patienceProfile = null)
    {
        var config = LoadConfiguration();
        
        var providerType = ResolveProviderType(providerName, config);
        
        if (!config.Profiles.TryGetValue(providerType, out var profile))
        {
            _logger.LogWarning(
                "No timeout profile found for provider type {Type}, using local_llm defaults",
                providerType);
            profile = config.Profiles["local_llm"];
        }

        if (!string.IsNullOrEmpty(patienceProfile) && 
            config.PatienceProfiles.TryGetValue(patienceProfile, out var patienceSettings))
        {
            profile = ApplyPatienceMultiplier(profile, patienceSettings);
        }

        return profile;
    }

    /// <summary>
    /// Resolves the provider type from provider name
    /// </summary>
    private string ResolveProviderType(string providerName, ProviderTimeoutConfiguration config)
    {
        if (config.ProviderMapping.TryGetValue(providerName, out var mappedType))
        {
            return mappedType;
        }

        if (providerName.Contains("Ollama", StringComparison.OrdinalIgnoreCase))
            return "local_llm";
        
        if (providerName.Contains("OpenAI", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Anthropic", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Azure", StringComparison.OrdinalIgnoreCase))
            return "cloud_llm";

        if (providerName.Contains("ElevenLabs", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("PlayHT", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Piper", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("SAPI", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Mimic", StringComparison.OrdinalIgnoreCase))
            return "tts";

        if (providerName.Contains("Stable", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Diffusion", StringComparison.OrdinalIgnoreCase) ||
            providerName.Contains("Replicate", StringComparison.OrdinalIgnoreCase))
            return "image_gen";

        if (providerName.Contains("FFmpeg", StringComparison.OrdinalIgnoreCase))
            return "video_render";

        if (providerName.Contains("RuleBased", StringComparison.OrdinalIgnoreCase))
            return "fallback_provider";

        _logger.LogWarning(
            "Could not determine provider type for {Provider}, defaulting to local_llm",
            providerName);
        
        return "local_llm";
    }

    /// <summary>
    /// Applies patience profile multipliers to a timeout profile
    /// </summary>
    private ProviderTimeoutProfile ApplyPatienceMultiplier(
        ProviderTimeoutProfile baseProfile,
        PatienceProfile patienceSettings)
    {
        return new ProviderTimeoutProfile
        {
            NormalThresholdMs = (int)(baseProfile.NormalThresholdMs * patienceSettings.TimeoutMultiplier),
            ExtendedThresholdMs = (int)(baseProfile.ExtendedThresholdMs * patienceSettings.TimeoutMultiplier),
            DeepWaitThresholdMs = (int)(baseProfile.DeepWaitThresholdMs * patienceSettings.TimeoutMultiplier),
            HeartbeatIntervalMs = baseProfile.HeartbeatIntervalMs,
            StallSuspicionMultiplier = (int)(baseProfile.StallSuspicionMultiplier * patienceSettings.StallMultiplier),
            Description = $"{baseProfile.Description} ({patienceSettings.Label})"
        };
    }

    /// <summary>
    /// Gets default configuration when file is missing
    /// </summary>
    private ProviderTimeoutConfiguration GetDefaultConfiguration()
    {
        return new ProviderTimeoutConfiguration
        {
            Profiles = new Dictionary<string, ProviderTimeoutProfile>
            {
                ["local_llm"] = new()
                {
                    NormalThresholdMs = 30000,
                    ExtendedThresholdMs = 180000,
                    DeepWaitThresholdMs = 300000,
                    HeartbeatIntervalMs = 15000,
                    StallSuspicionMultiplier = 3,
                    Description = "Local LLM models (default)"
                },
                ["cloud_llm"] = new()
                {
                    NormalThresholdMs = 15000,
                    ExtendedThresholdMs = 60000,
                    DeepWaitThresholdMs = 120000,
                    HeartbeatIntervalMs = 5000,
                    StallSuspicionMultiplier = 4,
                    Description = "Cloud LLM APIs (default)"
                }
            },
            PatienceProfiles = new Dictionary<string, PatienceProfile>
            {
                ["balanced"] = new()
                {
                    Label = "Balanced (Default)",
                    TimeoutMultiplier = 1.0,
                    StallMultiplier = 1.0,
                    Description = "Standard patience windows"
                }
            },
            ProviderMapping = new Dictionary<string, string>()
        };
    }
}

/// <summary>
/// Root configuration for provider timeouts
/// </summary>
public sealed class ProviderTimeoutConfiguration
{
    [JsonPropertyName("profiles")]
    public Dictionary<string, ProviderTimeoutProfile> Profiles { get; set; } = new();

    [JsonPropertyName("patienceProfiles")]
    public Dictionary<string, PatienceProfile> PatienceProfiles { get; set; } = new();

    [JsonPropertyName("providerMapping")]
    public Dictionary<string, string> ProviderMapping { get; set; } = new();
}

/// <summary>
/// Timeout profile for a specific provider type
/// </summary>
public sealed class ProviderTimeoutProfile
{
    [JsonPropertyName("normalThresholdMs")]
    public int NormalThresholdMs { get; set; }

    [JsonPropertyName("extendedThresholdMs")]
    public int ExtendedThresholdMs { get; set; }

    [JsonPropertyName("deepWaitThresholdMs")]
    public int DeepWaitThresholdMs { get; set; }

    [JsonPropertyName("heartbeatIntervalMs")]
    public int HeartbeatIntervalMs { get; set; }

    [JsonPropertyName("stallSuspicionMultiplier")]
    public int StallSuspicionMultiplier { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets the stall threshold based on heartbeat interval and multiplier
    /// </summary>
    public TimeSpan GetStallThreshold()
    {
        return TimeSpan.FromMilliseconds(HeartbeatIntervalMs * StallSuspicionMultiplier);
    }
}

/// <summary>
/// Patience profile that modifies timeout thresholds
/// </summary>
public sealed class PatienceProfile
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("timeoutMultiplier")]
    public double TimeoutMultiplier { get; set; }

    [JsonPropertyName("stallMultiplier")]
    public double StallMultiplier { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("recommendedFor")]
    public string[] RecommendedFor { get; set; } = Array.Empty<string>();
}
