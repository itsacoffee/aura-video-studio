using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Media;
using Aura.Core.Services.Storage;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service for generating thumbnails from media files
/// </summary>
public interface IThumbnailGenerationService
{
    Task<string?> GenerateThumbnailAsync(
        string sourceFilePath,
        MediaType mediaType,
        CancellationToken ct = default);

    Task<string?> GenerateThumbnailFromStreamAsync(
        Stream sourceStream,
        MediaType mediaType,
        string fileName,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of thumbnail generation service
/// </summary>
public class ThumbnailGenerationService : IThumbnailGenerationService
{
    private readonly IStorageService _storageService;
    private readonly ILogger<ThumbnailGenerationService> _logger;
    private readonly string _ffmpegPath;
    private const int ThumbnailWidth = 320;
    private const int ThumbnailHeight = 180;

    public ThumbnailGenerationService(
        IStorageService storageService,
        ILogger<ThumbnailGenerationService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Try to find ffmpeg
        _ffmpegPath = FindFfmpeg() ?? "ffmpeg";
    }

    public async Task<string?> GenerateThumbnailAsync(
        string sourceFilePath,
        MediaType mediaType,
        CancellationToken ct = default)
    {
        try
        {
            using var sourceStream = File.OpenRead(sourceFilePath);
            return await GenerateThumbnailFromStreamAsync(sourceStream, mediaType, Path.GetFileName(sourceFilePath), ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail from file: {FilePath}", sourceFilePath);
            return null;
        }
    }

    public async Task<string?> GenerateThumbnailFromStreamAsync(
        Stream sourceStream,
        MediaType mediaType,
        string fileName,
        CancellationToken ct = default)
    {
        try
        {
            Stream? thumbnailStream = null;

            switch (mediaType)
            {
                case MediaType.Image:
                    thumbnailStream = await GenerateImageThumbnailAsync(sourceStream, ct).ConfigureAwait(false);
                    break;

                case MediaType.Video:
                    thumbnailStream = await GenerateVideoThumbnailAsync(sourceStream, fileName, ct).ConfigureAwait(false);
                    break;

                case MediaType.Audio:
                    // For audio, we could generate a waveform visualization
                    // For now, return null (no thumbnail)
                    return null;

                default:
                    return null;
            }

            if (thumbnailStream == null)
            {
                return null;
            }

            // Upload thumbnail to storage
            var thumbnailFileName = $"thumb_{Guid.NewGuid()}.jpg";
            var thumbnailUrl = await _storageService.UploadFileAsync(
                thumbnailStream,
                thumbnailFileName,
                "image/jpeg",
                ct).ConfigureAwait(false);

            await thumbnailStream.DisposeAsync().ConfigureAwait(false);

            return thumbnailUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail from stream: {FileName}", fileName);
            return null;
        }
    }

    private async Task<Stream?> GenerateImageThumbnailAsync(Stream sourceStream, CancellationToken ct)
    {
        try
        {
            using var image = await Image.LoadAsync(sourceStream, ct).ConfigureAwait(false);
            
            // Calculate dimensions maintaining aspect ratio
            var width = ThumbnailWidth;
            var height = ThumbnailHeight;
            
            if (image.Width > image.Height)
            {
                height = (int)(image.Height * ((double)width / image.Width));
            }
            else
            {
                width = (int)(image.Width * ((double)height / image.Height));
            }

            image.Mutate(x => x.Resize(width, height));

            var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 }, ct).ConfigureAwait(false);
            outputStream.Position = 0;

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image thumbnail");
            return null;
        }
    }

    private async Task<Stream?> GenerateVideoThumbnailAsync(Stream sourceStream, string fileName, CancellationToken ct)
    {
        try
        {
            // Save stream to temp file (required for ffmpeg)
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"input_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid()}.jpg");

            try
            {
                using (var fileStream = File.Create(tempInputPath))
                {
                    await sourceStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                }

                // Extract frame at 1 second using ffmpeg
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = $"-i \"{tempInputPath}\" -ss 00:00:01 -vframes 1 -vf scale={ThumbnailWidth}:{ThumbnailHeight}:force_original_aspect_ratio=decrease \"{tempOutputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogWarning("Failed to start ffmpeg process");
                    return null;
                }

                await process.WaitForExitAsync(ct).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    _logger.LogWarning("ffmpeg failed to generate thumbnail: {Error}", error);
                    return null;
                }

                if (!File.Exists(tempOutputPath))
                {
                    return null;
                }

                var outputStream = new MemoryStream();
                using (var fileStream = File.OpenRead(tempOutputPath))
                {
                    await fileStream.CopyToAsync(outputStream, ct).ConfigureAwait(false);
                }
                outputStream.Position = 0;

                return outputStream;
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempInputPath))
                {
                    File.Delete(tempInputPath);
                }
                if (File.Exists(tempOutputPath))
                {
                    File.Delete(tempOutputPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate video thumbnail");
            return null;
        }
    }

    private string? FindFfmpeg()
    {
        try
        {
            // Check common locations
            var locations = new[]
            {
                "ffmpeg",
                "/usr/bin/ffmpeg",
                "/usr/local/bin/ffmpeg",
                "C:\\ffmpeg\\bin\\ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe")
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
                    // Continue to next location
                }
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }
}
