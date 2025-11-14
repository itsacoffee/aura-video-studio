using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates critical dependencies like FFmpeg, GPU, and system hardware
/// </summary>
public class DependencyHealthCheck : IHealthCheck
{
    private readonly ILogger<DependencyHealthCheck> _logger;
    private readonly HardwareDetector _hardwareDetector;
    private readonly IFfmpegLocator _ffmpegLocator;

    public DependencyHealthCheck(
        ILogger<DependencyHealthCheck> logger,
        HardwareDetector hardwareDetector,
        IFfmpegLocator ffmpegLocator)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _ffmpegLocator = ffmpegLocator;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check FFmpeg availability
            var ffmpegResult = await _ffmpegLocator.CheckAllCandidatesAsync(null, cancellationToken).ConfigureAwait(false);
            var ffmpegAvailable = ffmpegResult.Found && !string.IsNullOrEmpty(ffmpegResult.FfmpegPath);

            // Detect system hardware and dependencies
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);

            var data = new Dictionary<string, object>
            {
                ["ffmpeg_available"] = ffmpegAvailable,
                ["platform"] = Environment.OSVersion.Platform.ToString(),
                ["gpu_available"] = systemProfile.Gpu != null,
                ["tier"] = systemProfile.Tier.ToString()
            };

            if (ffmpegAvailable)
            {
                data["ffmpeg_path"] = ffmpegResult.FfmpegPath!;
                if (!string.IsNullOrEmpty(ffmpegResult.VersionString))
                {
                    data["ffmpeg_version"] = ffmpegResult.VersionString;
                }
            }

            if (systemProfile.Gpu != null)
            {
                data["nvenc_available"] = systemProfile.EnableNVENC;
                data["gpu_vendor"] = systemProfile.Gpu.Vendor;
                data["gpu_model"] = systemProfile.Gpu.Model;
                data["gpu_vram_gb"] = systemProfile.Gpu.VramGB;
            }

            // If FFmpeg not available, return degraded status
            if (!ffmpegAvailable)
            {
                _logger.LogWarning("FFmpeg not available - video rendering will be disabled");
                return HealthCheckResult.Degraded(
                    "FFmpeg not available - video rendering disabled",
                    data: data);
            }

            // All critical dependencies available
            return HealthCheckResult.Healthy(
                "All dependencies available",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking dependencies");
            return HealthCheckResult.Unhealthy(
                "Error checking dependencies",
                exception: ex);
        }
    }
}
