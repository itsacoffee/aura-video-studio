using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Services.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service for analytics maintenance tasks
/// Handles automatic cleanup and data aggregation based on retention settings
/// </summary>
public class AnalyticsMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyticsMaintenanceService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public AnalyticsMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<AnalyticsMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Maintenance Service started");

        // Wait a bit before starting to let the app fully initialize
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing analytics maintenance");
            }

            // Wait for next check
            await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Analytics Maintenance Service stopped");
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IAnalyticsCleanupService>();

        try
        {
            // Get retention settings
            var settings = await context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (settings?.AutoCleanupEnabled != true)
            {
                return;
            }

            var currentHour = DateTime.UtcNow.Hour;
            
            // Only run cleanup at the configured hour (default 3 AM UTC)
            if (currentHour == settings.CleanupHourUtc)
            {
                _logger.LogInformation("Starting scheduled analytics maintenance");

                // Run cleanup
                await cleanupService.CleanupAsync(cancellationToken).ConfigureAwait(false);

                // Run aggregation
                if (settings.AggregateOldData)
                {
                    await cleanupService.AggregateOldDataAsync(cancellationToken).ConfigureAwait(false);
                }

                // Check database size
                var dbSize = await cleanupService.GetDatabaseSizeBytesAsync(cancellationToken).ConfigureAwait(false);
                var dbSizeMB = dbSize / (1024.0 * 1024.0);
                
                _logger.LogInformation(
                    "Analytics maintenance completed. Database size: {Size:F2} MB",
                    dbSizeMB);

                if (settings.MaxDatabaseSizeMB > 0 && dbSizeMB > settings.MaxDatabaseSizeMB * 0.9)
                {
                    _logger.LogWarning(
                        "Analytics database approaching size limit: {Current:F2}/{Limit} MB",
                        dbSizeMB, settings.MaxDatabaseSizeMB);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform analytics maintenance");
        }
    }
}
