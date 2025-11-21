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

        // Wait longer for application startup to complete
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);

        // Verify schema before starting maintenance
        var healthCheck = await CheckDatabaseSchemaAsync(stoppingToken).ConfigureAwait(false);
        if (!healthCheck.IsHealthy)
        {
            _logger.LogWarning(
                "Analytics Maintenance Service cannot start: {Message}",
                healthCheck.Message);
            _logger.LogWarning("SOLUTION: Run database migrations or delete the database to recreate with correct schema");
            _logger.LogInformation("Analytics Maintenance Service exiting gracefully due to missing database schema");
            return;
        }

        _logger.LogInformation("Database schema validated successfully, starting analytics maintenance");

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

    /// <summary>
    /// Checks if the database schema is valid and creates default settings if needed
    /// </summary>
    private async Task<Aura.Core.Services.ServiceHealthCheckResult> CheckDatabaseSchemaAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            // Check if AnalyticsRetentionSettings table exists and has data
            var settingsExist = await context.AnalyticsRetentionSettings.AnyAsync(cancellationToken).ConfigureAwait(false);

            if (!settingsExist)
            {
                _logger.LogWarning("AnalyticsRetentionSettings table has no data, creating default settings");

                // Create default settings
                var defaultSettings = new AnalyticsRetentionSettingsEntity
                {
                    Id = "default",
                    IsEnabled = true,
                    AutoCleanupEnabled = true,
                    UsageStatisticsRetentionDays = 90,
                    PerformanceMetricsRetentionDays = 30,
                    CostTrackingRetentionDays = 365,
                    AggregateOldData = true,
                    AggregationThresholdDays = 30,
                    MaxDatabaseSizeMB = 500,
                    CleanupHourUtc = 2,
                    TrackSuccessOnly = false,
                    CollectHardwareMetrics = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.AnalyticsRetentionSettings.Add(defaultSettings);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Created default AnalyticsRetentionSettings");
            }

            return new Aura.Core.Services.ServiceHealthCheckResult(true, "Database schema is valid");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // SQLITE_ERROR - table doesn't exist
            var message = $"AnalyticsRetentionSettings table does not exist. Error: {ex.Message}";
            _logger.LogWarning(ex, message);
            return new Aura.Core.Services.ServiceHealthCheckResult(false, message, ex);
        }
        catch (Exception ex)
        {
            var message = $"Failed to check database schema: {ex.Message}";
            _logger.LogError(ex, message);
            return new Aura.Core.Services.ServiceHealthCheckResult(false, message, ex);
        }
    }
}
