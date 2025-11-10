using Aura.Core.Data;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// API controller for comprehensive project management
/// </summary>
[ApiController]
[Route("api/project-management")]
public class ProjectManagementController : ControllerBase
{
    private readonly ProjectManagementService _projectService;
    private readonly ProjectVersionRepository _versionRepository;

    public ProjectManagementController(
        ProjectManagementService projectService,
        ProjectVersionRepository versionRepository)
    {
        _projectService = projectService;
        _versionRepository = versionRepository;
    }

    /// <summary>
    /// Get all projects with filtering, sorting, and pagination
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null,
        [FromQuery] string? tags = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] bool ascending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/project-management/projects - Search: {Search}, Status: {Status}", 
                correlationId, search, status);

            var tagList = !string.IsNullOrWhiteSpace(tags) 
                ? tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                : null;

            var (projects, totalCount) = await _projectService.GetProjectsAsync(
                search, status, category, tagList, fromDate, toDate, 
                sortBy, ascending, page, pageSize, ct);

            var response = new
            {
                projects = projects.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    description = p.Description,
                    status = p.Status,
                    category = p.Category,
                    tags = !string.IsNullOrWhiteSpace(p.Tags) 
                        ? p.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                        : new List<string>(),
                    thumbnailPath = p.ThumbnailPath,
                    outputFilePath = p.OutputFilePath,
                    durationSeconds = p.DurationSeconds,
                    currentWizardStep = p.CurrentWizardStep,
                    progressPercent = p.ProgressPercent,
                    sceneCount = p.Scenes.Count,
                    assetCount = p.Assets.Count,
                    templateId = p.TemplateId,
                    createdAt = p.CreatedAt,
                    updatedAt = p.UpdatedAt,
                    lastAutoSaveAt = p.LastAutoSaveAt,
                    createdBy = p.CreatedBy
                }),
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    hasNextPage = page * pageSize < totalCount,
                    hasPreviousPage = page > 1
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve projects", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve projects",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get a single project by ID
    /// </summary>
    [HttpGet("projects/{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/project-management/projects/{ProjectId}", correlationId, projectId);

            var project = await _projectService.GetProjectByIdAsync(projectId, ct);
            if (project == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Project Not Found",
                    status = 404,
                    detail = $"Project {projectId} does not exist",
                    correlationId
                });
            }

            var response = new
            {
                id = project.Id,
                title = project.Title,
                description = project.Description,
                status = project.Status,
                category = project.Category,
                tags = !string.IsNullOrWhiteSpace(project.Tags)
                    ? project.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                    : new List<string>(),
                thumbnailPath = project.ThumbnailPath,
                outputFilePath = project.OutputFilePath,
                durationSeconds = project.DurationSeconds,
                currentWizardStep = project.CurrentWizardStep,
                progressPercent = project.ProgressPercent,
                templateId = project.TemplateId,
                jobId = project.JobId,
                briefJson = project.BriefJson,
                planSpecJson = project.PlanSpecJson,
                voiceSpecJson = project.VoiceSpecJson,
                renderSpecJson = project.RenderSpecJson,
                errorMessage = project.ErrorMessage,
                scenes = project.Scenes.Select(s => new
                {
                    id = s.Id,
                    sceneIndex = s.SceneIndex,
                    scriptText = s.ScriptText,
                    audioFilePath = s.AudioFilePath,
                    imageFilePath = s.ImageFilePath,
                    durationSeconds = s.DurationSeconds,
                    isCompleted = s.IsCompleted
                }),
                assets = project.Assets.Select(a => new
                {
                    id = a.Id,
                    assetType = a.AssetType,
                    filePath = a.FilePath,
                    fileSizeBytes = a.FileSizeBytes,
                    mimeType = a.MimeType,
                    isTemporary = a.IsTemporary,
                    createdAt = a.CreatedAt
                }),
                checkpoints = project.Checkpoints.Select(c => new
                {
                    id = c.Id,
                    stageName = c.StageName,
                    checkpointTime = c.CheckpointTime,
                    completedScenes = c.CompletedScenes,
                    totalScenes = c.TotalScenes,
                    outputFilePath = c.OutputFilePath,
                    isValid = c.IsValid
                }),
                createdAt = project.CreatedAt,
                updatedAt = project.UpdatedAt,
                completedAt = project.CompletedAt,
                lastAutoSaveAt = project.LastAutoSaveAt,
                createdBy = project.CreatedBy,
                modifiedBy = project.ModifiedBy
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve project {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/project-management/projects - Title: {Title}", 
                correlationId, request.Title);

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Project title is required",
                    correlationId
                });
            }

            var project = await _projectService.CreateProjectAsync(
                request.Title,
                request.Description,
                request.Category,
                request.Tags,
                request.TemplateId,
                ct);

            return CreatedAtAction(
                nameof(GetProject),
                new { projectId = project.Id },
                new
                {
                    id = project.Id,
                    title = project.Title,
                    status = project.Status,
                    createdAt = project.CreatedAt
                });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to create project", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to create project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    [HttpPut("projects/{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(
        Guid projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] PUT /api/project-management/projects/{ProjectId}", correlationId, projectId);

            var project = await _projectService.UpdateProjectAsync(
                projectId,
                request.Title,
                request.Description,
                request.Category,
                request.Tags,
                request.Status,
                request.CurrentWizardStep,
                request.ThumbnailPath,
                request.OutputFilePath,
                request.DurationSeconds,
                ct);

            if (project == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Project Not Found",
                    status = 404,
                    detail = $"Project {projectId} does not exist",
                    correlationId
                });
            }

            return Ok(new
            {
                id = project.Id,
                title = project.Title,
                status = project.Status,
                updatedAt = project.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to update project {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to update project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Auto-save project data
    /// </summary>
    [HttpPost("projects/{projectId:guid}/auto-save")]
    public async Task<IActionResult> AutoSaveProject(
        Guid projectId,
        [FromBody] AutoSaveProjectRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Debug("[{CorrelationId}] POST /api/project-management/projects/{ProjectId}/auto-save", correlationId, projectId);

            var success = await _projectService.AutoSaveProjectAsync(
                projectId,
                request.BriefJson,
                request.PlanSpecJson,
                request.VoiceSpecJson,
                request.RenderSpecJson,
                ct);

            if (!success)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Project Not Found",
                    status = 404,
                    detail = $"Project {projectId} does not exist",
                    correlationId
                });
            }

            return Ok(new { success = true, autoSavedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to auto-save project {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to auto-save project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Duplicate a project
    /// </summary>
    [HttpPost("projects/{projectId:guid}/duplicate")]
    public async Task<IActionResult> DuplicateProject(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/project-management/projects/{ProjectId}/duplicate", correlationId, projectId);

            var duplicate = await _projectService.DuplicateProjectAsync(projectId, ct);
            if (duplicate == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Project Not Found",
                    status = 404,
                    detail = $"Project {projectId} does not exist",
                    correlationId
                });
            }

            return CreatedAtAction(
                nameof(GetProject),
                new { projectId = duplicate.Id },
                new
                {
                    id = duplicate.Id,
                    title = duplicate.Title,
                    status = duplicate.Status,
                    createdAt = duplicate.CreatedAt
                });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to duplicate project {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to duplicate project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Delete a project (soft delete)
    /// </summary>
    [HttpDelete("projects/{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] DELETE /api/project-management/projects/{ProjectId}", correlationId, projectId);

            var success = await _projectService.DeleteProjectAsync(projectId, ct);
            if (!success)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Project Not Found",
                    status = 404,
                    detail = $"Project {projectId} does not exist",
                    correlationId
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to delete project {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to delete project",
                correlationId
            });
        }
    }

    /// <summary>
    /// Bulk delete projects
    /// </summary>
    [HttpPost("projects/bulk-delete")]
    public async Task<IActionResult> BulkDeleteProjects(
        [FromBody] BulkDeleteRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/project-management/projects/bulk-delete - Count: {Count}", 
                correlationId, request.ProjectIds.Count);

            if (!request.ProjectIds.Any())
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "No project IDs provided",
                    correlationId
                });
            }

            var deletedCount = await _projectService.BulkDeleteProjectsAsync(request.ProjectIds, ct);

            return Ok(new
            {
                deletedCount,
                requestedCount = request.ProjectIds.Count
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to bulk delete projects", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to bulk delete projects",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get project version history
    /// </summary>
    [HttpGet("projects/{projectId:guid}/versions")]
    public async Task<IActionResult> GetProjectVersions(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/project-management/projects/{ProjectId}/versions", correlationId, projectId);

            var versions = await _versionRepository.GetVersionsByProjectIdAsync(projectId, ct);

            var response = versions.Select(v => new
            {
                id = v.Id,
                projectId = v.ProjectId,
                versionNumber = v.VersionNumber,
                name = v.Name,
                description = v.Description,
                versionType = v.VersionType,
                trigger = v.Trigger,
                storageSizeBytes = v.StorageSizeBytes,
                isMarkedImportant = v.IsMarkedImportant,
                createdAt = v.CreatedAt,
                createdBy = v.CreatedByUserId
            });

            return Ok(new { versions = response });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve project versions {ProjectId}", correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve project versions",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get available categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        try
        {
            var categories = await _projectService.GetCategoriesAsync(ct);
            return Ok(new { categories });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve categories", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve categories",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get available tags
    /// </summary>
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(CancellationToken ct = default)
    {
        try
        {
            var tags = await _projectService.GetTagsAsync(ct);
            return Ok(new { tags });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve tags", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve tags",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get project statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct = default)
    {
        try
        {
            var stats = await _projectService.GetStatisticsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve statistics", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve statistics",
                correlationId
            });
        }
    }
}

// Request/Response DTOs
public record CreateProjectRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public string? TemplateId { get; init; }
}

public record UpdateProjectRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public string? Status { get; init; }
    public int? CurrentWizardStep { get; init; }
    public string? ThumbnailPath { get; init; }
    public string? OutputFilePath { get; init; }
    public double? DurationSeconds { get; init; }
}

public record AutoSaveProjectRequest
{
    public string? BriefJson { get; init; }
    public string? PlanSpecJson { get; init; }
    public string? VoiceSpecJson { get; init; }
    public string? RenderSpecJson { get; init; }
}

public record BulkDeleteRequest
{
    public List<Guid> ProjectIds { get; init; } = new();
}
