using Aura.Api.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for quality monitoring dashboard endpoints
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class QualityDashboardController : ControllerBase
{
    private readonly MetricsAggregationService _metricsService;
    private readonly TrendAnalysisService _trendService;
    private readonly RecommendationService _recommendationService;
    private readonly ReportGenerationService _reportService;

    public QualityDashboardController(
        MetricsAggregationService metricsService,
        TrendAnalysisService trendService,
        RecommendationService recommendationService,
        ReportGenerationService reportService)
    {
        _metricsService = metricsService;
        _trendService = trendService;
        _recommendationService = recommendationService;
        _reportService = reportService;
    }

    /// <summary>
    /// Gets overall quality metrics for the dashboard
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting dashboard metrics", correlationId);

            var metrics = await _metricsService.GetAggregatedMetricsAsync(ct).ConfigureAwait(false);
            var breakdown = await _metricsService.GetMetricsBreakdownAsync(ct).ConfigureAwait(false);

            return Ok(new
            {
                metrics = new
                {
                    metrics.TotalVideosProcessed,
                    metrics.AverageQualityScore,
                    metrics.SuccessRate,
                    averageProcessingTime = metrics.AverageProcessingTime.ToString(),
                    metrics.TotalErrorsLast24h,
                    metrics.CurrentProcessingJobs,
                    metrics.QueuedJobs,
                    metrics.PeakQualityScore,
                    metrics.LowestQualityScore,
                    metrics.ComplianceRate,
                    lastUpdated = metrics.LastUpdated
                },
                breakdown = new
                {
                    resolution = new
                    {
                        breakdown.ResolutionMetrics.TotalChecks,
                        breakdown.ResolutionMetrics.PassedChecks,
                        breakdown.ResolutionMetrics.FailedChecks,
                        breakdown.ResolutionMetrics.AverageScore
                    },
                    audio = new
                    {
                        breakdown.AudioQualityMetrics.TotalChecks,
                        breakdown.AudioQualityMetrics.PassedChecks,
                        breakdown.AudioQualityMetrics.FailedChecks,
                        breakdown.AudioQualityMetrics.AverageScore
                    },
                    frameRate = new
                    {
                        breakdown.FrameRateMetrics.TotalChecks,
                        breakdown.FrameRateMetrics.PassedChecks,
                        breakdown.FrameRateMetrics.FailedChecks,
                        breakdown.FrameRateMetrics.AverageScore
                    },
                    consistency = new
                    {
                        breakdown.ConsistencyMetrics.TotalChecks,
                        breakdown.ConsistencyMetrics.PassedChecks,
                        breakdown.ConsistencyMetrics.FailedChecks,
                        breakdown.ConsistencyMetrics.AverageScore
                    }
                },
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting dashboard metrics", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#dashboard-metrics",
                title = "Dashboard Metrics Failed",
                status = 500,
                detail = $"Failed to retrieve dashboard metrics: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets historical trend data for quality metrics
    /// </summary>
    [HttpGet("historical-data")]
    public async Task<IActionResult> GetHistoricalData(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string granularity = "daily",
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting historical data", correlationId);

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var trends = await _trendService.GetHistoricalTrendsAsync(start, end, granularity, ct).ConfigureAwait(false);

            return Ok(new
            {
                startDate = trends.StartDate,
                endDate = trends.EndDate,
                trends.Granularity,
                trendDirection = trends.TrendDirection,
                averageChange = trends.AverageChange,
                dataPoints = trends.DataPoints.Select(d => new
                {
                    timestamp = d.Timestamp,
                    qualityScore = d.QualityScore,
                    processedVideos = d.ProcessedVideos,
                    errorCount = d.ErrorCount,
                    averageProcessingTime = d.AverageProcessingTime.ToString()
                }),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting historical data", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#dashboard-historical",
                title = "Historical Data Failed",
                status = 500,
                detail = $"Failed to retrieve historical data: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets platform-specific compliance metrics
    /// </summary>
    [HttpGet("platform-compliance")]
    public async Task<IActionResult> GetPlatformCompliance(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting platform compliance data", correlationId);

            await Task.Delay(50, ct).ConfigureAwait(false);

            // Sample platform compliance data
            var compliance = new[]
            {
                new
                {
                    platform = "YouTube",
                    complianceRate = 98.5,
                    totalVideos = 450,
                    compliantVideos = 443,
                    commonIssues = new[] { "Audio normalization", "Thumbnail size" }
                },
                new
                {
                    platform = "TikTok",
                    complianceRate = 96.2,
                    totalVideos = 380,
                    compliantVideos = 366,
                    commonIssues = new[] { "Aspect ratio", "Duration limits" }
                },
                new
                {
                    platform = "Instagram",
                    complianceRate = 97.8,
                    totalVideos = 290,
                    compliantVideos = 284,
                    commonIssues = new[] { "Video length", "File size" }
                },
                new
                {
                    platform = "Facebook",
                    complianceRate = 99.1,
                    totalVideos = 127,
                    compliantVideos = 126,
                    commonIssues = new[] { "Caption format" }
                }
            };

            return Ok(new
            {
                platforms = compliance,
                overallComplianceRate = compliance.Average(p => p.complianceRate),
                totalPlatforms = compliance.Length,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting platform compliance", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#dashboard-compliance",
                title = "Platform Compliance Failed",
                status = 500,
                detail = $"Failed to retrieve platform compliance: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets AI-driven quality improvement recommendations
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting quality recommendations", correlationId);

            var metrics = await _metricsService.GetAggregatedMetricsAsync(ct).ConfigureAwait(false);
            var recommendations = await _recommendationService.GetRecommendationsAsync(metrics, ct).ConfigureAwait(false);

            return Ok(new
            {
                recommendations = recommendations.Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Description,
                    r.Priority,
                    r.Category,
                    r.ImpactScore,
                    r.EstimatedImprovement,
                    r.ActionItems
                }),
                totalRecommendations = recommendations.Count,
                highPriority = recommendations.Count(r => r.Priority == "high"),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting recommendations", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#dashboard-recommendations",
                title = "Recommendations Failed",
                status = 500,
                detail = $"Failed to retrieve recommendations: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Generates an exportable quality report
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportReport(
        [FromBody] ReportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Generating report with format {Format}", 
                correlationId, options.Format);

            var report = await _reportService.GenerateReportAsync(options, ct).ConfigureAwait(false);

            return File(
                System.Text.Encoding.UTF8.GetBytes(report.Content),
                report.ContentType,
                report.Filename
            );
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error generating report", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#dashboard-export",
                title = "Report Export Failed",
                status = 500,
                detail = $"Failed to generate report: {ex.Message}",
                correlationId
            });
        }
    }
}
