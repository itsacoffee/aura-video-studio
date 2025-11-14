using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.StatePersistence;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Service for managing cancellable, pausable, and resumable video generation operations
/// Provides fine-grained control over long-running generation jobs
/// </summary>
public class CancellableGenerationService
{
    private readonly ILogger<CancellableGenerationService> _logger;
    private readonly GenerationStateManager _stateManager;
    private readonly ConcurrentDictionary<string, GenerationControl> _activeGenerations;

    public CancellableGenerationService(
        ILogger<CancellableGenerationService> logger,
        GenerationStateManager stateManager)
    {
        _logger = logger;
        _stateManager = stateManager;
        _activeGenerations = new ConcurrentDictionary<string, GenerationControl>();
    }

    /// <summary>
    /// Register a new generation job with cancellation support
    /// </summary>
    public GenerationControl RegisterGeneration(string jobId)
    {
        var control = new GenerationControl(jobId, _logger);
        
        if (!_activeGenerations.TryAdd(jobId, control))
        {
            _logger.LogWarning("Generation {JobId} is already registered", jobId);
            return _activeGenerations[jobId];
        }

        _logger.LogInformation("Registered cancellable generation: {JobId}", jobId);
        return control;
    }

    /// <summary>
    /// Get control for an active generation
    /// </summary>
    public GenerationControl? GetControl(string jobId)
    {
        return _activeGenerations.TryGetValue(jobId, out var control) ? control : null;
    }

    /// <summary>
    /// Cancel a generation job
    /// </summary>
    public async Task<bool> CancelGenerationAsync(string jobId)
    {
        if (!_activeGenerations.TryGetValue(jobId, out var control))
        {
            _logger.LogWarning("Cannot cancel unknown generation: {JobId}", jobId);
            return false;
        }

        _logger.LogInformation("Cancelling generation: {JobId}", jobId);
        control.Cancel();

        await _stateManager.MarkCancelledAsync(jobId).ConfigureAwait(false);

        // Remove from active after a delay to allow cleanup
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            _activeGenerations.TryRemove(jobId, out _);
        });

        return true;
    }

    /// <summary>
    /// Pause a generation job
    /// </summary>
    public bool PauseGeneration(string jobId)
    {
        if (!_activeGenerations.TryGetValue(jobId, out var control))
        {
            _logger.LogWarning("Cannot pause unknown generation: {JobId}", jobId);
            return false;
        }

        _logger.LogInformation("Pausing generation: {JobId}", jobId);
        return control.Pause();
    }

    /// <summary>
    /// Resume a paused generation job
    /// </summary>
    public bool ResumeGeneration(string jobId)
    {
        if (!_activeGenerations.TryGetValue(jobId, out var control))
        {
            _logger.LogWarning("Cannot resume unknown generation: {JobId}", jobId);
            return false;
        }

        _logger.LogInformation("Resuming generation: {JobId}", jobId);
        return control.Resume();
    }

    /// <summary>
    /// Complete and unregister a generation
    /// </summary>
    public void CompleteGeneration(string jobId)
    {
        if (_activeGenerations.TryRemove(jobId, out var control))
        {
            control.Dispose();
            _logger.LogInformation("Completed and unregistered generation: {JobId}", jobId);
        }
    }

    /// <summary>
    /// Get all active generation IDs
    /// </summary>
    public string[] GetActiveGenerationIds()
    {
        return _activeGenerations.Keys.ToArray();
    }

    /// <summary>
    /// Clean up all active generations
    /// </summary>
    public void CleanupAll()
    {
        foreach (var kvp in _activeGenerations)
        {
            kvp.Value.Dispose();
        }
        _activeGenerations.Clear();
    }
}

/// <summary>
/// Control interface for a generation job
/// </summary>
public class GenerationControl : IDisposable
{
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ManualResetEventSlim _pauseEvent;
    private bool _isPaused;
    private bool _isCancelled;

    public string JobId { get; }
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public GenerationControl(string jobId, ILogger logger)
    {
        JobId = jobId;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _pauseEvent = new ManualResetEventSlim(initialState: true); // Start unpaused
    }

    /// <summary>
    /// Check if generation is paused
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Check if generation is cancelled
    /// </summary>
    public bool IsCancelled => _isCancelled;

    /// <summary>
    /// Cancel the generation
    /// </summary>
    public void Cancel()
    {
        if (_isCancelled)
        {
            return;
        }

        _logger.LogInformation("Cancelling generation control for {JobId}", JobId);
        _isCancelled = true;
        _cancellationTokenSource.Cancel();
        
        // Release pause if paused
        if (_isPaused)
        {
            Resume();
        }
    }

    /// <summary>
    /// Pause the generation
    /// </summary>
    public bool Pause()
    {
        if (_isCancelled)
        {
            _logger.LogWarning("Cannot pause cancelled generation {JobId}", JobId);
            return false;
        }

        if (_isPaused)
        {
            _logger.LogDebug("Generation {JobId} is already paused", JobId);
            return false;
        }

        _logger.LogInformation("Pausing generation {JobId}", JobId);
        _isPaused = true;
        _pauseEvent.Reset();
        return true;
    }

    /// <summary>
    /// Resume the generation
    /// </summary>
    public bool Resume()
    {
        if (!_isPaused)
        {
            _logger.LogDebug("Generation {JobId} is not paused", JobId);
            return false;
        }

        _logger.LogInformation("Resuming generation {JobId}", JobId);
        _isPaused = false;
        _pauseEvent.Set();
        return true;
    }

    /// <summary>
    /// Wait for pause to be released (call this at checkpoints in generation pipeline)
    /// </summary>
    public void WaitIfPaused()
    {
        if (_isPaused)
        {
            _logger.LogDebug("Generation {JobId} waiting for resume...", JobId);
            _pauseEvent.Wait(_cancellationTokenSource.Token);
        }
    }

    /// <summary>
    /// Async version of WaitIfPaused
    /// </summary>
    public async Task WaitIfPausedAsync()
    {
        if (_isPaused)
        {
            _logger.LogDebug("Generation {JobId} waiting for resume (async)...", JobId);
            await Task.Run(() => _pauseEvent.Wait(_cancellationTokenSource.Token), _cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checkpoint method to call at safe pause/cancel points
    /// Throws OperationCanceledException if cancelled
    /// </summary>
    public void Checkpoint(string checkpointName)
    {
        // Check for cancellation first
        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

        // Wait if paused
        WaitIfPaused();

        _logger.LogTrace("Generation {JobId} passed checkpoint: {Checkpoint}", JobId, checkpointName);
    }

    /// <summary>
    /// Async checkpoint
    /// </summary>
    public async Task CheckpointAsync(string checkpointName)
    {
        _cancellationTokenSource.Token.ThrowIfCancellationRequested();
        await WaitIfPausedAsync().ConfigureAwait(false);
        _logger.LogTrace("Generation {JobId} passed checkpoint: {Checkpoint}", JobId, checkpointName);
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _pauseEvent?.Dispose();
    }
}
