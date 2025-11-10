using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Analytics;

/// <summary>
/// Service for managing analytics data retention and cleanup
/// Implements configurable data retention policies
/// </summary>
public interface IAnalyticsCleanupService
{
    /// <summary>
    /// Run cleanup based on retention settings
    /// </summary>
    Task CleanupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Aggregate old data into summaries
    /// </summary>
    Task AggregateOldDataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current database size for analytics
    /// </summary>
    Task<long> GetDatabaseSizeBytesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clear all analytics data (user-initiated)
    /// </summary>
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
}

public class AnalyticsCleanupService : IAnalyticsCleanupService
{
    private readonly AuraDbContext _context;
    private readonly ILogger<AnalyticsCleanupService> _logger;

    public AnalyticsCleanupService(
        AuraDbContext context,
        ILogger<AnalyticsCleanupService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings?.AutoCleanupEnabled != true)
            {
                _logger.LogDebug("Auto cleanup disabled");
                return;
            }

            _logger.LogInformation("Starting analytics cleanup");

            var now = DateTime.UtcNow;
            var cleaned = 0;

            // Cleanup usage statistics
            if (settings.UsageStatisticsRetentionDays > 0)
            {
                var cutoffDate = now.AddDays(-settings.UsageStatisticsRetentionDays);
                var toDelete = await _context.UsageStatistics
                    .Where(u => u.Timestamp < cutoffDate)
                    .ToListAsync(cancellationToken);
                
                _context.UsageStatistics.RemoveRange(toDelete);
                cleaned += toDelete.Count;
                _logger.LogInformation("Removed {Count} old usage statistics records", toDelete.Count);
            }

            // Cleanup cost tracking
            if (settings.CostTrackingRetentionDays > 0)
            {
                var cutoffDate = now.AddDays(-settings.CostTrackingRetentionDays);
                var toDelete = await _context.CostTracking
                    .Where(c => c.Timestamp < cutoffDate)
                    .ToListAsync(cancellationToken);
                
                _context.CostTracking.RemoveRange(toDelete);
                cleaned += toDelete.Count;
                _logger.LogInformation("Removed {Count} old cost tracking records", toDelete.Count);
            }

            // Cleanup performance metrics
            if (settings.PerformanceMetricsRetentionDays > 0)
            {
                var cutoffDate = now.AddDays(-settings.PerformanceMetricsRetentionDays);
                var toDelete = await _context.PerformanceMetrics
                    .Where(p => p.Timestamp < cutoffDate)
                    .ToListAsync(cancellationToken);
                
                _context.PerformanceMetrics.RemoveRange(toDelete);
                cleaned += toDelete.Count;
                _logger.LogInformation("Removed {Count} old performance metrics records", toDelete.Count);
            }

            // Check database size limit
            if (settings.MaxDatabaseSizeMB > 0)
            {
                var currentSize = await GetDatabaseSizeBytesAsync(cancellationToken);
                var currentSizeMB = currentSize / (1024.0 * 1024.0);
                
                if (currentSizeMB > settings.MaxDatabaseSizeMB)
                {
                    _logger.LogWarning(
                        "Analytics database size ({CurrentMB:F2} MB) exceeds limit ({LimitMB} MB), performing aggressive cleanup",
                        currentSizeMB, settings.MaxDatabaseSizeMB);
                    
                    await AggressiveCleanupAsync(settings.MaxDatabaseSizeMB, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Analytics cleanup completed, removed {Count} records", cleaned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup analytics data");
        }
    }

    public async Task AggregateOldDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings?.AggregateOldData != true)
            {
                return;
            }

            _logger.LogInformation("Starting data aggregation");

            var cutoffDate = DateTime.UtcNow.AddDays(-settings.AggregationThresholdDays);

            // Aggregate by day
            await AggregateDailyDataAsync(cutoffDate, cancellationToken);
            
            // Aggregate by month (for data older than 3 months)
            var monthCutoff = DateTime.UtcNow.AddDays(-90);
            await AggregateMonthlyDataAsync(monthCutoff, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Data aggregation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to aggregate old data");
        }
    }

    public async Task<long> GetDatabaseSizeBytesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Approximate size calculation based on record counts
            var usageCount = await _context.UsageStatistics.CountAsync(cancellationToken);
            var costCount = await _context.CostTracking.CountAsync(cancellationToken);
            var perfCount = await _context.PerformanceMetrics.CountAsync(cancellationToken);
            var summaryCount = await _context.AnalyticsSummaries.CountAsync(cancellationToken);

            // Rough estimates: usage ~500 bytes, cost ~300 bytes, perf ~800 bytes, summary ~1KB
            var estimatedSize = (usageCount * 500L) + 
                                (costCount * 300L) + 
                                (perfCount * 800L) + 
                                (summaryCount * 1024L);

            return estimatedSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate database size");
            return 0;
        }
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Clearing all analytics data");

            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM UsageStatistics", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM CostTracking", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM PerformanceMetrics", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM AnalyticsSummaries", cancellationToken);

            _logger.LogInformation("All analytics data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear analytics data");
            throw;
        }
    }

    private async Task AggregateDailyDataAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        // Get all dates that need aggregation
        var dates = await _context.UsageStatistics
            .Where(u => u.Timestamp < cutoffDate)
            .Select(u => u.Timestamp.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var date in dates)
        {
            var periodId = date.ToString("yyyy-MM-dd");
            
            // Check if summary already exists
            var existing = await _context.AnalyticsSummaries
                .FirstOrDefaultAsync(s => s.PeriodType == "daily" && s.PeriodId == periodId, cancellationToken);

            if (existing != null)
            {
                continue;
            }

            var endDate = date.AddDays(1);
            
            var usageData = await _context.UsageStatistics
                .Where(u => u.Timestamp >= date && u.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            var costData = await _context.CostTracking
                .Where(c => c.Timestamp >= date && c.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            var perfData = await _context.PerformanceMetrics
                .Where(p => p.Timestamp >= date && p.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            // Create summary
            var summary = new AnalyticsSummaryEntity
            {
                PeriodType = "daily",
                PeriodId = periodId,
                PeriodStart = date,
                PeriodEnd = endDate.AddSeconds(-1),
                TotalGenerations = usageData.Count,
                SuccessfulGenerations = usageData.Count(u => u.Success),
                FailedGenerations = usageData.Count(u => !u.Success),
                TotalTokens = usageData.Sum(u => u.InputTokens + u.OutputTokens),
                TotalInputTokens = usageData.Sum(u => u.InputTokens),
                TotalOutputTokens = usageData.Sum(u => u.OutputTokens),
                TotalCostUSD = costData.Sum(c => c.TotalCost),
                AverageDurationMs = usageData.Any() ? (long)usageData.Average(u => u.DurationMs) : 0,
                TotalRenderingTimeMs = perfData.Sum(p => p.DurationMs),
                MostUsedProvider = usageData.GroupBy(u => u.Provider)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault(),
                MostUsedModel = usageData.Where(u => u.Model != null)
                    .GroupBy(u => u.Model!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault(),
                MostUsedFeature = usageData.Where(u => u.FeatureUsed != null)
                    .GroupBy(u => u.FeatureUsed!)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault(),
                TotalVideoDurationSeconds = usageData.Where(u => u.OutputDurationSeconds.HasValue)
                    .Sum(u => u.OutputDurationSeconds!.Value),
                TotalScenes = usageData.Where(u => u.SceneCount.HasValue)
                    .Sum(u => u.SceneCount!.Value),
                AverageCpuUsage = perfData.Where(p => p.CpuUsagePercent.HasValue).Any()
                    ? perfData.Where(p => p.CpuUsagePercent.HasValue).Average(p => p.CpuUsagePercent!.Value)
                    : null,
                AverageMemoryUsageMB = perfData.Where(p => p.MemoryUsedMB.HasValue).Any()
                    ? perfData.Where(p => p.MemoryUsedMB.HasValue).Average(p => p.MemoryUsedMB!.Value)
                    : null,
                ProviderBreakdown = JsonSerializer.Serialize(
                    usageData.GroupBy(u => u.Provider)
                        .ToDictionary(g => g.Key, g => new { count = g.Count(), cost = costData.Where(c => c.Provider == g.Key).Sum(c => c.TotalCost) })),
                FeatureBreakdown = JsonSerializer.Serialize(
                    usageData.Where(u => u.FeatureUsed != null)
                        .GroupBy(u => u.FeatureUsed!)
                        .ToDictionary(g => g.Key, g => g.Count()))
            };

            _context.AnalyticsSummaries.Add(summary);
            
            _logger.LogDebug("Created daily summary for {Date}", periodId);
        }
    }

    private async Task AggregateMonthlyDataAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        // Similar to daily aggregation but for months
        var months = await _context.UsageStatistics
            .Where(u => u.Timestamp < cutoffDate)
            .Select(u => new { u.Timestamp.Year, u.Timestamp.Month })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var month in months)
        {
            var periodId = $"{month.Year:D4}-{month.Month:D2}";
            
            var existing = await _context.AnalyticsSummaries
                .FirstOrDefaultAsync(s => s.PeriodType == "monthly" && s.PeriodId == periodId, cancellationToken);

            if (existing != null)
            {
                continue;
            }

            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1);

            var usageData = await _context.UsageStatistics
                .Where(u => u.Timestamp >= startDate && u.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            var costData = await _context.CostTracking
                .Where(c => c.Timestamp >= startDate && c.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            var perfData = await _context.PerformanceMetrics
                .Where(p => p.Timestamp >= startDate && p.Timestamp < endDate)
                .ToListAsync(cancellationToken);

            var summary = new AnalyticsSummaryEntity
            {
                PeriodType = "monthly",
                PeriodId = periodId,
                PeriodStart = startDate,
                PeriodEnd = endDate.AddSeconds(-1),
                TotalGenerations = usageData.Count,
                SuccessfulGenerations = usageData.Count(u => u.Success),
                FailedGenerations = usageData.Count(u => !u.Success),
                TotalTokens = usageData.Sum(u => u.InputTokens + u.OutputTokens),
                TotalInputTokens = usageData.Sum(u => u.InputTokens),
                TotalOutputTokens = usageData.Sum(u => u.OutputTokens),
                TotalCostUSD = costData.Sum(c => c.TotalCost),
                AverageDurationMs = usageData.Any() ? (long)usageData.Average(u => u.DurationMs) : 0,
                TotalRenderingTimeMs = perfData.Sum(p => p.DurationMs),
                MostUsedProvider = usageData.GroupBy(u => u.Provider)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault(),
                TotalVideoDurationSeconds = usageData.Where(u => u.OutputDurationSeconds.HasValue)
                    .Sum(u => u.OutputDurationSeconds!.Value),
                TotalScenes = usageData.Where(u => u.SceneCount.HasValue)
                    .Sum(u => u.SceneCount!.Value),
                ProviderBreakdown = JsonSerializer.Serialize(
                    usageData.GroupBy(u => u.Provider)
                        .ToDictionary(g => g.Key, g => new { count = g.Count(), cost = costData.Where(c => c.Provider == g.Key).Sum(c => c.TotalCost) })),
                FeatureBreakdown = JsonSerializer.Serialize(
                    usageData.Where(u => u.FeatureUsed != null)
                        .GroupBy(u => u.FeatureUsed!)
                        .ToDictionary(g => g.Key, g => g.Count()))
            };

            _context.AnalyticsSummaries.Add(summary);
            _logger.LogDebug("Created monthly summary for {PeriodId}", periodId);
        }
    }

    private async Task AggressiveCleanupAsync(int targetSizeMB, CancellationToken cancellationToken)
    {
        // Remove oldest 25% of data until under limit
        var quarterDate = DateTime.UtcNow.AddDays(-90);

        var oldUsage = await _context.UsageStatistics
            .Where(u => u.Timestamp < quarterDate)
            .OrderBy(u => u.Timestamp)
            .Take(1000)
            .ToListAsync(cancellationToken);

        _context.UsageStatistics.RemoveRange(oldUsage);

        var oldPerf = await _context.PerformanceMetrics
            .Where(p => p.Timestamp < quarterDate)
            .OrderBy(p => p.Timestamp)
            .Take(1000)
            .ToListAsync(cancellationToken);

        _context.PerformanceMetrics.RemoveRange(oldPerf);

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogWarning("Performed aggressive cleanup, removed {UsageCount} usage and {PerfCount} performance records",
            oldUsage.Count, oldPerf.Count);
    }
}
