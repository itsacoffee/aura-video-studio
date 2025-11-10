using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Services.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for local analytics and usage insights
/// All data stays local - privacy-first design
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly AuraDbContext _context;
    private readonly IUsageAnalyticsService _analyticsService;
    private readonly IAnalyticsCleanupService _cleanupService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        AuraDbContext context,
        IUsageAnalyticsService analyticsService,
        IAnalyticsCleanupService cleanupService,
        ILogger<AnalyticsController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get usage statistics for a date range
    /// </summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(UsageStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageStatistics>> GetUsageStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? provider = null,
        [FromQuery] string? generationType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _analyticsService.GetUsageStatisticsAsync(
                start, end, provider, generationType, cancellationToken);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics");
            return StatusCode(500, new { error = "Failed to retrieve usage statistics" });
        }
    }

    /// <summary>
    /// Get cost statistics for a date range
    /// </summary>
    [HttpGet("costs")]
    [ProducesResponseType(typeof(CostStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<CostStatistics>> GetCostStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? provider = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _analyticsService.GetCostStatisticsAsync(
                start, end, provider, cancellationToken);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost statistics");
            return StatusCode(500, new { error = "Failed to retrieve cost statistics" });
        }
    }

    /// <summary>
    /// Get performance statistics for a date range
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceStatistics), StatusCodes.Status200OK)]
    public async Task<ActionResult<PerformanceStatistics>> GetPerformanceStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? operationType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _analyticsService.GetPerformanceStatisticsAsync(
                start, end, operationType, cancellationToken);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance statistics");
            return StatusCode(500, new { error = "Failed to retrieve performance statistics" });
        }
    }

    /// <summary>
    /// Get aggregated summaries for a period
    /// </summary>
    [HttpGet("summaries")]
    [ProducesResponseType(typeof(List<AnalyticsSummaryEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnalyticsSummaryEntity>>> GetSummaries(
        [FromQuery, Required] string periodType = "daily",
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summaries = await _context.AnalyticsSummaries
                .Where(s => s.PeriodType == periodType)
                .OrderByDescending(s => s.PeriodStart)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics summaries");
            return StatusCode(500, new { error = "Failed to retrieve summaries" });
        }
    }

    /// <summary>
    /// Get current month's cost summary
    /// </summary>
    [HttpGet("costs/current-month")]
    [ProducesResponseType(typeof(MonthlyBudgetStatus), StatusCodes.Status200OK)]
    public async Task<ActionResult<MonthlyBudgetStatus>> GetCurrentMonthBudget(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var costs = await _context.CostTracking
                .Where(c => c.Timestamp >= monthStart && c.Timestamp < monthEnd)
                .ToListAsync(cancellationToken);

            var totalCost = costs.Sum(c => c.TotalCost);
            var providerCosts = costs
                .GroupBy(c => c.Provider)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.TotalCost));

            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);

            return Ok(new MonthlyBudgetStatus
            {
                YearMonth = now.ToString("yyyy-MM"),
                TotalCost = totalCost,
                ProviderCosts = providerCosts,
                Currency = costs.FirstOrDefault()?.Currency ?? "USD",
                DaysInMonth = DateTime.DaysInMonth(now.Year, now.Month),
                DaysElapsed = now.Day,
                ProjectedMonthlyTotal = totalCost / now.Day * DateTime.DaysInMonth(now.Year, now.Month),
                AnalyticsEnabled = settings?.IsEnabled ?? true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current month budget");
            return StatusCode(500, new { error = "Failed to retrieve budget status" });
        }
    }

    /// <summary>
    /// Estimate cost for a planned operation
    /// </summary>
    [HttpPost("costs/estimate")]
    [ProducesResponseType(typeof(CostEstimate), StatusCodes.Status200OK)]
    public async Task<ActionResult<CostEstimate>> EstimateCost(
        [FromBody] CostEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cost = await _analyticsService.EstimateCostAsync(
                request.Provider,
                request.Model,
                request.InputTokens,
                request.OutputTokens,
                cancellationToken);

            return Ok(new CostEstimate
            {
                Provider = request.Provider,
                Model = request.Model,
                InputTokens = request.InputTokens,
                OutputTokens = request.OutputTokens,
                EstimatedCost = cost,
                Currency = "USD"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate cost");
            return StatusCode(500, new { error = "Failed to estimate cost" });
        }
    }

    /// <summary>
    /// Get analytics retention settings
    /// </summary>
    [HttpGet("settings")]
    [ProducesResponseType(typeof(AnalyticsRetentionSettingsEntity), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsRetentionSettingsEntity>> GetSettings(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            
            if (settings == null)
            {
                return NotFound(new { error = "Settings not found" });
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analytics settings");
            return StatusCode(500, new { error = "Failed to retrieve settings" });
        }
    }

    /// <summary>
    /// Update analytics retention settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(AnalyticsRetentionSettingsEntity), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsRetentionSettingsEntity>> UpdateSettings(
        [FromBody] AnalyticsRetentionSettingsEntity settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);
            
            if (existing == null)
            {
                return NotFound(new { error = "Settings not found" });
            }

            // Update settings
            existing.IsEnabled = settings.IsEnabled;
            existing.UsageStatisticsRetentionDays = settings.UsageStatisticsRetentionDays;
            existing.CostTrackingRetentionDays = settings.CostTrackingRetentionDays;
            existing.PerformanceMetricsRetentionDays = settings.PerformanceMetricsRetentionDays;
            existing.AutoCleanupEnabled = settings.AutoCleanupEnabled;
            existing.CleanupHourUtc = settings.CleanupHourUtc;
            existing.TrackSuccessOnly = settings.TrackSuccessOnly;
            existing.CollectHardwareMetrics = settings.CollectHardwareMetrics;
            existing.AggregateOldData = settings.AggregateOldData;
            existing.AggregationThresholdDays = settings.AggregationThresholdDays;
            existing.MaxDatabaseSizeMB = settings.MaxDatabaseSizeMB;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Analytics settings updated");

            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update analytics settings");
            return StatusCode(500, new { error = "Failed to update settings" });
        }
    }

    /// <summary>
    /// Get database size and statistics
    /// </summary>
    [HttpGet("database/info")]
    [ProducesResponseType(typeof(DatabaseInfo), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseInfo>> GetDatabaseInfo(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var usageCount = await _context.UsageStatistics.CountAsync(cancellationToken);
            var costCount = await _context.CostTracking.CountAsync(cancellationToken);
            var perfCount = await _context.PerformanceMetrics.CountAsync(cancellationToken);
            var summaryCount = await _context.AnalyticsSummaries.CountAsync(cancellationToken);
            
            var estimatedSize = await _cleanupService.GetDatabaseSizeBytesAsync(cancellationToken);
            var sizeMB = estimatedSize / (1024.0 * 1024.0);

            var oldestUsage = await _context.UsageStatistics
                .OrderBy(u => u.Timestamp)
                .Select(u => u.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            var settings = await _context.AnalyticsRetentionSettings.FirstOrDefaultAsync(cancellationToken);

            return Ok(new DatabaseInfo
            {
                UsageRecords = usageCount,
                CostRecords = costCount,
                PerformanceRecords = perfCount,
                SummaryRecords = summaryCount,
                TotalRecords = usageCount + costCount + perfCount + summaryCount,
                EstimatedSizeMB = sizeMB,
                OldestRecordDate = oldestUsage,
                MaxSizeMB = settings?.MaxDatabaseSizeMB ?? 0,
                UsagePercent = settings?.MaxDatabaseSizeMB > 0 
                    ? (sizeMB / settings.MaxDatabaseSizeMB * 100) 
                    : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database info");
            return StatusCode(500, new { error = "Failed to retrieve database info" });
        }
    }

    /// <summary>
    /// Trigger manual cleanup (user-initiated)
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> TriggerCleanup(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manual cleanup triggered");
            
            await _cleanupService.CleanupAsync(cancellationToken);
            await _cleanupService.AggregateOldDataAsync(cancellationToken);

            return Ok(new { message = "Cleanup completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger cleanup");
            return StatusCode(500, new { error = "Failed to perform cleanup" });
        }
    }

    /// <summary>
    /// Clear all analytics data (user-initiated)
    /// </summary>
    [HttpDelete("data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearAllData(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Clear all analytics data requested");
            
            await _cleanupService.ClearAllDataAsync(cancellationToken);

            return Ok(new { message = "All analytics data cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear data");
            return StatusCode(500, new { error = "Failed to clear data" });
        }
    }

    /// <summary>
    /// Export analytics data as JSON
    /// </summary>
    [HttpGet("export")]
    [Produces("application/json", "text/csv")]
    public async Task<ActionResult> ExportData(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var usage = await _context.UsageStatistics
                .Where(u => u.Timestamp >= start && u.Timestamp <= end)
                .ToListAsync(cancellationToken);

            var costs = await _context.CostTracking
                .Where(c => c.Timestamp >= start && c.Timestamp <= end)
                .ToListAsync(cancellationToken);

            var performance = await _context.PerformanceMetrics
                .Where(p => p.Timestamp >= start && p.Timestamp <= end)
                .ToListAsync(cancellationToken);

            var export = new
            {
                exportDate = DateTime.UtcNow,
                dateRange = new { start, end },
                usage,
                costs,
                performance
            };

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCSV(usage, costs, performance);
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"analytics-export-{DateTime.UtcNow:yyyy-MM-dd}.csv");
            }

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return File(Encoding.UTF8.GetBytes(json), "application/json", $"analytics-export-{DateTime.UtcNow:yyyy-MM-dd}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export data");
            return StatusCode(500, new { error = "Failed to export data" });
        }
    }

    private static string GenerateCSV(
        List<UsageStatisticsEntity> usage,
        List<CostTrackingEntity> costs,
        List<PerformanceMetricsEntity> performance)
    {
        var csv = new StringBuilder();
        
        // Usage CSV
        csv.AppendLine("# Usage Statistics");
        csv.AppendLine("Timestamp,Provider,Model,GenerationType,Success,InputTokens,OutputTokens,DurationMs");
        foreach (var u in usage)
        {
            csv.AppendLine($"{u.Timestamp:O},{u.Provider},{u.Model},{u.GenerationType},{u.Success},{u.InputTokens},{u.OutputTokens},{u.DurationMs}");
        }
        
        csv.AppendLine();
        csv.AppendLine("# Cost Tracking");
        csv.AppendLine("Timestamp,Provider,Model,InputTokens,OutputTokens,TotalCost,Currency");
        foreach (var c in costs)
        {
            csv.AppendLine($"{c.Timestamp:O},{c.Provider},{c.Model},{c.InputTokens},{c.OutputTokens},{c.TotalCost},{c.Currency}");
        }
        
        csv.AppendLine();
        csv.AppendLine("# Performance Metrics");
        csv.AppendLine("Timestamp,OperationType,DurationMs,Success,CpuUsage,MemoryUsedMB");
        foreach (var p in performance)
        {
            csv.AppendLine($"{p.Timestamp:O},{p.OperationType},{p.DurationMs},{p.Success},{p.CpuUsagePercent},{p.MemoryUsedMB}");
        }

        return csv.ToString();
    }
}

// Request/Response DTOs
public class CostEstimateRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;
    
    [Required]
    public string Model { get; set; } = string.Empty;
    
    public long InputTokens { get; set; }
    
    public long OutputTokens { get; set; }
}

public class CostEstimate
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Currency { get; set; } = "USD";
}

public class MonthlyBudgetStatus
{
    public string YearMonth { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public Dictionary<string, decimal> ProviderCosts { get; set; } = new();
    public string Currency { get; set; } = "USD";
    public int DaysInMonth { get; set; }
    public int DaysElapsed { get; set; }
    public decimal ProjectedMonthlyTotal { get; set; }
    public bool AnalyticsEnabled { get; set; }
}

public class DatabaseInfo
{
    public int UsageRecords { get; set; }
    public int CostRecords { get; set; }
    public int PerformanceRecords { get; set; }
    public int SummaryRecords { get; set; }
    public int TotalRecords { get; set; }
    public double EstimatedSizeMB { get; set; }
    public DateTime? OldestRecordDate { get; set; }
    public int MaxSizeMB { get; set; }
    public double UsagePercent { get; set; }
}
