using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.VideoEffects;
using Aura.Core.Services.VideoEffects;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Video;

/// <summary>
/// Integration service for applying video effects to generated videos
/// </summary>
public interface IVideoEffectsIntegration
{
    /// <summary>
    /// Apply post-processing effects to a generated video
    /// </summary>
    Task<string> ApplyPostProcessingEffectsAsync(
        string videoPath,
        VideoEffectsProfile profile,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default effects profile for a video style
    /// </summary>
    VideoEffectsProfile GetDefaultProfile(string style);

    /// <summary>
    /// Validate effects profile
    /// </summary>
    bool ValidateProfile(VideoEffectsProfile profile, out string? errorMessage);
}

/// <summary>
/// Profile containing video effects to apply
/// </summary>
public class VideoEffectsProfile
{
    /// <summary>
    /// Profile name
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Effects to apply
    /// </summary>
    public List<VideoEffect> Effects { get; set; } = new();

    /// <summary>
    /// Whether to enable effects
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether to use cached results
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// Quality/performance trade-off (0.0 to 1.0, higher = better quality but slower)
    /// </summary>
    public double Quality { get; set; } = 0.75;
}

/// <summary>
/// Implementation of video effects integration
/// </summary>
public class VideoEffectsIntegration : IVideoEffectsIntegration
{
    private readonly IVideoEffectService _effectService;
    private readonly IEffectCacheService _cacheService;
    private readonly ILogger<VideoEffectsIntegration> _logger;

    public VideoEffectsIntegration(
        IVideoEffectService effectService,
        IEffectCacheService cacheService,
        ILogger<VideoEffectsIntegration> logger)
    {
        _effectService = effectService ?? throw new ArgumentNullException(nameof(effectService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ApplyPostProcessingEffectsAsync(
        string videoPath,
        VideoEffectsProfile profile,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Video file not found", videoPath);
        }

        if (!profile.Enabled || profile.Effects.Count == 0)
        {
            _logger.LogInformation("Effects disabled or no effects to apply, returning original video");
            return videoPath;
        }

        if (!ValidateProfile(profile, out var errorMessage))
        {
            throw new InvalidOperationException($"Invalid effects profile: {errorMessage}");
        }

        _logger.LogInformation(
            "Applying {Count} effects to video: {Path}",
            profile.Effects.Count,
            videoPath
        );

        // Generate output path
        var outputPath = Path.Combine(
            Path.GetDirectoryName(videoPath) ?? Path.GetTempPath(),
            $"{Path.GetFileNameWithoutExtension(videoPath)}_effects{Path.GetExtension(videoPath)}"
        );

        // Check cache if enabled
        if (profile.UseCache)
        {
            var cacheKey = _cacheService.GenerateCacheKey(videoPath, profile.Effects);
            var cachedPath = await _cacheService.GetCachedEffectAsync(cacheKey, cancellationToken);
            
            if (cachedPath != null)
            {
                _logger.LogInformation("Using cached effects result: {Path}", cachedPath);
                return cachedPath;
            }
        }

        // Apply effects
        var resultPath = await _effectService.ApplyEffectsAsync(
            videoPath,
            outputPath,
            profile.Effects,
            progressCallback,
            cancellationToken
        );

        // Cache the result
        if (profile.UseCache)
        {
            var cacheKey = _cacheService.GenerateCacheKey(videoPath, profile.Effects);
            await _cacheService.CacheEffectAsync(cacheKey, resultPath, cancellationToken);
        }

        _logger.LogInformation("Successfully applied effects: {Path}", resultPath);
        return resultPath;
    }

    public VideoEffectsProfile GetDefaultProfile(string style)
    {
        var profile = new VideoEffectsProfile
        {
            Name = $"{style} Default",
            Enabled = true,
            UseCache = true,
            Quality = 0.75
        };

        // Add default effects based on style
        switch (style.ToLowerInvariant())
        {
            case "cinematic":
                profile.Effects.Add(new ColorCorrectionEffect
                {
                    Name = "Cinematic Grade",
                    Contrast = 0.2,
                    Saturation = -0.1,
                    Temperature = 10,
                    Duration = 1.0,
                    StartTime = 0
                });
                break;

            case "vintage":
                profile.Effects.Add(new VintageEffect
                {
                    Name = "Vintage Look",
                    Style = VintageEffect.VintageStyle.OldFilm,
                    Grain = 0.3,
                    Vignette = 0.5,
                    Duration = 1.0,
                    StartTime = 0
                });
                break;

            case "dramatic":
                profile.Effects.Add(new ColorCorrectionEffect
                {
                    Name = "Dramatic Grade",
                    Contrast = 0.4,
                    Saturation = 0.2,
                    Brightness = -0.1,
                    Duration = 1.0,
                    StartTime = 0
                });
                break;

            case "professional":
                profile.Effects.Add(new ColorCorrectionEffect
                {
                    Name = "Professional Grade",
                    Contrast = 0.15,
                    Saturation = 0.05,
                    Duration = 1.0,
                    StartTime = 0
                });
                profile.Effects.Add(new SharpenEffect
                {
                    Name = "Sharpen",
                    Strength = 1.5,
                    Duration = 1.0,
                    StartTime = 0
                });
                break;

            default:
                // No effects for default style
                profile.Enabled = false;
                break;
        }

        return profile;
    }

    public bool ValidateProfile(VideoEffectsProfile profile, out string? errorMessage)
    {
        if (profile.Effects.Count == 0)
        {
            errorMessage = "Profile must contain at least one effect";
            return false;
        }

        if (profile.Quality < 0 || profile.Quality > 1)
        {
            errorMessage = "Quality must be between 0 and 1";
            return false;
        }

        // Validate each effect
        foreach (var effect in profile.Effects)
        {
            if (!_effectService.ValidateEffect(effect, out var effectError))
            {
                errorMessage = $"Effect '{effect.Name}' validation failed: {effectError}";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }
}
