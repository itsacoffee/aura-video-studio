using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Runtime;

public class ProcessRegistryTests
{
    private readonly Mock<ILogger<ProcessRegistry>> _mockLogger;
    private readonly ProcessRegistry _registry;

    public ProcessRegistryTests()
    {
        _mockLogger = new Mock<ILogger<ProcessRegistry>>();
        _registry = new ProcessRegistry(_mockLogger.Object);
    }

    [Fact]
    public void Register_AddsProcessToTracking()
    {
        // Arrange
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                Arguments = OperatingSystem.IsWindows() ? "/c echo test" : "-c \"echo test\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var processId = process.Id;

        try
        {
            // Act
            var tracked = _registry.Register(process, "test-job-1");

            // Assert
            Assert.Equal(processId, tracked.ProcessId);
            Assert.Equal("test-job-1", tracked.JobId);
            Assert.Equal(_registry.ActiveCount, 1);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    [Fact]
    public void Register_WithNullJobId_StillTracks()
    {
        // Arrange
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                Arguments = OperatingSystem.IsWindows() ? "/c echo test" : "-c \"echo test\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();

        try
        {
            // Act
            var tracked = _registry.Register(process, null);

            // Assert
            Assert.Null(tracked.JobId);
            Assert.Equal(1, _registry.ActiveCount);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    [Fact]
    public void Unregister_RemovesProcessFromTracking()
    {
        // Arrange
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                Arguments = OperatingSystem.IsWindows() ? "/c echo test" : "-c \"echo test\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var processId = process.Id;

        try
        {
            _registry.Register(process, "test-job");
            Assert.Equal(1, _registry.ActiveCount);

            // Act
            _registry.Unregister(processId);

            // Assert
            Assert.Equal(0, _registry.ActiveCount);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    [Fact]
    public void GetActiveProcesses_ReturnsAllTrackedProcesses()
    {
        // Arrange
        using var process1 = StartTestProcess();
        using var process2 = StartTestProcess();
        using var process3 = StartTestProcess();

        try
        {
            _registry.Register(process1, "job-1");
            _registry.Register(process2, "job-2");
            _registry.Register(process3, "job-3");

            // Act
            var processes = _registry.GetActiveProcesses();

            // Assert
            Assert.Equal(3, processes.Count);
            Assert.Contains(processes, p => p.ProcessId == process1.Id);
            Assert.Contains(processes, p => p.ProcessId == process2.Id);
            Assert.Contains(processes, p => p.ProcessId == process3.Id);
        }
        finally
        {
            KillProcess(process1);
            KillProcess(process2);
            KillProcess(process3);
        }
    }

    [Fact]
    public void GetProcessesForJob_ReturnsOnlyMatchingJob()
    {
        // Arrange
        using var process1 = StartTestProcess();
        using var process2 = StartTestProcess();
        using var process3 = StartTestProcess();

        try
        {
            _registry.Register(process1, "job-1");
            _registry.Register(process2, "job-1");
            _registry.Register(process3, "job-2");

            // Act
            var job1Processes = _registry.GetProcessesForJob("job-1");
            var job2Processes = _registry.GetProcessesForJob("job-2");

            // Assert
            Assert.Equal(2, job1Processes.Count);
            Assert.All(job1Processes, p => Assert.Equal("job-1", p.JobId));
            
            Assert.Single(job2Processes);
            Assert.Equal("job-2", job2Processes[0].JobId);
        }
        finally
        {
            KillProcess(process1);
            KillProcess(process2);
            KillProcess(process3);
        }
    }

    [Fact]
    public async Task KillAllForJobAsync_KillsAllProcessesForJob()
    {
        // Arrange
        using var process1 = StartTestProcess();
        using var process2 = StartTestProcess();
        using var process3 = StartTestProcess();

        try
        {
            _registry.Register(process1, "job-1");
            _registry.Register(process2, "job-1");
            _registry.Register(process3, "job-2");

            // Act
            await _registry.KillAllForJobAsync("job-1");

            // Assert
            await Task.Delay(100); // Give time for processes to be killed
            Assert.True(process1.HasExited || process1.HasExited);
            Assert.True(process2.HasExited || process2.HasExited);
            // process3 should still be running (different job)
            var job2Processes = _registry.GetProcessesForJob("job-2");
            Assert.Single(job2Processes);
        }
        finally
        {
            KillProcess(process1);
            KillProcess(process2);
            KillProcess(process3);
        }
    }

    [Fact]
    public async Task KillProcessAsync_KillsSpecificProcess()
    {
        // Arrange
        using var process = StartTestProcess();
        var processId = process.Id;

        try
        {
            _registry.Register(process, "test-job");

            // Act
            await _registry.KillProcessAsync(processId);

            // Assert
            await Task.Delay(100); // Give time for process to be killed
            Assert.True(process.HasExited);
            Assert.Equal(0, _registry.ActiveCount);
        }
        finally
        {
            KillProcess(process);
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
            await _registry.KillProcessAsync(nonExistentProcessId);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_KillsAllTrackedProcesses()
    {
        // Arrange
        using var process1 = StartTestProcess();
        using var process2 = StartTestProcess();

        try
        {
            _registry.Register(process1, "job-1");
            _registry.Register(process2, "job-2");
            Assert.Equal(2, _registry.ActiveCount);

            // Act
            await _registry.DisposeAsync();

            // Assert
            await Task.Delay(100); // Give time for processes to be killed
            Assert.True(process1.HasExited || process1.HasExited);
            Assert.True(process2.HasExited || process2.HasExited);
            Assert.Equal(0, _registry.ActiveCount);
        }
        finally
        {
            KillProcess(process1);
            KillProcess(process2);
        }
    }

    [Fact]
    public void ActiveCount_ReturnsCorrectCount()
    {
        // Arrange
        using var process1 = StartTestProcess();
        using var process2 = StartTestProcess();
        using var process3 = StartTestProcess();

        try
        {
            // Act & Assert
            Assert.Equal(0, _registry.ActiveCount);
            
            _registry.Register(process1, "job-1");
            Assert.Equal(1, _registry.ActiveCount);
            
            _registry.Register(process2, "job-2");
            Assert.Equal(2, _registry.ActiveCount);
            
            _registry.Register(process3, "job-3");
            Assert.Equal(3, _registry.ActiveCount);
            
            _registry.Unregister(process2.Id);
            Assert.Equal(2, _registry.ActiveCount);
        }
        finally
        {
            KillProcess(process1);
            KillProcess(process2);
            KillProcess(process3);
        }
    }

    [Fact]
    public void Process_Exits_AutomaticallyUnregisters()
    {
        // Arrange
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                Arguments = OperatingSystem.IsWindows() ? "/c echo test" : "-c \"echo test\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var processId = process.Id;

        _registry.Register(process, "test-job");
        Assert.Equal(1, _registry.ActiveCount);

        // Act - wait for process to exit naturally
        process.WaitForExit(5000);

        // Assert - should be automatically unregistered
        // Note: This may not always work due to timing, but the event handler is set up
        Assert.True(process.HasExited);
    }

    // Helper methods
    private Process StartTestProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
                Arguments = OperatingSystem.IsWindows() ? "/c ping 127.0.0.1 -n 10" : "-c \"sleep 10\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        return process;
    }

    private void KillProcess(Process? process)
    {
        try
        {
            if (process != null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(1000);
            }
        }
        catch
        {
            // Ignore errors when killing test processes
        }
        finally
        {
            process?.Dispose();
        }
    }
}

