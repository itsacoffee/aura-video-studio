using Aura.Api.HostedServices;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing project versions, snapshots, and restore points
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/[controller]")]
public class VersionsController : ControllerBase
{
    private readonly ProjectVersionService _versionService;
    private readonly ProjectAutosaveService _autosaveService;

    public VersionsController(
        ProjectVersionService versionService,
        ProjectAutosaveService autosaveService)
    {
        _versionService = versionService;
        _autosaveService = autosaveService;
    }

    /// <summary>
    /// Get all versions for a project
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVersions(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/{ProjectId}/versions", correlationId, projectId);

            var versions = await _versionService.GetVersionsAsync(projectId, ct);
            var totalStorage = await _versionService.GetProjectStorageSizeAsync(projectId, ct);

            var response = new VersionListResponse(
                Versions: versions.Select(v => new VersionResponse(
                    v.Id,
                    v.ProjectId,
                    v.VersionNumber,
                    v.Name,
                    v.Description,
                    v.VersionType,
                    v.Trigger,
                    v.CreatedAt,
                    v.CreatedByUserId,
                    v.StorageSizeBytes,
                    v.IsMarkedImportant
                )).ToList(),
                TotalCount: versions.Count,
                TotalStorageBytes: totalStorage
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve versions for project {ProjectId}", 
                correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve versions",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get a specific version by ID
    /// </summary>
    [HttpGet("{versionId:guid}")]
    public async Task<IActionResult> GetVersion(Guid projectId, Guid versionId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/{ProjectId}/versions/{VersionId}", 
                correlationId, projectId, versionId);

            var version = await _versionService.GetVersionDetailAsync(versionId, ct);
            if (version == null || version.ProjectId != projectId)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Version Not Found",
                    status = 404,
                    detail = $"Version {versionId} not found for project {projectId}",
                    correlationId
                });
            }

            var response = new VersionDetailResponse(
                version.Id,
                version.ProjectId,
                version.VersionNumber,
                version.Name,
                version.Description,
                version.VersionType,
                version.Trigger,
                version.CreatedAt,
                version.CreatedByUserId,
                version.BriefJson,
                version.PlanSpecJson,
                version.VoiceSpecJson,
                version.RenderSpecJson,
                version.TimelineJson,
                version.StorageSizeBytes,
                version.IsMarkedImportant
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to retrieve version {VersionId}", correlationId, versionId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to retrieve version",
                correlationId
            });
        }
    }

    /// <summary>
    /// Create a manual snapshot
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSnapshot(
        Guid projectId, 
        [FromBody] CreateSnapshotRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/projects/{ProjectId}/versions - Creating snapshot", 
                correlationId, projectId);

            if (request.ProjectId != projectId)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Project ID in URL does not match request body",
                    correlationId
                });
            }

            var versionId = await _versionService.CreateManualSnapshotAsync(
                projectId,
                request.Name,
                request.Description,
                null,
                ct);

            var version = await _versionService.GetVersionDetailAsync(versionId, ct);
            if (version == null)
            {
                throw new InvalidOperationException("Failed to retrieve created version");
            }

            var response = new VersionResponse(
                version.Id,
                version.ProjectId,
                version.VersionNumber,
                version.Name,
                version.Description,
                version.VersionType,
                version.Trigger,
                version.CreatedAt,
                version.CreatedByUserId,
                version.StorageSizeBytes,
                version.IsMarkedImportant
            );

            return CreatedAtAction(
                nameof(GetVersion),
                new { projectId, versionId },
                response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to create snapshot for project {ProjectId}", 
                correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to create snapshot",
                correlationId
            });
        }
    }

    /// <summary>
    /// Restore a project to a specific version
    /// </summary>
    [HttpPost("restore")]
    public async Task<IActionResult> RestoreVersion(
        Guid projectId,
        [FromBody] RestoreVersionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/projects/{ProjectId}/versions/restore - Version {VersionId}", 
                correlationId, projectId, request.VersionId);

            if (request.ProjectId != projectId)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Project ID in URL does not match request body",
                    correlationId
                });
            }

            await _versionService.RestoreVersionAsync(projectId, request.VersionId, ct);

            return Ok(new
            {
                success = true,
                message = "Version restored successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Warning(ex, "[{CorrelationId}] Failed to restore version {VersionId} for project {ProjectId}", 
                correlationId, request.VersionId, projectId);
            return NotFound(new
            {
                type = "https://docs.aura.studio/errors/E404",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to restore version for project {ProjectId}", 
                correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to restore version",
                correlationId
            });
        }
    }

    /// <summary>
    /// Update version metadata
    /// </summary>
    [HttpPatch("{versionId:guid}")]
    public async Task<IActionResult> UpdateVersion(
        Guid projectId,
        Guid versionId,
        [FromBody] UpdateVersionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] PATCH /api/projects/{ProjectId}/versions/{VersionId}", 
                correlationId, projectId, versionId);

            var version = await _versionService.GetVersionDetailAsync(versionId, ct);
            if (version == null || version.ProjectId != projectId)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Version Not Found",
                    status = 404,
                    detail = $"Version {versionId} not found for project {projectId}",
                    correlationId
                });
            }

            await _versionService.UpdateVersionMetadataAsync(
                versionId,
                request.Name,
                request.Description,
                request.IsMarkedImportant,
                ct);

            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to update version {VersionId}", correlationId, versionId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to update version",
                correlationId
            });
        }
    }

    /// <summary>
    /// Delete a version
    /// </summary>
    [HttpDelete("{versionId:guid}")]
    public async Task<IActionResult> DeleteVersion(
        Guid projectId,
        Guid versionId,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] DELETE /api/projects/{ProjectId}/versions/{VersionId}", 
                correlationId, projectId, versionId);

            var version = await _versionService.GetVersionDetailAsync(versionId, ct);
            if (version == null || version.ProjectId != projectId)
            {
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Version Not Found",
                    status = 404,
                    detail = $"Version {versionId} not found for project {projectId}",
                    correlationId
                });
            }

            await _versionService.DeleteVersionAsync(versionId, ct);

            return NoContent();
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to delete version {VersionId}", correlationId, versionId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to delete version",
                correlationId
            });
        }
    }

    /// <summary>
    /// Compare two versions
    /// </summary>
    [HttpGet("compare")]
    public async Task<IActionResult> CompareVersions(
        Guid projectId,
        [FromQuery] Guid version1Id,
        [FromQuery] Guid version2Id,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/{ProjectId}/versions/compare?version1={V1}&version2={V2}", 
                correlationId, projectId, version1Id, version2Id);

            var comparison = await _versionService.CompareVersionsAsync(version1Id, version2Id, ct);

            var response = new VersionComparisonResponse(
                comparison.Version1Id,
                comparison.Version2Id,
                comparison.Version1Number,
                comparison.Version2Number,
                comparison.BriefChanged,
                comparison.PlanChanged,
                comparison.VoiceChanged,
                comparison.RenderChanged,
                comparison.TimelineChanged,
                new VersionDataDto(
                    comparison.Version1Data.BriefJson,
                    comparison.Version1Data.PlanSpecJson,
                    comparison.Version1Data.VoiceSpecJson,
                    comparison.Version1Data.RenderSpecJson,
                    comparison.Version1Data.TimelineJson
                ),
                new VersionDataDto(
                    comparison.Version2Data.BriefJson,
                    comparison.Version2Data.PlanSpecJson,
                    comparison.Version2Data.VoiceSpecJson,
                    comparison.Version2Data.RenderSpecJson,
                    comparison.Version2Data.TimelineJson
                )
            );

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Warning(ex, "[{CorrelationId}] Failed to compare versions", correlationId);
            return NotFound(new
            {
                type = "https://docs.aura.studio/errors/E404",
                title = "Not Found",
                status = 404,
                detail = ex.Message,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to compare versions", correlationId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to compare versions",
                correlationId
            });
        }
    }

    /// <summary>
    /// Get storage usage information
    /// </summary>
    [HttpGet("storage")]
    public async Task<IActionResult> GetStorageUsage(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/projects/{ProjectId}/versions/storage", 
                correlationId, projectId);

            var versions = await _versionService.GetVersionsAsync(projectId, ct);
            var totalStorage = await _versionService.GetProjectStorageSizeAsync(projectId, ct);

            var autosaveCount = versions.Count(v => v.VersionType == "Autosave");
            var manualCount = versions.Count(v => v.VersionType == "Manual");
            var restorePointCount = versions.Count(v => v.VersionType == "RestorePoint");

            var formattedSize = FormatBytes(totalStorage);

            var response = new StorageUsageResponse(
                totalStorage,
                versions.Count,
                autosaveCount,
                manualCount,
                restorePointCount,
                formattedSize
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to get storage usage for project {ProjectId}", 
                correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to get storage usage",
                correlationId
            });
        }
    }

    /// <summary>
    /// Trigger manual autosave
    /// </summary>
    [HttpPost("autosave")]
    public async Task<IActionResult> TriggerAutosave(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/projects/{ProjectId}/versions/autosave", 
                correlationId, projectId);

            var success = await _autosaveService.TriggerAutosaveAsync(projectId, ct);

            if (!success)
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Autosave Failed",
                    status = 400,
                    detail = "Project is not registered for autosave",
                    correlationId
                });
            }

            return Ok(new
            {
                success = true,
                message = "Autosave triggered successfully"
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Failed to trigger autosave for project {ProjectId}", 
                correlationId, projectId);
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Server Error",
                status = 500,
                detail = "Failed to trigger autosave",
                correlationId
            });
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
