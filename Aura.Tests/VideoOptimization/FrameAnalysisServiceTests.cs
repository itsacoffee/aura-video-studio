using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.FrameAnalysis;
using Aura.Core.Services.VideoOptimization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.VideoOptimization;

public class FrameAnalysisServiceTests
{
    private readonly Mock<ILogger<FrameAnalysisService>> _mockLogger;
    private readonly FrameAnalysisService _service;

    public FrameAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<FrameAnalysisService>>();
        _service = new FrameAnalysisService(_mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeFramesAsync_WithValidVideo_ReturnsAnalysisResult()
    {
        // Arrange
        var videoPath = System.IO.Path.GetTempFileName();
        var options = new FrameAnalysisOptions(MaxFramesToAnalyze: 50);
        
        try
        {
            // Act
            var result = await _service.AnalyzeFramesAsync(videoPath, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalFrames > 0);
            Assert.True(result.AnalyzedFrames > 0);
            Assert.NotNull(result.ImportanceScores);
            Assert.NotNull(result.Recommendations);
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(videoPath))
                System.IO.File.Delete(videoPath);
        }
    }

    [Fact]
    public async Task AnalyzeFramesAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var videoPath = "/nonexistent/video.mp4";
        var options = new FrameAnalysisOptions();

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.FileNotFoundException>(
            () => _service.AnalyzeFramesAsync(videoPath, options));
    }

    [Fact]
    public async Task AnalyzeFramesAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var videoPath = System.IO.Path.GetTempFileName();
        var options = new FrameAnalysisOptions();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.AnalyzeFramesAsync(videoPath, options, cts.Token));
        }
        finally
        {
            if (System.IO.File.Exists(videoPath))
                System.IO.File.Delete(videoPath);
        }
    }

    [Fact]
    public async Task AnalyzeFramesAsync_ReturnsKeyFrames()
    {
        // Arrange
        var videoPath = System.IO.Path.GetTempFileName();
        var options = new FrameAnalysisOptions();

        try
        {
            // Act
            var result = await _service.AnalyzeFramesAsync(videoPath, options);

            // Assert
            Assert.NotEmpty(result.KeyFrames);
            Assert.All(result.KeyFrames, frame => Assert.True(frame.IsKeyFrame));
        }
        finally
        {
            if (System.IO.File.Exists(videoPath))
                System.IO.File.Delete(videoPath);
        }
    }

    [Fact]
    public async Task AnalyzeFramesAsync_ReturnsImportanceScores()
    {
        // Arrange
        var videoPath = System.IO.Path.GetTempFileName();
        var options = new FrameAnalysisOptions();

        try
        {
            // Act
            var result = await _service.AnalyzeFramesAsync(videoPath, options);

            // Assert
            Assert.NotEmpty(result.ImportanceScores);
            Assert.All(result.ImportanceScores.Values, score =>
            {
                Assert.True(score >= 0.0 && score <= 1.0);
            });
        }
        finally
        {
            if (System.IO.File.Exists(videoPath))
                System.IO.File.Delete(videoPath);
        }
    }

    [Fact]
    public async Task ExtractFrameAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var videoPath = "/nonexistent/video.mp4";
        var timestamp = TimeSpan.FromSeconds(5);

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.FileNotFoundException>(
            () => _service.ExtractFrameAsync(videoPath, timestamp));
    }
}
