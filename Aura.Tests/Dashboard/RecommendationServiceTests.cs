using System.Threading.Tasks;
using Aura.Api.Services.Dashboard;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Dashboard;

public class RecommendationServiceTests
{
    private readonly Mock<ILogger<RecommendationService>> _mockLogger;
    private readonly RecommendationService _service;

    public RecommendationServiceTests()
    {
        _mockLogger = new Mock<ILogger<RecommendationService>>();
        _service = new RecommendationService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsRecommendations()
    {
        // Arrange
        var metrics = new QualityMetrics
        {
            TotalVideosProcessed = 1000,
            AverageQualityScore = 85.0,
            SuccessRate = 95.0,
            AverageProcessingTime = System.TimeSpan.FromMinutes(20),
            TotalErrorsLast24h = 10,
            ComplianceRate = 90.0
        };

        // Act
        var result = await _service.GetRecommendationsAsync(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetRecommendationsAsync_RecommendationsAreOrderedByImpact()
    {
        // Arrange
        var metrics = new QualityMetrics
        {
            AverageQualityScore = 80.0,
            TotalErrorsLast24h = 15,
            ComplianceRate = 85.0
        };

        // Act
        var result = await _service.GetRecommendationsAsync(metrics);

        // Assert
        Assert.True(result.Count >= 2);
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].ImpactScore >= result[i + 1].ImpactScore);
        }
    }

    [Fact]
    public async Task GetRecommendationsAsync_IncludesHighPriorityForLowQuality()
    {
        // Arrange
        var metrics = new QualityMetrics
        {
            AverageQualityScore = 85.0, // Below 90
            TotalErrorsLast24h = 0
        };

        // Act
        var result = await _service.GetRecommendationsAsync(metrics);

        // Assert
        Assert.Contains(result, r => r.Priority == "high");
    }

    [Fact]
    public async Task GetRecommendationsAsync_IncludesPerformanceRecommendation()
    {
        // Arrange
        var metrics = new QualityMetrics
        {
            AverageProcessingTime = System.TimeSpan.FromMinutes(20), // Over 15 minutes
            AverageQualityScore = 95.0
        };

        // Act
        var result = await _service.GetRecommendationsAsync(metrics);

        // Assert
        Assert.Contains(result, r => r.Category == "performance");
    }
}
