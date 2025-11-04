using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Preflight validation result for an export operation
/// </summary>
public record PreflightValidationResult
{
    public bool CanProceed { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public PreflightEstimates Estimates { get; init; } = new();
    public List<string> RecommendedActions { get; init; } = new();
}

/// <summary>
/// Estimates for export operation
/// </summary>
public record PreflightEstimates
{
    public double EstimatedFileSizeMB { get; init; }
    public double EstimatedDurationMinutes { get; init; }
    public double RequiredDiskSpaceMB { get; init; }
    public double AvailableDiskSpaceGB { get; init; }
    public string RecommendedEncoder { get; init; } = string.Empty;
    public bool HardwareAccelerationAvailable { get; init; }
}

/// <summary>
/// Validates export settings before starting an export operation
/// </summary>
public class ExportPreflightValidator
{
    private readonly ILogger<ExportPreflightValidator> _logger;
    private readonly IHardwareDetector _hardwareDetector;

    public ExportPreflightValidator(
        ILogger<ExportPreflightValidator> logger,
        IHardwareDetector hardwareDetector)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
    }

    /// <summary>
    /// Performs preflight validation for an export operation
    /// </summary>
    public async Task<PreflightValidationResult> ValidateAsync(
        ExportPreset preset,
        TimeSpan videoDuration,
        string outputDirectory,
        Resolution? sourceResolution = null,
        AspectRatio? sourceAspectRatio = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running preflight validation for preset {PresetName}", preset.Name);

        var errors = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        // Validate output directory
        if (!Directory.Exists(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogDebug("Created output directory: {Directory}", outputDirectory);
            }
            catch (Exception ex)
            {
                errors.Add($"Cannot create output directory: {ex.Message}");
            }
        }

        // Check disk space
        var diskSpaceInfo = GetDiskSpaceInfo(outputDirectory);
        var estimatedFileSizeMB = ExportPresets.EstimateFileSizeMB(preset, videoDuration);
        var requiredSpaceMB = estimatedFileSizeMB * 2.5; // Add 150% buffer for temporary files

        if (diskSpaceInfo.AvailableGB * 1024 < requiredSpaceMB)
        {
            errors.Add(
                $"Insufficient disk space. Required: {requiredSpaceMB:F2} MB, " +
                $"Available: {diskSpaceInfo.AvailableGB * 1024:F2} MB"
            );
        }
        else if (diskSpaceInfo.AvailableGB * 1024 < requiredSpaceMB * 1.5)
        {
            warnings.Add(
                $"Low disk space. Required: {requiredSpaceMB:F2} MB, " +
                $"Available: {diskSpaceInfo.AvailableGB * 1024:F2} MB"
            );
        }

        // Validate aspect ratio conformity
        if (sourceAspectRatio.HasValue && sourceAspectRatio.Value != preset.AspectRatio)
        {
            warnings.Add(
                $"Source aspect ratio ({GetAspectRatioString(sourceAspectRatio.Value)}) " +
                $"differs from preset ({GetAspectRatioString(preset.AspectRatio)}). " +
                $"Video will be letterboxed or pillarboxed."
            );
            recommendations.Add("Consider using 'Fix automatically' to adjust project aspect ratio");
        }

        // Validate resolution
        if (sourceResolution != null)
        {
            if (sourceResolution.Width < preset.Resolution.Width ||
                sourceResolution.Height < preset.Resolution.Height)
            {
                warnings.Add(
                    $"Source resolution ({sourceResolution.Width}x{sourceResolution.Height}) " +
                    $"is smaller than target ({preset.Resolution.Width}x{preset.Resolution.Height}). " +
                    $"Quality may be degraded by upscaling."
                );
            }
        }

        // Validate bitrate ceilings
        var bitrateWarnings = ValidateBitrateCeilings(preset);
        warnings.AddRange(bitrateWarnings);

        // Check codec compatibility
        var codecValidation = ValidateCodecCompatibility(preset);
        if (!codecValidation.IsCompatible)
        {
            warnings.Add(codecValidation.Message);
            if (!string.IsNullOrEmpty(codecValidation.Recommendation))
            {
                recommendations.Add(codecValidation.Recommendation);
            }
        }

        // Validate platform-specific requirements
        if (preset.Platform != Models.Export.Platform.Generic)
        {
            var platformValidation = ValidatePlatformRequirements(preset, videoDuration);
            errors.AddRange(platformValidation.Errors);
            warnings.AddRange(platformValidation.Warnings);
        }

        // Get hardware capabilities
        var systemProfile = await _hardwareDetector.DetectSystemAsync();
        var hardwareInfo = GetHardwareCapabilities(systemProfile);

        // Estimate duration
        var estimatedDurationMinutes = EstimateEncodingDuration(
            preset,
            videoDuration,
            systemProfile.Tier,
            hardwareInfo.HardwareAccelerationAvailable
        );

        var estimates = new PreflightEstimates
        {
            EstimatedFileSizeMB = estimatedFileSizeMB,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            RequiredDiskSpaceMB = requiredSpaceMB,
            AvailableDiskSpaceGB = diskSpaceInfo.AvailableGB,
            RecommendedEncoder = hardwareInfo.RecommendedEncoder,
            HardwareAccelerationAvailable = hardwareInfo.HardwareAccelerationAvailable
        };

        var canProceed = errors.Count == 0;

        _logger.LogInformation(
            "Preflight validation completed: CanProceed={CanProceed}, Errors={ErrorCount}, Warnings={WarningCount}",
            canProceed,
            errors.Count,
            warnings.Count
        );

        return new PreflightValidationResult
        {
            CanProceed = canProceed,
            Errors = errors,
            Warnings = warnings,
            Estimates = estimates,
            RecommendedActions = recommendations
        };
    }

    private (double AvailableGB, double TotalGB) GetDiskSpaceInfo(string path)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
            return (
                AvailableGB: drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0,
                TotalGB: drive.TotalSize / 1024.0 / 1024.0 / 1024.0
            );
        }
        catch
        {
            return (AvailableGB: 0, TotalGB: 0);
        }
    }

    private List<string> ValidateBitrateCeilings(ExportPreset preset)
    {
        var warnings = new List<string>();

        if (preset.Platform != Models.Export.Platform.Generic)
        {
            var profile = PlatformExportProfileFactory.GetProfile(preset.Platform);

            if (preset.VideoBitrate > profile.MaxVideoBitrate)
            {
                warnings.Add(
                    $"Video bitrate ({preset.VideoBitrate} kbps) exceeds {preset.Platform} " +
                    $"maximum ({profile.MaxVideoBitrate} kbps). Platform may reject or re-encode the file."
                );
            }

            if (preset.VideoBitrate < profile.MinVideoBitrate)
            {
                warnings.Add(
                    $"Video bitrate ({preset.VideoBitrate} kbps) is below {preset.Platform} " +
                    $"minimum ({profile.MinVideoBitrate} kbps). Quality may be poor."
                );
            }
        }

        return warnings;
    }

    private (bool IsCompatible, string Message, string Recommendation) ValidateCodecCompatibility(
        ExportPreset preset)
    {
        if (preset.VideoCodec == "libx265" || preset.VideoCodec == "hevc")
        {
            return (
                IsCompatible: true,
                Message: "H.265/HEVC codec selected. Note: Some older devices may not support this codec.",
                Recommendation: "Consider using H.264 (libx264) for maximum compatibility"
            );
        }

        if (preset.VideoCodec == "libvpx-vp9")
        {
            return (
                IsCompatible: true,
                Message: "VP9 codec selected. Best for web playback but may have slower encoding.",
                Recommendation: string.Empty
            );
        }

        return (IsCompatible: true, Message: string.Empty, Recommendation: string.Empty);
    }

    private (List<string> Errors, List<string> Warnings) ValidatePlatformRequirements(
        ExportPreset preset,
        TimeSpan videoDuration)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var profile = PlatformExportProfileFactory.GetProfile(preset.Platform);

            if (profile.MaxDuration.HasValue && videoDuration.TotalSeconds > profile.MaxDuration.Value)
            {
                errors.Add(
                    $"Video duration ({videoDuration.TotalSeconds:F0}s) exceeds {preset.Platform} " +
                    $"maximum ({profile.MaxDuration.Value}s)"
                );
            }

            var estimatedFileSizeMB = ExportPresets.EstimateFileSizeMB(preset, videoDuration);
            if (profile.MaxFileSize.HasValue && estimatedFileSizeMB > profile.MaxFileSize.Value)
            {
                warnings.Add(
                    $"Estimated file size ({estimatedFileSizeMB:F2} MB) may exceed {preset.Platform} " +
                    $"limit ({profile.MaxFileSize.Value} MB). Consider reducing bitrate or duration."
                );
            }
        }
        catch (ArgumentException)
        {
            // Generic platform doesn't have a profile
        }

        return (errors, warnings);
    }

    private (string RecommendedEncoder, bool HardwareAccelerationAvailable) GetHardwareCapabilities(
        SystemProfile systemProfile)
    {
        if (systemProfile.EnableNVENC && systemProfile.Gpu?.Vendor == "NVIDIA")
        {
            return ("h264_nvenc", true);
        }

        if (systemProfile.Gpu?.Vendor == "AMD")
        {
            return ("h264_amf", true);
        }

        if (systemProfile.Gpu?.Vendor == "Intel")
        {
            return ("h264_qsv", true);
        }

        return ("libx264", false);
    }

    private double EstimateEncodingDuration(
        ExportPreset preset,
        TimeSpan videoDuration,
        HardwareTier tier,
        bool hardwareAcceleration)
    {
        var baseMultiplier = preset.Quality switch
        {
            QualityLevel.Draft => 0.5,
            QualityLevel.Good => 1.0,
            QualityLevel.High => 1.5,
            QualityLevel.Maximum => 2.5,
            _ => 1.0
        };

        var tierMultiplier = tier switch
        {
            HardwareTier.A => 0.5,
            HardwareTier.B => 0.75,
            HardwareTier.C => 1.0,
            HardwareTier.D => 1.5,
            _ => 1.0
        };

        var hwMultiplier = hardwareAcceleration ? 0.3 : 1.0;

        var estimatedMinutes = videoDuration.TotalMinutes * baseMultiplier * tierMultiplier * hwMultiplier;
        return Math.Max(0.1, estimatedMinutes);
    }

    private string GetAspectRatioString(AspectRatio aspect)
    {
        return aspect switch
        {
            AspectRatio.SixteenByNine => "16:9",
            AspectRatio.NineBySixteen => "9:16",
            AspectRatio.OneByOne => "1:1",
            AspectRatio.FourByFive => "4:5",
            _ => aspect.ToString()
        };
    }
}
