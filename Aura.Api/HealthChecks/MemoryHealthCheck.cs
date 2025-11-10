using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that monitors memory usage and reports degraded status when thresholds are exceeded
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public MemoryHealthCheck(
        ILogger<MemoryHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            
            // Get memory metrics
            var workingSetMB = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
            var privateMemoryMB = currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
            var virtualMemoryMB = currentProcess.VirtualMemorySize64 / (1024.0 * 1024.0);
            
            // Get GC statistics
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            var totalMemoryMB = GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0);
            
            // Get thresholds from configuration (with defaults)
            var warningThresholdMB = _configuration.GetValue<double>("HealthChecks:MemoryWarningThresholdMB", 1024.0);
            var criticalThresholdMB = _configuration.GetValue<double>("HealthChecks:MemoryCriticalThresholdMB", 2048.0);
            
            var data = new Dictionary<string, object>
            {
                ["working_set_mb"] = Math.Round(workingSetMB, 2),
                ["private_memory_mb"] = Math.Round(privateMemoryMB, 2),
                ["virtual_memory_mb"] = Math.Round(virtualMemoryMB, 2),
                ["gc_total_memory_mb"] = Math.Round(totalMemoryMB, 2),
                ["gc_gen0_collections"] = gen0Collections,
                ["gc_gen1_collections"] = gen1Collections,
                ["gc_gen2_collections"] = gen2Collections,
                ["warning_threshold_mb"] = warningThresholdMB,
                ["critical_threshold_mb"] = criticalThresholdMB
            };

            // Try to get system-wide memory info (platform-specific)
            try
            {
                var gcMemoryInfo = GC.GetGCMemoryInfo();
                var totalAvailableMemoryMB = gcMemoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0);
                var heapSizeMB = gcMemoryInfo.HeapSizeBytes / (1024.0 * 1024.0);
                var fragmentedMB = gcMemoryInfo.FragmentedBytes / (1024.0 * 1024.0);
                
                data["total_available_memory_mb"] = Math.Round(totalAvailableMemoryMB, 2);
                data["heap_size_mb"] = Math.Round(heapSizeMB, 2);
                data["fragmented_mb"] = Math.Round(fragmentedMB, 2);
                data["memory_load"] = gcMemoryInfo.MemoryLoadBytes;
                
                // Calculate memory pressure percentage
                if (totalAvailableMemoryMB > 0)
                {
                    var memoryPressure = (workingSetMB / totalAvailableMemoryMB) * 100;
                    data["memory_pressure_percent"] = Math.Round(memoryPressure, 2);
                }
            }
            catch (Exception ex)
            {
                // System memory info not available on all platforms
                _logger.LogDebug(ex, "System memory info not available");
            }

            // Check for critical memory usage
            if (workingSetMB > criticalThresholdMB)
            {
                _logger.LogError(
                    "Critical memory usage: {WorkingSet:F2} MB (threshold: {Threshold} MB)",
                    workingSetMB, criticalThresholdMB);
                    
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Critical memory usage: {workingSetMB:F2} MB exceeds {criticalThresholdMB} MB threshold",
                    data: data));
            }

            // Check for warning-level memory usage
            if (workingSetMB > warningThresholdMB)
            {
                _logger.LogWarning(
                    "Elevated memory usage: {WorkingSet:F2} MB (threshold: {Threshold} MB)",
                    workingSetMB, warningThresholdMB);
                    
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Elevated memory usage: {workingSetMB:F2} MB exceeds {warningThresholdMB} MB threshold",
                    data: data));
            }

            // Check for excessive Gen2 collections (potential memory leak indicator)
            if (gen2Collections > 100)
            {
                _logger.LogWarning(
                    "High Gen2 collection count: {Gen2Collections} (may indicate memory pressure)",
                    gen2Collections);
                    
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High Gen2 GC collections: {gen2Collections}",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Memory usage normal: {workingSetMB:F2} MB",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Memory health check failed",
                exception: ex));
        }
    }
}
