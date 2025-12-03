using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for QueryExpansionService
/// </summary>
public class QueryExpansionServiceTests
{
    private readonly Mock<ILogger<QueryExpansionService>> _loggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;

    public QueryExpansionServiceTests()
    {
        _loggerMock = new Mock<ILogger<QueryExpansionService>>();
        _llmProviderMock = new Mock<ILlmProvider>();
    }

    [Fact]
    public async Task ExpandQueryAsync_IncludesOriginalQuery()
    {
        // Arrange
        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: false);
        var originalQuery = "machine learning algorithms";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.Contains(originalQuery, result);
    }

    [Fact]
    public async Task ExpandQueryAsync_WithLlmDisabled_UsesBasicExpansion()
    {
        // Arrange
        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: false);
        var originalQuery = "artificial intelligence research methods";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(originalQuery, result);
        // Should extract key phrases based on heuristics
        _llmProviderMock.Verify(
            x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExpandQueryAsync_WithLlmEnabled_CallsLlmProvider()
    {
        // Arrange
        var llmResponse = @"[""ML algorithms"", ""deep learning"", ""neural networks"", ""data science""]";
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "machine learning";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.Contains("ML algorithms", result);
        Assert.Contains("deep learning", result);
        Assert.Contains("neural networks", result);
        Assert.Contains("data science", result);
        _llmProviderMock.Verify(
            x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExpandQueryAsync_RespectsMaxVariations()
    {
        // Arrange
        var llmResponse = @"[""query1"", ""query2"", ""query3"", ""query4""]";
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "test query with multiple words";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery, maxVariations: 3);

        // Assert
        Assert.True(result.Count <= 3);
    }

    [Fact]
    public async Task ExpandQueryAsync_RemovesDuplicates()
    {
        // Arrange
        var llmResponse = @"[""machine learning"", ""Machine Learning"", ""deep learning"", ""machine learning""]";
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "machine learning";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        // Should have distinct values only (case-sensitive dedup)
        var distinctCount = new HashSet<string>(result).Count;
        Assert.Equal(distinctCount, result.Count);
    }

    [Fact]
    public async Task ExpandQueryAsync_WhenLlmFails_FallsBackToBasicExpansion()
    {
        // Arrange
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service unavailable"));

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "artificial intelligence research";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(originalQuery, result);
        // Verify warning was logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LLM query expansion failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExpandQueryAsync_WhenLlmReturnsInvalidJson_FallsBackToBasicExpansion()
    {
        // Arrange
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is not valid JSON");

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "test query";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(originalQuery, result);
    }

    [Fact]
    public async Task ExpandQueryAsync_WithContext_IncludesContextInPrompt()
    {
        // Arrange
        string? capturedPrompt = null;
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, LlmParameters?, CancellationToken>((system, user, parameters, ct) =>
            {
                capturedPrompt = user;
            })
            .ReturnsAsync(@"[""query1"", ""query2""]");

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "machine learning";
        var context = "Goal: Educate, Audience: Beginners";

        // Act
        await service.ExpandQueryAsync(originalQuery, context);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains(originalQuery, capturedPrompt);
        Assert.Contains(context, capturedPrompt);
    }

    [Fact]
    public async Task ExpandQueryAsync_ExtractsKeyPhrasesFromLongQuery()
    {
        // Arrange
        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: false);
        var originalQuery = "Understanding advanced machine learning algorithms for natural language processing";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.NotEmpty(result);
        // Should include original query
        Assert.Contains(originalQuery, result);
        // Should have multiple variations (basic key phrases extracted)
        Assert.True(result.Count > 1);
    }

    [Fact]
    public async Task ExpandQueryAsync_WhenLlmReturnsJsonWithExtraText_ExtractsJsonArray()
    {
        // Arrange
        var llmResponse = @"Here are some alternatives:
[""alternative1"", ""alternative2"", ""alternative3""]
These should help with your search.";
        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);
        var originalQuery = "test";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.Contains("alternative1", result);
        Assert.Contains("alternative2", result);
        Assert.Contains("alternative3", result);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryExpansionService(null!, _llmProviderMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLlmProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryExpansionService(_loggerMock.Object, null!));
    }

    [Fact]
    public async Task ExpandQueryAsync_WithShortQuery_StillReturnsVariations()
    {
        // Arrange
        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: false);
        var originalQuery = "AI";

        // Act
        var result = await service.ExpandQueryAsync(originalQuery);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(originalQuery, result);
    }

    [Fact]
    public async Task ExpandQueryAsync_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _llmProviderMock
            .Setup(x => x.GenerateChatCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmParameters>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = new QueryExpansionService(_loggerMock.Object, _llmProviderMock.Object, enableLlmExpansion: true);

        // Act
        var result = await service.ExpandQueryAsync("test query", ct: cts.Token);

        // Assert - should fall back to basic expansion
        Assert.NotEmpty(result);
    }
}
