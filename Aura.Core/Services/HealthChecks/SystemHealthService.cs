using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.HealthChecks;

/// <summary>
/// Provides comprehensive system health monitoring
/// </summary>
public class SystemHealthService
{
    private readonly ILogger<SystemHealthService> _logger;
    private readonly IEnumerable<IHealthCheck> _healthChecks;

    public SystemHealthService(
        ILogger<SystemHealthService> logger,
        IEnumerable<IHealthCheck> healthChecks)
    {
        _logger = logger;
        _healthChecks = healthChecks;
    }

    /// <summary>
    /// Performs a complete system health check
    /// </summary>
    public async Task<SystemHealthReport> CheckHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting system health check");
        var startTime = DateTime.UtcNow;

        var checkResults = new List<HealthCheckResult>();

        foreach (var healthCheck in _healthChecks)
        {
            try
            {
                var result = await healthCheck.CheckHealthAsync(ct);
                checkResults.Add(result);
                
                _logger.LogInformation(
                    "Health check {Name}: {Status}",
                    healthCheck.Name,
                    result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check {Name} failed with exception", healthCheck.Name);
                checkResults.Add(new HealthCheckResult
                {
                    Name = healthCheck.Name,
                    Status = HealthStatus.Unhealthy,
                    Message = $"Exception during health check: {ex.Message}",
                    Exception = ex
                });
            }
        }

        var duration = DateTime.UtcNow - startTime;
        var overallStatus = DetermineOverallStatus(checkResults);

        var report = new SystemHealthReport
        {
            Status = overallStatus,
            CheckResults = checkResults,
            TotalChecks = checkResults.Count,
            HealthyChecks = checkResults.Count(r => r.Status == HealthStatus.Healthy),
            DegradedChecks = checkResults.Count(r => r.Status == HealthStatus.Degraded),
            UnhealthyChecks = checkResults.Count(r => r.Status == HealthStatus.Unhealthy),
            CheckDuration = duration,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation(
            "System health check complete: {Status} ({Healthy}/{Total} healthy) in {Duration}ms",
            overallStatus,
            report.HealthyChecks,
            report.TotalChecks,
            duration.TotalMilliseconds);

        return report;
    }

    private HealthStatus DetermineOverallStatus(List<HealthCheckResult> results)
    {
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;

        if (results.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }
}

/// <summary>
/// Interface for individual health checks
/// </summary>
public interface IHealthCheck
{
    string Name { get; }
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>
/// Health check result
/// </summary>
public class HealthCheckResult
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public Exception? Exception { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Overall system health report
/// </summary>
public class SystemHealthReport
{
    public HealthStatus Status { get; set; }
    public List<HealthCheckResult> CheckResults { get; set; } = new();
    public int TotalChecks { get; set; }
    public int HealthyChecks { get; set; }
    public int DegradedChecks { get; set; }
    public int UnhealthyChecks { get; set; }
    public TimeSpan CheckDuration { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
