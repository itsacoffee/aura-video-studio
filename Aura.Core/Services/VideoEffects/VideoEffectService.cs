using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.VideoEffects;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.FFmpeg.Filters;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VideoEffects;

/// <summary>
/// Service for managing video effects
/// </summary>
public interface IVideoEffectService
{
    /// <summary>
    /// Get all available effect presets
    /// </summary>
    Task<List<EffectPreset>> GetPresetsAsync(EffectCategory? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific preset by ID
    /// </summary>
    Task<EffectPreset?> GetPresetByIdAsync(string presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a custom preset
    /// </summary>
    Task<EffectPreset> SavePresetAsync(EffectPreset preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a custom preset
    /// </summary>
    Task<bool> DeletePresetAsync(string presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply effects to a video file
    /// </summary>
    Task<string> ApplyEffectsAsync(
        string inputPath,
        string outputPath,
        List<VideoEffect> effects,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply a preset to a video file
    /// </summary>
    Task<string> ApplyPresetAsync(
        string inputPath,
        string outputPath,
        string presetId,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate preview for an effect
    /// </summary>
    Task<string> GenerateEffectPreviewAsync(
        string inputPath,
        VideoEffect effect,
        TimeSpan previewDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate effect parameters
    /// </summary>
    bool ValidateEffect(VideoEffect effect, out string? errorMessage);

    /// <summary>
    /// Get recommended effects for a video
    /// </summary>
    Task<List<EffectPreset>> GetRecommendedEffectsAsync(
        string videoPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build FFmpeg filter complex for multiple effects
    /// </summary>
    string BuildEffectFilterComplex(List<VideoEffect> effects);
}

/// <summary>
/// Implementation of video effect service
/// </summary>
public class VideoEffectService : IVideoEffectService
{
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly ILogger<VideoEffectService> _logger;
    private readonly string _presetsDirectory;
    private readonly Dictionary<string, EffectPreset> _builtInPresets;

    public VideoEffectService(
        IFFmpegExecutor ffmpegExecutor,
        ILogger<VideoEffectService> logger,
        string? presetsDirectory = null)
    {
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _presetsDirectory = presetsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aura", "VideoEffects", "Presets");
        
        Directory.CreateDirectory(_presetsDirectory);
        _builtInPresets = InitializeBuiltInPresets();
    }

    public async Task<List<EffectPreset>> GetPresetsAsync(
        EffectCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var presets = new List<EffectPreset>(_builtInPresets.Values);

        // Load custom presets from disk
        if (Directory.Exists(_presetsDirectory))
        {
            var presetFiles = Directory.GetFiles(_presetsDirectory, "*.json");
            foreach (var file in presetFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var preset = JsonSerializer.Deserialize<EffectPreset>(json);
                    if (preset != null)
                    {
                        presets.Add(preset);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load preset from {File}", file);
                }
            }
        }

        if (category.HasValue)
        {
            presets = presets.Where(p => p.Category == category.Value).ToList();
        }

        return presets.OrderByDescending(p => p.IsFavorite)
                     .ThenByDescending(p => p.UsageCount)
                     .ThenBy(p => p.Name)
                     .ToList();
    }

    public async Task<EffectPreset?> GetPresetByIdAsync(string presetId, CancellationToken cancellationToken = default)
    {
        // Check built-in presets
        if (_builtInPresets.TryGetValue(presetId, out var builtInPreset))
        {
            return builtInPreset;
        }

        // Check custom presets
        var presetFile = Path.Combine(_presetsDirectory, $"{presetId}.json");
        if (File.Exists(presetFile))
        {
            var json = await File.ReadAllTextAsync(presetFile, cancellationToken);
            return JsonSerializer.Deserialize<EffectPreset>(json);
        }

        return null;
    }

    public async Task<EffectPreset> SavePresetAsync(EffectPreset preset, CancellationToken cancellationToken = default)
    {
        preset.ModifiedAt = DateTime.UtcNow;
        preset.IsBuiltIn = false;

        var presetFile = Path.Combine(_presetsDirectory, $"{preset.Id}.json");
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(presetFile, json, cancellationToken);

        _logger.LogInformation("Saved preset {PresetId} to {File}", preset.Id, presetFile);
        return preset;
    }

    public async Task<bool> DeletePresetAsync(string presetId, CancellationToken cancellationToken = default)
    {
        // Can't delete built-in presets
        if (_builtInPresets.ContainsKey(presetId))
        {
            return false;
        }

        var presetFile = Path.Combine(_presetsDirectory, $"{presetId}.json");
        if (File.Exists(presetFile))
        {
            await Task.Run(() => File.Delete(presetFile), cancellationToken);
            _logger.LogInformation("Deleted preset {PresetId}", presetId);
            return true;
        }

        return false;
    }

    public async Task<string> ApplyEffectsAsync(
        string inputPath,
        string outputPath,
        List<VideoEffect> effects,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input video file not found", inputPath);
        }

        _logger.LogInformation("Applying {Count} effects to video: {Input}", effects.Count, inputPath);

        // Validate all effects
        foreach (var effect in effects)
        {
            if (!ValidateEffect(effect, out var errorMessage))
            {
                throw new InvalidOperationException($"Invalid effect {effect.Name}: {errorMessage}");
            }
        }

        // Build FFmpeg command
        var filterComplex = BuildEffectFilterComplex(effects);
        var builder = new FFmpegCommandBuilder()
            .AddInput(inputPath)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .AddFilter(filterComplex)
            .SetVideoCodec("libx264")
            .SetAudioCodec("aac")
            .SetPreset("medium")
            .SetCRF(23);

        // Execute command
        double lastProgress = 0;
        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            progress =>
            {
                if (progress.PercentComplete > lastProgress + 1)
                {
                    lastProgress = progress.PercentComplete;
                    progressCallback?.Invoke(progress.PercentComplete);
                }
            },
            timeout: TimeSpan.FromMinutes(60),
            cancellationToken
        );

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to apply effects: {result.ErrorMessage}");
        }

        _logger.LogInformation("Successfully applied effects to {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> ApplyPresetAsync(
        string inputPath,
        string outputPath,
        string presetId,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var preset = await GetPresetByIdAsync(presetId, cancellationToken);
        if (preset == null)
        {
            throw new InvalidOperationException($"Preset not found: {presetId}");
        }

        // Update usage count
        preset.UsageCount++;
        if (!preset.IsBuiltIn)
        {
            await SavePresetAsync(preset, cancellationToken);
        }

        return await ApplyEffectsAsync(inputPath, outputPath, preset.Effects, progressCallback, cancellationToken);
    }

    public async Task<string> GenerateEffectPreviewAsync(
        string inputPath,
        VideoEffect effect,
        TimeSpan previewDuration,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input video file not found", inputPath);
        }

        var previewPath = Path.Combine(
            Path.GetTempPath(),
            $"preview_{Guid.NewGuid()}.mp4"
        );

        _logger.LogInformation("Generating preview for effect {Effect}", effect.Name);

        var filterString = effect.ToFFmpegFilter();
        var builder = new FFmpegCommandBuilder()
            .AddInput(inputPath)
            .SetOutput(previewPath)
            .SetOverwrite(true)
            .SetStartTime(TimeSpan.Zero)
            .SetDuration(previewDuration)
            .AddFilter(filterString)
            .SetVideoCodec("libx264")
            .SetPreset("ultrafast")
            .SetCRF(28)
            .SetResolution(640, 360); // Lower resolution for preview

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        );

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to generate preview: {result.ErrorMessage}");
        }

        return previewPath;
    }

    public bool ValidateEffect(VideoEffect effect, out string? errorMessage)
    {
        return effect.Validate(out errorMessage);
    }

    public async Task<List<EffectPreset>> GetRecommendedEffectsAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        // For now, return popular presets
        // In future, analyze video content and recommend appropriate effects
        var allPresets = await GetPresetsAsync(cancellationToken: cancellationToken);
        return allPresets.OrderByDescending(p => p.UsageCount).Take(5).ToList();
    }

    public string BuildEffectFilterComplex(List<VideoEffect> effects)
    {
        if (effects.Count == 0)
        {
            return "copy";
        }

        // Sort effects by start time and layer
        var sortedEffects = effects
            .Where(e => e.Enabled)
            .OrderBy(e => e.Layer)
            .ThenBy(e => e.StartTime)
            .ToList();

        var filters = new List<string>();
        
        foreach (var effect in sortedEffects)
        {
            var filterString = effect.ToFFmpegFilter();
            if (!string.IsNullOrEmpty(filterString) && filterString != "copy")
            {
                filters.Add(filterString);
            }
        }

        return filters.Count > 0 ? string.Join(",", filters) : "copy";
    }

    private Dictionary<string, EffectPreset> InitializeBuiltInPresets()
    {
        var presets = new Dictionary<string, EffectPreset>();

        // Cinematic preset
        presets["cinematic"] = new EffectPreset
        {
            Id = "cinematic",
            Name = "Cinematic",
            Description = "Professional cinematic look with color grading",
            Category = EffectCategory.Cinematic,
            IsBuiltIn = true,
            Tags = new List<string> { "cinematic", "professional", "color-grading" },
            Effects = new List<VideoEffect>
            {
                new ColorCorrectionEffect
                {
                    Name = "Cinematic Color Grade",
                    Contrast = 0.2,
                    Saturation = -0.1,
                    Temperature = 10,
                    Duration = 1.0
                }
            }
        };

        // Vintage preset
        presets["vintage"] = new EffectPreset
        {
            Id = "vintage",
            Name = "Vintage Film",
            Description = "Classic vintage film look",
            Category = EffectCategory.Vintage,
            IsBuiltIn = true,
            Tags = new List<string> { "vintage", "retro", "film" },
            Effects = new List<VideoEffect>
            {
                new VintageEffect
                {
                    Name = "Vintage",
                    Style = VintageEffect.VintageStyle.OldFilm,
                    Grain = 0.4,
                    Vignette = 0.6,
                    Duration = 1.0
                }
            }
        };

        // Dramatic preset
        presets["dramatic"] = new EffectPreset
        {
            Id = "dramatic",
            Name = "Dramatic",
            Description = "High contrast dramatic look",
            Category = EffectCategory.Cinematic,
            IsBuiltIn = true,
            Tags = new List<string> { "dramatic", "high-contrast", "bold" },
            Effects = new List<VideoEffect>
            {
                new ColorCorrectionEffect
                {
                    Name = "Dramatic Grade",
                    Contrast = 0.4,
                    Saturation = 0.2,
                    Brightness = -0.1,
                    Duration = 1.0
                }
            }
        };

        // Soft blur preset
        presets["soft-blur"] = new EffectPreset
        {
            Id = "soft-blur",
            Name = "Soft Blur",
            Description = "Gentle gaussian blur effect",
            Category = EffectCategory.Blur,
            IsBuiltIn = true,
            Tags = new List<string> { "blur", "soft", "dreamy" },
            Effects = new List<VideoEffect>
            {
                new BlurEffect
                {
                    Name = "Soft Blur",
                    BlurStyle = BlurEffect.BlurType.Gaussian,
                    Strength = 3.0,
                    Duration = 1.0
                }
            }
        };

        // Black and white preset
        presets["black-white"] = new EffectPreset
        {
            Id = "black-white",
            Name = "Black & White",
            Description = "Classic black and white conversion",
            Category = EffectCategory.Artistic,
            IsBuiltIn = true,
            Tags = new List<string> { "black-white", "monochrome", "classic" },
            Effects = new List<VideoEffect>
            {
                new VintageEffect
                {
                    Name = "B&W",
                    Style = VintageEffect.VintageStyle.BlackAndWhite,
                    Grain = 0.0,
                    Vignette = 0.3,
                    Duration = 1.0
                }
            }
        };

        return presets;
    }
}
