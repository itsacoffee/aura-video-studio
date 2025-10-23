using Aura.Core.Models;
using Aura.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for input validation before generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ValidationController : ControllerBase
{
    private readonly PreGenerationValidator _validator;

    public ValidationController(PreGenerationValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Validates a brief and plan specification before starting generation
    /// </summary>
    /// <param name="request">Validation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("brief")]
    public async Task<IActionResult> ValidateBrief(
        [FromBody] ValidateBriefRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/validation/brief endpoint called", correlationId);
            Log.Information("[{CorrelationId}] Validating brief for topic: {Topic}", correlationId, request.Topic ?? "(null)");
            
            // Create Brief from request
            var brief = new Brief(
                Topic: request.Topic ?? string.Empty,
                Audience: request.Audience ?? string.Empty,
                Goal: request.Goal ?? string.Empty,
                Tone: request.Tone ?? "Informative",
                Language: request.Language ?? "en-US",
                Aspect: Aspect.Widescreen16x9
            );

            // Create PlanSpec from request
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(request.DurationMinutes ?? 1.0),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Default"
            );

            // Validate
            var result = await _validator.ValidateSystemReadyAsync(brief, planSpec, ct);

            Log.Information("[{CorrelationId}] Validation result: IsValid={IsValid}, Issues={IssueCount}", 
                correlationId, result.IsValid, result.Issues.Count);

            return Ok(new
            {
                isValid = result.IsValid,
                issues = result.Issues
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating brief");
            return StatusCode(500, new { error = "Error validating brief", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for brief validation
/// </summary>
public record ValidateBriefRequest(
    string? Topic,
    string? Audience = null,
    string? Goal = null,
    string? Tone = null,
    string? Language = null,
    double? DurationMinutes = null);
