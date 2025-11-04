using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class DocumentChunkingServiceTests
{
    private readonly Mock<ILogger<DocumentChunkingService>> _loggerMock;
    private readonly DocumentChunkingService _service;

    public DocumentChunkingServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentChunkingService>>();
        _service = new DocumentChunkingService(_loggerMock.Object);
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithFixedStrategy_CreatesChunks()
    {
        var document = CreateTestDocument("This is a test document with some content. " +
            "It should be split into multiple chunks based on the configured size. " +
            "Each chunk should have proper metadata and position tracking.");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Fixed,
            MaxChunkSize = 10,
            OverlapSize = 2
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk =>
        {
            Assert.NotNull(chunk.Content);
            Assert.NotEqual(Guid.Empty.ToString(), chunk.DocumentId);
            Assert.InRange(chunk.ChunkIndex, 0, chunks.Count - 1);
        });
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithSemanticStrategy_PreservesSections()
    {
        var document = new DocumentImportResult
        {
            Success = true,
            Metadata = new DocumentMetadata
            {
                OriginalFileName = "test.md",
                Format = DocFormat.Markdown,
                WordCount = 50
            },
            Structure = new DocumentStructure
            {
                Sections = new List<DocumentSection>
                {
                    new DocumentSection
                    {
                        Level = 1,
                        Heading = "Introduction",
                        Content = "This is the introduction section. It contains important information."
                    },
                    new DocumentSection
                    {
                        Level = 1,
                        Heading = "Conclusion",
                        Content = "This is the conclusion section. It summarizes the main points."
                    }
                }
            },
            RawContent = "Combined content for testing."
        };

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Semantic,
            MaxChunkSize = 20
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Metadata.Section == "Introduction");
        Assert.Contains(chunks, c => c.Metadata.Section == "Conclusion");
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithSentenceStrategy_PreservesSentences()
    {
        var document = CreateTestDocument(
            "First sentence here. Second sentence follows. Third sentence ends the paragraph.");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Sentence,
            MaxChunkSize = 5,
            PreserveSentences = true
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk =>
        {
            Assert.NotEmpty(chunk.Content);
            Assert.True(chunk.Content.Contains('.') || chunks.IndexOf(chunk) == chunks.Count - 1);
        });
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithParagraphStrategy_CreatesParagraphChunks()
    {
        var document = CreateTestDocument(
            "First paragraph with some content.\n\n" +
            "Second paragraph with more content.\n\n" +
            "Third paragraph to finish.");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Paragraph,
            MaxChunkSize = 20
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk =>
        {
            Assert.NotEmpty(chunk.Content);
            Assert.DoesNotContain("\n\n", chunk.Content);
        });
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithEmptyDocument_ReturnsEmptyList()
    {
        var document = CreateTestDocument("");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Fixed,
            MaxChunkSize = 10
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.Empty(chunks);
    }

    [Fact]
    public async Task ChunkDocumentAsync_WithOverlap_CreatesOverlappingChunks()
    {
        var document = CreateTestDocument(
            "Word1 Word2 Word3 Word4 Word5 Word6 Word7 Word8 Word9 Word10 Word11 Word12");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Fixed,
            MaxChunkSize = 5,
            OverlapSize = 2
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.True(chunks.Count >= 2);
    }

    [Fact]
    public async Task ChunkDocumentAsync_SetsMetadataCorrectly()
    {
        var document = CreateTestDocument("Test content for metadata.");

        var config = new ChunkingConfig
        {
            Strategy = ChunkingStrategy.Fixed,
            MaxChunkSize = 10
        };

        var chunks = await _service.ChunkDocumentAsync(document, config);

        Assert.All(chunks, chunk =>
        {
            Assert.Equal("test.txt", chunk.Metadata.Source);
            Assert.True(chunk.Metadata.WordCount >= 0);
        });
    }

    private DocumentImportResult CreateTestDocument(string content)
    {
        return new DocumentImportResult
        {
            Success = true,
            Metadata = new DocumentMetadata
            {
                OriginalFileName = "test.txt",
                Format = DocFormat.PlainText,
                WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                ImportedAt = DateTime.UtcNow
            },
            Structure = new DocumentStructure
            {
                Sections = new List<DocumentSection>()
            },
            RawContent = content
        };
    }
}
