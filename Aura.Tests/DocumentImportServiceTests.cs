using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Aura.Core.Providers;
using Aura.Core.Services.Content;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class DocumentImportServiceTests
{
    private readonly Mock<ILogger<DocumentImportService>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly DocumentImportService _service;

    public DocumentImportServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentImportService>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _llmProviderMock = new Mock<ILlmProvider>();

        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _service = new DocumentImportService(_loggerMock.Object, _llmProviderMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task ImportDocumentAsync_PlainText_Success()
    {
        var content = @"Introduction to Machine Learning

Machine learning is a subset of artificial intelligence that enables systems to learn from data.

Key Concepts
- Supervised Learning
- Unsupervised Learning
- Reinforcement Learning

Applications
Machine learning powers recommendation systems, fraud detection, and autonomous vehicles.";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _service.ImportDocumentAsync(stream, "test.txt", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Metadata);
        Assert.Equal("test.txt", result.Metadata.OriginalFileName);
        Assert.Equal(DocFormat.PlainText, result.Metadata.Format);
        Assert.True(result.Metadata.WordCount > 0);
        
        Assert.NotNull(result.Structure);
        Assert.NotEmpty(result.Structure.Sections);
        Assert.NotEmpty(result.Structure.KeyConcepts);
    }

    [Fact]
    public async Task ImportDocumentAsync_Markdown_Success()
    {
        var content = @"# Machine Learning Tutorial

## Introduction
Machine learning enables computers to learn from data without explicit programming.

## Types of Learning
### Supervised Learning
Learns from labeled data.

### Unsupervised Learning
Finds patterns in unlabeled data.

## Conclusion
Machine learning is transforming technology.";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _service.ImportDocumentAsync(stream, "tutorial.md", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(DocFormat.Markdown, result.Metadata.Format);
        Assert.NotEmpty(result.Structure.Sections);
        Assert.True(result.Structure.HeadingLevels >= 1);
    }

    [Fact]
    public async Task ImportDocumentAsync_HTML_Success()
    {
        var content = @"<!DOCTYPE html>
<html>
<head><title>AI Overview</title></head>
<body>
<h1>Artificial Intelligence</h1>
<p>AI is revolutionizing how we interact with technology.</p>

<h2>Machine Learning</h2>
<p>A subset of AI that learns from data.</p>

<h2>Deep Learning</h2>
<p>Neural networks with multiple layers.</p>
</body>
</html>";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _service.ImportDocumentAsync(stream, "article.html", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(DocFormat.Html, result.Metadata.Format);
        Assert.NotEmpty(result.Structure.Sections);
    }

    [Fact]
    public async Task ImportDocumentAsync_ExceedsSizeLimit_ReturnsError()
    {
        var largeContent = new string('x', 11 * 1024 * 1024); // 11MB
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeContent));

        var result = await _service.ImportDocumentAsync(stream, "large.txt", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("exceeds maximum limit", result.ErrorMessage);
    }

    [Fact]
    public async Task ImportDocumentAsync_UnsupportedFormat_ReturnsError()
    {
        var content = "Some content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _service.ImportDocumentAsync(stream, "file.xyz", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Unsupported file format", result.ErrorMessage);
    }

    [Fact]
    public void SuggestBriefFromDocument_CreatesValidBrief()
    {
        var importResult = new DocumentImportResult
        {
            Success = true,
            Metadata = new DocumentMetadata
            {
                Title = "Machine Learning Basics",
                WordCount = 500,
                DetectedLanguage = "en"
            },
            Structure = new DocumentStructure
            {
                Sections = new System.Collections.Generic.List<DocumentSection>
                {
                    new DocumentSection { Heading = "Introduction", Content = "Content here", WordCount = 100 }
                },
                Tone = new DocumentTone { PrimaryTone = "Educational", FormalityLevel = 0.7 }
            },
            InferredAudience = new InferredAudience
            {
                EducationLevel = "College",
                ConfidenceScore = 0.8
            }
        };

        var brief = _service.SuggestBriefFromDocument(importResult);

        Assert.NotNull(brief);
        Assert.Equal("Machine Learning Basics", brief.Topic);
        Assert.Equal("College", brief.Audience);
        Assert.Equal("educational", brief.Tone);
    }

    [Fact]
    public void EstimateVideoDuration_CalculatesCorrectly()
    {
        var importResult = new DocumentImportResult
        {
            Metadata = new DocumentMetadata { WordCount = 600 }
        };

        var duration = _service.EstimateVideoDuration(importResult, wordsPerMinute: 150);

        Assert.Equal(4, duration.TotalMinutes);
    }

    [Fact]
    public void EstimateVideoDuration_RespectsCap()
    {
        var importResult = new DocumentImportResult
        {
            Metadata = new DocumentMetadata { WordCount = 5000 }
        };

        var duration = _service.EstimateVideoDuration(importResult, wordsPerMinute: 150);

        Assert.True(duration.TotalMinutes <= 15);
    }
}
