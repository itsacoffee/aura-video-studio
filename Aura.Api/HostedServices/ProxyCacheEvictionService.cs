using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service for automatic proxy cache eviction using LRU strategy
/// </summary>
public class ProxyCacheEvictionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProxyCacheEvictionService> _logger;
    private readonly TimeSpan _evictionInterval = TimeSpan.FromMinutes(15);

    public ProxyCacheEvictionService(
        IServiceProvider serviceProvider,
        ILogger<ProxyCacheEvictionService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Proxy cache eviction service starting. Check interval: {Interval}", _evictionInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_evictionInterval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await PerformEvictionCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Proxy cache eviction service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during proxy cache eviction check");
            }
        }

        _logger.LogInformation("Proxy cache eviction service stopped");
    }

    private async Task PerformEvictionCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var proxyMediaService = scope.ServiceProvider.GetService<IProxyMediaService>();

        if (proxyMediaService == null)
        {
            _logger.LogWarning("ProxyMediaService not available for eviction check");
            return;
        }

        try
        {
            var stats = await proxyMediaService.GetCacheStatisticsAsync();

            _logger.LogDebug("Cache stats: {Proxies} proxies, {Size:N0} bytes ({Usage:F1}% of limit)",
                stats.TotalProxies, stats.TotalCacheSizeBytes, stats.CacheUsagePercent);

            if (stats.IsOverLimit)
            {
                _logger.LogInformation("Cache exceeds limit. Triggering LRU eviction.");
                await proxyMediaService.EvictLeastRecentlyUsedAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing cache eviction");
        }
    }
}
