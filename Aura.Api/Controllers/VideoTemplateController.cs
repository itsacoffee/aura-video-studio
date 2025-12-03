using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Templates;
using Aura.Core.Services.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video structure templates (script-to-video templates).
/// Provides pre-built video formats like Explainer, Listicle, Comparison, etc.
/// </summary>
[ApiController]
[Route("api/video-templates")]
public class VideoTemplateController : ControllerBase
{
    private readonly ILogger<VideoTemplateController> _logger;
    private readonly IVideoTemplateService _templateService;

    public VideoTemplateController(
        ILogger<VideoTemplateController> logger,
        IVideoTemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    /// <summary>
    /// Get all available video structure templates.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<VideoTemplateDto>> GetAllTemplates()
    {
        try
        {
            var templates = _templateService.GetAllTemplates();
            return Ok(templates.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video templates");
            return StatusCode(500, new { error = "Failed to get video templates" });
        }
    }

    /// <summary>
    /// Get a specific video template by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<VideoTemplateDto> GetTemplate(string id)
    {
        try
        {
            var template = _templateService.GetTemplateById(id);
            if (template == null)
            {
                return NotFound(new { error = $"Template '{id}' not found" });
            }

            return Ok(MapToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video template {TemplateId}", id);
            return StatusCode(500, new { error = "Failed to get video template" });
        }
    }

    /// <summary>
    /// Get templates filtered by category.
    /// </summary>
    [HttpGet("category/{category}")]
    public ActionResult<IEnumerable<VideoTemplateDto>> GetTemplatesByCategory(string category)
    {
        try
        {
            var templates = _templateService.GetTemplatesByCategory(category);
            return Ok(templates.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video templates for category {Category}", category);
            return StatusCode(500, new { error = "Failed to get video templates" });
        }
    }

    /// <summary>
    /// Search templates by query string.
    /// </summary>
    [HttpGet("search")]
    public ActionResult<IEnumerable<VideoTemplateDto>> SearchTemplates([FromQuery] string q)
    {
        try
        {
            var templates = _templateService.SearchTemplates(q ?? "");
            return Ok(templates.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching video templates with query '{Query}'", q);
            return StatusCode(500, new { error = "Failed to search video templates" });
        }
    }

    /// <summary>
    /// Apply a template with variable values to create a TemplatedBrief.
    /// </summary>
    [HttpPost("{id}/apply")]
    public async Task<ActionResult<TemplatedBriefDto>> ApplyTemplate(
        string id,
        [FromBody] ApplyTemplateRequestDto request,
        CancellationToken ct)
    {
        try
        {
            // Validate request
            if (request.VariableValues == null)
            {
                return BadRequest(new { error = "VariableValues is required" });
            }

            // Validate variables first
            var (isValid, errors) = _templateService.ValidateVariables(id, request.VariableValues);
            if (!isValid)
            {
                return BadRequest(new { error = "Validation failed", errors });
            }

            var result = await _templateService.ApplyTemplateAsync(
                id,
                request.VariableValues,
                request.Language,
                ct).ConfigureAwait(false);

            return Ok(MapToDto(result));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid template application request for {TemplateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying video template {TemplateId}", id);
            return StatusCode(500, new { error = "Failed to apply video template" });
        }
    }

    /// <summary>
    /// Preview a generated script from a template without creating a full brief.
    /// </summary>
    [HttpPost("{id}/preview-script")]
    public async Task<ActionResult<ScriptPreviewResponseDto>> PreviewScript(
        string id,
        [FromBody] ApplyTemplateRequestDto request,
        CancellationToken ct)
    {
        try
        {
            // Validate request
            if (request.VariableValues == null)
            {
                return BadRequest(new { error = "VariableValues is required" });
            }

            // Validate variables first
            var (isValid, errors) = _templateService.ValidateVariables(id, request.VariableValues);
            if (!isValid)
            {
                return BadRequest(new { error = "Validation failed", errors });
            }

            var result = await _templateService.PreviewScriptAsync(
                id,
                request.VariableValues,
                ct).ConfigureAwait(false);

            return Ok(new ScriptPreviewResponseDto
            {
                Script = result.Script,
                Sections = result.Sections.Select(s => new GeneratedSectionDto
                {
                    Name = s.Name,
                    Content = s.Content,
                    SuggestedDurationSeconds = (int)s.SuggestedDuration.TotalSeconds,
                    Type = s.Type.ToString()
                }).ToList(),
                EstimatedDurationSeconds = (int)result.EstimatedDuration.TotalSeconds,
                SceneCount = result.SceneCount
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid script preview request for {TemplateId}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing script for template {TemplateId}", id);
            return StatusCode(500, new { error = "Failed to preview script" });
        }
    }

    /// <summary>
    /// Validate variable values against a template's requirements.
    /// </summary>
    [HttpPost("{id}/validate")]
    public ActionResult<TemplateValidationResultDto> ValidateVariables(
        string id,
        [FromBody] ApplyTemplateRequestDto request)
    {
        try
        {
            if (request.VariableValues == null)
            {
                return BadRequest(new { error = "VariableValues is required" });
            }

            var (isValid, errors) = _templateService.ValidateVariables(id, request.VariableValues);

            return Ok(new TemplateValidationResultDto
            {
                IsValid = isValid,
                Errors = errors.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating variables for template {TemplateId}", id);
            return StatusCode(500, new { error = "Failed to validate variables" });
        }
    }

    // DTOs

    private static VideoTemplateDto MapToDto(VideoTemplate template)
    {
        return new VideoTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Structure = new TemplateStructureDto
            {
                Sections = template.Structure.Sections.Select(s => new TemplateSectionDto
                {
                    Name = s.Name,
                    Purpose = s.Purpose,
                    Type = s.Type.ToString(),
                    SuggestedDurationSeconds = (int)s.SuggestedDuration.TotalSeconds,
                    PromptTemplate = s.PromptTemplate,
                    IsOptional = s.IsOptional,
                    ExampleContent = s.ExampleContent?.ToList(),
                    IsRepeatable = s.IsRepeatable,
                    RepeatCountVariable = s.RepeatCountVariable
                }).ToList(),
                EstimatedDurationSeconds = (int)template.Structure.EstimatedDuration.TotalSeconds,
                RecommendedSceneCount = template.Structure.RecommendedSceneCount
            },
            Variables = template.Variables.Select(v => new TemplateVariableDto
            {
                Name = v.Name,
                DisplayName = v.DisplayName,
                Type = v.Type.ToString(),
                DefaultValue = v.DefaultValue,
                Placeholder = v.Placeholder,
                IsRequired = v.IsRequired,
                Options = v.Options?.ToList(),
                MinValue = v.MinValue,
                MaxValue = v.MaxValue
            }).ToList(),
            Thumbnail = template.Thumbnail != null ? new TemplateThumbnailDto
            {
                IconName = template.Thumbnail.IconName,
                AccentColor = template.Thumbnail.AccentColor
            } : null,
            Metadata = new TemplateMetadataDto
            {
                RecommendedAudiences = template.Metadata.RecommendedAudiences.ToList(),
                RecommendedTones = template.Metadata.RecommendedTones.ToList(),
                SupportedAspects = template.Metadata.SupportedAspects.Select(a => a.ToString()).ToList(),
                MinDurationSeconds = (int)template.Metadata.MinDuration.TotalSeconds,
                MaxDurationSeconds = (int)template.Metadata.MaxDuration.TotalSeconds,
                Tags = template.Metadata.Tags.ToList()
            }
        };
    }

    private static TemplatedBriefDto MapToDto(TemplatedBrief brief)
    {
        return new TemplatedBriefDto
        {
            Brief = new TemplateBriefResultDto
            {
                Topic = brief.Brief.Topic,
                Audience = brief.Brief.Audience,
                Goal = brief.Brief.Goal,
                Tone = brief.Brief.Tone,
                Language = brief.Brief.Language,
                Aspect = brief.Brief.Aspect.ToString()
            },
            PlanSpec = new TemplatePlanSpecResultDto
            {
                TargetDurationSeconds = (int)brief.PlanSpec.TargetDuration.TotalSeconds,
                Pacing = brief.PlanSpec.Pacing.ToString(),
                Density = brief.PlanSpec.Density.ToString(),
                Style = brief.PlanSpec.Style,
                TargetSceneCount = brief.PlanSpec.TargetSceneCount
            },
            Sections = brief.Sections.Select(s => new GeneratedSectionDto
            {
                Name = s.Name,
                Content = s.Content,
                SuggestedDurationSeconds = (int)s.SuggestedDuration.TotalSeconds,
                Type = s.Type.ToString()
            }).ToList(),
            SourceTemplateId = brief.SourceTemplate.Id
        };
    }
}

// DTOs for API responses

public class VideoTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TemplateStructureDto Structure { get; set; } = new();
    public List<TemplateVariableDto> Variables { get; set; } = new();
    public TemplateThumbnailDto? Thumbnail { get; set; }
    public TemplateMetadataDto Metadata { get; set; } = new();
}

public class TemplateStructureDto
{
    public List<TemplateSectionDto> Sections { get; set; } = new();
    public int EstimatedDurationSeconds { get; set; }
    public int RecommendedSceneCount { get; set; }
}

public class TemplateSectionDto
{
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int SuggestedDurationSeconds { get; set; }
    public string PromptTemplate { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public List<string>? ExampleContent { get; set; }
    public bool IsRepeatable { get; set; }
    public string? RepeatCountVariable { get; set; }
}

public class TemplateVariableDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public List<string>? Options { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
}

public class TemplateThumbnailDto
{
    public string IconName { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
}

public class TemplateMetadataDto
{
    public List<string> RecommendedAudiences { get; set; } = new();
    public List<string> RecommendedTones { get; set; } = new();
    public List<string> SupportedAspects { get; set; } = new();
    public int MinDurationSeconds { get; set; }
    public int MaxDurationSeconds { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ApplyTemplateRequestDto
{
    public Dictionary<string, string> VariableValues { get; set; } = new();
    public string? Language { get; set; }
    public string? Aspect { get; set; }
    public string? Pacing { get; set; }
    public string? Density { get; set; }
}

public class TemplatedBriefDto
{
    public TemplateBriefResultDto Brief { get; set; } = new();
    public TemplatePlanSpecResultDto PlanSpec { get; set; } = new();
    public List<GeneratedSectionDto> Sections { get; set; } = new();
    public string SourceTemplateId { get; set; } = string.Empty;
}

public class TemplateBriefResultDto
{
    public string Topic { get; set; } = string.Empty;
    public string? Audience { get; set; }
    public string? Goal { get; set; }
    public string Tone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Aspect { get; set; } = string.Empty;
}

public class TemplatePlanSpecResultDto
{
    public int TargetDurationSeconds { get; set; }
    public string Pacing { get; set; } = string.Empty;
    public string Density { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public int? TargetSceneCount { get; set; }
}

public class GeneratedSectionDto
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int SuggestedDurationSeconds { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class ScriptPreviewResponseDto
{
    public string Script { get; set; } = string.Empty;
    public List<GeneratedSectionDto> Sections { get; set; } = new();
    public int EstimatedDurationSeconds { get; set; }
    public int SceneCount { get; set; }
}

public class TemplateValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
