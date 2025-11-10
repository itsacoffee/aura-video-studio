using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for applying watermarks to videos
/// </summary>
public class WatermarkService
{
    private readonly ILogger<WatermarkService> _logger;
    private readonly IFFmpegService _ffmpegService;

    public WatermarkService(ILogger<WatermarkService> logger, IFFmpegService ffmpegService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
    }

    /// <summary>
    /// Apply a watermark to a video
    /// </summary>
    public async Task<string> ApplyWatermarkAsync(
        string inputVideoPath,
        string watermarkImagePath,
        string outputVideoPath,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        double opacity = 0.7,
        int marginPixels = 10,
        CancellationToken ct = default)
    {
        if (!File.Exists(inputVideoPath))
        {
            throw new FileNotFoundException("Input video not found", inputVideoPath);
        }
        
        if (!File.Exists(watermarkImagePath))
        {
            throw new FileNotFoundException("Watermark image not found", watermarkImagePath);
        }
        
        _logger.LogInformation("Applying watermark to video: {Input} -> {Output}", inputVideoPath, outputVideoPath);
        
        var builder = new FFmpegCommandBuilder()
            .AddInput(inputVideoPath)
            .SetOutput(outputVideoPath)
            .SetOverwrite(true)
            .SetVideoCodec("libx264")
            .SetAudioCodec("copy") // Don't re-encode audio
            .SetPreset("fast")
            .AddWatermark(watermarkImagePath, GetPositionString(position), opacity, marginPixels);
        
        var command = builder.Build();
        var result = await _ffmpegService.ExecuteAsync(command, cancellationToken: ct);
        
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to apply watermark: {result.ErrorMessage}");
        }
        
        _logger.LogInformation("Watermark applied successfully: {Output}", outputVideoPath);
        return outputVideoPath;
    }

    /// <summary>
    /// Apply text watermark to a video
    /// </summary>
    public async Task<string> ApplyTextWatermarkAsync(
        string inputVideoPath,
        string text,
        string outputVideoPath,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        int fontSize = 24,
        string fontColor = "white",
        string? boxColor = "black@0.5",
        CancellationToken ct = default)
    {
        if (!File.Exists(inputVideoPath))
        {
            throw new FileNotFoundException("Input video not found", inputVideoPath);
        }
        
        _logger.LogInformation("Applying text watermark to video: {Input} -> {Output}", inputVideoPath, outputVideoPath);
        
        var (x, y) = GetTextPosition(position);
        
        var builder = new FFmpegCommandBuilder()
            .AddInput(inputVideoPath)
            .SetOutput(outputVideoPath)
            .SetOverwrite(true)
            .SetVideoCodec("libx264")
            .SetAudioCodec("copy")
            .SetPreset("fast")
            .AddTextOverlay(text, null, fontSize, x, y, fontColor, boxColor);
        
        var command = builder.Build();
        var result = await _ffmpegService.ExecuteAsync(command, cancellationToken: ct);
        
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to apply text watermark: {result.ErrorMessage}");
        }
        
        _logger.LogInformation("Text watermark applied successfully: {Output}", outputVideoPath);
        return outputVideoPath;
    }

    private static string GetPositionString(WatermarkPosition position)
    {
        return position switch
        {
            WatermarkPosition.TopLeft => "top-left",
            WatermarkPosition.TopRight => "top-right",
            WatermarkPosition.BottomLeft => "bottom-left",
            WatermarkPosition.BottomRight => "bottom-right",
            WatermarkPosition.Center => "center",
            _ => "bottom-right"
        };
    }

    private static (string x, string y) GetTextPosition(WatermarkPosition position)
    {
        return position switch
        {
            WatermarkPosition.TopLeft => ("10", "10"),
            WatermarkPosition.TopRight => ("w-text_w-10", "10"),
            WatermarkPosition.BottomLeft => ("10", "h-text_h-10"),
            WatermarkPosition.BottomRight => ("w-text_w-10", "h-text_h-10"),
            WatermarkPosition.Center => ("(w-text_w)/2", "(h-text_h)/2"),
            _ => ("w-text_w-10", "h-text_h-10")
        };
    }
}

/// <summary>
/// Watermark position options
/// </summary>
public enum WatermarkPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}
