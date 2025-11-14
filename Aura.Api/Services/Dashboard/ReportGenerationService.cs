using System.Text;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.Dashboard;

/// <summary>
/// Service for generating exportable quality reports
/// </summary>
public class ReportGenerationService
{
    private readonly ILogger<ReportGenerationService> _logger;
    private readonly MetricsAggregationService _metricsService;
    private readonly TrendAnalysisService _trendService;
    private readonly RecommendationService _recommendationService;

    public ReportGenerationService(
        ILogger<ReportGenerationService> logger,
        MetricsAggregationService metricsService,
        TrendAnalysisService trendService,
        RecommendationService recommendationService)
    {
        _logger = logger;
        _metricsService = metricsService;
        _trendService = trendService;
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// Generates a comprehensive quality report
    /// </summary>
    public async Task<ReportData> GenerateReportAsync(
        ReportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating quality report with format {Format}", options.Format);

            // Gather all necessary data
            var metrics = await _metricsService.GetAggregatedMetricsAsync(ct).ConfigureAwait(false);
            var breakdown = await _metricsService.GetMetricsBreakdownAsync(ct).ConfigureAwait(false);
            var trends = await _trendService.GetHistoricalTrendsAsync(
                options.StartDate ?? DateTime.UtcNow.AddDays(-30),
                options.EndDate ?? DateTime.UtcNow,
                options.Granularity ?? "daily",
                ct).ConfigureAwait(false);
            var recommendations = await _recommendationService.GetRecommendationsAsync(metrics, ct).ConfigureAwait(false);

            // Generate report content based on format
            var content = options.Format?.ToLower() switch
            {
                "json" => GenerateJsonReport(metrics, breakdown, trends, recommendations),
                "csv" => GenerateCsvReport(metrics, breakdown, trends),
                "markdown" => GenerateMarkdownReport(metrics, breakdown, trends, recommendations),
                _ => GenerateJsonReport(metrics, breakdown, trends, recommendations)
            };

            return new ReportData
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                Format = options.Format ?? "json",
                Content = content,
                ContentType = GetContentType(options.Format ?? "json"),
                Filename = GenerateFilename(options.Format ?? "json")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            throw;
        }
    }

    private string GenerateJsonReport(
        QualityMetrics metrics,
        MetricsBreakdown breakdown,
        HistoricalTrends trends,
        List<QualityRecommendation> recommendations)
    {
        var report = new
        {
            generatedAt = DateTime.UtcNow,
            summary = new
            {
                metrics.TotalVideosProcessed,
                metrics.AverageQualityScore,
                metrics.SuccessRate,
                metrics.ComplianceRate
            },
            breakdown = new
            {
                resolution = breakdown.ResolutionMetrics,
                audio = breakdown.AudioQualityMetrics,
                frameRate = breakdown.FrameRateMetrics,
                consistency = breakdown.ConsistencyMetrics
            },
            trends = new
            {
                trends.TrendDirection,
                trends.AverageChange,
                dataPoints = trends.DataPoints.Select(d => new
                {
                    timestamp = d.Timestamp,
                    qualityScore = d.QualityScore,
                    processedVideos = d.ProcessedVideos,
                    errorCount = d.ErrorCount
                })
            },
            recommendations = recommendations.Select(r => new
            {
                r.Title,
                r.Description,
                r.Priority,
                r.Category,
                r.ImpactScore,
                r.ActionItems
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GenerateCsvReport(
        QualityMetrics metrics,
        MetricsBreakdown breakdown,
        HistoricalTrends trends)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Category,Metric,Value");
        csv.AppendLine($"Overall,Total Videos Processed,{metrics.TotalVideosProcessed}");
        csv.AppendLine($"Overall,Average Quality Score,{metrics.AverageQualityScore:F2}");
        csv.AppendLine($"Overall,Success Rate,{metrics.SuccessRate:F2}%");
        csv.AppendLine($"Overall,Compliance Rate,{metrics.ComplianceRate:F2}%");
        csv.AppendLine();
        csv.AppendLine("Date,Quality Score,Videos Processed,Errors");
        foreach (var point in trends.DataPoints)
        {
            csv.AppendLine($"{point.Timestamp:yyyy-MM-dd},{point.QualityScore:F2},{point.ProcessedVideos},{point.ErrorCount}");
        }

        return csv.ToString();
    }

    private string GenerateMarkdownReport(
        QualityMetrics metrics,
        MetricsBreakdown breakdown,
        HistoricalTrends trends,
        List<QualityRecommendation> recommendations)
    {
        var md = new StringBuilder();
        md.AppendLine("# Quality Dashboard Report");
        md.AppendLine($"*Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");
        md.AppendLine();
        md.AppendLine("## Summary Metrics");
        md.AppendLine($"- **Total Videos Processed**: {metrics.TotalVideosProcessed}");
        md.AppendLine($"- **Average Quality Score**: {metrics.AverageQualityScore:F2}");
        md.AppendLine($"- **Success Rate**: {metrics.SuccessRate:F2}%");
        md.AppendLine($"- **Compliance Rate**: {metrics.ComplianceRate:F2}%");
        md.AppendLine();
        md.AppendLine("## Quality Breakdown");
        md.AppendLine($"- **Resolution**: {breakdown.ResolutionMetrics.AverageScore:F2}% ({breakdown.ResolutionMetrics.PassedChecks}/{breakdown.ResolutionMetrics.TotalChecks} passed)");
        md.AppendLine($"- **Audio Quality**: {breakdown.AudioQualityMetrics.AverageScore:F2}% ({breakdown.AudioQualityMetrics.PassedChecks}/{breakdown.AudioQualityMetrics.TotalChecks} passed)");
        md.AppendLine($"- **Frame Rate**: {breakdown.FrameRateMetrics.AverageScore:F2}% ({breakdown.FrameRateMetrics.PassedChecks}/{breakdown.FrameRateMetrics.TotalChecks} passed)");
        md.AppendLine($"- **Consistency**: {breakdown.ConsistencyMetrics.AverageScore:F2}% ({breakdown.ConsistencyMetrics.PassedChecks}/{breakdown.ConsistencyMetrics.TotalChecks} passed)");
        md.AppendLine();
        md.AppendLine("## Recommendations");
        foreach (var rec in recommendations.Take(5))
        {
            md.AppendLine($"### {rec.Title} ({rec.Priority.ToUpper()})");
            md.AppendLine(rec.Description);
            md.AppendLine("**Action Items:**");
            foreach (var item in rec.ActionItems)
            {
                md.AppendLine($"- {item}");
            }
            md.AppendLine();
        }

        return md.ToString();
    }

    private string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "json" => "application/json",
            "csv" => "text/csv",
            "markdown" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    private string GenerateFilename(string format)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"quality-report-{timestamp}.{format.ToLower()}";
    }
}

public class ReportOptions
{
    public string? Format { get; set; } = "json"; // json, csv, markdown
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Granularity { get; set; } = "daily";
    public bool IncludeRecommendations { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
}

public class ReportData
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
}
