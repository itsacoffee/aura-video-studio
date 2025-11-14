using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Aura.Core.Services.PerformanceAnalytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video performance analytics and feedback-based learning
/// </summary>
[ApiController]
[Route("api/performance-analytics")]
public class PerformanceAnalyticsController : ControllerBase
{
    private readonly ILogger<PerformanceAnalyticsController> _logger;
    private readonly PerformanceAnalyticsService _analyticsService;

    public PerformanceAnalyticsController(
        ILogger<PerformanceAnalyticsController> logger,
        PerformanceAnalyticsService analyticsService)
    {
        _logger = logger;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Import analytics data from CSV or JSON file
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportAnalytics(
        [FromBody] ImportAnalyticsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return BadRequest(new { error = "FilePath is required" });
            }

            AnalyticsImport import;

            if (request.FileType == "csv")
            {
                import = await _analyticsService.ImportCsvAsync(
                    request.ProfileId, request.Platform, request.FilePath, ct).ConfigureAwait(false);
            }
            else if (request.FileType == "json")
            {
                import = await _analyticsService.ImportJsonAsync(
                    request.ProfileId, request.Platform, request.FilePath, ct).ConfigureAwait(false);
            }
            else
            {
                return BadRequest(new { error = "FileType must be 'csv' or 'json'" });
            }

            return Ok(new
            {
                success = true,
                import = new
                {
                    import.ImportId,
                    import.Platform,
                    import.ImportType,
                    import.ImportedAt,
                    import.VideosImported
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing analytics for profile {ProfileId}", request.ProfileId);
            return StatusCode(500, new { error = $"Failed to import analytics: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get all videos with performance data for a profile
    /// </summary>
    [HttpGet("videos/{profileId}")]
    public async Task<IActionResult> GetVideos(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var videos = await _analyticsService.GetVideosAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                videos = videos.Select(v => new
                {
                    v.VideoId,
                    v.ProjectId,
                    v.Platform,
                    v.VideoTitle,
                    v.VideoUrl,
                    v.PublishedAt,
                    metrics = new
                    {
                        v.Metrics.Views,
                        v.Metrics.WatchTimeMinutes,
                        v.Metrics.AverageViewDuration,
                        v.Metrics.AverageViewPercentage,
                        engagement = new
                        {
                            v.Metrics.Engagement.Likes,
                            v.Metrics.Engagement.Comments,
                            v.Metrics.Engagement.Shares,
                            v.Metrics.Engagement.EngagementRate
                        }
                    }
                }).ToList(),
                count = videos.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve videos" });
        }
    }

    /// <summary>
    /// Link a video to a project manually
    /// </summary>
    [HttpPost("link-video")]
    public async Task<IActionResult> LinkVideo(
        [FromBody] LinkVideoRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.VideoId) || 
                string.IsNullOrWhiteSpace(request.ProjectId) || 
                string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "VideoId, ProjectId, and ProfileId are required" });
            }

            var link = await _analyticsService.LinkVideoToProjectAsync(
                request.VideoId, 
                request.ProjectId, 
                request.ProfileId,
                request.LinkedBy ?? "user",
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                link = new
                {
                    link.LinkId,
                    link.VideoId,
                    link.ProjectId,
                    link.LinkType,
                    link.ConfidenceScore,
                    link.LinkedAt,
                    link.IsConfirmed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking video to project");
            return StatusCode(500, new { error = "Failed to link video" });
        }
    }

    /// <summary>
    /// Get correlations between AI decisions and performance for a project
    /// </summary>
    [HttpGet("correlations/{projectId}")]
    public async Task<IActionResult> GetCorrelations(
        string projectId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            var correlations = await _analyticsService.GetProjectCorrelationsAsync(projectId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                projectId,
                correlations = correlations.Select(c => new
                {
                    c.CorrelationId,
                    c.VideoId,
                    c.DecisionType,
                    c.DecisionValue,
                    outcome = new
                    {
                        c.Outcome.OutcomeType,
                        c.Outcome.PerformanceScore,
                        c.Outcome.ComparedTo
                    },
                    c.CorrelationStrength,
                    c.StatisticalSignificance,
                    c.AnalyzedAt
                }).ToList(),
                count = correlations.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correlations for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to retrieve correlations" });
        }
    }

    /// <summary>
    /// Get performance insights for a profile
    /// </summary>
    [HttpGet("insights/{profileId}")]
    public async Task<IActionResult> GetInsights(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var insights = await _analyticsService.GetInsightsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                insights = new
                {
                    insights.GeneratedAt,
                    insights.TotalVideos,
                    insights.AverageViews,
                    insights.AverageEngagementRate,
                    insights.ActionableInsights,
                    insights.OverallTrend,
                    topSuccessPatterns = insights.TopSuccessPatterns.Select(p => new
                    {
                        p.PatternId,
                        p.PatternType,
                        p.Description,
                        p.Strength,
                        p.Occurrences,
                        impact = new
                        {
                            p.Impact.ViewsImpact,
                            p.Impact.EngagementImpact,
                            p.Impact.RetentionImpact,
                            p.Impact.OverallImpact
                        }
                    }).ToList(),
                    topFailurePatterns = insights.TopFailurePatterns.Select(p => new
                    {
                        p.PatternId,
                        p.PatternType,
                        p.Description,
                        p.Strength,
                        p.Occurrences,
                        impact = new
                        {
                            p.Impact.ViewsImpact,
                            p.Impact.EngagementImpact,
                            p.Impact.RetentionImpact,
                            p.Impact.OverallImpact
                        }
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve insights" });
        }
    }

    /// <summary>
    /// Analyze performance for a profile
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzePerformance(
        [FromBody] AnalyzePerformanceRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var result = await _analyticsService.AnalyzePerformanceAsync(request.ProfileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                analysis = new
                {
                    result.ProfileId,
                    result.AnalyzedAt,
                    result.TotalVideos,
                    result.AnalyzedProjects,
                    result.TotalCorrelations,
                    successPatternsFound = result.SuccessPatterns.Count,
                    failurePatternsFound = result.FailurePatterns.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing performance for profile {ProfileId}", request.ProfileId);
            return StatusCode(500, new { error = "Failed to analyze performance" });
        }
    }

    /// <summary>
    /// Get success patterns for a profile
    /// </summary>
    [HttpGet("success-patterns/{profileId}")]
    public async Task<IActionResult> GetSuccessPatterns(
        string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "ProfileId is required" });
            }

            var patterns = await _analyticsService.GetSuccessPatternsAsync(profileId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                profileId,
                patterns = patterns.Select(p => new
                {
                    p.PatternId,
                    p.PatternType,
                    p.Description,
                    p.Strength,
                    p.Occurrences,
                    p.FirstObserved,
                    p.LastObserved,
                    impact = new
                    {
                        p.Impact.ViewsImpact,
                        p.Impact.EngagementImpact,
                        p.Impact.RetentionImpact,
                        p.Impact.OverallImpact
                    },
                    p.PatternData
                }).ToList(),
                count = patterns.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting success patterns for profile {ProfileId}", profileId);
            return StatusCode(500, new { error = "Failed to retrieve success patterns" });
        }
    }

    /// <summary>
    /// Create a new A/B test
    /// </summary>
    [HttpPost("ab-test")]
    public async Task<IActionResult> CreateABTest(
        [FromBody] CreateABTestRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProfileId) || 
                string.IsNullOrWhiteSpace(request.TestName))
            {
                return BadRequest(new { error = "ProfileId and TestName are required" });
            }

            if (request.Variants == null || request.Variants.Count < 2)
            {
                return BadRequest(new { error = "At least 2 variants are required for A/B test" });
            }

            var variants = request.Variants.Select(v => new TestVariant(
                VariantId: Guid.NewGuid().ToString(),
                VariantName: v.VariantName,
                Description: v.Description,
                ProjectId: v.ProjectId,
                VideoId: null,
                Configuration: v.Configuration ?? new Dictionary<string, object>(),
                CreatedAt: DateTime.UtcNow
            )).ToList();

            var test = await _analyticsService.CreateABTestAsync(
                request.ProfileId,
                request.TestName,
                request.Description ?? "",
                request.Category ?? "general",
                variants,
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                test = new
                {
                    test.TestId,
                    test.TestName,
                    test.Description,
                    test.Category,
                    test.Status,
                    test.CreatedAt,
                    variants = test.Variants.Select(v => new
                    {
                        v.VariantId,
                        v.VariantName,
                        v.Description,
                        v.ProjectId
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating A/B test for profile {ProfileId}", request.ProfileId);
            return StatusCode(500, new { error = "Failed to create A/B test" });
        }
    }

    /// <summary>
    /// Get A/B test results
    /// </summary>
    [HttpGet("ab-results/{testId}")]
    public async Task<IActionResult> GetABTestResults(
        string testId,
        [FromQuery] string profileId,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(testId) || string.IsNullOrWhiteSpace(profileId))
            {
                return BadRequest(new { error = "TestId and ProfileId are required" });
            }

            var test = await _analyticsService.GetABTestResultsAsync(profileId, testId, ct).ConfigureAwait(false);

            if (test == null)
            {
                return NotFound(new { error = "A/B test not found" });
            }

            return Ok(new
            {
                success = true,
                test = new
                {
                    test.TestId,
                    test.TestName,
                    test.Description,
                    test.Category,
                    test.Status,
                    test.CreatedAt,
                    test.StartedAt,
                    test.CompletedAt,
                    variants = test.Variants.Select(v => new
                    {
                        v.VariantId,
                        v.VariantName,
                        v.Description,
                        v.ProjectId,
                        v.VideoId
                    }).ToList(),
                    results = test.Results != null ? new
                    {
                        test.Results.AnalyzedAt,
                        test.Results.Winner,
                        test.Results.Confidence,
                        test.Results.IsStatisticallySignificant,
                        test.Results.Insights
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting A/B test results for test {TestId}", testId);
            return StatusCode(500, new { error = "Failed to retrieve A/B test results" });
        }
    }
}

// Request models
public record ImportAnalyticsRequest(
    string ProfileId,
    string Platform,
    string FileType,  // "csv" or "json"
    string FilePath
);

public record LinkVideoRequest(
    string VideoId,
    string ProjectId,
    string ProfileId,
    string? LinkedBy
);

public record AnalyzePerformanceRequest(
    string ProfileId
);

public record CreateABTestRequest(
    string ProfileId,
    string TestName,
    string? Description,
    string? Category,
    List<VariantRequest> Variants
);

public record VariantRequest(
    string VariantName,
    string? Description,
    string? ProjectId,
    Dictionary<string, object>? Configuration
);
