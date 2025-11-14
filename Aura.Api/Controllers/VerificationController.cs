using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Aura.Core.Services.ContentVerification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for content verification and fact-checking
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VerificationController : ControllerBase
{
    private readonly ILogger<VerificationController> _logger;
    private readonly ContentVerificationOrchestrator _orchestrator;
    private readonly VerificationPersistence _persistence;
    private readonly SourceAttributionService _sourceService;
    private readonly ConfidenceAnalysisService _confidenceService;

    public VerificationController(
        ILogger<VerificationController> logger,
        ContentVerificationOrchestrator orchestrator,
        VerificationPersistence persistence,
        SourceAttributionService sourceService,
        ConfidenceAnalysisService confidenceService)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _persistence = persistence;
        _sourceService = sourceService;
        _confidenceService = confidenceService;
    }

    /// <summary>
    /// Verify content for factual accuracy
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyContent(
        [FromBody] VerificationRequestDto request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content is required" });
            }

            var verificationRequest = new VerificationRequest(
                ContentId: request.ContentId ?? Guid.NewGuid().ToString(),
                Content: request.Content,
                Options: request.Options ?? new VerificationOptions()
            );

            var result = await _orchestrator.VerifyContentAsync(verificationRequest, ct).ConfigureAwait(false);

            // Save result
            await _persistence.SaveVerificationResultAsync(result, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                result = new
                {
                    result.ContentId,
                    result.OverallStatus,
                    result.OverallConfidence,
                    ClaimCount = result.Claims.Count,
                    FactCheckCount = result.FactChecks.Count,
                    SourceCount = result.Sources.Count,
                    result.Warnings,
                    MisinformationRisk = result.Misinformation?.RiskLevel,
                    result.VerifiedAt
                },
                details = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying content");
            return StatusCode(500, new { error = "Failed to verify content" });
        }
    }

    /// <summary>
    /// Quick verification for real-time feedback
    /// </summary>
    [HttpPost("quick-verify")]
    public async Task<IActionResult> QuickVerify(
        [FromBody] QuickVerifyRequestDto request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content is required" });
            }

            var result = await _orchestrator.QuickVerifyAsync(request.Content, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing quick verification");
            return StatusCode(500, new { error = "Failed to perform quick verification" });
        }
    }

    /// <summary>
    /// Get verification result by content ID
    /// </summary>
    [HttpGet("{contentId}")]
    public async Task<IActionResult> GetVerification(
        string contentId,
        CancellationToken ct)
    {
        try
        {
            var result = await _persistence.LoadVerificationResultAsync(contentId, ct).ConfigureAwait(false);

            if (result == null)
            {
                return NotFound(new { error = "Verification result not found" });
            }

            return Ok(new
            {
                success = true,
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading verification result for {ContentId}", contentId);
            return StatusCode(500, new { error = "Failed to load verification result" });
        }
    }

    /// <summary>
    /// Get verification history for content
    /// </summary>
    [HttpGet("{contentId}/history")]
    public async Task<IActionResult> GetVerificationHistory(
        string contentId,
        CancellationToken ct,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var history = await _persistence.LoadVerificationHistoryAsync(contentId, maxResults, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                contentId,
                count = history.Count,
                history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading verification history for {ContentId}", contentId);
            return StatusCode(500, new { error = "Failed to load verification history" });
        }
    }

    /// <summary>
    /// Generate citations for sources
    /// </summary>
    [HttpPost("citations")]
    public async Task<IActionResult> GenerateCitations(
        [FromBody] CitationRequestDto request,
        CancellationToken ct)
    {
        try
        {
            if (request.Sources == null || request.Sources.Count == 0)
            {
                return BadRequest(new { error = "Sources are required" });
            }

            var citations = await _sourceService.GenerateCitationsAsync(
                request.Sources,
                request.Format ?? CitationFormat.APA,
                ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                format = request.Format ?? CitationFormat.APA,
                citations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating citations");
            return StatusCode(500, new { error = "Failed to generate citations" });
        }
    }

    /// <summary>
    /// Get verification statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
    {
        try
        {
            var stats = await _persistence.GetStatisticsAsync(ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                statistics = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verification statistics");
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    /// <summary>
    /// Delete verification result
    /// </summary>
    [HttpDelete("{contentId}")]
    public async Task<IActionResult> DeleteVerification(
        string contentId,
        CancellationToken ct)
    {
        try
        {
            await _persistence.DeleteVerificationResultAsync(contentId, ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                message = "Verification result deleted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting verification result for {ContentId}", contentId);
            return StatusCode(500, new { error = "Failed to delete verification result" });
        }
    }

    /// <summary>
    /// List all verified content
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> ListVerifiedContent(CancellationToken ct)
    {
        try
        {
            var contentIds = await _persistence.ListVerifiedContentAsync(ct).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                count = contentIds.Count,
                contentIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing verified content");
            return StatusCode(500, new { error = "Failed to list verified content" });
        }
    }
}

/// <summary>
/// Request DTO for verification
/// </summary>
public record VerificationRequestDto(
    string? ContentId,
    string Content,
    VerificationOptions? Options
);

/// <summary>
/// Request DTO for quick verification
/// </summary>
public record QuickVerifyRequestDto(
    string Content
);

/// <summary>
/// Request DTO for citation generation
/// </summary>
public record CitationRequestDto(
    List<SourceAttribution> Sources,
    CitationFormat? Format
);
