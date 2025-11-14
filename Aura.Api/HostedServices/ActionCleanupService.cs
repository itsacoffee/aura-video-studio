using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that periodically cleans up expired actions
/// </summary>
public class ActionCleanupService : BackgroundService
{
    private readonly ILogger<ActionCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval;

    public ActionCleanupService(
        ILogger<ActionCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cleanupInterval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Action cleanup service starting. Cleanup interval: {Interval}", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("Running action cleanup job");
                
                using var scope = _serviceProvider.CreateScope();
                var actionService = scope.ServiceProvider.GetRequiredService<IActionService>();
                
                var cleanedCount = await actionService.CleanupExpiredActionsAsync(stoppingToken).ConfigureAwait(false);
                _logger.LogInformation("Action cleanup completed. Cleaned up {Count} expired actions", cleanedCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Action cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during action cleanup");
            }
        }

        _logger.LogInformation("Action cleanup service stopped");
    }
}
