using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Visual;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for visual image selection with aesthetic scoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VisualSelectionController : ControllerBase
{
    private readonly ILogger<VisualSelectionController> _logger;
    private readonly ImageSelectionService _selectionService;
    private readonly AestheticScoringService _scoringService;
    private readonly VisualSelectionService? _visualSelectionService;
    private readonly VisualPromptRefinementService? _refinementService;
    private readonly LicensingExportService? _exportService;

    public VisualSelectionController(
        ILogger<VisualSelectionController> logger,
        ImageSelectionService selectionService,
        AestheticScoringService scoringService,
        VisualSelectionService? visualSelectionService = null,
        VisualPromptRefinementService? refinementService = null,
        LicensingExportService? exportService = null)
    {
        _logger = logger;
        _selectionService = selectionService;
        _scoringService = scoringService;
        _visualSelectionService = visualSelectionService;
        _refinementService = refinementService;
        _exportService = exportService;
    }

    /// <summary>
    /// Select best image for a single scene
    /// </summary>
    [HttpPost("select")]
    public async Task<ActionResult<ImageSelectionResultDto>> SelectImageForScene(
        [FromBody] ImageSelectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Selecting image for scene {SceneIndex}, CorrelationId: {CorrelationId}",
                request.SceneIndex,
                HttpContext.TraceIdentifier);

            var prompt = MapToVisualPrompt(request);
            var config = MapToConfig(request.Config);

            var result = await _selectionService.SelectImageForSceneAsync(prompt, config, cancellationToken);

            var dto = MapToDto(result);

            _logger.LogInformation(
                "Image selection completed for scene {SceneIndex}. Selected: {Selected}, Score: {Score:F1}",
                request.SceneIndex,
                dto.SelectedImage != null,
                dto.SelectedImage?.OverallScore ?? 0);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error selecting image for scene {SceneIndex}, CorrelationId: {CorrelationId}",
                request.SceneIndex,
                HttpContext.TraceIdentifier);

            return StatusCode(500, new
            {
                error = "Image selection failed",
                message = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Select images for multiple scenes in batch
    /// </summary>
    [HttpPost("select/batch")]
    public async Task<ActionResult<BatchImageSelectionResponse>> SelectImagesForScenes(
        [FromBody] BatchImageSelectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Batch selecting images for {Count} scenes, CorrelationId: {CorrelationId}",
                request.Scenes.Count,
                HttpContext.TraceIdentifier);

            var stopwatch = Stopwatch.StartNew();

            var prompts = request.Scenes.Select(MapToVisualPrompt).ToList();
            var config = MapToConfig(request.Config);

            var results = await _selectionService.SelectImagesForScenesAsync(prompts, config, cancellationToken);

            stopwatch.Stop();

            var dtos = results.Select(MapToDto).ToList();
            var successCount = dtos.Count(d => d.MeetsCriteria);

            var response = new BatchImageSelectionResponse
            {
                Results = dtos,
                TotalScenes = request.Scenes.Count,
                SuccessfulSelections = successCount,
                TotalSelectionTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };

            _logger.LogInformation(
                "Batch selection completed. {Success}/{Total} successful, Time: {Time:F0}ms",
                successCount,
                request.Scenes.Count,
                response.TotalSelectionTimeMs);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in batch image selection, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            return StatusCode(500, new
            {
                error = "Batch image selection failed",
                message = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get aesthetic score for an image candidate
    /// </summary>
    [HttpPost("score")]
    public async Task<ActionResult<ImageCandidateDto>> ScoreImageCandidate(
        [FromBody] ScoreImageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Scoring image candidate from {Source}, CorrelationId: {CorrelationId}",
                request.Source,
                HttpContext.TraceIdentifier);

            var candidate = new ImageCandidate
            {
                ImageUrl = request.ImageUrl,
                Source = request.Source,
                Width = request.Width,
                Height = request.Height,
                GenerationLatencyMs = request.GenerationLatencyMs
            };

            var prompt = new VisualPrompt
            {
                SceneIndex = 0,
                DetailedDescription = request.DetailedDescription ?? string.Empty,
                Subject = request.Subject ?? string.Empty,
                NarrativeKeywords = (request.NarrativeKeywords ?? new List<string>()).ToArray(),
                Style = ParseVisualStyle(request.Style ?? "Cinematic")
            };

            var score = await _scoringService.ScoreImageAsync(candidate, prompt, cancellationToken);

            var dto = new ImageCandidateDto
            {
                ImageUrl = candidate.ImageUrl,
                Source = candidate.Source,
                Width = candidate.Width,
                Height = candidate.Height,
                OverallScore = score,
                GenerationLatencyMs = candidate.GenerationLatencyMs
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error scoring image candidate, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);

            return StatusCode(500, new
            {
                error = "Image scoring failed",
                message = ex.Message,
                correlationId = HttpContext.TraceIdentifier
            });
        }
    }

    private static VisualPrompt MapToVisualPrompt(ImageSelectionRequest request)
    {
        return new VisualPrompt
        {
            SceneIndex = request.SceneIndex,
            DetailedDescription = request.DetailedDescription,
            Subject = request.Subject,
            Framing = request.Framing,
            NarrativeKeywords = request.NarrativeKeywords,
            Style = ParseVisualStyle(request.Style),
            QualityTier = ParseQualityTier(request.QualityTier)
        };
    }

    private static ImageSelectionConfig? MapToConfig(ImageSelectionConfigDto? dto)
    {
        if (dto == null)
        {
            return null;
        }

        return new ImageSelectionConfig
        {
            MinimumAestheticThreshold = dto.MinimumAestheticThreshold,
            CandidatesPerScene = dto.CandidatesPerScene,
            AestheticWeight = dto.AestheticWeight,
            KeywordWeight = dto.KeywordWeight,
            QualityWeight = dto.QualityWeight,
            PreferGeneratedImages = dto.PreferGeneratedImages,
            MaxGenerationTimeSeconds = dto.MaxGenerationTimeSeconds
        };
    }

    private static ImageSelectionResultDto MapToDto(ImageSelectionResult result)
    {
        return new ImageSelectionResultDto
        {
            SceneIndex = result.SceneIndex,
            SelectedImage = result.SelectedImage != null ? MapCandidateToDto(result.SelectedImage) : null,
            Candidates = result.Candidates.Select(MapCandidateToDto).ToList(),
            MinimumAestheticThreshold = result.MinimumAestheticThreshold,
            NarrativeKeywords = result.NarrativeKeywords.ToList(),
            SelectionTimeMs = result.SelectionTimeMs,
            MeetsCriteria = result.MeetsCriteria,
            Warnings = result.Warnings.ToList()
        };
    }

    private static ImageCandidateDto MapCandidateToDto(ImageCandidate candidate)
    {
        return new ImageCandidateDto
        {
            ImageUrl = candidate.ImageUrl,
            Source = candidate.Source,
            AestheticScore = candidate.AestheticScore,
            KeywordCoverageScore = candidate.KeywordCoverageScore,
            QualityScore = candidate.QualityScore,
            OverallScore = candidate.OverallScore,
            Reasoning = candidate.Reasoning,
            Licensing = candidate.Licensing != null ? MapLicensingToDto(candidate.Licensing) : null,
            Width = candidate.Width,
            Height = candidate.Height,
            RejectionReasons = candidate.RejectionReasons.ToList(),
            GenerationLatencyMs = candidate.GenerationLatencyMs
        };
    }

    private static LicensingInfoDto MapLicensingToDto(LicensingInfo licensing)
    {
        return new LicensingInfoDto
        {
            LicenseType = licensing.LicenseType,
            Attribution = licensing.Attribution,
            LicenseUrl = licensing.LicenseUrl,
            CommercialUseAllowed = licensing.CommercialUseAllowed,
            AttributionRequired = licensing.AttributionRequired,
            CreatorName = licensing.CreatorName,
            CreatorUrl = licensing.CreatorUrl,
            SourcePlatform = licensing.SourcePlatform
        };
    }

    private static VisualStyle ParseVisualStyle(string style)
    {
        return style.ToLowerInvariant() switch
        {
            "realistic" => VisualStyle.Realistic,
            "cinematic" => VisualStyle.Cinematic,
            "illustrated" => VisualStyle.Illustrated,
            "abstract" => VisualStyle.Abstract,
            "animated" => VisualStyle.Animated,
            "documentary" => VisualStyle.Documentary,
            "dramatic" => VisualStyle.Dramatic,
            "minimalist" => VisualStyle.Minimalist,
            "vintage" => VisualStyle.Vintage,
            "modern" => VisualStyle.Modern,
            _ => VisualStyle.Cinematic
        };
    }

    private static VisualQualityTier ParseQualityTier(string tier)
    {
        return tier.ToLowerInvariant() switch
        {
            "basic" => VisualQualityTier.Basic,
            "standard" => VisualQualityTier.Standard,
            "enhanced" => VisualQualityTier.Enhanced,
            "premium" => VisualQualityTier.Premium,
            _ => VisualQualityTier.Standard
        };
    }

    /// <summary>
    /// Export licensing information to CSV
    /// </summary>
    [HttpGet("{jobId}/export/licensing/csv")]
    public async Task<IActionResult> ExportLicensingCsv(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (_visualSelectionService == null || _exportService == null)
        {
            return StatusCode(501, new { error = "Licensing export not configured" });
        }

        try
        {
            var selections = await _visualSelectionService.GetSelectionsForJobAsync(jobId, cancellationToken);
            var csv = await _exportService.ExportToCsvAsync(selections, jobId, cancellationToken);

            return Content(csv, "text/csv", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export licensing CSV for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to export licensing information" });
        }
    }

    /// <summary>
    /// Export licensing information to JSON
    /// </summary>
    [HttpGet("{jobId}/export/licensing/json")]
    public async Task<IActionResult> ExportLicensingJson(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (_visualSelectionService == null || _exportService == null)
        {
            return StatusCode(501, new { error = "Licensing export not configured" });
        }

        try
        {
            var selections = await _visualSelectionService.GetSelectionsForJobAsync(jobId, cancellationToken);
            var json = await _exportService.ExportToJsonAsync(selections, jobId, cancellationToken);

            return Content(json, "application/json", System.Text.Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export licensing JSON for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to export licensing information" });
        }
    }

    /// <summary>
    /// Get licensing summary for a job
    /// </summary>
    [HttpGet("{jobId}/licensing/summary")]
    public async Task<IActionResult> GetLicensingSummary(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (_visualSelectionService == null || _exportService == null)
        {
            return StatusCode(501, new { error = "Licensing export not configured" });
        }

        try
        {
            var selections = await _visualSelectionService.GetSelectionsForJobAsync(jobId, cancellationToken);
            var summary = await _exportService.GenerateSummaryAsync(selections, cancellationToken);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get licensing summary for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to get licensing summary" });
        }
    }
}

/// <summary>
/// Request for scoring an image candidate
/// </summary>
public record ScoreImageRequest
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public double GenerationLatencyMs { get; init; }
    public string? DetailedDescription { get; init; }
    public string? Subject { get; init; }
    public List<string>? NarrativeKeywords { get; init; }
    public string? Style { get; init; }
}
