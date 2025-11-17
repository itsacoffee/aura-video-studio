using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Services.FFmpeg;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for shutdown behavior with active SSE connections
/// Addresses requirements from PR #372: "Verify shutdown behavior with active SSE connections"
/// </summary>
public class ShutdownWithSseTests
{
    private readonly Mock<ILogger<ShutdownOrchestrator>> _loggerMock;
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly Mock<IProcessManager> _processManagerMock;
    private readonly ShutdownOrchestrator _orchestrator;

    public ShutdownWithSseTests()
    {
        _loggerMock = new Mock<ILogger<ShutdownOrchestrator>>();
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _processManagerMock = new Mock<IProcessManager>();
        
        _orchestrator = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
    }

    [Fact]
    public async Task InitiateShutdown_WithActiveSseConnections_NotifiesAndCloses()
    {
        // Arrange
        var mockResponses = new List<Mock<HttpResponse>>();
        for (int i = 0; i < 3; i++)
        {
            var mockResponse = new Mock<HttpResponse>();
            mockResponses.Add(mockResponse);
            _orchestrator.RegisterSseConnection($"conn-{i}", mockResponse.Object);
        }
        
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _orchestrator.InitiateShutdownAsync(force: false);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        
        // Verify shutdown completed quickly (under 3 seconds as per PR 372 goals)
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
            $"Shutdown with SSE connections should complete in under 3 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify connections were cleared
        var status = _orchestrator.GetStatus();
        Assert.Equal(0, status.ActiveConnections);
    }

    [Fact]
    public async Task InitiateShutdown_MultipleActiveSseConnections_CompletesWithinTimeout()
    {
        // Arrange - register many connections
        for (int i = 0; i < 10; i++)
        {
            var mockResponse = new Mock<HttpResponse>();
            _orchestrator.RegisterSseConnection($"conn-{i}", mockResponse.Object);
        }
        
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _orchestrator.InitiateShutdownAsync(force: false);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        
        // Even with 10 connections, should complete quickly
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Shutdown should complete in under 2 seconds with optimized timeouts, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task InitiateShutdown_NoSseConnections_CompletesQuickly()
    {
        // Arrange
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _orchestrator.InitiateShutdownAsync(force: false);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        
        // Without SSE connections, should be even faster
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Shutdown without SSE should complete in under 1 second, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task InitiateShutdown_ForceMode_IgnoresConnectionDelay()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var mockResponse = new Mock<HttpResponse>();
            _orchestrator.RegisterSseConnection($"conn-{i}", mockResponse.Object);
        }
        
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _orchestrator.InitiateShutdownAsync(force: true);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        
        // Force mode should be very fast (no delays for notifications)
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Force shutdown should complete in under 500ms, took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task RapidRestart_ShutdownThenRegisterNewConnection_Works()
    {
        // Arrange
        var mockResponse1 = new Mock<HttpResponse>();
        _orchestrator.RegisterSseConnection("conn-1", mockResponse1.Object);
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act - shutdown
        var result1 = await _orchestrator.InitiateShutdownAsync(force: false);
        Assert.True(result1.Success);

        // Simulate rapid restart - create new orchestrator (simulating new app instance)
        var newOrchestrator = new ShutdownOrchestrator(
            _loggerMock.Object,
            _lifetimeMock.Object,
            _processManagerMock.Object);
        
        var mockResponse2 = new Mock<HttpResponse>();
        newOrchestrator.RegisterSseConnection("conn-2", mockResponse2.Object);

        // Act - shutdown again
        var result2 = await newOrchestrator.InitiateShutdownAsync(force: false);

        // Assert
        Assert.True(result2.Success);
    }

    [Fact]
    public void GetStatus_AfterShutdownWithSse_ShowsCorrectState()
    {
        // Arrange
        var mockResponse = new Mock<HttpResponse>();
        _orchestrator.RegisterSseConnection("conn-1", mockResponse.Object);
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act
        var statusBefore = _orchestrator.GetStatus();
        var shutdownTask = _orchestrator.InitiateShutdownAsync(force: false);
        shutdownTask.Wait();
        var statusAfter = _orchestrator.GetStatus();

        // Assert
        Assert.Equal(1, statusBefore.ActiveConnections);
        Assert.False(statusBefore.ShutdownInitiated);
        
        Assert.Equal(0, statusAfter.ActiveConnections);
        Assert.True(statusAfter.ShutdownInitiated);
    }

    [Fact]
    public async Task InitiateShutdown_ConcurrentSseRegistration_HandlesGracefully()
    {
        // Arrange
        var mockResponse = new Mock<HttpResponse>();
        _orchestrator.RegisterSseConnection("conn-1", mockResponse.Object);
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act - start shutdown and try to register new connection concurrently
        var shutdownTask = _orchestrator.InitiateShutdownAsync(force: false);
        
        // Try to register connection during shutdown (may or may not succeed depending on timing)
        var mockResponse2 = new Mock<HttpResponse>();
        _orchestrator.RegisterSseConnection("conn-2", mockResponse2.Object);
        
        var result = await shutdownTask;

        // Assert - should complete without error
        Assert.True(result.Success);
    }

    [Fact]
    public async Task InitiateShutdown_WithSseAndProcesses_CompletesInExpectedTime()
    {
        // Arrange - both SSE connections and processes
        for (int i = 0; i < 3; i++)
        {
            var mockResponse = new Mock<HttpResponse>();
            _orchestrator.RegisterSseConnection($"conn-{i}", mockResponse.Object);
        }
        
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(2);
        _processManagerMock.Setup(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _orchestrator.InitiateShutdownAsync(force: false);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        
        // With both SSE and processes, should still be under 3 seconds (PR 372 target)
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, 
            $"Shutdown with SSE and processes should complete in under 3 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        
        _processManagerMock.Verify(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void UnregisterSseConnection_DuringShutdown_DoesNotCrash()
    {
        // Arrange
        var mockResponse = new Mock<HttpResponse>();
        _orchestrator.RegisterSseConnection("conn-1", mockResponse.Object);
        _processManagerMock.Setup(x => x.GetProcessCount()).Returns(0);

        // Act - start shutdown and unregister connection
        var shutdownTask = _orchestrator.InitiateShutdownAsync(force: false);
        _orchestrator.UnregisterSseConnection("conn-1");
        
        // Assert - should complete without exception
        shutdownTask.Wait(5000);
        Assert.True(shutdownTask.IsCompleted, "Shutdown should complete even with concurrent unregister");
    }
}
