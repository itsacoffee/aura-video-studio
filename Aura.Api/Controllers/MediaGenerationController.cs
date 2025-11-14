using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Media;
using Aura.Core.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for integrating media library with video generation pipeline
/// </summary>
[ApiController]
[Route("api/media-generation")]
public class MediaGenerationController : ControllerBase
{
    private readonly IMediaGenerationIntegrationService _integrationService;
    private readonly ILogger<MediaGenerationController> _logger;

    public MediaGenerationController(
        IMediaGenerationIntegrationService integrationService,
        ILogger<MediaGenerationController> logger)
    {
        _integrationService = integrationService ?? throw new ArgumentNullException(nameof(integrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get media items for a specific project
    /// </summary>
    [HttpGet("projects/{projectId}/media")]
    public async Task<IActionResult> GetProjectMedia(
        string projectId,
        CancellationToken ct)
    {
        try
        {
            var media = await _integrationService.GetProjectMediaAsync(projectId, ct).ConfigureAwait(false);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media for project {ProjectId}, CorrelationId: {CorrelationId}",
                projectId, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Project Media",
                Status = 500,
                Detail = "An error occurred while retrieving project media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Save generated media to the library
    /// </summary>
    [HttpPost("save-generated")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5GB limit
    public async Task<IActionResult> SaveGeneratedMedia(
        [FromForm] IFormFile file,
        [FromForm] string type,
        [FromForm] string? projectId,
        [FromForm] string? description,
        [FromForm] string? tags,
        CancellationToken ct)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File",
                    Status = 400,
                    Detail = "No file was uploaded",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            if (!Enum.TryParse<MediaType>(type, true, out var mediaType))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Media Type",
                    Status = 400,
                    Detail = $"Invalid media type: {type}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            // Save file to temporary location
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            
            try
            {
                using (var stream = file.OpenReadStream())
                using (var fileStream = System.IO.File.Create(tempPath))
                {
                    await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                }

                var tagList = string.IsNullOrWhiteSpace(tags)
                    ? new List<string>()
                    : new List<string>(tags.Split(',', StringSplitOptions.RemoveEmptyEntries));

                var result = await _integrationService.SaveGeneratedMediaAsync(
                    tempPath,
                    mediaType,
                    projectId,
                    description,
                    tagList,
                    ct).ConfigureAwait(false);

                return Ok(result);
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving generated media, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Save Failed",
                Status = 500,
                Detail = "An error occurred while saving generated media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Link existing media to a project
    /// </summary>
    [HttpPost("link-media")]
    public async Task<IActionResult> LinkMediaToProject(
        [FromBody] LinkMediaRequest request,
        CancellationToken ct)
    {
        try
        {
            await _integrationService.LinkMediaToProjectAsync(
                request.MediaId,
                request.ProjectId,
                request.ProjectName,
                ct).ConfigureAwait(false);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking media to project, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Link Failed",
                Status = 500,
                Detail = "An error occurred while linking media to project",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get all media used in a project
    /// </summary>
    [HttpGet("projects/{projectId}/usage")]
    public async Task<IActionResult> GetMediaUsedInProject(
        string projectId,
        CancellationToken ct)
    {
        try
        {
            var media = await _integrationService.GetMediaUsedInProjectAsync(projectId, ct).ConfigureAwait(false);
            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media usage for project {ProjectId}, CorrelationId: {CorrelationId}",
                projectId, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Media Usage",
                Status = 500,
                Detail = "An error occurred while retrieving media usage",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Create a collection for a project's media
    /// </summary>
    [HttpPost("projects/{projectId}/collection")]
    public async Task<IActionResult> CreateProjectCollection(
        string projectId,
        [FromBody] CreateProjectCollectionRequest request,
        CancellationToken ct)
    {
        try
        {
            var collection = await _integrationService.CreateProjectCollectionAsync(
                projectId,
                request.ProjectName,
                ct).ConfigureAwait(false);

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project collection for {ProjectId}, CorrelationId: {CorrelationId}",
                projectId, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Collection Creation Failed",
                Status = 500,
                Detail = "An error occurred while creating project collection",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get downloadable URLs for media items
    /// </summary>
    [HttpPost("download-urls")]
    public async Task<IActionResult> GetDownloadUrls(
        [FromBody] GetDownloadUrlsRequest request,
        CancellationToken ct)
    {
        try
        {
            var urls = await _integrationService.GetDownloadUrlsAsync(request.MediaIds, ct).ConfigureAwait(false);
            return Ok(urls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URLs, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Download URLs",
                Status = 500,
                Detail = "An error occurred while retrieving download URLs",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}

/// <summary>
/// Request to link media to a project
/// </summary>
public record LinkMediaRequest(Guid MediaId, string ProjectId, string? ProjectName);

/// <summary>
/// Request to create a project collection
/// </summary>
public record CreateProjectCollectionRequest(string ProjectName);

/// <summary>
/// Request to get download URLs
/// </summary>
public record GetDownloadUrlsRequest(List<Guid> MediaIds);
