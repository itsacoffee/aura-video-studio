using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.ScriptReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for interactive script refinement and review
/// </summary>
[ApiController]
[Route("api/script")]
public class ScriptReviewController : ControllerBase
{
    private readonly ILogger<ScriptReviewController> _logger;
    private readonly ScriptRefinementService _refinementService;

    public ScriptReviewController(
        ILogger<ScriptReviewController> logger,
        ScriptRefinementService refinementService)
    {
        _logger = logger;
        _refinementService = refinementService;
    }

    /// <summary>
    /// Refine script based on natural language instruction
    /// </summary>
    [HttpPost("refine")]
    public async Task<IActionResult> RefineScript(
        [FromBody] ScriptRefinementRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CurrentScript))
            {
                return BadRequest(new { error = "CurrentScript is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Instruction))
            {
                return BadRequest(new { error = "Instruction is required" });
            }

            var response = await _refinementService.RefineScriptAsync(request, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                revisedScript = response.RevisedScript,
                diffSummary = response.DiffSummary,
                riskNote = response.RiskNote,
                generatedAt = response.GeneratedAt
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid refinement request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining script with instruction: {Instruction}", 
                request.Instruction);
            return StatusCode(500, new { error = "Failed to refine script" });
        }
    }
}
