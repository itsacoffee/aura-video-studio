using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Video;

/// <summary>
/// Audio track for mixing
/// </summary>
public record AudioTrack
{
    public required string FilePath { get; init; }
    public double Volume { get; init; } = 1.0; // 0.0 to 2.0+
    public TimeSpan StartTime { get; init; } = TimeSpan.Zero;
    public TimeSpan? Duration { get; init; }
    public TimeSpan Delay { get; init; } = TimeSpan.Zero;
    public bool Loop { get; init; }
    public bool FadeIn { get; init; }
    public TimeSpan FadeInDuration { get; init; } = TimeSpan.FromSeconds(1);
    public bool FadeOut { get; init; }
    public TimeSpan FadeOutDuration { get; init; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Audio mixing settings
/// </summary>
public record AudioMixingSettings
{
    public int SampleRate { get; init; } = 48000;
    public int Channels { get; init; } = 2; // Stereo
    public int Bitrate { get; init; } = 192; // kbps
    public string Codec { get; init; } = "aac";
    public bool Normalize { get; init; } = true;
    public double NormalizationTarget { get; init; } = -16.0; // LUFS
    public bool EnableDucking { get; init; }
    public int ForegroundTrackIndex { get; init; }  // Voice track for ducking
    public double DuckingThreshold { get; init; } = -20.0;
    public double DuckingRatio { get; init; } = 4.0;
}

/// <summary>
/// Service for mixing multiple audio tracks with voice and music
/// </summary>
public interface IAudioMixer
{
    /// <summary>
    /// Mix multiple audio tracks into a single output file
    /// </summary>
    Task<string> MixAsync(
        IEnumerable<AudioTrack> tracks,
        string outputPath,
        AudioMixingSettings settings,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalize audio file to target loudness
    /// </summary>
    Task<string> NormalizeAsync(
        string inputPath,
        string outputPath,
        double targetLufs = -16.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply ducking to lower background music when voice is present
    /// </summary>
    Task<string> ApplyDuckingAsync(
        string voicePath,
        string musicPath,
        string outputPath,
        double threshold = -20.0,
        double ratio = 4.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Concatenate multiple audio files
    /// </summary>
    Task<string> ConcatenateAsync(
        IEnumerable<string> audioPaths,
        string outputPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of audio mixer using FFmpeg
/// </summary>
public class AudioMixer : IAudioMixer
{
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly ILogger<AudioMixer> _logger;

    public AudioMixer(
        IFFmpegExecutor ffmpegExecutor,
        ILogger<AudioMixer> logger)
    {
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> MixAsync(
        IEnumerable<AudioTrack> tracks,
        string outputPath,
        AudioMixingSettings settings,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var trackList = tracks.ToList();
        if (trackList.Count == 0)
        {
            throw new ArgumentException("At least one audio track must be provided", nameof(tracks));
        }

        _logger.LogInformation(
            "Mixing {Count} audio tracks into: {Output}",
            trackList.Count,
            outputPath
        );

        // Validate all input files exist
        foreach (var track in trackList)
        {
            if (!File.Exists(track.FilePath))
            {
                throw new FileNotFoundException($"Audio file not found: {track.FilePath}");
            }
        }

        var builder = new FFmpegCommandBuilder();

        // Add all input tracks
        foreach (var track in trackList)
        {
            builder.AddInput(track.FilePath);
        }

        // Build filter complex for mixing
        var filterComplex = BuildMixingFilter(trackList, settings);
        builder.AddFilter(filterComplex);

        // Set output format
        builder
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetAudioCodec(settings.Codec)
            .SetAudioBitrate(settings.Bitrate)
            .SetAudioSampleRate(settings.SampleRate)
            .SetAudioChannels(settings.Channels);

        // Execute mixing
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
            timeout: TimeSpan.FromMinutes(30),
            cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogError("Audio mixing failed: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Audio mixing failed: {result.ErrorMessage}");
        }

        // Normalize if requested
        if (settings.Normalize)
        {
            var tempOutput = outputPath + ".temp.m4a";
            File.Move(outputPath, tempOutput);
            await NormalizeAsync(tempOutput, outputPath, settings.NormalizationTarget, cancellationToken)
                .ConfigureAwait(false);
            File.Delete(tempOutput);
        }

        _logger.LogInformation("Audio mixing completed: {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> NormalizeAsync(
        string inputPath,
        string outputPath,
        double targetLufs = -16.0,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Audio file not found", inputPath);
        }

        _logger.LogInformation(
            "Normalizing audio {Input} to {Target} LUFS",
            inputPath,
            targetLufs
        );

        var targetStr = targetLufs.ToString("F1", CultureInfo.InvariantCulture);

        var builder = new FFmpegCommandBuilder()
            .AddInput(inputPath)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetAudioCodec("aac")
            .AddFilter($"loudnorm=I={targetStr}:TP=-1.5:LRA=11");

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Normalization failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Audio normalization completed: {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> ApplyDuckingAsync(
        string voicePath,
        string musicPath,
        string outputPath,
        double threshold = -20.0,
        double ratio = 4.0,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(voicePath))
        {
            throw new FileNotFoundException("Voice file not found", voicePath);
        }

        if (!File.Exists(musicPath))
        {
            throw new FileNotFoundException("Music file not found", musicPath);
        }

        _logger.LogInformation(
            "Applying ducking: voice={Voice}, music={Music}",
            voicePath,
            musicPath
        );

        var thresholdStr = threshold.ToString("F1", CultureInfo.InvariantCulture);
        var ratioStr = ratio.ToString("F1", CultureInfo.InvariantCulture);

        var builder = new FFmpegCommandBuilder()
            .AddInput(musicPath)
            .AddInput(voicePath)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetAudioCodec("aac")
            .SetAudioBitrate(192)
            .AddFilter($"[0:a][1:a]sidechaincompress=threshold={thresholdStr}dB:ratio={ratioStr}:attack=20:release=250[aout]");

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Ducking failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Ducking completed: {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> ConcatenateAsync(
        IEnumerable<string> audioPaths,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var pathList = audioPaths.ToList();
        if (pathList.Count == 0)
        {
            throw new ArgumentException("At least one audio file must be provided", nameof(audioPaths));
        }

        _logger.LogInformation(
            "Concatenating {Count} audio files into: {Output}",
            pathList.Count,
            outputPath
        );

        // Validate all input files exist
        foreach (var path in pathList)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Audio file not found: {path}");
            }
        }

        var builder = new FFmpegCommandBuilder();

        // Add all inputs
        foreach (var path in pathList)
        {
            builder.AddInput(path);
        }

        // Build concat filter
        var inputsString = string.Join("", Enumerable.Range(0, pathList.Count).Select(i => $"[{i}:a]"));
        var concatFilter = $"{inputsString}concat=n={pathList.Count}:v=0:a=1[aout]";

        builder
            .AddFilter(concatFilter)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetAudioCodec("aac")
            .SetAudioBitrate(192);

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Concatenation failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Audio concatenation completed: {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Build complex filter for mixing multiple tracks
    /// </summary>
    private string BuildMixingFilter(List<AudioTrack> tracks, AudioMixingSettings settings)
    {
        var filterParts = new List<string>();

        // Process each track with volume, fade, and timing adjustments
        for (int i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var filters = new List<string>();

            // Delay
            if (track.Delay > TimeSpan.Zero)
            {
                var delayMs = (int)track.Delay.TotalMilliseconds;
                filters.Add($"adelay={delayMs}|{delayMs}");
            }

            // Volume
            if (Math.Abs(track.Volume - 1.0) > 0.01)
            {
                var volumeStr = track.Volume.ToString("F3", CultureInfo.InvariantCulture);
                filters.Add($"volume={volumeStr}");
            }

            // Fade in
            if (track.FadeIn)
            {
                var fadeInSeconds = track.FadeInDuration.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
                filters.Add($"afade=t=in:st=0:d={fadeInSeconds}");
            }

            // Fade out (needs duration knowledge - apply at end)
            if (track.FadeOut && track.Duration.HasValue)
            {
                var fadeOutStart = (track.Duration.Value - track.FadeOutDuration).TotalSeconds;
                var fadeOutDuration = track.FadeOutDuration.TotalSeconds;
                var fadeOutStartStr = fadeOutStart.ToString("F3", CultureInfo.InvariantCulture);
                var fadeOutDurationStr = fadeOutDuration.ToString("F3", CultureInfo.InvariantCulture);
                filters.Add($"afade=t=out:st={fadeOutStartStr}:d={fadeOutDurationStr}");
            }

            // Loop (if needed)
            if (track.Loop)
            {
                filters.Add("aloop=loop=-1:size=2e+09");
            }

            var filterString = filters.Count > 0 ? string.Join(",", filters) : "anull";
            filterParts.Add($"[{i}:a]{filterString}[a{i}]");
        }

        // Apply ducking if enabled
        if (settings.EnableDucking && tracks.Count >= 2)
        {
            var foregroundIndex = Math.Clamp(settings.ForegroundTrackIndex, 0, tracks.Count - 1);
            var backgroundIndices = Enumerable.Range(0, tracks.Count)
                .Where(i => i != foregroundIndex)
                .ToList();

            // For now, apply ducking between foreground and first background track
            if (backgroundIndices.Count > 0)
            {
                var thresholdStr = settings.DuckingThreshold.ToString("F1", CultureInfo.InvariantCulture);
                var ratioStr = settings.DuckingRatio.ToString("F1", CultureInfo.InvariantCulture);
                
                var bgIndex = backgroundIndices[0];
                filterParts.Add($"[a{bgIndex}][a{foregroundIndex}]sidechaincompress=threshold={thresholdStr}dB:ratio={ratioStr}:attack=20:release=250[ducked]");

                // Mix ducked background with foreground
                var mixInputs = $"[ducked][a{foregroundIndex}]";
                if (backgroundIndices.Count > 1)
                {
                    // Add remaining backgrounds
                    foreach (var idx in backgroundIndices.Skip(1))
                    {
                        mixInputs += $"[a{idx}]";
                    }
                }
                filterParts.Add($"{mixInputs}amix=inputs={backgroundIndices.Count + 1}:duration=longest[aout]");
            }
            else
            {
                // Just output foreground
                filterParts.Add($"[a{foregroundIndex}]anull[aout]");
            }
        }
        else
        {
            // Simple mix without ducking
            var mixInputs = string.Join("", Enumerable.Range(0, tracks.Count).Select(i => $"[a{i}]"));
            filterParts.Add($"{mixInputs}amix=inputs={tracks.Count}:duration=longest[aout]");
        }

        return string.Join(";", filterParts);
    }
}
