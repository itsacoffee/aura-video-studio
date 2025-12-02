using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Settings;

/// <summary>
/// Service for managing graphics and visual settings with hardware detection
/// </summary>
public class GraphicsSettingsService : IGraphicsSettingsService
{
    private readonly ILogger<GraphicsSettingsService> _logger;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly IGpuDetectionService _gpuDetectionService;
    private readonly string _settingsFilePath;

    private GraphicsSettings? _cachedSettings;
    private readonly object _cacheLock = new();

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public event EventHandler<GraphicsSettings>? SettingsChanged;

    public GraphicsSettingsService(
        ILogger<GraphicsSettingsService> logger,
        IHardwareDetector hardwareDetector,
        IGpuDetectionService gpuDetectionService,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _gpuDetectionService = gpuDetectionService;

        var auraDataDir = providerSettings.GetAuraDataDirectory();
        _settingsFilePath = Path.Combine(auraDataDir, "graphics-settings.json");
    }

    /// <inheritdoc />
    public async Task<GraphicsSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }
        }

        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath, ct).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<GraphicsSettings>(json, JsonReadOptions);

                if (settings != null)
                {
                    lock (_cacheLock)
                    {
                        _cachedSettings = settings;
                    }
                    return settings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load graphics settings, using defaults");
            }
        }

        // First run: detect optimal settings
        return await DetectOptimalSettingsAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GraphicsSettings> DetectOptimalSettingsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Detecting optimal graphics settings...");

        var gpuInfo = await _gpuDetectionService.DetectGpuAsync(ct).ConfigureAwait(false);
        var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);

        var settings = new GraphicsSettings
        {
            DetectedGpuName = gpuInfo.GpuName,
            DetectedGpuVendor = systemProfile.Gpu?.Vendor,
            DetectedVramMB = gpuInfo.VramMB,
            GpuAccelerationEnabled = gpuInfo.HasGpu
        };

        // Determine optimal profile based on hardware
        settings.Profile = DetermineOptimalProfile(systemProfile, gpuInfo);
        ApplyProfileToSettings(settings, settings.Profile);

        // Check Windows accessibility settings
        settings.Accessibility.ReducedMotion = await CheckSystemReducedMotionAsync().ConfigureAwait(false);
        settings.Accessibility.HighContrast = await CheckSystemHighContrastAsync().ConfigureAwait(false);

        await SaveSettingsAsync(settings, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Optimal settings detected: Profile={Profile}, GPU={Gpu}, VRAM={VramMB}MB",
            settings.Profile, settings.DetectedGpuName ?? "None", settings.DetectedVramMB);

        return settings;
    }

    /// <inheritdoc />
    public async Task<GraphicsSettings> ApplyProfileAsync(PerformanceProfile profile, CancellationToken ct = default)
    {
        var settings = await GetSettingsAsync(ct).ConfigureAwait(false);
        settings.Profile = profile;
        ApplyProfileToSettings(settings, profile);
        await SaveSettingsAsync(settings, ct).ConfigureAwait(false);
        return settings;
    }

    private static void ApplyProfileToSettings(GraphicsSettings settings, PerformanceProfile profile)
    {
        switch (profile)
        {
            case PerformanceProfile.Maximum:
                settings.Effects = new VisualEffectsSettings
                {
                    Animations = true,
                    BlurEffects = true,
                    Shadows = true,
                    Transparency = true,
                    SmoothScrolling = true,
                    SpringPhysics = true,
                    ParallaxEffects = true,
                    GlowEffects = true,
                    MicroInteractions = true,
                    StaggeredAnimations = true
                };
                break;

            case PerformanceProfile.Balanced:
                settings.Effects = new VisualEffectsSettings
                {
                    Animations = true,
                    BlurEffects = false, // Disable expensive blur
                    Shadows = true,
                    Transparency = true,
                    SmoothScrolling = true,
                    SpringPhysics = false, // Use simple easing
                    ParallaxEffects = false,
                    GlowEffects = false,
                    MicroInteractions = true,
                    StaggeredAnimations = false
                };
                break;

            case PerformanceProfile.PowerSaver:
                settings.Effects = new VisualEffectsSettings
                {
                    Animations = false,
                    BlurEffects = false,
                    Shadows = false,
                    Transparency = false,
                    SmoothScrolling = false,
                    SpringPhysics = false,
                    ParallaxEffects = false,
                    GlowEffects = false,
                    MicroInteractions = false,
                    StaggeredAnimations = false
                };
                break;

            case PerformanceProfile.Custom:
                // Don't modify effects, user controls individually
                break;
        }
    }

    private static PerformanceProfile DetermineOptimalProfile(SystemProfile system, GpuDetectionResult gpu)
    {
        // High-end: Dedicated GPU with 4GB+ VRAM and 16GB+ RAM
        if (gpu.HasGpu && gpu.VramMB >= 4096 && system.RamGB >= 16)
        {
            return PerformanceProfile.Maximum;
        }

        // Mid-range: Any GPU or 8GB+ RAM
        if (gpu.HasGpu || system.RamGB >= 8)
        {
            return PerformanceProfile.Balanced;
        }

        // Low-end: No GPU and limited RAM
        return PerformanceProfile.PowerSaver;
    }

    private Task<bool> CheckSystemReducedMotionAsync()
    {
        // Check Windows "Show animations" setting
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(false);
        }

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Desktop\WindowMetrics");
            var value = key?.GetValue("MinAnimate");
            return Task.FromResult(value?.ToString() == "0");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check system reduced motion setting");
            return Task.FromResult(false);
        }
    }

    private Task<bool> CheckSystemHighContrastAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(false);
        }

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Accessibility\HighContrast");
            var flags = key?.GetValue("Flags");
            if (flags is int flagsInt)
            {
                return Task.FromResult((flagsInt & 1) == 1);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check system high contrast setting");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SaveSettingsAsync(GraphicsSettings settings, CancellationToken ct = default)
    {
        try
        {
            settings.LastModified = DateTime.UtcNow;

            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonWriteOptions);

            await File.WriteAllTextAsync(_settingsFilePath, json, ct).ConfigureAwait(false);

            lock (_cacheLock)
            {
                _cachedSettings = settings;
            }

            SettingsChanged?.Invoke(this, settings);

            _logger.LogInformation("Graphics settings saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save graphics settings");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResetToDefaultsAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            _cachedSettings = null;
        }

        if (File.Exists(_settingsFilePath))
        {
            try
            {
                File.Delete(_settingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete existing graphics settings file");
            }
        }

        await DetectOptimalSettingsAsync(ct).ConfigureAwait(false);
        return true;
    }
}
