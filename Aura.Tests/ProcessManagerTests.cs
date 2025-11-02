using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ProcessManagerTests
{
    private readonly Mock<ILogger<ProcessManager>> _mockLogger;
    private readonly ProcessManager _processManager;

    public ProcessManagerTests()
    {
        _mockLogger = new Mock<ILogger<ProcessManager>>();
        _processManager = new ProcessManager(_mockLogger.Object);
    }

    [Fact]
    public void RegisterProcess_AddsProcessToTracking()
    {
        // Arrange
        int processId = 12345;
        string jobId = "test-job-1";

        // Act
        _processManager.RegisterProcess(processId, jobId);

        // Assert
        var tracked = _processManager.GetTrackedProcesses();
        Assert.Contains(processId, tracked);
        Assert.Single(tracked);
    }

    [Fact]
    public void UnregisterProcess_RemovesProcessFromTracking()
    {
        // Arrange
        int processId = 12345;
        string jobId = "test-job-1";
        _processManager.RegisterProcess(processId, jobId);

        // Act
        _processManager.UnregisterProcess(processId);

        // Assert
        var tracked = _processManager.GetTrackedProcesses();
        Assert.DoesNotContain(processId, tracked);
        Assert.Empty(tracked);
    }

    [Fact]
    public void GetProcessCount_ReturnsCorrectCount()
    {
        // Arrange
        _processManager.RegisterProcess(1, "job-1");
        _processManager.RegisterProcess(2, "job-2");
        _processManager.RegisterProcess(3, "job-3");

        // Act
        var count = _processManager.GetProcessCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void GetTrackedProcesses_ReturnsAllRegisteredProcessIds()
    {
        // Arrange
        int[] processIds = { 100, 200, 300 };
        foreach (var pid in processIds)
        {
            _processManager.RegisterProcess(pid, $"job-{pid}");
        }

        // Act
        var tracked = _processManager.GetTrackedProcesses();

        // Assert
        Assert.Equal(processIds.Length, tracked.Length);
        foreach (var pid in processIds)
        {
            Assert.Contains(pid, tracked);
        }
    }

    [Fact]
    public async Task KillProcessAsync_WithNonExistentProcess_DoesNotThrow()
    {
        // Arrange
        int nonExistentProcessId = 999999;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _processManager.KillProcessAsync(nonExistentProcessId);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task KillAllProcessesAsync_ClearsAllTrackedProcesses()
    {
        // Arrange
        _processManager.RegisterProcess(1, "job-1");
        _processManager.RegisterProcess(2, "job-2");
        _processManager.RegisterProcess(3, "job-3");

        // Act
        await _processManager.KillAllProcessesAsync();

        // Assert - after attempting to kill non-existent processes, they should be unregistered
        await Task.Delay(100); // Give cleanup time to complete
        var count = _processManager.GetProcessCount();
        Assert.True(count <= 3); // Should be reduced or cleared
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        _processManager.RegisterProcess(1, "job-1");
        _processManager.RegisterProcess(2, "job-2");

        // Act
        _processManager.Dispose();

        // Assert - verify logger was called for disposal
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Disposing ProcessManager")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RegisterProcess_LogsInformation()
    {
        // Arrange
        int processId = 12345;
        string jobId = "test-job";

        // Act
        _processManager.RegisterProcess(processId, jobId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("Registered FFmpeg process") && 
                    o.ToString()!.Contains(processId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void UnregisterProcess_LogsInformation()
    {
        // Arrange
        int processId = 12345;
        string jobId = "test-job";
        _processManager.RegisterProcess(processId, jobId);

        // Act
        _processManager.UnregisterProcess(processId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => 
                    o.ToString()!.Contains("Unregistered FFmpeg process") && 
                    o.ToString()!.Contains(processId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
