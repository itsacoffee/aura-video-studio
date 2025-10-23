using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Aura.Core.Models;
using Aura.Core.Models.Export;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Builder for constructing FFmpeg command line arguments
/// </summary>
public class FFmpegCommandBuilder
{
    private readonly List<string> _inputFiles = new();
    private readonly List<string> _outputOptions = new();
    private readonly List<string> _filterComplex = new();
    private string? _outputFile;
    private bool _overwrite = true;
    private int? _threads;
    private string? _hwaccel;
    private Dictionary<string, string> _metadata = new();

    /// <summary>
    /// Add an input file
    /// </summary>
    public FFmpegCommandBuilder AddInput(string filePath)
    {
        _inputFiles.Add($"-i \"{filePath}\"");
        return this;
    }

    /// <summary>
    /// Set the output file
    /// </summary>
    public FFmpegCommandBuilder SetOutput(string filePath)
    {
        _outputFile = filePath;
        return this;
    }

    /// <summary>
    /// Set whether to overwrite output file
    /// </summary>
    public FFmpegCommandBuilder SetOverwrite(bool overwrite)
    {
        _overwrite = overwrite;
        return this;
    }

    /// <summary>
    /// Set video codec
    /// </summary>
    public FFmpegCommandBuilder SetVideoCodec(string codec)
    {
        _outputOptions.Add($"-c:v {codec}");
        return this;
    }

    /// <summary>
    /// Set audio codec
    /// </summary>
    public FFmpegCommandBuilder SetAudioCodec(string codec)
    {
        _outputOptions.Add($"-c:a {codec}");
        return this;
    }

    /// <summary>
    /// Set video bitrate
    /// </summary>
    public FFmpegCommandBuilder SetVideoBitrate(int bitrateKbps)
    {
        _outputOptions.Add($"-b:v {bitrateKbps}k");
        return this;
    }

    /// <summary>
    /// Set audio bitrate
    /// </summary>
    public FFmpegCommandBuilder SetAudioBitrate(int bitrateKbps)
    {
        _outputOptions.Add($"-b:a {bitrateKbps}k");
        return this;
    }

    /// <summary>
    /// Set output resolution
    /// </summary>
    public FFmpegCommandBuilder SetResolution(int width, int height)
    {
        _outputOptions.Add($"-s {width}x{height}");
        return this;
    }

    /// <summary>
    /// Set frame rate
    /// </summary>
    public FFmpegCommandBuilder SetFrameRate(int fps)
    {
        _outputOptions.Add($"-r {fps}");
        return this;
    }

    /// <summary>
    /// Set pixel format
    /// </summary>
    public FFmpegCommandBuilder SetPixelFormat(string format)
    {
        _outputOptions.Add($"-pix_fmt {format}");
        return this;
    }

    /// <summary>
    /// Set hardware acceleration
    /// </summary>
    public FFmpegCommandBuilder SetHardwareAcceleration(string hwaccel)
    {
        _hwaccel = hwaccel;
        return this;
    }

    /// <summary>
    /// Set number of threads
    /// </summary>
    public FFmpegCommandBuilder SetThreads(int threads)
    {
        _threads = threads;
        return this;
    }

    /// <summary>
    /// Add a video filter
    /// </summary>
    public FFmpegCommandBuilder AddFilter(string filter)
    {
        _filterComplex.Add(filter);
        return this;
    }

    /// <summary>
    /// Add scale filter with proper aspect ratio handling
    /// </summary>
    public FFmpegCommandBuilder AddScaleFilter(int width, int height, string scaleMode = "fit")
    {
        var filter = scaleMode switch
        {
            "fit" => $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2",
            "crop" => $"scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height}",
            "stretch" => $"scale={width}:{height}",
            _ => $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2"
        };
        
        _filterComplex.Add(filter);
        return this;
    }

    /// <summary>
    /// Add fade in effect
    /// </summary>
    public FFmpegCommandBuilder AddFadeIn(double durationSeconds)
    {
        _filterComplex.Add($"fade=t=in:st=0:d={durationSeconds.ToString(CultureInfo.InvariantCulture)}");
        return this;
    }

    /// <summary>
    /// Add fade out effect
    /// </summary>
    public FFmpegCommandBuilder AddFadeOut(double startSeconds, double durationSeconds)
    {
        _filterComplex.Add($"fade=t=out:st={startSeconds.ToString(CultureInfo.InvariantCulture)}:d={durationSeconds.ToString(CultureInfo.InvariantCulture)}");
        return this;
    }

    /// <summary>
    /// Set encoding preset (ultrafast, fast, medium, slow, slower)
    /// </summary>
    public FFmpegCommandBuilder SetPreset(string preset)
    {
        _outputOptions.Add($"-preset {preset}");
        return this;
    }

    /// <summary>
    /// Set CRF quality (0-51, lower is better quality)
    /// </summary>
    public FFmpegCommandBuilder SetCRF(int crf)
    {
        if (crf < 0 || crf > 51)
        {
            throw new ArgumentOutOfRangeException(nameof(crf), "CRF must be between 0 and 51");
        }
        _outputOptions.Add($"-crf {crf}");
        return this;
    }

    /// <summary>
    /// Add metadata
    /// </summary>
    public FFmpegCommandBuilder AddMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Set start time for trimming
    /// </summary>
    public FFmpegCommandBuilder SetStartTime(TimeSpan time)
    {
        _outputOptions.Add($"-ss {time:hh\\:mm\\:ss\\.fff}");
        return this;
    }

    /// <summary>
    /// Set duration for trimming
    /// </summary>
    public FFmpegCommandBuilder SetDuration(TimeSpan duration)
    {
        _outputOptions.Add($"-t {duration:hh\\:mm\\:ss\\.fff}");
        return this;
    }

    /// <summary>
    /// Set audio sample rate
    /// </summary>
    public FFmpegCommandBuilder SetAudioSampleRate(int sampleRate)
    {
        _outputOptions.Add($"-ar {sampleRate}");
        return this;
    }

    /// <summary>
    /// Set audio channels
    /// </summary>
    public FFmpegCommandBuilder SetAudioChannels(int channels)
    {
        _outputOptions.Add($"-ac {channels}");
        return this;
    }

    /// <summary>
    /// Build the complete FFmpeg command
    /// </summary>
    public string Build()
    {
        if (_outputFile == null)
        {
            throw new InvalidOperationException("Output file must be set");
        }

        var command = new StringBuilder();

        // Add overwrite flag
        if (_overwrite)
        {
            command.Append("-y ");
        }

        // Add hardware acceleration
        if (!string.IsNullOrEmpty(_hwaccel))
        {
            command.Append($"-hwaccel {_hwaccel} ");
        }

        // Add input files
        foreach (var input in _inputFiles)
        {
            command.Append($"{input} ");
        }

        // Add threads
        if (_threads.HasValue)
        {
            command.Append($"-threads {_threads.Value} ");
        }

        // Add filter complex
        if (_filterComplex.Count > 0)
        {
            var filters = string.Join(",", _filterComplex);
            command.Append($"-filter_complex \"{filters}\" ");
        }

        // Add output options
        foreach (var option in _outputOptions)
        {
            command.Append($"{option} ");
        }

        // Add metadata
        foreach (var metadata in _metadata)
        {
            command.Append($"-metadata {metadata.Key}=\"{metadata.Value}\" ");
        }

        // Add output file
        command.Append($"\"{_outputFile}\"");

        return command.ToString();
    }

    /// <summary>
    /// Create a builder from an export preset
    /// </summary>
    public static FFmpegCommandBuilder FromPreset(ExportPreset preset, string inputFile, string outputFile)
    {
        var builder = new FFmpegCommandBuilder()
            .AddInput(inputFile)
            .SetOutput(outputFile)
            .SetVideoCodec(preset.VideoCodec)
            .SetAudioCodec(preset.AudioCodec)
            .SetVideoBitrate(preset.VideoBitrate)
            .SetAudioBitrate(preset.AudioBitrate)
            .SetResolution(preset.Resolution.Width, preset.Resolution.Height)
            .SetFrameRate(preset.FrameRate)
            .SetPixelFormat(preset.PixelFormat);

        // Set encoding preset based on quality
        var encodingPreset = preset.Quality switch
        {
            QualityLevel.Draft => "ultrafast",
            QualityLevel.Good => "fast",
            QualityLevel.High => "medium",
            QualityLevel.Maximum => "slow",
            _ => "medium"
        };
        
        builder.SetPreset(encodingPreset);

        return builder;
    }

    /// <summary>
    /// Create a builder for generating a thumbnail
    /// </summary>
    public static FFmpegCommandBuilder CreateThumbnailCommand(
        string inputFile, 
        string outputFile, 
        TimeSpan position,
        int width = 1280,
        int height = 720)
    {
        return new FFmpegCommandBuilder()
            .AddInput(inputFile)
            .SetOutput(outputFile)
            .SetStartTime(position)
            .AddFilter($"scale={width}:{height}:force_original_aspect_ratio=decrease")
            .SetVideoCodec("mjpeg")
            .AddMetadata("comment", "Generated by Aura Video Studio");
    }
}

/// <summary>
/// Extension methods for common FFmpeg operations
/// </summary>
public static class FFmpegCommandBuilderExtensions
{
    /// <summary>
    /// Configure builder for platform-optimized export
    /// </summary>
    public static FFmpegCommandBuilder ConfigureForPlatform(
        this FFmpegCommandBuilder builder,
        IPlatformExportProfile profile,
        Resolution targetResolution)
    {
        // Set recommended bitrate
        builder.SetVideoBitrate(profile.RecommendedVideoBitrate);
        builder.SetAudioBitrate(profile.RecommendedAudioBitrate);
        
        // Set recommended frame rate
        builder.SetFrameRate(profile.RecommendedFrameRate);
        
        // Add platform-specific optimizations
        builder.AddMetadata("platform", profile.PlatformName);
        
        return builder;
    }
}
