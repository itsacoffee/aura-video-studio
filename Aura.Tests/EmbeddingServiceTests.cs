using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class EmbeddingServiceTests
{
    private readonly Mock<ILogger<EmbeddingService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly EmbeddingService _service;

    public EmbeddingServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmbeddingService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var config = new EmbeddingConfig
        {
            Provider = EmbeddingProvider.Local,
            DimensionSize = 384
        };

        _service = new EmbeddingService(_loggerMock.Object, _httpClientFactoryMock.Object, config);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithText_ReturnsEmbedding()
    {
        var text = "This is a test document for embedding generation.";

        var embedding = await _service.GenerateEmbeddingAsync(text);

        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);
        Assert.All(embedding, value => Assert.InRange(value, -10, 10));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithMultipleTexts_ReturnsMultipleEmbeddings()
    {
        var texts = new List<string>
        {
            "First test document.",
            "Second test document.",
            "Third test document."
        };

        var embeddings = await _service.GenerateEmbeddingsAsync(texts);

        Assert.Equal(texts.Count, embeddings.Count);
        Assert.All(embeddings, embedding =>
        {
            Assert.NotNull(embedding);
            Assert.Equal(384, embedding.Length);
        });
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_WithEmptyList_ReturnsEmptyList()
    {
        var texts = new List<string>();

        var embeddings = await _service.GenerateEmbeddingsAsync(texts);

        Assert.Empty(embeddings);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ReturnsZeroEmbedding()
    {
        var embedding = await _service.GenerateEmbeddingAsync("");

        Assert.NotNull(embedding);
        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    public void CosineSimilarity_WithIdenticalEmbeddings_ReturnsOne()
    {
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = 0.5f;
        }

        var similarity = EmbeddingService.CosineSimilarity(embedding, embedding);

        Assert.InRange(similarity, 0.99f, 1.01f);
    }

    [Fact]
    public void CosineSimilarity_WithOrthogonalEmbeddings_ReturnsZero()
    {
        var embedding1 = new float[384];
        var embedding2 = new float[384];

        for (int i = 0; i < 192; i++)
        {
            embedding1[i] = 1.0f;
        }

        for (int i = 192; i < 384; i++)
        {
            embedding2[i] = 1.0f;
        }

        var similarity = EmbeddingService.CosineSimilarity(embedding1, embedding2);

        Assert.InRange(similarity, -0.01f, 0.01f);
    }

    [Fact]
    public void CosineSimilarity_WithDifferentLengths_ThrowsException()
    {
        var embedding1 = new float[384];
        var embedding2 = new float[256];

        Assert.Throws<ArgumentException>(() =>
            EmbeddingService.CosineSimilarity(embedding1, embedding2));
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_GeneratesDifferentEmbeddings()
    {
        var text1 = "Machine learning is a subset of artificial intelligence.";
        var text2 = "The weather is nice today.";

        var embedding1 = await _service.GenerateEmbeddingAsync(text1);
        var embedding2 = await _service.GenerateEmbeddingAsync(text2);

        var similarity = EmbeddingService.CosineSimilarity(embedding1, embedding2);

        Assert.InRange(similarity, -1.0f, 0.99f);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ProducesNormalizedVectors()
    {
        var text = "Testing vector normalization.";

        var embedding = await _service.GenerateEmbeddingAsync(text);

        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));

        Assert.InRange(magnitude, 0.99, 1.01);
    }
}
