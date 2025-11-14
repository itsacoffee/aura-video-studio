using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that performs periodic cache maintenance (eviction of expired entries)
/// </summary>
public class LlmCacheMaintenanceService : BackgroundService
{
    private readonly ILogger<LlmCacheMaintenanceService> _logger;
    private readonly ILlmCache _cache;
    private readonly TimeSpan _maintenanceInterval;
    
    public LlmCacheMaintenanceService(
        ILogger<LlmCacheMaintenanceService> logger,
        ILlmCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _maintenanceInterval = TimeSpan.FromMinutes(5);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "LLM Cache Maintenance Service started (interval: {Interval})",
            _maintenanceInterval);
        
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken).ConfigureAwait(false);
                
                await Task.Delay(_maintenanceInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LLM Cache Maintenance Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache maintenance");
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }
        
        _logger.LogInformation("LLM Cache Maintenance Service stopped");
    }
    
    private async Task PerformMaintenanceAsync(CancellationToken ct)
    {
        try
        {
            await _cache.EvictExpiredAsync(ct).ConfigureAwait(false);
            
            var stats = await _cache.GetStatisticsAsync(ct).ConfigureAwait(false);
            
            _logger.LogDebug(
                "Cache maintenance completed: {Entries} entries, {Hits} hits, {Misses} misses, {HitRate:P2} hit rate",
                stats.TotalEntries,
                stats.TotalHits,
                stats.TotalMisses,
                stats.HitRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache maintenance");
            throw;
        }
    }
}
