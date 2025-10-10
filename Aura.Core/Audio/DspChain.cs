using System;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Provides DSP (Digital Signal Processing) chain for audio processing.
/// Implements: HPF -> De-esser -> Compressor -> Limiter -> LUFS normalization
/// Target loudness: -14 LUFS with peak -1 dBFS (YouTube standard)
/// </summary>
public class DspChain
{
    private readonly ILogger<DspChain> _logger;

    public DspChain(ILogger<DspChain> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies the DSP chain to normalize loudness to target LUFS.
    /// Default: -14 LUFS (YouTube), alternatives: -16 LUFS (voice-only), -12 LUFS (music-forward)
    /// </summary>
    public string BuildDspFilterChain(
        double targetLufs = -14.0,
        double peakCeiling = -1.0,
        bool enableHpf = true,
        bool enableDeEsser = true,
        bool enableCompressor = true,
        bool enableLimiter = true)
    {
        _logger.LogDebug("Building DSP chain: target LUFS={TargetLufs}, peak={PeakCeiling}dBFS", 
            targetLufs, peakCeiling);

        var filters = new System.Collections.Generic.List<string>();

        // Stage 1: High-pass filter (HPF) - removes low-frequency rumble below 80Hz
        if (enableHpf)
        {
            filters.Add("highpass=f=80");
        }

        // Stage 2: De-esser - reduces harsh sibilance in the 6-8kHz range
        if (enableDeEsser)
        {
            // Reduces treble by 3dB centered at 7kHz with 2kHz width
            filters.Add("treble=g=-3:f=7000:w=2000");
        }

        // Stage 3: Compressor - dynamic range compression
        // Threshold: -18dB, Ratio: 3:1, Attack: 20ms, Release: 250ms, Makeup gain: 6dB
        if (enableCompressor)
        {
            filters.Add("acompressor=threshold=-18dB:ratio=3:attack=20:release=250:makeup=6dB");
        }

        // Stage 4: Limiter - prevents peaks from exceeding ceiling
        // Attack: 5ms, Release: 50ms
        if (enableLimiter)
        {
            filters.Add($"alimiter=limit={peakCeiling}dB:attack=5:release=50");
        }

        // Stage 5: LUFS normalization - final loudness target
        // I: Integrated loudness target (LUFS)
        // TP: True peak ceiling (dBFS)
        // LRA: Loudness range target (LU)
        filters.Add($"loudnorm=I={targetLufs}:TP={peakCeiling}:LRA=11");

        string filterChain = string.Join(",", filters);
        _logger.LogInformation("DSP chain built with {StageCount} stages: {FilterChain}", 
            filters.Count, filterChain);

        return filterChain;
    }

    /// <summary>
    /// Validates audio meets loudness specifications.
    /// </summary>
    public bool ValidateLoudness(
        double measuredLufs,
        double measuredPeak,
        double targetLufs,
        double peakCeiling,
        out string? validationMessage,
        double tolerance = 1.0)
    {
        _logger.LogDebug("Validating loudness: measured={MeasuredLufs} LUFS, peak={MeasuredPeak} dBFS", 
            measuredLufs, measuredPeak);

        // Check LUFS is within tolerance of target
        double lufsDiff = Math.Abs(measuredLufs - targetLufs);
        if (lufsDiff > tolerance)
        {
            validationMessage = $"LUFS {measuredLufs:F1} is {lufsDiff:F1}dB away from target {targetLufs:F1} LUFS (tolerance: {tolerance:F1}dB).";
            _logger.LogWarning(validationMessage);
            return false;
        }

        // Check peak does not exceed ceiling
        if (measuredPeak > peakCeiling)
        {
            validationMessage = $"Peak level {measuredPeak:F1} dBFS exceeds ceiling {peakCeiling:F1} dBFS.";
            _logger.LogWarning(validationMessage);
            return false;
        }

        validationMessage = null;
        _logger.LogInformation("Loudness validation passed: {MeasuredLufs:F1} LUFS (target: {TargetLufs:F1}), peak: {MeasuredPeak:F1} dBFS",
            measuredLufs, targetLufs, measuredPeak);
        return true;
    }

    /// <summary>
    /// Gets recommended LUFS target based on content type.
    /// </summary>
    public double GetRecommendedLufs(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "youtube" or "web" or "default" => -14.0,  // YouTube/web standard
            "voice" or "podcast" or "narration" => -16.0,  // Voice-only content
            "music" or "music-forward" => -12.0,  // Music-heavy content
            _ => -14.0  // Default to YouTube standard
        };
    }
}
