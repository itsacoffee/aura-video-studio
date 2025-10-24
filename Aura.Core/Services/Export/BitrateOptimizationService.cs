using System;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service for optimizing video bitrate based on platform and content
/// </summary>
public interface IBitrateOptimizationService
{
    /// <summary>
    /// Calculate optimal bitrate for given resolution and platform
    /// </summary>
    int CalculateOptimalBitrate(Resolution resolution, Models.Export.Platform platform, int frameRate = 30);
    
    /// <summary>
    /// Adjust bitrate based on content complexity
    /// </summary>
    int AdjustForComplexity(int baseBitrate, ContentComplexity complexity);
    
    /// <summary>
    /// Validate bitrate against platform constraints
    /// </summary>
    (bool IsValid, int AdjustedBitrate) ValidateBitrate(int bitrate, IPlatformExportProfile profile);
}

/// <summary>
/// Content complexity level for bitrate optimization
/// </summary>
public enum ContentComplexity
{
    Low,      // Talking head, simple animations
    Medium,   // General content
    High,     // Action, gaming, fast motion
    VeryHigh  // High detail, rapid movement
}

/// <summary>
/// Implementation of bitrate optimization service
/// </summary>
public class BitrateOptimizationService : IBitrateOptimizationService
{
    private readonly ILogger<BitrateOptimizationService> _logger;

    public BitrateOptimizationService(ILogger<BitrateOptimizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int CalculateOptimalBitrate(Resolution resolution, Models.Export.Platform platform, int frameRate = 30)
    {
        // Base bitrate calculation (kbps per pixel at 30fps)
        var pixels = resolution.Width * resolution.Height;
        var basePixelRate = 0.1; // 0.1 kbps per pixel for standard quality
        
        // Adjust for frame rate
        var frameRateMultiplier = frameRate / 30.0;
        
        // Calculate base bitrate
        var baseBitrate = (int)(pixels * basePixelRate * frameRateMultiplier);
        
        // Apply platform-specific adjustments
        var platformMultiplier = platform switch
        {
            Models.Export.Platform.YouTube => 1.2,      // Higher quality for YouTube
            Models.Export.Platform.TikTok => 0.9,       // Optimize for mobile
            Models.Export.Platform.Instagram => 0.9,    // Optimize for mobile
            Models.Export.Platform.LinkedIn => 1.0,     // Standard professional quality
            Models.Export.Platform.Twitter => 0.85,     // Lower for file size constraints
            Models.Export.Platform.Facebook => 0.9,     // Optimize for streaming
            _ => 1.0
        };
        
        var optimizedBitrate = (int)(baseBitrate * platformMultiplier);
        
        // Ensure minimum quality
        var minBitrate = platform switch
        {
            Models.Export.Platform.YouTube => 2000,
            Models.Export.Platform.TikTok => 1000,
            Models.Export.Platform.Instagram => 1000,
            _ => 1000
        };
        
        return Math.Max(optimizedBitrate, minBitrate);
    }

    public int AdjustForComplexity(int baseBitrate, ContentComplexity complexity)
    {
        var multiplier = complexity switch
        {
            ContentComplexity.Low => 0.7,       // Can use lower bitrate
            ContentComplexity.Medium => 1.0,    // Standard bitrate
            ContentComplexity.High => 1.3,      // Need higher bitrate
            ContentComplexity.VeryHigh => 1.6,  // Much higher bitrate
            _ => 1.0
        };
        
        return (int)(baseBitrate * multiplier);
    }

    public (bool IsValid, int AdjustedBitrate) ValidateBitrate(int bitrate, IPlatformExportProfile profile)
    {
        if (bitrate < profile.MinVideoBitrate)
        {
            _logger.LogWarning("Bitrate {Bitrate} below minimum {MinBitrate} for {Platform}, adjusting",
                bitrate, profile.MinVideoBitrate, profile.PlatformName);
            return (false, profile.MinVideoBitrate);
        }
        
        if (bitrate > profile.MaxVideoBitrate)
        {
            _logger.LogWarning("Bitrate {Bitrate} above maximum {MaxBitrate} for {Platform}, adjusting",
                bitrate, profile.MaxVideoBitrate, profile.PlatformName);
            return (false, profile.MaxVideoBitrate);
        }
        
        return (true, bitrate);
    }
}
