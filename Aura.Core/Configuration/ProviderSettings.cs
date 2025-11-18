using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Manages provider configuration settings (paths, URLs, etc.)
/// Portable-only: All data stored relative to application root
/// </summary>
public class ProviderSettings
{
    private readonly ILogger<ProviderSettings> _logger;
    private readonly string _configPath;
    private readonly string _portableRoot;
    private Dictionary<string, object>? _settings;

    public ProviderSettings(ILogger<ProviderSettings> logger)
    {
        _logger = logger;
        
        var envDataRoot = AuraEnvironmentPaths.TryGetDataRootFromEnvironment();
        if (!string.IsNullOrWhiteSpace(envDataRoot))
        {
            _portableRoot = AuraEnvironmentPaths.EnsureDirectory(envDataRoot);
        }
        else
        {
            // Determine portable root from assembly location
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            _portableRoot = AuraEnvironmentPaths.EnsureDirectory(DeterminePortableRoot(assemblyDir ?? Directory.GetCurrentDirectory()));
        }
        
        // Store settings in AuraData subfolder
        var auraDataDir = Path.Combine(_portableRoot, "AuraData");
        if (!Directory.Exists(auraDataDir))
        {
            Directory.CreateDirectory(auraDataDir);
        }
        
        _configPath = Path.Combine(auraDataDir, "settings.json");
        
        _logger.LogInformation("ProviderSettings initialized with portable root: {PortableRoot}", _portableRoot);
    }
    
    private static string DeterminePortableRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
        // Check if we're in a bin folder (development scenario)
        if (current.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) || 
            current.Parent?.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Go up to project root
            while (current != null && current.Name != "Aura.Api" && current.Name != "Aura.Core")
            {
                current = current.Parent;
            }
            
            // If we found project folder, go up one more to solution root
            if (current != null && (current.Name == "Aura.Api" || current.Name == "Aura.Core"))
            {
                current = current.Parent;
            }
        }
        
        return current?.FullName ?? startPath;
    }

    /// <summary>
    /// Get the portable root path (application root directory)
    /// </summary>
    public string GetPortableRootPath()
    {
        return _portableRoot;
    }

    /// <summary>
    /// Get the tools directory (where dependencies are installed)
    /// </summary>
    public string GetToolsDirectory()
    {
        var toolsDir = Path.Combine(_portableRoot, "Tools");
        if (!Directory.Exists(toolsDir))
        {
            Directory.CreateDirectory(toolsDir);
        }
        return toolsDir;
    }
    
    /// <summary>
    /// Get the AuraData directory (for settings, manifests, logs)
    /// </summary>
    public string GetAuraDataDirectory()
    {
        var auraDataDir = Path.Combine(_portableRoot, "AuraData");
        if (!Directory.Exists(auraDataDir))
        {
            Directory.CreateDirectory(auraDataDir);
        }
        return auraDataDir;
    }
    
    /// <summary>
    /// Get the downloads directory (for in-progress downloads)
    /// </summary>
    public string GetDownloadsDirectory()
    {
        var downloadsDir = Path.Combine(_portableRoot, "Downloads");
        if (!Directory.Exists(downloadsDir))
        {
            Directory.CreateDirectory(downloadsDir);
        }
        return downloadsDir;
    }
    
    /// <summary>
    /// Get the logs directory
    /// </summary>
    public string GetLogsDirectory()
    {
        var logsDir = Path.Combine(_portableRoot, "Logs");
        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }
        return logsDir;
    }
    
    /// <summary>
    /// Get the projects directory (for user projects)
    /// </summary>
    public string GetProjectsDirectory()
    {
        var projectsDir = Path.Combine(_portableRoot, "Projects");
        if (!Directory.Exists(projectsDir))
        {
            Directory.CreateDirectory(projectsDir);
        }
        return projectsDir;
    }

    /// <summary>
    /// Get Stable Diffusion WebUI URL
    /// </summary>
    public string GetStableDiffusionUrl()
    {
        LoadSettings();
        return GetStringSetting("stableDiffusionUrl", "http://127.0.0.1:7860");
    }

    /// <summary>
    /// Set Stable Diffusion WebUI URL
    /// </summary>
    public void SetStableDiffusionUrl(string url)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["stableDiffusionUrl"] = url;
        SaveSettings();
    }

    /// <summary>
    /// Stable Diffusion WebUI URL (for offline availability check)
    /// </summary>
    public string? StableDiffusionWebUiUrl => GetStableDiffusionUrl();

    /// <summary>
    /// Get Ollama URL
    /// </summary>
    public string GetOllamaUrl()
    {
        LoadSettings();
        return GetStringSetting("ollamaUrl", "http://127.0.0.1:11434");
    }

    /// <summary>
    /// Set Ollama URL
    /// </summary>
    public void SetOllamaUrl(string url)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["ollamaUrl"] = url;
        SaveSettings();
    }

    /// <summary>
    /// Ollama base URL (for offline availability check)
    /// </summary>
    public string? OllamaBaseUrl => GetOllamaUrl();

    /// <summary>
    /// Get Ollama model name
    /// </summary>
    public string GetOllamaModel()
    {
        LoadSettings();
        return GetStringSetting("ollamaModel", "llama3.1:8b-q4_k_m");
    }

    /// <summary>
    /// Set Ollama model name
    /// </summary>
    public void SetOllamaModel(string modelName)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["ollamaModel"] = modelName;
        SaveSettings();
    }

    /// <summary>
    /// Get Ollama executable path
    /// </summary>
    public string GetOllamaExecutablePath()
    {
        LoadSettings();
        var path = GetStringSetting("ollamaExecutablePath", "");
        
        // If empty, try to find in common locations
        if (string.IsNullOrWhiteSpace(path))
        {
            return Aura.Core.Services.OllamaService.FindOllamaExecutable() ?? "";
        }
        
        return path;
    }

    /// <summary>
    /// Set Ollama executable path
    /// </summary>
    public void SetOllamaExecutablePath(string path)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["ollamaExecutablePath"] = path;
        SaveSettings();
    }

    /// <summary>
    /// Get FFmpeg executable path
    /// </summary>
    public string GetFfmpegPath()
    {
        LoadSettings();
        var path = GetStringSetting("ffmpegPath", "");
        
        // If empty, try to find in common locations or use system PATH
        if (string.IsNullOrWhiteSpace(path))
        {
            // Default to system PATH
            return "ffmpeg";
        }
        
        return path;
    }

    /// <summary>
    /// Get FFprobe executable path
    /// </summary>
    public string GetFfprobePath()
    {
        LoadSettings();
        var path = GetStringSetting("ffprobePath", "");
        
        // If empty, try to find in common locations or use system PATH
        if (string.IsNullOrWhiteSpace(path))
        {
            // Default to system PATH
            return "ffprobe";
        }
        
        return path;
    }

    /// <summary>
    /// Get output directory for rendered videos
    /// </summary>
    public string GetOutputDirectory()
    {
        LoadSettings();
        var path = GetStringSetting("outputDirectory", "");
        
        // If user has set custom path, use it; otherwise use Projects folder
        if (string.IsNullOrWhiteSpace(path))
        {
            return GetProjectsDirectory();
        }
        
        return path;
    }

    /// <summary>
    /// Get OpenAI API key
    /// </summary>
    public string? GetOpenAiApiKey()
    {
        LoadSettings();
        return GetStringSetting("openAiApiKey", "");
    }

    /// <summary>
    /// Get OpenAI endpoint URL
    /// </summary>
    public string GetOpenAiEndpoint()
    {
        LoadSettings();
        return GetStringSetting("openAiEndpoint", "https://api.openai.com/v1");
    }

    /// <summary>
    /// Set OpenAI API key
    /// </summary>
    public void SetOpenAiKey(string apiKey)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["openAiApiKey"] = apiKey;
        SaveSettings();
    }

    /// <summary>
    /// Set OpenAI endpoint URL
    /// </summary>
    public void SetOpenAiEndpoint(string endpoint)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["openAiEndpoint"] = endpoint;
        SaveSettings();
    }

    /// <summary>
    /// Get Azure OpenAI API key
    /// </summary>
    public string? GetAzureOpenAiApiKey()
    {
        LoadSettings();
        return GetStringSetting("azureOpenAiApiKey", "");
    }

    /// <summary>
    /// Get Azure OpenAI endpoint URL
    /// </summary>
    public string? GetAzureOpenAiEndpoint()
    {
        LoadSettings();
        return GetStringSetting("azureOpenAiEndpoint", "");
    }

    /// <summary>
    /// Get Gemini API key
    /// </summary>
    public string? GetGeminiApiKey()
    {
        LoadSettings();
        return GetStringSetting("geminiApiKey", "");
    }

    /// <summary>
    /// Set Gemini API key
    /// </summary>
    public void SetGeminiKey(string apiKey)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["geminiApiKey"] = apiKey;
        SaveSettings();
    }

    /// <summary>
    /// Get Anthropic API key
    /// </summary>
    public string? GetAnthropicKey()
    {
        LoadSettings();
        return GetStringSetting("anthropicApiKey", "");
    }

    /// <summary>
    /// Set Anthropic API key
    /// </summary>
    public void SetAnthropicKey(string apiKey)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["anthropicApiKey"] = apiKey;
        SaveSettings();
    }

    /// <summary>
    /// Get ElevenLabs API key
    /// </summary>
    public string? GetElevenLabsApiKey()
    {
        LoadSettings();
        return GetStringSetting("elevenLabsApiKey", "");
    }

    /// <summary>
    /// Set ElevenLabs API key
    /// </summary>
    public void SetElevenLabsKey(string apiKey)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }
        _settings["elevenLabsApiKey"] = apiKey;
        SaveSettings();
    }

    /// <summary>
    /// Get PlayHT API key
    /// </summary>
    public string? GetPlayHTApiKey()
    {
        LoadSettings();
        return GetStringSetting("playHTApiKey", "");
    }

    /// <summary>
    /// Get Pexels API key
    /// </summary>
    public string? GetPexelsApiKey()
    {
        LoadSettings();
        return GetStringSetting("pexelsApiKey", "");
    }

    /// <summary>
    /// Get Unsplash access key
    /// </summary>
    public string? GetUnsplashAccessKey()
    {
        LoadSettings();
        return GetStringSetting("unsplashAccessKey", "");
    }

    /// <summary>
    /// Get Pixabay API key
    /// </summary>
    public string? GetPixabayApiKey()
    {
        LoadSettings();
        return GetStringSetting("pixabayApiKey", "");
    }

    /// <summary>
    /// Get PlayHT User ID
    /// </summary>
    public string? GetPlayHTUserId()
    {
        LoadSettings();
        return GetStringSetting("playHTUserId", "");
    }

    /// <summary>
    /// Get Azure Speech API key
    /// </summary>
    public string? GetAzureSpeechKey()
    {
        LoadSettings();
        return GetStringSetting("azureSpeechKey", "");
    }

    /// <summary>
    /// Get Azure Speech region (e.g., "eastus", "westeurope")
    /// </summary>
    public string GetAzureSpeechRegion()
    {
        LoadSettings();
        return GetStringSetting("azureSpeechRegion", "eastus");
    }

    /// <summary>
    /// Get default Azure voice
    /// </summary>
    public string GetAzureDefaultVoice()
    {
        LoadSettings();
        return GetStringSetting("azureDefaultVoice", "en-US-JennyNeural");
    }

    /// <summary>
    /// Get default Azure speaking style
    /// </summary>
    public string? GetAzureDefaultStyle()
    {
        LoadSettings();
        return GetStringSetting("azureDefaultStyle", "");
    }

    /// <summary>
    /// Get default Azure speaking rate
    /// </summary>
    public double GetAzureDefaultRate()
    {
        LoadSettings();
        return GetDoubleSetting("azureDefaultRate", 0.0);
    }

    /// <summary>
    /// Get Piper TTS executable path
    /// </summary>
    public string? PiperExecutablePath
    {
        get
        {
            LoadSettings();
            return GetStringSetting("piperExecutablePath", "");
        }
    }

    /// <summary>
    /// Get Piper TTS voice model path
    /// </summary>
    public string? PiperVoiceModelPath
    {
        get
        {
            LoadSettings();
            return GetStringSetting("piperVoiceModelPath", "");
        }
    }

    /// <summary>
    /// Get Mimic3 TTS server base URL
    /// </summary>
    public string? Mimic3BaseUrl
    {
        get
        {
            LoadSettings();
            return GetStringSetting("mimic3BaseUrl", "http://127.0.0.1:59125");
        }
    }

    /// <summary>
    /// Set Azure Speech configuration
    /// </summary>
    public void SetAzureSpeechConfig(string? apiKey, string? region, string? defaultVoice = null, string? defaultStyle = null, double? defaultRate = null)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }

        if (apiKey != null)
        {
            _settings["azureSpeechKey"] = apiKey;
        }
        if (region != null)
        {
            _settings["azureSpeechRegion"] = region;
        }
        if (defaultVoice != null)
        {
            _settings["azureDefaultVoice"] = defaultVoice;
        }
        if (defaultStyle != null)
        {
            _settings["azureDefaultStyle"] = defaultStyle;
        }
        if (defaultRate.HasValue)
        {
            _settings["azureDefaultRate"] = defaultRate.Value;
        }

        SaveSettings();
    }

    /// <summary>
    /// Check if offline-only mode is enabled
    /// </summary>
    public bool IsOfflineOnly()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("offlineOnly", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if pacing optimization is enabled
    /// </summary>
    public bool GetEnablePacingOptimization()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enablePacingOptimization", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Get pacing optimization level
    /// </summary>
    public string GetPacingOptimizationLevel()
    {
        LoadSettings();
        return GetStringSetting("pacingOptimizationLevel", "Balanced");
    }

    /// <summary>
    /// Check if auto-apply pacing suggestions is enabled
    /// </summary>
    public bool GetAutoApplyPacingSuggestions()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("autoApplyPacingSuggestions", out var value))
        {
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True)
                    return true;
                if (jsonElement.ValueKind == JsonValueKind.False)
                    return false;
            }
        }
        return true; // Default: enabled if pacing is enabled
    }

    /// <summary>
    /// Get minimum confidence threshold for pacing suggestions (0-100)
    /// </summary>
    public int GetMinimumConfidenceThreshold()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("minimumConfidenceThreshold", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.TryGetInt32(out var intValue))
            {
                return Math.Clamp(intValue, 0, 100);
            }
        }
        return 70; // Default: 70%
    }

    /// <summary>
    /// Set pacing optimization configuration
    /// </summary>
    public void SetPacingOptimizationConfig(
        bool? enabled = null,
        string? level = null,
        bool? autoApply = null,
        int? minimumConfidenceThreshold = null)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }

        if (enabled.HasValue)
        {
            _settings["enablePacingOptimization"] = enabled.Value;
        }
        if (level != null)
        {
            _settings["pacingOptimizationLevel"] = level;
        }
        if (autoApply.HasValue)
        {
            _settings["autoApplyPacingSuggestions"] = autoApply.Value;
        }
        if (minimumConfidenceThreshold.HasValue)
        {
            _settings["minimumConfidenceThreshold"] = Math.Clamp(minimumConfidenceThreshold.Value, 0, 100);
        }

        SaveSettings();
    }

    /// <summary>
    /// Check if provider recommendations are enabled (OFF by default - opt-in)
    /// </summary>
    public bool GetEnableRecommendations()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableRecommendations", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled (opt-in model)
    }

    /// <summary>
    /// Get assistance level for recommendations
    /// </summary>
    public string GetAssistanceLevel()
    {
        LoadSettings();
        return GetStringSetting("assistanceLevel", "Off");
    }

    /// <summary>
    /// Check if health monitoring is enabled (OFF by default)
    /// </summary>
    public bool GetEnableHealthMonitoring()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableHealthMonitoring", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Check if cost tracking is enabled (OFF by default)
    /// </summary>
    public bool GetEnableCostTracking()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableCostTracking", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Check if preference learning is enabled (OFF by default)
    /// </summary>
    public bool GetEnableLearning()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableLearning", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Check if provider profiles are enabled (OFF by default)
    /// </summary>
    public bool GetEnableProfiles()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableProfiles", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Check if automatic fallback is enabled (OFF by default)
    /// </summary>
    public bool GetEnableAutoFallback()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("enableAutoFallback", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false; // Default: disabled
    }

    /// <summary>
    /// Set provider recommendation preferences
    /// </summary>
    public void SetRecommendationPreferences(
        bool? enableRecommendations = null,
        string? assistanceLevel = null,
        bool? enableHealthMonitoring = null,
        bool? enableCostTracking = null,
        bool? enableLearning = null,
        bool? enableProfiles = null,
        bool? enableAutoFallback = null)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }

        if (enableRecommendations.HasValue)
        {
            _settings["enableRecommendations"] = enableRecommendations.Value;
        }
        if (assistanceLevel != null)
        {
            _settings["assistanceLevel"] = assistanceLevel;
        }
        if (enableHealthMonitoring.HasValue)
        {
            _settings["enableHealthMonitoring"] = enableHealthMonitoring.Value;
        }
        if (enableCostTracking.HasValue)
        {
            _settings["enableCostTracking"] = enableCostTracking.Value;
        }
        if (enableLearning.HasValue)
        {
            _settings["enableLearning"] = enableLearning.Value;
        }
        if (enableProfiles.HasValue)
        {
            _settings["enableProfiles"] = enableProfiles.Value;
        }
        if (enableAutoFallback.HasValue)
        {
            _settings["enableAutoFallback"] = enableAutoFallback.Value;
        }

        SaveSettings();
    }

    private void LoadSettings()
    {
        if (_settings != null)
        {
            return; // Already loaded
        }

        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                _logger.LogInformation("Loaded provider settings from {Path}", _configPath);
            }
            else
            {
                _settings = new Dictionary<string, object>();
                _logger.LogInformation("Provider settings file not found, using defaults");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load provider settings, using defaults");
            _settings = new Dictionary<string, object>();
        }
    }

    private string GetStringSetting(string key, string defaultValue)
    {
        if (_settings == null || !_settings.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.GetString() ?? defaultValue;
        }

        return value?.ToString() ?? defaultValue;
    }

    private double GetDoubleSetting(string key, double defaultValue)
    {
        if (_settings == null || !_settings.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.TryGetDouble(out var doubleValue))
            {
                return doubleValue;
            }
        }

        if (double.TryParse(value?.ToString(), out var parsedValue))
        {
            return parsedValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Reload settings from disk (useful after changes)
    /// </summary>
    public void Reload()
    {
        _settings = null;
        LoadSettings();
    }

    /// <summary>
    /// Update settings asynchronously with thread-safe mutation
    /// </summary>
    /// <param name="updateAction">Action to modify settings</param>
    /// <param name="ct">Cancellation token</param>
    public async Task UpdateAsync(Action<ProviderSettings> updateAction, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            lock (_settings ?? new Dictionary<string, object>())
            {
                LoadSettings();
                updateAction(this);
            }
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate if an API key is present and has a reasonable format
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if the API key appears valid, false otherwise</returns>
    public static bool IsValidApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // Basic validation: API keys should be at least 20 characters
        // and contain alphanumeric characters
        return apiKey.Length >= 20 && apiKey.Any(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Get and validate an API key from settings
    /// </summary>
    /// <param name="key">The settings key for the API key</param>
    /// <param name="providerName">The name of the provider (for error messages)</param>
    /// <returns>The API key if valid</returns>
    /// <exception cref="InvalidOperationException">Thrown if the API key is missing or invalid</exception>
    public string GetApiKey(string key, string providerName)
    {
        LoadSettings();
        var apiKey = GetStringSetting(key, "");
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"{providerName} API key is not configured. Please add your API key in Settings → Providers → {providerName}");
        }

        if (!IsValidApiKey(apiKey))
        {
            throw new InvalidOperationException(
                $"{providerName} API key appears to be invalid. Please check your API key in Settings → Providers → {providerName}");
        }

        return apiKey;
    }

    /// <summary>
    /// Validate Azure OpenAI endpoint URL format
    /// </summary>
    /// <param name="endpoint">The endpoint URL to validate</param>
    /// <returns>True if the endpoint format is valid, false otherwise</returns>
    public static bool IsValidAzureEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return false;
        }

        // Azure OpenAI endpoints should be HTTPS and contain openai.azure.com
        return endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
               endpoint.Contains("openai.azure.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get Piper TTS executable path
    /// </summary>
    public string? GetPiperPath()
    {
        LoadSettings();
        var result = GetStringSetting("piperPath", string.Empty);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>
    /// Get Mimic3 service URL
    /// </summary>
    public string? GetMimic3Url()
    {
        LoadSettings();
        var result = GetStringSetting("mimic3Url", string.Empty);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
            _logger.LogInformation("Saved provider settings to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save provider settings");
            throw;
        }
    }
}
