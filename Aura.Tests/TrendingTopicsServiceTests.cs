using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Ideation;
using Aura.Core.Providers;
using Aura.Core.Services.Ideation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class TrendingTopicsServiceTests
{
    private readonly Mock<ILogger<TrendingTopicsService>> _mockLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly TrendingTopicsService _service;

    public TrendingTopicsServiceTests()
    {
        _mockLogger = new Mock<ILogger<TrendingTopicsService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _service = new TrendingTopicsService(
            _mockLogger.Object,
            _mockLlmProvider.Object,
            _mockHttpClientFactory.Object,
            _memoryCache
        );
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_GeneralNiche_ReturnsTopics()
    {
        // Arrange
        var niche = "general";
        var maxResults = 5;

        // Mock LLM provider to return a simple response for AI analysis
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: This topic is trending because of increased interest.\nAUDIENCE ENGAGEMENT: Audiences are actively engaging.\nCONTENT ANGLES:\n- Angle 1\n- Angle 2\n- Angle 3\nDEMOGRAPHIC APPEAL: Broad appeal.\nVIRALITY SCORE: 75");

        // Act
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Count <= maxResults);
        Assert.All(result, topic =>
        {
            Assert.NotNull(topic.TopicId);
            Assert.NotNull(topic.Topic);
            Assert.InRange(topic.TrendScore, 0, 100);
        });
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_GamingNiche_ReturnsGamingRelatedTopics()
    {
        // Arrange
        var niche = "gaming";
        var maxResults = 5;

        // Mock LLM provider
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: Gaming hardware is trending.\nAUDIENCE ENGAGEMENT: Gamers are highly engaged.\nCONTENT ANGLES:\n- GPU reviews\n- PC builds\n- Console comparisons\nDEMOGRAPHIC APPEAL: Gaming enthusiasts.\nVIRALITY SCORE: 85");

        // Act
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, topic => topic.Topic.Contains("Gaming", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_WithCaching_ReturnsCachedResults()
    {
        // Arrange
        var niche = "technology";
        var maxResults = 5;

        // Mock LLM provider
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: Tech trends are emerging.\nAUDIENCE ENGAGEMENT: Tech enthusiasts are engaged.\nCONTENT ANGLES:\n- AI tools\n- Software dev\n- Cybersecurity\nDEMOGRAPHIC APPEAL: Tech professionals.\nVIRALITY SCORE: 80");

        // Act - First call should fetch and cache
        var result1 = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Act - Second call should return cached results
        var result2 = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Count, result2.Count);

        // Verify LLM provider was called (for AI insights)
        _mockLlmProvider.Verify(
            x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_WithForceRefresh_SkipsCache()
    {
        // Arrange
        var niche = "health";
        var maxResults = 3;

        // Mock LLM provider
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: Health trends are important.\nAUDIENCE ENGAGEMENT: Health-conscious audience.\nCONTENT ANGLES:\n- Fitness\n- Nutrition\n- Mental health\nDEMOGRAPHIC APPEAL: Health enthusiasts.\nVIRALITY SCORE: 70");

        // Act - First call with cache
        await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Act - Second call with force refresh
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_AllTopicsHaveAIInsights()
    {
        // Arrange
        var niche = "business";
        var maxResults = 3;

        // Mock LLM provider with detailed response
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"WHY TRENDING: Business strategies are evolving rapidly in the current market environment.

AUDIENCE ENGAGEMENT: Entrepreneurs and business professionals are actively seeking guidance.

CONTENT ANGLES:
- Startup fundraising strategies
- Remote team management
- Digital transformation
- Customer acquisition tactics

DEMOGRAPHIC APPEAL: Appeals to business owners, entrepreneurs, and corporate professionals.

VIRALITY SCORE: 82");

        // Act
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, topic =>
        {
            Assert.NotNull(topic.AiInsights);
            Assert.NotNull(topic.AiInsights.WhyTrending);
            Assert.NotNull(topic.AiInsights.AudienceEngagement);
            Assert.NotNull(topic.AiInsights.ContentAngles);
            Assert.NotEmpty(topic.AiInsights.ContentAngles);
            Assert.NotNull(topic.AiInsights.DemographicAppeal);
            Assert.InRange(topic.AiInsights.ViralityScore, 0, 100);
        });
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_TopicsHaveHashtags()
    {
        // Arrange
        var niche = "entertainment";
        var maxResults = 5;

        // Mock LLM provider
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: Entertainment content is popular.\nAUDIENCE ENGAGEMENT: High engagement.\nCONTENT ANGLES:\n- Reviews\n- Analysis\n- Reactions\nDEMOGRAPHIC APPEAL: General audience.\nVIRALITY SCORE: 75");

        // Act
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, topic =>
        {
            Assert.NotNull(topic.Hashtags);
            Assert.NotEmpty(topic.Hashtags);
        });
    }

    [Fact]
    public async Task GetTrendingTopicsAsync_TopicsHaveMetrics()
    {
        // Arrange
        var niche = "education";
        var maxResults = 5;

        // Mock LLM provider
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Aura.Core.Models.Brief>(), It.IsAny<Aura.Core.Models.PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("WHY TRENDING: Educational content demand is growing.\nAUDIENCE ENGAGEMENT: Students are seeking resources.\nCONTENT ANGLES:\n- Tutorials\n- Courses\n- Study guides\nDEMOGRAPHIC APPEAL: Students and learners.\nVIRALITY SCORE: 68");

        // Act
        var result = await _service.GetTrendingTopicsAsync(niche, maxResults, forceRefresh: false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, topic =>
        {
            Assert.NotNull(topic.SearchVolume);
            Assert.NotNull(topic.Competition);
            Assert.NotNull(topic.Lifecycle);
            Assert.NotNull(topic.TrendVelocity);
            Assert.NotNull(topic.EstimatedAudience);
            Assert.True(topic.EstimatedAudience > 0);
        });
    }
}
