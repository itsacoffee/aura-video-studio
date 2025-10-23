using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.Dashboard;

/// <summary>
/// Service for generating AI-driven quality recommendations
/// </summary>
public class RecommendationService
{
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(ILogger<RecommendationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets quality improvement recommendations based on current metrics
    /// </summary>
    public async Task<List<QualityRecommendation>> GetRecommendationsAsync(
        QualityMetrics metrics,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating quality recommendations");

            await Task.Delay(50, ct);

            var recommendations = new List<QualityRecommendation>();

            // Analyze metrics and generate recommendations
            if (metrics.AverageQualityScore < 90)
            {
                recommendations.Add(new QualityRecommendation
                {
                    Id = "rec-1",
                    Title = "Improve Overall Quality Score",
                    Description = "Your average quality score is below 90%. Consider reviewing encoding settings and source material quality.",
                    Priority = "high",
                    Category = "quality",
                    ImpactScore = 8.5,
                    EstimatedImprovement = "5-10% quality increase",
                    ActionItems = new List<string>
                    {
                        "Review video encoding bitrate settings",
                        "Ensure source materials meet minimum quality standards",
                        "Enable hardware acceleration if available"
                    }
                });
            }

            if (metrics.AverageProcessingTime > TimeSpan.FromMinutes(15))
            {
                recommendations.Add(new QualityRecommendation
                {
                    Id = "rec-2",
                    Title = "Optimize Processing Time",
                    Description = "Average processing time exceeds 15 minutes. Performance optimization could reduce wait times.",
                    Priority = "medium",
                    Category = "performance",
                    ImpactScore = 7.0,
                    EstimatedImprovement = "30-40% faster processing",
                    ActionItems = new List<string>
                    {
                        "Enable GPU acceleration for video encoding",
                        "Consider using faster encoding presets",
                        "Review system resource allocation"
                    }
                });
            }

            if (metrics.TotalErrorsLast24h > 5)
            {
                recommendations.Add(new QualityRecommendation
                {
                    Id = "rec-3",
                    Title = "Address Error Rate",
                    Description = "Recent error rate is elevated. Investigate common failure patterns.",
                    Priority = "high",
                    Category = "reliability",
                    ImpactScore = 9.0,
                    EstimatedImprovement = "Reduce errors by 60-80%",
                    ActionItems = new List<string>
                    {
                        "Review error logs for common patterns",
                        "Update to latest stable versions",
                        "Implement additional validation checks"
                    }
                });
            }

            if (metrics.ComplianceRate < 98)
            {
                recommendations.Add(new QualityRecommendation
                {
                    Id = "rec-4",
                    Title = "Improve Platform Compliance",
                    Description = "Some videos are not meeting platform-specific requirements.",
                    Priority = "medium",
                    Category = "compliance",
                    ImpactScore = 6.5,
                    EstimatedImprovement = "98%+ compliance rate",
                    ActionItems = new List<string>
                    {
                        "Enable platform-specific validation before export",
                        "Review failed compliance checks",
                        "Update platform requirement profiles"
                    }
                });
            }

            // Always include some best practices
            recommendations.Add(new QualityRecommendation
            {
                Id = "rec-5",
                Title = "Enable Automated Quality Checks",
                Description = "Implement pre-flight quality validation to catch issues before processing.",
                Priority = "low",
                Category = "best-practice",
                ImpactScore = 5.0,
                EstimatedImprovement = "Prevent 90% of quality issues",
                ActionItems = new List<string>
                {
                    "Enable automated pre-flight checks",
                    "Configure quality thresholds",
                    "Set up alert notifications"
                }
            });

            return recommendations.OrderByDescending(r => r.ImpactScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            throw;
        }
    }
}

public class QualityRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // low, medium, high
    public string Category { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
    public string EstimatedImprovement { get; set; } = string.Empty;
    public List<string> ActionItems { get; set; } = new();
}
