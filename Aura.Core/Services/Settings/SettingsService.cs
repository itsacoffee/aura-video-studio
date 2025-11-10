using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Settings;

/// <summary>
/// Centralized service for managing all application settings
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly ISecureStorageService _secureStorage;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private UserSettings? _cachedSettings;

    public SettingsService(
        ILogger<SettingsService> logger,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        ISecureStorageService secureStorage,
        IHardwareDetector hardwareDetector)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _secureStorage = secureStorage;
        _hardwareDetector = hardwareDetector;

        // Settings stored in AuraData/user-settings.json
        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _settingsFilePath = Path.Combine(auraDataDir, "user-settings.json");
    }

    public async Task<UserSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cachedSettings != null)
            {
                return CloneSettings(_cachedSettings);
            }
        }

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath, ct);
                var settings = JsonSerializer.Deserialize<UserSettings>(json) ?? CreateDefaultSettings();
                
                lock (_lock)
                {
                    _cachedSettings = settings;
                }
                
                _logger.LogInformation("Loaded user settings from {Path}", _settingsFilePath);
                return CloneSettings(settings);
            }
            else
            {
                var settings = CreateDefaultSettings();
                await SaveSettingsAsync(settings, ct);
                return CloneSettings(settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user settings, using defaults");
            return CreateDefaultSettings();
        }
    }

    public async Task<SettingsUpdateResult> UpdateSettingsAsync(UserSettings settings, CancellationToken ct = default)
    {
        try
        {
            // Validate settings
            var validation = await ValidateSettingsAsync(settings, ct);
            if (!validation.IsValid)
            {
                return new SettingsUpdateResult
                {
                    Success = false,
                    Message = "Settings validation failed",
                    Errors = validation.Issues
                        .Where(i => i.Severity == ValidationSeverity.Error)
                        .Select(i => i.Message)
                        .ToList(),
                    Warnings = validation.Issues
                        .Where(i => i.Severity == ValidationSeverity.Warning)
                        .Select(i => i.Message)
                        .ToList()
                };
            }

            settings.LastUpdated = DateTime.UtcNow;
            await SaveSettingsAsync(settings, ct);

            lock (_lock)
            {
                _cachedSettings = CloneSettings(settings);
            }

            _logger.LogInformation("Successfully updated user settings");
            return new SettingsUpdateResult
            {
                Success = true,
                Message = "Settings updated successfully",
                Warnings = validation.Issues
                    .Where(i => i.Severity == ValidationSeverity.Warning)
                    .Select(i => i.Message)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user settings");
            return new SettingsUpdateResult
            {
                Success = false,
                Message = "Failed to update settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<SettingsUpdateResult> ResetToDefaultsAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = CreateDefaultSettings();
            await SaveSettingsAsync(settings, ct);

            lock (_lock)
            {
                _cachedSettings = CloneSettings(settings);
            }

            _logger.LogInformation("Reset settings to defaults");
            return new SettingsUpdateResult
            {
                Success = true,
                Message = "Settings reset to defaults successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset settings to defaults");
            return new SettingsUpdateResult
            {
                Success = false,
                Message = "Failed to reset settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<T> GetSettingsSectionAsync<T>(CancellationToken ct = default) where T : class, new()
    {
        var settings = await GetSettingsAsync(ct);
        
        // Use reflection to get the appropriate section
        var property = typeof(UserSettings).GetProperty(typeof(T).Name.Replace("Settings", ""));
        if (property != null && property.PropertyType == typeof(T))
        {
            return (T)(property.GetValue(settings) ?? new T());
        }

        return new T();
    }

    public async Task<SettingsUpdateResult> UpdateSettingsSectionAsync<T>(T section, CancellationToken ct = default) where T : class
    {
        var settings = await GetSettingsAsync(ct);
        
        // Use reflection to update the appropriate section
        var property = typeof(UserSettings).GetProperty(typeof(T).Name.Replace("Settings", ""));
        if (property != null && property.PropertyType == typeof(T))
        {
            property.SetValue(settings, section);
            return await UpdateSettingsAsync(settings, ct);
        }

        return new SettingsUpdateResult
        {
            Success = false,
            Message = $"Settings section {typeof(T).Name} not found",
            Errors = new List<string> { $"Unknown settings section: {typeof(T).Name}" }
        };
    }

    public async Task<SettingsValidationResult> ValidateSettingsAsync(UserSettings settings, CancellationToken ct = default)
    {
        var result = new SettingsValidationResult { IsValid = true };

        // Validate autosave interval
        if (settings.General.AutosaveIntervalSeconds < 30 || settings.General.AutosaveIntervalSeconds > 3600)
        {
            result.Issues.Add(new ValidationIssue
            {
                Category = "General",
                Key = "AutosaveIntervalSeconds",
                Message = "Autosave interval must be between 30 and 3600 seconds",
                Severity = ValidationSeverity.Error
            });
            result.IsValid = false;
        }

        // Validate file paths
        if (!string.IsNullOrEmpty(settings.FileLocations.FFmpegPath) && !File.Exists(settings.FileLocations.FFmpegPath))
        {
            result.Issues.Add(new ValidationIssue
            {
                Category = "FileLocations",
                Key = "FFmpegPath",
                Message = "FFmpeg path does not exist",
                Severity = ValidationSeverity.Warning
            });
        }

        // Validate directories
        if (!string.IsNullOrEmpty(settings.FileLocations.OutputDirectory) && !Directory.Exists(settings.FileLocations.OutputDirectory))
        {
            result.Issues.Add(new ValidationIssue
            {
                Category = "FileLocations",
                Key = "OutputDirectory",
                Message = "Output directory does not exist",
                Severity = ValidationSeverity.Warning
            });
        }

        // Validate video defaults
        var validResolutions = new[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" };
        if (!validResolutions.Contains(settings.VideoDefaults.DefaultResolution))
        {
            result.Issues.Add(new ValidationIssue
            {
                Category = "VideoDefaults",
                Key = "DefaultResolution",
                Message = "Invalid resolution",
                Severity = ValidationSeverity.Error
            });
            result.IsValid = false;
        }

        if (settings.VideoDefaults.DefaultFrameRate < 24 || settings.VideoDefaults.DefaultFrameRate > 120)
        {
            result.Issues.Add(new ValidationIssue
            {
                Category = "VideoDefaults",
                Key = "DefaultFrameRate",
                Message = "Frame rate must be between 24 and 120",
                Severity = ValidationSeverity.Error
            });
            result.IsValid = false;
        }

        return result;
    }

    public async Task<string> ExportSettingsAsync(bool includeSecrets = false, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct);
        
        var exportData = new
        {
            settings.Version,
            ExportedAt = DateTime.UtcNow,
            IncludesSecrets = includeSecrets,
            settings.General,
            settings.FileLocations,
            settings.VideoDefaults,
            settings.EditorPreferences,
            settings.UI,
            settings.VisualGeneration,
            settings.Advanced
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(exportData, options);
    }

    public async Task<SettingsUpdateResult> ImportSettingsAsync(string json, bool overwriteExisting = false, CancellationToken ct = default)
    {
        try
        {
            var importedSettings = JsonSerializer.Deserialize<UserSettings>(json);
            if (importedSettings == null)
            {
                return new SettingsUpdateResult
                {
                    Success = false,
                    Message = "Failed to parse settings JSON",
                    Errors = new List<string> { "Invalid JSON format" }
                };
            }

            if (overwriteExisting)
            {
                return await UpdateSettingsAsync(importedSettings, ct);
            }
            else
            {
                // Merge with existing settings
                var currentSettings = await GetSettingsAsync(ct);
                MergeSettings(currentSettings, importedSettings);
                return await UpdateSettingsAsync(currentSettings, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import settings");
            return new SettingsUpdateResult
            {
                Success = false,
                Message = "Failed to import settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<HardwarePerformanceSettings> GetHardwareSettingsAsync(CancellationToken ct = default)
    {
        // Load from a separate file to keep hardware settings isolated
        var hwSettingsPath = Path.Combine(Path.GetDirectoryName(_settingsFilePath)!, "hardware-settings.json");
        
        try
        {
            if (File.Exists(hwSettingsPath))
            {
                var json = await File.ReadAllTextAsync(hwSettingsPath, ct);
                return JsonSerializer.Deserialize<HardwarePerformanceSettings>(json) ?? new HardwarePerformanceSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load hardware settings, using defaults");
        }

        return new HardwarePerformanceSettings();
    }

    public async Task<SettingsUpdateResult> UpdateHardwareSettingsAsync(HardwarePerformanceSettings settings, CancellationToken ct = default)
    {
        try
        {
            var hwSettingsPath = Path.Combine(Path.GetDirectoryName(_settingsFilePath)!, "hardware-settings.json");
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(hwSettingsPath, json, ct);

            _logger.LogInformation("Updated hardware performance settings");
            return new SettingsUpdateResult
            {
                Success = true,
                Message = "Hardware settings updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update hardware settings");
            return new SettingsUpdateResult
            {
                Success = false,
                Message = "Failed to update hardware settings",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ProviderConfiguration> GetProviderConfigurationAsync(CancellationToken ct = default)
    {
        var config = new ProviderConfiguration();

        // Load from ProviderSettings and KeyStore
        config.OpenAI = new OpenAIProviderSettings
        {
            ApiKey = _providerSettings.GetOpenAiApiKey() ?? string.Empty,
            Enabled = !string.IsNullOrEmpty(_providerSettings.GetOpenAiApiKey())
        };

        config.Ollama = new OllamaProviderSettings
        {
            BaseUrl = _providerSettings.GetOllamaUrl(),
            Model = _providerSettings.GetOllamaModel(),
            ExecutablePath = _providerSettings.GetOllamaExecutablePath(),
            Enabled = true
        };

        config.AzureOpenAI = new AzureOpenAIProviderSettings
        {
            ApiKey = _providerSettings.GetAzureOpenAiApiKey() ?? string.Empty,
            Endpoint = _providerSettings.GetAzureOpenAiEndpoint() ?? string.Empty,
            Enabled = !string.IsNullOrEmpty(_providerSettings.GetAzureOpenAiApiKey())
        };

        config.Gemini = new GeminiProviderSettings
        {
            ApiKey = _providerSettings.GetGeminiApiKey() ?? string.Empty,
            Enabled = !string.IsNullOrEmpty(_providerSettings.GetGeminiApiKey())
        };

        config.ElevenLabs = new ElevenLabsProviderSettings
        {
            ApiKey = _providerSettings.GetElevenLabsApiKey() ?? string.Empty,
            Enabled = !string.IsNullOrEmpty(_providerSettings.GetElevenLabsApiKey())
        };

        config.StableDiffusion = new StableDiffusionProviderSettings
        {
            BaseUrl = _providerSettings.GetStableDiffusionUrl(),
            Enabled = false // Disabled by default, user must enable
        };

        return config;
    }

    public async Task<SettingsUpdateResult> UpdateProviderConfigurationAsync(ProviderConfiguration config, CancellationToken ct = default)
    {
        try
        {
            // Update Ollama settings
            _providerSettings.SetOllamaModel(config.Ollama.Model);
            if (!string.IsNullOrEmpty(config.Ollama.ExecutablePath))
            {
                _providerSettings.SetOllamaExecutablePath(config.Ollama.ExecutablePath);
            }

            // Store API keys securely
            if (!string.IsNullOrEmpty(config.OpenAI.ApiKey))
            {
                await _keyStore.SetKeyAsync("OpenAI", config.OpenAI.ApiKey, ct);
            }

            if (!string.IsNullOrEmpty(config.Anthropic.ApiKey))
            {
                await _keyStore.SetKeyAsync("Anthropic", config.Anthropic.ApiKey, ct);
            }

            if (!string.IsNullOrEmpty(config.AzureOpenAI.ApiKey))
            {
                await _keyStore.SetKeyAsync("AzureOpenAI", config.AzureOpenAI.ApiKey, ct);
            }

            if (!string.IsNullOrEmpty(config.Gemini.ApiKey))
            {
                await _keyStore.SetKeyAsync("Gemini", config.Gemini.ApiKey, ct);
            }

            if (!string.IsNullOrEmpty(config.ElevenLabs.ApiKey))
            {
                await _keyStore.SetKeyAsync("ElevenLabs", config.ElevenLabs.ApiKey, ct);
            }

            _logger.LogInformation("Updated provider configuration");
            return new SettingsUpdateResult
            {
                Success = true,
                Message = "Provider configuration updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider configuration");
            return new SettingsUpdateResult
            {
                Success = false,
                Message = "Failed to update provider configuration",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ProviderTestResult> TestProviderConnectionAsync(string providerName, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            switch (providerName.ToLowerInvariant())
            {
                case "openai":
                    return await TestOpenAIConnectionAsync(ct);
                    
                case "ollama":
                    return await TestOllamaConnectionAsync(ct);
                    
                case "stablediffusion":
                    return await TestStableDiffusionConnectionAsync(ct);
                    
                default:
                    return new ProviderTestResult
                    {
                        Success = false,
                        ProviderName = providerName,
                        Message = $"Unknown provider: {providerName}",
                        ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test {Provider} connection", providerName);
            return new ProviderTestResult
            {
                Success = false,
                ProviderName = providerName,
                Message = $"Connection test failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<List<GpuDevice>> GetAvailableGpuDevicesAsync(CancellationToken ct = default)
    {
        var devices = new List<GpuDevice>();

        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync();
            if (systemProfile.Gpu != null)
            {
                devices.Add(new GpuDevice
                {
                    Id = "0",
                    Name = systemProfile.Gpu.Model,
                    Vendor = systemProfile.Gpu.Vendor,
                    VramMB = systemProfile.Gpu.VramGB * 1024,
                    IsDefault = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect GPU devices");
        }

        // Always add auto option
        devices.Insert(0, new GpuDevice
        {
            Id = "auto",
            Name = "Auto-detect (Recommended)",
            Vendor = "System",
            VramMB = 0,
            IsDefault = devices.Count == 0
        });

        return devices;
    }

    public async Task<List<EncoderOption>> GetAvailableEncodersAsync(CancellationToken ct = default)
    {
        var encoders = new List<EncoderOption>
        {
            new EncoderOption
            {
                Id = "auto",
                Name = "Auto (Recommended)",
                Description = "Automatically select best available encoder",
                IsHardwareAccelerated = false,
                IsAvailable = true
            },
            new EncoderOption
            {
                Id = "libx264",
                Name = "H.264 Software (libx264)",
                Description = "CPU-based H.264 encoder, compatible with all systems",
                IsHardwareAccelerated = false,
                IsAvailable = true
            },
            new EncoderOption
            {
                Id = "libx265",
                Name = "H.265 Software (libx265)",
                Description = "CPU-based H.265/HEVC encoder for better compression",
                IsHardwareAccelerated = false,
                IsAvailable = true
            }
        };

        // Check for hardware encoders
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync();
            
            if (systemProfile.EnableNVENC)
            {
                encoders.Add(new EncoderOption
                {
                    Id = "h264_nvenc",
                    Name = "H.264 NVIDIA NVENC",
                    Description = "Hardware-accelerated H.264 encoding for NVIDIA GPUs",
                    IsHardwareAccelerated = true,
                    IsAvailable = true,
                    RequiredHardware = new List<string> { "NVIDIA GPU" }
                });

                encoders.Add(new EncoderOption
                {
                    Id = "hevc_nvenc",
                    Name = "H.265 NVIDIA NVENC",
                    Description = "Hardware-accelerated H.265/HEVC encoding for NVIDIA GPUs",
                    IsHardwareAccelerated = true,
                    IsAvailable = true,
                    RequiredHardware = new List<string> { "NVIDIA GPU" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect hardware encoders");
        }

        return encoders;
    }

    // Private helper methods

    private async Task SaveSettingsAsync(UserSettings settings, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_settingsFilePath, json, ct);
    }

    private UserSettings CreateDefaultSettings()
    {
        return new UserSettings
        {
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow,
            General = new GeneralSettings
            {
                DefaultProjectSaveLocation = _providerSettings.GetProjectsDirectory(),
                AutosaveIntervalSeconds = 300,
                AutosaveEnabled = true,
                Language = "en-US",
                Theme = ThemeMode.Auto,
                CheckForUpdatesOnStartup = true,
                AdvancedModeEnabled = false
            },
            FileLocations = new FileLocationsSettings
            {
                OutputDirectory = _providerSettings.GetOutputDirectory(),
                TempDirectory = Path.GetTempPath(),
                ProjectsDirectory = _providerSettings.GetProjectsDirectory()
            },
            VideoDefaults = new VideoDefaultsSettings
            {
                DefaultResolution = "1920x1080",
                DefaultFrameRate = 30,
                DefaultCodec = "libx264",
                DefaultBitrate = "5M"
            },
            EditorPreferences = new EditorPreferencesSettings
            {
                TimelineSnapEnabled = true,
                TimelineSnapInterval = 1.0,
                PlaybackQuality = "high",
                GenerateThumbnails = true,
                ShowWaveforms = true,
                ShowTimecode = true
            },
            UI = new UISettings
            {
                Scale = 100,
                CompactMode = false,
                ColorScheme = "default"
            },
            VisualGeneration = new VisualGenerationSettings
            {
                EnableNsfwDetection = true,
                ContentSafetyLevel = "Moderate",
                EnableClipScoring = true,
                EnableQualityChecks = true
            },
            Advanced = new AdvancedSettings
            {
                OfflineMode = false,
                StableDiffusionUrl = "http://127.0.0.1:7860",
                OllamaUrl = "http://127.0.0.1:11434",
                EnableTelemetry = false
            }
        };
    }

    private UserSettings CloneSettings(UserSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<UserSettings>(json) ?? CreateDefaultSettings();
    }

    private void MergeSettings(UserSettings target, UserSettings source)
    {
        // Merge non-null values from source to target
        if (source.General != null) target.General = source.General;
        if (source.FileLocations != null) target.FileLocations = source.FileLocations;
        if (source.VideoDefaults != null) target.VideoDefaults = source.VideoDefaults;
        if (source.EditorPreferences != null) target.EditorPreferences = source.EditorPreferences;
        if (source.UI != null) target.UI = source.UI;
        if (source.VisualGeneration != null) target.VisualGeneration = source.VisualGeneration;
        if (source.Advanced != null) target.Advanced = source.Advanced;
    }

    private async Task<ProviderTestResult> TestOpenAIConnectionAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var apiKey = _providerSettings.GetOpenAiApiKey();

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ProviderTestResult
            {
                Success = false,
                ProviderName = "OpenAI",
                Message = "API key not configured",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync("https://api.openai.com/v1/models", ct);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Success = true,
                    ProviderName = "OpenAI",
                    Message = "Connection successful",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            else
            {
                return new ProviderTestResult
                {
                    Success = false,
                    ProviderName = "OpenAI",
                    Message = $"API returned {response.StatusCode}",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Success = false,
                ProviderName = "OpenAI",
                Message = $"Connection failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private async Task<ProviderTestResult> TestOllamaConnectionAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var baseUrl = _providerSettings.GetOllamaUrl();

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{baseUrl}/api/tags", ct);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Success = true,
                    ProviderName = "Ollama",
                    Message = "Connection successful",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            else
            {
                return new ProviderTestResult
                {
                    Success = false,
                    ProviderName = "Ollama",
                    Message = $"API returned {response.StatusCode}",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Success = false,
                ProviderName = "Ollama",
                Message = $"Connection failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    private async Task<ProviderTestResult> TestStableDiffusionConnectionAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var baseUrl = _providerSettings.GetStableDiffusionUrl();

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{baseUrl}/sdapi/v1/options", ct);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Success = true,
                    ProviderName = "StableDiffusion",
                    Message = "Connection successful",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            else
            {
                return new ProviderTestResult
                {
                    Success = false,
                    ProviderName = "StableDiffusion",
                    Message = $"API returned {response.StatusCode}",
                    ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Success = false,
                ProviderName = "StableDiffusion",
                Message = $"Connection failed: {ex.Message}",
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }
}
