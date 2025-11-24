using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Runtime;

/// <summary>
/// Centralized registry for tracking all spawned processes to prevent orphaned/zombie processes
/// </summary>
public class ProcessRegistry : IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, TrackedProcess> _processes = new();
    private readonly ILogger<ProcessRegistry> _logger;
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    public ProcessRegistry(ILogger<ProcessRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Setup periodic cleanup timer to detect and clean up exited processes
        _cleanupTimer = new Timer(
            PeriodicCleanup,
            null,
            TimeSpan.FromMinutes(1), // Check every minute
            TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Register a process for tracking
    /// </summary>
    public TrackedProcess Register(Process process, string? jobId = null)
    {
        if (process == null)
        {
            throw new ArgumentNullException(nameof(process));
        }

        var tracked = new TrackedProcess(
            process.Id,
            process.ProcessName,
            jobId,
            DateTime.UtcNow,
            new CancellationTokenSource());

        _processes[process.Id] = tracked;
        _logger.LogDebug("Registered process {Name} (PID: {Pid}, JobId: {JobId})",
            process.ProcessName, process.Id, jobId ?? "none");

        // Auto-remove when process exits
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            Unregister(process.Id);
        };

        return tracked;
    }

    /// <summary>
    /// Unregister a process (called automatically on exit or manually)
    /// </summary>
    public void Unregister(int processId)
    {
        if (_processes.TryRemove(processId, out var tracked))
        {
            var duration = DateTime.UtcNow - tracked.StartedAt;
            _logger.LogDebug("Unregistered process {Name} (PID: {Pid}, JobId: {JobId}, Duration: {Duration})",
                tracked.Name, processId, tracked.JobId ?? "none", duration);

            tracked.Cts.Dispose();
        }
    }

    /// <summary>
    /// Kill all processes associated with a specific job
    /// </summary>
    public async Task KillAllForJobAsync(string jobId)
    {
        var toKill = _processes.Values
            .Where(p => p.JobId == jobId)
            .ToList();

        if (toKill.Count == 0)
        {
            _logger.LogDebug("No processes found for job {JobId}", jobId);
            return;
        }

        _logger.LogInformation("Killing {Count} processes for job {JobId}", toKill.Count, jobId);

        var tasks = toKill.Select(tracked => KillProcessAsync(tracked.ProcessId));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Kill a specific process by PID
    /// </summary>
    public async Task KillProcessAsync(int processId)
    {
        if (!_processes.TryGetValue(processId, out var tracked))
        {
            _logger.LogDebug("Process {Pid} not found in registry", processId);
            return;
        }

        try
        {
            var process = Process.GetProcessById(processId);

            if (!process.HasExited)
            {
                _logger.LogWarning("Killing process {Name} (PID: {Pid}, JobId: {JobId})",
                    tracked.Name, processId, tracked.JobId ?? "none");

                // Cancel any associated cancellation tokens
                tracked.Cts.Cancel();

                // Kill the process tree
                process.Kill(entireProcessTree: true);

                // Wait for it to exit (with timeout)
                try
                {
                    await process.WaitForExitAsync(tracked.Cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected if cancellation token was triggered
                }
            }
        }
        catch (ArgumentException)
        {
            // Process already exited or doesn't exist
            _logger.LogDebug("Process {Pid} already exited", processId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing process {Pid}", processId);
        }
        finally
        {
            Unregister(processId);
        }
    }

    /// <summary>
    /// Get count of active tracked processes
    /// </summary>
    public int ActiveCount => _processes.Count;

    /// <summary>
    /// Get all active tracked processes
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetActiveProcesses()
    {
        return _processes.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Get processes for a specific job
    /// </summary>
    public IReadOnlyList<TrackedProcess> GetProcessesForJob(string jobId)
    {
        return _processes.Values
            .Where(p => p.JobId == jobId)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Periodic cleanup to detect and remove exited processes that weren't properly unregistered
    /// </summary>
    private void PeriodicCleanup(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var toRemove = new List<int>();

            foreach (var kvp in _processes)
            {
                try
                {
                    var process = Process.GetProcessById(kvp.Key);
                    if (process.HasExited)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist anymore
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var pid in toRemove)
            {
                Unregister(pid);
            }

            if (toRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} exited processes", toRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic cleanup");
        }
    }

    /// <summary>
    /// Dispose and kill all tracked processes
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cleanupTimer?.Dispose();

        var count = _processes.Count;
        if (count > 0)
        {
            _logger.LogInformation("Shutting down - killing {Count} tracked processes", count);

            var tasks = _processes.Keys.Select(KillProcessAsync);
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        // Dispose all cancellation token sources
        foreach (var tracked in _processes.Values)
        {
            tracked.Cts.Dispose();
        }

        _processes.Clear();
    }
}

/// <summary>
/// Information about a tracked process
/// </summary>
public record TrackedProcess(
    int ProcessId,
    string Name,
    string? JobId,
    DateTime StartedAt,
    CancellationTokenSource Cts
);

