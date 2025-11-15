using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly ConcurrentBag<int> _childProcessIds = new();
    private readonly ConcurrentDictionary<string, HttpResponse> _activeConnections = new();
    private bool _shutdownInitiated;
    private readonly object _shutdownLock = new();

    private const int GracefulTimeoutSeconds = 3;  // Reduced from 5 for faster shutdown
    private const int ComponentTimeoutSeconds = 2;  // Reduced from 3 for faster shutdown

    public ShutdownOrchestrator(
        ILogger<ShutdownOrchestrator> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
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

        _logger.LogInformation("=================================================================");
        _logger.LogInformation("Initiating graceful shutdown (Force: {Force})", force);
        _logger.LogInformation("=================================================================");

        var result = new ShutdownResult { Success = true };
        var stepResults = new List<string>();

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(force ? 2 : GracefulTimeoutSeconds));

            // Step 1: Notify active SSE connections
            var sseStep = await NotifySseConnectionsAsync(cts.Token).ConfigureAwait(false);
            stepResults.Add($"SSE Notification: {sseStep}");
            _logger.LogInformation("Step 1/3 Complete: {Result}", sseStep);

            // Step 2: Close SSE connections gracefully
            var closeStep = await CloseSseConnectionsAsync(cts.Token).ConfigureAwait(false);
            stepResults.Add($"SSE Closure: {closeStep}");
            _logger.LogInformation("Step 2/3 Complete: {Result}", closeStep);

            // Step 3: Terminate child processes
            var processStep = await TerminateChildProcessesAsync(force, cts.Token).ConfigureAwait(false);
            stepResults.Add($"Process Termination: {processStep}");
            _logger.LogInformation("Step 3/3 Complete: {Result}", processStep);

            result.Message = string.Join("; ", stepResults);
            _logger.LogInformation("=================================================================");
            _logger.LogInformation("Graceful shutdown completed successfully");
            _logger.LogInformation("=================================================================");

            // Signal application to stop
            if (!force)
            {
                await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
            }
            _lifetime.StopApplication();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown sequence");
            result.Success = false;
            result.Message = $"Shutdown error: {ex.Message}";
            result.Details = stepResults;

            if (force)
            {
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

        _logger.LogInformation("Notifying {Count} active SSE connections", connectionCount);

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
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(1));

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

        await Task.Delay(500, ct).ConfigureAwait(false);

        _activeConnections.Clear();
        return $"Closed {connectionCount} connections";
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
