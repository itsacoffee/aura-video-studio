using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Main service for performance analytics operations
/// </summary>
public class PerformanceAnalyticsService
{
    private readonly ILogger<PerformanceAnalyticsService> _logger;
    private readonly AnalyticsPersistence _persistence;
    private readonly AnalyticsImporter _importer;
    private readonly VideoProjectLinker _linker;
    private readonly CorrelationAnalyzer _correlationAnalyzer;
    private readonly PerformancePatternDetector _patternDetector;

    public PerformanceAnalyticsService(
        ILogger<PerformanceAnalyticsService> logger,
        AnalyticsPersistence persistence,
        AnalyticsImporter importer,
        VideoProjectLinker linker,
        CorrelationAnalyzer correlationAnalyzer,
        PerformancePatternDetector patternDetector)
    {
        _logger = logger;
        _persistence = persistence;
        _importer = importer;
        _linker = linker;
        _correlationAnalyzer = correlationAnalyzer;
        _patternDetector = patternDetector;
    }

    #region Import Operations

    /// <summary>
    /// Import analytics from CSV file
    /// </summary>
    public async Task<AnalyticsImport> ImportCsvAsync(
        string profileId,
        string platform,
        string filePath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing CSV analytics for profile {ProfileId}", profileId);
        return await _importer.ImportFromCsvAsync(profileId, platform, filePath, ct);
    }

    /// <summary>
    /// Import analytics from JSON file
    /// </summary>
    public async Task<AnalyticsImport> ImportJsonAsync(
        string profileId,
        string platform,
        string filePath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing JSON analytics for profile {ProfileId}", profileId);
        return await _importer.ImportFromJsonAsync(profileId, platform, filePath, ct);
    }

    #endregion

    #region Video Operations

    /// <summary>
    /// Get all videos with performance data for a profile
    /// </summary>
    public async Task<List<VideoPerformanceData>> GetVideosAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadAllVideosAsync(profileId, ct);
    }

    /// <summary>
    /// Get a specific video's performance data
    /// </summary>
    public async Task<VideoPerformanceData?> GetVideoAsync(
        string profileId,
        string videoId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadVideoPerformanceAsync(profileId, videoId, ct);
    }

    #endregion

    #region Linking Operations

    /// <summary>
    /// Create manual link between video and project
    /// </summary>
    public async Task<VideoProjectLink> LinkVideoToProjectAsync(
        string videoId,
        string projectId,
        string profileId,
        string linkedBy,
        CancellationToken ct = default)
    {
        return await _linker.CreateManualLinkAsync(videoId, projectId, profileId, linkedBy, ct);
    }

    /// <summary>
    /// Get unlinked videos for a profile
    /// </summary>
    public async Task<List<VideoPerformanceData>> GetUnlinkedVideosAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _linker.GetUnlinkedVideosAsync(profileId, ct);
    }

    /// <summary>
    /// Get linked videos for a profile
    /// </summary>
    public async Task<List<(VideoPerformanceData Video, VideoProjectLink Link)>> GetLinkedVideosAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _linker.GetLinkedVideosAsync(profileId, ct);
    }

    #endregion

    #region Analysis Operations

    /// <summary>
    /// Analyze performance for a profile
    /// </summary>
    public async Task<PerformanceAnalysisResult> AnalyzePerformanceAsync(
        string profileId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing performance for profile {ProfileId}", profileId);

        // Get all linked videos
        var linkedVideos = await _linker.GetLinkedVideosAsync(profileId, ct);

        // Analyze correlations for each linked project
        var allCorrelations = new List<DecisionPerformanceCorrelation>();
        foreach (var (video, link) in linkedVideos.Where(lv => lv.Link.IsConfirmed))
        {
            var correlations = await _correlationAnalyzer.AnalyzeProjectCorrelationsAsync(
                link.ProjectId, profileId, ct);
            allCorrelations.AddRange(correlations);
        }

        // Detect patterns
        var (successPatterns, failurePatterns) = await _patternDetector.DetectPatternsAsync(profileId, ct);

        return new PerformanceAnalysisResult(
            ProfileId: profileId,
            AnalyzedAt: DateTime.UtcNow,
            TotalVideos: linkedVideos.Count,
            AnalyzedProjects: linkedVideos.Count(lv => lv.Link.IsConfirmed),
            TotalCorrelations: allCorrelations.Count,
            SuccessPatterns: successPatterns,
            FailurePatterns: failurePatterns
        );
    }

    /// <summary>
    /// Get correlations for a specific project
    /// </summary>
    public async Task<List<DecisionPerformanceCorrelation>> GetProjectCorrelationsAsync(
        string projectId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadCorrelationsAsync(projectId, ct);
    }

    /// <summary>
    /// Get success patterns for a profile
    /// </summary>
    public async Task<List<SuccessPattern>> GetSuccessPatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadSuccessPatternsAsync(profileId, ct);
    }

    /// <summary>
    /// Get failure patterns for a profile
    /// </summary>
    public async Task<List<FailurePattern>> GetFailurePatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadFailurePatternsAsync(profileId, ct);
    }

    /// <summary>
    /// Get performance insights for a profile
    /// </summary>
    public async Task<PerformanceInsights> GetInsightsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        var successPatterns = await _persistence.LoadSuccessPatternsAsync(profileId, ct);
        var failurePatterns = await _persistence.LoadFailurePatternsAsync(profileId, ct);
        var videos = await _persistence.LoadAllVideosAsync(profileId, ct);

        var insights = new List<string>();

        // Generate insights from success patterns
        foreach (var pattern in successPatterns.OrderByDescending(p => p.Impact.OverallImpact).Take(3))
        {
            insights.Add($"✓ {pattern.Description}");
        }

        // Generate insights from failure patterns
        foreach (var pattern in failurePatterns.OrderBy(p => p.Impact.OverallImpact).Take(3))
        {
            insights.Add($"✗ {pattern.Description}");
        }

        // Calculate overall performance
        var avgViews = videos.Count != 0 ? videos.Average(v => v.Metrics.Views) : 0;
        var avgEngagement = videos.Count != 0 ? videos.Average(v => v.Metrics.Engagement.EngagementRate) : 0;

        return new PerformanceInsights(
            ProfileId: profileId,
            GeneratedAt: DateTime.UtcNow,
            TotalVideos: videos.Count,
            AverageViews: avgViews,
            AverageEngagementRate: avgEngagement,
            TopSuccessPatterns: successPatterns.OrderByDescending(p => p.Strength).Take(5).ToList(),
            TopFailurePatterns: failurePatterns.OrderByDescending(p => p.Strength).Take(5).ToList(),
            ActionableInsights: insights,
            OverallTrend: CalculateTrend(videos)
        );
    }

    #endregion

    #region A/B Testing

    /// <summary>
    /// Create a new A/B test
    /// </summary>
    public async Task<ABTest> CreateABTestAsync(
        string profileId,
        string testName,
        string description,
        string category,
        List<TestVariant> variants,
        CancellationToken ct = default)
    {
        var test = new ABTest(
            TestId: Guid.NewGuid().ToString(),
            ProfileId: profileId,
            TestName: testName,
            Description: description,
            Category: category,
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            Status: "draft",
            Variants: variants,
            Results: null
        );

        await _persistence.SaveABTestAsync(test, ct);
        return test;
    }

    /// <summary>
    /// Get A/B test results
    /// </summary>
    public async Task<ABTest?> GetABTestResultsAsync(
        string profileId,
        string testId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadABTestAsync(profileId, testId, ct);
    }

    /// <summary>
    /// Get all A/B tests for a profile
    /// </summary>
    public async Task<List<ABTest>> GetAllABTestsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        return await _persistence.LoadAllABTestsAsync(profileId, ct);
    }

    #endregion

    #region Helper Methods

    private string CalculateTrend(List<VideoPerformanceData> videos)
    {
        if (videos.Count < 2)
        {
            return "insufficient_data";
        }

        var recentVideos = videos
            .OrderByDescending(v => v.PublishedAt)
            .Take(Math.Min(5, videos.Count))
            .ToList();

        var olderVideos = videos
            .OrderByDescending(v => v.PublishedAt)
            .Skip(Math.Min(5, videos.Count))
            .Take(Math.Min(5, videos.Count))
            .ToList();

        if (olderVideos.Count == 0)
        {
            return "new_profile";
        }

        var recentAvg = recentVideos.Average(v => v.Metrics.Views);
        var olderAvg = olderVideos.Average(v => v.Metrics.Views);

        var change = (recentAvg - olderAvg) / olderAvg;

        return change switch
        {
            > 0.2 => "improving",
            > 0.05 => "slightly_improving",
            > -0.05 => "stable",
            > -0.2 => "slightly_declining",
            _ => "declining"
        };
    }

    #endregion
}

/// <summary>
/// Result of performance analysis
/// </summary>
public record PerformanceAnalysisResult(
    string ProfileId,
    DateTime AnalyzedAt,
    int TotalVideos,
    int AnalyzedProjects,
    int TotalCorrelations,
    List<SuccessPattern> SuccessPatterns,
    List<FailurePattern> FailurePatterns
);

/// <summary>
/// Performance insights for a profile
/// </summary>
public record PerformanceInsights(
    string ProfileId,
    DateTime GeneratedAt,
    int TotalVideos,
    double AverageViews,
    double AverageEngagementRate,
    List<SuccessPattern> TopSuccessPatterns,
    List<FailurePattern> TopFailurePatterns,
    List<string> ActionableInsights,
    string OverallTrend
);
