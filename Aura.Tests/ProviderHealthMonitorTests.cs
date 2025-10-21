using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class ProviderHealthMonitorTests
{
    private readonly ILogger<ProviderHealthMonitor> _logger;

    public ProviderHealthMonitorTests()
    {
        _logger = NullLogger<ProviderHealthMonitor>.Instance;
    }

    [Fact]
    public async Task CheckProviderHealthAsync_Success_RecordsHealthyMetrics()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(true);

        // Act
        var metrics = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);

        // Assert
        Assert.Equal("TestProvider", metrics.ProviderName);
        Assert.True(metrics.IsHealthy);
        Assert.Equal(0, metrics.ConsecutiveFailures);
        Assert.Null(metrics.LastError);
        Assert.True(metrics.ResponseTime.TotalMilliseconds >= 0);
    }

    [Fact]
    public async Task CheckProviderHealthAsync_Failure_RecordsUnhealthyMetrics()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(false);

        // Act
        var metrics = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);

        // Assert
        Assert.Equal("TestProvider", metrics.ProviderName);
        Assert.True(metrics.IsHealthy); // Still healthy after 1 failure (needs 3 for unhealthy)
        Assert.Equal(1, metrics.ConsecutiveFailures);
        Assert.NotNull(metrics.LastError);
        Assert.Contains("returned false", metrics.LastError);
    }

    [Fact]
    public async Task CheckProviderHealthAsync_Exception_RecordsError()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => 
            throw new InvalidOperationException("Test error");

        // Act
        var metrics = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);

        // Assert
        Assert.Equal("TestProvider", metrics.ProviderName);
        Assert.True(metrics.IsHealthy); // Still healthy after 1 failure (needs 3 for unhealthy)
        Assert.Equal(1, metrics.ConsecutiveFailures);
        Assert.NotNull(metrics.LastError);
        Assert.Contains("InvalidOperationException", metrics.LastError);
        Assert.Contains("Test error", metrics.LastError);
    }

    [Fact]
    public async Task CheckProviderHealthAsync_Timeout_RecordsTimeoutError()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return true;
        };

        // Act
        var metrics = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);

        // Assert
        Assert.Equal("TestProvider", metrics.ProviderName);
        Assert.True(metrics.IsHealthy); // Still healthy after 1 failure (needs 3 for unhealthy)
        Assert.Equal(1, metrics.ConsecutiveFailures);
        Assert.NotNull(metrics.LastError);
        Assert.Contains("timed out", metrics.LastError);
    }

    [Fact]
    public async Task CheckProviderHealthAsync_ConsecutiveFailures_Increments()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(false);

        // Act - Run 3 failed checks
        var metrics1 = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);
        var metrics2 = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);
        var metrics3 = await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);

        // Assert
        Assert.Equal(1, metrics1.ConsecutiveFailures);
        Assert.Equal(2, metrics2.ConsecutiveFailures);
        Assert.Equal(3, metrics3.ConsecutiveFailures);
        Assert.False(metrics3.IsHealthy); // Should be unhealthy after 3 failures
    }

    [Fact]
    public async Task CheckProviderHealthAsync_FailureThenSuccess_ResetsConsecutiveFailures()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> failCheck = _ => Task.FromResult(false);
        Func<CancellationToken, Task<bool>> successCheck = _ => Task.FromResult(true);

        // Act
        await monitor.CheckProviderHealthAsync("TestProvider", failCheck);
        await monitor.CheckProviderHealthAsync("TestProvider", failCheck);
        var metricsAfterSuccess = await monitor.CheckProviderHealthAsync("TestProvider", successCheck);

        // Assert
        Assert.Equal(0, metricsAfterSuccess.ConsecutiveFailures);
        Assert.True(metricsAfterSuccess.IsHealthy);
        Assert.Null(metricsAfterSuccess.LastError);
    }

    [Fact]
    public void GetProviderHealth_ReturnsNullForUnknownProvider()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);

        // Act
        var metrics = monitor.GetProviderHealth("UnknownProvider");

        // Assert
        Assert.Null(metrics);
    }

    [Fact]
    public async Task GetProviderHealth_ReturnsCachedMetrics()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(true);
        
        // Act
        await monitor.CheckProviderHealthAsync("TestProvider", healthCheck);
        var cachedMetrics = monitor.GetProviderHealth("TestProvider");

        // Assert
        Assert.NotNull(cachedMetrics);
        Assert.Equal("TestProvider", cachedMetrics.ProviderName);
        Assert.True(cachedMetrics.IsHealthy);
    }

    [Fact]
    public async Task GetAllProviderHealth_ReturnsAllRegisteredProviders()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(true);

        // Act
        await monitor.CheckProviderHealthAsync("Provider1", healthCheck);
        await monitor.CheckProviderHealthAsync("Provider2", healthCheck);
        var allMetrics = monitor.GetAllProviderHealth();

        // Assert
        Assert.Equal(2, allMetrics.Count);
        Assert.True(allMetrics.ContainsKey("Provider1"));
        Assert.True(allMetrics.ContainsKey("Provider2"));
    }

    [Fact]
    public async Task SuccessRate_CalculatedCorrectly()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> successCheck = _ => Task.FromResult(true);
        Func<CancellationToken, Task<bool>> failCheck = _ => Task.FromResult(false);

        // Act - 3 successes, 1 failure = 75% success rate
        await monitor.CheckProviderHealthAsync("TestProvider", successCheck);
        await monitor.CheckProviderHealthAsync("TestProvider", successCheck);
        await monitor.CheckProviderHealthAsync("TestProvider", successCheck);
        var metrics = await monitor.CheckProviderHealthAsync("TestProvider", failCheck);

        // Assert
        Assert.Equal(0.75, metrics.SuccessRate, 0.01); // 75% with 1% tolerance
    }

    [Fact]
    public void RegisterHealthCheck_StoresHealthCheckFunction()
    {
        // Arrange
        var monitor = new ProviderHealthMonitor(_logger);
        Func<CancellationToken, Task<bool>> healthCheck = _ => Task.FromResult(true);

        // Act
        monitor.RegisterHealthCheck("TestProvider", healthCheck);
        var metrics = monitor.GetProviderHealth("TestProvider");

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal("TestProvider", metrics.ProviderName);
    }
}
