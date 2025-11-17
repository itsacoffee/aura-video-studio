using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Orchestrates graceful shutdown of the application
/// Coordinates termination of background services, SSE connections, and child processes
/// </summary>
public class ShutdownOrchestrator
{
    private readonly ILogger<ShutdownOrchestrator> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IProcessManager? _ffmpegProcessManager;
    private readonly ConcurrentBag<int> _childProcessIds = new();
    private readonly ConcurrentDictionary<string, HttpResponse> _activeConnections = new();
    private bool _shutdownInitiated;
    private readonly object _shutdownLock = new();

    private const int GracefulTimeoutSeconds = 2;  // Reduced from 3 for faster shutdown
    private const int ComponentTimeoutSeconds = 1;  // Reduced from 2 for faster shutdown

    public ShutdownOrchestrator(
        ILogger<ShutdownOrchestrator> logger,
        IHostApplicationLifetime lifetime,
        IProcessManager? ffmpegProcessManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _ffmpegProcessManager = ffmpegProcessManager;
        
        if (_ffmpegProcessManager != null)
        {
            _logger.LogInformation("ShutdownOrchestrator initialized with FFmpeg ProcessManager");
        }
        else
        {
            _logger.LogWarning("ShutdownOrchestrator initialized WITHOUT FFmpeg ProcessManager - FFmpeg processes may not be tracked");
        }
    }

    /// <summary>
    /// Register a child process for tracking
    /// </summary>
    public void RegisterChildProcess(int processId)
    {
        _childProcessIds.Add(processId);
        _logger.LogInformation("Registered child process {ProcessId}", processId);
    }

    /// <summary>
    /// Unregister a child process when it exits normally
    /// </summary>
    public void UnregisterChildProcess(int processId)
    {
        _logger.LogInformation("Unregistered child process {ProcessId}", processId);
    }

    /// <summary>
    /// Register an active SSE connection for notification on shutdown
    /// </summary>
    public void RegisterSseConnection(string connectionId, HttpResponse response)
    {
        _activeConnections.TryAdd(connectionId, response);
        _logger.LogDebug("Registered SSE connection {ConnectionId}", connectionId);
    }

    /// <summary>
    /// Unregister an SSE connection when it closes normally
    /// </summary>
    public void UnregisterSseConnection(string connectionId)
    {
        _activeConnections.TryRemove(connectionId, out _);
        _logger.LogDebug("Unregistered SSE connection {ConnectionId}", connectionId);
    }

    /// <summary>
    /// Initiate graceful shutdown sequence
    /// </summary>
    public async Task<ShutdownResult> InitiateShutdownAsync(bool force = false)
    {
        lock (_shutdownLock)
        {
            if (_shutdownInitiated)
            {
                _logger.LogWarning("Shutdown already initiated");
                return new ShutdownResult
                {
                    Success = false,
                    Message = "Shutdown already in progress"
                };
            }
            _shutdownInitiated = true;
        }

        var shutdownStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        
        _logger.LogInformation("=================================================================");
        _logger.LogInformation("Initiating graceful shutdown (Force: {Force}, PID: {ProcessId})", force, processId);
        _logger.LogInformation("=================================================================");

        var result = new ShutdownResult { Success = true };
        var stepResults = new List<string>();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(force ? 1 : GracefulTimeoutSeconds));

            // Step 1: Notify active SSE connections
            var step1Start = System.Diagnostics.Stopwatch.StartNew();
            var sseStep = await NotifySseConnectionsAsync(cts.Token).ConfigureAwait(false);
            step1Start.Stop();
            stepResults.Add($"SSE Notification: {sseStep}");
            _logger.LogInformation("Step 1/4 Complete: {Result} (Elapsed: {ElapsedMs}ms)", sseStep, step1Start.ElapsedMilliseconds);

            // Step 2: Close SSE connections gracefully
            var step2Start = System.Diagnostics.Stopwatch.StartNew();
            var closeStep = await CloseSseConnectionsAsync(cts.Token).ConfigureAwait(false);
            step2Start.Stop();
            stepResults.Add($"SSE Closure: {closeStep}");
            _logger.LogInformation("Step 2/4 Complete: {Result} (Elapsed: {ElapsedMs}ms)", closeStep, step2Start.ElapsedMilliseconds);

            // Step 3: Terminate FFmpeg processes
            var step3Start = System.Diagnostics.Stopwatch.StartNew();
            var ffmpegStep = await TerminateFFmpegProcessesAsync(force, cts.Token).ConfigureAwait(false);
            step3Start.Stop();
            stepResults.Add($"FFmpeg Termination: {ffmpegStep}");
            _logger.LogInformation("Step 3/4 Complete: {Result} (Elapsed: {ElapsedMs}ms)", ffmpegStep, step3Start.ElapsedMilliseconds);

            // Step 4: Terminate other child processes
            var step4Start = System.Diagnostics.Stopwatch.StartNew();
            var processStep = await TerminateChildProcessesAsync(force, cts.Token).ConfigureAwait(false);
            step4Start.Stop();
            stepResults.Add($"Process Termination: {processStep}");
            _logger.LogInformation("Step 4/4 Complete: {Result} (Elapsed: {ElapsedMs}ms)", processStep, step4Start.ElapsedMilliseconds);

            result.Message = string.Join("; ", stepResults);
            
            shutdownStopwatch.Stop();
            _logger.LogInformation("=================================================================");
            _logger.LogInformation("Graceful shutdown completed successfully (Total: {ElapsedMs}ms)", shutdownStopwatch.ElapsedMilliseconds);
            _logger.LogInformation("=================================================================");

            // Signal application to stop with minimal delay for faster shutdown
            if (!force)
            {
                await Task.Delay(50, CancellationToken.None).ConfigureAwait(false); // Reduced from 200ms to 50ms
            }
            
            _logger.LogInformation("Calling IHostApplicationLifetime.StopApplication()...");
            _lifetime.StopApplication();
            _logger.LogInformation("StopApplication() called - host shutdown initiated");
        }
        catch (Exception ex)
        {
            shutdownStopwatch.Stop();
            _logger.LogError(ex, "Error during shutdown sequence (Elapsed: {ElapsedMs}ms)", shutdownStopwatch.ElapsedMilliseconds);
            result.Success = false;
            result.Message = $"Shutdown error: {ex.Message}";
            result.Details = stepResults;

            if (force)
            {
                _logger.LogWarning("Force shutdown requested, calling StopApplication() despite error");
                _lifetime.StopApplication();
            }
        }

        return result;
    }

    private async Task<string> NotifySseConnectionsAsync(CancellationToken ct)
    {
        var connectionCount = _activeConnections.Count;
        if (connectionCount == 0)
        {
            return "No active connections";
        }

        _logger.LogInformation("Notifying {Count} active SSE connections of shutdown", connectionCount);

        var notificationTasks = _activeConnections.Select(async kvp =>
        {
            try
            {
                var message = "event: shutdown\ndata: {\"message\":\"Server shutting down\"}\n\n";
                await kvp.Value.WriteAsync(message, ct).ConfigureAwait(false);
                await kvp.Value.Body.FlushAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to notify SSE connection {ConnectionId}", kvp.Key);
            }
        });

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500)); // Reduced from 1 second for faster shutdown

        try
        {
            await Task.WhenAll(notificationTasks).ConfigureAwait(false);
            return $"Notified {connectionCount} connections";
        }
        catch (OperationCanceledException)
        {
            return $"Notified {connectionCount} connections (timeout)";
        }
    }

    private async Task<string> CloseSseConnectionsAsync(CancellationToken ct)
    {
        var connectionCount = _activeConnections.Count;
        if (connectionCount == 0)
        {
            return "No connections to close";
        }

        _logger.LogInformation("Closing {Count} SSE connections", connectionCount);

        // Reduced delay from 200ms to 100ms for faster shutdown
        await Task.Delay(100, ct).ConfigureAwait(false);

        _activeConnections.Clear();
        return $"Closed {connectionCount} connections";
    }

    private async Task<string> TerminateFFmpegProcessesAsync(bool force, CancellationToken ct)
    {
        if (_ffmpegProcessManager == null)
        {
            return "No FFmpeg ProcessManager available";
        }

        var processCount = _ffmpegProcessManager.GetProcessCount();
        if (processCount == 0)
        {
            return "No FFmpeg processes";
        }

        _logger.LogInformation("Terminating {Count} FFmpeg processes (Force: {Force})", processCount, force);

        try
        {
            await _ffmpegProcessManager.KillAllProcessesAsync(ct).ConfigureAwait(false);
            return $"Terminated {processCount} FFmpeg process(es)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating FFmpeg processes");
            return $"FFmpeg termination error: {ex.Message}";
        }
    }

    private async Task<string> TerminateChildProcessesAsync(bool force, CancellationToken ct)
    {
        var processIds = _childProcessIds.ToArray();
        if (processIds.Length == 0)
        {
            return "No child processes";
        }

        _logger.LogInformation("Terminating {Count} child processes (Force: {Force})", processIds.Length, force);

        var terminatedCount = 0;
        var failedCount = 0;

        foreach (var pid in processIds)
        {
            try
            {
                var process = Process.GetProcessById(pid);

                if (!force)
                {
                    _logger.LogDebug("Sending graceful termination to process {ProcessId}", pid);
                    process.Kill(entireProcessTree: true);

                    var exited = await Task.Run(() => process.WaitForExit(ComponentTimeoutSeconds * 1000), ct)
                        .ConfigureAwait(false);

                    if (!exited)
                    {
                        _logger.LogWarning("Process {ProcessId} did not exit gracefully, force killing", pid);
                        process.Kill(entireProcessTree: true);
                    }
                }
                else
                {
                    process.Kill(entireProcessTree: true);
                }

                terminatedCount++;
                _logger.LogInformation("Terminated child process {ProcessId}", pid);
            }
            catch (ArgumentException)
            {
                _logger.LogDebug("Process {ProcessId} no longer exists", pid);
                terminatedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to terminate process {ProcessId}", pid);
                failedCount++;
            }
        }

        _childProcessIds.Clear();
        return $"Terminated {terminatedCount}/{processIds.Length} processes (Failed: {failedCount})";
    }

    /// <summary>
    /// Get shutdown status
    /// </summary>
    public ShutdownStatus GetStatus()
    {
        return new ShutdownStatus
        {
            ShutdownInitiated = _shutdownInitiated,
            ActiveConnections = _activeConnections.Count,
            TrackedProcesses = _childProcessIds.Count
        };
    }
}

public class ShutdownResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Details { get; set; }
}

public class ShutdownStatus
{
    public bool ShutdownInitiated { get; set; }
    public int ActiveConnections { get; set; }
    public int TrackedProcesses { get; set; }
}
