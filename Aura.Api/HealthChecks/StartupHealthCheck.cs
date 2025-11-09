using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates startup dependencies and configuration
/// Used to ensure all services are ready before accepting traffic
/// </summary>
public class StartupHealthCheck : IHealthCheck
{
    private readonly ILogger<StartupHealthCheck> _logger;
    private bool _isReady;

    public StartupHealthCheck(ILogger<StartupHealthCheck> logger)
    {
        _logger = logger;
        _isReady = false;
    }

    /// <summary>
    /// Mark the service as ready to accept traffic
    /// This should be called after all initialization is complete
    /// </summary>
    public void MarkAsReady()
    {
        _isReady = true;
        _logger.LogInformation("Application startup complete - ready to accept traffic");
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Application is still starting up",
                    data: new Dictionary<string, object>
                    {
                        ["ready"] = false,
                        ["timestamp"] = DateTime.UtcNow
                    }));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Application is ready",
                data: new Dictionary<string, object>
                {
                    ["ready"] = true,
                    ["timestamp"] = DateTime.UtcNow
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking startup status");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking startup status",
                exception: ex));
        }
    }
}
