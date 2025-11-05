using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.CostTracking;

/// <summary>
/// Service for generating comprehensive cost reports for video generation runs
/// </summary>
public class RunCostReportService
{
    private readonly ILogger<RunCostReportService> _logger;
    private readonly ProviderSettings _settings;
    private readonly TokenTrackingService _tokenTrackingService;
    private readonly EnhancedCostTrackingService _costTrackingService;
    private readonly string _dataDirectory;
    private readonly string _reportsDirectory;

    public RunCostReportService(
        ILogger<RunCostReportService> logger,
        ProviderSettings settings,
        TokenTrackingService tokenTrackingService,
        EnhancedCostTrackingService costTrackingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _tokenTrackingService = tokenTrackingService ?? throw new ArgumentNullException(nameof(tokenTrackingService));
        _costTrackingService = costTrackingService ?? throw new ArgumentNullException(nameof(costTrackingService));
        
        _dataDirectory = Path.Combine(_settings.GetAuraDataDirectory(), "cost-tracking");
        _reportsDirectory = Path.Combine(_dataDirectory, "reports");
        
        Directory.CreateDirectory(_reportsDirectory);
    }

    /// <summary>
    /// Generate a comprehensive cost report for a completed run
    /// </summary>
    public RunCostReport GenerateReport(
        string jobId,
        string? projectId,
        string? projectName,
        DateTime startedAt,
        DateTime? completedAt,
        Dictionary<string, decimal> stageCosts,
        decimal? budgetLimit = null)
    {
        var tokenStats = _tokenTrackingService.GetJobStatistics(jobId);
        var tokenMetrics = _tokenTrackingService.GetJobMetrics(jobId);
        
        var totalCost = stageCosts.Values.Sum();
        
        var costByProvider = tokenMetrics
            .GroupBy(m => m.ProviderName)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.EstimatedCost));
        
        var costByStage = stageCosts.ToDictionary(
            kvp => kvp.Key,
            kvp => new StageCostBreakdown
            {
                StageName = kvp.Key,
                Cost = kvp.Value,
                PercentageOfTotal = totalCost > 0 ? (double)(kvp.Value / totalCost * 100) : 0,
                DurationSeconds = 0,
                OperationCount = tokenMetrics.Count(m => GetStageForOperation(m.OperationType) == kvp.Key),
                ProviderName = tokenMetrics
                    .Where(m => GetStageForOperation(m.OperationType) == kvp.Key)
                    .GroupBy(m => m.ProviderName)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
            });
        
        var operations = tokenMetrics
            .Select(m => new OperationCostDetail
            {
                Timestamp = m.Timestamp,
                OperationType = m.OperationType,
                ProviderName = m.ProviderName,
                Cost = m.EstimatedCost,
                DurationMs = m.ResponseTimeMs,
                TokensUsed = m.TotalTokens,
                CacheHit = m.CacheHit
            })
            .OrderBy(o => o.Timestamp)
            .ToList();
        
        var suggestions = _tokenTrackingService.GenerateOptimizationSuggestions(jobId);
        
        var report = new RunCostReport
        {
            JobId = jobId,
            ProjectId = projectId,
            ProjectName = projectName,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TotalCost = totalCost,
            CostByStage = costByStage,
            CostByProvider = costByProvider,
            TokenStats = tokenStats,
            Operations = operations,
            OptimizationSuggestions = suggestions,
            WithinBudget = !budgetLimit.HasValue || totalCost <= budgetLimit.Value,
            BudgetLimit = budgetLimit
        };
        
        SaveReport(report);
        
        _logger.LogInformation(
            "Generated cost report for job {JobId}: ${TotalCost:F4} across {OperationCount} operations",
            jobId, totalCost, operations.Count);
        
        return report;
    }

    /// <summary>
    /// Export report to JSON format
    /// </summary>
    public string ExportToJson(RunCostReport report)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        var fileName = $"cost-report-{report.JobId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_reportsDirectory, fileName);
        
        File.WriteAllText(filePath, json);
        
        _logger.LogInformation("Exported cost report to JSON: {FilePath}", filePath);
        
        return filePath;
    }

    /// <summary>
    /// Export report to CSV format
    /// </summary>
    public string ExportToCsv(RunCostReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("Aura Video Studio - Cost Report");
        sb.AppendLine($"Job ID,{report.JobId}");
        sb.AppendLine($"Project,{report.ProjectName ?? "N/A"}");
        sb.AppendLine($"Started,{report.StartedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Completed,{report.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "In Progress"}");
        sb.AppendLine($"Duration,{report.DurationSeconds:F1} seconds");
        sb.AppendLine($"Total Cost,${report.TotalCost:F4}");
        sb.AppendLine($"Currency,{report.Currency}");
        sb.AppendLine();
        
        sb.AppendLine("Stage Breakdown");
        sb.AppendLine("Stage,Cost,Percentage,Operations");
        foreach (var stage in report.CostByStage.Values.OrderByDescending(s => s.Cost))
        {
            sb.AppendLine($"{stage.StageName},${stage.Cost:F4},{stage.PercentageOfTotal:F1}%,{stage.OperationCount}");
        }
        sb.AppendLine();
        
        sb.AppendLine("Provider Breakdown");
        sb.AppendLine("Provider,Cost");
        foreach (var provider in report.CostByProvider.OrderByDescending(p => p.Value))
        {
            sb.AppendLine($"{provider.Key},${provider.Value:F4}");
        }
        sb.AppendLine();
        
        if (report.TokenStats != null)
        {
            sb.AppendLine("Token Usage Statistics");
            sb.AppendLine($"Total Input Tokens,{report.TokenStats.TotalInputTokens:N0}");
            sb.AppendLine($"Total Output Tokens,{report.TokenStats.TotalOutputTokens:N0}");
            sb.AppendLine($"Total Tokens,{report.TokenStats.TotalTokens:N0}");
            sb.AppendLine($"Cache Hits,{report.TokenStats.CacheHits}");
            sb.AppendLine($"Cache Hit Rate,{report.TokenStats.CacheHitRate:F1}%");
            sb.AppendLine($"Cost Saved by Cache,${report.TokenStats.CostSavedByCache:F4}");
            sb.AppendLine($"Average Response Time,{report.TokenStats.AverageResponseTimeMs}ms");
            sb.AppendLine();
        }
        
        sb.AppendLine("Operations");
        sb.AppendLine("Timestamp,Operation,Provider,Cost,Duration (ms),Tokens,Cache Hit");
        foreach (var op in report.Operations)
        {
            sb.AppendLine($"{op.Timestamp:yyyy-MM-dd HH:mm:ss},{op.OperationType},{op.ProviderName},${op.Cost:F4},{op.DurationMs},{op.TokensUsed ?? 0},{op.CacheHit}");
        }
        sb.AppendLine();
        
        if (report.OptimizationSuggestions.Any())
        {
            sb.AppendLine("Optimization Suggestions");
            sb.AppendLine("Category,Suggestion,Estimated Savings,Quality Impact");
            foreach (var suggestion in report.OptimizationSuggestions)
            {
                sb.AppendLine($"{suggestion.Category},{suggestion.Suggestion},${suggestion.EstimatedSavings:F4},{suggestion.QualityImpact ?? "N/A"}");
            }
        }
        
        var fileName = $"cost-report-{report.JobId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var filePath = Path.Combine(_reportsDirectory, fileName);
        
        File.WriteAllText(filePath, sb.ToString());
        
        _logger.LogInformation("Exported cost report to CSV: {FilePath}", filePath);
        
        return filePath;
    }

    /// <summary>
    /// Get a previously saved report
    /// </summary>
    public RunCostReport? GetReport(string jobId)
    {
        var reportPath = Path.Combine(_reportsDirectory, $"{jobId}.json");
        
        if (!File.Exists(reportPath))
        {
            return null;
        }
        
        try
        {
            var json = File.ReadAllText(reportPath);
            return JsonSerializer.Deserialize<RunCostReport>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cost report for job {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Get all available reports
    /// </summary>
    public List<RunCostReport> GetAllReports(int maxCount = 50)
    {
        var reports = new List<RunCostReport>();
        
        try
        {
            var files = Directory.GetFiles(_reportsDirectory, "*.json")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .Take(maxCount);
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var report = JsonSerializer.Deserialize<RunCostReport>(json);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load report from {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate cost reports");
        }
        
        return reports;
    }

    private void SaveReport(RunCostReport report)
    {
        try
        {
            var reportPath = Path.Combine(_reportsDirectory, $"{report.JobId}.json");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(reportPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cost report for job {JobId}", report.JobId);
        }
    }

    private static string GetStageForOperation(string operationType)
    {
        return operationType.ToLowerInvariant() switch
        {
            var op when op.Contains("script") || op.Contains("planning") => "ScriptGeneration",
            var op when op.Contains("tts") || op.Contains("speech") || op.Contains("ssml") => "TTS",
            var op when op.Contains("visual") || op.Contains("image") => "Visuals",
            var op when op.Contains("render") || op.Contains("video") => "Rendering",
            _ => "Other"
        };
    }
}
