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

public class TrendAnalysisServiceTests
{
    private readonly Mock<ILogger<TrendAnalysisService>> _mockLogger;
    private readonly TrendAnalysisService _service;

    public TrendAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<TrendAnalysisService>>();
        _service = new TrendAnalysisService(_mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_ValidRequest_ReturnsTrendsWithSummary()
    {
        // Arrange
        var request = new TrendAnalysisRequest
        {
            Category = "Technology",
            Platform = "YouTube",
            Keywords = new() { "AI", "Machine Learning", "Programming" }
        };

        // Act
        var response = await _service.AnalyzeTrendsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Trends);
        Assert.NotNull(response.Summary);
        Assert.All(response.Trends, trend =>
        {
            Assert.NotNull(trend.Topic);
            Assert.NotNull(trend.Platform);
            Assert.True(trend.TrendScore >= 0 && trend.TrendScore <= 100);
            Assert.NotEmpty(trend.DataPoints);
        });
    }

    [Fact]
    public async Task GetPlatformTrendsAsync_ValidPlatform_ReturnsTrendsForPlatform()
    {
        // Arrange
        var platform = "YouTube";
        var category = "Technology";

        // Act
        var trends = await _service.GetPlatformTrendsAsync(platform, category, CancellationToken.None);

        // Assert
        Assert.NotNull(trends);
        Assert.NotEmpty(trends);
        Assert.All(trends, trend => Assert.Equal(platform, trend.Platform));
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_EmptyKeywords_ReturnsValidResponse()
    {
        // Arrange
        var request = new TrendAnalysisRequest
        {
            Keywords = new()
        };

        // Act
        var response = await _service.AnalyzeTrendsAsync(request, CancellationToken.None);

        // Assert - Should still return trends (using defaults or platform-specific trends)
        Assert.NotNull(response);
    }

    [Theory]
    [InlineData("YouTube")]
    [InlineData("TikTok")]
    [InlineData("Instagram")]
    public async Task GetPlatformTrendsAsync_DifferentPlatforms_ReturnsUniqueTrends(string platform)
    {
        // Arrange & Act
        var trends = await _service.GetPlatformTrendsAsync(platform, null, CancellationToken.None);

        // Assert
        Assert.NotNull(trends);
        Assert.NotEmpty(trends);
        Assert.All(trends, trend =>
        {
            Assert.Equal(platform, trend.Platform);
            Assert.Contains(trend.Direction, new[] { TrendDirection.Rising, TrendDirection.Stable, TrendDirection.Declining });
        });
    }

    [Fact]
    public async Task AnalyzeTrendsAsync_GeneratesProperDataPoints()
    {
        // Arrange
        var request = new TrendAnalysisRequest
        {
            Keywords = new() { "Test Topic" }
        };

        // Act
        var response = await _service.AnalyzeTrendsAsync(request, CancellationToken.None);

        // Assert
        var firstTrend = response.Trends.First();
        Assert.NotEmpty(firstTrend.DataPoints);
        Assert.Equal(7, firstTrend.DataPoints.Count); // Should have 7 days of data
        Assert.All(firstTrend.DataPoints, dp =>
        {
            Assert.True(dp.Value >= 0);
            Assert.True(dp.Timestamp <= DateTime.UtcNow);
        });
    }
}
