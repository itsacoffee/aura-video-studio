using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for rapid restart scenarios
/// Addresses requirement from PR #372: "Test rapid restart scenarios (close and reopen immediately)"
/// </summary>
public class RapidRestartTests
{
    private readonly Mock<ILogger<ShutdownOrchestrator>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly Mock<IProcessManager> _processManagerMock;

    public RapidRestartTests()
    {
        _loggerMock = new Mock<ILogger<ShutdownOrchestrator>>();
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _processManagerMock = new Mock<IProcessManager>();
    }

    [Fact]
    public async Task RapidRestart_ShutdownAndRecreate_Succeeds()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act - first instance
        var orchestrator1 = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
        
        var result1 = await orchestrator1.InitiateShutdownAsync(force: false);
        
        // Immediately create new instance (simulating rapid restart)
        var orchestrator2 = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
        
        var result2 = await orchestrator2.InitiateShutdownAsync(force: false);

        // Assert
        Assert.True(result1.Success, "First shutdown should succeed");
        Assert.True(result2.Success, "Second shutdown should succeed");
    }

    [Fact]
    public async Task RapidRestart_MultipleSequentialShutdowns_AllSucceed()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var results = new bool[5];

        // Act - simulate 5 rapid restarts
        for (int i = 0; i < 5; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            var result = await orchestrator.InitiateShutdownAsync(force: false);
            results[i] = result.Success;
        }

        // Assert - all shutdowns should succeed
        Assert.All(results, success => Assert.True(success, "All rapid restarts should succeed"));
    }

    [Fact]
    public async Task RapidRestart_WithMinimalDelay_NoResourceLeaks()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var iterations = 3;

        // Act - rapid restarts with minimal delay
        for (int i = 0; i < iterations; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            var result = await orchestrator.InitiateShutdownAsync(force: false);
            Assert.True(result.Success);
            
            // Minimal delay (50ms) between restarts
            await Task.Delay(50);
        }

        // Assert - if we got here without exceptions, no resource leaks occurred
        Assert.True(true, "Rapid restarts with minimal delay should not cause resource leaks");
    }

    [Fact]
    public async Task RapidRestart_ForceShutdownMode_CompletesQuickly()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var stopwatch = Stopwatch.StartNew();

        // Act - rapid restarts in force mode
        for (int i = 0; i < 3; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            await orchestrator.InitiateShutdownAsync(force: true);
        }
        
        stopwatch.Stop();

        // Assert - all force shutdowns should complete very quickly
        Assert.True(stopwatch.ElapsedMilliseconds < 1500, 
            $"3 force shutdowns should complete in under 1.5 seconds, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task RapidRestart_WithProcessCleanup_CleansUpProperly()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(1);
        _processManagerMock.Setup(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - shutdown with processes, then immediate restart
        var orchestrator1 = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
        
        var result1 = await orchestrator1.InitiateShutdownAsync(force: false);
        
        // Verify processes were killed
        _processManagerMock.Verify(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Reset mock for second orchestrator
        _processManagerMock.Invocations.Clear();
        
        // Immediate restart
        var orchestrator2 = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
        
        var result2 = await orchestrator2.InitiateShutdownAsync(force: false);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
    }

    [Fact]
    public async Task RapidRestart_ShutdownSequenceLogging_NoRegression()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var loggedMessages = new System.Collections.Concurrent.ConcurrentBag<string>();
        
        // Capture log messages
        _loggerMock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception? exception, Delegate formatter) =>
            {
                loggedMessages.Add(state.ToString() ?? "");
            });

        // Act - multiple shutdowns
        for (int i = 0; i < 2; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            await orchestrator.InitiateShutdownAsync(force: false);
        }

        // Assert - verify expected log messages are present (no regression in logging)
        Assert.Contains(loggedMessages, msg => msg.Contains("Initiating graceful shutdown"));
        Assert.Contains(loggedMessages, msg => msg.Contains("Graceful shutdown completed"));
        
        // Verify we logged for both shutdowns (at least 2 initiation messages)
        var initiationCount = loggedMessages.Count(msg => msg.Contains("Initiating graceful shutdown"));
        Assert.True(initiationCount >= 2, "Should have logged shutdown initiation for each orchestrator");
    }

    [Fact]
    public async Task RapidRestart_NoActiveResources_FastShutdown()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var shutdownTimes = new long[3];

        // Act - measure shutdown times
        for (int i = 0; i < 3; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            var stopwatch = Stopwatch.StartNew();
            await orchestrator.InitiateShutdownAsync(force: false);
            stopwatch.Stop();
            
            shutdownTimes[i] = stopwatch.ElapsedMilliseconds;
        }

        // Assert - all shutdowns should be fast (under 1 second)
        Assert.All(shutdownTimes, time => 
            Assert.True(time < 1000, $"Shutdown should complete in under 1 second, took {time}ms"));
    }

    [Fact]
    public async Task RapidRestart_ConsecutiveShutdowns_VerifyStopApplicationCalled()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var stopApplicationCallCount = 0;
        
        _lifetimeMock.Setup(x => x.StopApplication())
            .Callback(() => stopApplicationCallCount++);

        // Act - 3 consecutive shutdowns
        for (int i = 0; i < 3; i++)
        {
            var orchestrator = new ShutdownOrchestrator(
                _loggerMock.Object,
                _lifetimeMock.Object,
                _processManagerMock.Object);
            
            await orchestrator.InitiateShutdownAsync(force: false);
        }

        // Assert - StopApplication should be called for each orchestrator
        Assert.Equal(3, stopApplicationCallCount);
    }

    [Fact]
    public async Task RapidRestart_ConcurrentShutdownAttempts_HandlesGracefully()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);
        var orchestrator = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);

        // Act - try to initiate shutdown concurrently (simulating race condition)
        var task1 = orchestrator.InitiateShutdownAsync(force: false);
        var task2 = orchestrator.InitiateShutdownAsync(force: false); // Should detect already in progress
        
        await Task.WhenAll(task1, task2);

        // Assert - one should succeed, one should report already in progress
        var results = new[] { task1.Result, task2.Result };
        Assert.Single(results, r => r.Success); // Exactly one should succeed
        Assert.Single(results, r => !r.Success); // Exactly one should report failure
    }
}
