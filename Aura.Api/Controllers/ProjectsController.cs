using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing video generation projects with state persistence
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly CheckpointManager _checkpointManager;

    public ProjectsController(CheckpointManager checkpointManager)
    {
        _checkpointManager = checkpointManager;
    }

    /// <summary>
    /// Get all incomplete projects available for recovery
    /// </summary>
    [HttpGet("incomplete")]
    public async Task<IActionResult> GetIncompleteProjects(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/incomplete endpoint called", correlationId);

            var projects = await _checkpointManager.GetIncompleteProjectsAsync(ct);

            var response = new
            {
                projects = projects.Select(p => new
                {
                    projectId = p.ProjectId,
                    title = p.Title,
                    jobId = p.JobId,
                    currentStage = p.CurrentStage,
                    progressPercent = p.ProgressPercent,
                    createdAt = p.CreatedAt,
                    updatedAt = p.UpdatedAt,
                    filesExist = p.FilesExist,
                    missingFilesCount = p.MissingFiles.Count,
                    canRecover = p.FilesExist && p.LatestCheckpoint != null
                }),
                count = projects.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve incomplete projects", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve incomplete projects",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get details of a specific project for recovery
    /// </summary>
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/{ProjectId} endpoint called", correlationId, projectId);

            var project = await _checkpointManager.GetProjectForRecoveryAsync(projectId, ct);
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
                projectId = project.ProjectId,
                title = project.Title,
                jobId = project.JobId,
                currentStage = project.CurrentStage,
                progressPercent = project.ProgressPercent,
                createdAt = project.CreatedAt,
                updatedAt = project.UpdatedAt,
                filesExist = project.FilesExist,
                missingFiles = project.MissingFiles,
                latestCheckpoint = project.LatestCheckpoint != null ? new
                {
                    stageName = project.LatestCheckpoint.StageName,
                    checkpointTime = project.LatestCheckpoint.CheckpointTime,
                    completedScenes = project.LatestCheckpoint.CompletedScenes,
                    totalScenes = project.LatestCheckpoint.TotalScenes,
                    outputFilePath = project.LatestCheckpoint.OutputFilePath
                } : null,
                scenes = project.Scenes.Select(s => new
                {
                    sceneIndex = s.SceneIndex,
                    scriptText = s.ScriptText,
                    durationSeconds = s.DurationSeconds,
                    isCompleted = s.IsCompleted,
                    audioFilePath = s.AudioFilePath,
                    imageFilePath = s.ImageFilePath
                })
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
                detail = "Failed to retrieve project details",
                correlationId
            });
        }
    }

    /// <summary>
    /// Delete a project and discard all associated data
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] DELETE /api/projects/{ProjectId} endpoint called", correlationId, projectId);

            var project = await _checkpointManager.GetProjectForRecoveryAsync(projectId, ct);
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

            await _checkpointManager.CancelProjectAsync(projectId, ct);

            Log.Information("[{CorrelationId}] Project {ProjectId} marked as cancelled", correlationId, projectId);

            return Ok(new
            {
                message = "Project discarded successfully",
                projectId
            });
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
}
