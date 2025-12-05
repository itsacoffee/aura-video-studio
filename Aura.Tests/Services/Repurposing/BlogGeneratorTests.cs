using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Repurposing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Tests.Services.Repurposing;

public class BlogGeneratorTests
{
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<ILogger<BlogGenerator>> _loggerMock;
    private readonly BlogGenerator _generator;

    public BlogGeneratorTests()
    {
        _llmProviderMock = new Mock<ILlmProvider>();
        _loggerMock = new Mock<ILogger<BlogGenerator>>();

        _generator = new BlogGenerator(
            _llmProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateAsync_WithValidPlan_ReturnsGeneratedBlogPost()
    {
        // Arrange
        var plan = CreateTestBlogPostPlan();
        var markdownContent = "# Test Blog Post\n\nThis is the content.";

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markdownContent);

        // Act
        var result = await _generator.GenerateAsync(plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan.Title, result.Title);
        Assert.NotEmpty(result.MarkdownContent);
        Assert.NotEmpty(result.HtmlContent);
        Assert.NotEmpty(result.PlainTextContent);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsCorrectMetadata()
    {
        // Arrange
        var plan = CreateTestBlogPostPlan();
        var markdownContent = "# Test\n\nThis is a test blog post with some content.";

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markdownContent);

        // Act
        var result = await _generator.GenerateAsync(plan);

        // Assert
        Assert.Equal(plan.MetaDescription, result.Metadata.MetaDescription);
        Assert.Equal(plan.Tags, result.Metadata.Tags);
        Assert.True(result.Metadata.WordCount > 0);
        Assert.True(result.Metadata.EstimatedReadTime >= 1);
        Assert.True(result.Metadata.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateAsync_ConvertsMarkdownToHtml()
    {
        // Arrange
        var plan = CreateTestBlogPostPlan();
        var markdownContent = "# Header\n\n**Bold text**\n\n*Italic text*";

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markdownContent);

        // Act
        var result = await _generator.GenerateAsync(plan);

        // Assert
        Assert.Contains("<h1>", result.HtmlContent);
        Assert.Contains("<strong>", result.HtmlContent);
        Assert.Contains("<em>", result.HtmlContent);
    }

    [Fact]
    public async Task GenerateAsync_StripsMarkdownFromPlainText()
    {
        // Arrange
        var plan = CreateTestBlogPostPlan();
        var markdownContent = "# Header\n\n**Bold text**";

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(markdownContent);

        // Act
        var result = await _generator.GenerateAsync(plan);

        // Assert
        Assert.DoesNotContain("#", result.PlainTextContent);
        Assert.DoesNotContain("**", result.PlainTextContent);
        Assert.Contains("Bold text", result.PlainTextContent);
    }

    [Fact]
    public async Task GenerateAsync_GeneratesUniqueId()
    {
        // Arrange
        var plan = CreateTestBlogPostPlan();

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test\n\nContent");

        // Act
        var result1 = await _generator.GenerateAsync(plan);
        var result2 = await _generator.GenerateAsync(plan);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
    }

    private static BlogPostPlan CreateTestBlogPostPlan()
    {
        var timeline = CreateTestTimeline();

        return new BlogPostPlan(
            Title: "Test Blog Post",
            MetaDescription: "This is a test blog post about software testing.",
            Introduction: "Testing is an important part of software development.",
            Sections: new List<BlogSection>
            {
                new BlogSection(
                    "Why Testing Matters",
                    "Testing helps ensure software quality.",
                    new List<string> { "Catch bugs early", "Improve reliability" }),
                new BlogSection(
                    "Types of Testing",
                    "There are many types of testing.",
                    new List<string> { "Unit testing", "Integration testing", "E2E testing" })
            },
            Conclusion: "Always write tests for your code.",
            CallToAction: "Start testing today!",
            Tags: new List<string> { "testing", "software", "quality" },
            EstimatedReadTime: 5,
            SourceTimeline: timeline);
    }

    private static ProviderTimeline CreateTestTimeline()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Introduction", "Welcome to this video.", TimeSpan.Zero, TimeSpan.FromSeconds(15)),
            new Scene(1, "Main Content", "Here is the main content.", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30))
        };

        return new ProviderTimeline(
            scenes,
            new Dictionary<int, IReadOnlyList<Asset>>(),
            "/test/narration.wav",
            "/test/music.mp3",
            null);
    }
}
