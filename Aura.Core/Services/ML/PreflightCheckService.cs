using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// Preflight checks for ML training to ensure system resources are adequate
/// </summary>
public class PreflightCheckService
{
    private readonly ILogger<PreflightCheckService> _logger;
    private readonly HardwareDetector _hardwareDetector;

    public PreflightCheckService(
        ILogger<PreflightCheckService> logger,
        HardwareDetector hardwareDetector)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
    }

    /// <summary>
    /// Perform comprehensive preflight check for training
    /// </summary>
    public async Task<PreflightCheckResult> CheckSystemCapabilitiesAsync(
        int annotationCount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting preflight check for ML training with {AnnotationCount} annotations", annotationCount);

        var result = new PreflightCheckResult
        {
            Timestamp = DateTime.UtcNow,
            AnnotationCount = annotationCount
        };

        try
        {
            // Check GPU/VRAM
            await CheckGpuCapabilitiesAsync(result, cancellationToken);

            // Check RAM
            CheckRamCapabilities(result);

            // Check disk space
            CheckDiskSpace(result);

            // Estimate training time
            EstimateTrainingTime(result);

            // Determine if requirements are met
            DetermineRequirementsMet(result);

            _logger.LogInformation(
                "Preflight check completed: MeetsMinimumRequirements={Meets}, Warnings={WarningCount}",
                result.MeetsMinimumRequirements,
                result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during preflight check");
            result.Errors.Add("Failed to complete preflight check: " + ex.Message);
            result.MeetsMinimumRequirements = false;
        }

        return result;
    }

    private async Task CheckGpuCapabilitiesAsync(PreflightCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            var gpuInfo = await _hardwareDetector.DetectGpuAsync(cancellationToken);

            result.HasGpu = gpuInfo.IsAvailable;
            result.GpuName = gpuInfo.Name;
            result.GpuVramGb = gpuInfo.VramMb / 1024.0;

            if (!result.HasGpu)
            {
                result.Warnings.Add("No GPU detected - training will use CPU and will be significantly slower (10-50x)");
                result.Recommendations.Add("Consider using a system with a dedicated GPU (NVIDIA recommended) for faster training");
            }
            else if (result.GpuVramGb < 2.0)
            {
                result.Warnings.Add($"Low VRAM detected ({result.GpuVramGb:F1} GB) - training may be slow or fail with out-of-memory errors");
                result.Recommendations.Add("Minimum 2GB VRAM recommended; 4GB+ ideal for ML training");
            }
            else if (result.GpuVramGb < 4.0)
            {
                result.Warnings.Add($"VRAM is adequate but limited ({result.GpuVramGb:F1} GB) - consider reducing batch size if training fails");
            }

            _logger.LogDebug("GPU check: HasGpu={HasGpu}, Name={Name}, VRAM={Vram}GB",
                result.HasGpu, result.GpuName, result.GpuVramGb);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not detect GPU capabilities");
            result.Warnings.Add("Could not detect GPU - training will default to CPU");
        }
    }

    private void CheckRamCapabilities(PreflightCheckResult result)
    {
        try
        {
            var memoryInfo = _hardwareDetector.GetMemoryInfo();

            result.TotalRamGb = memoryInfo.TotalGB;
            result.AvailableRamGb = memoryInfo.AvailableGB;

            if (result.TotalRamGb < 8)
            {
                result.Warnings.Add($"Insufficient RAM ({result.TotalRamGb:F1} GB) - minimum 8GB required, training may fail");
                result.MeetsMinimumRequirements = false;
            }
            else if (result.AvailableRamGb < 4)
            {
                result.Warnings.Add($"Low available RAM ({result.AvailableRamGb:F1} GB) - close other applications before training");
                result.Recommendations.Add("Ensure at least 4GB RAM is available for training");
            }
            else if (result.TotalRamGb < 16)
            {
                result.Recommendations.Add("16GB+ RAM recommended for optimal training performance");
            }

            _logger.LogDebug("RAM check: Total={Total}GB, Available={Available}GB",
                result.TotalRamGb, result.AvailableRamGb);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check RAM");
            result.Warnings.Add("Could not determine available RAM");
        }
    }

    private void CheckDiskSpace(PreflightCheckResult result)
    {
        try
        {
            var diskInfo = _hardwareDetector.GetDiskInfo();

            result.AvailableDiskSpaceGb = diskInfo.AvailableGB;

            if (result.AvailableDiskSpaceGb < 2)
            {
                result.Warnings.Add($"Critically low disk space ({result.AvailableDiskSpaceGb:F1} GB) - training may fail");
                result.MeetsMinimumRequirements = false;
            }
            else if (result.AvailableDiskSpaceGb < 5)
            {
                result.Warnings.Add($"Low disk space ({result.AvailableDiskSpaceGb:F1} GB) - ensure sufficient space for model files");
                result.Recommendations.Add("Free up at least 5GB disk space before training");
            }

            _logger.LogDebug("Disk check: Available={Available}GB", result.AvailableDiskSpaceGb);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check disk space");
            result.Warnings.Add("Could not determine available disk space");
        }
    }

    private void EstimateTrainingTime(PreflightCheckResult result)
    {
        try
        {
            // Rough time estimation based on annotation count and hardware
            double baseTimeMinutes;

            if (result.HasGpu)
            {
                // GPU training: ~0.5-2 minutes per 100 annotations depending on GPU quality
                baseTimeMinutes = (result.AnnotationCount / 100.0) * (result.GpuVramGb >= 4 ? 0.5 : 1.5);
            }
            else
            {
                // CPU training: ~5-20 minutes per 100 annotations depending on CPU
                baseTimeMinutes = (result.AnnotationCount / 100.0) * 10.0;
            }

            // Account for RAM limitations
            if (result.TotalRamGb < 16)
            {
                baseTimeMinutes *= 1.5;
            }

            // Minimum 1 minute, cap at 120 minutes for estimation
            result.EstimatedTrainingTimeMinutes = Math.Max(1, Math.Min(120, Math.Ceiling(baseTimeMinutes)));

            _logger.LogDebug("Training time estimate: {Minutes} minutes", result.EstimatedTrainingTimeMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not estimate training time");
            result.EstimatedTrainingTimeMinutes = 0;
        }
    }

    private void DetermineRequirementsMet(PreflightCheckResult result)
    {
        // Already set to false if critical issues found
        if (!result.MeetsMinimumRequirements)
        {
            return;
        }

        // Check minimums
        var criticalIssues = new List<string>();

        if (result.AnnotationCount < 20)
        {
            criticalIssues.Add("Insufficient training data - minimum 20 annotations required, 100+ recommended");
        }

        if (result.TotalRamGb < 8)
        {
            criticalIssues.Add("Insufficient RAM - minimum 8GB required");
        }

        if (result.AvailableDiskSpaceGb < 2)
        {
            criticalIssues.Add("Insufficient disk space - minimum 2GB required");
        }

        if (criticalIssues.Any())
        {
            result.MeetsMinimumRequirements = false;
            result.Warnings.AddRange(criticalIssues);
        }
        else
        {
            result.MeetsMinimumRequirements = true;
        }

        // Add general recommendations
        if (result.AnnotationCount < 100)
        {
            result.Recommendations.Add("100+ annotations recommended for better model quality");
        }

        if (!result.HasGpu)
        {
            result.Recommendations.Add("GPU highly recommended for acceptable training times");
        }
    }
}

/// <summary>
/// Result of preflight check
/// </summary>
public class PreflightCheckResult
{
    public DateTime Timestamp { get; set; }
    public int AnnotationCount { get; set; }
    
    // GPU/VRAM
    public bool HasGpu { get; set; }
    public string? GpuName { get; set; }
    public double GpuVramGb { get; set; }
    
    // RAM
    public double TotalRamGb { get; set; }
    public double AvailableRamGb { get; set; }
    
    // Disk
    public double AvailableDiskSpaceGb { get; set; }
    
    // Estimates
    public int EstimatedTrainingTimeMinutes { get; set; }
    
    // Results
    public bool MeetsMinimumRequirements { get; set; } = true;
    public List<string> Warnings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
