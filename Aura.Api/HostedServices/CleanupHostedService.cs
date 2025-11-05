using Aura.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that periodically sweeps orphaned temporary files and proxy media
/// </summary>
public class CleanupHostedService : BackgroundService
{
    private readonly ILogger<CleanupHostedService> _logger;
    private readonly CleanupService _cleanupService;
    private readonly TimeSpan _sweepInterval = TimeSpan.FromHours(1); // Run every hour

    public CleanupHostedService(
        ILogger<CleanupHostedService> logger,
        CleanupService cleanupService)
    {
        _logger = logger;
        _cleanupService = cleanupService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup background service started");

        // Wait a bit before first sweep to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Starting orphaned file sweep");
                
                // Perform sweep
                var cleanedCount = _cleanupService.SweepAllOrphaned();
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Orphaned file sweep completed: {Count} directories cleaned", cleanedCount);
                }
                else
                {
                    _logger.LogDebug("Orphaned file sweep completed: no files to clean");
                }
                
                // Get storage statistics
                var (tempSize, proxySize, tempDirs, proxyDirs) = _cleanupService.GetStorageStats();
                _logger.LogDebug("Storage stats: {TempDirs} temp dirs ({TempSizeMB:F2} MB), {ProxyDirs} proxy dirs ({ProxySizeMB:F2} MB)",
                    tempDirs, tempSize / 1024.0 / 1024.0,
                    proxyDirs, proxySize / 1024.0 / 1024.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned file sweep");
            }

            // Wait for next sweep
            await Task.Delay(_sweepInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Cleanup background service stopped");
    }
}
