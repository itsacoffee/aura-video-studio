using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that performs periodic maintenance on the job queue
/// - Cleans up old completed/failed jobs
/// - Recovers stale jobs
/// - Updates statistics
/// </summary>
public class QueueMaintenanceService : BackgroundService
{
    private readonly ILogger<QueueMaintenanceService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(1);

    public QueueMaintenanceService(
        ILogger<QueueMaintenanceService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueMaintenanceService starting");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("QueueMaintenanceService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in queue maintenance, will retry");
            }

            // Wait before next maintenance cycle
            await Task.Delay(_maintenanceInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("QueueMaintenanceService stopped");
    }

    private async Task PerformMaintenanceAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting queue maintenance");

        using var scope = _serviceProvider.CreateScope();
        var queueManager = scope.ServiceProvider.GetRequiredService<BackgroundJobQueueManager>();

        try
        {
            // Clean up old jobs
            var deletedCount = await queueManager.CleanupOldJobsAsync(stoppingToken).ConfigureAwait(false);
            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old jobs", deletedCount);
            }

            // Get and log statistics
            var stats = await queueManager.GetStatisticsAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Queue statistics: Total={Total}, Pending={Pending}, Processing={Processing}, " +
                "Completed={Completed}, Failed={Failed}, Cancelled={Cancelled}, Active Workers={Workers}",
                stats.TotalJobs, stats.PendingJobs, stats.ProcessingJobs,
                stats.CompletedJobs, stats.FailedJobs, stats.CancelledJobs, stats.ActiveWorkers);

            _logger.LogInformation("Queue maintenance completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during queue maintenance");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueMaintenanceService stop requested");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
