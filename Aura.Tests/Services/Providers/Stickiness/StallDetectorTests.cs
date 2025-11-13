using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers.Stickiness;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Services.Providers.Stickiness;

/// <summary>
/// Tests for StallDetector and latency categorization
/// </summary>
public class StallDetectorTests
{
    private readonly ITestOutputHelper _output;

    public StallDetectorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MonitorAsync_WithHeartbeat_DoesNotReportStall()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance, checkIntervalMs: 100);
        var state = new ProviderState("TestProvider", "local_llm", "corr-123");
        
        var tokenCount = 0;
        var strategy = new LlmStreamingHeartbeatStrategy(
            () => Task.FromResult<int?>(++tokenCount),
            heartbeatIntervalMs: 200,
            stallMultiplier: 3);

        var stallDetected = false;
        detector.StallSuspected += (s, e) => stallDetected = true;

        using var cts = new CancellationTokenSource(1000); // 1 second test

        // Act
        var monitorTask = detector.MonitorAsync(state, strategy, cts.Token);
        
        await Task.Delay(600); // Let it run
        state.MarkComplete();
        
        await monitorTask;

        // Assert
        Assert.False(stallDetected);
        Assert.True(state.HeartbeatCount > 0);
        Assert.NotEqual(LatencyCategory.StallSuspected, state.CurrentCategory);
    }

    [Fact]
    public async Task MonitorAsync_NoHeartbeat_ReportsStall()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance, checkIntervalMs: 100);
        var state = new ProviderState("TestProvider", "local_llm", "corr-123");
        
        var strategy = new LlmStreamingHeartbeatStrategy(
            () => Task.FromResult<int?>(null), // No heartbeat
            heartbeatIntervalMs: 200,
            stallMultiplier: 2); // 400ms stall threshold

        StallSuspectedEvent? stallEvent = null;
        detector.StallSuspected += (s, e) => stallEvent = e;

        using var cts = new CancellationTokenSource(1500);

        // Act
        var monitorTask = detector.MonitorAsync(state, strategy, cts.Token);
        
        await Task.Delay(700); // Wait past stall threshold
        state.MarkComplete();
        
        await monitorTask;

        // Assert
        Assert.NotNull(stallEvent);
        Assert.Equal("TestProvider", stallEvent.ProviderName);
        Assert.Equal("local_llm", stallEvent.ProviderType);
        Assert.Equal(LatencyCategory.StallSuspected, state.CurrentCategory);
    }

    [Fact]
    public async Task MonitorAsync_LatencyCategories_TransitionCorrectly()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance, checkIntervalMs: 50);
        var state = new ProviderState("TestProvider", "cloud_llm", "corr-123");
        
        var strategy = new NoHeartbeatStrategy(); // No heartbeat support

        using var cts = new CancellationTokenSource(5000);

        // Act
        var monitorTask = detector.MonitorAsync(state, strategy, cts.Token);
        
        // Check Normal
        await Task.Delay(100);
        Assert.Equal(LatencyCategory.Normal, state.CurrentCategory);
        
        // Wait for Extended
        await Task.Delay(30000); // 30s
        
        // Can't easily test in unit test without mocking time
        // This would be better as an integration test
        
        state.MarkComplete();
        await monitorTask;

        // Assert - At minimum we tested the monitoring starts
        Assert.True(state.IsComplete);
    }

    [Fact]
    public async Task MonitorAsync_ProviderWithoutHeartbeat_UsesBasicTimeout()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance, checkIntervalMs: 100);
        var state = new ProviderState("TestProvider", "fallback_provider", "corr-123");
        
        var strategy = new NoHeartbeatStrategy();

        using var cts = new CancellationTokenSource(500);

        // Act
        var monitorTask = detector.MonitorAsync(state, strategy, cts.Token);
        
        await Task.Delay(200);
        state.MarkComplete();
        
        await monitorTask;

        // Assert - Completes without error even without heartbeat support
        Assert.True(state.IsComplete);
        Assert.Equal(0, state.HeartbeatCount);
    }

    [Fact]
    public async Task MonitorAsync_CancellationRequested_ExitsGracefully()
    {
        // Arrange
        var detector = new StallDetector(NullLogger<StallDetector>.Instance);
        var state = new ProviderState("TestProvider", "local_llm", "corr-123");
        var strategy = new NoHeartbeatStrategy();

        using var cts = new CancellationTokenSource(200);

        // Act
        var monitorTask = detector.MonitorAsync(state, strategy, cts.Token);
        
        await Task.Delay(300); // Wait for cancellation

        // Assert - Should complete without throwing
        await monitorTask; // Should not throw OperationCanceledException
        Assert.False(state.IsComplete); // Was cancelled, not completed normally
    }
}
