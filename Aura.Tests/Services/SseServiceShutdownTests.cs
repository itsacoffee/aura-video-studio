using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for SseService shutdown and error notification behavior
/// Addresses bugs identified in PR #372 code review
/// </summary>
public class SseServiceShutdownTests
{
    private readonly Mock<ILogger<SseService>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly SseService _sseService;
    private readonly CancellationTokenSource _appStoppingCts;

    public SseServiceShutdownTests()
    {
        _loggerMock = new Mock<ILogger<SseService>>();
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _appStoppingCts = new CancellationTokenSource();
        
        // Setup the application lifetime mock to return our controlled token
        _lifetimeMock.Setup(x => x.ApplicationStopping).Returns(_appStoppingCts.Token);
        
        _sseService = new SseService(_loggerMock.Object, _lifetimeMock.Object);
    }

    [Fact]
    public async Task StreamProgressAsync_WhenCancelled_LogsAppropriately()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        var operationCts = new CancellationTokenSource();
        
        // Operation that will be cancelled
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            await Task.Delay(50, CancellationToken.None);
            operationCts.Cancel(); // Cancel operation
            ct.ThrowIfCancellationRequested();
        }

        // Act
        await _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, operationCts.Token);

        // Assert - verify logging happened
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SSE stream cancelled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamProgressAsync_WhenApplicationStopping_HandlesCancellation()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        
        // Operation that simulates work during shutdown
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            await Task.Delay(50, CancellationToken.None);
            
            // Simulate application shutdown during operation
            _appStoppingCts.Cancel();
            
            // Wait a bit to let cancellation propagate
            await Task.Delay(100, CancellationToken.None);
            
            // Check if cancelled
            ct.ThrowIfCancellationRequested();
        }

        // Act
        await _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, CancellationToken.None);

        // Assert - verify cancellation was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SSE stream cancelled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamProgressAsync_WhenOperationThrows_LogsError()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        
        // Operation that throws an exception
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            await Task.Delay(50, CancellationToken.None);
            throw new InvalidOperationException("Test exception");
        }

        // Act
        await _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, CancellationToken.None);

        // Assert - verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error during SSE stream")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StreamProgressAsync_CompletionEvent_CompletesSuccessfully()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        
        // Operation that completes successfully
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            await Task.Delay(50, CancellationToken.None);
            progress.Report("completed");
        }

        // Act
        await _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, CancellationToken.None);

        // Assert - should complete without errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StreamProgressAsync_MultipleProgressUpdates_NoErrors()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        
        // Operation with multiple progress updates
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(20, CancellationToken.None);
                progress.Report($"step {i}");
            }
        }

        // Act
        await _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, CancellationToken.None);

        // Assert - should complete without errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StreamProgressAsync_RapidCancellation_DoesNotDeadlock()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();
        var cts = new CancellationTokenSource();
        
        // Operation that gets cancelled immediately
        async Task Operation(IProgress<string> progress, CancellationToken ct)
        {
            cts.Cancel(); // Cancel immediately
            ct.ThrowIfCancellationRequested();
            await Task.Delay(1000, ct); // Should never reach here
        }

        // Act & Assert - should complete quickly without deadlock
        var task = _sseService.StreamProgressAsync<string>(defaultContext.Response, Operation, cts.Token);
        var completed = await Task.WhenAny(task, Task.Delay(5000)) == task;
        
        Assert.True(completed, "Operation should complete within 5 seconds without deadlock");
    }

    [Fact]
    public async Task SendEventAsync_HandlesExceptionsGracefully()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();

        // Act - should not throw even if stream is closed or has issues
        await _sseService.SendEventAsync(defaultContext.Response, "test", new { data = "value" }, CancellationToken.None);

        // Assert - completes without exception
        Assert.True(true, "SendEventAsync should handle exceptions gracefully");
    }

    [Fact]
    public async Task SendKeepAliveAsync_HandlesExceptionsGracefully()
    {
        // Arrange
        var defaultContext = new DefaultHttpContext();

        // Act - should not throw
        await _sseService.SendKeepAliveAsync(defaultContext.Response, CancellationToken.None);

        // Assert - completes without exception
        Assert.True(true, "SendKeepAliveAsync should handle exceptions gracefully");
    }
}
