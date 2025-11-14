using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Analytics;

/// <summary>
/// Service for tracking and analyzing local usage statistics
/// All data stays local - privacy-first design with no external telemetry
/// </summary>
public interface IUsageAnalyticsService
{
    /// <summary>
    /// Record a usage event
    /// </summary>
    Task RecordUsageAsync(UsageStatisticsEntity usage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record a cost event
    /// </summary>
    Task RecordCostAsync(CostTrackingEntity cost, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Record performance metrics
    /// </summary>
    Task RecordPerformanceAsync(PerformanceMetricsEntity metrics, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get usage statistics for a date range
    /// </summary>
    Task<UsageStatistics> GetUsageStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? provider = null,
        string? generationType = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get cost statistics for a date range
    /// </summary>
    Task<CostStatistics> GetCostStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? provider = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get performance statistics for a date range
    /// </summary>
    Task<PerformanceStatistics> GetPerformanceStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? operationType = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Estimate cost for a planned operation
    /// </summary>
    Task<decimal> EstimateCostAsync(
        string provider, 
        string model, 
        long inputTokens, 
        long outputTokens,
        CancellationToken cancellationToken = default);
}

public class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly AuraDbContext _context;
    private readonly ILogger<UsageAnalyticsService> _logger;
    private readonly LlmPricingConfiguration _pricing;

    public UsageAnalyticsService(
        AuraDbContext context,
        ILogger<UsageAnalyticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Load pricing configuration
        _pricing = LlmPricingConfiguration.LoadDefault(_logger);
    }

    public async Task RecordUsageAsync(UsageStatisticsEntity usage, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if analytics is enabled
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings?.IsEnabled == false)
            {
                _logger.LogDebug("Analytics disabled, skipping usage recording");
                return;
            }

            // Only track successful operations if configured
            if (settings?.TrackSuccessOnly == true && !usage.Success)
            {
                _logger.LogDebug("Skipping failed operation (TrackSuccessOnly enabled)");
                return;
            }

            _context.UsageStatistics.Add(usage);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Recorded usage: {Provider}/{Model} - {Type} - Success: {Success}",
                usage.Provider, usage.Model, usage.GenerationType, usage.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage statistics");
        }
    }

    public async Task RecordCostAsync(CostTrackingEntity cost, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings?.IsEnabled == false)
            {
                return;
            }

            _context.CostTracking.Add(cost);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Recorded cost: {Provider}/{Model} - ${Cost:F4}",
                cost.Provider, cost.Model, cost.TotalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record cost tracking");
        }
    }

    public async Task RecordPerformanceAsync(PerformanceMetricsEntity metrics, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings?.IsEnabled == false || settings?.CollectHardwareMetrics == false)
            {
                return;
            }

            _context.PerformanceMetrics.Add(metrics);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug(
                "Recorded performance: {Operation} - {Duration}ms",
                metrics.OperationType, metrics.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance metrics");
        }
    }

    public async Task<UsageStatistics> GetUsageStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? provider = null,
        string? generationType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.UsageStatistics
                .Where(u => u.Timestamp >= startDate && u.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(provider))
            {
                query = query.Where(u => u.Provider == provider);
            }

            if (!string.IsNullOrEmpty(generationType))
            {
                query = query.Where(u => u.GenerationType == generationType);
            }

            var data = await query.ToListAsync(cancellationToken);

            var successfulOps = data.Where(u => u.Success).ToList();
            var failedOps = data.Where(u => !u.Success).ToList();

            var providerBreakdown = data
                .GroupBy(u => u.Provider)
                .ToDictionary(g => g.Key, g => new ProviderUsageStats
                {
                    TotalOperations = g.Count(),
                    SuccessfulOperations = g.Count(u => u.Success),
                    TotalInputTokens = g.Sum(u => u.InputTokens),
                    TotalOutputTokens = g.Sum(u => u.OutputTokens),
                    AverageDurationMs = g.Any() ? (long)g.Average(u => u.DurationMs) : 0
                });

            var featureBreakdown = data
                .Where(u => u.FeatureUsed != null)
                .GroupBy(u => u.FeatureUsed!)
                .ToDictionary(g => g.Key, g => g.Count());

            return new UsageStatistics
            {
                TotalOperations = data.Count,
                SuccessfulOperations = successfulOps.Count,
                FailedOperations = failedOps.Count,
                SuccessRate = data.Count > 0 ? (double)successfulOps.Count / data.Count * 100 : 0,
                TotalInputTokens = data.Sum(u => u.InputTokens),
                TotalOutputTokens = data.Sum(u => u.OutputTokens),
                TotalTokens = data.Sum(u => u.InputTokens + u.OutputTokens),
                AverageDurationMs = data.Count != 0 ? (long)data.Average(u => u.DurationMs) : 0,
                TotalDurationMs = data.Sum(u => u.DurationMs),
                TotalVideoDurationSeconds = data.Where(u => u.OutputDurationSeconds.HasValue)
                    .Sum(u => u.OutputDurationSeconds!.Value),
                TotalScenes = data.Where(u => u.SceneCount.HasValue).Sum(u => u.SceneCount!.Value),
                ProviderBreakdown = providerBreakdown,
                FeatureBreakdown = featureBreakdown,
                RetryRate = data.Count > 0 ? (double)data.Count(u => u.IsRetry) / data.Count * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics");
            return new UsageStatistics();
        }
    }

    public async Task<CostStatistics> GetCostStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? provider = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CostTracking
                .Where(c => c.Timestamp >= startDate && c.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(provider))
            {
                query = query.Where(c => c.Provider == provider);
            }

            var data = await query.ToListAsync(cancellationToken);

            var providerCosts = data
                .GroupBy(c => c.Provider)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            var monthlyCosts = data
                .GroupBy(c => c.YearMonth)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            var modelCosts = data
                .GroupBy(c => c.Model)
                .OrderByDescending(g => g.Sum(c => c.TotalCost))
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            return new CostStatistics
            {
                TotalCost = data.Sum(c => c.TotalCost),
                TotalInputCost = data.Sum(c => c.InputCost),
                TotalOutputCost = data.Sum(c => c.OutputCost),
                AverageCostPerOperation = data.Count != 0 ? data.Average(c => c.TotalCost) : 0,
                TotalInputTokens = data.Sum(c => c.InputTokens),
                TotalOutputTokens = data.Sum(c => c.OutputTokens),
                CostPerProvider = providerCosts,
                CostPerMonth = monthlyCosts,
                TopModels = modelCosts,
                Currency = data.FirstOrDefault()?.Currency ?? "USD"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost statistics");
            return new CostStatistics();
        }
    }

    public async Task<PerformanceStatistics> GetPerformanceStatisticsAsync(
        DateTime startDate, 
        DateTime endDate, 
        string? operationType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PerformanceMetrics
                .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(operationType))
            {
                query = query.Where(p => p.OperationType == operationType);
            }

            var data = await query.ToListAsync(cancellationToken);

            var operationBreakdown = data
                .GroupBy(p => p.OperationType)
                .ToDictionary(g => g.Key, g => new OperationPerformance
                {
                    Count = g.Count(),
                    AverageDurationMs = g.Any() ? (long)g.Average(p => p.DurationMs) : 0,
                    MinDurationMs = g.Any() ? g.Min(p => p.DurationMs) : 0,
                    MaxDurationMs = g.Any() ? g.Max(p => p.DurationMs) : 0,
                    SuccessRate = g.Count() > 0 ? (double)g.Count(p => p.Success) / g.Count() * 100 : 0
                });

            return new PerformanceStatistics
            {
                TotalOperations = data.Count,
                SuccessfulOperations = data.Count(p => p.Success),
                AverageDurationMs = data.Count != 0 ? (long)data.Average(p => p.DurationMs) : 0,
                MedianDurationMs = CalculateMedian(data.Select(p => p.DurationMs).ToList()),
                MinDurationMs = data.Count != 0 ? data.Min(p => p.DurationMs) : 0,
                MaxDurationMs = data.Count != 0 ? data.Max(p => p.DurationMs) : 0,
                AverageCpuUsage = data.Where(p => p.CpuUsagePercent.HasValue).Any()
                    ? data.Where(p => p.CpuUsagePercent.HasValue).Average(p => p.CpuUsagePercent!.Value)
                    : null,
                AverageMemoryUsageMB = data.Where(p => p.MemoryUsedMB.HasValue).Any()
                    ? data.Where(p => p.MemoryUsedMB.HasValue).Average(p => p.MemoryUsedMB!.Value)
                    : null,
                PeakMemoryUsageMB = data.Where(p => p.PeakMemoryMB.HasValue).Any()
                    ? data.Where(p => p.PeakMemoryMB.HasValue).Max(p => p.PeakMemoryMB!.Value)
                    : null,
                OperationBreakdown = operationBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance statistics");
            return new PerformanceStatistics();
        }
    }

    public async Task<decimal> EstimateCostAsync(
        string provider, 
        string model, 
        long inputTokens, 
        long outputTokens,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get pricing from configuration
            var modelPricing = _pricing.GetModelPricing(model);
            
            if (modelPricing == null)
            {
                // Fallback to configured pricing
                modelPricing = _pricing.FallbackModel;
                _logger.LogWarning("No pricing found for model {Model}, using fallback", model);
            }

            // Calculate cost per 1M tokens
            var inputCost = (inputTokens / 1_000_000m) * modelPricing.InputPrice;
            var outputCost = (outputTokens / 1_000_000m) * modelPricing.OutputPrice;
            
            return inputCost + outputCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate cost");
            return 0;
        }
    }

    private static long CalculateMedian(List<long> values)
    {
        if (values.Count == 0) return 0;
        
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
}

// DTOs for statistics results
public class UsageStatistics
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double SuccessRate { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public long AverageDurationMs { get; set; }
    public long TotalDurationMs { get; set; }
    public double TotalVideoDurationSeconds { get; set; }
    public int TotalScenes { get; set; }
    public Dictionary<string, ProviderUsageStats> ProviderBreakdown { get; set; } = new();
    public Dictionary<string, int> FeatureBreakdown { get; set; } = new();
    public double RetryRate { get; set; }
}

public class ProviderUsageStats
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long AverageDurationMs { get; set; }
}

public class CostStatistics
{
    public decimal TotalCost { get; set; }
    public decimal TotalInputCost { get; set; }
    public decimal TotalOutputCost { get; set; }
    public decimal AverageCostPerOperation { get; set; }
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public Dictionary<string, decimal> CostPerProvider { get; set; } = new();
    public Dictionary<string, decimal> CostPerMonth { get; set; } = new();
    public Dictionary<string, decimal> TopModels { get; set; } = new();
    public string Currency { get; set; } = "USD";
}

public class PerformanceStatistics
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public long AverageDurationMs { get; set; }
    public long MedianDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public double? AverageCpuUsage { get; set; }
    public double? AverageMemoryUsageMB { get; set; }
    public double? PeakMemoryUsageMB { get; set; }
    public Dictionary<string, OperationPerformance> OperationBreakdown { get; set; } = new();
}

public class OperationPerformance
{
    public int Count { get; set; }
    public long AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public double SuccessRate { get; set; }
}
