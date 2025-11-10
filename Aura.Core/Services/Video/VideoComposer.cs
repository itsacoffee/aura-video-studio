using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.FFmpeg.Filters;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Video;

/// <summary>
/// Input clip for video composition
/// </summary>
public record VideoClip
{
    public required string FilePath { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string? AudioPath { get; init; }
    public TransitionBuilder.TransitionType? TransitionType { get; init; }
    public double TransitionDuration { get; init; } = 0.5;
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Video composition settings
/// </summary>
public record VideoCompositionSettings
{
    public Resolution Resolution { get; init; } = new Resolution(1920, 1080);
    public int FrameRate { get; init; } = 30;
    public int VideoBitrate { get; init; } = 5000;
    public int AudioBitrate { get; init; } = 192;
    public string VideoCodec { get; init; } = "libx264";
    public string AudioCodec { get; init; } = "aac";
    public string Preset { get; init; } = "medium";
    public int Crf { get; init; } = 23;
    public string PixelFormat { get; init; } = "yuv420p";
    public bool UseHardwareAcceleration { get; init; } = true;
    public string? SubtitlePath { get; init; }
    public string? BackgroundMusicPath { get; init; }
    public double BackgroundMusicVolume { get; init; } = 0.3;
    public bool NormalizeAudio { get; init; } = true;
}

/// <summary>
/// Service for composing multiple video clips into a final video
/// </summary>
public interface IVideoComposer
{
    /// <summary>
    /// Compose multiple clips into a single video with transitions
    /// </summary>
    Task<string> ComposeAsync(
        IEnumerable<VideoClip> clips,
        string outputPath,
        VideoCompositionSettings settings,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge video with separate audio track
    /// </summary>
    Task<string> MergeVideoAudioAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add subtitles to a video
    /// </summary>
    Task<string> AddSubtitlesAsync(
        string videoPath,
        string subtitlePath,
        string outputPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of video composer using FFmpeg
/// </summary>
public class VideoComposer : IVideoComposer
{
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly IHardwareAccelerationDetector _hardwareDetector;
    private readonly ILogger<VideoComposer> _logger;

    public VideoComposer(
        IFFmpegExecutor ffmpegExecutor,
        IHardwareAccelerationDetector hardwareDetector,
        ILogger<VideoComposer> logger)
    {
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
        _hardwareDetector = hardwareDetector ?? throw new ArgumentNullException(nameof(hardwareDetector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ComposeAsync(
        IEnumerable<VideoClip> clips,
        string outputPath,
        VideoCompositionSettings settings,
        Action<double>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var clipList = clips.ToList();
        if (clipList.Count == 0)
        {
            throw new ArgumentException("At least one clip must be provided", nameof(clips));
        }

        _logger.LogInformation(
            "Composing {Count} clips into video: {Output}",
            clipList.Count,
            outputPath
        );

        // Validate all input files exist
        foreach (var clip in clipList)
        {
            if (!File.Exists(clip.FilePath))
            {
                throw new FileNotFoundException($"Clip file not found: {clip.FilePath}");
            }
        }

        // Detect hardware acceleration if enabled
        HardwareAccelerationInfo? hwInfo = null;
        if (settings.UseHardwareAcceleration)
        {
            hwInfo = await _hardwareDetector.DetectAsync(cancellationToken).ConfigureAwait(false);
            if (hwInfo.IsAvailable)
            {
                _logger.LogInformation(
                    "Using hardware acceleration: {Type} with codec {Codec}",
                    hwInfo.AccelerationType,
                    hwInfo.VideoCodec
                );
            }
        }

        // Build FFmpeg command
        var builder = new FFmpegCommandBuilder();

        // Add all input clips
        foreach (var clip in clipList)
        {
            builder.AddInput(clip.FilePath);
        }

        // Configure hardware acceleration
        if (hwInfo?.IsAvailable == true)
        {
            builder.SetHardwareAcceleration(hwInfo.HwaccelDevice);
            builder.SetVideoCodec(hwInfo.VideoCodec);
        }
        else
        {
            builder.SetVideoCodec(settings.VideoCodec);
        }

        // Set output format
        builder
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetResolution(settings.Resolution.Width, settings.Resolution.Height)
            .SetFrameRate(settings.FrameRate)
            .SetAudioCodec(settings.AudioCodec)
            .SetAudioBitrate(settings.AudioBitrate)
            .SetPixelFormat(settings.PixelFormat);

        // Set encoding quality
        if (hwInfo?.IsAvailable != true)
        {
            builder.SetPreset(settings.Preset);
            builder.SetCRF(settings.Crf);
        }
        else
        {
            builder.SetVideoBitrate(settings.VideoBitrate);
        }

        // Build transitions filter if multiple clips
        if (clipList.Count > 1)
        {
            var filterComplex = BuildTransitionFilter(clipList);
            builder.AddFilter(filterComplex);
        }
        else
        {
            // Single clip - just scale to target resolution
            builder.AddScaleFilter(
                settings.Resolution.Width,
                settings.Resolution.Height,
                "fit"
            );
        }

        // Add background music if specified
        if (!string.IsNullOrEmpty(settings.BackgroundMusicPath) && File.Exists(settings.BackgroundMusicPath))
        {
            builder.AddInput(settings.BackgroundMusicPath);
            // Audio mixing will be handled in the filter
            var audioFilter = $"[0:a]volume=1.0[voice];[{clipList.Count}:a]volume={settings.BackgroundMusicVolume}[music];[voice][music]amix=inputs=2:duration=shortest[aout]";
            builder.AddFilter(audioFilter);
        }

        // Execute composition
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
            timeout: TimeSpan.FromMinutes(120),
            cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogError(
                "Video composition failed: {Error}",
                result.ErrorMessage
            );
            throw new InvalidOperationException($"Video composition failed: {result.ErrorMessage}");
        }

        // Add subtitles if specified
        if (!string.IsNullOrEmpty(settings.SubtitlePath) && File.Exists(settings.SubtitlePath))
        {
            var tempOutput = outputPath + ".temp.mp4";
            File.Move(outputPath, tempOutput);
            await AddSubtitlesAsync(tempOutput, settings.SubtitlePath, outputPath, cancellationToken);
            File.Delete(tempOutput);
        }

        _logger.LogInformation("Video composition completed: {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> MergeVideoAudioAsync(
        string videoPath,
        string audioPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Video file not found", videoPath);
        }

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("Audio file not found", audioPath);
        }

        _logger.LogInformation(
            "Merging video {Video} with audio {Audio}",
            videoPath,
            audioPath
        );

        var builder = new FFmpegCommandBuilder()
            .AddInput(videoPath)
            .AddInput(audioPath)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetVideoCodec("copy") // No re-encoding
            .SetAudioCodec("aac")
            .SetAudioBitrate(192);

        // Map video from first input, audio from second
        builder.AddFilter("[0:v:0][1:a:0]");

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Merge failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Merge completed: {Output}", outputPath);
        return outputPath;
    }

    public async Task<string> AddSubtitlesAsync(
        string videoPath,
        string subtitlePath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Video file not found", videoPath);
        }

        if (!File.Exists(subtitlePath))
        {
            throw new FileNotFoundException("Subtitle file not found", subtitlePath);
        }

        _logger.LogInformation(
            "Adding subtitles {Subtitle} to video {Video}",
            subtitlePath,
            videoPath
        );

        // Escape subtitle path for FFmpeg filter
        var escapedSubPath = subtitlePath.Replace("\\", "\\\\").Replace(":", "\\:");

        var builder = new FFmpegCommandBuilder()
            .AddInput(videoPath)
            .SetOutput(outputPath)
            .SetOverwrite(true)
            .SetVideoCodec("libx264")
            .SetAudioCodec("copy") // Don't re-encode audio
            .SetPreset("fast")
            .AddFilter($"subtitles='{escapedSubPath}'");

        var result = await _ffmpegExecutor.ExecuteCommandAsync(
            builder,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Add subtitles failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Subtitles added: {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Build complex filter for transitions between clips
    /// </summary>
    private string BuildTransitionFilter(List<VideoClip> clips)
    {
        if (clips.Count == 1)
        {
            return "[0:v]scale=1920:1080[vout]";
        }

        var filterParts = new List<string>();
        double currentOffset = 0;

        // Scale all inputs first
        for (int i = 0; i < clips.Count; i++)
        {
            filterParts.Add($"[{i}:v]scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2[v{i}]");
            if (clips[i].Duration > TimeSpan.Zero)
            {
                currentOffset += clips[i].Duration.TotalSeconds;
            }
        }

        // Build transitions
        currentOffset = clips[0].Duration.TotalSeconds;
        string lastOutput = "v0";

        for (int i = 0; i < clips.Count - 1; i++)
        {
            var transitionType = clips[i + 1].TransitionType ?? TransitionBuilder.TransitionType.Fade;
            var transitionDuration = clips[i + 1].TransitionDuration;
            var offset = currentOffset - transitionDuration;

            var transition = TransitionBuilder.BuildCrossfade(
                transitionDuration,
                offset,
                transitionType
            );

            var nextInput = $"v{i + 1}";
            var outputLabel = i == clips.Count - 2 ? "vout" : $"vt{i}";

            filterParts.Add($"[{lastOutput}][{nextInput}]{transition}[{outputLabel}]");

            lastOutput = outputLabel;
            if (i + 1 < clips.Count)
            {
                currentOffset += clips[i + 1].Duration.TotalSeconds;
            }
        }

        return string.Join(";", filterParts);
    }
}
