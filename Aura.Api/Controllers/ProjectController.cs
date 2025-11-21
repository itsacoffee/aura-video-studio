using Aura.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Collections.Generic;
using System.Text.Json;
using ProblemDetailsHelper = Aura.Api.Utilities.ProblemDetailsFactory;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing video editor projects
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private static readonly List<Project> _projects = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Get all projects
    /// </summary>
    [HttpGet]
    public IActionResult GetProjects()
    {
        lock (_lock)
        {
            var projectList = _projects
                .Select(p => new ProjectListItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Thumbnail = p.Thumbnail,
                    LastModifiedAt = p.LastModifiedAt,
                    Duration = p.Duration,
                    ClipCount = p.ClipCount
                })
                .OrderByDescending(p => p.LastModifiedAt)
                .ToList();

            return Ok(projectList);
        }
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetProject(string id)
    {
        lock (_lock)
        {
            var project = _projects.FirstOrDefault(p => p.Id == id);
            if (project == null)
            {
                return ProblemDetailsHelper.CreateNotFound(
                    detail: $"Project with ID '{id}' was not found",
                    resourceId: id,
                    resourceType: "Project");
            }

            var response = new LoadProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Thumbnail = project.Thumbnail,
                CreatedAt = project.CreatedAt,
                LastModifiedAt = project.LastModifiedAt,
                ProjectData = project.ProjectData
            };

            return Ok(response);
        }
    }

    /// <summary>
    /// Create a new project or update an existing one
    /// </summary>
    [HttpPost]
    public IActionResult SaveProject([FromBody] SaveProjectRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            Log.Warning("[{CorrelationId}] Project save rejected: Name is required", correlationId);
            return ProblemDetailsHelper.CreateBadRequest(
                detail: "Project name is required",
                correlationId: correlationId,
                field: "Name");
        }

        if (string.IsNullOrWhiteSpace(request.ProjectData))
        {
            Log.Warning("[{CorrelationId}] Project save rejected: ProjectData is required", correlationId);
            return ProblemDetailsHelper.CreateBadRequest(
                detail: "Project data is required",
                correlationId: correlationId,
                field: "ProjectData");
        }

        // Validate JSON format
        try
        {
            JsonDocument.Parse(request.ProjectData);
        }
        catch (JsonException ex)
        {
            Log.Warning("[{CorrelationId}] Project save rejected: Invalid JSON in ProjectData", correlationId);
            return ProblemDetailsHelper.CreateBadRequest(
                detail: $"Project data must be valid JSON: {ex.Message}",
                correlationId: correlationId,
                field: "ProjectData");
        }

        // Extract clip count from project data
        int clipCount = 0;
        double duration = 0;
        try
        {
            using var doc = JsonDocument.Parse(request.ProjectData);
            if (doc.RootElement.TryGetProperty("clips", out var clipsElement))
            {
                clipCount = clipsElement.GetArrayLength();
            }
            if (doc.RootElement.TryGetProperty("metadata", out var metadataElement))
            {
                if (metadataElement.TryGetProperty("duration", out var durationElement))
                {
                    duration = durationElement.GetDouble();
                }
            }
        }
        catch
        {
            // Ignore parsing errors for metadata extraction
        }

        lock (_lock)
        {
            Project project;
            
            // Update existing project
            if (!string.IsNullOrWhiteSpace(request.Id))
            {
                var existingIndex = _projects.FindIndex(p => p.Id == request.Id);
                if (existingIndex >= 0)
                {
                    project = _projects[existingIndex] with
                    {
                        Name = request.Name,
                        Description = request.Description,
                        Thumbnail = request.Thumbnail,
                        LastModifiedAt = DateTime.UtcNow,
                        ProjectData = request.ProjectData,
                        ClipCount = clipCount,
                        Duration = duration
                    };
                    _projects[existingIndex] = project;
                    Log.Information("[{CorrelationId}] Updated project: {ProjectId} - {ProjectName}", 
                        correlationId, project.Id, project.Name);
                }
                else
                {
                    return ProblemDetailsHelper.CreateNotFound(
                        detail: $"Project with ID '{request.Id}' was not found",
                        correlationId: correlationId,
                        resourceId: request.Id,
                        resourceType: "Project");
                }
            }
            // Create new project
            else
            {
                project = new Project
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Description = request.Description,
                    Thumbnail = request.Thumbnail,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    ProjectData = request.ProjectData,
                    ClipCount = clipCount,
                    Duration = duration
                };
                _projects.Add(project);
                Log.Information("[{CorrelationId}] Created new project: {ProjectId} - {ProjectName}", 
                    correlationId, project.Id, project.Name);
            }

            return Ok(new
            {
                id = project.Id,
                name = project.Name,
                lastModifiedAt = project.LastModifiedAt
            });
        }
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult DeleteProject(string id)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        lock (_lock)
        {
            var index = _projects.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                return ProblemDetailsHelper.CreateNotFound(
                    detail: $"Project with ID '{id}' was not found",
                    correlationId: correlationId,
                    resourceId: id,
                    resourceType: "Project");
            }

            var project = _projects[index];
            _projects.RemoveAt(index);
            
            Log.Information("[{CorrelationId}] Deleted project: {ProjectId} - {ProjectName}", 
                correlationId, project.Id, project.Name);

            return NoContent();
        }
    }

    /// <summary>
    /// Duplicate a project
    /// </summary>
    [HttpPost("{id}/duplicate")]
    public IActionResult DuplicateProject(string id)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        lock (_lock)
        {
            var original = _projects.FirstOrDefault(p => p.Id == id);
            if (original == null)
            {
                return ProblemDetailsHelper.CreateNotFound(
                    detail: $"Project with ID '{id}' was not found",
                    correlationId: correlationId,
                    resourceId: id,
                    resourceType: "Project");
            }

            var duplicate = original with
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{original.Name} (Copy)",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };
            
            _projects.Add(duplicate);
            
            Log.Information("[{CorrelationId}] Duplicated project: {OriginalId} -> {DuplicateId}", 
                correlationId, original.Id, duplicate.Id);

            return Ok(new
            {
                id = duplicate.Id,
                name = duplicate.Name,
                lastModifiedAt = duplicate.LastModifiedAt
            });
        }
    }
}
