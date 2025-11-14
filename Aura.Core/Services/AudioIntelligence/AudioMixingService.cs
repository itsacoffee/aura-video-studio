using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for intelligent audio mixing and balancing
/// </summary>
public class AudioMixingService
{
    private readonly ILogger<AudioMixingService> _logger;

    public AudioMixingService(ILogger<AudioMixingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates mixing suggestions based on content type and audio elements
    /// </summary>
    public async Task<AudioMixing> GenerateMixingSuggestionsAsync(
        string contentType,
        bool hasNarration,
        bool hasMusic,
        bool hasSoundEffects,
        double targetLUFS = -14.0,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating mixing suggestions for content type: {ContentType}", contentType);

        await Task.CompletedTask.ConfigureAwait(false); // For async pattern

        try
        {
            // Determine optimal volume levels
            var (narrationVol, musicVol, sfxVol) = CalculateVolumeLevels(
                contentType, hasNarration, hasMusic, hasSoundEffects);

            // Configure ducking if both narration and music are present
            var ducking = hasNarration && hasMusic
                ? GenerateDuckingSettings(contentType)
                : new DuckingSettings(-12.0, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 0.02);

            // Configure EQ for voice clarity
            var eq = hasNarration
                ? GenerateVoiceEQSettings()
                : new EqualizationSettings(80, 0, 0);

            // Configure compression for dynamic range
            var compression = GenerateCompressionSettings(contentType);

            return new AudioMixing(
                MusicVolume: musicVol,
                NarrationVolume: narrationVol,
                SoundEffectsVolume: sfxVol,
                Ducking: ducking,
                EQ: eq,
                Compression: compression,
                Normalize: true,
                TargetLUFS: targetLUFS
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mixing suggestions");
            throw;
        }
    }

    /// <summary>
    /// Calculates optimal volume levels for each audio element
    /// </summary>
    private (double Narration, double Music, double SoundEffects) CalculateVolumeLevels(
        string contentType,
        bool hasNarration,
        bool hasMusic,
        bool hasSoundEffects)
    {
        var type = contentType.ToLowerInvariant();

        // Voice-forward content (narration is primary)
        if (type.Contains("educational") || type.Contains("tutorial") || type.Contains("documentary"))
        {
            return (
                Narration: hasNarration ? 100 : 0,
                Music: hasMusic ? 25 : 0,        // Keep music low
                SoundEffects: hasSoundEffects ? 40 : 0
            );
        }

        // Music-forward content
        if (type.Contains("music video") || type.Contains("montage"))
        {
            return (
                Narration: hasNarration ? 70 : 0,
                Music: hasMusic ? 90 : 0,         // Music is prominent
                SoundEffects: hasSoundEffects ? 50 : 0
            );
        }

        // Balanced corporate/promotional
        if (type.Contains("corporate") || type.Contains("promotional") || type.Contains("marketing"))
        {
            return (
                Narration: hasNarration ? 95 : 0,
                Music: hasMusic ? 40 : 0,         // Supporting role
                SoundEffects: hasSoundEffects ? 45 : 0
            );
        }

        // Action/gaming content
        if (type.Contains("gaming") || type.Contains("action") || type.Contains("sports"))
        {
            return (
                Narration: hasNarration ? 90 : 0,
                Music: hasMusic ? 60 : 0,         // More energetic
                SoundEffects: hasSoundEffects ? 70 : 0  // Prominent effects
            );
        }

        // Default balanced mix
        return (
            Narration: hasNarration ? 100 : 0,
            Music: hasMusic ? 35 : 0,
            SoundEffects: hasSoundEffects ? 50 : 0
        );
    }

    /// <summary>
    /// Generates ducking settings for music when narration plays
    /// </summary>
    private DuckingSettings GenerateDuckingSettings(string contentType)
    {
        var type = contentType.ToLowerInvariant();

        // Aggressive ducking for voice-heavy content
        if (type.Contains("educational") || type.Contains("tutorial"))
        {
            return new DuckingSettings(
                DuckDepthDb: -15.0,              // Reduce music by 15dB
                AttackTime: TimeSpan.FromMilliseconds(80),
                ReleaseTime: TimeSpan.FromMilliseconds(600),
                Threshold: 0.02
            );
        }

        // Gentle ducking for corporate
        if (type.Contains("corporate") || type.Contains("promotional"))
        {
            return new DuckingSettings(
                DuckDepthDb: -10.0,              // Moderate reduction
                AttackTime: TimeSpan.FromMilliseconds(120),
                ReleaseTime: TimeSpan.FromMilliseconds(800),
                Threshold: 0.02
            );
        }

        // Default moderate ducking
        return new DuckingSettings(
            DuckDepthDb: -12.0,
            AttackTime: TimeSpan.FromMilliseconds(100),
            ReleaseTime: TimeSpan.FromMilliseconds(500),
            Threshold: 0.02
        );
    }

    /// <summary>
    /// Generates EQ settings for voice clarity
    /// </summary>
    private EqualizationSettings GenerateVoiceEQSettings()
    {
        return new EqualizationSettings(
            HighPassFrequency: 80,               // Remove low rumble
            PresenceBoost: 3.0,                  // Boost clarity around 3-5kHz
            DeEsserReduction: -4.0               // Reduce harsh sibilance at 7kHz
        );
    }

    /// <summary>
    /// Generates compression settings for dynamic range
    /// </summary>
    private CompressionSettings GenerateCompressionSettings(string contentType)
    {
        var type = contentType.ToLowerInvariant();

        // Gentle compression for natural sound
        if (type.Contains("documentary") || type.Contains("educational"))
        {
            return new CompressionSettings(
                Threshold: -20.0,
                Ratio: 2.5,
                AttackTime: TimeSpan.FromMilliseconds(30),
                ReleaseTime: TimeSpan.FromMilliseconds(300),
                MakeupGain: 4.0
            );
        }

        // More aggressive for consistent levels
        if (type.Contains("corporate") || type.Contains("promotional"))
        {
            return new CompressionSettings(
                Threshold: -18.0,
                Ratio: 3.5,
                AttackTime: TimeSpan.FromMilliseconds(20),
                ReleaseTime: TimeSpan.FromMilliseconds(250),
                MakeupGain: 6.0
            );
        }

        // Default moderate compression
        return new CompressionSettings(
            Threshold: -18.0,
            Ratio: 3.0,
            AttackTime: TimeSpan.FromMilliseconds(20),
            ReleaseTime: TimeSpan.FromMilliseconds(250),
            MakeupGain: 5.0
        );
    }

    /// <summary>
    /// Analyzes audio levels and detects frequency conflicts
    /// </summary>
    public Task<List<string>> DetectFrequencyConflictsAsync(
        bool hasNarration,
        bool hasMusic,
        bool hasSoundEffects,
        CancellationToken ct = default)
    {
        var conflicts = new List<string>();

        if (hasNarration && hasMusic)
        {
            conflicts.Add("Narration and music may compete in the 200-500Hz range. " +
                         "Consider applying a slight low-cut to music around 150-200Hz.");
        }

        if (hasNarration && hasSoundEffects)
        {
            conflicts.Add("Voice and sound effects may clash in the 2-4kHz presence range. " +
                         "Ensure sound effects are timed to not overlap with important speech.");
        }

        if (hasMusic && hasSoundEffects)
        {
            conflicts.Add("Music and sound effects may compete in the high-mid frequencies. " +
                         "Consider ducking music slightly during sound effect hits.");
        }

        return Task.FromResult(conflicts);
    }

    /// <summary>
    /// Suggests stereo field placement for depth
    /// </summary>
    public Dictionary<string, string> SuggestStereoPlacement(
        bool hasNarration,
        bool hasMusic,
        bool hasSoundEffects)
    {
        var placement = new Dictionary<string, string>();

        if (hasNarration)
        {
            placement["Narration"] = "Center (0% pan) - Keep voice anchored in center for clarity";
        }

        if (hasMusic)
        {
            placement["Music"] = "Wide stereo (100% width) - Full stereo spread for richness";
        }

        if (hasSoundEffects)
        {
            placement["SoundEffects"] = "Positioned based on visual elements - " +
                "UI sounds center, ambient sounds wide, directional sounds positioned appropriately";
        }

        return placement;
    }

    /// <summary>
    /// Validates audio mixing against quality standards
    /// </summary>
    public (bool IsValid, List<string> Issues) ValidateMixing(AudioMixing mixing)
    {
        var issues = new List<string>();

        // Check LUFS target
        if (mixing.TargetLUFS < -18 || mixing.TargetLUFS > -10)
        {
            issues.Add($"Target LUFS {mixing.TargetLUFS:F1} is outside recommended range (-16 to -12). " +
                      "YouTube standard is -14 LUFS.");
        }

        // Check volume balance
        if (mixing.NarrationVolume > 0 && mixing.MusicVolume > mixing.NarrationVolume)
        {
            issues.Add("Music volume exceeds narration volume. This may make speech hard to understand.");
        }

        // Check ducking settings
        if (mixing.NarrationVolume > 0 && mixing.MusicVolume > 0)
        {
            if (Math.Abs(mixing.Ducking.DuckDepthDb) < 8)
            {
                issues.Add("Ducking depth is too shallow. Music may compete with narration. " +
                          "Recommended: -10 to -15dB.");
            }
        }

        // Check EQ settings
        if (mixing.EQ.HighPassFrequency < 60 || mixing.EQ.HighPassFrequency > 120)
        {
            issues.Add($"High-pass frequency {mixing.EQ.HighPassFrequency}Hz is unusual. " +
                      "Recommended: 80-100Hz for voice.");
        }

        return (issues.Count == 0, issues);
    }

    /// <summary>
    /// Generates FFmpeg filter string for the mixing configuration
    /// </summary>
    public string GenerateFFmpegMixingFilter(AudioMixing mixing)
    {
        var filters = new List<string>();

        // High-pass filter
        filters.Add($"highpass=f={mixing.EQ.HighPassFrequency}");

        // Presence boost for voice (around 3-5kHz)
        if (Math.Abs(mixing.EQ.PresenceBoost) > 0.1)
        {
            filters.Add($"equalizer=f=4000:width_type=h:width=2000:g={mixing.EQ.PresenceBoost}");
        }

        // De-esser (reduce 7kHz)
        if (Math.Abs(mixing.EQ.DeEsserReduction) > 0.1)
        {
            filters.Add($"equalizer=f=7000:width_type=h:width=2000:g={mixing.EQ.DeEsserReduction}");
        }

        // Compression
        filters.Add($"acompressor=threshold={mixing.Compression.Threshold}dB:" +
                   $"ratio={mixing.Compression.Ratio}:" +
                   $"attack={mixing.Compression.AttackTime.TotalMilliseconds}:" +
                   $"release={mixing.Compression.ReleaseTime.TotalMilliseconds}:" +
                   $"makeup={mixing.Compression.MakeupGain}dB");

        // Normalization to target LUFS
        if (mixing.Normalize)
        {
            filters.Add($"loudnorm=I={mixing.TargetLUFS}:TP=-1.5:LRA=11");
        }

        return string.Join(",", filters);
    }
}
