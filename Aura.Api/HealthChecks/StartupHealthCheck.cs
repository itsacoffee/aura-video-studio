using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates startup dependencies and configuration
/// Used to ensure all services are ready before accepting traffic
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly ILogger<StartupHealthCheck> _logger;
    private readonly FFmpegConfigurationStore? _ffmpegConfigStore;
    private bool _isReady;

    public StartupHealthCheck(
        ILogger<StartupHealthCheck> logger,
        FFmpegConfigurationStore? ffmpegConfigStore = null)
    {
        _logger = logger;
        _ffmpegConfigStore = ffmpegConfigStore;
        _isReady = false;
    }

    /// <summary>
    /// Mark the service as ready to accept traffic
    /// This should be called after all initialization is complete
    /// </summary>
    public void MarkAsReady()
    {
        _isReady = true;
        _logger.LogInformation("Application startup complete - ready to accept traffic");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["ready"] = _isReady,
                ["timestamp"] = DateTime.UtcNow
            };

            // Check FFmpeg configuration status if available
            if (_ffmpegConfigStore != null)
            {
                try
                {
                    var ffmpegConfig = await _ffmpegConfigStore.LoadAsync(cancellationToken).ConfigureAwait(false);
                    var ffmpegHealthy = ffmpegConfig != null && 
                                        !string.IsNullOrEmpty(ffmpegConfig.Path) && 
                                        File.Exists(ffmpegConfig.Path);
                    
                    data["ffmpeg_configured"] = ffmpegHealthy;
                    
                    if (ffmpegHealthy)
                    {
                        data["ffmpeg_path"] = ffmpegConfig.Path!;
                        data["ffmpeg_source"] = ffmpegConfig.Source ?? "Unknown";
                    }
                    else
                    {
                        data["ffmpeg_warning"] = "FFmpeg not configured or not found";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check FFmpeg configuration during health check");
                    data["ffmpeg_configured"] = false;
                    data["ffmpeg_error"] = ex.Message;
                }
            }

            if (!_isReady)
            {
                return HealthCheckResult.Unhealthy(
                    "Application is still starting up",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Application is ready",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking startup status");
            return HealthCheckResult.Unhealthy(
                "Error checking startup status",
                exception: ex);
        }
    }
}
