using System;
using System.Threading.Tasks;
using Aura.Core.Analytics.Retention;
using Aura.Core.Analytics.Platforms;
using Aura.Core.Analytics.Content;
using Aura.Core.Analytics.Recommendations;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class AnalyticsServicesTests
{
    private readonly ILogger<RetentionPredictor> _retentionLogger;
    private readonly ILogger<PlatformOptimizer> _platformLogger;
    private readonly ILogger<ContentAnalyzer> _contentLogger;
    private readonly ILogger<ImprovementEngine> _improvementLogger;

    public AnalyticsServicesTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _retentionLogger = loggerFactory.CreateLogger<RetentionPredictor>();
        _platformLogger = loggerFactory.CreateLogger<PlatformOptimizer>();
        _contentLogger = loggerFactory.CreateLogger<ContentAnalyzer>();
        _improvementLogger = loggerFactory.CreateLogger<ImprovementEngine>();
    }

    [Fact]
    public async Task RetentionPredictor_PredictRetention_ReturnsValidPrediction()
    {
        // Arrange
        var predictor = new RetentionPredictor(_retentionLogger);
        var content = "This is a test video script about programming tutorials. It covers various topics.";
        var contentType = "tutorial";
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var result = await predictor.PredictRetentionAsync(content, contentType, duration);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RetentionCurve);
        Assert.NotEmpty(result.RetentionCurve);
        Assert.True(result.PredictedAverageRetention >= 0 && result.PredictedAverageRetention <= 1);
        Assert.NotNull(result.Recommendations);
        Assert.True(result.OptimalLength > TimeSpan.Zero);
    }

    [Fact]
    public async Task RetentionPredictor_AnalyzeAttentionSpan_ReturnsValidAnalysis()
    {
        // Arrange
        var predictor = new RetentionPredictor(_retentionLogger);
        var content = "First segment about introduction. Second segment covers main content. Third segment wraps up.";
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var result = await predictor.AnalyzeAttentionSpanAsync(content, duration);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SegmentScores);
        Assert.NotEmpty(result.SegmentScores);
        Assert.True(result.AverageEngagement >= 0 && result.AverageEngagement <= 1);
        Assert.NotNull(result.Suggestions);
    }

    [Fact]
    public async Task PlatformOptimizer_GetOptimization_ReturnsYouTubeOptimization()
    {
        // Arrange
        var optimizer = new PlatformOptimizer(_platformLogger);
        var content = "Tutorial about web development";
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var result = await optimizer.GetPlatformOptimizationAsync("youtube", content, duration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("youtube", result.Platform.ToLowerInvariant());
        Assert.Equal("16:9", result.RecommendedAspectRatio);
        Assert.NotNull(result.Recommendations);
        Assert.NotEmpty(result.Recommendations);
        Assert.NotNull(result.HashtagSuggestions);
        Assert.NotNull(result.MetadataGuidelines);
    }

    [Fact]
    public async Task PlatformOptimizer_GetOptimization_ReturnsTikTokOptimization()
    {
        // Arrange
        var optimizer = new PlatformOptimizer(_platformLogger);
        var content = "Quick tips for social media success";
        var duration = TimeSpan.FromSeconds(60);

        // Act
        var result = await optimizer.GetPlatformOptimizationAsync("tiktok", content, duration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tiktok", result.Platform.ToLowerInvariant());
        Assert.Equal("9:16", result.RecommendedAspectRatio);
        Assert.True(result.OptimalDuration <= TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task PlatformOptimizer_SuggestAspectRatios_ReturnsMultiplePlatforms()
    {
        // Arrange
        var optimizer = new PlatformOptimizer(_platformLogger);
        var platforms = new System.Collections.Generic.List<string> { "youtube", "tiktok", "instagram" };

        // Act
        var result = await optimizer.SuggestAspectRatioAdaptationsAsync(platforms);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Suggestions);
        Assert.Equal(3, result.Suggestions.Count);
        Assert.NotNull(result.RecommendedPrimaryFormat);
        Assert.NotNull(result.AdaptationStrategy);
    }

    [Fact]
    public async Task ContentAnalyzer_AnalyzeStructure_ReturnsValidAnalysis()
    {
        // Arrange
        var analyzer = new ContentAnalyzer(_contentLogger);
        var content = "Are you struggling with time management? Let me show you the top 5 tips that transformed my productivity!";
        var contentType = "educational";

        // Act
        var result = await analyzer.AnalyzeContentStructureAsync(content, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HookQuality >= 0 && result.HookQuality <= 1);
        Assert.True(result.PacingScore >= 0 && result.PacingScore <= 1);
        Assert.True(result.StructuralStrength >= 0 && result.StructuralStrength <= 1);
        Assert.True(result.OverallScore >= 0 && result.OverallScore <= 1);
        Assert.NotNull(result.HookSuggestions);
    }

    [Fact]
    public async Task ContentAnalyzer_GetRecommendations_ReturnsActionableRecommendations()
    {
        // Arrange
        var analyzer = new ContentAnalyzer(_contentLogger);
        var content = "This is a basic video script without much engagement.";
        var targetAudience = "young professionals";

        // Act
        var result = await analyzer.GetContentRecommendationsAsync(content, targetAudience);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetAudience, result.TargetAudience);
        Assert.NotNull(result.Recommendations);
        Assert.True(result.EstimatedImprovementScore >= 0);
    }

    [Fact]
    public async Task ContentAnalyzer_CompareWithPatterns_ReturnsComparativeAnalysis()
    {
        // Arrange
        var analyzer = new ContentAnalyzer(_contentLogger);
        var content = "What if I told you there's a better way? First, let's look at the problem. Next, I'll show you the solution.";
        var category = "tutorial";

        // Act
        var result = await analyzer.CompareWithSuccessfulPatternsAsync(content, category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category, result.Category);
        Assert.NotNull(result.MatchedPatterns);
        Assert.NotNull(result.MissingPatterns);
        Assert.True(result.AlignmentScore >= 0 && result.AlignmentScore <= 1);
        Assert.NotNull(result.TopSuggestions);
    }

    [Fact]
    public async Task ImprovementEngine_GenerateRoadmap_ReturnsValidRoadmap()
    {
        // Arrange
        var retentionPredictor = new RetentionPredictor(_retentionLogger);
        var platformOptimizer = new PlatformOptimizer(_platformLogger);
        var contentAnalyzer = new ContentAnalyzer(_contentLogger);
        var engine = new ImprovementEngine(
            _improvementLogger,
            retentionPredictor,
            platformOptimizer,
            contentAnalyzer
        );

        var content = "Welcome to my channel! Today I'll show you how to code in Python.";
        var contentType = "tutorial";
        var duration = TimeSpan.FromMinutes(8);
        var platforms = new System.Collections.Generic.List<string> { "youtube", "tiktok" };

        // Act
        var result = await engine.GenerateImprovementRoadmapAsync(content, contentType, duration, platforms);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CurrentScore >= 0 && result.CurrentScore <= 1);
        Assert.True(result.PotentialScore >= 0 && result.PotentialScore <= 1);
        Assert.NotNull(result.PrioritizedActions);
        Assert.NotNull(result.QuickWins);
        Assert.True(result.EstimatedTimeToImprove > TimeSpan.Zero);
    }

    [Fact]
    public async Task ImprovementEngine_GetRealTimeFeedback_ReturnsValidFeedback()
    {
        // Arrange
        var retentionPredictor = new RetentionPredictor(_retentionLogger);
        var platformOptimizer = new PlatformOptimizer(_platformLogger);
        var contentAnalyzer = new ContentAnalyzer(_contentLogger);
        var engine = new ImprovementEngine(
            _improvementLogger,
            retentionPredictor,
            platformOptimizer,
            contentAnalyzer
        );

        var currentContent = "This is a work in progress script.";
        var wordCount = 6;
        var currentDuration = TimeSpan.FromMinutes(2);

        // Act
        var result = await engine.GetRealTimeFeedbackAsync(currentContent, wordCount, currentDuration);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Issues);
        Assert.NotNull(result.Strengths);
        Assert.True(result.CurrentQualityScore >= 0 && result.CurrentQualityScore <= 1);
        Assert.NotNull(result.Suggestions);
    }

    [Theory]
    [InlineData("tutorial", 8)]
    [InlineData("entertainment", 10)]
    [InlineData("educational", 12)]
    [InlineData("short", 1)]
    public async Task RetentionPredictor_OptimalLength_VariesByContentType(string contentType, int expectedMinutes)
    {
        // Arrange
        var predictor = new RetentionPredictor(_retentionLogger);
        var content = "Sample content for testing optimal length calculation";
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var result = await predictor.PredictRetentionAsync(content, contentType, duration);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), result.OptimalLength);
    }
}
