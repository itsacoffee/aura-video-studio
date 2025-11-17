using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that continuously processes jobs from the queue
/// Runs independently from HTTP requests, enabling true background processing
/// </summary>
public class BackgroundJobProcessorService : BackgroundService
{
    private readonly ILogger<BackgroundJobProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public BackgroundJobProcessorService(
        ILogger<BackgroundJobProcessorService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobProcessorService starting");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobsAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, exit gracefully
                _logger.LogInformation("BackgroundJobProcessorService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background job processor, will retry");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }

            // Wait before next polling cycle
            await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("BackgroundJobProcessorService stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BackgroundJobProcessorService StopAsync called - initiating graceful shutdown");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("BackgroundJobProcessorService StopAsync completed");
    }

    private async Task ProcessNextJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueManager = scope.ServiceProvider.GetRequiredService<BackgroundJobQueueManager>();

        // Check configuration to see how many jobs we can process
        var config = await queueManager.GetConfigurationAsync(stoppingToken).ConfigureAwait(false);
        if (!config.IsEnabled)
        {
            _logger.LogDebug("Queue processing is disabled");
            return;
        }

        // Get current statistics
        var stats = await queueManager.GetStatisticsAsync(stoppingToken).ConfigureAwait(false);
        
        // Calculate how many new jobs we can start
        var availableSlots = Math.Max(0, config.MaxConcurrentJobs - stats.ActiveWorkers);
        
        if (availableSlots == 0)
        {
            _logger.LogDebug(
                "No available slots for new jobs (Active: {Active}, Max: {Max})",
                stats.ActiveWorkers, config.MaxConcurrentJobs);
            return;
        }

        _logger.LogDebug(
            "Processing jobs: {Pending} pending, {Processing} processing, {Available} slots available",
            stats.PendingJobs, stats.ProcessingJobs, availableSlots);

        // Start new jobs up to available slots
        for (int i = 0; i < availableSlots; i++)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var nextJob = await queueManager.DequeueNextJobAsync(stoppingToken).ConfigureAwait(false);
            if (nextJob == null)
            {
                _logger.LogDebug("No more jobs available in queue");
                break;
            }

            // Start job processing in background task
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation(
                        "Starting background processing of job {JobId} (Priority: {Priority})",
                        nextJob.JobId, nextJob.Priority);

                    // Create a new scope for this job
                    using var jobScope = _serviceProvider.CreateScope();
                    var jobQueueManager = jobScope.ServiceProvider.GetRequiredService<BackgroundJobQueueManager>();
                    
                    await jobQueueManager.ProcessJobAsync(nextJob, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing job {JobId}", nextJob.JobId);
                }
            }, stoppingToken);

            // Small delay between starting jobs
            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken).ConfigureAwait(false);
        }
    }
}
