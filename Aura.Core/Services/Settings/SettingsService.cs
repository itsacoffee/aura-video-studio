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
using Aura.Core.Data;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Settings;

/// <summary>
/// Centralized service for managing all application settings
/// Now using database storage with optional encryption
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly ISecureStorageService _secureStorage;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly AuraDbContext _dbContext;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();
    private UserSettings? _cachedSettings;

    public SettingsService(
        ILogger<SettingsService> logger,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        ISecureStorageService secureStorage,
        IHardwareDetector hardwareDetector,
        AuraDbContext dbContext)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _secureStorage = secureStorage;
        _hardwareDetector = hardwareDetector;
        _dbContext = dbContext;

        // Settings stored in AuraData/user-settings.json (legacy, for migration)
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
            // Try loading from database first
            var settingsEntity = await _dbContext.Settings
                .FirstOrDefaultAsync(s => s.Id == "user-settings", ct).ConfigureAwait(false);

            if (settingsEntity != null)
            {
                // Settings are stored as plain JSON (API keys are handled separately via KeyStore)
                var settings = JsonSerializer.Deserialize<UserSettings>(settingsEntity.SettingsJson) 
                    ?? CreateDefaultSettings();

                lock (_lock)
                {
                    _cachedSettings = settings;
                }

                _logger.LogInformation("Loaded user settings from database");
                return CloneSettings(settings);
            }

            // Fallback to JSON file (for migration from old versions)
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath, ct).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<UserSettings>(json) ?? CreateDefaultSettings();

                // Migrate to database
                await MigrateJsonToDatabase(settings, ct).ConfigureAwait(false);

                lock (_lock)
                {
                    _cachedSettings = settings;
                }

                _logger.LogInformation("Loaded user settings from JSON file and migrated to database");
                return CloneSettings(settings);
            }

            // No settings found, create defaults
            var defaultSettings = CreateDefaultSettings();
            await SaveSettingsToDatabaseAsync(defaultSettings, ct).ConfigureAwait(false);
            return CloneSettings(defaultSettings);
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
            var validation = await ValidateSettingsAsync(settings, ct).ConfigureAwait(false);
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
            await SaveSettingsToDatabaseAsync(settings, ct).ConfigureAwait(false);

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
            await SaveSettingsToDatabaseAsync(settings, ct).ConfigureAwait(false);

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
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        
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
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        
        // Use reflection to update the appropriate section
        var property = typeof(UserSettings).GetProperty(typeof(T).Name.Replace("Settings", ""));
        if (property != null && property.PropertyType == typeof(T))
        {
            property.SetValue(settings, section);
            return await UpdateSettingsAsync(settings, ct).ConfigureAwait(false);
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
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        
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
                return await UpdateSettingsAsync(importedSettings, ct).ConfigureAwait(false);
            }
            else
            {
                // Merge with existing settings
                var currentSettings = await GetSettingsAsync(ct).ConfigureAwait(false);
                MergeSettings(currentSettings, importedSettings);
                return await UpdateSettingsAsync(currentSettings, ct).ConfigureAwait(false);
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
                var json = await File.ReadAllTextAsync(hwSettingsPath, ct).ConfigureAwait(false);
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
            await File.WriteAllTextAsync(hwSettingsPath, json, ct).ConfigureAwait(false);

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
                await _keyStore.SetKeyAsync("OpenAI", config.OpenAI.ApiKey, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(config.Anthropic.ApiKey))
            {
                await _keyStore.SetKeyAsync("Anthropic", config.Anthropic.ApiKey, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(config.AzureOpenAI.ApiKey))
            {
                await _keyStore.SetKeyAsync("AzureOpenAI", config.AzureOpenAI.ApiKey, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(config.Gemini.ApiKey))
            {
                await _keyStore.SetKeyAsync("Gemini", config.Gemini.ApiKey, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(config.ElevenLabs.ApiKey))
            {
                await _keyStore.SetKeyAsync("ElevenLabs", config.ElevenLabs.ApiKey, ct).ConfigureAwait(false);
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
                    return await TestOpenAIConnectionAsync(ct).ConfigureAwait(false);
                    
                case "ollama":
                    return await TestOllamaConnectionAsync(ct).ConfigureAwait(false);
                    
                case "stablediffusion":
                    return await TestStableDiffusionConnectionAsync(ct).ConfigureAwait(false);
                    
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
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
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
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
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

    private async Task SaveSettingsToDatabaseAsync(UserSettings settings, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var settingsEntity = await _dbContext.Settings
            .FirstOrDefaultAsync(s => s.Id == "user-settings", ct).ConfigureAwait(false);

        if (settingsEntity == null)
        {
            settingsEntity = new SettingsEntity
            {
                Id = "user-settings",
                SettingsJson = json,
                IsEncrypted = false,
                Version = settings.Version,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Settings.Add(settingsEntity);
        }
        else
        {
            settingsEntity.SettingsJson = json;
            settingsEntity.Version = settings.Version;
            settingsEntity.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Saved settings to database");
    }

    private async Task MigrateJsonToDatabase(UserSettings settings, CancellationToken ct)
    {
        try
        {
            await SaveSettingsToDatabaseAsync(settings, ct).ConfigureAwait(false);
            _logger.LogInformation("Successfully migrated settings from JSON to database");

            // Optionally rename the old JSON file instead of deleting it
            if (File.Exists(_settingsFilePath))
            {
                var backupPath = _settingsFilePath + ".backup";
                File.Move(_settingsFilePath, backupPath, overwrite: true);
                _logger.LogInformation("Backed up old settings file to {BackupPath}", backupPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate settings from JSON to database");
            throw;
        }
    }

    private async Task SaveSettingsAsync(UserSettings settings, CancellationToken ct)
    {
        // Legacy method - now just calls the database method
        await SaveSettingsToDatabaseAsync(settings, ct).ConfigureAwait(false);
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
            Export = new Aura.Core.Models.Settings.ExportSettings
            {
                DefaultPreset = "YouTube1080p",
                AutoOpenOutputFolder = true,
                GenerateThumbnail = true,
                Watermark = new Aura.Core.Models.Settings.WatermarkSettings
                {
                    Enabled = false,
                    Type = Aura.Core.Models.Settings.WatermarkType.Text,
                    Opacity = 0.7,
                    Scale = 0.1,
                    Position = Aura.Core.Models.Settings.WatermarkPosition.BottomRight
                },
                NamingPattern = new Aura.Core.Models.Settings.NamingPatternSettings
                {
                    Pattern = "{project}_{date}_{time}",
                    DateFormat = "yyyy-MM-dd",
                    TimeFormat = "HHmmss",
                    SanitizeFilenames = true,
                    ReplaceSpaces = true
                }
            },
            RateLimits = new Aura.Core.Models.Settings.ProviderRateLimits
            {
                Global = new Aura.Core.Models.Settings.GlobalRateLimitSettings
                {
                    Enabled = true,
                    MaxTotalRequestsPerMinute = 100,
                    MaxTotalDailyCost = 50,
                    MaxTotalMonthlyCost = 500,
                    EnableCircuitBreaker = true,
                    EnableLoadBalancing = true,
                    LoadBalancingStrategy = Aura.Core.Models.Settings.LoadBalancingStrategy.LeastCost
                }
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
        if (source.Export != null) target.Export = source.Export;
        if (source.RateLimits != null) target.RateLimits = source.RateLimits;
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

            var response = await client.GetAsync("https://api.openai.com/v1/models", ct).ConfigureAwait(false);
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

            var response = await client.GetAsync($"{baseUrl}/api/tags", ct).ConfigureAwait(false);
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

            var response = await client.GetAsync($"{baseUrl}/sdapi/v1/options", ct).ConfigureAwait(false);
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
