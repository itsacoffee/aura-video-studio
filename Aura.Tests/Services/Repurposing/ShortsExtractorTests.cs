using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.Repurposing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Tests.Services.Repurposing;

public class ShortsExtractorTests
{
    private readonly Mock<IFFmpegExecutor> _ffmpegExecutorMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<ILogger<ShortsExtractor>> _loggerMock;
    private readonly ShortsExtractor _extractor;

    public ShortsExtractorTests()
    {
        _ffmpegExecutorMock = new Mock<IFFmpegExecutor>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _loggerMock = new Mock<ILogger<ShortsExtractor>>();

        _extractor = new ShortsExtractor(
            _ffmpegExecutorMock.Object,
            _llmProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExtractShortAsync_WithValidPlan_ReturnsGeneratedShort()
    {
        // Arrange
        var plan = CreateTestShortsPlan();

        _ffmpegExecutorMock
            .Setup(x => x.ExecuteCommandAsync(
                It.IsAny<FFmpegCommandBuilder>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, ExitCode = 0 });

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{""caption"": ""Test caption"", ""hashtags"": [""#test""], ""cta"": ""Follow!""}");

        // Act
        var result = await _extractor.ExtractShortAsync(plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan.Title, result.Title);
        Assert.Equal(Aspect.Vertical9x16, result.Aspect);
        Assert.Equal(plan.Platform, result.Platform);
    }

    [Fact]
    public async Task ExtractShortAsync_WithFFmpegFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var plan = CreateTestShortsPlan();

        _ffmpegExecutorMock
            .Setup(x => x.ExecuteCommandAsync(
                It.IsAny<FFmpegCommandBuilder>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = false, ExitCode = 1, ErrorMessage = "FFmpeg error" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _extractor.ExtractShortAsync(plan));
        Assert.Contains("Short extraction failed", exception.Message);
    }

    [Theory]
    [InlineData("tiktok", Aspect.Vertical9x16)]
    [InlineData("youtube_shorts", Aspect.Vertical9x16)]
    [InlineData("instagram_reels", Aspect.Vertical9x16)]
    public async Task ExtractShortAsync_SetsCorrectAspectForPlatform(string platform, Aspect expectedAspect)
    {
        // Arrange
        var plan = CreateTestShortsPlan() with { Platform = platform };

        _ffmpegExecutorMock
            .Setup(x => x.ExecuteCommandAsync(
                It.IsAny<FFmpegCommandBuilder>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, ExitCode = 0 });

        _llmProviderMock
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{""caption"": ""Test"", ""hashtags"": [], ""cta"": """"}");

        // Act
        var result = await _extractor.ExtractShortAsync(plan);

        // Assert
        Assert.Equal(expectedAspect, result.Aspect);
    }

    private static ShortsPlan CreateTestShortsPlan()
    {
        var timeline = CreateTestTimeline();

        return new ShortsPlan(
            Title: "Test Short Video",
            StartSceneIndex: 0,
            EndSceneIndex: 1,
            HookText: "Did you know that testing is important?",
            EstimatedDuration: TimeSpan.FromSeconds(30),
            ViralPotential: 0.8,
            Platform: "tiktok",
            Reasoning: "This segment has great hook potential",
            SourceTimeline: timeline,
            SourceVideoPath: "/tmp/test_video.mp4");
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
