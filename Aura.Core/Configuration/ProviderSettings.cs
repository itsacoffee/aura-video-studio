using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Manages provider configuration settings (paths, URLs, etc.)
/// Loads from JSON files in AppData folder
/// </summary>
public class ProviderSettings
{
    private readonly ILogger<ProviderSettings> _logger;
    private readonly string _configPath;
    private Dictionary<string, object>? _settings;

    public ProviderSettings(ILogger<ProviderSettings> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura",
            "provider-paths.json");
    }

    /// <summary>
    /// Check if portable mode is enabled
    /// </summary>
    public bool IsPortableModeEnabled()
    {
        LoadSettings();
        if (_settings != null && _settings.TryGetValue("portableModeEnabled", out var value))
        {
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get the portable root path (where all tools are installed in portable mode)
    /// </summary>
    public string? GetPortableRootPath()
    {
        LoadSettings();
        var path = GetStringSetting("portableRootPath", "");
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    /// <summary>
    /// Set portable mode configuration
    /// </summary>
    public void SetPortableMode(bool enabled, string? portableRootPath = null)
    {
        LoadSettings();
        if (_settings == null)
        {
            _settings = new Dictionary<string, object>();
        }

        _settings["portableModeEnabled"] = enabled;
        if (!string.IsNullOrWhiteSpace(portableRootPath))
        {
            _settings["portableRootPath"] = portableRootPath;
        }
        else if (!enabled)
        {
            // Remove portableRootPath if disabling portable mode
            _settings.Remove("portableRootPath");
        }

        SaveSettings();
        // Force reload to ensure changes are reflected
        _settings = null;
    }

    /// <summary>
    /// Get the effective tools directory (portable root or AppData)
    /// </summary>
    public string GetToolsDirectory()
    {
        if (IsPortableModeEnabled())
        {
            var portableRoot = GetPortableRootPath();
            if (!string.IsNullOrWhiteSpace(portableRoot))
            {
                return portableRoot;
            }
        }

        // Default to AppData
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura",
            "dependencies");
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
    /// Get Ollama URL
    /// </summary>
    public string GetOllamaUrl()
    {
        LoadSettings();
        return GetStringSetting("ollamaUrl", "http://127.0.0.1:11434");
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
        
        // If empty, use default Videos folder
        if (string.IsNullOrWhiteSpace(path))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "AuraVideoStudio");
        }
        
        return path;
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
    /// Get PlayHT API key
    /// </summary>
    public string? GetPlayHTApiKey()
    {
        LoadSettings();
        return GetStringSetting("playHTApiKey", "");
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

    /// <summary>
    /// Reload settings from disk (useful after changes)
    /// </summary>
    public void Reload()
    {
        _settings = null;
        LoadSettings();
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
