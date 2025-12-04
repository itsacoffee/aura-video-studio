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

public class RepurposingServiceTests
{
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<IShortsExtractor> _shortsExtractorMock;
    private readonly Mock<IBlogGenerator> _blogGeneratorMock;
    private readonly Mock<IQuoteGenerator> _quoteGeneratorMock;
    private readonly Mock<IAspectConverter> _aspectConverterMock;
    private readonly Mock<ILogger<RepurposingService>> _loggerMock;
    private readonly RepurposingService _service;

    public RepurposingServiceTests()
    {
        _llmProviderMock = new Mock<ILlmProvider>();
        _shortsExtractorMock = new Mock<IShortsExtractor>();
        _blogGeneratorMock = new Mock<IBlogGenerator>();
        _quoteGeneratorMock = new Mock<IQuoteGenerator>();
        _aspectConverterMock = new Mock<IAspectConverter>();
        _loggerMock = new Mock<ILogger<RepurposingService>>();

        _service = new RepurposingService(
            _llmProviderMock.Object,
            _shortsExtractorMock.Object,
            _blogGeneratorMock.Object,
            _quoteGeneratorMock.Object,
            _aspectConverterMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AnalyzeForRepurposingAsync_WithValidSource_ReturnsPlan()
    {
        // Arrange
        var sourceVideo = CreateTestVideoResult();
        var options = new RepurposingOptions();

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        // Act
        var result = await _service.AnalyzeForRepurposingAsync(sourceVideo, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SourceVideoId);
        Assert.NotNull(result.Metadata);
    }

    [Fact]
    public async Task AnalyzeForRepurposingAsync_WithNoTimeline_ThrowsArgumentException()
    {
        // Arrange
        var sourceVideo = new VideoGenerationResult(
            OutputPath: "/test/output.mp4",
            ProviderTimeline: null,
            EditableTimeline: null,
            NarrationPath: null,
            SubtitlesPath: null,
            CorrelationId: "test-123");
        var options = new RepurposingOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AnalyzeForRepurposingAsync(sourceVideo, options));
    }

    [Fact]
    public async Task AnalyzeForRepurposingAsync_WithShortsDisabled_ReturnsEmptyShorts()
    {
        // Arrange
        var sourceVideo = CreateTestVideoResult();
        var options = new RepurposingOptions(GenerateShorts: false);

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{}");

        // Act
        var result = await _service.AnalyzeForRepurposingAsync(sourceVideo, options);

        // Assert
        Assert.Empty(result.Shorts);
    }

    [Fact]
    public async Task AnalyzeForRepurposingAsync_WithBlogDisabled_ReturnsNullBlogPost()
    {
        // Arrange
        var sourceVideo = CreateTestVideoResult();
        var options = new RepurposingOptions(GenerateBlogPost: false);

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        // Act
        var result = await _service.AnalyzeForRepurposingAsync(sourceVideo, options);

        // Assert
        Assert.Null(result.BlogPost);
    }

    [Fact]
    public async Task ExecuteRepurposingAsync_WithEmptyPlan_ReturnsEmptyResult()
    {
        // Arrange
        var plan = new RepurposingPlan(
            SourceVideoId: "test-123",
            Shorts: Array.Empty<ShortsPlan>(),
            BlogPost: null,
            Quotes: Array.Empty<QuotePlan>(),
            AspectVariants: Array.Empty<AspectVariantPlan>(),
            Metadata: new RepurposingMetadata(TimeSpan.FromMinutes(5), 10, DateTime.UtcNow));

        // Act
        var result = await _service.ExecuteRepurposingAsync(plan);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Shorts);
        Assert.Null(result.BlogPost);
        Assert.Empty(result.Quotes);
        Assert.Empty(result.AspectVariants);
    }

    [Fact]
    public async Task ExecuteRepurposingAsync_ReportsProgress()
    {
        // Arrange
        var plan = new RepurposingPlan(
            SourceVideoId: "test-123",
            Shorts: Array.Empty<ShortsPlan>(),
            BlogPost: null,
            Quotes: Array.Empty<QuotePlan>(),
            AspectVariants: Array.Empty<AspectVariantPlan>(),
            Metadata: new RepurposingMetadata(TimeSpan.FromMinutes(5), 10, DateTime.UtcNow));

        var progressReported = false;
        var progress = new Progress<RepurposingProgress>(p =>
        {
            progressReported = true;
        });

        // Act
        await _service.ExecuteRepurposingAsync(plan, progress);

        // Assert - progress is reported at completion
        Assert.True(progressReported);
    }

    [Fact]
    public async Task ExecuteRepurposingAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var timeline = CreateTestTimeline();
        var shortPlan = new ShortsPlan(
            Title: "Test Short",
            StartSceneIndex: 0,
            EndSceneIndex: 0,
            HookText: "Hook text",
            EstimatedDuration: TimeSpan.FromSeconds(30),
            ViralPotential: 0.8,
            Platform: "tiktok",
            Reasoning: "Test reasoning",
            SourceTimeline: timeline,
            SourceVideoPath: "/test/video.mp4");

        var plan = new RepurposingPlan(
            SourceVideoId: "test-123",
            Shorts: new[] { shortPlan },
            BlogPost: null,
            Quotes: Array.Empty<QuotePlan>(),
            AspectVariants: Array.Empty<AspectVariantPlan>(),
            Metadata: new RepurposingMetadata(TimeSpan.FromMinutes(5), 10, DateTime.UtcNow));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ExecuteRepurposingAsync(plan, ct: cts.Token));
    }

    private static VideoGenerationResult CreateTestVideoResult()
    {
        var timeline = CreateTestTimeline();

        return new VideoGenerationResult(
            OutputPath: "/test/output.mp4",
            ProviderTimeline: timeline,
            EditableTimeline: null,
            NarrationPath: null,
            SubtitlesPath: null,
            CorrelationId: "test-123");
    }

    private static ProviderTimeline CreateTestTimeline()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Introduction", "Welcome to this video about testing.", TimeSpan.Zero, TimeSpan.FromSeconds(15)),
            new Scene(1, "Main Content", "Here is the main content of the video.", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)),
            new Scene(2, "Conclusion", "Thank you for watching!", TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(15))
        };

        return new ProviderTimeline(
            scenes,
            new Dictionary<int, IReadOnlyList<Asset>>(),
            "/test/narration.wav",
            "/test/music.mp3",
            null);
    }
}
