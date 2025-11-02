using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Data;

/// <summary>
/// Repository for managing project state persistence and recovery
/// </summary>
public class ProjectStateRepository
{
    private readonly AuraDbContext _context;
    private readonly ILogger<ProjectStateRepository> _logger;

    public ProjectStateRepository(AuraDbContext context, ILogger<ProjectStateRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new project state
    /// </summary>
    public async Task<ProjectStateEntity> CreateAsync(ProjectStateEntity project, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created project state {ProjectId} with title: {Title}", project.Id, project.Title);
        return project;
    }

    /// <summary>
    /// Get project by ID with related entities
    /// </summary>
    public async Task<ProjectStateEntity?> GetByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .Include(p => p.Checkpoints.OrderByDescending(c => c.CheckpointTime))
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);
    }

    /// <summary>
    /// Get project by job ID
    /// </summary>
    public async Task<ProjectStateEntity?> GetByJobIdAsync(string jobId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        return await _context.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .Include(p => p.Checkpoints.OrderByDescending(c => c.CheckpointTime))
            .FirstOrDefaultAsync(p => p.JobId == jobId, ct);
    }

    /// <summary>
    /// Get all incomplete projects (InProgress status)
    /// </summary>
    public async Task<List<ProjectStateEntity>> GetIncompleteProjectsAsync(CancellationToken ct = default)
    {
        return await _context.ProjectStates
            .Where(p => p.Status == "InProgress")
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Update existing project state
    /// </summary>
    public async Task<ProjectStateEntity> UpdateAsync(ProjectStateEntity project, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(project);

        project.UpdatedAt = DateTime.UtcNow;
        _context.ProjectStates.Update(project);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Updated project state {ProjectId}", project.Id);
        return project;
    }

    /// <summary>
    /// Update project status
    /// </summary>
    public async Task UpdateStatusAsync(Guid projectId, string status, string? errorMessage = null, CancellationToken ct = default)
    {
        var project = await _context.ProjectStates.FindAsync(new object[] { projectId }, ct);
        if (project == null)
        {
            _logger.LogWarning("Cannot update status for non-existent project {ProjectId}", projectId);
            return;
        }

        project.Status = status;
        project.UpdatedAt = DateTime.UtcNow;
        project.ErrorMessage = errorMessage;

        if (status == "Completed" || status == "Failed" || status == "Cancelled")
        {
            project.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated project {ProjectId} status to {Status}", projectId, status);
    }

    /// <summary>
    /// Add or update checkpoint for a project
    /// </summary>
    public async Task<RenderCheckpointEntity> SaveCheckpointAsync(
        Guid projectId,
        string stageName,
        int completedScenes,
        int totalScenes,
        string? checkpointData = null,
        string? outputFilePath = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stageName);

        var checkpoint = new RenderCheckpointEntity
        {
            ProjectId = projectId,
            StageName = stageName,
            CheckpointTime = DateTime.UtcNow,
            CompletedScenes = completedScenes,
            TotalScenes = totalScenes,
            CheckpointData = checkpointData,
            OutputFilePath = outputFilePath,
            IsValid = true
        };

        _context.RenderCheckpoints.Add(checkpoint);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Saved checkpoint for project {ProjectId} at stage {StageName} ({Completed}/{Total} scenes)",
            projectId, stageName, completedScenes, totalScenes);

        return checkpoint;
    }

    /// <summary>
    /// Get the latest valid checkpoint for a project
    /// </summary>
    public async Task<RenderCheckpointEntity?> GetLatestCheckpointAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.RenderCheckpoints
            .Where(c => c.ProjectId == projectId && c.IsValid)
            .OrderByDescending(c => c.CheckpointTime)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Add scene state to a project
    /// </summary>
    public async Task<SceneStateEntity> AddSceneAsync(SceneStateEntity scene, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(scene);

        _context.SceneStates.Add(scene);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Added scene {SceneIndex} to project {ProjectId}", scene.SceneIndex, scene.ProjectId);
        return scene;
    }

    /// <summary>
    /// Add asset to a project
    /// </summary>
    public async Task<AssetStateEntity> AddAssetAsync(AssetStateEntity asset, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        _context.AssetStates.Add(asset);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Added asset {AssetType} to project {ProjectId}: {FilePath}",
            asset.AssetType, asset.ProjectId, asset.FilePath);
        return asset;
    }

    /// <summary>
    /// Delete a project and all related entities
    /// </summary>
    public async Task DeleteAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await GetByIdAsync(projectId, ct);
        if (project == null)
        {
            _logger.LogWarning("Cannot delete non-existent project {ProjectId}", projectId);
            return;
        }

        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted project {ProjectId} and all related entities", projectId);
    }

    /// <summary>
    /// Get projects older than specified timespan with specific status
    /// </summary>
    public async Task<List<ProjectStateEntity>> GetOldProjectsByStatusAsync(
        string status,
        TimeSpan olderThan,
        CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;

        return await _context.ProjectStates
            .Where(p => p.Status == status && p.UpdatedAt < cutoffDate)
            .Include(p => p.Assets)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get temporary assets without associated project
    /// </summary>
    public async Task<List<AssetStateEntity>> GetOrphanedAssetsAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow - olderThan;

        return await _context.AssetStates
            .Where(a => a.IsTemporary && a.CreatedAt < cutoffDate)
            .Where(a => a.Project == null || a.Project.Status == "Failed" || a.Project.Status == "Cancelled")
            .ToListAsync(ct);
    }
}
