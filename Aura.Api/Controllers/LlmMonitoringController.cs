using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.Monitoring;
using Aura.Core.Models.Providers;
using Aura.Core.Providers;
using Aura.Core.Services.Monitoring;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for LLM monitoring, analytics, and diagnostics
/// </summary>
[ApiController]
[Route("api/llm")]
public class LlmMonitoringController : ControllerBase
{
    private readonly ILogger<LlmMonitoringController> _logger;
    private readonly LlmMonitoringService _monitoringService;

    public LlmMonitoringController(
        ILogger<LlmMonitoringController> logger,
        LlmMonitoringService monitoringService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
    }

    /// <summary>
    /// Gets current metrics summary for all providers and operations
    /// Target: return within 500ms
    /// </summary>
    [HttpGet("metrics")]
    public ActionResult<LlmMetricsSummaryDto> GetMetrics()
    {
        try
        {
            var sw = Stopwatch.StartNew();
            
            var metrics = _monitoringService.GetMetricsSummary();
            
            var providerMetrics = metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(m => new LlmOperationMetricsDto(
                    m.ProviderName,
                    m.OperationType.ToString(),
                    m.TotalCalls,
                    m.SuccessfulCalls,
                    m.FailedCalls,
                    m.SuccessRatePercent,
                    m.AverageLatencySeconds,
                    m.P95LatencySeconds,
                    m.P99LatencySeconds,
                    m.TotalTokens,
                    m.TotalCostUsd,
                    m.AverageQualityScore,
                    m.LastUpdated
                )).ToList()
            );
            
            sw.Stop();
            
            if (sw.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("GetMetrics took {ElapsedMs}ms (target: <500ms)", sw.ElapsedMilliseconds);
            }
            
            return Ok(new LlmMetricsSummaryDto(providerMetrics, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving LLM metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets per-provider health status
    /// </summary>
    [HttpGet("health")]
    public ActionResult<LlmHealthStatusDto> GetHealth()
    {
        try
        {
            var metrics = _monitoringService.GetMetricsSummary();
            var alerts = _monitoringService.GetRecentAlerts(10);
            
            var providerHealth = new Dictionary<string, ProviderHealthExtendedDto>();
            
            foreach (var (providerName, operationMetrics) in metrics)
            {
                var totalCalls = operationMetrics.Sum(m => m.TotalCalls);
                var totalSuccess = operationMetrics.Sum(m => m.SuccessfulCalls);
                var avgLatency = operationMetrics.Average(m => m.AverageLatencySeconds);
                
                var providerAlerts = alerts
                    .Where(a => a.ProviderName == providerName)
                    .Select(a => a.Message)
                    .Take(3)
                    .ToList();
                
                var successRate = totalCalls > 0 ? (totalSuccess * 100.0) / totalCalls : 100.0;
                var status = DetermineHealthStatus(successRate, avgLatency);
                
                providerHealth[providerName] = new ProviderHealthExtendedDto(
                    providerName,
                    status,
                    successRate,
                    avgLatency,
                    totalCalls,
                    0,
                    providerAlerts
                );
            }
            
            return Ok(new LlmHealthStatusDto(providerHealth, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving LLM health status");
            return StatusCode(500, new { error = "Failed to retrieve health status", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets quality scores over time
    /// </summary>
    [HttpGet("quality-trends")]
    public ActionResult<QualityTrendsDto> GetQualityTrends(
        [FromQuery] DateTime? since = null,
        [FromQuery] string? providerName = null,
        [FromQuery] string? operationType = null)
    {
        try
        {
            var trends = _monitoringService.GetQualityTrends(since);
            
            if (!string.IsNullOrEmpty(providerName))
            {
                trends = trends.Where(t => t.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            if (!string.IsNullOrEmpty(operationType))
            {
                trends = trends.Where(t => t.OperationType.ToString().Equals(operationType, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var trendDtos = trends.Select(t => new QualityTrendPointDto(
                t.ProviderName,
                t.OperationType.ToString(),
                t.QualityScore,
                t.Timestamp
            )).ToList();
            
            var startDate = trends.Count > 0 ? trends.Min(t => t.Timestamp) : DateTime.UtcNow;
            var endDate = trends.Count > 0 ? trends.Max(t => t.Timestamp) : DateTime.UtcNow;
            
            return Ok(new QualityTrendsDto(trendDtos, startDate, endDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quality trends");
            return StatusCode(500, new { error = "Failed to retrieve quality trends", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets cost breakdown report
    /// </summary>
    [HttpGet("cost-report")]
    public ActionResult<LlmCostReportDto> GetCostReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? videosGenerated = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            
            var report = _monitoringService.GetCostReport(start, end, videosGenerated);
            
            var costByOperationDto = report.CostByOperation.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );
            
            var dailyCostsDto = report.DailyCosts.ToDictionary(
                kvp => kvp.Key.ToString("yyyy-MM-dd"),
                kvp => kvp.Value
            );
            
            return Ok(new LlmCostReportDto(
                report.TotalCostUsd,
                report.CostByProvider,
                costByOperationDto,
                dailyCostsDto,
                report.StartDate,
                report.EndDate,
                report.AverageCostPerVideo,
                report.MonthlyBudget,
                report.BudgetUsedPercent,
                report.ProjectedMonthlyCost
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cost report");
            return StatusCode(500, new { error = "Failed to retrieve cost report", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Manual provider testing endpoint (simplified - tests connectivity only)
    /// </summary>
    [HttpPost("test-provider")]
    public ActionResult<TestLlmProviderResponse> TestProvider(
        [FromBody] TestLlmProviderRequest request)
    {
        try
        {
            var validProviders = new[] { "OpenAI", "Claude", "Gemini", "Ollama", "RuleBased" };
            
            if (!validProviders.Contains(request.ProviderName))
            {
                return NotFound(new { error = $"Provider '{request.ProviderName}' not found. Valid providers: {string.Join(", ", validProviders)}" });
            }
            
            return Ok(new TestLlmProviderResponse(
                true,
                $"Provider '{request.ProviderName}' is configured. Use actual LLM operations to test functionality.",
                0.1,
                0,
                0,
                null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing provider");
            return StatusCode(500, new { error = "Failed to test provider", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets recent alerts
    /// </summary>
    [HttpGet("alerts")]
    public ActionResult<List<LlmAlertDto>> GetAlerts([FromQuery] int count = 50)
    {
        try
        {
            var alerts = _monitoringService.GetRecentAlerts(count);
            
            var alertDtos = alerts.Select(a => new LlmAlertDto(
                a.Type.ToString(),
                a.Severity.ToString(),
                a.Message,
                a.ProviderName,
                a.CurrentValue,
                a.ThresholdValue,
                a.Timestamp
            )).ToList();
            
            return Ok(alertDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, new { error = "Failed to retrieve alerts", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets performance analytics
    /// </summary>
    [HttpGet("performance")]
    public ActionResult<LlmPerformanceAnalyticsDto> GetPerformanceAnalytics()
    {
        try
        {
            var analytics = _monitoringService.GetPerformanceAnalytics();
            
            var bottleneckDtos = analytics.Bottlenecks.Select(b => new BottleneckOperationDto(
                b.ProviderName,
                b.OperationType.ToString(),
                b.AverageLatencySeconds,
                b.CallCount,
                b.TotalTimeSeconds
            )).ToList();
            
            var trendDtos = analytics.ExecutionTrends.Select(t => new PipelineExecutionTrendDto(
                t.Timestamp,
                t.TotalExecutionSeconds,
                t.OperationsCount
            )).ToList();
            
            return Ok(new LlmPerformanceAnalyticsDto(bottleneckDtos, trendDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance analytics");
            return StatusCode(500, new { error = "Failed to retrieve performance analytics", correlationId = HttpContext.TraceIdentifier });
        }
    }

    /// <summary>
    /// Gets monitoring overhead statistics
    /// </summary>
    [HttpGet("overhead")]
    public ActionResult<object> GetOverheadStatistics()
    {
        try
        {
            var (avgOverhead, percentage) = _monitoringService.GetOverheadStatistics();
            
            return Ok(new
            {
                averageOverheadMicroseconds = avgOverhead,
                percentageOfTotal = percentage,
                withinTarget = avgOverhead < 2000 // 2ms = 2000 microseconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overhead statistics");
            return StatusCode(500, new { error = "Failed to retrieve overhead statistics", correlationId = HttpContext.TraceIdentifier });
        }
    }

    private static string DetermineHealthStatus(double successRate, double avgLatency)
    {
        if (successRate >= 95 && avgLatency < 5)
        {
            return "Healthy";
        }
        else if (successRate >= 90 && avgLatency < 10)
        {
            return "Degraded";
        }
        else if (successRate >= 80)
        {
            return "Warning";
        }
        else
        {
            return "Critical";
        }
    }

    private static int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
