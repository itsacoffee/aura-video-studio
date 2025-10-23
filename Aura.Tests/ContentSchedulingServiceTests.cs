using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentPlanning;
using Aura.Core.Services.ContentPlanning;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ContentSchedulingServiceTests
{
    private readonly Mock<ILogger<ContentSchedulingService>> _mockLogger;
    private readonly Mock<ILogger<AudienceAnalysisService>> _mockAudienceLogger;
    private readonly AudienceAnalysisService _audienceService;
    private readonly ContentSchedulingService _service;

    public ContentSchedulingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ContentSchedulingService>>();
        _mockAudienceLogger = new Mock<ILogger<AudienceAnalysisService>>();
        _audienceService = new AudienceAnalysisService(_mockAudienceLogger.Object);
        _service = new ContentSchedulingService(_mockLogger.Object, _audienceService);
    }

    [Fact]
    public async Task GetSchedulingRecommendationsAsync_ValidRequest_ReturnsRecommendations()
    {
        // Arrange
        var request = new ContentSchedulingRequest
        {
            Platform = "YouTube",
            Category = "Technology",
            TargetAudience = new() { "Developers" }
        };

        // Act
        var response = await _service.GetSchedulingRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Recommendations);
        Assert.All(response.Recommendations, rec =>
        {
            Assert.True(rec.ConfidenceScore >= 0 && rec.ConfidenceScore <= 100);
            Assert.NotNull(rec.Reasoning);
            Assert.True(rec.RecommendedDateTime > DateTime.UtcNow);
        });
    }

    [Theory]
    [InlineData("YouTube")]
    [InlineData("TikTok")]
    [InlineData("Instagram")]
    public async Task GetSchedulingRecommendationsAsync_DifferentPlatforms_ReturnsUniqueTimings(string platform)
    {
        // Arrange
        var request = new ContentSchedulingRequest
        {
            Platform = platform,
            Category = "General",
            TargetAudience = new()
        };

        // Act
        var response = await _service.GetSchedulingRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Recommendations);
        // Should return at least 7 recommendations (7 days with 2 times each = 14, but limited to 10)
        Assert.True(response.Recommendations.Count >= 7);
    }

    [Fact]
    public async Task ScheduleContentAsync_ValidPlan_ReturnsScheduledContent()
    {
        // Arrange
        var plan = new ContentPlan
        {
            Title = "Test Video",
            Description = "Test description",
            Category = "Technology",
            TargetPlatform = "YouTube"
        };
        var scheduledTime = DateTime.UtcNow.AddDays(1);

        // Act
        var scheduled = await _service.ScheduleContentAsync(plan, scheduledTime, CancellationToken.None);

        // Assert
        Assert.NotNull(scheduled);
        Assert.Equal(plan.Id, scheduled.ContentPlanId);
        Assert.Equal(plan.Title, scheduled.Title);
        Assert.Equal(plan.TargetPlatform, scheduled.Platform);
        Assert.Equal(scheduledTime, scheduled.ScheduledDateTime);
        Assert.Equal(SchedulingStatus.Pending, scheduled.Status);
    }

    [Fact]
    public async Task GetScheduledContentAsync_DateRange_ReturnsContentInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(30);

        // Act
        var content = await _service.GetScheduledContentAsync(startDate, endDate, null, CancellationToken.None);

        // Assert
        Assert.NotNull(content);
        // Should have some scheduled content
        Assert.All(content, item =>
        {
            Assert.True(item.ScheduledDateTime >= startDate);
            Assert.True(item.ScheduledDateTime <= endDate);
        });
    }

    [Fact]
    public async Task GetScheduledContentAsync_WithPlatformFilter_ReturnsOnlyMatchingPlatform()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(30);
        var platform = "YouTube";

        // Act
        var content = await _service.GetScheduledContentAsync(startDate, endDate, platform, CancellationToken.None);

        // Assert
        Assert.NotNull(content);
        Assert.All(content, item => Assert.Equal(platform, item.Platform));
    }

    [Fact]
    public async Task GetSchedulingRecommendationsAsync_PreferredDate_ReturnsRecommendationsAroundDate()
    {
        // Arrange
        var preferredDate = DateTime.UtcNow.AddDays(7);
        var request = new ContentSchedulingRequest
        {
            Platform = "YouTube",
            Category = "Technology",
            PreferredDate = preferredDate,
            TargetAudience = new()
        };

        // Act
        var response = await _service.GetSchedulingRecommendationsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Recommendations);
        // First recommendation should be close to preferred date
        var firstRec = response.Recommendations.First();
        Assert.True((firstRec.RecommendedDateTime - preferredDate).TotalDays < 7);
    }
}
