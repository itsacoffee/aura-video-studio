using System;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service for handling video resolution conversions
/// </summary>
public interface IResolutionService
{
    /// <summary>
    /// Determine the best scale mode for converting between resolutions
    /// </summary>
    string DetermineScaleMode(Resolution source, Resolution target, AspectRatio targetAspectRatio);
    
    /// <summary>
    /// Calculate output resolution maintaining aspect ratio
    /// </summary>
    Resolution CalculateOutputResolution(Resolution source, Resolution target, bool maintainAspectRatio = true);
    
    /// <summary>
    /// Check if resolution conversion will result in upscaling
    /// </summary>
    bool IsUpscaling(Resolution source, Resolution target);
    
    /// <summary>
    /// Get the aspect ratio of a resolution
    /// </summary>
    AspectRatio GetAspectRatio(Resolution resolution);
}

/// <summary>
/// Implementation of resolution service
/// </summary>
public class ResolutionService : IResolutionService
{
    private readonly ILogger<ResolutionService> _logger;

    public ResolutionService(ILogger<ResolutionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string DetermineScaleMode(Resolution source, Resolution target, AspectRatio targetAspectRatio)
    {
        var sourceAspect = (double)source.Width / source.Height;
        var targetAspect = (double)target.Width / target.Height;
        
        // If aspect ratios match closely, use stretch
        if (Math.Abs(sourceAspect - targetAspect) < 0.01)
        {
            return "stretch";
        }
        
        // For social media vertical content, prefer crop to avoid letterboxing
        if (targetAspectRatio == AspectRatio.NineBySixteen)
        {
            return sourceAspect > targetAspect ? "crop" : "fit";
        }
        
        // For square content, prefer fit to preserve full frame
        if (targetAspectRatio == AspectRatio.OneByOne)
        {
            return "fit";
        }
        
        // Default: fit with padding for landscape
        return "fit";
    }

    public Resolution CalculateOutputResolution(Resolution source, Resolution target, bool maintainAspectRatio = true)
    {
        if (!maintainAspectRatio)
        {
            return target;
        }

        var sourceAspect = (double)source.Width / source.Height;
        var targetAspect = (double)target.Width / target.Height;

        if (sourceAspect > targetAspect)
        {
            // Source is wider - fit to width
            var height = (int)Math.Round(target.Width / sourceAspect);
            return new Resolution(target.Width, height);
        }
        else
        {
            // Source is taller - fit to height
            var width = (int)Math.Round(target.Height * sourceAspect);
            return new Resolution(width, target.Height);
        }
    }

    public bool IsUpscaling(Resolution source, Resolution target)
    {
        return target.Width > source.Width || target.Height > source.Height;
    }

    public AspectRatio GetAspectRatio(Resolution resolution)
    {
        var ratio = (double)resolution.Width / resolution.Height;
        
        // Allow some tolerance for floating point comparisons
        const double tolerance = 0.02;
        
        if (Math.Abs(ratio - 16.0 / 9.0) < tolerance)
        {
            return AspectRatio.SixteenByNine;
        }
        else if (Math.Abs(ratio - 9.0 / 16.0) < tolerance)
        {
            return AspectRatio.NineBySixteen;
        }
        else if (Math.Abs(ratio - 1.0) < tolerance)
        {
            return AspectRatio.OneByOne;
        }
        else if (Math.Abs(ratio - 4.0 / 5.0) < tolerance)
        {
            return AspectRatio.FourByFive;
        }
        
        // Default to 16:9 for unknown ratios
        _logger.LogWarning("Unknown aspect ratio {Ratio} for resolution {Width}x{Height}, defaulting to 16:9",
            ratio, resolution.Width, resolution.Height);
        return AspectRatio.SixteenByNine;
    }
}
