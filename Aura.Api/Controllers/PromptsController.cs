using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models;
using Aura.Core.Services.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiV1 = Aura.Api.Models.ApiModels.V1;

namespace Aura.Api.Controllers;

/// <summary>
/// API controller for prompt engineering features
/// Provides endpoints for prompt preview, few-shot examples, and chain-of-thought generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromptsController : ControllerBase
{
    private readonly ILogger<PromptsController> _logger;
    private readonly PromptCustomizationService _promptService;

    public PromptsController(
        ILogger<PromptsController> logger,
        PromptCustomizationService promptService)
    {
        _logger = logger;
        _promptService = promptService;
    }

    /// <summary>
    /// Get preview of prompt with variable substitutions before LLM invocation
    /// GET /api/prompts/preview
    /// </summary>
    [HttpPost("preview")]
    [ProducesResponseType(typeof(PromptPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<PromptPreviewResponse> GetPromptPreview([FromBody] PromptPreviewRequest request)
    {
        _logger.LogInformation("Generating prompt preview for topic: {Topic}, CorrelationId: {CorrelationId}",
            request.Topic, HttpContext.TraceIdentifier);

        try
        {
            var brief = new Brief(
                Topic: request.Topic,
                Audience: request.Audience,
                Goal: request.Goal,
                Tone: request.Tone,
                Language: request.Language,
                Aspect: MapAspect(request.Aspect),
                PromptModifiers: MapPromptModifiers(request.PromptModifiers));

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(request.TargetDurationMinutes),
                Pacing: MapPacing(request.Pacing),
                Density: MapDensity(request.Density),
                Style: request.Style);

            var preview = _promptService.GeneratePreview(brief, planSpec, brief.PromptModifiers);

            var response = new PromptPreviewResponse(
                SystemPrompt: preview.SystemPrompt,
                UserPrompt: preview.UserPrompt,
                FinalPrompt: preview.FinalPrompt,
                SubstitutedVariables: preview.SubstitutedVariables,
                PromptVersion: preview.PromptVersion,
                EstimatedTokens: preview.EstimatedTokens);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate prompt preview, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            return BadRequest(new ProblemDetails
            {
                Title = "Prompt Preview Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get list of available few-shot examples
    /// GET /api/prompts/list-examples
    /// </summary>
    [HttpGet("list-examples")]
    [ProducesResponseType(typeof(ListExamplesResponse), StatusCodes.Status200OK)]
    public ActionResult<ListExamplesResponse> ListExamples([FromQuery] string? videoType = null)
    {
        _logger.LogInformation("Listing few-shot examples, VideoType: {VideoType}, CorrelationId: {CorrelationId}",
            videoType, HttpContext.TraceIdentifier);

        var library = _promptService.GetPromptLibrary();

        var examples = string.IsNullOrWhiteSpace(videoType)
            ? library.GetAllExamples()
            : library.GetExamplesByType(videoType);

        var exampleDtos = examples.Select(e => new FewShotExampleDto(
            VideoType: e.VideoType,
            ExampleName: e.ExampleName,
            Description: e.Description,
            SampleBrief: e.SampleBrief,
            SampleOutput: e.SampleOutput,
            KeyTechniques: e.KeyTechniques)).ToList();

        var videoTypes = library.GetVideoTypes().ToList();

        var response = new ListExamplesResponse(
            Examples: exampleDtos,
            VideoTypes: videoTypes);

        return Ok(response);
    }

    /// <summary>
    /// Get available prompt versions
    /// GET /api/prompts/versions
    /// </summary>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(ListPromptVersionsResponse), StatusCodes.Status200OK)]
    public ActionResult<ListPromptVersionsResponse> ListPromptVersions()
    {
        _logger.LogInformation("Listing prompt versions, CorrelationId: {CorrelationId}",
            HttpContext.TraceIdentifier);

        var versions = _promptService.GetPromptVersions();

        var versionDtos = versions.Values.Select(v => new PromptVersionDto(
            Version: v.Version,
            Name: v.Name,
            Description: v.Description,
            IsDefault: v.IsDefault)).ToList();

        var defaultVersion = versions.Values.FirstOrDefault(v => v.IsDefault)?.Version ?? "default-v1";

        var response = new ListPromptVersionsResponse(
            Versions: versionDtos,
            DefaultVersion: defaultVersion);

        return Ok(response);
    }

    /// <summary>
    /// Validate custom instructions for security
    /// POST /api/prompts/validate-instructions
    /// </summary>
    [HttpPost("validate-instructions")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    public ActionResult<ValidationResultDto> ValidateInstructions([FromBody] ValidateInstructionsRequest request)
    {
        _logger.LogInformation("Validating custom instructions, Length: {Length}, CorrelationId: {CorrelationId}",
            request.Instructions?.Length ?? 0, HttpContext.TraceIdentifier);

        var isValid = _promptService.ValidateCustomInstructions(request.Instructions ?? string.Empty);

        return Ok(new ValidationResultDto(
            IsValid: isValid,
            Message: isValid ? "Instructions are valid" : "Instructions contain potentially unsafe patterns",
            Errors: isValid ? null : new List<string> { "Custom instructions failed security validation" }));
    }

    private static Core.Models.Aspect MapAspect(ApiV1.Aspect aspect)
    {
        return aspect switch
        {
            ApiV1.Aspect.Widescreen16x9 => Core.Models.Aspect.Widescreen16x9,
            ApiV1.Aspect.Vertical9x16 => Core.Models.Aspect.Vertical9x16,
            ApiV1.Aspect.Square1x1 => Core.Models.Aspect.Square1x1,
            _ => Core.Models.Aspect.Widescreen16x9
        };
    }

    private static Core.Models.Pacing MapPacing(ApiV1.Pacing pacing)
    {
        return pacing switch
        {
            ApiV1.Pacing.Chill => Core.Models.Pacing.Chill,
            ApiV1.Pacing.Conversational => Core.Models.Pacing.Conversational,
            ApiV1.Pacing.Fast => Core.Models.Pacing.Fast,
            _ => Core.Models.Pacing.Conversational
        };
    }

    private static Core.Models.Density MapDensity(ApiV1.Density density)
    {
        return density switch
        {
            ApiV1.Density.Sparse => Core.Models.Density.Sparse,
            ApiV1.Density.Balanced => Core.Models.Density.Balanced,
            ApiV1.Density.Dense => Core.Models.Density.Dense,
            _ => Core.Models.Density.Balanced
        };
    }

    private static PromptModifiers? MapPromptModifiers(PromptModifiersDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        return new PromptModifiers(
            AdditionalInstructions: dto.AdditionalInstructions,
            ExampleStyle: dto.ExampleStyle,
            EnableChainOfThought: dto.EnableChainOfThought,
            PromptVersion: dto.PromptVersion);
    }
}

/// <summary>
/// Request to validate custom instructions
/// </summary>
public record ValidateInstructionsRequest(string? Instructions);

/// <summary>
/// Validation result DTO
/// </summary>
public record ValidationResultDto(
    bool IsValid,
    string Message,
    List<string>? Errors);
