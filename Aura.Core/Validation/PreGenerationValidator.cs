using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Validation;

/// <summary>
/// Validates system readiness before starting video generation
/// </summary>
public class PreGenerationValidator
{
    private readonly ILogger<PreGenerationValidator> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly IHardwareDetector _hardwareDetector;

    public PreGenerationValidator(
        ILogger<PreGenerationValidator> logger,
        IFfmpegLocator ffmpegLocator,
        IHardwareDetector hardwareDetector)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _hardwareDetector = hardwareDetector;
    }

    /// <summary>
    /// Validates system readiness for video generation
    /// </summary>
    /// <param name="brief">The video brief</param>
    /// <param name="planSpec">The plan specification</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with issues if any</returns>
    public async Task<ValidationResult> ValidateSystemReadyAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct = default)
    {
        var issues = new List<string>();

        // Validate FFmpeg availability
        try
        {
            var ffmpegResult = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            if (!ffmpegResult.Found || string.IsNullOrEmpty(ffmpegResult.FfmpegPath))
            {
                issues.Add("FFmpeg not found. Please install FFmpeg or configure the path in Settings.");
            }
            else if (!File.Exists(ffmpegResult.FfmpegPath))
            {
                issues.Add("FFmpeg not found. Please install FFmpeg or configure the path in Settings.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking FFmpeg availability");
            issues.Add("FFmpeg not found. Please install FFmpeg or configure the path in Settings.");
        }

        // Validate disk space
        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AuraVideos"
            );
            
            // Get the drive for the output path
            var rootPath = Path.GetPathRoot(outputPath);
            if (!string.IsNullOrEmpty(rootPath))
            {
                var driveInfo = new DriveInfo(rootPath);
                if (driveInfo.IsReady)
                {
                    double freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    if (freeSpaceGB < 1.0)
                    {
                        issues.Add($"Insufficient disk space: {freeSpaceGB:F1}GB free, need at least 1GB.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking disk space");
            // Don't fail validation if we can't check disk space
        }

        // Validate Brief topic
        if (string.IsNullOrWhiteSpace(brief.Topic))
        {
            issues.Add("Topic is required. Please provide a topic for your video.");
        }
        else if (brief.Topic.Trim().Length < 3)
        {
            issues.Add("Topic is too short. Please provide a descriptive topic (at least 3 characters).");
        }

        // Validate PlanSpec duration
        if (planSpec.TargetDuration.TotalSeconds < 10)
        {
            issues.Add("Duration too short. Minimum duration is 10 seconds.");
        }
        else if (planSpec.TargetDuration.TotalMinutes > 30)
        {
            issues.Add("Duration too long. Maximum duration is 30 minutes.");
        }

        // Validate system hardware
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
            // Check CPU cores
            if (systemProfile.LogicalCores < 2)
            {
                issues.Add("Insufficient CPU cores. At least 2 CPU cores required.");
            }

            // Check RAM
            if (systemProfile.RamGB < 4)
            {
                issues.Add("Insufficient RAM. At least 4GB RAM required.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting hardware");
            // Don't fail validation if we can't detect hardware
        }

        return new ValidationResult(issues.Count == 0, issues);
    }
}
