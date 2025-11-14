using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Platform;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Platform;

/// <summary>
/// Service for optimizing content for specific platforms
/// </summary>
public class PlatformOptimizationService
{
    private readonly ILogger<PlatformOptimizationService> _logger;
    private readonly PlatformProfileService _platformProfile;

    public PlatformOptimizationService(
        ILogger<PlatformOptimizationService> logger,
        PlatformProfileService platformProfile)
    {
        _logger = logger;
        _platformProfile = platformProfile;
    }

    /// <summary>
    /// Optimize video for a specific platform
    /// </summary>
    public async Task<PlatformOptimizationResult> OptimizeForPlatform(PlatformOptimizationRequest request)
    {
        _logger.LogInformation("Optimizing video for platform: {Platform}", request.TargetPlatform);

        var profile = _platformProfile.GetPlatformProfile(request.TargetPlatform);
        if (profile == null)
        {
            throw new ArgumentException($"Unknown platform: {request.TargetPlatform}");
        }

        var result = new PlatformOptimizationResult
        {
            AppliedOptimizations = new List<string>(),
            TechnicalSpecs = new Dictionary<string, string>()
        };

        // Simulate optimization process
        // In a real implementation, this would call FFmpeg and other tools
        result.AppliedOptimizations.Add($"Aspect ratio converted to {profile.Requirements.SupportedAspectRatios.FirstOrDefault(a => a.IsPreferred)?.Ratio ?? "16:9"}");
        result.AppliedOptimizations.Add($"Video bitrate optimized to {profile.Requirements.Video.RecommendedBitrate} kbps");
        result.AppliedOptimizations.Add("Audio normalized for platform standards");

        // Set technical specs
        result.TechnicalSpecs["codec"] = profile.Requirements.Video.RecommendedCodecs.FirstOrDefault() ?? "h264";
        result.TechnicalSpecs["bitrate"] = profile.Requirements.Video.RecommendedBitrate.ToString();
        result.TechnicalSpecs["aspectRatio"] = profile.Requirements.SupportedAspectRatios.FirstOrDefault(a => a.IsPreferred)?.Ratio ?? "16:9";

        // Simulated paths - in real implementation, these would be actual processed files
        result.OptimizedVideoPath = Path.Combine(Path.GetTempPath(), $"{request.TargetPlatform}_optimized.mp4");
        
        if (request.GenerateThumbnail)
        {
            result.ThumbnailPath = Path.Combine(Path.GetTempPath(), $"{request.TargetPlatform}_thumbnail.jpg");
            result.AppliedOptimizations.Add("Thumbnail generated with platform-specific dimensions");
        }

        if (request.OptimizeMetadata)
        {
            result.Metadata = GenerateMetadata(profile, "Sample Video Title");
            result.AppliedOptimizations.Add("Metadata optimized for platform");
        }

        await Task.Delay(100).ConfigureAwait(false); // Simulate async processing

        _logger.LogInformation("Optimization complete with {Count} optimizations applied", result.AppliedOptimizations.Count);
        return result;
    }

    /// <summary>
    /// Export video for multiple platforms
    /// </summary>
    public async Task<MultiPlatformExportResult> ExportForMultiplePlatforms(MultiPlatformExportRequest request)
    {
        _logger.LogInformation("Starting multi-platform export for {Count} platforms", request.TargetPlatforms.Count);

        var result = new MultiPlatformExportResult
        {
            Exports = new Dictionary<string, PlatformExport>(),
            Status = "Processing"
        };

        foreach (var platform in request.TargetPlatforms)
        {
            try
            {
                var optimizationRequest = new PlatformOptimizationRequest
                {
                    SourceVideoPath = request.SourceVideoPath,
                    TargetPlatform = platform,
                    OptimizeMetadata = request.GenerateMetadata,
                    GenerateThumbnail = request.GenerateThumbnails
                };

                var optimizationResult = await OptimizeForPlatform(optimizationRequest).ConfigureAwait(false);

                result.Exports[platform] = new PlatformExport
                {
                    Platform = platform,
                    VideoPath = optimizationResult.OptimizedVideoPath,
                    ThumbnailPath = optimizationResult.ThumbnailPath,
                    Metadata = optimizationResult.Metadata,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export for platform: {Platform}", platform);
                result.Exports[platform] = new PlatformExport
                {
                    Platform = platform,
                    Success = false,
                    Error = ex.Message
                };
                result.Warnings.Add($"Export failed for {platform}: {ex.Message}");
            }
        }

        result.Status = result.Exports.Values.All(e => e.Success) ? "Completed" : "Completed with errors";
        _logger.LogInformation("Multi-platform export completed: {Status}", result.Status);

        return result;
    }

    /// <summary>
    /// Adapt content from one platform to another
    /// </summary>
    public async Task<ContentAdaptationResult> AdaptContent(ContentAdaptationRequest request)
    {
        _logger.LogInformation("Adapting content from {Source} to {Target}", request.SourcePlatform, request.TargetPlatform);

        var sourceProfile = _platformProfile.GetPlatformProfile(request.SourcePlatform);
        var targetProfile = _platformProfile.GetPlatformProfile(request.TargetPlatform);

        if (sourceProfile == null || targetProfile == null)
        {
            throw new ArgumentException("Invalid source or target platform");
        }

        var result = new ContentAdaptationResult
        {
            ChangesApplied = new List<string>(),
            Recommendations = new Dictionary<string, string>()
        };

        // Simulated adaptation logic
        if (request.AdaptFormat)
        {
            var sourceRatio = sourceProfile.Requirements.SupportedAspectRatios.FirstOrDefault(a => a.IsPreferred)?.Ratio;
            var targetRatio = targetProfile.Requirements.SupportedAspectRatios.FirstOrDefault(a => a.IsPreferred)?.Ratio;
            
            if (sourceRatio != targetRatio)
            {
                result.ChangesApplied.Add($"Converted aspect ratio from {sourceRatio} to {targetRatio}");
                result.Recommendations["Cropping"] = "Review automatic crop to ensure key content is preserved";
            }
        }

        if (request.AdaptHook)
        {
            if (targetProfile.BestPractices.HookDurationSeconds < sourceProfile.BestPractices.HookDurationSeconds)
            {
                result.ChangesApplied.Add($"Shortened hook from {sourceProfile.BestPractices.HookDurationSeconds}s to {targetProfile.BestPractices.HookDurationSeconds}s");
                result.Recommendations["Hook"] = $"Consider creating a new hook optimized for {targetProfile.Name}'s {targetProfile.BestPractices.HookDurationSeconds}s standard";
            }
        }

        if (request.AdaptPacing)
        {
            if (sourceProfile.BestPractices.ContentPacing != targetProfile.BestPractices.ContentPacing)
            {
                result.ChangesApplied.Add($"Adapted pacing from {sourceProfile.BestPractices.ContentPacing} to {targetProfile.BestPractices.ContentPacing}");
            }
        }

        result.AdaptedVideoPath = Path.Combine(Path.GetTempPath(), $"{request.TargetPlatform}_adapted.mp4");
        result.AdaptationStrategy = $"Adapted from {request.SourcePlatform} best practices to {request.TargetPlatform} requirements";

        await Task.Delay(100).ConfigureAwait(false); // Simulate async processing

        _logger.LogInformation("Content adaptation complete with {Count} changes", result.ChangesApplied.Count);
        return result;
    }

    /// <summary>
    /// Generate optimized metadata for a platform
    /// </summary>
    private OptimizedMetadata GenerateMetadata(PlatformProfile profile, string baseTitle)
    {
        var metadata = new OptimizedMetadata
        {
            Title = TruncateToLength(baseTitle, profile.Requirements.Metadata.TitleMaxLength),
            Tags = new List<string>(),
            Hashtags = new List<string>()
        };

        // Add platform-specific recommendations
        if (profile.BestPractices.MusicImportant)
        {
            metadata.CustomFields["music_recommendation"] = "Use trending audio from platform library";
        }

        if (profile.BestPractices.CaptionsRequired)
        {
            metadata.CustomFields["captions_required"] = true;
        }

        return metadata;
    }

    /// <summary>
    /// Truncate text to specified length
    /// </summary>
    private string TruncateToLength(string text, int maxLength)
    {
        if (maxLength <= 0 || string.IsNullOrEmpty(text))
            return text;

        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
