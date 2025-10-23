using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.ContentPlanning;
using Aura.Core.Providers;
using Aura.Core.Services.ContentPlanning;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class TopicGenerationServiceTests
{
    private readonly Mock<ILogger<TopicGenerationService>> _mockLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly TopicGenerationService _service;

    public TopicGenerationServiceTests()
    {
        _mockLogger = new Mock<ILogger<TopicGenerationService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _service = new TopicGenerationService(_mockLogger.Object, _mockLlmProvider.Object);
    }

    [Fact]
    public async Task GenerateTopicsAsync_ValidRequest_ReturnsTopicSuggestions()
    {
        // Arrange
        var request = new TopicSuggestionRequest
        {
            Category = "Technology",
            TargetAudience = "Developers",
            Interests = new() { "AI", "Cloud Computing" },
            PreferredPlatforms = new() { "YouTube" },
            Count = 5
        };

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("- Topic 1\n- Topic 2\n- Topic 3\n- Topic 4\n- Topic 5");

        // Act
        var response = await _service.GenerateTopicsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Suggestions);
        Assert.True(response.TotalCount > 0);
        Assert.All(response.Suggestions, suggestion =>
        {
            Assert.NotNull(suggestion.Topic);
            Assert.NotNull(suggestion.Description);
            Assert.Equal(request.Category, suggestion.Category);
            Assert.True(suggestion.RelevanceScore >= 0 && suggestion.RelevanceScore <= 100);
            Assert.True(suggestion.TrendScore >= 0 && suggestion.TrendScore <= 100);
        });
    }

    [Fact]
    public async Task GenerateTopicsAsync_LlmFails_ReturnsFallbackTopics()
    {
        // Arrange
        var request = new TopicSuggestionRequest
        {
            Category = "Gaming",
            Count = 10
        };

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service unavailable"));

        // Act
        var response = await _service.GenerateTopicsAsync(request, CancellationToken.None);

        // Assert - Should use fallback generation
        Assert.NotNull(response);
        Assert.NotEmpty(response.Suggestions);
        Assert.Equal(request.Count, response.TotalCount);
    }

    [Fact]
    public async Task GenerateTrendBasedTopicsAsync_ValidTrends_ReturnsRelevantTopics()
    {
        // Arrange
        var trends = new System.Collections.Generic.List<TrendData>
        {
            new TrendData
            {
                Topic = "AI Revolution",
                Platform = "YouTube",
                Category = "Technology",
                TrendScore = 85,
                Direction = TrendDirection.Rising
            },
            new TrendData
            {
                Topic = "Cloud Gaming",
                Platform = "TikTok",
                Category = "Gaming",
                TrendScore = 75,
                Direction = TrendDirection.Stable
            }
        };

        // Act
        var topics = await _service.GenerateTrendBasedTopicsAsync(trends, 2, CancellationToken.None);

        // Assert
        Assert.NotNull(topics);
        Assert.Equal(2, topics.Count);
        Assert.All(topics, topic =>
        {
            Assert.Contains("Exploring", topic.Topic);
            Assert.True(topic.TrendScore > 0);
            Assert.NotEmpty(topic.RecommendedPlatforms);
        });
    }

    [Fact]
    public async Task GenerateTopicsAsync_EmptyInterests_StillGeneratesTopics()
    {
        // Arrange
        var request = new TopicSuggestionRequest
        {
            Category = "Education",
            Interests = new(),
            Count = 5
        };

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Topic A\nTopic B\nTopic C");

        // Act
        var response = await _service.GenerateTopicsAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response.Suggestions);
    }

    [Fact]
    public async Task GenerateTrendBasedTopicsAsync_OrdersByTrendScore()
    {
        // Arrange
        var trends = new System.Collections.Generic.List<TrendData>
        {
            new TrendData { Topic = "Low", TrendScore = 50, Platform = "YouTube", Category = "Test" },
            new TrendData { Topic = "High", TrendScore = 90, Platform = "YouTube", Category = "Test" },
            new TrendData { Topic = "Medium", TrendScore = 70, Platform = "YouTube", Category = "Test" }
        };

        // Act
        var topics = await _service.GenerateTrendBasedTopicsAsync(trends, 3, CancellationToken.None);

        // Assert
        Assert.Equal("Exploring High", topics[0].Topic);
        Assert.True(topics[0].TrendScore > topics[1].TrendScore);
        Assert.True(topics[1].TrendScore > topics[2].TrendScore);
    }
}
