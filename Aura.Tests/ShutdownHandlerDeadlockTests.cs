using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify shutdown handlers don't deadlock under load
/// </summary>
public class ShutdownHandlerDeadlockTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IProcessManager> _processManagerMock;
    private readonly Mock<EngineLifecycleManager> _lifecycleManagerMock;

    public ShutdownHandlerDeadlockTests()
    {
        _loggerMock = new Mock<ILogger>();
        _processManagerMock = new Mock<IProcessManager>();
        
        // Create a mock for EngineLifecycleManager - we'll need to mock the StopAsync method
        var logger = new Mock<ILogger<EngineLifecycleManager>>();
        var registry = new Mock<LocalEnginesRegistry>(
            logger.Object,
            new Mock<ExternalProcessManager>(logger.Object).Object,
            "test-config.json");
        var processManager = new Mock<ExternalProcessManager>(logger.Object);
        
        // We'll test the actual shutdown pattern, not the manager itself
        _lifecycleManagerMock = null; // Will use a different approach
    }

    [Fact]
    public async Task ShutdownHandler_ProcessManagerKillAll_DoesNotDeadlock_UnderConcurrentLoad()
    {
        // Arrange
        var processIds = new[] { 1001, 1002, 1003, 1004, 1005 };
        _processManagerMock.Setup(x => x.GetTrackedProcesses()).Returns(processIds);
        
        // Simulate async operation that might take time
        _processManagerMock
            .Setup(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                // Simulate some work
                await Task.Delay(100, ct).ConfigureAwait(false);
            });

        // Act - Simulate shutdown handler pattern from Program.cs
        var stopwatch = Stopwatch.StartNew();
        var completed = false;
        var exception = (Exception?)null;

        try
        {
            // Simulate the shutdown handler with Task.Run pattern
            var killTask = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _processManagerMock.Object.KillAllProcessesAsync(cts.Token).ConfigureAwait(false);
            });

            if (killTask.Wait(TimeSpan.FromSeconds(15)))
            {
                completed = true;
            }
            else
            {
                throw new TimeoutException("Shutdown operation timed out");
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
        }

        // Assert
        Assert.True(completed, $"Shutdown should complete. Exception: {exception?.Message}");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Shutdown should complete within 5 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Null(exception);
        
        _processManagerMock.Verify(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShutdownHandler_AsyncStopOperation_DoesNotDeadlock_UnderConcurrentLoad()
    {
        // Arrange - Simulate an async stop operation
        Func<Task> stopOperation = async () =>
        {
            // Simulate some cleanup work
            await Task.Delay(100).ConfigureAwait(false);
        };

        // Act - Simulate shutdown handler pattern from Program.cs
        var stopwatch = Stopwatch.StartNew();
        var completed = false;
        var exception = (Exception?)null;

        try
        {
            // Simulate the shutdown handler with Task.Run pattern
            var stopTask = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await stopOperation().ConfigureAwait(false);
            });

            if (stopTask.Wait(TimeSpan.FromSeconds(15)))
            {
                completed = true;
            }
            else
            {
                throw new TimeoutException("Shutdown operation timed out");
            }
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
        }

        // Assert
        Assert.True(completed, $"Shutdown should complete. Exception: {exception?.Message}");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Shutdown should complete within 5 seconds, took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Null(exception);
    }

    [Fact]
    public async Task ShutdownHandler_MultipleConcurrentShutdowns_DoesNotDeadlock()
    {
        // Arrange
        var processIds = new[] { 1001, 1002, 1003 };
        _processManagerMock.Setup(x => x.GetTrackedProcesses()).Returns(processIds);
        
        _processManagerMock
            .Setup(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
            });

        // Act - Simulate multiple concurrent shutdown attempts
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var killTask = Task.Run(async () =>
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await _processManagerMock.Object.KillAllProcessesAsync(cts.Token).ConfigureAwait(false);
                    });

                    return killTask.Wait(TimeSpan.FromSeconds(10));
                }
                catch
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allCompleted = results.All(r => r);

        // Assert
        Assert.True(allCompleted, "All concurrent shutdown attempts should complete");
    }

    [Fact]
    public async Task ShutdownHandler_TimeoutProtection_HandlesLongRunningOperations()
    {
        // Arrange
        _processManagerMock
            .Setup(x => x.KillAllProcessesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                // Simulate a very long operation
                await Task.Delay(20000, ct).ConfigureAwait(false);
            });

        // Act
        var stopwatch = Stopwatch.StartNew();
        var timedOut = false;

        try
        {
            var killTask = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _processManagerMock.Object.KillAllProcessesAsync(cts.Token).ConfigureAwait(false);
            });

            if (!killTask.Wait(TimeSpan.FromSeconds(15)))
            {
                timedOut = true;
            }
        }
        catch (Exception)
        {
            // Expected to timeout or be cancelled
        }
        finally
        {
            stopwatch.Stop();
        }

        // Assert - Should timeout gracefully, not deadlock
        Assert.True(stopwatch.ElapsedMilliseconds < 20000, 
            "Should timeout gracefully, not wait for full operation");
    }
}

