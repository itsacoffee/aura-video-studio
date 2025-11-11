using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aura.Core.Models.Projects;
using Aura.Core.Models.Storage;
using Aura.Core.Services.Projects;
using Aura.Core.Services.Storage;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for project storage and file management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectStorageController : ControllerBase
{
    private readonly ILogger<ProjectStorageController> _logger;
    private readonly IProjectFileService _projectFileService;
    private readonly IEnhancedLocalStorageService _storageService;
    private readonly ProjectAutoSaveService _autoSaveService;

    public ProjectStorageController(
        ILogger<ProjectStorageController> logger,
        IProjectFileService projectFileService,
        IEnhancedLocalStorageService storageService,
        ProjectAutoSaveService autoSaveService)
    {
        _logger = logger;
        _projectFileService = projectFileService;
        _storageService = storageService;
        _autoSaveService = autoSaveService;
    }

    #region Project Operations

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectStorageCreateRequest request, CancellationToken ct)
    {
        try
        {
            var project = await _projectFileService.CreateProjectAsync(request.Name, request.Description, ct);
            _autoSaveService.RegisterProject(project);
            
            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project: {Name}", request.Name);
            return StatusCode(500, new { error = "Failed to create project", details = ex.Message });
        }
    }

    /// <summary>
    /// Load a project
    /// </summary>
    [HttpGet("projects/{projectId}")]
    public async Task<IActionResult> LoadProject(Guid projectId, CancellationToken ct)
    {
        try
        {
            var project = await _projectFileService.LoadProjectAsync(projectId, ct);
            if (project == null)
            {
                return NotFound(new { error = $"Project not found: {projectId}" });
            }

            _autoSaveService.RegisterProject(project);
            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project: {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to load project", details = ex.Message });
        }
    }

    /// <summary>
    /// Save a project
    /// </summary>
    [HttpPut("projects/{projectId}")]
    public async Task<IActionResult> SaveProject(Guid projectId, [FromBody] AuraProjectFile project, CancellationToken ct)
    {
        try
        {
            if (projectId != project.Id)
            {
                return BadRequest(new { error = "Project ID mismatch" });
            }

            await _projectFileService.SaveProjectAsync(project, ct);
            _autoSaveService.MarkProjectModified(projectId, project);
            
            return Ok(new { success = true, savedAt = project.LastSavedAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project: {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to save project", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("projects/{projectId}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken ct)
    {
        try
        {
            var result = await _projectFileService.DeleteProjectAsync(projectId, ct);
            if (!result)
            {
                return NotFound(new { error = $"Project not found: {projectId}" });
            }

            _autoSaveService.UnregisterProject(projectId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project: {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to delete project", details = ex.Message });
        }
    }

    /// <summary>
    /// List all projects
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> ListProjects(CancellationToken ct)
    {
        try
        {
            var projects = await _projectFileService.ListProjectsAsync(ct);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list projects");
            return StatusCode(500, new { error = "Failed to list projects", details = ex.Message });
        }
    }

    #endregion

    #region Asset Management

    /// <summary>
    /// Add an asset to a project
    /// </summary>
    [HttpPost("projects/{projectId}/assets")]
    public async Task<IActionResult> AddAsset(Guid projectId, [FromBody] AddAssetRequest request, CancellationToken ct)
    {
        try
        {
            var asset = await _projectFileService.AddAssetAsync(projectId, request.AssetPath, request.AssetType, ct);
            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add asset to project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to add asset", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove an asset from a project
    /// </summary>
    [HttpDelete("projects/{projectId}/assets/{assetId}")]
    public async Task<IActionResult> RemoveAsset(Guid projectId, Guid assetId, CancellationToken ct)
    {
        try
        {
            var result = await _projectFileService.RemoveAssetAsync(projectId, assetId, ct);
            if (!result)
            {
                return NotFound(new { error = $"Asset not found: {assetId}" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove asset {AssetId} from project {ProjectId}", assetId, projectId);
            return StatusCode(500, new { error = "Failed to remove asset", details = ex.Message });
        }
    }

    /// <summary>
    /// Detect missing assets in a project
    /// </summary>
    [HttpGet("projects/{projectId}/missing-assets")]
    public async Task<IActionResult> DetectMissingAssets(Guid projectId, CancellationToken ct)
    {
        try
        {
            var report = await _projectFileService.DetectMissingAssetsAsync(projectId, ct);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect missing assets for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to detect missing assets", details = ex.Message });
        }
    }

    /// <summary>
    /// Relink a missing asset
    /// </summary>
    [HttpPost("projects/{projectId}/assets/{assetId}/relink")]
    public async Task<IActionResult> RelinkAsset(Guid projectId, Guid assetId, [FromBody] RelinkAssetRequest request, CancellationToken ct)
    {
        try
        {
            var relinkRequest = new AssetRelinkRequest
            {
                ProjectId = projectId,
                AssetId = assetId,
                NewPath = request.NewPath
            };

            var result = await _projectFileService.RelinkAssetAsync(relinkRequest, ct);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to relink asset {AssetId} in project {ProjectId}", assetId, projectId);
            return StatusCode(500, new { error = "Failed to relink asset", details = ex.Message });
        }
    }

    #endregion

    #region Project Consolidation

    /// <summary>
    /// Consolidate project assets into project folder
    /// </summary>
    [HttpPost("projects/{projectId}/consolidate")]
    public async Task<IActionResult> ConsolidateProject(Guid projectId, [FromBody] ProjectConsolidationRequest request, CancellationToken ct)
    {
        try
        {
            request.ProjectId = projectId;
            var result = await _projectFileService.ConsolidateProjectAsync(request, ct);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consolidate project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to consolidate project", details = ex.Message });
        }
    }

    /// <summary>
    /// Package project for export/sharing
    /// </summary>
    [HttpPost("projects/{projectId}/package")]
    public async Task<IActionResult> PackageProject(Guid projectId, [FromBody] ProjectPackageRequest request, CancellationToken ct)
    {
        try
        {
            request.ProjectId = projectId;
            var result = await _projectFileService.PackageProjectAsync(request, ct);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to package project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to package project", details = ex.Message });
        }
    }

    /// <summary>
    /// Import a packaged project
    /// </summary>
    [HttpPost("projects/import")]
    public async Task<IActionResult> ImportProject([FromBody] ProjectStorageImportRequest request, CancellationToken ct)
    {
        try
        {
            var projectId = await _projectFileService.UnpackageProjectAsync(request.PackagePath, ct);
            return Ok(new { projectId, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import project from {Package}", request.PackagePath);
            return StatusCode(500, new { error = "Failed to import project", details = ex.Message });
        }
    }

    #endregion

    #region Backups

    /// <summary>
    /// Create a backup of a project
    /// </summary>
    [HttpPost("projects/{projectId}/backups")]
    public async Task<IActionResult> CreateBackup(Guid projectId, [FromBody] CreateBackupRequest? request, CancellationToken ct)
    {
        try
        {
            var backupPath = await _storageService.CreateBackupAsync(projectId, request?.BackupName, ct);
            return Ok(new { success = true, backupPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to create backup", details = ex.Message });
        }
    }

    /// <summary>
    /// List backups for a project
    /// </summary>
    [HttpGet("projects/{projectId}/backups")]
    public async Task<IActionResult> ListBackups(Guid projectId, CancellationToken ct)
    {
        try
        {
            var backups = await _storageService.ListBackupsAsync(projectId, ct);
            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list backups for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to list backups", details = ex.Message });
        }
    }

    /// <summary>
    /// Restore a backup
    /// </summary>
    [HttpPost("projects/{projectId}/backups/{backupFileName}/restore")]
    public async Task<IActionResult> RestoreBackup(Guid projectId, string backupFileName, CancellationToken ct)
    {
        try
        {
            var content = await _storageService.RestoreBackupAsync(projectId, backupFileName, ct);
            if (content == null)
            {
                return NotFound(new { error = $"Backup not found: {backupFileName}" });
            }

            return Ok(new { success = true, restoredAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup {Backup} for project {ProjectId}", backupFileName, projectId);
            return StatusCode(500, new { error = "Failed to restore backup", details = ex.Message });
        }
    }

    #endregion

    #region Storage Statistics

    /// <summary>
    /// Get storage statistics
    /// </summary>
    [HttpGet("storage/statistics")]
    public async Task<IActionResult> GetStorageStatistics(CancellationToken ct)
    {
        try
        {
            var stats = await _storageService.GetStorageStatisticsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage statistics");
            return StatusCode(500, new { error = "Failed to get storage statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get disk space information
    /// </summary>
    [HttpGet("storage/disk-space")]
    public async Task<IActionResult> GetDiskSpaceInfo(CancellationToken ct)
    {
        try
        {
            var info = await _storageService.GetDiskSpaceInfoAsync(ct);
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disk space info");
            return StatusCode(500, new { error = "Failed to get disk space info", details = ex.Message });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("storage/cache/statistics")]
    public async Task<IActionResult> GetCacheStatistics(CancellationToken ct)
    {
        try
        {
            var stats = await _storageService.GetCacheStatisticsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return StatusCode(500, new { error = "Failed to get cache statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Clean up cache
    /// </summary>
    [HttpPost("storage/cache/cleanup")]
    public async Task<IActionResult> CleanupCache([FromQuery] bool forceAll = false, CancellationToken ct = default)
    {
        try
        {
            var result = await _storageService.CleanupCacheAsync(forceAll, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache");
            return StatusCode(500, new { error = "Failed to cleanup cache", details = ex.Message });
        }
    }

    /// <summary>
    /// Clean up cache by category
    /// </summary>
    [HttpPost("storage/cache/cleanup/{category}")]
    public async Task<IActionResult> CleanupCacheByCategory(string category, CancellationToken ct)
    {
        try
        {
            var result = await _storageService.CleanupCacheByCategoryAsync(category, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache for category {Category}", category);
            return StatusCode(500, new { error = "Failed to cleanup cache", details = ex.Message });
        }
    }

    #endregion

    #region Auto-Save

    /// <summary>
    /// Get auto-save statistics for a project
    /// </summary>
    [HttpGet("projects/{projectId}/autosave/statistics")]
    public IActionResult GetAutoSaveStatistics(Guid projectId)
    {
        try
        {
            var stats = _autoSaveService.GetProjectStatistics(projectId);
            if (stats == null)
            {
                return NotFound(new { error = $"Project not registered for auto-save: {projectId}" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-save statistics for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to get auto-save statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get auto-save statistics for all projects
    /// </summary>
    [HttpGet("projects/autosave/statistics")]
    public IActionResult GetAllAutoSaveStatistics()
    {
        try
        {
            var stats = _autoSaveService.GetAllStatistics();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-save statistics");
            return StatusCode(500, new { error = "Failed to get auto-save statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Force save a project immediately
    /// </summary>
    [HttpPost("projects/{projectId}/autosave/force")]
    public async Task<IActionResult> ForceSaveProject(Guid projectId, CancellationToken ct)
    {
        try
        {
            await _autoSaveService.ForceSaveProjectAsync(projectId, ct);
            return Ok(new { success = true, savedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force save project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to force save project", details = ex.Message });
        }
    }

    #endregion
}

#region Request Models

public class ProjectStorageCreateRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class AddAssetRequest
{
    public required string AssetPath { get; set; }
    public required string AssetType { get; set; }
}

public class RelinkAssetRequest
{
    public required string NewPath { get; set; }
}

public class ProjectStorageImportRequest
{
    public required string PackagePath { get; set; }
}

public class CreateBackupRequest
{
    public string? BackupName { get; set; }
}

#endregion
