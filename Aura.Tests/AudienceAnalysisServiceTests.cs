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

public class AudienceAnalysisServiceTests
{
    private readonly Mock<ILogger<AudienceAnalysisService>> _mockLogger;
    private readonly AudienceAnalysisService _service;

    public AudienceAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<AudienceAnalysisService>>();
        _service = new AudienceAnalysisService(_mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeAudienceAsync_ValidRequest_ReturnsInsightsAndRecommendations()
    {
        // Arrange
        var request = new AudienceAnalysisRequest
        {
            Platform = "YouTube",
            Category = "Technology",
            ContentTags = new() { "AI", "Programming" }
        };

        // Act
        var response = await _service.AnalyzeAudienceAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Insights);
        Assert.NotEmpty(response.Recommendations);
        Assert.Equal(request.Platform, response.Insights.Platform);
        Assert.NotEmpty(response.Insights.TopInterests);
        Assert.NotEmpty(response.Insights.PreferredContentTypes);
        Assert.True(response.Insights.EngagementRate > 0);
    }

    [Theory]
    [InlineData("YouTube")]
    [InlineData("TikTok")]
    [InlineData("Instagram")]
    public async Task GetDemographicsAsync_DifferentPlatforms_ReturnsValidDemographics(string platform)
    {
        // Act
        var demographics = await _service.GetDemographicsAsync(platform, CancellationToken.None);

        // Assert
        Assert.NotNull(demographics);
        Assert.NotEmpty(demographics.AgeDistribution);
        Assert.NotEmpty(demographics.GenderDistribution);
        Assert.NotEmpty(demographics.LocationDistribution);
        
        // Check that distributions sum to approximately 1.0
        var ageSum = demographics.AgeDistribution.Values.Sum();
        var genderSum = demographics.GenderDistribution.Values.Sum();
        var locationSum = demographics.LocationDistribution.Values.Sum();
        
        Assert.True(Math.Abs(ageSum - 1.0) < 0.01);
        Assert.True(Math.Abs(genderSum - 1.0) < 0.01);
        Assert.True(Math.Abs(locationSum - 1.0) < 0.01);
    }

    [Theory]
    [InlineData("Technology")]
    [InlineData("Gaming")]
    [InlineData("Fitness")]
    public async Task GetTopInterestsAsync_DifferentCategories_ReturnsRelevantInterests(string category)
    {
        // Act
        var interests = await _service.GetTopInterestsAsync(category, CancellationToken.None);

        // Assert
        Assert.NotNull(interests);
        Assert.NotEmpty(interests);
        Assert.True(interests.Count >= 3); // Should return at least 3 interests
    }

    [Fact]
    public async Task AnalyzeAudienceAsync_GeneratesActionableRecommendations()
    {
        // Arrange
        var request = new AudienceAnalysisRequest
        {
            Platform = "YouTube",
            Category = "Education",
            ContentTags = new()
        };

        // Act
        var response = await _service.AnalyzeAudienceAsync(request, CancellationToken.None);

        // Assert
        Assert.NotEmpty(response.Recommendations);
        Assert.True(response.Recommendations.Count >= 3); // Should provide multiple recommendations
        Assert.All(response.Recommendations, rec => Assert.NotEmpty(rec));
    }

    [Fact]
    public async Task GetDemographicsAsync_UnknownPlatform_ReturnsGenericDemographics()
    {
        // Arrange
        var platform = "UnknownPlatform";

        // Act
        var demographics = await _service.GetDemographicsAsync(platform, CancellationToken.None);

        // Assert
        Assert.NotNull(demographics);
        Assert.NotEmpty(demographics.AgeDistribution);
        Assert.NotEmpty(demographics.GenderDistribution);
        Assert.NotEmpty(demographics.LocationDistribution);
    }

    [Fact]
    public async Task AnalyzeAudienceAsync_ContainsBestPostingTimes()
    {
        // Arrange
        var request = new AudienceAnalysisRequest
        {
            Platform = "TikTok",
            Category = "Entertainment",
            ContentTags = new()
        };

        // Act
        var response = await _service.AnalyzeAudienceAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response.Insights.BestPostingTimes);
        Assert.NotEmpty(response.Insights.BestPostingTimes);
        Assert.All(response.Insights.BestPostingTimes.Values, score =>
        {
            Assert.True(score >= 0 && score <= 100);
        });
    }

    [Fact]
    public async Task GetTopInterestsAsync_UnknownCategory_ReturnsDefaultInterests()
    {
        // Arrange
        var category = "UnknownCategory";

        // Act
        var interests = await _service.GetTopInterestsAsync(category, CancellationToken.None);

        // Assert
        Assert.NotNull(interests);
        Assert.NotEmpty(interests);
        // Should return generic/default interests
        Assert.Contains(interests, i => i.Contains("Content") || i.Contains("Trending") || i.Contains("General"));
    }
}
