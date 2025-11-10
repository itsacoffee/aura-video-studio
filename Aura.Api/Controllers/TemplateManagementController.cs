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
/// API controller for project template management
/// </summary>
[ApiController]
[Route("api/template-management")]
public class TemplateManagementController : ControllerBase
{
    private readonly TemplateManagementService _templateService;
    private readonly ProjectManagementService _projectService;

    public TemplateManagementController(
        TemplateManagementService templateService,
        ProjectManagementService projectService)
    {
        _templateService = templateService;
        _projectService = projectService;
    }

    /// <summary>
    /// Get all templates with optional filtering
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? subCategory = null,
        [FromQuery] bool? isSystemTemplate = null,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/template-management/templates - Category: {Category}", 
                correlationId, category);

            var templates = await _templateService.GetTemplatesAsync(category, subCategory, isSystemTemplate, ct);

            var response = templates.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                description = t.Description,
                category = t.Category,
                subCategory = t.SubCategory,
                tags = !string.IsNullOrWhiteSpace(t.Tags)
                    ? t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).ToList()
                    : new List<string>(),
                previewImage = t.PreviewImage,
                previewVideo = t.PreviewVideo,
                isSystemTemplate = t.IsSystemTemplate,
                isCommunityTemplate = t.IsCommunityTemplate,
                author = t.Author,
                usageCount = t.UsageCount,
                rating = t.Rating,
                ratingCount = t.RatingCount,
                createdAt = t.CreatedAt,
                updatedAt = t.UpdatedAt
            });

            return Ok(new { templates = response });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve templates", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve templates",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get a single template by ID
    /// </summary>
    [HttpGet("templates/{templateId}")]
    public async Task<IActionResult> GetTemplate(string templateId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/template-management/templates/{TemplateId}", correlationId, templateId);

            var template = await _templateService.GetTemplateByIdAsync(templateId, ct);
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Template Not Found",
                    status = 404,
                    detail = $"Template {templateId} does not exist",
                    correlationId
                });
            }

            var response = new
            {
                id = template.Id,
                name = template.Name,
                description = template.Description,
                category = template.Category,
                subCategory = template.SubCategory,
                tags = !string.IsNullOrWhiteSpace(template.Tags)
                    ? template.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).ToList()
                    : new List<string>(),
                templateData = template.TemplateData,
                previewImage = template.PreviewImage,
                previewVideo = template.PreviewVideo,
                isSystemTemplate = template.IsSystemTemplate,
                isCommunityTemplate = template.IsCommunityTemplate,
                author = template.Author,
                usageCount = template.UsageCount,
                rating = template.Rating,
                ratingCount = template.RatingCount,
                createdAt = template.CreatedAt,
                updatedAt = template.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve template {TemplateId}", correlationId, templateId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve template",
                correlationId
            });
        }
    }

    /// <summary>
    /// Create a new custom template
    /// </summary>
    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/template-management/templates - Name: {Name}", 
                correlationId, request.Name);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Template name is required",
                    correlationId
                });
            }

            var template = await _templateService.CreateTemplateAsync(
                request.Name,
                request.Description ?? string.Empty,
                request.Category ?? "Custom",
                request.SubCategory ?? string.Empty,
                request.TemplateData ?? "{}",
                request.Tags,
                request.PreviewImage,
                request.PreviewVideo,
                false, // User templates are not system templates
                ct);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { templateId = template.Id },
                new
                {
                    id = template.Id,
                    name = template.Name,
                    createdAt = template.CreatedAt
                });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to create template", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to create template",
                correlationId
            });
        }
    }

    /// <summary>
    /// Create a project from a template
    /// </summary>
    [HttpPost("templates/{templateId}/create-project")]
    public async Task<IActionResult> CreateProjectFromTemplate(
        string templateId,
        [FromBody] CreateFromTemplateRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/template-management/templates/{TemplateId}/create-project", 
                correlationId, templateId);

            var template = await _templateService.GetTemplateByIdAsync(templateId, ct);
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Template Not Found",
                    status = 404,
                    detail = $"Template {templateId} does not exist",
                    correlationId
                });
            }

            // Create project with template data
            var project = await _projectService.CreateProjectAsync(
                request.ProjectName ?? template.Name,
                template.Description,
                template.Category,
                !string.IsNullOrWhiteSpace(template.Tags)
                    ? template.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                    : null,
                templateId,
                ct);

            // Increment template usage count
            await _templateService.IncrementUsageCountAsync(templateId, ct);

            return CreatedAtAction(
                "GetProject",
                "ProjectManagement",
                new { projectId = project.Id },
                new
                {
                    id = project.Id,
                    title = project.Title,
                    templateId = project.TemplateId,
                    createdAt = project.CreatedAt
                });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to create project from template {TemplateId}", correlationId, templateId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to create project from template",
                correlationId
            });
        }
    }

    /// <summary>
    /// Delete a custom template
    /// </summary>
    [HttpDelete("templates/{templateId}")]
    public async Task<IActionResult> DeleteTemplate(string templateId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] DELETE /api/template-management/templates/{TemplateId}", correlationId, templateId);

            var success = await _templateService.DeleteTemplateAsync(templateId, ct);
            if (!success)
            {
                return NotFound(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E404",
                    title = "Template Not Found or Cannot Delete",
                    status = 404,
                    detail = $"Template {templateId} does not exist or is a system template",
                    correlationId
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to delete template {TemplateId}", correlationId, templateId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to delete template",
                correlationId
            });
        }
    }

    /// <summary>
    /// Seed system templates (admin only)
    /// </summary>
    [HttpPost("templates/seed")]
    public async Task<IActionResult> SeedSystemTemplates(CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/template-management/templates/seed", correlationId);

            await _templateService.SeedSystemTemplatesAsync(ct);

            return Ok(new { message = "System templates seeded successfully" });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to seed system templates", correlationId);
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to seed system templates",
                correlationId
            });
        }
    }
}

// Request DTOs
public record CreateTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? SubCategory { get; init; }
    public string? TemplateData { get; init; }
    public List<string>? Tags { get; init; }
    public string? PreviewImage { get; init; }
    public string? PreviewVideo { get; init; }
}

public record CreateFromTemplateRequest
{
    public string? ProjectName { get; init; }
}
