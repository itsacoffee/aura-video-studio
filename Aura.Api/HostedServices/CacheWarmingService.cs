using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Data;
using Aura.Core.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service to warm up cache on application startup
/// </summary>
public class CacheWarmingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly CachingConfiguration _cachingConfig;

    public CacheWarmingService(
        IServiceProvider serviceProvider,
        ILogger<CacheWarmingService> logger,
        CachingConfiguration cachingConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cachingConfig = cachingConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_cachingConfig.Enabled || !_cachingConfig.EnableCacheWarming)
        {
            _logger.LogInformation("Cache warming is disabled");
            return;
        }

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            _logger.LogInformation("Starting cache warming...");
            var startTime = DateTime.UtcNow;

            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetService<IDistributedCacheService>();
            var dbContext = scope.ServiceProvider.GetService<AuraDbContext>();

            if (cacheService == null || dbContext == null)
            {
                _logger.LogWarning("Cache service or database context not available for cache warming");
                return;
            }

            // Warm up frequently accessed data
            var tasks = new List<Task>
            {
                WarmTemplatesCache(cacheService, dbContext, stoppingToken),
                WarmSystemConfigCache(cacheService, dbContext, stoppingToken),
                WarmUserPreferencesCache(cacheService, dbContext, stoppingToken)
            };

            await Task.WhenAll(tasks);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Cache warming completed in {DurationMs}ms", duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache warming");
        }
    }

    private async Task WarmTemplatesCache(
        IDistributedCacheService cacheService,
        AuraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var templates = await dbContext.Templates
                .AsNoTracking()
                .Where(t => t.IsSystemTemplate || t.IsCommunityTemplate)
                .Take(50)
                .ToListAsync(cancellationToken);

            if (templates.Any())
            {
                await cacheService.SetAsync(
                    "templates:popular",
                    templates,
                    TimeSpan.FromHours(1),
                    cancellationToken);

                _logger.LogInformation("Warmed template cache with {Count} entries", templates.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm templates cache");
        }
    }

    private async Task WarmSystemConfigCache(
        IDistributedCacheService cacheService,
        AuraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var systemConfig = await dbContext.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (systemConfig != null)
            {
                await cacheService.SetAsync(
                    "system:config",
                    systemConfig,
                    TimeSpan.FromMinutes(30),
                    cancellationToken);

                _logger.LogInformation("Warmed system configuration cache");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm system config cache");
        }
    }

    private async Task WarmUserPreferencesCache(
        IDistributedCacheService cacheService,
        AuraDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var configurations = await dbContext.Configurations
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsSensitive)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (configurations.Any())
            {
                foreach (var config in configurations.Take(20)) // Limit to prevent overwhelming cache
                {
                    await cacheService.SetAsync(
                        $"config:{config.Key}",
                        config,
                        TimeSpan.FromMinutes(15),
                        cancellationToken);
                }

                _logger.LogInformation("Warmed configuration cache with {Count} entries", 
                    Math.Min(20, configurations.Count));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm user preferences cache");
        }
    }
}
