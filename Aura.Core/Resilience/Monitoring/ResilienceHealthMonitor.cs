using Aura.Core.Resilience.ErrorTracking;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Resilience.Monitoring;

/// <summary>
/// Monitors overall resilience health and triggers alerts
/// </summary>
public class ResilienceHealthMonitor
{
    private readonly ILogger<ResilienceHealthMonitor> _logger;
    private readonly ErrorMetricsCollector _metricsCollector;
    private readonly CircuitBreakerStateManager _circuitBreakerManager;
    private readonly List<HealthAlert> _activeAlerts = new();

    public ResilienceHealthMonitor(
        ILogger<ResilienceHealthMonitor> logger,
        ErrorMetricsCollector metricsCollector,
        CircuitBreakerStateManager circuitBreakerManager)
    {
        _logger = logger;
        _metricsCollector = metricsCollector;
        _circuitBreakerManager = circuitBreakerManager;
    }

    /// <summary>
    /// Performs a health check of all resilience systems
    /// </summary>
    public ResilienceHealthReport GetHealthReport()
    {
        var report = new ResilienceHealthReport
        {
            Timestamp = DateTime.UtcNow,
            OverallStatus = HealthStatus.Healthy
        };

        // Check circuit breaker states
        var degradedServices = _circuitBreakerManager.GetDegradedServices().ToList();
        if (degradedServices.Count != 0)
        {
            report.OverallStatus = HealthStatus.Degraded;
            report.Issues.Add($"{degradedServices.Count} service(s) have open or half-open circuits");
            
            foreach (var service in degradedServices)
            {
                report.DegradedServices.Add(service.ServiceName);
            }
        }

        // Check error rates
        var metrics = _metricsCollector.GetAllMetrics();
        foreach (var (serviceName, serviceMetrics) in metrics)
        {
            var errorRate = _metricsCollector.GetErrorRate(serviceName, TimeSpan.FromMinutes(5));
            
            if (errorRate > 10) // More than 10 errors per minute
            {
                report.OverallStatus = HealthStatus.Degraded;
                report.Issues.Add($"High error rate for {serviceName}: {errorRate:F1} errors/min");
                report.HighErrorRateServices.Add(serviceName);
            }

            // Check if any service has a very high error rate (> 50%)
            if (serviceMetrics.ErrorRate > 0.5 && serviceMetrics.TotalErrors + serviceMetrics.TotalSuccesses > 10)
            {
                report.OverallStatus = HealthStatus.Unhealthy;
                report.Issues.Add($"Critical error rate for {serviceName}: {serviceMetrics.ErrorRate:P0}");
            }
        }

        // Check for recent error spikes
        var recentErrors = _metricsCollector.GetRecentErrors(100);
        var errorsByService = recentErrors
            .GroupBy(e => e.ServiceName)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (serviceName, count) in errorsByService.Where(kvp => kvp.Value > 20))
        {
            report.Issues.Add($"Recent error spike for {serviceName}: {count} errors in last 100 events");
            report.OverallStatus = HealthStatus.Degraded;
        }

        report.MetricsSnapshot = metrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new ServiceMetricsSnapshot
            {
                TotalErrors = kvp.Value.TotalErrors,
                TotalSuccesses = kvp.Value.TotalSuccesses,
                ErrorRate = kvp.Value.ErrorRate,
                LastErrorTime = kvp.Value.LastErrorTime
            });

        return report;
    }

    /// <summary>
    /// Triggers an alert if conditions are met
    /// </summary>
    public void CheckAndAlert()
    {
        var report = GetHealthReport();

        if (report.OverallStatus == HealthStatus.Unhealthy)
        {
            TriggerAlert(AlertSeverity.Critical, "System resilience is unhealthy", report.Issues);
        }
        else if (report.OverallStatus == HealthStatus.Degraded)
        {
            TriggerAlert(AlertSeverity.Warning, "System resilience is degraded", report.Issues);
        }
    }

    private void TriggerAlert(AlertSeverity severity, string message, List<string> issues)
    {
        var alert = new HealthAlert
        {
            Severity = severity,
            Message = message,
            Issues = issues,
            Timestamp = DateTime.UtcNow
        };

        _activeAlerts.Add(alert);

        _logger.Log(
            severity == AlertSeverity.Critical ? LogLevel.Critical : LogLevel.Warning,
            "Resilience health alert: {Message}. Issues: {Issues}",
            message,
            string.Join("; ", issues));
    }

    /// <summary>
    /// Gets active alerts
    /// </summary>
    public IReadOnlyList<HealthAlert> GetActiveAlerts()
    {
        // Return alerts from the last hour
        var cutoff = DateTime.UtcNow.AddHours(-1);
        return _activeAlerts.Where(a => a.Timestamp >= cutoff).ToList();
    }

    /// <summary>
    /// Clears old alerts
    /// </summary>
    public void ClearOldAlerts(TimeSpan age)
    {
        var cutoff = DateTime.UtcNow - age;
        _activeAlerts.RemoveAll(a => a.Timestamp < cutoff);
    }
}

/// <summary>
/// Overall resilience health report
/// </summary>
public class ResilienceHealthReport
{
    public DateTime Timestamp { get; init; }
    public HealthStatus OverallStatus { get; set; }
    public List<string> Issues { get; init; } = new();
    public List<string> DegradedServices { get; init; } = new();
    public List<string> HighErrorRateServices { get; init; } = new();
    public Dictionary<string, ServiceMetricsSnapshot> MetricsSnapshot { get; set; } = new();
}

/// <summary>
/// Snapshot of service metrics
/// </summary>
public class ServiceMetricsSnapshot
{
    public long TotalErrors { get; init; }
    public long TotalSuccesses { get; init; }
    public double ErrorRate { get; init; }
    public DateTime? LastErrorTime { get; init; }
}

/// <summary>
/// Health status levels
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Health alert
/// </summary>
public class HealthAlert
{
    public required AlertSeverity Severity { get; init; }
    public required string Message { get; init; }
    public required List<string> Issues { get; init; }
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
