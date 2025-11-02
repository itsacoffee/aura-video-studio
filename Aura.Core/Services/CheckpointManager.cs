using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Manages checkpoint creation and recovery for video generation pipeline
/// </summary>
public class CheckpointManager
{
    private readonly ProjectStateRepository _repository;
    private readonly ILogger<CheckpointManager> _logger;

    public CheckpointManager(ProjectStateRepository repository, ILogger<CheckpointManager> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new project state for tracking
    /// </summary>
    public async Task<Guid> CreateProjectStateAsync(
        string title,
        string jobId,
        Brief brief,
        PlanSpec planSpec,
        VoiceSpec voiceSpec,
        RenderSpec renderSpec,
        CancellationToken ct = default)
    {
        var project = new ProjectStateEntity
        {
            Title = title,
            JobId = jobId,
            Status = "InProgress",
            CurrentStage = "Initialization",
            BriefJson = JsonSerializer.Serialize(brief),
            PlanSpecJson = JsonSerializer.Serialize(planSpec),
            VoiceSpecJson = JsonSerializer.Serialize(voiceSpec),
            RenderSpecJson = JsonSerializer.Serialize(renderSpec)
        };

        await _repository.CreateAsync(project, ct);
        _logger.LogInformation("Created project state {ProjectId} for job {JobId}", project.Id, jobId);
        
        return project.Id;
    }

    /// <summary>
    /// Save a checkpoint at a specific pipeline stage
    /// </summary>
    public async Task SaveCheckpointAsync(
        Guid projectId,
        string stageName,
        int completedScenes,
        int totalScenes,
        Dictionary<string, object>? checkpointData = null,
        string? outputFilePath = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        
        var dataJson = checkpointData != null 
            ? JsonSerializer.Serialize(checkpointData) 
            : null;

        await _repository.SaveCheckpointAsync(
            projectId,
            stageName,
            completedScenes,
            totalScenes,
            dataJson,
            outputFilePath,
            ct);

        var elapsed = DateTime.UtcNow - startTime;
        
        if (elapsed.TotalMilliseconds > 100)
        {
            _logger.LogWarning("Checkpoint save took {Ms}ms, exceeding 100ms target", elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug("Checkpoint saved in {Ms}ms", elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Update project progress percentage
    /// </summary>
    public async Task UpdateProgressAsync(
        Guid projectId,
        string currentStage,
        int progressPercent,
        CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            _logger.LogWarning("Cannot update progress for non-existent project {ProjectId}", projectId);
            return;
        }

        project.CurrentStage = currentStage;
        project.ProgressPercent = progressPercent;
        project.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(project, ct);
    }

    /// <summary>
    /// Add a scene to the project state
    /// </summary>
    public async Task AddSceneAsync(
        Guid projectId,
        int sceneIndex,
        string scriptText,
        double durationSeconds,
        string? audioFilePath = null,
        string? imageFilePath = null,
        CancellationToken ct = default)
    {
        var scene = new SceneStateEntity
        {
            ProjectId = projectId,
            SceneIndex = sceneIndex,
            ScriptText = scriptText,
            DurationSeconds = durationSeconds,
            AudioFilePath = audioFilePath,
            ImageFilePath = imageFilePath,
            IsCompleted = audioFilePath != null && imageFilePath != null
        };

        await _repository.AddSceneAsync(scene, ct);
    }

    /// <summary>
    /// Add an asset to the project
    /// </summary>
    public async Task AddAssetAsync(
        Guid projectId,
        string assetType,
        string filePath,
        long fileSizeBytes,
        string? mimeType = null,
        bool isTemporary = true,
        CancellationToken ct = default)
    {
        var asset = new AssetStateEntity
        {
            ProjectId = projectId,
            AssetType = assetType,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            MimeType = mimeType,
            IsTemporary = isTemporary
        };

        await _repository.AddAssetAsync(asset, ct);
    }

    /// <summary>
    /// Mark project as completed
    /// </summary>
    public async Task CompleteProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await _repository.UpdateStatusAsync(projectId, "Completed", null, ct);
        _logger.LogInformation("Project {ProjectId} marked as completed", projectId);
    }

    /// <summary>
    /// Mark project as failed
    /// </summary>
    public async Task FailProjectAsync(Guid projectId, string errorMessage, CancellationToken ct = default)
    {
        await _repository.UpdateStatusAsync(projectId, "Failed", errorMessage, ct);
        _logger.LogWarning("Project {ProjectId} marked as failed: {Error}", projectId, errorMessage);
    }

    /// <summary>
    /// Mark project as cancelled
    /// </summary>
    public async Task CancelProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        await _repository.UpdateStatusAsync(projectId, "Cancelled", null, ct);
        _logger.LogInformation("Project {ProjectId} marked as cancelled", projectId);
    }

    /// <summary>
    /// Get the latest checkpoint for a project
    /// </summary>
    public async Task<CheckpointInfo?> GetLatestCheckpointAsync(Guid projectId, CancellationToken ct = default)
    {
        var checkpoint = await _repository.GetLatestCheckpointAsync(projectId, ct);
        if (checkpoint == null)
        {
            return null;
        }

        Dictionary<string, object>? checkpointData = null;
        if (!string.IsNullOrEmpty(checkpoint.CheckpointData))
        {
            try
            {
                checkpointData = JsonSerializer.Deserialize<Dictionary<string, object>>(checkpoint.CheckpointData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize checkpoint data for project {ProjectId}", projectId);
            }
        }

        return new CheckpointInfo
        {
            StageName = checkpoint.StageName,
            CheckpointTime = checkpoint.CheckpointTime,
            CompletedScenes = checkpoint.CompletedScenes,
            TotalScenes = checkpoint.TotalScenes,
            OutputFilePath = checkpoint.OutputFilePath,
            CheckpointData = checkpointData
        };
    }

    /// <summary>
    /// Get project state for recovery
    /// </summary>
    public async Task<ProjectRecoveryInfo?> GetProjectForRecoveryAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            return null;
        }

        var latestCheckpoint = await GetLatestCheckpointAsync(projectId, ct);

        Brief? brief = null;
        PlanSpec? planSpec = null;
        VoiceSpec? voiceSpec = null;
        RenderSpec? renderSpec = null;

        try
        {
            if (!string.IsNullOrEmpty(project.BriefJson))
                brief = JsonSerializer.Deserialize<Brief>(project.BriefJson);
            
            if (!string.IsNullOrEmpty(project.PlanSpecJson))
                planSpec = JsonSerializer.Deserialize<PlanSpec>(project.PlanSpecJson);
            
            if (!string.IsNullOrEmpty(project.VoiceSpecJson))
                voiceSpec = JsonSerializer.Deserialize<VoiceSpec>(project.VoiceSpecJson);
            
            if (!string.IsNullOrEmpty(project.RenderSpecJson))
                renderSpec = JsonSerializer.Deserialize<RenderSpec>(project.RenderSpecJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize project specs for {ProjectId}", projectId);
        }

        // Verify checkpoint files still exist
        var filesExist = true;
        var missingFiles = new List<string>();

        if (latestCheckpoint?.OutputFilePath != null)
        {
            if (!File.Exists(latestCheckpoint.OutputFilePath))
            {
                filesExist = false;
                missingFiles.Add(latestCheckpoint.OutputFilePath);
            }
        }

        foreach (var scene in project.Scenes.Where(s => s.IsCompleted))
        {
            if (scene.AudioFilePath != null && !File.Exists(scene.AudioFilePath))
            {
                filesExist = false;
                missingFiles.Add(scene.AudioFilePath);
            }
            
            if (scene.ImageFilePath != null && !File.Exists(scene.ImageFilePath))
            {
                filesExist = false;
                missingFiles.Add(scene.ImageFilePath);
            }
        }

        return new ProjectRecoveryInfo
        {
            ProjectId = projectId,
            Title = project.Title,
            JobId = project.JobId,
            CurrentStage = project.CurrentStage,
            ProgressPercent = project.ProgressPercent,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            Brief = brief,
            PlanSpec = planSpec,
            VoiceSpec = voiceSpec,
            RenderSpec = renderSpec,
            LatestCheckpoint = latestCheckpoint,
            Scenes = project.Scenes.OrderBy(s => s.SceneIndex).ToList(),
            FilesExist = filesExist,
            MissingFiles = missingFiles
        };
    }

    /// <summary>
    /// Get all incomplete projects for recovery
    /// </summary>
    public async Task<List<ProjectRecoveryInfo>> GetIncompleteProjectsAsync(CancellationToken ct = default)
    {
        var projects = await _repository.GetIncompleteProjectsAsync(ct);
        var recoveryInfos = new List<ProjectRecoveryInfo>();

        foreach (var project in projects)
        {
            var info = await GetProjectForRecoveryAsync(project.Id, ct);
            if (info != null)
            {
                recoveryInfos.Add(info);
            }
        }

        return recoveryInfos;
    }
}

/// <summary>
/// Information about a saved checkpoint
/// </summary>
public record CheckpointInfo
{
    public string StageName { get; init; } = string.Empty;
    public DateTime CheckpointTime { get; init; }
    public int CompletedScenes { get; init; }
    public int TotalScenes { get; init; }
    public string? OutputFilePath { get; init; }
    public Dictionary<string, object>? CheckpointData { get; init; }
}

/// <summary>
/// Information about a project available for recovery
/// </summary>
public record ProjectRecoveryInfo
{
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? JobId { get; init; }
    public string? CurrentStage { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Brief? Brief { get; init; }
    public PlanSpec? PlanSpec { get; init; }
    public VoiceSpec? VoiceSpec { get; init; }
    public RenderSpec? RenderSpec { get; init; }
    public CheckpointInfo? LatestCheckpoint { get; init; }
    public List<SceneStateEntity> Scenes { get; init; } = new();
    public bool FilesExist { get; init; }
    public List<string> MissingFiles { get; init; } = new();
}
