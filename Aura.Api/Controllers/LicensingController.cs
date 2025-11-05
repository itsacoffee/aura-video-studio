using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models.Licensing;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Licensing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for licensing and provenance management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LicensingController : ControllerBase
{
    private readonly ILicensingService _licensingService;
    private readonly ILogger<LicensingController> _logger;

    public LicensingController(
        ILicensingService licensingService,
        ILogger<LicensingController> logger)
    {
        _licensingService = licensingService ?? throw new ArgumentNullException(nameof(licensingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate licensing manifest for a project
    /// </summary>
    /// <param name="request">Generate manifest request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Licensing manifest</returns>
    [HttpPost("manifest/generate")]
    [ProducesResponseType(typeof(ProjectLicensingManifestDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GenerateManifest(
        [FromBody] GenerateLicensingManifestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating licensing manifest for project {ProjectId}, CorrelationId: {CorrelationId}",
                request.ProjectId, HttpContext.TraceIdentifier);

            if (string.IsNullOrEmpty(request.ProjectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            ProjectLicensingManifest manifest;

            if (request.TimelineData != null)
            {
                var timelineJson = JsonSerializer.Serialize(request.TimelineData);
                var timeline = JsonSerializer.Deserialize<Aura.Core.Models.Timeline.EditableTimeline>(timelineJson);

                if (timeline != null && _licensingService is Aura.Core.Services.Licensing.LicensingService service)
                {
                    manifest = service.GenerateManifestFromTimeline(request.ProjectId, timeline);
                }
                else
                {
                    manifest = await _licensingService.GenerateManifestAsync(request.ProjectId, cancellationToken);
                }
            }
            else
            {
                manifest = await _licensingService.GenerateManifestAsync(request.ProjectId, cancellationToken);
            }

            var dto = MapToDto(manifest);

            _logger.LogInformation("Generated licensing manifest for project {ProjectId} with {AssetCount} assets",
                request.ProjectId, manifest.Assets.Count);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate licensing manifest for project {ProjectId}", request.ProjectId);
            return StatusCode(500, new { error = "Failed to generate licensing manifest", details = ex.Message });
        }
    }

    /// <summary>
    /// Get existing licensing manifest for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Licensing manifest</returns>
    [HttpGet("manifest/{projectId}")]
    [ProducesResponseType(typeof(ProjectLicensingManifestDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetManifest(
        string projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting licensing manifest for project {ProjectId}, CorrelationId: {CorrelationId}",
                projectId, HttpContext.TraceIdentifier);

            var manifest = await _licensingService.GenerateManifestAsync(projectId, cancellationToken);
            var dto = MapToDto(manifest);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get licensing manifest for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to get licensing manifest", details = ex.Message });
        }
    }

    /// <summary>
    /// Export licensing manifest in specified format
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported manifest content</returns>
    [HttpPost("manifest/export")]
    [ProducesResponseType(typeof(LicensingExportResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExportManifest(
        [FromBody] ExportLicensingManifestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Exporting licensing manifest for project {ProjectId} in format {Format}, CorrelationId: {CorrelationId}",
                request.ProjectId, request.Format, HttpContext.TraceIdentifier);

            if (string.IsNullOrEmpty(request.ProjectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            var format = request.Format.ToLowerInvariant() switch
            {
                "json" => LicensingExportFormat.Json,
                "csv" => LicensingExportFormat.Csv,
                "html" => LicensingExportFormat.Html,
                "text" => LicensingExportFormat.Text,
                _ => LicensingExportFormat.Json
            };

            var manifest = await _licensingService.GenerateManifestAsync(request.ProjectId, cancellationToken);
            var content = await _licensingService.ExportManifestAsync(manifest, format, cancellationToken);

            var contentType = format switch
            {
                LicensingExportFormat.Json => "application/json",
                LicensingExportFormat.Csv => "text/csv",
                LicensingExportFormat.Html => "text/html",
                LicensingExportFormat.Text => "text/plain",
                _ => "application/octet-stream"
            };

            var extension = format switch
            {
                LicensingExportFormat.Json => "json",
                LicensingExportFormat.Csv => "csv",
                LicensingExportFormat.Html => "html",
                LicensingExportFormat.Text => "txt",
                _ => "txt"
            };

            var response = new LicensingExportResponse
            {
                Format = request.Format,
                Content = content,
                Filename = $"licensing-{request.ProjectId}.{extension}",
                ContentType = contentType
            };

            _logger.LogInformation("Exported licensing manifest for project {ProjectId} in format {Format}",
                request.ProjectId, request.Format);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export licensing manifest for project {ProjectId}", request.ProjectId);
            return StatusCode(500, new { error = "Failed to export licensing manifest", details = ex.Message });
        }
    }

    /// <summary>
    /// Download licensing manifest file
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="format">Export format (json, csv, html, text)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File download</returns>
    [HttpGet("manifest/{projectId}/download")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DownloadManifest(
        string projectId,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading licensing manifest for project {ProjectId} in format {Format}",
                projectId, format);

            var exportFormat = format.ToLowerInvariant() switch
            {
                "json" => LicensingExportFormat.Json,
                "csv" => LicensingExportFormat.Csv,
                "html" => LicensingExportFormat.Html,
                "text" => LicensingExportFormat.Text,
                _ => LicensingExportFormat.Json
            };

            var manifest = await _licensingService.GenerateManifestAsync(projectId, cancellationToken);
            var content = await _licensingService.ExportManifestAsync(manifest, exportFormat, cancellationToken);

            var contentType = exportFormat switch
            {
                LicensingExportFormat.Json => "application/json",
                LicensingExportFormat.Csv => "text/csv",
                LicensingExportFormat.Html => "text/html",
                LicensingExportFormat.Text => "text/plain",
                _ => "application/octet-stream"
            };

            var extension = exportFormat switch
            {
                LicensingExportFormat.Json => "json",
                LicensingExportFormat.Csv => "csv",
                LicensingExportFormat.Html => "html",
                LicensingExportFormat.Text => "txt",
                _ => "txt"
            };

            var filename = $"licensing-{projectId}.{extension}";

            return File(System.Text.Encoding.UTF8.GetBytes(content), contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download licensing manifest for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to download licensing manifest", details = ex.Message });
        }
    }

    /// <summary>
    /// Record licensing sign-off
    /// </summary>
    /// <param name="request">Sign-off request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sign-off confirmation</returns>
    [HttpPost("signoff")]
    [ProducesResponseType(typeof(LicensingSignOffResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RecordSignOff(
        [FromBody] LicensingSignOffRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Recording licensing sign-off for project {ProjectId}, CorrelationId: {CorrelationId}",
                request.ProjectId, HttpContext.TraceIdentifier);

            if (string.IsNullOrEmpty(request.ProjectId))
            {
                return BadRequest(new { error = "ProjectId is required" });
            }

            if (!request.AcknowledgedCommercialRestrictions ||
                !request.AcknowledgedAttributionRequirements ||
                !request.AcknowledgedWarnings)
            {
                return BadRequest(new { error = "All acknowledgments are required for sign-off" });
            }

            var signOff = new LicensingSignOff
            {
                ProjectId = request.ProjectId,
                AcknowledgedCommercialRestrictions = request.AcknowledgedCommercialRestrictions,
                AcknowledgedAttributionRequirements = request.AcknowledgedAttributionRequirements,
                AcknowledgedWarnings = request.AcknowledgedWarnings,
                SignedOffAt = DateTime.UtcNow,
                Notes = request.Notes
            };

            await _licensingService.RecordSignOffAsync(signOff, cancellationToken);

            var response = new LicensingSignOffResponse
            {
                ProjectId = request.ProjectId,
                SignedOffAt = signOff.SignedOffAt,
                Message = "Licensing sign-off recorded successfully"
            };

            _logger.LogInformation("Recorded licensing sign-off for project {ProjectId}", request.ProjectId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record licensing sign-off for project {ProjectId}", request.ProjectId);
            return StatusCode(500, new { error = "Failed to record licensing sign-off", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate licensing manifest
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpGet("manifest/{projectId}/validate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ValidateManifest(
        string projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validating licensing manifest for project {ProjectId}", projectId);

            var manifest = await _licensingService.GenerateManifestAsync(projectId, cancellationToken);
            var isValid = _licensingService.ValidateManifest(manifest);

            var result = new
            {
                projectId,
                isValid,
                hasWarnings = manifest.Warnings.Count > 0,
                hasMissingInfo = manifest.MissingLicensingInfo.Count > 0,
                commercialUseAllowed = manifest.AllCommercialUseAllowed,
                warnings = manifest.Warnings,
                missingInfo = manifest.MissingLicensingInfo
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate licensing manifest for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "Failed to validate licensing manifest", details = ex.Message });
        }
    }

    private ProjectLicensingManifestDto MapToDto(ProjectLicensingManifest manifest)
    {
        return new ProjectLicensingManifestDto
        {
            ProjectId = manifest.ProjectId,
            ProjectName = manifest.ProjectName,
            GeneratedAt = manifest.GeneratedAt,
            Assets = manifest.Assets.Select(a => new AssetLicensingInfoDto
            {
                AssetId = a.AssetId,
                AssetType = a.AssetType.ToString(),
                SceneIndex = a.SceneIndex,
                Name = a.Name,
                Source = a.Source,
                LicenseType = a.LicenseType,
                LicenseUrl = a.LicenseUrl,
                CommercialUseAllowed = a.CommercialUseAllowed,
                AttributionRequired = a.AttributionRequired,
                AttributionText = a.AttributionText,
                Creator = a.Creator,
                CreatorUrl = a.CreatorUrl,
                SourceUrl = a.SourceUrl,
                FilePath = a.FilePath,
                Metadata = a.Metadata
            }).ToList(),
            AllCommercialUseAllowed = manifest.AllCommercialUseAllowed,
            Warnings = manifest.Warnings,
            MissingLicensingInfo = manifest.MissingLicensingInfo,
            Summary = new LicensingSummaryDto
            {
                TotalAssets = manifest.Summary.TotalAssets,
                AssetsByType = manifest.Summary.AssetsByType.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value),
                AssetsBySource = manifest.Summary.AssetsBySource,
                AssetsByLicenseType = manifest.Summary.AssetsByLicenseType,
                AssetsRequiringAttribution = manifest.Summary.AssetsRequiringAttribution,
                AssetsWithCommercialRestrictions = manifest.Summary.AssetsWithCommercialRestrictions
            }
        };
    }
}
