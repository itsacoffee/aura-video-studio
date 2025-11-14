using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that performs scheduled health checks
/// </summary>
public class HealthCheckBackgroundService : BackgroundService
{
    private readonly ILogger<HealthCheckBackgroundService> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly TimeSpan _checkInterval;

    public HealthCheckBackgroundService(
        ILogger<HealthCheckBackgroundService> logger,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _checkInterval = TimeSpan.FromMinutes(5); // Run health checks every 5 minutes
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check background service starting");

        // Wait a bit after startup before running first check
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Running scheduled health check");
                var result = await _healthCheckService.CheckReadinessAsync(stoppingToken).ConfigureAwait(false);

                if (result.Status == Aura.Api.Models.HealthStatus.Unhealthy)
                {
                    _logger.LogWarning("Health check failed: {Errors}", string.Join(", ", result.Errors));
                }
                else if (result.Status == Aura.Api.Models.HealthStatus.Degraded)
                {
                    _logger.LogInformation("Health check degraded: {Errors}", string.Join(", ", result.Errors));
                }
                else
                {
                    _logger.LogDebug("Health check passed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled health check: {Message}", ex.Message);
            }

            // Wait for next check interval
            await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Health check background service stopping");
    }
}
