using Aura.Api.Configuration;
using Aura.Core.Monitoring;
using Microsoft.Extensions.Options;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service to export metrics to external systems
/// </summary>
public class MetricsExporterService : BackgroundService
{
    private readonly MetricsCollector _metrics;
    private readonly BusinessMetricsCollector _businessMetrics;
    private readonly ILogger<MetricsExporterService> _logger;
    private readonly MonitoringOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public MetricsExporterService(
        MetricsCollector metrics,
        BusinessMetricsCollector businessMetrics,
        ILogger<MetricsExporterService> logger,
        IOptions<MonitoringOptions> options,
        IServiceProvider serviceProvider)
    {
        _metrics = metrics;
        _businessMetrics = businessMetrics;
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics Exporter Service started");

        var interval = TimeSpan.FromSeconds(_options.MetricsExportIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);

                if (_options.EnableCustomMetrics)
                {
                    await ExportMetricsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting metrics");
            }
        }

        _logger.LogInformation("Metrics Exporter Service stopped");
    }

    private async Task ExportMetricsAsync(CancellationToken ct)
    {
        try
        {
            var snapshot = _metrics.GetSnapshot();

            // Log metrics snapshot
            _logger.LogInformation(
                "Metrics snapshot: {GaugeCount} gauges, {CounterCount} counters, {HistogramCount} histograms",
                snapshot.Gauges.Count, snapshot.Counters.Count, snapshot.Histograms.Count);

            // Export to Application Insights if configured
            if (_options.EnableApplicationInsights && !string.IsNullOrEmpty(_options.ApplicationInsightsConnectionString))
            {
                // Application Insights automatically collects metrics via its SDK
                // Custom metrics can be sent via TelemetryClient
                _logger.LogDebug("Exporting metrics to Application Insights");
            }

            // Export to Log Analytics if configured
            if (!string.IsNullOrEmpty(_options.LogAnalyticsWorkspaceId))
            {
                await ExportToLogAnalyticsAsync(snapshot, ct);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export metrics snapshot");
        }
    }

    private async Task ExportToLogAnalyticsAsync(MetricsSnapshot snapshot, CancellationToken ct)
    {
        try
        {
            // This would integrate with Azure Log Analytics REST API
            // For now, we'll log the metrics in a structured format that can be ingested
            _logger.LogInformation(
                "Log Analytics export: {@MetricsSnapshot}",
                new
                {
                    timestamp = snapshot.Timestamp,
                    gauges = snapshot.Gauges.Count,
                    counters = snapshot.Counters.Count,
                    histograms = snapshot.Histograms.Count
                });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export metrics to Log Analytics");
        }
    }
}
