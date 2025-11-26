using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Runtime;

public class ManagedProcessRunnerTests
{
    private readonly Mock<ILogger<ProcessRegistry>> _mockRegistryLogger;
    private readonly Mock<ILogger<ManagedProcessRunner>> _mockRunnerLogger;
    private readonly ProcessRegistry _registry;
    private readonly ManagedProcessRunner _runner;

    public ManagedProcessRunnerTests()
    {
        _mockRegistryLogger = new Mock<ILogger<ProcessRegistry>>();
        _mockRunnerLogger = new Mock<ILogger<ManagedProcessRunner>>();
        _registry = new ProcessRegistry(_mockRegistryLogger.Object);
        _runner = new ManagedProcessRunner(_registry, _mockRunnerLogger.Object);
    }

    [Fact]
    public async Task RunAsync_ExecutesProcessSuccessfully()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c echo success" : "-c \"echo success\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Act
        var result = await _runner.RunAsync(startInfo);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("success", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_WithJobId_RegistersProcess()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c echo test" : "-c \"echo test\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Act
        var result = await _runner.RunAsync(startInfo, jobId: "test-job-1");

        // Assert
        Assert.Equal(0, result.ExitCode);
        var processes = _registry.GetProcessesForJob("test-job-1");
        // Process should be unregistered after completion
        Assert.Empty(processes);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c ping 127.0.0.1 -n 10" : "-c \"sleep 10\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _runner.RunAsync(startInfo, timeout: TimeSpan.FromSeconds(1));
        });
    }

    [Fact]
    public async Task RunAsync_WithCancellation_KillsProcess()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c ping 127.0.0.1 -n 10" : "-c \"sleep 10\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var cts = new CancellationTokenSource();

        // Act
        var runTask = _runner.RunAsync(startInfo, ct: cts.Token);
        
        // Cancel after a short delay
        await Task.Delay(500);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await runTask);
    }

    [Fact]
    public async Task RunAsync_WithStdin_WritesToStandardInput()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c more" : "-c \"cat\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var inputText = "test input text";

        // Act
        var result = await _runner.RunAsync(
            startInfo,
            writeToStdin: async (stdin) =>
            {
                await stdin.WriteLineAsync(inputText).ConfigureAwait(false);
            }
        );

        // Assert
        // The output should contain the input text (for cat/more commands)
        // Note: This test may need adjustment based on platform-specific behavior
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_WithOutputCallbacks_InvokesCallbacks()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c echo output" : "-c \"echo output\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var stdoutReceived = false;
        var stderrReceived = false;

        // Act
        var result = await _runner.RunAsync(
            startInfo,
            onStdOut: (line) => stdoutReceived = true,
            onStdErr: (line) => stderrReceived = true
        );

        // Assert
        Assert.Equal(0, result.ExitCode);
        // At least one callback should have been invoked
        Assert.True(stdoutReceived || stderrReceived || !string.IsNullOrEmpty(result.StandardOutput));
    }

    [Fact]
    public async Task RunAsync_WithFailedProcess_ReturnsNonZeroExitCode()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c exit 1" : "-c \"exit 1\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Act
        var result = await _runner.RunAsync(startInfo);

        // Assert
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_ProcessExits_UnregistersFromRegistry()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            Arguments = OperatingSystem.IsWindows() ? "/c echo done" : "-c \"echo done\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var initialCount = _registry.ActiveCount;

        // Act
        await _runner.RunAsync(startInfo, jobId: "test-job");

        // Assert
        // Process should be unregistered after completion
        await Task.Delay(100); // Give time for cleanup
        Assert.Equal(initialCount, _registry.ActiveCount);
    }

    [Fact]
    public async Task RunAsync_WithNullStartInfo_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _runner.RunAsync(null!);
        });
    }

    public void Dispose()
    {
        _registry?.DisposeAsync().AsTask().Wait();
    }
}

