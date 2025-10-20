using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class ResourceMonitorTests
{
    private readonly ILogger<ResourceMonitor> _logger;

    public ResourceMonitorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ResourceMonitor>();
    }

    [Fact]
    public void GetCurrentSnapshot_ShouldReturnValidSnapshot()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        var snapshot = monitor.GetCurrentSnapshot();

        // Assert
        Assert.NotNull(snapshot);
        Assert.InRange(snapshot.CpuUsagePercent, 0, 100);
        Assert.InRange(snapshot.MemoryUsagePercent, 0, 100);
        Assert.InRange(snapshot.GpuUsagePercent, 0, 100);
        Assert.True(snapshot.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void GetRecommendedConcurrency_ShouldReturnPositiveValue()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        int concurrency = monitor.GetRecommendedConcurrency();

        // Assert
        Assert.True(concurrency > 0);
        Assert.True(concurrency <= Environment.ProcessorCount);
    }

    [Fact]
    public void CanStartTask_WithLowCost_ShouldReturnTrue()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        bool canStart = monitor.CanStartTask(0.1); // Low resource cost

        // Assert
        Assert.True(canStart);
    }

    [Fact]
    public void CanStartTask_WithResourceCost_ShouldEvaluateCorrectly()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        bool canStartLight = monitor.CanStartTask(0.3);
        bool canStartMedium = monitor.CanStartTask(0.5);
        bool canStartHeavy = monitor.CanStartTask(0.8);

        // Assert
        // All should be boolean values (no exceptions)
        Assert.IsType<bool>(canStartLight);
        Assert.IsType<bool>(canStartMedium);
        Assert.IsType<bool>(canStartHeavy);
    }

    [Fact]
    public async Task WaitForResourcesAsync_WithLowCost_ShouldCompleteQuickly()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var task = monitor.WaitForResourcesAsync(0.1, cts.Token);
        await task.ConfigureAwait(false);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task WaitForResourcesAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitor.WaitForResourcesAsync(0.5, cts.Token).ConfigureAwait(false));
    }

    [Fact]
    public void ResourceSnapshot_ShouldStoreCorrectData()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var snapshot = new ResourceSnapshot(50.5, 60.3, 10.2, timestamp);

        // Assert
        Assert.Equal(50.5, snapshot.CpuUsagePercent);
        Assert.Equal(60.3, snapshot.MemoryUsagePercent);
        Assert.Equal(10.2, snapshot.GpuUsagePercent);
        Assert.Equal(timestamp, snapshot.Timestamp);
    }

    [Fact]
    public void GetCurrentSnapshot_ShouldCacheResults()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        var snapshot1 = monitor.GetCurrentSnapshot();
        var snapshot2 = monitor.GetCurrentSnapshot();

        // Assert
        // Should return the same snapshot if called within the update interval
        Assert.Equal(snapshot1.Timestamp, snapshot2.Timestamp);
    }

    [Fact]
    public async Task GetCurrentSnapshot_AfterDelay_ShouldUpdateSnapshot()
    {
        // Arrange
        var monitor = new ResourceMonitor(_logger);

        // Act
        var snapshot1 = monitor.GetCurrentSnapshot();
        await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false); // Wait longer than update interval
        var snapshot2 = monitor.GetCurrentSnapshot();

        // Assert
        // Should have different timestamps after the update interval
        Assert.True(snapshot2.Timestamp > snapshot1.Timestamp);
    }
}
