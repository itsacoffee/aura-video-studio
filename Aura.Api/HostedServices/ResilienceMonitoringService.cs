using Aura.Core.Resilience.ErrorTracking;
using Aura.Core.Resilience.Idempotency;
using Aura.Core.Resilience.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that monitors resilience health and performs maintenance
/// </summary>
public class ResilienceMonitoringService : BackgroundService
{
    private readonly ILogger<ResilienceMonitoringService> _logger;
    private readonly ResilienceHealthMonitor _healthMonitor;
    private readonly IdempotencyManager _idempotencyManager;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public ResilienceMonitoringService(
        ILogger<ResilienceMonitoringService> logger,
        ResilienceHealthMonitor healthMonitor,
        IdempotencyManager idempotencyManager)
    {
        _logger = logger;
        _healthMonitor = healthMonitor;
        _idempotencyManager = idempotencyManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Resilience Monitoring Service started");

        var lastCleanup = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform health check and alert if needed
                _healthMonitor.CheckAndAlert();

                // Get health report for logging
                var report = _healthMonitor.GetHealthReport();
                
                if (report.OverallStatus != HealthStatus.Healthy)
                {
                    _logger.LogWarning(
                        "Resilience health status: {Status}. Issues: {Issues}",
                        report.OverallStatus,
                        string.Join("; ", report.Issues));
                }
                else
                {
                    _logger.LogDebug("Resilience health check: All systems healthy");
                }

                // Perform cleanup if needed
                if (DateTime.UtcNow - lastCleanup >= _cleanupInterval)
                {
                    PerformCleanup();
                    lastCleanup = DateTime.UtcNow;
                }

                await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in resilience monitoring service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Resilience Monitoring Service stopped");
    }

    private void PerformCleanup()
    {
        _logger.LogInformation("Performing resilience system cleanup");

        // Clean up expired idempotency records
        var removed = _idempotencyManager.CleanupExpired();
        _logger.LogInformation("Cleaned up {Count} expired idempotency records", removed);

        // Clean up old alerts
        _healthMonitor.ClearOldAlerts(TimeSpan.FromDays(1));
        _logger.LogInformation("Cleaned up old health alerts");
    }
}
