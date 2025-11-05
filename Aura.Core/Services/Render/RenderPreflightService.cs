using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Rendering;
using Aura.Core.Services.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Result of render preflight validation
/// </summary>
public record RenderPreflightResult
{
    public bool CanProceed { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public List<string> RecommendedActions { get; init; } = new();
    public RenderPreflightEstimates Estimates { get; init; } = new();
    public EncoderSelection EncoderSelection { get; init; } = new("libx264", "Software encoding (CPU)", false);
    public string? PreferredPresetName { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Estimates for render operation
/// </summary>
public record RenderPreflightEstimates
{
    public double EstimatedFileSizeMB { get; init; }
    public double EstimatedDurationMinutes { get; init; }
    public double RequiredDiskSpaceMB { get; init; }
    public double AvailableDiskSpaceGB { get; init; }
    public double RequiredTempSpaceMB { get; init; }
    public string TempDirectory { get; init; } = string.Empty;
}

/// <summary>
/// Encoder selection information
/// </summary>
public record EncoderSelection(
    string EncoderName,
    string Description,
    bool IsHardwareAccelerated,
    Dictionary<string, string>? Parameters = null,
    string? FallbackEncoder = null);

/// <summary>
/// Comprehensive preflight validation service for render operations
/// </summary>
public class RenderPreflightService
{
    private readonly ILogger<RenderPreflightService> _logger;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly ExportPreflightValidator _exportValidator;
    private readonly HardwareEncoder _hardwareEncoder;

    private const double TempSpaceMultiplier = 1.5;
    private const double MinimumFreeDiskSpaceGB = 2.0;

    public RenderPreflightService(
        ILogger<RenderPreflightService> logger,
        IHardwareDetector hardwareDetector,
        ExportPreflightValidator exportValidator,
        HardwareEncoder hardwareEncoder)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _exportValidator = exportValidator;
        _hardwareEncoder = hardwareEncoder;
    }

    /// <summary>
    /// Performs comprehensive preflight validation for a render operation
    /// </summary>
    public async Task<RenderPreflightResult> ValidateRenderAsync(
        ExportPreset preset,
        TimeSpan videoDuration,
        string outputDirectory,
        string? encoderOverride = null,
        bool preferHardware = true,
        Resolution? sourceResolution = null,
        AspectRatio? sourceAspectRatio = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var corrId = correlationId ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting render preflight validation. Preset={PresetName}, Duration={Duration}, CorrelationId={CorrelationId}",
            preset.Name, videoDuration, corrId);

        var errors = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        // Run base export validation
        var exportValidation = await _exportValidator.ValidateAsync(
            preset, videoDuration, outputDirectory, sourceResolution, sourceAspectRatio, cancellationToken);

        errors.AddRange(exportValidation.Errors);
        warnings.AddRange(exportValidation.Warnings);
        recommendations.AddRange(exportValidation.RecommendedActions);

        // Validate temp directory
        var tempValidation = ValidateTempDirectory(outputDirectory);
        errors.AddRange(tempValidation.Errors);
        warnings.AddRange(tempValidation.Warnings);

        // Validate write permissions
        var permissionValidation = ValidateWritePermissions(outputDirectory, tempValidation.TempDir);
        errors.AddRange(permissionValidation.Errors);
        warnings.AddRange(permissionValidation.Warnings);

        // Select encoder
        var encoderSelection = await SelectEncoderAsync(preset, encoderOverride, preferHardware, cancellationToken);

        // Calculate estimates
        var estimatedFileSizeMB = ExportPresets.EstimateFileSizeMB(preset, videoDuration);
        var requiredDiskSpace = estimatedFileSizeMB * 2.5;
        var requiredTempSpace = estimatedFileSizeMB * TempSpaceMultiplier;

        var diskInfo = GetDiskSpaceInfo(outputDirectory);
        var tempDiskInfo = GetDiskSpaceInfo(tempValidation.TempDir);

        // Validate temp space
        if (tempDiskInfo.AvailableGB * 1024 < requiredTempSpace)
        {
            errors.Add(
                $"Insufficient temp space. Required: {requiredTempSpace:F2} MB, " +
                $"Available: {tempDiskInfo.AvailableGB * 1024:F2} MB in {tempValidation.TempDir}");
        }

        // Estimate rendering duration
        var systemProfile = await _hardwareDetector.DetectSystemAsync();
        var estimatedDuration = EstimateRenderingDuration(
            preset, videoDuration, systemProfile.Tier, encoderSelection.IsHardwareAccelerated);

        var estimates = new RenderPreflightEstimates
        {
            EstimatedFileSizeMB = estimatedFileSizeMB,
            EstimatedDurationMinutes = estimatedDuration,
            RequiredDiskSpaceMB = requiredDiskSpace,
            AvailableDiskSpaceGB = diskInfo.AvailableGB,
            RequiredTempSpaceMB = requiredTempSpace,
            TempDirectory = tempValidation.TempDir
        };

        var canProceed = errors.Count == 0;

        _logger.LogInformation(
            "Preflight validation completed: CanProceed={CanProceed}, Errors={ErrorCount}, Warnings={WarningCount}, CorrelationId={CorrelationId}",
            canProceed, errors.Count, warnings.Count, corrId);

        return new RenderPreflightResult
        {
            CanProceed = canProceed,
            Errors = errors,
            Warnings = warnings,
            RecommendedActions = recommendations,
            Estimates = estimates,
            EncoderSelection = encoderSelection,
            CorrelationId = corrId
        };
    }

    private (List<string> Errors, List<string> Warnings, string TempDir) ValidateTempDirectory(string outputDirectory)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        var tempDir = Path.Combine(Path.GetTempPath(), "aura-render");
        
        try
        {
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
                _logger.LogDebug("Created temp directory: {TempDir}", tempDir);
            }

            var diskInfo = GetDiskSpaceInfo(tempDir);
            if (diskInfo.AvailableGB < MinimumFreeDiskSpaceGB)
            {
                warnings.Add(
                    $"Low disk space in temp directory ({tempDir}): {diskInfo.AvailableGB:F2} GB available. " +
                    "Consider freeing up space for optimal performance.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Cannot access or create temp directory: {ex.Message}");
            _logger.LogError(ex, "Failed to validate temp directory");
        }

        return (errors, warnings, tempDir);
    }

    private (List<string> Errors, List<string> Warnings) ValidateWritePermissions(
        string outputDirectory, string tempDirectory)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var testFiles = new[]
        {
            (outputDirectory, "output-test"),
            (tempDirectory, "temp-test")
        };

        foreach (var (dir, prefix) in testFiles)
        {
            try
            {
                var testFile = Path.Combine(dir, $".{prefix}-{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                _logger.LogDebug("Write permission validated for: {Directory}", dir);
            }
            catch (UnauthorizedAccessException)
            {
                errors.Add($"No write permission for directory: {dir}. " +
                    "Run application with appropriate permissions or choose a different location.");
            }
            catch (Exception ex)
            {
                warnings.Add($"Cannot validate write permission for {dir}: {ex.Message}");
            }
        }

        return (errors, warnings);
    }

    private async Task<EncoderSelection> SelectEncoderAsync(
        ExportPreset preset,
        string? encoderOverride,
        bool preferHardware,
        CancellationToken cancellationToken)
    {
        EncoderConfig selectedEncoder;

        if (!string.IsNullOrEmpty(encoderOverride))
        {
            _logger.LogInformation("Using encoder override: {Encoder}", encoderOverride);
            
            var capabilities = await _hardwareEncoder.DetectHardwareCapabilitiesAsync();
            var isHardware = capabilities.AvailableEncoders.Any(e => 
                e.Contains("nvenc") || e.Contains("amf") || e.Contains("qsv") || e.Contains("videotoolbox"));

            selectedEncoder = new EncoderConfig(
                EncoderName: encoderOverride,
                Description: $"User-specified encoder: {encoderOverride}",
                IsHardwareAccelerated: isHardware && (
                    encoderOverride.Contains("nvenc") || 
                    encoderOverride.Contains("amf") || 
                    encoderOverride.Contains("qsv") ||
                    encoderOverride.Contains("videotoolbox")),
                Parameters: new Dictionary<string, string>
                {
                    ["-c:v"] = encoderOverride
                }
            );
        }
        else
        {
            selectedEncoder = await _hardwareEncoder.SelectBestEncoderAsync(preset, preferHardware);
        }

        var fallbackEncoder = selectedEncoder.IsHardwareAccelerated ? "libx264" : null;

        return new EncoderSelection(
            selectedEncoder.EncoderName,
            selectedEncoder.Description,
            selectedEncoder.IsHardwareAccelerated,
            selectedEncoder.Parameters,
            fallbackEncoder
        );
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

    private double EstimateRenderingDuration(
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

        var hwMultiplier = hardwareAcceleration ? 0.2 : 1.0;

        var estimatedMinutes = videoDuration.TotalMinutes * baseMultiplier * tierMultiplier * hwMultiplier;
        return Math.Max(0.1, estimatedMinutes);
    }
}
