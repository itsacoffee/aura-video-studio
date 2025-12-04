using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Comprehensive voice enhancement service that analyzes voice audio
/// and applies professional-grade enhancements via FFmpeg filters.
/// </summary>
public class VoiceEnhancementService : IVoiceEnhancementService
{
    private readonly ILogger<VoiceEnhancementService> _logger;
    private readonly IFFmpegService _ffmpegService;

    private static readonly Dictionary<VoiceEnhancementPreset, VoiceEnhancementOptions> Presets = new()
    {
        [VoiceEnhancementPreset.Light] = new VoiceEnhancementOptions(
            EnableNoiseReduction: true,
            NoiseReductionStrength: 0.3,
            EnableHighPassFilter: true,
            HighPassFrequency: 80,
            EnableLowPassFilter: false,
            LowPassFrequency: 18000,
            EnableCompression: false,
            CompressionThreshold: -18,
            CompressionRatio: 2,
            CompressionAttack: 20,
            CompressionRelease: 200,
            CompressionMakeup: 0,
            EnableEQ: false,
            PresenceBoost: 0,
            BassRolloff: 0,
            DeEsserReduction: 0,
            EnableLoudnessNormalization: true,
            TargetLUFS: -16,
            TargetTruePeak: -1.5,
            EnableDeClip: false,
            EnableDeClick: true
        ),
        [VoiceEnhancementPreset.Standard] = new VoiceEnhancementOptions(
            EnableNoiseReduction: true,
            NoiseReductionStrength: 0.5,
            EnableHighPassFilter: true,
            HighPassFrequency: 80,
            EnableLowPassFilter: true,
            LowPassFrequency: 16000,
            EnableCompression: true,
            CompressionThreshold: -20,
            CompressionRatio: 3,
            CompressionAttack: 15,
            CompressionRelease: 150,
            CompressionMakeup: 3,
            EnableEQ: true,
            PresenceBoost: 2,
            BassRolloff: 0,
            DeEsserReduction: -3,
            EnableLoudnessNormalization: true,
            TargetLUFS: -14,
            TargetTruePeak: -1.5,
            EnableDeClip: true,
            EnableDeClick: true
        ),
        [VoiceEnhancementPreset.Broadcast] = new VoiceEnhancementOptions(
            EnableNoiseReduction: true,
            NoiseReductionStrength: 0.7,
            EnableHighPassFilter: true,
            HighPassFrequency: 100,
            EnableLowPassFilter: true,
            LowPassFrequency: 15000,
            EnableCompression: true,
            CompressionThreshold: -18,
            CompressionRatio: 4,
            CompressionAttack: 10,
            CompressionRelease: 100,
            CompressionMakeup: 5,
            EnableEQ: true,
            PresenceBoost: 3,
            BassRolloff: 2,
            DeEsserReduction: -4,
            EnableLoudnessNormalization: true,
            TargetLUFS: -14,
            TargetTruePeak: -1,
            EnableDeClip: true,
            EnableDeClick: true
        ),
        [VoiceEnhancementPreset.Podcast] = new VoiceEnhancementOptions(
            EnableNoiseReduction: true,
            NoiseReductionStrength: 0.6,
            EnableHighPassFilter: true,
            HighPassFrequency: 80,
            EnableLowPassFilter: true,
            LowPassFrequency: 16000,
            EnableCompression: true,
            CompressionThreshold: -22,
            CompressionRatio: 3.5,
            CompressionAttack: 12,
            CompressionRelease: 120,
            CompressionMakeup: 4,
            EnableEQ: true,
            PresenceBoost: 2.5,
            BassRolloff: 1,
            DeEsserReduction: -3.5,
            EnableLoudnessNormalization: true,
            TargetLUFS: -16,
            TargetTruePeak: -1.5,
            EnableDeClip: true,
            EnableDeClick: true
        ),
        [VoiceEnhancementPreset.VideoNarration] = new VoiceEnhancementOptions(
            EnableNoiseReduction: true,
            NoiseReductionStrength: 0.55,
            EnableHighPassFilter: true,
            HighPassFrequency: 85,
            EnableLowPassFilter: true,
            LowPassFrequency: 16000,
            EnableCompression: true,
            CompressionThreshold: -20,
            CompressionRatio: 3,
            CompressionAttack: 15,
            CompressionRelease: 140,
            CompressionMakeup: 3,
            EnableEQ: true,
            PresenceBoost: 3,
            BassRolloff: 1.5,
            DeEsserReduction: -3,
            EnableLoudnessNormalization: true,
            TargetLUFS: -14,
            TargetTruePeak: -1.5,
            EnableDeClip: true,
            EnableDeClick: true
        )
    };

    public VoiceEnhancementService(
        ILogger<VoiceEnhancementService> logger,
        IFFmpegService ffmpegService)
    {
        _logger = logger;
        _ffmpegService = ffmpegService;
    }

    /// <inheritdoc />
    public async Task<VoiceAnalysis> AnalyzeVoiceAsync(
        string audioPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing voice audio: {Path}", audioPath);

        if (!File.Exists(audioPath))
            throw new FileNotFoundException("Audio file not found", audioPath);

        var stopwatch = Stopwatch.StartNew();

        // Get duration
        var duration = await GetDurationAsync(audioPath, ct).ConfigureAwait(false);

        // Analyze loudness with loudnorm
        var (integratedLufs, truePeak, loudnessRange) = await AnalyzeLoudnessAsync(audioPath, ct).ConfigureAwait(false);

        // Estimate noise floor
        var noiseFloor = await EstimateNoiseFloorAsync(audioPath, ct).ConfigureAwait(false);

        stopwatch.Stop();

        var hasClipping = truePeak > -0.5;
        var hasExcessiveNoise = noiseFloor > -45;
        var needsNormalization = integratedLufs < -20 || integratedLufs > -10;

        var issues = new List<string>();
        var recommendations = new List<string>();

        if (hasClipping)
        {
            issues.Add("Audio clipping detected (true peak > -0.5 dBTP)");
            recommendations.Add("Enable de-clipping or reduce input gain");
        }

        if (hasExcessiveNoise)
        {
            issues.Add($"High noise floor detected ({noiseFloor:F1} dB)");
            recommendations.Add("Enable noise reduction with strength 0.6 or higher");
        }

        if (needsNormalization)
        {
            issues.Add($"Loudness outside recommended range ({integratedLufs:F1} LUFS)");
            recommendations.Add("Enable loudness normalization to -14 LUFS");
        }

        if (loudnessRange > 12)
        {
            issues.Add($"High dynamic range ({loudnessRange:F1} LU)");
            recommendations.Add("Enable compression to reduce dynamic range");
        }

        _logger.LogInformation(
            "Voice analysis complete in {ElapsedMs}ms. LUFS: {LUFS:F1}, True Peak: {Peak:F1}, Issues: {Count}",
            stopwatch.ElapsedMilliseconds, integratedLufs, truePeak, issues.Count);

        return new VoiceAnalysis(
            Duration: duration,
            AverageLoudness: integratedLufs,
            IntegratedLoudness: integratedLufs,
            TruePeak: truePeak,
            LoudnessRange: loudnessRange,
            NoiseFloor: noiseFloor,
            HasClipping: hasClipping,
            HasExcessiveNoise: hasExcessiveNoise,
            NeedsNormalization: needsNormalization,
            Issues: issues,
            Recommendations: recommendations
        );
    }

    /// <inheritdoc />
    public async Task<VoiceEnhancementResult> EnhanceVoiceAsync(
        string audioPath,
        string outputPath,
        VoiceEnhancementOptions options,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Enhancing voice audio: {Path}", audioPath);

        if (!File.Exists(audioPath))
            throw new FileNotFoundException("Audio file not found", audioPath);

        var stopwatch = Stopwatch.StartNew();

        // Analyze before enhancement
        var beforeAnalysis = await AnalyzeVoiceAsync(audioPath, ct).ConfigureAwait(false);

        // Build filter chain
        var filterChain = BuildFilterChain(options);
        var appliedEnhancements = GetAppliedEnhancements(options);

        _logger.LogDebug("Applying filter chain: {Filter}", filterChain);

        // Apply enhancements
        var arguments = $"-i \"{audioPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 -y \"{outputPath}\"";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        stopwatch.Stop();

        if (!result.Success || !File.Exists(outputPath))
        {
            return new VoiceEnhancementResult(
                OutputPath: outputPath,
                AppliedEnhancements: appliedEnhancements,
                BeforeAnalysis: beforeAnalysis,
                AfterAnalysis: null,
                ProcessingTime: stopwatch.Elapsed,
                Success: false,
                ErrorMessage: result.ErrorMessage ?? "Enhancement failed"
            );
        }

        // Analyze after enhancement
        VoiceAnalysis? afterAnalysis = null;
        try
        {
            afterAnalysis = await AnalyzeVoiceAsync(outputPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze enhanced audio");
        }

        _logger.LogInformation(
            "Voice enhancement complete in {ElapsedMs}ms. Applied {Count} enhancements.",
            stopwatch.ElapsedMilliseconds, appliedEnhancements.Count);

        return new VoiceEnhancementResult(
            OutputPath: outputPath,
            AppliedEnhancements: appliedEnhancements,
            BeforeAnalysis: beforeAnalysis,
            AfterAnalysis: afterAnalysis,
            ProcessingTime: stopwatch.Elapsed,
            Success: true,
            ErrorMessage: null
        );
    }

    /// <inheritdoc />
    public VoiceEnhancementOptions GetPreset(VoiceEnhancementPreset preset)
    {
        return Presets.TryGetValue(preset, out var options)
            ? options
            : Presets[VoiceEnhancementPreset.Standard];
    }

    /// <inheritdoc />
    public string BuildFilterChain(VoiceEnhancementOptions options)
    {
        var filters = new List<string>();

        // 1. De-clipping (if enabled)
        if (options.EnableDeClip)
        {
            filters.Add("adeclip");
        }

        // 2. De-clicking (if enabled)
        if (options.EnableDeClick)
        {
            filters.Add("adeclick");
        }

        // 3. Noise reduction (if enabled)
        if (options.EnableNoiseReduction)
        {
            var nf = -25 + (options.NoiseReductionStrength * 15); // -25 to -10 range
            filters.Add($"afftdn=nf={nf:F0}");
        }

        // 4. High-pass filter (if enabled)
        if (options.EnableHighPassFilter)
        {
            filters.Add($"highpass=f={options.HighPassFrequency}:poles=2");
        }

        // 5. Low-pass filter (if enabled)
        if (options.EnableLowPassFilter)
        {
            filters.Add($"lowpass=f={options.LowPassFrequency}:poles=2");
        }

        // 6. EQ (if enabled)
        if (options.EnableEQ)
        {
            // Bass rolloff
            if (Math.Abs(options.BassRolloff) > 0.1)
            {
                filters.Add($"equalizer=f=200:width_type=o:width=1:g={-options.BassRolloff:F1}");
            }

            // Presence boost (3-5kHz)
            if (Math.Abs(options.PresenceBoost) > 0.1)
            {
                filters.Add($"equalizer=f=4000:width_type=o:width=1.5:g={options.PresenceBoost:F1}");
            }

            // De-esser (6-8kHz)
            if (Math.Abs(options.DeEsserReduction) > 0.1)
            {
                filters.Add($"equalizer=f=7000:width_type=o:width=1:g={options.DeEsserReduction:F1}");
            }
        }

        // 7. Compression (if enabled)
        if (options.EnableCompression)
        {
            filters.Add($"acompressor=threshold={options.CompressionThreshold}dB:" +
                $"ratio={options.CompressionRatio}:" +
                $"attack={options.CompressionAttack}:" +
                $"release={options.CompressionRelease}:" +
                $"makeup={options.CompressionMakeup}dB:" +
                $"knee=2:mix=1:detection=rms:link=average");
        }

        // 8. Loudness normalization (if enabled) - should be last
        if (options.EnableLoudnessNormalization)
        {
            filters.Add($"loudnorm=I={options.TargetLUFS}:TP={options.TargetTruePeak}:LRA=11");
        }

        return string.Join(",", filters);
    }

    private List<string> GetAppliedEnhancements(VoiceEnhancementOptions options)
    {
        var enhancements = new List<string>();

        if (options.EnableDeClip) enhancements.Add("De-clipping");
        if (options.EnableDeClick) enhancements.Add("De-clicking");
        if (options.EnableNoiseReduction) enhancements.Add($"Noise reduction ({options.NoiseReductionStrength:P0})");
        if (options.EnableHighPassFilter) enhancements.Add($"High-pass filter ({options.HighPassFrequency} Hz)");
        if (options.EnableLowPassFilter) enhancements.Add($"Low-pass filter ({options.LowPassFrequency} Hz)");
        if (options.EnableEQ) enhancements.Add("Equalization");
        if (options.EnableCompression) enhancements.Add($"Compression ({options.CompressionRatio}:1)");
        if (options.EnableLoudnessNormalization) enhancements.Add($"Loudness normalization ({options.TargetLUFS} LUFS)");

        return enhancements;
    }

    private async Task<TimeSpan> GetDurationAsync(string path, CancellationToken ct)
    {
        var arguments = $"-i \"{path}\" -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var match = Regex.Match(result.StandardError ?? "", @"Duration:\s*(\d+):(\d+):(\d+\.\d+)");
        if (match.Success)
        {
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = double.Parse(match.Groups[3].Value);
            return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromMinutes(1);
    }

    private async Task<(double integratedLufs, double truePeak, double loudnessRange)> AnalyzeLoudnessAsync(
        string path, CancellationToken ct)
    {
        var arguments = $"-i \"{path}\" -af loudnorm=print_format=summary -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var output = result.StandardError ?? "";

        double integratedLufs = -23;
        double truePeak = -1;
        double loudnessRange = 7;

        var iMatch = Regex.Match(output, @"Input Integrated:\s*([-\d.]+)\s*LUFS");
        if (iMatch.Success)
            integratedLufs = double.Parse(iMatch.Groups[1].Value);

        var tpMatch = Regex.Match(output, @"Input True Peak:\s*([-\d.]+)\s*dBTP");
        if (tpMatch.Success)
            truePeak = double.Parse(tpMatch.Groups[1].Value);

        var lraMatch = Regex.Match(output, @"Input LRA:\s*([\d.]+)\s*LU");
        if (lraMatch.Success)
            loudnessRange = double.Parse(lraMatch.Groups[1].Value);

        return (integratedLufs, truePeak, loudnessRange);
    }

    private async Task<double> EstimateNoiseFloorAsync(string path, CancellationToken ct)
    {
        // Use silencedetect to find the quietest parts
        var arguments = $"-i \"{path}\" -af silencedetect=noise=-50dB:d=0.1 -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var output = result.StandardError ?? "";

        // Count silence detections - if many, noise floor is low
        var silenceCount = Regex.Matches(output, @"silence_start").Count;

        // Estimate based on silence count and audio properties
        return silenceCount > 5 ? -55 : silenceCount > 2 ? -45 : -35;
    }
}
