using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.EditingIntelligence;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class EditingIntelligenceTests
{
    private readonly ILogger<CutPointDetectionService> _cutPointLogger;
    private readonly ILogger<PacingOptimizationService> _pacingLogger;
    private readonly ILogger<TransitionRecommendationService> _transitionLogger;
    private readonly ILogger<EngagementOptimizationService> _engagementLogger;
    private readonly ILogger<QualityControlService> _qualityLogger;

    public EditingIntelligenceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _cutPointLogger = loggerFactory.CreateLogger<CutPointDetectionService>();
        _pacingLogger = loggerFactory.CreateLogger<PacingOptimizationService>();
        _transitionLogger = loggerFactory.CreateLogger<TransitionRecommendationService>();
        _engagementLogger = loggerFactory.CreateLogger<EngagementOptimizationService>();
        _qualityLogger = loggerFactory.CreateLogger<QualityControlService>();
    }

    private EditableTimeline CreateTestTimeline()
    {
        var timeline = new EditableTimeline();
        
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Introduction",
            Script: "Welcome to this tutorial. Today we'll learn about video editing. It's an exciting topic.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(10)
        ));

        timeline.AddScene(new TimelineScene(
            Index: 1,
            Heading: "Main Content",
            Script: "Let's dive into the details. First, we need to understand the basics. This is important to remember.",
            Start: TimeSpan.FromSeconds(10),
            Duration: TimeSpan.FromSeconds(15)
        ));

        timeline.AddScene(new TimelineScene(
            Index: 2,
            Heading: "Conclusion",
            Script: "In conclusion, remember to subscribe and like this video. Thank you for watching!",
            Start: TimeSpan.FromSeconds(25),
            Duration: TimeSpan.FromSeconds(8)
        ));

        return timeline;
    }

    [Fact]
    public async Task CutPointDetection_DetectsCutPoints()
    {
        // Arrange
        var service = new CutPointDetectionService(_cutPointLogger);
        var timeline = CreateTestTimeline();

        // Act
        var cutPoints = await service.DetectCutPointsAsync(timeline);

        // Assert
        Assert.NotNull(cutPoints);
        Assert.NotEmpty(cutPoints);
        Assert.All(cutPoints, cp =>
        {
            Assert.InRange(cp.Confidence, 0, 1);
            Assert.NotEmpty(cp.Reasoning);
        });
    }

    [Fact]
    public async Task CutPointDetection_DetectsAwkwardPauses()
    {
        // Arrange
        var service = new CutPointDetectionService(_cutPointLogger);
        var timeline = new EditableTimeline();
        
        // Add scenes with a large gap
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Scene 1",
            Script: "First scene content.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        ));

        timeline.AddScene(new TimelineScene(
            Index: 1,
            Heading: "Scene 2",
            Script: "Second scene content.",
            Start: TimeSpan.FromSeconds(8), // 3 second gap
            Duration: TimeSpan.FromSeconds(5)
        ));

        // Act
        var awkwardPauses = await service.DetectAwkwardPausesAsync(timeline);

        // Assert
        Assert.NotNull(awkwardPauses);
        Assert.NotEmpty(awkwardPauses);
    }

    [Fact]
    public async Task PacingOptimization_AnalyzesPacing()
    {
        // Arrange
        var service = new PacingOptimizationService(_pacingLogger);
        var timeline = CreateTestTimeline();

        // Act
        var analysis = await service.AnalyzePacingAsync(timeline);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.SceneRecommendations);
        Assert.InRange(analysis.OverallEngagementScore, 0, 1);
        Assert.NotNull(analysis.Summary);
        Assert.True(analysis.ContentDensity > 0);
    }

    [Fact]
    public async Task PacingOptimization_DetectsSlowSegments()
    {
        // Arrange
        var service = new PacingOptimizationService(_pacingLogger);
        var timeline = new EditableTimeline();
        
        // Add a very slow scene (very few words for long duration)
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Slow Scene",
            Script: "Um... well... this is slow.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(30) // Very slow
        ));

        // Act
        var slowSegments = await service.DetectSlowSegmentsAsync(timeline);

        // Assert
        Assert.NotNull(slowSegments);
        Assert.NotEmpty(slowSegments);
    }

    [Fact]
    public async Task PacingOptimization_OptimizesForDuration()
    {
        // Arrange
        var service = new PacingOptimizationService(_pacingLogger);
        var timeline = CreateTestTimeline();
        var targetDuration = TimeSpan.FromSeconds(20);

        // Act
        var optimized = await service.OptimizeForDurationAsync(timeline, targetDuration);

        // Assert
        Assert.NotNull(optimized);
        Assert.True(Math.Abs((optimized.TotalDuration - targetDuration).TotalSeconds) < 1);
    }

    [Fact]
    public async Task TransitionRecommendation_RecommendsTransitions()
    {
        // Arrange
        var service = new TransitionRecommendationService(_transitionLogger);
        var timeline = CreateTestTimeline();

        // Act
        var suggestions = await service.RecommendTransitionsAsync(timeline);

        // Assert
        Assert.NotNull(suggestions);
        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, s =>
        {
            Assert.InRange(s.Confidence, 0, 1);
            Assert.NotEmpty(s.Reasoning);
            Assert.True(s.Duration >= TimeSpan.Zero);
        });
    }

    [Fact]
    public async Task TransitionRecommendation_EnforcesVariety()
    {
        // Arrange
        var service = new TransitionRecommendationService(_transitionLogger);
        var timeline = CreateTestTimeline();
        var initialSuggestions = await service.RecommendTransitionsAsync(timeline);

        // Act
        var varied = await service.EnforceTransitionVarietyAsync(timeline, initialSuggestions);

        // Assert
        Assert.NotNull(varied);
        Assert.NotEmpty(varied);
    }

    [Fact]
    public async Task EngagementOptimization_GeneratesEngagementCurve()
    {
        // Arrange
        var service = new EngagementOptimizationService(_engagementLogger);
        var timeline = CreateTestTimeline();

        // Act
        var curve = await service.GenerateEngagementCurveAsync(timeline);

        // Assert
        Assert.NotNull(curve);
        Assert.NotEmpty(curve.Points);
        Assert.InRange(curve.AverageEngagement, 0, 1);
        Assert.InRange(curve.HookStrength, 0, 1);
        Assert.InRange(curve.EndingImpact, 0, 1);
        Assert.NotNull(curve.BoosterSuggestions);
    }

    [Fact]
    public async Task EngagementOptimization_DetectsFatiguePoints()
    {
        // Arrange
        var service = new EngagementOptimizationService(_engagementLogger);
        var timeline = new EditableTimeline();
        
        // Add a very long scene
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Long Scene",
            Script: "This is a very long scene with lots of content that goes on for a while to test fatigue detection.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(60) // Long enough to trigger fatigue
        ));

        // Act
        var fatiguePoints = await service.DetectFatiguePointsAsync(timeline);

        // Assert
        Assert.NotNull(fatiguePoints);
        Assert.NotEmpty(fatiguePoints);
    }

    [Fact]
    public async Task QualityControl_DetectsMissingAssets()
    {
        // Arrange
        var service = new QualityControlService(_qualityLogger);
        var timeline = new EditableTimeline();
        
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Scene with missing audio",
            Script: "Test scene",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5),
            NarrationAudioPath: "/nonexistent/path/audio.wav"
        ));

        // Act
        var issues = await service.RunQualityChecksAsync(timeline);

        // Assert
        Assert.NotNull(issues);
        Assert.Contains(issues, i => i.Type == QualityIssueType.MissingAsset);
    }

    [Fact]
    public async Task QualityControl_DetectsGaps()
    {
        // Arrange
        var service = new QualityControlService(_qualityLogger);
        var timeline = new EditableTimeline();
        
        // Add scenes with a gap
        timeline.AddScene(new TimelineScene(
            Index: 0,
            Heading: "Scene 1",
            Script: "First scene",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5)
        ));

        timeline.AddScene(new TimelineScene(
            Index: 1,
            Heading: "Scene 2",
            Script: "Second scene",
            Start: TimeSpan.FromSeconds(6), // 1 second gap
            Duration: TimeSpan.FromSeconds(5)
        ));

        // Act
        var issues = await service.RunQualityChecksAsync(timeline);

        // Assert
        Assert.NotNull(issues);
        Assert.Contains(issues, i => i.Type == QualityIssueType.BlackFrame);
    }
}
