using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing project versions, snapshots, and restore points
/// </summary>
public class ProjectVersionService
{
    private readonly ProjectVersionRepository _versionRepository;
    private readonly ProjectStateRepository _projectRepository;
    private readonly ILogger<ProjectVersionService> _logger;

    public ProjectVersionService(
        ProjectVersionRepository versionRepository,
        ProjectStateRepository projectRepository,
        ILogger<ProjectVersionService> logger)
    {
        _versionRepository = versionRepository ?? throw new ArgumentNullException(nameof(versionRepository));
        _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a manual snapshot with user-provided metadata
    /// </summary>
    public async Task<Guid> CreateManualSnapshotAsync(
        Guid projectId,
        string? name,
        string? description,
        string? userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating manual snapshot for project {ProjectId}", projectId);

        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var version = await CreateVersionFromProjectAsync(
            project,
            "Manual",
            name,
            description,
            userId,
            null,
            ct);

        _logger.LogInformation("Created manual snapshot {VersionId} (v{VersionNumber}) for project {ProjectId}",
            version.Id, version.VersionNumber, projectId);

        return version.Id;
    }

    /// <summary>
    /// Create an autosave version
    /// </summary>
    public async Task<Guid> CreateAutosaveAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Creating autosave for project {ProjectId}", projectId);

        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var version = await CreateVersionFromProjectAsync(
            project,
            "Autosave",
            null,
            "Automatic save",
            null,
            null,
            ct);

        _logger.LogDebug("Created autosave {VersionId} (v{VersionNumber}) for project {ProjectId}",
            version.Id, version.VersionNumber, projectId);

        return version.Id;
    }

    /// <summary>
    /// Create a restore point before or after a major operation
    /// </summary>
    public async Task<Guid> CreateRestorePointAsync(
        Guid projectId,
        string trigger,
        string? description = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating restore point for project {ProjectId}, trigger: {Trigger}",
            projectId, trigger);

        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var version = await CreateVersionFromProjectAsync(
            project,
            "RestorePoint",
            null,
            description ?? $"Restore point: {trigger}",
            null,
            trigger,
            ct);

        _logger.LogInformation("Created restore point {VersionId} (v{VersionNumber}) for project {ProjectId}",
            version.Id, version.VersionNumber, projectId);

        return version.Id;
    }

    /// <summary>
    /// Restore a project to a specific version
    /// </summary>
    public async Task RestoreVersionAsync(
        Guid projectId,
        Guid versionId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Restoring project {ProjectId} to version {VersionId}", projectId, versionId);

        var version = await _versionRepository.GetVersionByIdAsync(versionId, ct);
        if (version == null || version.ProjectId != projectId)
        {
            throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");
        }

        var project = await _projectRepository.GetByIdAsync(projectId, ct);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        project.BriefJson = version.BriefJson;
        project.PlanSpecJson = version.PlanSpecJson;
        project.VoiceSpecJson = version.VoiceSpecJson;
        project.RenderSpecJson = version.RenderSpecJson;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project, ct);

        _logger.LogInformation("Restored project {ProjectId} to version {VersionId} (v{VersionNumber})",
            projectId, versionId, version.VersionNumber);
    }

    /// <summary>
    /// Get all versions for a project
    /// </summary>
    public async Task<List<ProjectVersionInfo>> GetVersionsAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        var versions = await _versionRepository.GetVersionsAsync(projectId, false, ct);

        return versions.Select(v => new ProjectVersionInfo
        {
            Id = v.Id,
            ProjectId = v.ProjectId,
            VersionNumber = v.VersionNumber,
            Name = v.Name,
            Description = v.Description,
            VersionType = v.VersionType,
            Trigger = v.Trigger,
            CreatedAt = v.CreatedAt,
            CreatedByUserId = v.CreatedByUserId,
            StorageSizeBytes = v.StorageSizeBytes,
            IsMarkedImportant = v.IsMarkedImportant
        }).ToList();
    }

    /// <summary>
    /// Get a specific version with full data
    /// </summary>
    public async Task<ProjectVersionDetail?> GetVersionDetailAsync(
        Guid versionId,
        CancellationToken ct = default)
    {
        var version = await _versionRepository.GetVersionByIdAsync(versionId, ct);
        if (version == null)
        {
            return null;
        }

        return new ProjectVersionDetail
        {
            Id = version.Id,
            ProjectId = version.ProjectId,
            VersionNumber = version.VersionNumber,
            Name = version.Name,
            Description = version.Description,
            VersionType = version.VersionType,
            Trigger = version.Trigger,
            CreatedAt = version.CreatedAt,
            CreatedByUserId = version.CreatedByUserId,
            BriefJson = version.BriefJson,
            PlanSpecJson = version.PlanSpecJson,
            VoiceSpecJson = version.VoiceSpecJson,
            RenderSpecJson = version.RenderSpecJson,
            TimelineJson = version.TimelineJson,
            StorageSizeBytes = version.StorageSizeBytes,
            IsMarkedImportant = version.IsMarkedImportant
        };
    }

    /// <summary>
    /// Update version metadata
    /// </summary>
    public async Task UpdateVersionMetadataAsync(
        Guid versionId,
        string? name,
        string? description,
        bool? isMarkedImportant,
        CancellationToken ct = default)
    {
        await _versionRepository.UpdateVersionMetadataAsync(versionId, name, description, isMarkedImportant, ct);
    }

    /// <summary>
    /// Delete a version
    /// </summary>
    public async Task DeleteVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting version {VersionId}", versionId);
        await _versionRepository.DeleteVersionAsync(versionId, ct);
    }

    /// <summary>
    /// Get total storage used by project versions
    /// </summary>
    public async Task<long> GetProjectStorageSizeAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _versionRepository.GetProjectStorageSizeAsync(projectId, ct);
    }

    /// <summary>
    /// Clean up old autosave versions (keep last N, or older than X days)
    /// </summary>
    public async Task<int> CleanupOldAutosavesAsync(
        Guid projectId,
        int keepCount = 10,
        int olderThanDays = 7,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Cleaning up old autosaves for project {ProjectId}", projectId);

        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        var oldAutosaves = await _versionRepository.GetOldAutosavesAsync(projectId, cutoffDate, ct);

        var toDelete = oldAutosaves.Count > keepCount
            ? oldAutosaves.Skip(keepCount).ToList()
            : new List<ProjectVersionEntity>();

        foreach (var autosave in toDelete)
        {
            await _versionRepository.DeleteVersionAsync(autosave.Id, ct);
        }

        _logger.LogInformation("Cleaned up {Count} old autosaves for project {ProjectId}", toDelete.Count, projectId);
        return toDelete.Count;
    }

    /// <summary>
    /// Compare two versions and return differences
    /// </summary>
    public async Task<VersionComparisonResult> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken ct = default)
    {
        var version1 = await _versionRepository.GetVersionByIdAsync(versionId1, ct);
        var version2 = await _versionRepository.GetVersionByIdAsync(versionId2, ct);

        if (version1 == null || version2 == null)
        {
            throw new InvalidOperationException("One or both versions not found");
        }

        return new VersionComparisonResult
        {
            Version1Id = versionId1,
            Version2Id = versionId2,
            Version1Number = version1.VersionNumber,
            Version2Number = version2.VersionNumber,
            BriefChanged = version1.BriefHash != version2.BriefHash,
            PlanChanged = version1.PlanHash != version2.PlanHash,
            VoiceChanged = version1.VoiceHash != version2.VoiceHash,
            RenderChanged = version1.RenderHash != version2.RenderHash,
            TimelineChanged = version1.TimelineHash != version2.TimelineHash,
            Version1Data = new VersionData
            {
                BriefJson = version1.BriefJson,
                PlanSpecJson = version1.PlanSpecJson,
                VoiceSpecJson = version1.VoiceSpecJson,
                RenderSpecJson = version1.RenderSpecJson,
                TimelineJson = version1.TimelineJson
            },
            Version2Data = new VersionData
            {
                BriefJson = version2.BriefJson,
                PlanSpecJson = version2.PlanSpecJson,
                VoiceSpecJson = version2.VoiceSpecJson,
                RenderSpecJson = version2.RenderSpecJson,
                TimelineJson = version2.TimelineJson
            }
        };
    }

    /// <summary>
    /// Helper method to create a version from current project state
    /// </summary>
    private async Task<ProjectVersionEntity> CreateVersionFromProjectAsync(
        ProjectStateEntity project,
        string versionType,
        string? name,
        string? description,
        string? userId,
        string? trigger,
        CancellationToken ct)
    {
        var version = new ProjectVersionEntity
        {
            ProjectId = project.Id,
            Name = name,
            Description = description,
            VersionType = versionType,
            Trigger = trigger,
            CreatedByUserId = userId,
            BriefJson = project.BriefJson,
            PlanSpecJson = project.PlanSpecJson,
            VoiceSpecJson = project.VoiceSpecJson,
            RenderSpecJson = project.RenderSpecJson
        };

        if (!string.IsNullOrEmpty(version.BriefJson))
        {
            version.BriefHash = await _versionRepository.StoreContentBlobAsync(version.BriefJson, "Brief", ct);
        }

        if (!string.IsNullOrEmpty(version.PlanSpecJson))
        {
            version.PlanHash = await _versionRepository.StoreContentBlobAsync(version.PlanSpecJson, "Plan", ct);
        }

        if (!string.IsNullOrEmpty(version.VoiceSpecJson))
        {
            version.VoiceHash = await _versionRepository.StoreContentBlobAsync(version.VoiceSpecJson, "Voice", ct);
        }

        if (!string.IsNullOrEmpty(version.RenderSpecJson))
        {
            version.RenderHash = await _versionRepository.StoreContentBlobAsync(version.RenderSpecJson, "Render", ct);
        }

        long storageSize = 0;
        if (!string.IsNullOrEmpty(version.BriefJson)) storageSize += version.BriefJson.Length;
        if (!string.IsNullOrEmpty(version.PlanSpecJson)) storageSize += version.PlanSpecJson.Length;
        if (!string.IsNullOrEmpty(version.VoiceSpecJson)) storageSize += version.VoiceSpecJson.Length;
        if (!string.IsNullOrEmpty(version.RenderSpecJson)) storageSize += version.RenderSpecJson.Length;
        version.StorageSizeBytes = storageSize;

        return await _versionRepository.CreateVersionAsync(version, ct);
    }
}

/// <summary>
/// Information about a project version
/// </summary>
public record ProjectVersionInfo
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public int VersionNumber { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string VersionType { get; init; } = string.Empty;
    public string? Trigger { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByUserId { get; init; }
    public long StorageSizeBytes { get; init; }
    public bool IsMarkedImportant { get; init; }
}

/// <summary>
/// Detailed version data
/// </summary>
public record ProjectVersionDetail : ProjectVersionInfo
{
    public string? BriefJson { get; init; }
    public string? PlanSpecJson { get; init; }
    public string? VoiceSpecJson { get; init; }
    public string? RenderSpecJson { get; init; }
    public string? TimelineJson { get; init; }
}

/// <summary>
/// Result of comparing two versions
/// </summary>
public record VersionComparisonResult
{
    public Guid Version1Id { get; init; }
    public Guid Version2Id { get; init; }
    public int Version1Number { get; init; }
    public int Version2Number { get; init; }
    public bool BriefChanged { get; init; }
    public bool PlanChanged { get; init; }
    public bool VoiceChanged { get; init; }
    public bool RenderChanged { get; init; }
    public bool TimelineChanged { get; init; }
    public VersionData Version1Data { get; init; } = new();
    public VersionData Version2Data { get; init; } = new();
}

/// <summary>
/// Version data for comparison
/// </summary>
public record VersionData
{
    public string? BriefJson { get; init; }
    public string? PlanSpecJson { get; init; }
    public string? VoiceSpecJson { get; init; }
    public string? RenderSpecJson { get; init; }
    public string? TimelineJson { get; init; }
}
