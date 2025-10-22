using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PerformanceAnalytics;
using Aura.Core.Services.PerformanceAnalytics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class PerformanceAnalyticsTests
{
    private readonly string _testDirectory;

    public PerformanceAnalyticsTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task AnalyticsPersistence_SaveAndLoadVideo_Success()
    {
        // Arrange
        var persistence = new AnalyticsPersistence(
            NullLogger<AnalyticsPersistence>.Instance,
            _testDirectory
        );

        var video = new VideoPerformanceData(
            VideoId: "test-video-1",
            ProfileId: "test-profile",
            ProjectId: null,
            Platform: "YouTube",
            VideoTitle: "Test Video",
            VideoUrl: "https://youtube.com/watch?v=test",
            PublishedAt: DateTime.UtcNow,
            DataCollectedAt: DateTime.UtcNow,
            Metrics: new PerformanceMetrics(
                Views: 1000,
                WatchTimeMinutes: 500,
                AverageViewDuration: 30.0,
                AverageViewPercentage: 0.6,
                Engagement: new EngagementMetrics(
                    Likes: 100,
                    Dislikes: 5,
                    Comments: 20,
                    Shares: 10,
                    EngagementRate: 0.135
                ),
                ClickThroughRate: 0.05,
                Traffic: null,
                RetentionCurve: null,
                Devices: null
            ),
            Audience: null,
            RawData: null
        );

        // Act
        await persistence.SaveVideoPerformanceAsync(video);
        var loaded = await persistence.LoadVideoPerformanceAsync("test-profile", "test-video-1");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("test-video-1", loaded.VideoId);
        Assert.Equal("Test Video", loaded.VideoTitle);
        Assert.Equal(1000, loaded.Metrics.Views);
        Assert.Equal(0.135, loaded.Metrics.Engagement.EngagementRate);
    }

    [Fact]
    public async Task VideoProjectLinker_CreateManualLink_Success()
    {
        // Arrange
        var persistence = new AnalyticsPersistence(
            NullLogger<AnalyticsPersistence>.Instance,
            _testDirectory
        );

        var linker = new VideoProjectLinker(
            NullLogger<VideoProjectLinker>.Instance,
            persistence
        );

        // Act
        var link = await linker.CreateManualLinkAsync(
            "video-1",
            "project-1",
            "profile-1",
            "test-user"
        );

        // Assert
        Assert.NotNull(link);
        Assert.Equal("video-1", link.VideoId);
        Assert.Equal("project-1", link.ProjectId);
        Assert.Equal("manual", link.LinkType);
        Assert.Equal(1.0, link.ConfidenceScore);
        Assert.True(link.IsConfirmed);
    }

    [Fact]
    public async Task PerformancePatternDetector_DetectsSuccessPattern()
    {
        // Arrange
        var persistence = new AnalyticsPersistence(
            NullLogger<AnalyticsPersistence>.Instance,
            _testDirectory
        );

        var detector = new PerformancePatternDetector(
            NullLogger<PerformancePatternDetector>.Instance,
            persistence
        );

        // Create high-performing videos
        var highPerformer1 = CreateVideo("video-1", "profile-test", 10000, 0.08);
        var highPerformer2 = CreateVideo("video-2", "profile-test", 12000, 0.09);
        var highPerformer3 = CreateVideo("video-3", "profile-test", 11000, 0.085);

        await persistence.SaveVideoPerformanceAsync(highPerformer1);
        await persistence.SaveVideoPerformanceAsync(highPerformer2);
        await persistence.SaveVideoPerformanceAsync(highPerformer3);

        // Act
        var (successPatterns, failurePatterns) = await detector.DetectPatternsAsync("profile-test");

        // Assert
        Assert.NotEmpty(successPatterns);
        Assert.All(successPatterns, p => Assert.True(p.Strength > 0));
    }

    [Fact]
    public async Task PerformanceAnalyticsService_GetInsights_ReturnsData()
    {
        // Arrange
        var persistence = new AnalyticsPersistence(
            NullLogger<AnalyticsPersistence>.Instance,
            _testDirectory
        );

        var importer = new AnalyticsImporter(
            NullLogger<AnalyticsImporter>.Instance,
            persistence
        );

        var linker = new VideoProjectLinker(
            NullLogger<VideoProjectLinker>.Instance,
            persistence
        );

        var correlationAnalyzer = new CorrelationAnalyzer(
            NullLogger<CorrelationAnalyzer>.Instance,
            persistence,
            null! // ProfileService not needed for this test
        );

        var detector = new PerformancePatternDetector(
            NullLogger<PerformancePatternDetector>.Instance,
            persistence
        );

        var service = new PerformanceAnalyticsService(
            NullLogger<PerformanceAnalyticsService>.Instance,
            persistence,
            importer,
            linker,
            correlationAnalyzer,
            detector
        );

        // Add some test videos
        var video = CreateVideo("test-video", "test-profile", 5000, 0.05);
        await persistence.SaveVideoPerformanceAsync(video);

        // Act
        var insights = await service.GetInsightsAsync("test-profile");

        // Assert
        Assert.NotNull(insights);
        Assert.Equal("test-profile", insights.ProfileId);
        Assert.Equal(1, insights.TotalVideos);
        Assert.Equal(5000, insights.AverageViews);
    }

    [Fact]
    public async Task ABTest_CreateAndRetrieve_Success()
    {
        // Arrange
        var persistence = new AnalyticsPersistence(
            NullLogger<AnalyticsPersistence>.Instance,
            _testDirectory
        );

        var variants = new List<TestVariant>
        {
            new TestVariant(
                VariantId: "var-1",
                VariantName: "Variant A",
                Description: "Original",
                ProjectId: "proj-1",
                VideoId: null,
                Configuration: new Dictionary<string, object>(),
                CreatedAt: DateTime.UtcNow
            ),
            new TestVariant(
                VariantId: "var-2",
                VariantName: "Variant B",
                Description: "Alternative",
                ProjectId: "proj-2",
                VideoId: null,
                Configuration: new Dictionary<string, object>(),
                CreatedAt: DateTime.UtcNow
            )
        };

        var test = new ABTest(
            TestId: "test-1",
            ProfileId: "profile-1",
            TestName: "Title Test",
            Description: "Testing different titles",
            Category: "title",
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            Status: "draft",
            Variants: variants,
            Results: null
        );

        // Act
        await persistence.SaveABTestAsync(test);
        var loaded = await persistence.LoadABTestAsync("profile-1", "test-1");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("test-1", loaded.TestId);
        Assert.Equal("Title Test", loaded.TestName);
        Assert.Equal(2, loaded.Variants.Count);
    }

    private VideoPerformanceData CreateVideo(string videoId, string profileId, long views, double engagementRate)
    {
        var likes = (long)(views * engagementRate * 0.7);
        var comments = (long)(views * engagementRate * 0.2);
        var shares = (long)(views * engagementRate * 0.1);

        return new VideoPerformanceData(
            VideoId: videoId,
            ProfileId: profileId,
            ProjectId: null,
            Platform: "YouTube",
            VideoTitle: $"Video {videoId}",
            VideoUrl: $"https://youtube.com/watch?v={videoId}",
            PublishedAt: DateTime.UtcNow.AddDays(-7),
            DataCollectedAt: DateTime.UtcNow,
            Metrics: new PerformanceMetrics(
                Views: views,
                WatchTimeMinutes: views / 2,
                AverageViewDuration: 60.0,
                AverageViewPercentage: 0.5,
                Engagement: new EngagementMetrics(
                    Likes: likes,
                    Dislikes: 0,
                    Comments: comments,
                    Shares: shares,
                    EngagementRate: engagementRate
                ),
                ClickThroughRate: 0.05,
                Traffic: null,
                RetentionCurve: null,
                Devices: null
            ),
            Audience: null,
            RawData: null
        );
    }
}
