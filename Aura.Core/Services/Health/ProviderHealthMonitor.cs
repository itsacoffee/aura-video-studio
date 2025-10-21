using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Health;

/// <summary>
/// Monitors provider health and tracks performance metrics
/// </summary>
public class ProviderHealthMonitor
{
    private readonly ILogger<ProviderHealthMonitor> _logger;
    private readonly ConcurrentDictionary<string, ProviderHealthState> _healthStates = new();
    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>> _healthCheckFunctions = new();

    public ProviderHealthMonitor(ILogger<ProviderHealthMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a health check function for a provider
    /// </summary>
    public void RegisterHealthCheck(string providerName, Func<CancellationToken, Task<bool>> healthCheckFunc)
    {
        _healthCheckFunctions[providerName] = healthCheckFunc;
        _healthStates.TryAdd(providerName, new ProviderHealthState(providerName));
        _logger.LogDebug("Registered health check for provider: {ProviderName}", providerName);
    }

    /// <summary>
    /// Check the health of a specific provider
    /// </summary>
    public async Task<ProviderHealthMetrics> CheckProviderHealthAsync(
        string providerName,
        Func<CancellationToken, Task<bool>> healthCheckFunc,
        CancellationToken ct = default)
    {
        var state = _healthStates.GetOrAdd(providerName, _ => new ProviderHealthState(providerName));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var result = await healthCheckFunc(linkedCts.Token).ConfigureAwait(false);
            stopwatch.Stop();

            if (result)
            {
                state.RecordSuccess(stopwatch.Elapsed);
                _logger.LogDebug("Health check passed for {ProviderName} in {ElapsedMs}ms",
                    providerName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                state.RecordFailure("Health check returned false");
                _logger.LogWarning("Health check failed for {ProviderName}", providerName);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            state.RecordFailure("Health check timed out after 5 seconds");
            _logger.LogWarning("Health check timed out for {ProviderName}", providerName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = $"{ex.GetType().Name}: {ex.Message}";
            state.RecordFailure(errorMessage);
            _logger.LogWarning(ex, "Health check error for {ProviderName}", providerName);
        }

        return state.ToMetrics();
    }

    /// <summary>
    /// Get cached health metrics for a provider
    /// </summary>
    public ProviderHealthMetrics? GetProviderHealth(string providerName)
    {
        if (_healthStates.TryGetValue(providerName, out var state))
        {
            return state.ToMetrics();
        }

        return null;
    }

    /// <summary>
    /// Get health metrics for all registered providers
    /// </summary>
    public Dictionary<string, ProviderHealthMetrics> GetAllProviderHealth()
    {
        return _healthStates.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToMetrics()
        );
    }

    /// <summary>
    /// Run periodic health checks in the background
    /// </summary>
    public async Task RunPeriodicHealthChecksAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting periodic health checks (interval: 5 minutes)");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct).ConfigureAwait(false);

                _logger.LogDebug("Running scheduled health checks for {Count} providers", _healthCheckFunctions.Count);

                foreach (var (providerName, healthCheckFunc) in _healthCheckFunctions)
                {
                    if (ct.IsCancellationRequested) break;

                    try
                    {
                        var metrics = await CheckProviderHealthAsync(providerName, healthCheckFunc, ct).ConfigureAwait(false);

                        if (metrics.IsHealthy)
                        {
                            _logger.LogInformation(
                                "✓ {ProviderName} healthy - {ResponseTimeMs}ms, {SuccessRate:P0} success rate",
                                providerName, metrics.ResponseTime.TotalMilliseconds, metrics.SuccessRate);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "✗ {ProviderName} unhealthy - {ConsecutiveFailures} consecutive failures, last error: {LastError}",
                                providerName, metrics.ConsecutiveFailures, metrics.LastError);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error during health check for {ProviderName}", providerName);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Health check loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check loop");
                // Continue running despite errors
            }
        }

        _logger.LogInformation("Periodic health checks stopped");
    }
}
