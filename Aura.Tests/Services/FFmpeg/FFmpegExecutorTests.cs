using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.FFmpeg;

public class FFmpegExecutorTests
{
    private readonly Mock<IFFmpegService> _mockFFmpegService;
    private readonly Mock<ILogger<FFmpegExecutor>> _mockLogger;
    private readonly FFmpegExecutor _executor;

    public FFmpegExecutorTests()
    {
        _mockFFmpegService = new Mock<IFFmpegService>();
        _mockLogger = new Mock<ILogger<FFmpegExecutor>>();
        _executor = new FFmpegExecutor(_mockFFmpegService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidBuilder_ShouldSucceed()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx264");

        var expectedResult = new FFmpegResult
        {
            Success = true,
            ExitCode = 0,
            Duration = TimeSpan.FromSeconds(5)
        };

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _executor.ExecuteCommandAsync(builder);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNullBuilder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteCommandAsync(null!)
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithProgressCallback_ShouldReportProgress()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4");

        var progressReported = false;
        Action<FFmpegProgress> progressCallback = progress =>
        {
            progressReported = true;
        };

        var expectedResult = new FFmpegResult { Success = true, ExitCode = 0 };

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Action<FFmpegProgress>?, CancellationToken>((cmd, callback, ct) =>
            {
                // Simulate progress callback
                callback?.Invoke(new FFmpegProgress { PercentComplete = 50 });
            })
            .ReturnsAsync(expectedResult);

        // Act
        await _executor.ExecuteCommandAsync(builder, progressCallback);

        // Assert
        Assert.True(progressReported);
    }

    [Fact]
    public async Task ExecuteTwoPassAsync_WithValidBuilders_ShouldExecuteBothPasses()
    {
        // Arrange
        var firstPassBuilder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetTwoPassEncoding("pass.log", 1);

        var secondPassBuilder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetTwoPassEncoding("pass.log", 2);

        var expectedResult = new FFmpegResult { Success = true, ExitCode = 0 };

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _executor.ExecuteTwoPassAsync(firstPassBuilder, secondPassBuilder);

        // Assert
        Assert.True(result.Success);
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task ExecuteTwoPassAsync_WhenFirstPassFails_ShouldReturnFailure()
    {
        // Arrange
        var firstPassBuilder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4");

        var secondPassBuilder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4");

        var failedResult = new FFmpegResult
        {
            Success = false,
            ExitCode = 1,
            ErrorMessage = "First pass failed"
        };

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // Act
        var result = await _executor.ExecuteTwoPassAsync(firstPassBuilder, secondPassBuilder);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("First pass failed", result.ErrorMessage);
        // Should only call once (first pass)
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ExecuteSequentialAsync_WithMultipleBuilders_ShouldExecuteInOrder()
    {
        // Arrange
        var builders = new[]
        {
            new FFmpegCommandBuilder().AddInput("input1.mp4").SetOutput("output1.mp4"),
            new FFmpegCommandBuilder().AddInput("input2.mp4").SetOutput("output2.mp4"),
            new FFmpegCommandBuilder().AddInput("input3.mp4").SetOutput("output3.mp4")
        };

        var expectedResult = new FFmpegResult { Success = true, ExitCode = 0 };

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var results = await _executor.ExecuteSequentialAsync(builders);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.True(r.Success));
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task ExecuteSequentialAsync_WhenOneCommandFails_ShouldStopExecution()
    {
        // Arrange
        var builders = new[]
        {
            new FFmpegCommandBuilder().AddInput("input1.mp4").SetOutput("output1.mp4"),
            new FFmpegCommandBuilder().AddInput("input2.mp4").SetOutput("output2.mp4"),
            new FFmpegCommandBuilder().AddInput("input3.mp4").SetOutput("output3.mp4")
        };

        var callCount = 0;
        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 2
                    ? new FFmpegResult { Success = false, ExitCode = 1, ErrorMessage = "Failed" }
                    : new FFmpegResult { Success = true, ExitCode = 0 };
            });

        // Act
        var results = await _executor.ExecuteSequentialAsync(builders);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        Assert.Null(results[2]); // Should not execute third command
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4");

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<FFmpegProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(async (string cmd, Action<FFmpegProgress>? callback, CancellationToken ct) =>
            {
                // Simulate long-running operation
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new FFmpegResult { Success = true };
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<TimeoutException>(() =>
            _executor.ExecuteCommandAsync(builder, timeout: TimeSpan.FromMilliseconds(100))
        );
    }
}
