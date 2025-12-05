using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class RagContextBuilderStatusTests
{
    [Fact]
    public async Task BuildContextAsync_WithDisabledConfig_ReturnsDisabledStatus()
    {
        // Arrange
        var builder = CreateBuilder();
        var config = new RagConfig { Enabled = false };

        // Act
        var result = await builder.BuildContextAsync("test query", config, CancellationToken.None);

        // Assert
        Assert.Equal(RagContextStatus.Disabled, result.Status);
        Assert.False(result.HasMeaningfulContext);
        Assert.Empty(result.Chunks);
    }

    [Fact]
    public async Task BuildContextAsync_WithEmptyIndex_ReturnsNoDocumentsStatus()
    {
        // Arrange
        var vectorIndexMock = new Mock<VectorIndex>(
            Mock.Of<ILogger<VectorIndex>>(),
            "/tmp/test-index.json");

        // Create a builder with a real VectorIndex that is empty
        var loggerMock = new Mock<ILogger<RagContextBuilder>>();
        var embeddingLoggerMock = new Mock<ILogger<EmbeddingService>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var embeddingConfig = new EmbeddingConfig { Provider = EmbeddingProvider.Local };
        var embeddingService = new EmbeddingService(embeddingLoggerMock.Object, httpClientFactoryMock.Object, embeddingConfig);

        var indexPath = $"/tmp/test-empty-index-{Guid.NewGuid()}.json";
        var vectorIndex = new VectorIndex(Mock.Of<ILogger<VectorIndex>>(), indexPath);

        var builder = new RagContextBuilder(loggerMock.Object, vectorIndex, embeddingService);
        var config = new RagConfig { Enabled = true };

        // Act
        var result = await builder.BuildContextAsync("test query", config, CancellationToken.None);

        // Assert
        Assert.Equal(RagContextStatus.NoDocuments, result.Status);
        Assert.False(result.HasMeaningfulContext);
        Assert.Empty(result.Chunks);

        // Cleanup
        try { System.IO.File.Delete(indexPath); } catch { }
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenSuccessButNoChunks()
    {
        // Arrange & Act
        var emptyContext = RagContext.Empty("query", RagContextStatus.Success);

        // Assert
        Assert.False(emptyContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.Success, emptyContext.Status);
        Assert.Empty(emptyContext.Chunks);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenDisabled()
    {
        // Arrange & Act
        var disabledContext = RagContext.Empty("query", RagContextStatus.Disabled);

        // Assert
        Assert.False(disabledContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.Disabled, disabledContext.Status);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenNoDocuments()
    {
        // Arrange & Act
        var noDocsContext = RagContext.Empty("query", RagContextStatus.NoDocuments);

        // Assert
        Assert.False(noDocsContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.NoDocuments, noDocsContext.Status);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenEmbeddingFailed()
    {
        // Arrange & Act
        var embeddingFailedContext = RagContext.Empty("query", RagContextStatus.EmbeddingFailed);

        // Assert
        Assert.False(embeddingFailedContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.EmbeddingFailed, embeddingFailedContext.Status);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenNoRelevantChunks()
    {
        // Arrange & Act
        var noChunksContext = RagContext.Empty("query", RagContextStatus.NoRelevantChunks);

        // Assert
        Assert.False(noChunksContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.NoRelevantChunks, noChunksContext.Status);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_FalseWhenIndexUnavailable()
    {
        // Arrange & Act
        var indexUnavailableContext = RagContext.Empty("query", RagContextStatus.IndexUnavailable);

        // Assert
        Assert.False(indexUnavailableContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.IndexUnavailable, indexUnavailableContext.Status);
    }

    [Fact]
    public void RagContext_HasMeaningfulContext_TrueOnlyWhenSuccessWithChunks()
    {
        // Arrange & Act
        var successContext = new RagContext
        {
            Query = "query",
            Status = RagContextStatus.Success,
            Chunks = new List<ContextChunk>
            {
                new ContextChunk { Content = "test content", Source = "test.pdf" }
            }
        };

        // Assert
        Assert.True(successContext.HasMeaningfulContext);
        Assert.Equal(RagContextStatus.Success, successContext.Status);
        Assert.Single(successContext.Chunks);
    }

    [Fact]
    public void RagContext_Empty_CreatesContextWithCorrectStatus()
    {
        // Act
        var disabled = RagContext.Empty("q1", RagContextStatus.Disabled);
        var unavailable = RagContext.Empty("q2", RagContextStatus.IndexUnavailable);
        var noDocs = RagContext.Empty("q3", RagContextStatus.NoDocuments);
        var embedFail = RagContext.Empty("q4", RagContextStatus.EmbeddingFailed);
        var noChunks = RagContext.Empty("q5", RagContextStatus.NoRelevantChunks);

        // Assert
        Assert.Equal("q1", disabled.Query);
        Assert.Equal(RagContextStatus.Disabled, disabled.Status);

        Assert.Equal("q2", unavailable.Query);
        Assert.Equal(RagContextStatus.IndexUnavailable, unavailable.Status);

        Assert.Equal("q3", noDocs.Query);
        Assert.Equal(RagContextStatus.NoDocuments, noDocs.Status);

        Assert.Equal("q4", embedFail.Query);
        Assert.Equal(RagContextStatus.EmbeddingFailed, embedFail.Status);

        Assert.Equal("q5", noChunks.Query);
        Assert.Equal(RagContextStatus.NoRelevantChunks, noChunks.Status);
    }

    [Fact]
    public void RagContext_Empty_HasEmptyChunksAndFormattedContext()
    {
        // Act
        var context = RagContext.Empty("test query", RagContextStatus.NoDocuments);

        // Assert
        Assert.NotNull(context.Chunks);
        Assert.Empty(context.Chunks);
        Assert.Equal(string.Empty, context.FormattedContext);
        Assert.Equal(0, context.TotalTokens);
    }

    [Fact]
    public void RagContextStatus_HasAllExpectedValues()
    {
        // Assert that all expected status values exist
        Assert.Equal(0, (int)RagContextStatus.Success);
        Assert.Equal(1, (int)RagContextStatus.Disabled);
        Assert.Equal(2, (int)RagContextStatus.IndexUnavailable);
        Assert.Equal(3, (int)RagContextStatus.NoDocuments);
        Assert.Equal(4, (int)RagContextStatus.EmbeddingFailed);
        Assert.Equal(5, (int)RagContextStatus.NoRelevantChunks);
    }

    private RagContextBuilder CreateBuilder()
    {
        var loggerMock = new Mock<ILogger<RagContextBuilder>>();
        var vectorIndexLoggerMock = new Mock<ILogger<VectorIndex>>();
        var embeddingLoggerMock = new Mock<ILogger<EmbeddingService>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var embeddingConfig = new EmbeddingConfig { Provider = EmbeddingProvider.Local };
        var embeddingService = new EmbeddingService(embeddingLoggerMock.Object, httpClientFactoryMock.Object, embeddingConfig);

        var indexPath = $"/tmp/test-index-{Guid.NewGuid()}.json";
        var vectorIndex = new VectorIndex(vectorIndexLoggerMock.Object, indexPath);

        return new RagContextBuilder(loggerMock.Object, vectorIndex, embeddingService);
    }
}
