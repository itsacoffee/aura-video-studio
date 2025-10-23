using System;
using System.Threading.Tasks;
using Aura.Api.Services.Dashboard;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Dashboard;

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
    public async Task GetHistoricalTrendsAsync_ReturnsHistoricalData()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetHistoricalTrendsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.NotEmpty(result.DataPoints);
        Assert.True(result.DataPoints.Count > 0);
    }

    [Fact]
    public async Task GetHistoricalTrendsAsync_CalculatesTrendDirection()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetHistoricalTrendsAsync(startDate, endDate, "daily");

        // Assert
        Assert.NotNull(result.TrendDirection);
        Assert.Contains(result.TrendDirection, new[] { "improving", "declining", "stable" });
    }

    [Fact]
    public async Task GetHistoricalTrendsAsync_DataPointsHaveCorrectStructure()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetHistoricalTrendsAsync(startDate, endDate);

        // Assert
        foreach (var point in result.DataPoints)
        {
            Assert.True(point.QualityScore >= 0);
            Assert.True(point.ProcessedVideos >= 0);
            Assert.True(point.ErrorCount >= 0);
            Assert.True(point.AverageProcessingTime > TimeSpan.Zero);
        }
    }
}
