using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.StatePersistence;

/// <summary>
/// Manages state persistence for long-running video generation operations
/// Enables recovery from failures and provides progress tracking
/// </summary>
public class GenerationStateManager
{
    private readonly ILogger<GenerationStateManager> _logger;
    private readonly IDbContextFactory<AuraDbContext> _contextFactory;
    private readonly ConcurrentDictionary<string, GenerationState> _activeStates;
    private readonly Timer _persistenceTimer;

    public GenerationStateManager(
        ILogger<GenerationStateManager> _logger,
        IDbContextFactory<AuraDbContext> contextFactory)
    {
        this._logger = _logger;
        _contextFactory = contextFactory;
        _activeStates = new ConcurrentDictionary<string, GenerationState>();
        
        // Auto-persist every 5 seconds
        _persistenceTimer = new Timer(
            AutoPersistCallback,
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Initialize a new generation operation
    /// </summary>
    public async Task<string> InitializeGenerationAsync(
        string jobId,
        GenerationConfiguration config,
        CancellationToken ct = default)
    {
        var state = new GenerationState
        {
            JobId = jobId,
            Status = GenerationStatus.Initializing,
            Configuration = config,
            StartedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            CurrentStage = "Initialization",
            OverallProgress = 0
        };

        _activeStates[jobId] = state;

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = new ProjectStateEntity
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            Status = "Initializing",
            BriefJson = config != null ? JsonSerializer.Serialize(config) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.ProjectStates.Add(entity);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Initialized generation state for job {JobId}", jobId);
        return entity.Id.ToString();
    }

    /// <summary>
    /// Update generation progress
    /// </summary>
    public void UpdateProgress(
        string jobId,
        string stage,
        double overallProgress,
        double stageProgress,
        string? message = null)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            _logger.LogWarning("Cannot update progress for unknown job {JobId}", jobId);
            return;
        }

        state.CurrentStage = stage;
        state.OverallProgress = overallProgress;
        state.StageProgress = stageProgress;
        state.LastUpdatedAt = DateTime.UtcNow;
        
        if (message != null)
        {
            state.StatusMessage = message;
        }

        _logger.LogDebug(
            "Progress update for {JobId}: {Stage} {Overall:F1}% (stage: {StageProgress:F1}%)",
            jobId, stage, overallProgress, stageProgress);
    }

    /// <summary>
    /// Record a completed stage
    /// </summary>
    public async Task RecordStageCompletionAsync(
        string jobId,
        string stageName,
        object? stageData = null,
        CancellationToken ct = default)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            _logger.LogWarning("Cannot record stage completion for unknown job {JobId}", jobId);
            return;
        }

        var checkpoint = new StageCheckpoint
        {
            StageName = stageName,
            CompletedAt = DateTime.UtcNow,
            Data = stageData
        };

        state.CompletedStages.Add(checkpoint);
        state.LastUpdatedAt = DateTime.UtcNow;

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var checkpointEntity = new RenderCheckpointEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = !string.IsNullOrEmpty(state.ProjectStateId) && Guid.TryParse(state.ProjectStateId, out var projectGuid) ? projectGuid : Guid.Empty,
            StageName = stageName,
            CheckpointTime = DateTime.UtcNow,
            CheckpointData = stageData != null ? JsonSerializer.Serialize(stageData) : null
        };

        context.RenderCheckpoints.Add(checkpointEntity);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Recorded checkpoint for {JobId}: {Stage}", jobId, stageName);
    }

    /// <summary>
    /// Mark generation as completed successfully
    /// </summary>
    public async Task MarkCompletedAsync(
        string jobId,
        string outputPath,
        CancellationToken ct = default)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            _logger.LogWarning("Cannot mark completion for unknown job {JobId}", jobId);
            return;
        }

        state.Status = GenerationStatus.Completed;
        state.OutputPath = outputPath;
        state.CompletedAt = DateTime.UtcNow;
        state.LastUpdatedAt = DateTime.UtcNow;

        await PersistStateAsync(jobId, ct);
        _activeStates.TryRemove(jobId, out _);

        _logger.LogInformation("Generation {JobId} completed successfully: {OutputPath}", jobId, outputPath);
    }

    /// <summary>
    /// Mark generation as failed
    /// </summary>
    public async Task MarkFailedAsync(
        string jobId,
        string errorMessage,
        Exception? exception = null,
        CancellationToken ct = default)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            _logger.LogWarning("Cannot mark failure for unknown job {JobId}", jobId);
            return;
        }

        state.Status = GenerationStatus.Failed;
        state.ErrorMessage = errorMessage;
        state.LastUpdatedAt = DateTime.UtcNow;

        if (exception != null)
        {
            state.ExceptionDetails = new ExceptionDetails
            {
                Type = exception.GetType().FullName ?? "Unknown",
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };
        }

        await PersistStateAsync(jobId, ct);
        _activeStates.TryRemove(jobId, out _);

        _logger.LogError(exception, "Generation {JobId} failed: {Error}", jobId, errorMessage);
    }

    /// <summary>
    /// Mark generation as cancelled
    /// </summary>
    public async Task MarkCancelledAsync(string jobId, CancellationToken ct = default)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            _logger.LogWarning("Cannot mark cancellation for unknown job {JobId}", jobId);
            return;
        }

        state.Status = GenerationStatus.Cancelled;
        state.LastUpdatedAt = DateTime.UtcNow;

        await PersistStateAsync(jobId, ct);
        _activeStates.TryRemove(jobId, out _);

        _logger.LogInformation("Generation {JobId} cancelled", jobId);
    }

    /// <summary>
    /// Get current state for a job
    /// </summary>
    public GenerationState? GetState(string jobId)
    {
        return _activeStates.TryGetValue(jobId, out var state) ? state : null;
    }

    /// <summary>
    /// Recover state from database for resumption
    /// </summary>
    public async Task<GenerationState?> RecoverStateAsync(
        string jobId,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = await context.ProjectStates
            .FirstOrDefaultAsync(p => p.JobId == jobId, ct);

        if (entity == null || string.IsNullOrEmpty(entity.BriefJson))
        {
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize<GenerationState>(entity.BriefJson);
            if (state != null)
            {
                state.ProjectStateId = entity.Id.ToString();
                _activeStates[jobId] = state;
                _logger.LogInformation("Recovered state for job {JobId} from database", jobId);
                return state;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize state for job {JobId}", jobId);
        }

        return null;
    }

    /// <summary>
    /// Persist state to database
    /// </summary>
    private async Task PersistStateAsync(string jobId, CancellationToken ct = default)
    {
        if (!_activeStates.TryGetValue(jobId, out var state))
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        
        var entity = await context.ProjectStates
            .FirstOrDefaultAsync(p => p.JobId == jobId, ct);

        if (entity != null)
        {
            entity.Status = state.Status.ToString();
            entity.BriefJson = JsonSerializer.Serialize(state);
            entity.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Auto-persist callback for timer
    /// </summary>
    private async void AutoPersistCallback(object? state)
    {
        foreach (var jobId in _activeStates.Keys)
        {
            try
            {
                await PersistStateAsync(jobId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-persist state for job {JobId}", jobId);
            }
        }
    }

    public void Dispose()
    {
        _persistenceTimer?.Dispose();
    }
}

/// <summary>
/// Generation state for persistence
/// </summary>
public class GenerationState
{
    public string JobId { get; set; } = "";
    public string? ProjectStateId { get; set; }
    public GenerationStatus Status { get; set; }
    public GenerationConfiguration? Configuration { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CurrentStage { get; set; } = "";
    public double OverallProgress { get; set; }
    public double StageProgress { get; set; }
    public string? StatusMessage { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public ExceptionDetails? ExceptionDetails { get; set; }
    public List<StageCheckpoint> CompletedStages { get; set; } = new();
}

public class GenerationConfiguration
{
    public string Topic { get; set; } = "";
    public int TargetDurationSeconds { get; set; }
    public string Style { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int Fps { get; set; }
}

public class StageCheckpoint
{
    public string StageName { get; set; } = "";
    public DateTime CompletedAt { get; set; }
    public object? Data { get; set; }
}

public class ExceptionDetails
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
}

public enum GenerationStatus
{
    Initializing,
    InProgress,
    Completed,
    Failed,
    Cancelled,
    Paused
}
