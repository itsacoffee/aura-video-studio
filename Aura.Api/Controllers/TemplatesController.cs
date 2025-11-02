using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing project templates
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;
    private readonly TemplateService _templateService;

    public TemplatesController(ILogger<TemplatesController> logger, TemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    /// <summary>
    /// Get all templates with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? subCategory = null,
        [FromQuery] bool systemOnly = false,
        [FromQuery] bool communityOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            TemplateCategory? categoryEnum = null;
            if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TemplateCategory>(category, true, out var parsed))
            {
                categoryEnum = parsed;
            }

            var response = await _templateService.GetTemplatesAsync(
                categoryEnum,
                subCategory,
                systemOnly,
                communityOnly,
                page,
                pageSize);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve templates",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplate(string id)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Template Not Found",
                    status = 404,
                    detail = $"Template with ID '{id}' was not found",
                    templateId = id
                });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a new project from a template
    /// </summary>
    [HttpPost("create-from-template")]
    public async Task<IActionResult> CreateFromTemplate([FromBody] CreateFromTemplateRequest request)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(request.TemplateId);
            
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Template Not Found",
                    status = 404,
                    detail = $"Template with ID '{request.TemplateId}' was not found",
                    templateId = request.TemplateId
                });
            }

            // Increment usage count
            await _templateService.IncrementUsageAsync(request.TemplateId);

            // Parse template structure
            var templateStructure = JsonSerializer.Deserialize<TemplateStructure>(template.TemplateData);
            
            if (templateStructure == null)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Template",
                    status = 400,
                    detail = "Template data is invalid",
                    templateId = request.TemplateId
                });
            }

            // Convert template to project file
            var projectFile = ConvertTemplateToProject(templateStructure, request.ProjectName);

            return Ok(new
            {
                projectFile,
                templateName = template.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project from template {TemplateId}", request.TemplateId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to create project from template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Save current project as a template
    /// </summary>
    [HttpPost("save-as-template")]
    public async Task<IActionResult> SaveAsTemplate([FromBody] SaveAsTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Template name is required",
                    field = "Name"
                });
            }

            var template = await _templateService.CreateTemplateAsync(request);

            return Ok(new
            {
                id = template.Id,
                name = template.Name,
                message = "Template saved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save template {TemplateName}", request.Name);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to save template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(string id)
    {
        try
        {
            var deleted = await _templateService.DeleteTemplateAsync(id);
            
            if (!deleted)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Template Not Found",
                    status = 404,
                    detail = $"Template with ID '{id}' was not found or cannot be deleted",
                    templateId = id
                });
            }

            return Ok(new { message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to delete template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get effect presets
    /// </summary>
    [HttpGet("effect-presets")]
    public IActionResult GetEffectPresets()
    {
        try
        {
            var presets = _templateService.GetEffectPresets();
            return Ok(presets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effect presets");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve effect presets",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get transition presets
    /// </summary>
    [HttpGet("transition-presets")]
    public IActionResult GetTransitionPresets()
    {
        try
        {
            var presets = _templateService.GetTransitionPresets();
            return Ok(presets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transition presets");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve transition presets",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get title templates
    /// </summary>
    [HttpGet("title-templates")]
    public IActionResult GetTitleTemplates()
    {
        try
        {
            var templates = _templateService.GetTitleTemplates();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get title templates");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve title templates",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Seed sample templates (for development/testing)
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedTemplates()
    {
        try
        {
            await _templateService.SeedSampleTemplatesAsync();
            return Ok(new { message = "Sample templates seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample templates");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to seed sample templates",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all custom video templates
    /// </summary>
    [HttpGet("custom")]
    public async Task<IActionResult> GetCustomTemplates([FromQuery] string? category = null)
    {
        try
        {
            var templates = await _templateService.GetCustomTemplatesAsync(category);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get custom templates");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve custom templates",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get a specific custom template by ID
    /// </summary>
    [HttpGet("custom/{id}")]
    public async Task<IActionResult> GetCustomTemplate(string id)
    {
        try
        {
            var template = await _templateService.GetCustomTemplateByIdAsync(id);
            
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Custom Template Not Found",
                    status = 404,
                    detail = $"Custom template with ID '{id}' was not found",
                    templateId = id
                });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a new custom video template
    /// </summary>
    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustomTemplate([FromBody] CreateCustomTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Template name is required",
                    field = "Name"
                });
            }

            var template = await _templateService.CreateCustomTemplateAsync(request);

            return CreatedAtAction(
                nameof(GetCustomTemplate),
                new { id = template.Id },
                template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create custom template {TemplateName}", request.Name);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to create custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update an existing custom template
    /// </summary>
    [HttpPut("custom/{id}")]
    public async Task<IActionResult> UpdateCustomTemplate(string id, [FromBody] UpdateCustomTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Template name is required",
                    field = "Name"
                });
            }

            var template = await _templateService.UpdateCustomTemplateAsync(id, request);
            
            if (template == null)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Custom Template Not Found",
                    status = 404,
                    detail = $"Custom template with ID '{id}' was not found",
                    templateId = id
                });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to update custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a custom template
    /// </summary>
    [HttpDelete("custom/{id}")]
    public async Task<IActionResult> DeleteCustomTemplate(string id)
    {
        try
        {
            var deleted = await _templateService.DeleteCustomTemplateAsync(id);
            
            if (!deleted)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Custom Template Not Found",
                    status = 404,
                    detail = $"Custom template with ID '{id}' was not found",
                    templateId = id
                });
            }

            return Ok(new { message = "Custom template deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to delete custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Duplicate a custom template
    /// </summary>
    [HttpPost("custom/{id}/duplicate")]
    public async Task<IActionResult> DuplicateCustomTemplate(string id)
    {
        try
        {
            var template = await _templateService.DuplicateCustomTemplateAsync(id);

            return CreatedAtAction(
                nameof(GetCustomTemplate),
                new { id = template.Id },
                template);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                type = "https://docs.aura.studio/errors/E404",
                title = "Custom Template Not Found",
                status = 404,
                detail = ex.Message,
                templateId = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to duplicate custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Set default custom template
    /// </summary>
    [HttpPost("custom/{id}/set-default")]
    public async Task<IActionResult> SetDefaultCustomTemplate(string id)
    {
        try
        {
            var success = await _templateService.SetDefaultCustomTemplateAsync(id);
            
            if (!success)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Custom Template Not Found",
                    status = 404,
                    detail = $"Custom template with ID '{id}' was not found",
                    templateId = id
                });
            }

            return Ok(new { message = "Default template set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to set default custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Export a custom template to JSON
    /// </summary>
    [HttpGet("custom/{id}/export")]
    public async Task<IActionResult> ExportCustomTemplate(string id)
    {
        try
        {
            var exportData = await _templateService.ExportCustomTemplateAsync(id);
            return Ok(exportData);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                type = "https://docs.aura.studio/errors/E404",
                title = "Custom Template Not Found",
                status = 404,
                detail = ex.Message,
                templateId = id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export custom template {TemplateId}", id);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to export custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Import a custom template from JSON
    /// </summary>
    [HttpPost("custom/import")]
    public async Task<IActionResult> ImportCustomTemplate([FromBody] TemplateExportData exportData)
    {
        try
        {
            if (exportData?.Template == null)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Template data is required"
                });
            }

            var template = await _templateService.ImportCustomTemplateAsync(exportData);

            return CreatedAtAction(
                nameof(GetCustomTemplate),
                new { id = template.Id },
                template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import custom template");
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to import custom template",
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    private object ConvertTemplateToProject(TemplateStructure templateStructure, string projectName)
    {
        // Convert template structure to project file format
        var now = DateTime.UtcNow.ToString("o");
        
        return new
        {
            version = "1.0.0",
            metadata = new
            {
                name = projectName,
                createdAt = now,
                lastModifiedAt = now,
                duration = templateStructure.Duration
            },
            settings = new
            {
                resolution = new
                {
                    width = templateStructure.Settings.Width,
                    height = templateStructure.Settings.Height
                },
                frameRate = templateStructure.Settings.FrameRate,
                sampleRate = 48000
            },
            tracks = templateStructure.Tracks.Select(t => new
            {
                id = t.Id,
                label = t.Label,
                type = t.Type,
                visible = true,
                locked = false
            }).ToList(),
            clips = templateStructure.Placeholders.Select(p => new
            {
                id = p.Id,
                trackId = p.TrackId,
                startTime = p.StartTime,
                duration = p.Duration,
                label = p.PlaceholderText,
                type = p.Type,
                isPlaceholder = true,
                preview = p.PreviewUrl
            }).ToList(),
            mediaLibrary = new List<object>(),
            playerPosition = 0
        };
    }
}
