using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Media;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service for extracting metadata from media files
/// </summary>
public interface IMediaMetadataService
{
    Task<MediaMetadata?> ExtractMetadataAsync(
        string filePath,
        MediaType mediaType,
        CancellationToken ct = default);

    Task<MediaMetadata?> ExtractMetadataFromStreamAsync(
        Stream stream,
        MediaType mediaType,
        string fileName,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of media metadata service
/// </summary>
public class MediaMetadataService : IMediaMetadataService
{
    private readonly ILogger<MediaMetadataService> _logger;
    private readonly string _ffprobePath;

    public MediaMetadataService(ILogger<MediaMetadataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffprobePath = FindFfprobe() ?? "ffprobe";
    }

    public async Task<MediaMetadata?> ExtractMetadataAsync(
        string filePath,
        MediaType mediaType,
        CancellationToken ct = default)
    {
        try
        {
            return mediaType switch
            {
                MediaType.Image => await ExtractImageMetadataAsync(filePath, ct).ConfigureAwait(false),
                MediaType.Video => await ExtractVideoMetadataAsync(filePath, ct).ConfigureAwait(false),
                MediaType.Audio => await ExtractAudioMetadataAsync(filePath, ct).ConfigureAwait(false),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<MediaMetadata?> ExtractMetadataFromStreamAsync(
        Stream stream,
        MediaType mediaType,
        string fileName,
        CancellationToken ct = default)
    {
        try
        {
            // Save to temp file for extraction
            var tempPath = Path.Combine(Path.GetTempPath(), $"metadata_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
            
            try
            {
                using (var fileStream = File.Create(tempPath))
                {
                    await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                }

                return await ExtractMetadataAsync(tempPath, mediaType, ct).ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from stream: {FileName}", fileName);
            return null;
        }
    }

    private async Task<MediaMetadata?> ExtractImageMetadataAsync(string filePath, CancellationToken ct)
    {
        try
        {
            using var image = await Image.LoadAsync(filePath, ct).ConfigureAwait(false);
            
            return new MediaMetadata
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                ColorSpace = image.PixelType.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract image metadata: {FilePath}", filePath);
            return null;
        }
    }

    private async Task<MediaMetadata?> ExtractVideoMetadataAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start ffprobe process");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                _logger.LogWarning("ffprobe failed: {Error}", error);
                return null;
            }

            var probeData = JsonSerializer.Deserialize<FfprobeOutput>(output);
            if (probeData?.Streams == null || probeData.Streams.Length == 0)
            {
                return null;
            }

            // Find video stream
            var videoStream = Array.Find(probeData.Streams, s => s.CodecType == "video");
            if (videoStream == null)
            {
                return null;
            }

            var metadata = new MediaMetadata
            {
                Width = videoStream.Width,
                Height = videoStream.Height,
                Format = probeData.Format?.FormatName,
                Codec = videoStream.CodecName,
                Bitrate = probeData.Format?.BitRate
            };

            // Parse duration
            if (double.TryParse(probeData.Format?.Duration, out var duration))
            {
                metadata.Duration = duration;
            }

            // Parse framerate
            if (!string.IsNullOrEmpty(videoStream.RFrameRate))
            {
                var parts = videoStream.RFrameRate.Split('/');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out var num) &&
                    double.TryParse(parts[1], out var den) &&
                    den != 0)
                {
                    metadata.Framerate = num / den;
                }
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract video metadata: {FilePath}", filePath);
            return null;
        }
    }

    private async Task<MediaMetadata?> ExtractAudioMetadataAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return null;
            }

            var probeData = JsonSerializer.Deserialize<FfprobeOutput>(output);
            if (probeData?.Streams == null || probeData.Streams.Length == 0)
            {
                return null;
            }

            // Find audio stream
            var audioStream = Array.Find(probeData.Streams, s => s.CodecType == "audio");
            if (audioStream == null)
            {
                return null;
            }

            var metadata = new MediaMetadata
            {
                Format = probeData.Format?.FormatName,
                Codec = audioStream.CodecName,
                Bitrate = probeData.Format?.BitRate,
                Channels = audioStream.Channels,
                SampleRate = audioStream.SampleRate
            };

            if (double.TryParse(probeData.Format?.Duration, out var duration))
            {
                metadata.Duration = duration;
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract audio metadata: {FilePath}", filePath);
            return null;
        }
    }

    private string? FindFfprobe()
    {
        try
        {
            var locations = new[]
            {
                "ffprobe",
                "/usr/bin/ffprobe",
                "/usr/local/bin/ffprobe",
                "C:\\ffmpeg\\bin\\ffprobe.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffprobe.exe")
            };

            foreach (var location in locations)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = location,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit(1000);
                        if (process.ExitCode == 0)
                        {
                            return location;
                        }
                    }
                }
                catch
                {
                    // Continue
                }
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    // FFprobe output models
    private class FfprobeOutput
    {
        public FfprobeStream[]? Streams { get; set; }
        public FfprobeFormat? Format { get; set; }
    }

    private class FfprobeStream
    {
        public string? CodecType { get; set; }
        public string? CodecName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? RFrameRate { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
    }

    private class FfprobeFormat
    {
        public string? FormatName { get; set; }
        public string? Duration { get; set; }
        public long BitRate { get; set; }
    }
}
