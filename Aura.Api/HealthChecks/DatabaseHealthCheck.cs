using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates database connectivity and responsiveness
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseHealthCheck> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseHealthCheck(
        ILogger<DatabaseHealthCheck> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            
            // Test basic connectivity with a simple query
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                _logger.LogError("Database connection failed");
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        ["connection_available"] = false
                    });
            }

            // Test query execution with a simple count
            var projectCount = await dbContext.ProjectStates.CountAsync(cancellationToken);
            
            stopwatch.Stop();
            var responseTimeMs = stopwatch.ElapsedMilliseconds;

            var data = new Dictionary<string, object>
            {
                ["connection_available"] = true,
                ["response_time_ms"] = responseTimeMs,
                ["project_count"] = projectCount,
                ["provider"] = dbContext.Database.ProviderName ?? "Unknown"
            };

            // Check for slow response times
            const int warningThresholdMs = 500;
            const int criticalThresholdMs = 2000;

            if (responseTimeMs > criticalThresholdMs)
            {
                _logger.LogWarning(
                    "Database response time critical: {ResponseTime}ms (threshold: {Threshold}ms)",
                    responseTimeMs, criticalThresholdMs);
                    
                return HealthCheckResult.Degraded(
                    $"Database responding slowly: {responseTimeMs}ms",
                    data: data);
            }

            if (responseTimeMs > warningThresholdMs)
            {
                _logger.LogWarning(
                    "Database response time elevated: {ResponseTime}ms (threshold: {Threshold}ms)",
                    responseTimeMs, warningThresholdMs);
                    
                return HealthCheckResult.Degraded(
                    $"Database response time elevated: {responseTimeMs}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Database healthy (response: {responseTimeMs}ms)",
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["elapsed_ms"] = stopwatch.ElapsedMilliseconds
                });
        }
    }
}
