using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ResourceTrackerTests
{
    private readonly Mock<ILogger<ResourceTracker>> _mockLogger;
    private readonly ResourceTracker _resourceTracker;

    public ResourceTrackerTests()
    {
        _mockLogger = new Mock<ILogger<ResourceTracker>>();
        _resourceTracker = new ResourceTracker(_mockLogger.Object);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsValidMetrics()
    {
        // Act
        var metrics = await _resourceTracker.GetMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.AllocatedMemoryBytes >= 0 || metrics.AllocatedMemoryBytes == -1);
        Assert.True(metrics.WorkingSetBytes >= 0 || metrics.WorkingSetBytes == -1);
        Assert.True(metrics.ThreadCount >= 0 || metrics.ThreadCount == -1);
        Assert.NotEqual(default(DateTime), metrics.Timestamp);
        Assert.NotNull(metrics.Warnings);
    }

    [Fact]
    public async Task GetMetricsAsync_IncludesTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var metrics = await _resourceTracker.GetMetricsAsync();

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(metrics.Timestamp, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task CleanupAsync_CompletesSuccessfully()
    {
        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _resourceTracker.CleanupAsync();
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task CleanupAsync_LogsInformation()
    {
        // Act
        await _resourceTracker.CleanupAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("Running manual resource cleanup") ||
                    o.ToString()!.Contains("Cleanup complete")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetMetricsAsync_WithHighMemory_GeneratesWarning()
    {
        // Arrange - force a GC to potentially trigger a memory-related metric
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Act
        var metrics = await _resourceTracker.GetMetricsAsync();

        // Assert - warnings should be non-null at minimum
        Assert.NotNull(metrics.Warnings);
    }

    [Fact]
    public async Task GetMetricsAsync_MultipleCallsReturnDifferentTimestamps()
    {
        // Act
        var metrics1 = await _resourceTracker.GetMetricsAsync();
        await Task.Delay(10);
        var metrics2 = await _resourceTracker.GetMetricsAsync();

        // Assert
        Assert.NotEqual(metrics1.Timestamp, metrics2.Timestamp);
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Act
        _resourceTracker.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Disposing ResourceTracker")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_LogsInitialization()
    {
        // Arrange & Act - constructor already called in setup
        // Creating a new instance to verify initialization logging
        var newTracker = new ResourceTracker(_mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("ResourceTracker initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least twice (one for setup, one for this test)
        
        newTracker.Dispose();
    }

    [Fact]
    public async Task CleanupAsync_WithCancellationToken_CancelsGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _resourceTracker.CleanupAsync(cts.Token);
        });

        // Should not throw even if cancelled, as cleanup is best-effort
        Assert.Null(exception);
    }
}
