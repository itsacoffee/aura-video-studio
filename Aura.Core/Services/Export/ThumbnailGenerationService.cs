using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public interface IThumbnailGenerationService
{
    /// <summary>
    /// Generate a thumbnail at a specific time position
    /// </summary>
    Task<string> GenerateThumbnailAsync(
        string videoPath,
        TimeSpan position,
        string? outputPath = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate multiple thumbnails at different positions
    /// </summary>
    Task<string[]> GenerateMultipleThumbnailsAsync(
        string videoPath,
        TimeSpan[] positions,
        string? outputDirectory = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a thumbnail at a percentage through the video
    /// </summary>
    Task<string> GenerateThumbnailAtPercentAsync(
        string videoPath,
        double percent,
        string? outputPath = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of thumbnail generation service
/// </summary>
public class ThumbnailGenerationService : IThumbnailGenerationService
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<ThumbnailGenerationService> _logger;

    public ThumbnailGenerationService(
        IFFmpegService ffmpegService,
        ILogger<ThumbnailGenerationService> logger)
    {
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateThumbnailAsync(
        string videoPath,
        TimeSpan position,
        string? outputPath = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Video file not found", videoPath);
        }

        // Generate output path if not provided
        if (string.IsNullOrEmpty(outputPath))
        {
            var directory = Path.GetDirectoryName(videoPath) ?? ".";
            var filename = Path.GetFileNameWithoutExtension(videoPath);
            outputPath = Path.Combine(directory, $"{filename}_thumb_{position.TotalSeconds:F2}s.jpg");
        }

        // Use default resolution if not specified
        var thumbResolution = resolution ?? new Resolution(1280, 720);

        _logger.LogInformation("Generating thumbnail for {VideoPath} at {Position}", videoPath, position);

        // Build FFmpeg command for thumbnail generation
        var command = FFmpegCommandBuilder.CreateThumbnailCommand(
            videoPath,
            outputPath,
            position,
            thumbResolution.Width,
            thumbResolution.Height);

        // Add one frame extraction
        command.AddFilter("select='eq(n\\,0)'")
               .SetVideoCodec("mjpeg");

        var result = await _ffmpegService.ExecuteAsync(
            command.Build(),
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to generate thumbnail: {result.ErrorMessage}");
        }

        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException("Thumbnail was not created");
        }

        _logger.LogInformation("Thumbnail generated: {OutputPath}", outputPath);
        return outputPath;
    }

    public async Task<string[]> GenerateMultipleThumbnailsAsync(
        string videoPath,
        TimeSpan[] positions,
        string? outputDirectory = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(videoPath))
        {
            throw new FileNotFoundException("Video file not found", videoPath);
        }

        // Use video directory if output directory not specified
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = Path.GetDirectoryName(videoPath) ?? ".";
        }

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);

        var thumbnails = new string[positions.Length];
        var filename = Path.GetFileNameWithoutExtension(videoPath);

        for (int i = 0; i < positions.Length; i++)
        {
            var outputPath = Path.Combine(outputDirectory, $"{filename}_thumb_{i + 1}.jpg");
            thumbnails[i] = await GenerateThumbnailAsync(
                videoPath,
                positions[i],
                outputPath,
                resolution,
                cancellationToken);
        }

        return thumbnails;
    }

    public async Task<string> GenerateThumbnailAtPercentAsync(
        string videoPath,
        double percent,
        string? outputPath = null,
        Resolution? resolution = null,
        CancellationToken cancellationToken = default)
    {
        if (percent < 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100");
        }

        // Get video duration
        var videoInfo = await _ffmpegService.GetVideoInfoAsync(videoPath, cancellationToken);
        
        // Calculate position
        var position = TimeSpan.FromSeconds(videoInfo.Duration.TotalSeconds * (percent / 100.0));

        return await GenerateThumbnailAsync(videoPath, position, outputPath, resolution, cancellationToken);
    }
}
