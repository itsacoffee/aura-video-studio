using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Service for analyzing and optimizing video pacing
/// </summary>
public class PacingOptimizationService
{
    private readonly ILogger<PacingOptimizationService> _logger;

    public PacingOptimizationService(ILogger<PacingOptimizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze timeline pacing and provide recommendations
    /// </summary>
    public async Task<PacingAnalysis> AnalyzePacingAsync(EditableTimeline timeline, TimeSpan? targetDuration = null)
    {
        _logger.LogInformation("Analyzing timeline pacing");
        
        var recommendations = new List<ScenePacingRecommendation>();
        var slowSegments = new List<TimeSpan>();
        var fastSegments = new List<TimeSpan>();

        // Analyze each scene
        foreach (var scene in timeline.Scenes)
        {
            var analysis = AnalyzeScene(scene);
            recommendations.Add(analysis);

            if (analysis.IssueType == PacingIssueType.TooSlow)
                slowSegments.Add(scene.Start);
            else if (analysis.IssueType == PacingIssueType.TooFast)
                fastSegments.Add(scene.Start);
        }

        // Calculate overall metrics
        var overallEngagement = recommendations.Average(r => r.EngagementScore);
        var contentDensity = CalculateContentDensity(timeline);
        var summary = GenerateSummary(recommendations, overallEngagement, contentDensity);

        await Task.CompletedTask.ConfigureAwait(false);
        return new PacingAnalysis(
            SceneRecommendations: recommendations,
            OverallEngagementScore: overallEngagement,
            SlowSegments: slowSegments,
            FastSegments: fastSegments,
            ContentDensity: contentDensity,
            Summary: summary
        );
    }

    private ScenePacingRecommendation AnalyzeScene(TimelineScene scene)
    {
        var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var duration = scene.Duration.TotalSeconds;
        var wordsPerSecond = wordCount / duration;

        // Ideal range: 2-3 words per second
        var engagementScore = CalculateEngagementScore(wordsPerSecond, duration);
        var issueType = DetermineIssueType(wordsPerSecond, duration);
        var recommendedDuration = CalculateRecommendedDuration(wordCount, wordsPerSecond, duration);
        var reasoning = GenerateReasoning(wordsPerSecond, duration, wordCount, issueType);

        return new ScenePacingRecommendation(
            SceneIndex: scene.Index,
            CurrentDuration: scene.Duration,
            RecommendedDuration: recommendedDuration,
            EngagementScore: engagementScore,
            IssueType: issueType,
            Reasoning: reasoning
        );
    }

    private double CalculateEngagementScore(double wordsPerSecond, double duration)
    {
        // Ideal words per second: 2.5
        var pacingScore = Math.Max(0, 1 - Math.Abs(wordsPerSecond - 2.5) / 2.5);

        // Penalize very long or very short scenes
        var durationScore = duration switch
        {
            < 3 => 0.6,
            < 5 => 0.8,
            < 15 => 1.0,
            < 30 => 0.9,
            < 60 => 0.7,
            _ => 0.5
        };

        return (pacingScore * 0.7 + durationScore * 0.3);
    }

    private PacingIssueType? DetermineIssueType(double wordsPerSecond, double duration)
    {
        if (wordsPerSecond < 1.5)
            return PacingIssueType.TooSlow;
        
        if (wordsPerSecond > 4.0)
            return PacingIssueType.TooFast;
        
        if (duration > 45)
            return PacingIssueType.AttentionSpanExceeded;

        return null;
    }

    private TimeSpan CalculateRecommendedDuration(int wordCount, double wordsPerSecond, double currentDuration)
    {
        // Target 2.5 words per second
        const double targetWps = 2.5;
        
        if (wordsPerSecond < 1.5)
        {
            // Speed up by removing pauses
            return TimeSpan.FromSeconds(wordCount / targetWps);
        }
        else if (wordsPerSecond > 4.0)
        {
            // Slow down or split into multiple scenes
            return TimeSpan.FromSeconds(wordCount / targetWps);
        }

        return TimeSpan.FromSeconds(currentDuration);
    }

    private string GenerateReasoning(double wps, double duration, int wordCount, PacingIssueType? issue)
    {
        if (issue == null)
            return $"Good pacing at {wps:F1} words/sec. Scene maintains viewer engagement.";

        return issue switch
        {
            PacingIssueType.TooSlow => $"Pacing is slow at {wps:F1} words/sec (target: 2.5). Consider tightening cuts or reducing pauses.",
            PacingIssueType.TooFast => $"Pacing is fast at {wps:F1} words/sec (target: 2.5). Consider adding breathing room or splitting scene.",
            PacingIssueType.AttentionSpanExceeded => $"Scene duration ({duration:F0}s) exceeds recommended attention span. Consider splitting into multiple scenes.",
            _ => "Analysis complete."
        };
    }

    private double CalculateContentDensity(EditableTimeline timeline)
    {
        var totalWords = timeline.Scenes.Sum(s => s.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        var totalSeconds = timeline.TotalDuration.TotalSeconds;
        
        if (totalSeconds == 0)
            return 0;

        return totalWords / totalSeconds;
    }

    private string GenerateSummary(
        List<ScenePacingRecommendation> recommendations,
        double overallEngagement,
        double contentDensity)
    {
        var issueCount = recommendations.Count(r => r.IssueType.HasValue);
        var avgEngagement = overallEngagement * 100;

        if (issueCount == 0)
        {
            return $"Excellent pacing! Overall engagement score: {avgEngagement:F0}%. Content density: {contentDensity:F1} words/sec.";
        }

        var issues = recommendations
            .Where(r => r.IssueType.HasValue)
            .GroupBy(r => r.IssueType)
            .Select(g => $"{g.Count()} {g.Key}")
            .ToList();

        return $"Found {issueCount} pacing issues: {string.Join(", ", issues)}. Overall engagement: {avgEngagement:F0}%. Consider adjustments for optimal viewer experience.";
    }

    /// <summary>
    /// Detect slow segments that could benefit from speed-up
    /// </summary>
    public async Task<IReadOnlyList<(TimeSpan Start, TimeSpan Duration, double RecommendedSpeed)>> DetectSlowSegmentsAsync(
        EditableTimeline timeline)
    {
        var slowSegments = new List<(TimeSpan, TimeSpan, double)>();

        foreach (var scene in timeline.Scenes)
        {
            var wordCount = scene.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var wordsPerSecond = wordCount / scene.Duration.TotalSeconds;

            if (wordsPerSecond < 1.5)
            {
                var recommendedSpeed = Math.Min(1.25, 2.5 / wordsPerSecond);
                slowSegments.Add((scene.Start, scene.Duration, recommendedSpeed));
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return slowSegments;
    }

    /// <summary>
    /// Optimize timeline to target duration
    /// </summary>
    public async Task<EditableTimeline> OptimizeForDurationAsync(
        EditableTimeline timeline,
        TimeSpan targetDuration,
        string strategy = "balanced")
    {
        _logger.LogInformation("Optimizing timeline to target duration: {Duration}", targetDuration);

        var currentDuration = timeline.TotalDuration;
        var scaleFactor = targetDuration.TotalSeconds / currentDuration.TotalSeconds;

        _logger.LogInformation("Scale factor: {Factor}", scaleFactor);

        // Clone timeline
        var optimized = new EditableTimeline
        {
            BackgroundMusicPath = timeline.BackgroundMusicPath,
            Subtitles = timeline.Subtitles
        };

        var currentTime = TimeSpan.Zero;
        foreach (var scene in timeline.Scenes)
        {
            var newDuration = TimeSpan.FromSeconds(scene.Duration.TotalSeconds * scaleFactor);
            
            optimized.AddScene(new TimelineScene(
                Index: scene.Index,
                Heading: scene.Heading,
                Script: scene.Script,
                Start: currentTime,
                Duration: newDuration,
                NarrationAudioPath: scene.NarrationAudioPath,
                VisualAssets: scene.VisualAssets,
                TransitionType: scene.TransitionType,
                TransitionDuration: scene.TransitionDuration
            ));

            currentTime += newDuration;
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return optimized;
    }
}
