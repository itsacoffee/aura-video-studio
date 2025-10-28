using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates available disk space
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public DiskSpaceHealthCheck(
        ILogger<DiskSpaceHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the drive where the app is running
            var appDirectory = AppContext.BaseDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appDirectory) ?? "/");

            // Calculate free space in GB
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);

            // Get thresholds from configuration (with defaults)
            var thresholdGB = _configuration.GetValue<double>("HealthChecks:DiskSpaceThresholdGB", 1.0);
            var criticalGB = _configuration.GetValue<double>("HealthChecks:DiskSpaceCriticalGB", 0.5);

            var data = new Dictionary<string, object>
            {
                ["free_gb"] = Math.Round(freeSpaceGB, 2),
                ["total_gb"] = Math.Round(totalSpaceGB, 2),
                ["threshold_gb"] = thresholdGB,
                ["critical_gb"] = criticalGB,
                ["drive"] = driveInfo.Name
            };

            // Critical if below critical threshold
            if (freeSpaceGB < criticalGB)
            {
                var message = $"Critical: Only {freeSpaceGB:F2} GB free on {driveInfo.Name}. Need at least {criticalGB} GB.";
                _logger.LogError(message);
                return Task.FromResult(HealthCheckResult.Unhealthy(message, data: data));
            }

            // Degraded if below warning threshold
            if (freeSpaceGB < thresholdGB)
            {
                var message = $"Warning: Only {freeSpaceGB:F2} GB free on {driveInfo.Name}. Recommend at least {thresholdGB} GB.";
                _logger.LogWarning(message);
                return Task.FromResult(HealthCheckResult.Degraded(message, data: data));
            }

            // Healthy
            var healthyMessage = $"Sufficient disk space: {freeSpaceGB:F2} GB free on {driveInfo.Name}";
            return Task.FromResult(HealthCheckResult.Healthy(healthyMessage, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disk space");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking disk space",
                exception: ex));
        }
    }
}
