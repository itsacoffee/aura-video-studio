using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PerformanceAnalytics;

/// <summary>
/// Detects success and failure patterns from performance data
/// </summary>
public class PerformancePatternDetector
{
    private readonly ILogger<PerformancePatternDetector> _logger;
    private readonly AnalyticsPersistence _persistence;
    private const double MIN_PATTERN_CONFIDENCE = 0.6;
    private const int MIN_OCCURRENCES = 3;

    public PerformancePatternDetector(
        ILogger<PerformancePatternDetector> logger,
        AnalyticsPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    /// <summary>
    /// Detect patterns for a profile
    /// </summary>
    public async Task<(List<SuccessPattern> SuccessPatterns, List<FailurePattern> FailurePatterns)> DetectPatternsAsync(
        string profileId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Detecting patterns for profile {ProfileId}", profileId);

        // Load all videos for the profile
        var videos = await _persistence.LoadAllVideosAsync(profileId, ct).ConfigureAwait(false);
        if (videos.Count < MIN_OCCURRENCES)
        {
            _logger.LogInformation("Not enough videos ({Count}) to detect patterns", videos.Count);
            return (new List<SuccessPattern>(), new List<FailurePattern>());
        }

        // Load all links to get project correlations
        var links = await _persistence.LoadLinksAsync(profileId, ct).ConfigureAwait(false);

        // Group videos by performance outcome
        var highPerformers = new List<VideoPerformanceData>();
        var lowPerformers = new List<VideoPerformanceData>();

        foreach (var video in videos)
        {
            var score = CalculatePerformanceScore(video);
            if (score >= 70)
            {
                highPerformers.Add(video);
            }
            else if (score <= 30)
            {
                lowPerformers.Add(video);
            }
        }

        // Detect success patterns
        var successPatterns = new List<SuccessPattern>();
        if (highPerformers.Count >= MIN_OCCURRENCES)
        {
            successPatterns.AddRange(DetectSuccessPatterns(profileId, highPerformers, links));
        }

        // Detect failure patterns
        var failurePatterns = new List<FailurePattern>();
        if (lowPerformers.Count >= MIN_OCCURRENCES)
        {
            failurePatterns.AddRange(DetectFailurePatterns(profileId, lowPerformers, links));
        }

        // Save patterns
        await _persistence.SaveSuccessPatternsAsync(profileId, successPatterns, ct).ConfigureAwait(false);
        await _persistence.SaveFailurePatternsAsync(profileId, failurePatterns, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Detected {SuccessCount} success patterns and {FailureCount} failure patterns",
            successPatterns.Count, failurePatterns.Count);

        return (successPatterns, failurePatterns);
    }

    /// <summary>
    /// Detect success patterns from high-performing videos
    /// </summary>
    private List<SuccessPattern> DetectSuccessPatterns(
        string profileId,
        List<VideoPerformanceData> highPerformers,
        List<VideoProjectLink> links)
    {
        var patterns = new List<SuccessPattern>();

        // Pattern: High engagement rate
        var highEngagementVideos = highPerformers
            .Where(v => v.Metrics.Engagement.EngagementRate > 0.05)
            .ToList();

        if (highEngagementVideos.Count >= MIN_OCCURRENCES)
        {
            var avgEngagement = highEngagementVideos.Average(v => v.Metrics.Engagement.EngagementRate);
            var avgViews = highEngagementVideos.Average(v => v.Metrics.Views);

            patterns.Add(new SuccessPattern(
                PatternId: Guid.NewGuid().ToString(),
                ProfileId: profileId,
                PatternType: "high_engagement",
                Description: $"Videos with high engagement rate (>{avgEngagement:P2}) consistently perform well",
                Strength: CalculatePatternStrength(highEngagementVideos.Count, highPerformers.Count),
                Occurrences: highEngagementVideos.Count,
                Impact: new PerformanceImpact(
                    ViewsImpact: (avgViews / highPerformers.Average(v => v.Metrics.Views) - 1) * 100,
                    EngagementImpact: 0,
                    RetentionImpact: null,
                    OverallImpact: 25
                ),
                FirstObserved: highEngagementVideos.Min(v => v.PublishedAt),
                LastObserved: highEngagementVideos.Max(v => v.PublishedAt),
                PatternData: new Dictionary<string, object>
                {
                    { "avg_engagement_rate", avgEngagement },
                    { "video_count", highEngagementVideos.Count }
                }
            ));
        }

        // Pattern: Good retention
        var goodRetentionVideos = highPerformers
            .Where(v => v.Metrics.AverageViewPercentage.HasValue && v.Metrics.AverageViewPercentage.Value > 0.5)
            .ToList();

        if (goodRetentionVideos.Count >= MIN_OCCURRENCES)
        {
            var avgRetention = goodRetentionVideos.Average(v => v.Metrics.AverageViewPercentage!.Value);

            patterns.Add(new SuccessPattern(
                PatternId: Guid.NewGuid().ToString(),
                ProfileId: profileId,
                PatternType: "high_retention",
                Description: $"Videos with high retention (>{avgRetention:P0}) drive better performance",
                Strength: CalculatePatternStrength(goodRetentionVideos.Count, highPerformers.Count),
                Occurrences: goodRetentionVideos.Count,
                Impact: new PerformanceImpact(
                    ViewsImpact: 0,
                    EngagementImpact: null,
                    RetentionImpact: (avgRetention - 0.5) * 100,
                    OverallImpact: 30
                ),
                FirstObserved: goodRetentionVideos.Min(v => v.PublishedAt),
                LastObserved: goodRetentionVideos.Max(v => v.PublishedAt),
                PatternData: new Dictionary<string, object>
                {
                    { "avg_retention", avgRetention },
                    { "video_count", goodRetentionVideos.Count }
                }
            ));
        }

        // Pattern: Platform-specific success
        var platformGroups = highPerformers.GroupBy(v => v.Platform);
        foreach (var group in platformGroups)
        {
            if (group.Count() >= MIN_OCCURRENCES && 
                (double)group.Count() / highPerformers.Count >= 0.6)
            {
                patterns.Add(new SuccessPattern(
                    PatternId: Guid.NewGuid().ToString(),
                    ProfileId: profileId,
                    PatternType: "platform_strength",
                    Description: $"Content performs exceptionally well on {group.Key}",
                    Strength: (double)group.Count() / highPerformers.Count,
                    Occurrences: group.Count(),
                    Impact: new PerformanceImpact(
                        ViewsImpact: 20,
                        EngagementImpact: 15,
                        RetentionImpact: null,
                        OverallImpact: 20
                    ),
                    FirstObserved: group.Min(v => v.PublishedAt),
                    LastObserved: group.Max(v => v.PublishedAt),
                    PatternData: new Dictionary<string, object>
                    {
                        { "platform", group.Key },
                        { "video_count", group.Count() }
                    }
                ));
            }
        }

        return patterns;
    }

    /// <summary>
    /// Detect failure patterns from low-performing videos
    /// </summary>
    private List<FailurePattern> DetectFailurePatterns(
        string profileId,
        List<VideoPerformanceData> lowPerformers,
        List<VideoProjectLink> links)
    {
        var patterns = new List<FailurePattern>();

        // Pattern: Low engagement rate
        var lowEngagementVideos = lowPerformers
            .Where(v => v.Metrics.Engagement.EngagementRate < 0.01)
            .ToList();

        if (lowEngagementVideos.Count >= MIN_OCCURRENCES)
        {
            var avgEngagement = lowEngagementVideos.Average(v => v.Metrics.Engagement.EngagementRate);

            patterns.Add(new FailurePattern(
                PatternId: Guid.NewGuid().ToString(),
                ProfileId: profileId,
                PatternType: "low_engagement",
                Description: $"Videos with low engagement rate (<{avgEngagement:P2}) struggle to gain traction",
                Strength: CalculatePatternStrength(lowEngagementVideos.Count, lowPerformers.Count),
                Occurrences: lowEngagementVideos.Count,
                Impact: new PerformanceImpact(
                    ViewsImpact: -30,
                    EngagementImpact: -50,
                    RetentionImpact: null,
                    OverallImpact: -35
                ),
                FirstObserved: lowEngagementVideos.Min(v => v.PublishedAt),
                LastObserved: lowEngagementVideos.Max(v => v.PublishedAt),
                PatternData: new Dictionary<string, object>
                {
                    { "avg_engagement_rate", avgEngagement },
                    { "video_count", lowEngagementVideos.Count }
                }
            ));
        }

        // Pattern: Poor retention
        var poorRetentionVideos = lowPerformers
            .Where(v => v.Metrics.AverageViewPercentage.HasValue && v.Metrics.AverageViewPercentage.Value < 0.3)
            .ToList();

        if (poorRetentionVideos.Count >= MIN_OCCURRENCES)
        {
            var avgRetention = poorRetentionVideos.Average(v => v.Metrics.AverageViewPercentage!.Value);

            patterns.Add(new FailurePattern(
                PatternId: Guid.NewGuid().ToString(),
                ProfileId: profileId,
                PatternType: "poor_retention",
                Description: $"Videos with poor retention (<{avgRetention:P0}) fail to keep audience engaged",
                Strength: CalculatePatternStrength(poorRetentionVideos.Count, lowPerformers.Count),
                Occurrences: poorRetentionVideos.Count,
                Impact: new PerformanceImpact(
                    ViewsImpact: -20,
                    EngagementImpact: null,
                    RetentionImpact: -40,
                    OverallImpact: -30
                ),
                FirstObserved: poorRetentionVideos.Min(v => v.PublishedAt),
                LastObserved: poorRetentionVideos.Max(v => v.PublishedAt),
                PatternData: new Dictionary<string, object>
                {
                    { "avg_retention", avgRetention },
                    { "video_count", poorRetentionVideos.Count }
                }
            ));
        }

        return patterns;
    }

    /// <summary>
    /// Calculate performance score for a video
    /// </summary>
    private double CalculatePerformanceScore(VideoPerformanceData video)
    {
        var scores = new List<double>();

        // View score (normalized to 0-100)
        var viewScore = Math.Min(100, (video.Metrics.Views / 1000.0) * 10);
        scores.Add(viewScore);

        // Engagement score
        var engagementScore = Math.Min(100, video.Metrics.Engagement.EngagementRate * 10000);
        scores.Add(engagementScore);

        // Retention score (if available)
        if (video.Metrics.AverageViewPercentage.HasValue)
        {
            scores.Add(video.Metrics.AverageViewPercentage.Value * 100);
        }

        return scores.Average();
    }

    /// <summary>
    /// Calculate pattern strength based on occurrence rate
    /// </summary>
    private double CalculatePatternStrength(int occurrences, int total)
    {
        if (total == 0) return 0;
        return (double)occurrences / total;
    }
}
