using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Manages FFmpeg process lifecycle including tracking, timeout enforcement, and cleanup
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Register a process for tracking and management
    /// </summary>
    void RegisterProcess(int processId, string jobId);
    
    /// <summary>
    /// Unregister a process after completion
    /// </summary>
    void UnregisterProcess(int processId);
    
    /// <summary>
    /// Get all tracked process IDs
    /// </summary>
    int[] GetTrackedProcesses();
    
    /// <summary>
    /// Kill a specific process
    /// </summary>
    Task KillProcessAsync(int processId, CancellationToken ct = default);
    
    /// <summary>
    /// Kill all tracked processes
    /// </summary>
    Task KillAllProcessesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get process count
    /// </summary>
    int GetProcessCount();
}

/// <summary>
/// Implementation of FFmpeg process manager with timeout enforcement and cleanup
/// </summary>
public class ProcessManager : IProcessManager, IDisposable
{
    private readonly ILogger<ProcessManager> _logger;
    private readonly ConcurrentDictionary<int, ProcessInfo> _processes = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(60);
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public ProcessManager(ILogger<ProcessManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Setup periodic cleanup sweep every 15 minutes
        _cleanupTimer = new Timer(
            PeriodicCleanup, 
            null, 
            TimeSpan.FromMinutes(15), 
            TimeSpan.FromMinutes(15));
    }

    public void RegisterProcess(int processId, string jobId)
    {
        var info = new ProcessInfo
        {
            ProcessId = processId,
            JobId = jobId,
            StartTime = DateTime.UtcNow,
            TimeoutAt = DateTime.UtcNow.Add(_defaultTimeout)
        };

        if (_processes.TryAdd(processId, info))
        {
            _logger.LogInformation(
                "Registered FFmpeg process PID={ProcessId} for job {JobId}, timeout at {TimeoutAt}",
                processId, jobId, info.TimeoutAt);
        }
    }

    public void UnregisterProcess(int processId)
    {
        if (_processes.TryRemove(processId, out var info))
        {
            var duration = DateTime.UtcNow - info.StartTime;
            _logger.LogInformation(
                "Unregistered FFmpeg process PID={ProcessId} for job {JobId}, duration {Duration}",
                processId, info.JobId, duration);
        }
    }

    public int[] GetTrackedProcesses()
    {
        return _processes.Keys.ToArray();
    }

    public async Task KillProcessAsync(int processId, CancellationToken ct = default)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            
            _logger.LogWarning("Killing FFmpeg process PID={ProcessId}", processId);
            
            // Try graceful kill first
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
            }
            
            UnregisterProcess(processId);
        }
        catch (ArgumentException)
        {
            // Process already exited or doesn't exist
            _logger.LogDebug("Process PID={ProcessId} already exited", processId);
            UnregisterProcess(processId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing process PID={ProcessId}", processId);
        }
    }

    public async Task KillAllProcessesAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Killing all tracked FFmpeg processes (count={Count})", _processes.Count);
        
        var tasks = _processes.Keys
            .Select(pid => KillProcessAsync(pid, ct))
            .ToArray();
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public int GetProcessCount()
    {
        return _processes.Count;
    }

    private void PeriodicCleanup(object? state)
    {
        try
        {
            _logger.LogDebug("Running periodic process cleanup sweep");
            
            var now = DateTime.UtcNow;
            var timedOut = _processes
                .Where(kvp => now > kvp.Value.TimeoutAt)
                .ToList();

            if (timedOut.Any())
            {
                _logger.LogWarning(
                    "Found {Count} timed-out FFmpeg processes, terminating",
                    timedOut.Count);

                foreach (var kvp in timedOut)
                {
                    _ = KillProcessAsync(kvp.Key);
                }
            }

            // Also check for processes that have exited but weren't unregistered
            var exited = _processes
                .Where(kvp => !IsProcessRunning(kvp.Key))
                .ToList();

            if (exited.Any())
            {
                _logger.LogDebug(
                    "Found {Count} exited processes, cleaning up",
                    exited.Count);

                foreach (var kvp in exited)
                {
                    UnregisterProcess(kvp.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic cleanup sweep");
        }
    }

    private bool IsProcessRunning(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing ProcessManager, killing all tracked processes");
        
        _cleanupTimer?.Dispose();
        
        // Kill all tracked processes synchronously on dispose
        KillAllProcessesAsync().Wait(TimeSpan.FromSeconds(10));
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private class ProcessInfo
    {
        public required int ProcessId { get; init; }
        public required string JobId { get; init; }
        public required DateTime StartTime { get; init; }
        public required DateTime TimeoutAt { get; init; }
    }
}
