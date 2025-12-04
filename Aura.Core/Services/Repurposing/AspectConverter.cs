using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Repurposing;

/// <summary>
/// Interface for converting video aspect ratios
/// </summary>
public interface IAspectConverter
{
    /// <summary>
    /// Convert a video to a different aspect ratio
    /// </summary>
    Task<GeneratedAspectVariant> ConvertAsync(
        AspectVariantPlan plan,
        CancellationToken ct = default);
}

/// <summary>
/// Converts videos between different aspect ratios
/// </summary>
public class AspectConverter : IAspectConverter
{
    private readonly IFFmpegExecutor _ffmpegExecutor;
    private readonly ILogger<AspectConverter> _logger;

    public AspectConverter(
        IFFmpegExecutor ffmpegExecutor,
        ILogger<AspectConverter> logger)
    {
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GeneratedAspectVariant> ConvertAsync(
        AspectVariantPlan plan,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Converting video from {Source} to {Target} using {Strategy}",
            plan.SourceAspect, plan.TargetAspect, plan.CropStrategy);

        var outputDir = Path.Combine(
            Path.GetTempPath(), "AuraVideoStudio", "AspectVariants", Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputDir);

        var (width, height) = GetDimensionsForAspect(plan.TargetAspect);
        var outputFileName = $"{plan.TargetAspect}_{width}x{height}.mp4";
        var outputPath = Path.Combine(outputDir, outputFileName);

        var filter = BuildCropFilter(plan.SourceAspect, plan.TargetAspect, plan.CropStrategy, width, height);

        var builder = new FFmpegCommandBuilder()
            .AddInput(plan.SourceVideoPath)
            .AddFilter(filter)
            .SetVideoCodec("libx264")
            .SetPreset("medium")
            .SetCRF(23)
            .SetAudioCodec("aac")
            .SetAudioBitrate(128)
            .SetOutput(outputPath)
            .SetOverwrite(true);

        var result = await _ffmpegExecutor.ExecuteCommandAsync(builder, cancellationToken: ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Aspect conversion failed: {result.ErrorMessage}");
        }

        // Get duration from result or calculate from timeline
        var duration = result.Duration;
        if (duration == TimeSpan.Zero && plan.SourceTimeline != null)
        {
            duration = plan.SourceTimeline.Scenes.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.Duration);
        }

        return new GeneratedAspectVariant(
            Id: Guid.NewGuid().ToString(),
            OutputPath: outputPath,
            Aspect: plan.TargetAspect,
            Width: width,
            Height: height,
            Duration: duration);
    }

    private static (int width, int height) GetDimensionsForAspect(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.Widescreen16x9 => (1920, 1080),
            Aspect.Vertical9x16 => (1080, 1920),
            Aspect.Square1x1 => (1080, 1080),
            _ => (1920, 1080)
        };
    }

    private string BuildCropFilter(
        Aspect source,
        Aspect target,
        CropStrategy strategy,
        int targetWidth,
        int targetHeight)
    {
        // Calculate the filter based on source and target aspects
        var filter = strategy switch
        {
            CropStrategy.SmartCenter => BuildSmartCenterFilter(source, target, targetWidth, targetHeight),
            CropStrategy.CenterCrop => BuildCenterCropFilter(source, target, targetWidth, targetHeight),
            CropStrategy.Letterbox => BuildLetterboxFilter(targetWidth, targetHeight),
            CropStrategy.Stretch => $"scale={targetWidth}:{targetHeight}",
            _ => BuildCenterCropFilter(source, target, targetWidth, targetHeight)
        };

        _logger.LogDebug("Generated filter for {Source} to {Target}: {Filter}",
            source, target, filter);

        return filter;
    }

    private static string BuildSmartCenterFilter(
        Aspect source,
        Aspect target,
        int targetWidth,
        int targetHeight)
    {
        // Smart center tries to keep the center of the frame
        // For 16:9 to 9:16, we crop from the horizontal center
        if (source == Aspect.Widescreen16x9 && target == Aspect.Vertical9x16)
        {
            // Crop width to 9:16 ratio from height, then scale
            return $"crop=ih*9/16:ih,scale={targetWidth}:{targetHeight}";
        }

        // For 16:9 to 1:1, take the center square
        if (source == Aspect.Widescreen16x9 && target == Aspect.Square1x1)
        {
            return $"crop=ih:ih,scale={targetWidth}:{targetHeight}";
        }

        // For 9:16 to 1:1, take the center square from the vertical
        if (source == Aspect.Vertical9x16 && target == Aspect.Square1x1)
        {
            return $"crop=iw:iw,scale={targetWidth}:{targetHeight}";
        }

        // Default to center crop
        return BuildCenterCropFilter(source, target, targetWidth, targetHeight);
    }

    private static string BuildCenterCropFilter(
        Aspect source,
        Aspect target,
        int targetWidth,
        int targetHeight)
    {
        // Calculate aspect ratios
        double sourceRatio = GetAspectRatioValue(source);
        double targetRatio = (double)targetWidth / targetHeight;

        if (sourceRatio > targetRatio)
        {
            // Source is wider, crop width
            return $"crop=ih*{targetWidth}/{targetHeight}:ih,scale={targetWidth}:{targetHeight}";
        }
        else
        {
            // Source is taller, crop height
            return $"crop=iw:iw*{targetHeight}/{targetWidth},scale={targetWidth}:{targetHeight}";
        }
    }

    private static string BuildLetterboxFilter(int targetWidth, int targetHeight)
    {
        // Scale to fit within bounds, then pad to exact size
        return $"scale={targetWidth}:{targetHeight}:force_original_aspect_ratio=decrease," +
               $"pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2:black";
    }

    private static double GetAspectRatioValue(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.Widescreen16x9 => 16.0 / 9.0,
            Aspect.Vertical9x16 => 9.0 / 16.0,
            Aspect.Square1x1 => 1.0,
            _ => 16.0 / 9.0
        };
    }
}
