using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for SSML planning, validation, and optimization
/// </summary>
[ApiController]
[Route("api/ssml")]
public class SSMLController : ControllerBase
{
    private readonly ILogger<SSMLController> _logger;
    private readonly SSMLPlannerService _plannerService;
    private readonly IEnumerable<ISSMLMapper> _mappers;

    public SSMLController(
        ILogger<SSMLController> logger,
        SSMLPlannerService plannerService,
        IEnumerable<ISSMLMapper> mappers)
    {
        _logger = logger;
        _plannerService = plannerService;
        _mappers = mappers;
    }

    /// <summary>
    /// Plan SSML for script lines with duration targeting
    /// </summary>
    [HttpPost("plan")]
    public async Task<IActionResult> PlanSSML(
        [FromBody] SSMLPlanningRequestDto request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Planning SSML for {LineCount} lines with provider {Provider}, CorrelationId: {CorrelationId}",
                request.ScriptLines.Count, request.TargetProvider, HttpContext.TraceIdentifier);

            if (!Enum.TryParse<VoiceProvider>(request.TargetProvider, true, out var provider))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Status = 400,
                    Detail = $"Unknown TTS provider: {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var scriptLines = request.ScriptLines.Select(line => new ScriptLine(
                line.SceneIndex,
                line.Text,
                TimeSpan.FromSeconds(line.StartSeconds),
                TimeSpan.FromSeconds(line.DurationSeconds)
            )).ToList();

            var voiceSpec = new VoiceSpec(
                request.VoiceSpec.VoiceName,
                request.VoiceSpec.Rate,
                request.VoiceSpec.Pitch,
                Aura.Core.Models.PauseStyle.Natural
            );

            var planningRequest = new SSMLPlanningRequest
            {
                ScriptLines = scriptLines,
                TargetProvider = provider,
                VoiceSpec = voiceSpec,
                TargetDurations = request.TargetDurations,
                DurationTolerance = request.DurationTolerance,
                MaxFittingIterations = request.MaxFittingIterations,
                EnableAggressiveAdjustments = request.EnableAggressiveAdjustments
            };

            var result = await _plannerService.PlanSSMLAsync(planningRequest, ct);

            var responseDto = new SSMLPlanningResultDto(
                Segments: result.Segments.Select(s => new SSMLSegmentResultDto(
                    s.SceneIndex,
                    s.OriginalText,
                    s.SsmlMarkup,
                    s.EstimatedDurationMs,
                    s.TargetDurationMs,
                    s.DeviationPercent,
                    new ProsodyAdjustmentsDto(
                        s.Adjustments.Rate,
                        s.Adjustments.Pitch,
                        s.Adjustments.Volume,
                        s.Adjustments.Pauses.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        s.Adjustments.Emphasis.Select(e => new EmphasisSpanDto(
                            e.StartPosition,
                            e.Length,
                            e.Level.ToString()
                        )).ToList(),
                        s.Adjustments.Iterations
                    ),
                    s.TimingMarkers.Select(m => new TimingMarkerDto(
                        m.OffsetMs,
                        m.Name,
                        m.Metadata
                    )).ToList()
                )).ToList(),
                Stats: new DurationFittingStatsDto(
                    result.Stats.SegmentsAdjusted,
                    result.Stats.AverageFitIterations,
                    result.Stats.MaxFitIterations,
                    result.Stats.WithinTolerancePercent,
                    result.Stats.AverageDeviation,
                    result.Stats.MaxDeviation,
                    result.Stats.TargetDurationSeconds,
                    result.Stats.ActualDurationSeconds
                ),
                Warnings: result.Warnings.ToList(),
                PlanningDurationMs: result.PlanningDurationMs
            );

            return Ok(responseDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during SSML planning");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Operation",
                Status = 400,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error planning SSML, CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(500, new ProblemDetails
            {
                Title = "SSML Planning Failed",
                Status = 500,
                Detail = "An error occurred while planning SSML",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Validate SSML for provider compatibility
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateSSML([FromBody] SSMLValidationRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Validating SSML for provider {Provider}, CorrelationId: {CorrelationId}",
                request.TargetProvider, HttpContext.TraceIdentifier);

            if (!Enum.TryParse<VoiceProvider>(request.TargetProvider, true, out var provider))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Status = 400,
                    Detail = $"Unknown TTS provider: {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var mapper = _mappers.FirstOrDefault(m => m.Provider == provider);
            if (mapper == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Mapper Not Found",
                    Status = 404,
                    Detail = $"No SSML mapper found for provider {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var validationResult = mapper.Validate(request.Ssml);

            var responseDto = new SSMLValidationResultDto(
                validationResult.IsValid,
                validationResult.Errors.ToList(),
                validationResult.Warnings.ToList(),
                validationResult.RepairSuggestions.Select(s => new SSMLRepairSuggestionDto(
                    s.Issue,
                    s.Suggestion,
                    s.CanAutoFix
                )).ToList()
            );

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SSML, CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(500, new ProblemDetails
            {
                Title = "SSML Validation Failed",
                Status = 500,
                Detail = "An error occurred while validating SSML",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Auto-repair invalid SSML
    /// </summary>
    [HttpPost("repair")]
    public IActionResult RepairSSML([FromBody] SSMLRepairRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Repairing SSML for provider {Provider}, CorrelationId: {CorrelationId}",
                request.TargetProvider, HttpContext.TraceIdentifier);

            if (!Enum.TryParse<VoiceProvider>(request.TargetProvider, true, out var provider))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Status = 400,
                    Detail = $"Unknown TTS provider: {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var mapper = _mappers.FirstOrDefault(m => m.Provider == provider);
            if (mapper == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Mapper Not Found",
                    Status = 404,
                    Detail = $"No SSML mapper found for provider {request.TargetProvider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var originalValidation = mapper.Validate(request.Ssml);
            var repairedSsml = mapper.AutoRepair(request.Ssml);
            var repairedValidation = mapper.Validate(repairedSsml);

            var repairsApplied = new List<string>();
            if (!originalValidation.IsValid && repairedValidation.IsValid)
            {
                repairsApplied.AddRange(originalValidation.RepairSuggestions
                    .Where(s => s.CanAutoFix)
                    .Select(s => s.Suggestion));
            }

            var responseDto = new SSMLRepairResultDto(
                repairedSsml,
                !originalValidation.IsValid && repairedValidation.IsValid,
                repairsApplied
            );

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error repairing SSML, CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(500, new ProblemDetails
            {
                Title = "SSML Repair Failed",
                Status = 500,
                Detail = "An error occurred while repairing SSML",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get provider-specific SSML constraints
    /// </summary>
    [HttpGet("constraints/{provider}")]
    public IActionResult GetConstraints(string provider)
    {
        try
        {
            _logger.LogInformation(
                "Getting SSML constraints for provider {Provider}, CorrelationId: {CorrelationId}",
                provider, HttpContext.TraceIdentifier);

            if (!Enum.TryParse<VoiceProvider>(provider, true, out var voiceProvider))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Provider",
                    Status = 400,
                    Detail = $"Unknown TTS provider: {provider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var mapper = _mappers.FirstOrDefault(m => m.Provider == voiceProvider);
            if (mapper == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Mapper Not Found",
                    Status = 404,
                    Detail = $"No SSML mapper found for provider {provider}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var constraints = mapper.GetConstraints();

            var responseDto = new ProviderSSMLConstraintsDto(
                constraints.SupportedTags.ToList(),
                constraints.SupportedProsodyAttributes.ToList(),
                constraints.RateRange.Min,
                constraints.RateRange.Max,
                constraints.PitchRange.Min,
                constraints.PitchRange.Max,
                constraints.VolumeRange.Min,
                constraints.VolumeRange.Max,
                constraints.MaxPauseDurationMs,
                constraints.SupportsTimingMarkers,
                constraints.MaxTextLength
            );

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SSML constraints, CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Get Constraints Failed",
                Status = 500,
                Detail = "An error occurred while getting SSML constraints",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}
