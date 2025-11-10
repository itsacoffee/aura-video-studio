using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Media;
using Aura.Core.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for media library operations
/// </summary>
[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IMediaService mediaService,
        ILogger<MediaController> logger)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get media item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedia(Guid id, CancellationToken ct)
    {
        try
        {
            var media = await _mediaService.GetMediaByIdAsync(id, ct);
            
            if (media == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Media Not Found",
                    Status = 404,
                    Detail = $"Media with ID {id} was not found",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            return Ok(media);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Media",
                Status = 500,
                Detail = "An error occurred while retrieving the media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Search and filter media items
    /// </summary>
    [HttpPost("search")]
    public async Task<IActionResult> SearchMedia(
        [FromBody] MediaSearchRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await _mediaService.SearchMediaAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching media, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Search Failed",
                Status = 500,
                Detail = "An error occurred while searching media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Upload a media file
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(5L * 1024 * 1024 * 1024)] // 5GB limit
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string? description,
        [FromForm] string? tags,
        [FromForm] Guid? collectionId,
        [FromForm] string type,
        [FromForm] bool generateThumbnail = true,
        [FromForm] bool extractMetadata = true,
        CancellationToken ct = default)
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

            var tagList = string.IsNullOrWhiteSpace(tags)
                ? new List<string>()
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

            var uploadRequest = new MediaUploadRequest
            {
                FileName = file.FileName,
                Type = mediaType,
                Source = MediaSource.UserUpload,
                Description = description,
                Tags = tagList,
                CollectionId = collectionId,
                GenerateThumbnail = generateThumbnail,
                ExtractMetadata = extractMetadata
            };

            using var stream = file.OpenReadStream();
            var result = await _mediaService.UploadMediaAsync(stream, uploadRequest, ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Upload Failed",
                Status = 500,
                Detail = "An error occurred while uploading the media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Update media metadata
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedia(
        Guid id,
        [FromBody] MediaUploadRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediaService.UpdateMediaAsync(id, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Media {Id} not found, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return NotFound(new ProblemDetails
            {
                Title = "Media Not Found",
                Status = 404,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating media {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Update Failed",
                Status = 500,
                Detail = "An error occurred while updating the media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Delete media item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedia(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediaService.DeleteMediaAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Delete Failed",
                Status = 500,
                Detail = "An error occurred while deleting the media",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Perform bulk operations on media items
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkOperation(
        [FromBody] BulkMediaOperationRequest request,
        CancellationToken ct)
    {
        try
        {
            var results = await _mediaService.BulkOperationAsync(request, ct);
            return Ok(new { success = true, results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Bulk Operation Failed",
                Status = 500,
                Detail = "An error occurred while performing the bulk operation",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get all collections
    /// </summary>
    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections(CancellationToken ct)
    {
        try
        {
            var collections = await _mediaService.GetAllCollectionsAsync(ct);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Collections",
                Status = 500,
                Detail = "An error occurred while retrieving collections",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get collection by ID
    /// </summary>
    [HttpGet("collections/{id}")]
    public async Task<IActionResult> GetCollection(Guid id, CancellationToken ct)
    {
        try
        {
            var collection = await _mediaService.GetCollectionByIdAsync(id, ct);
            
            if (collection == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Collection Not Found",
                    Status = 404,
                    Detail = $"Collection with ID {id} was not found",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Collection",
                Status = 500,
                Detail = "An error occurred while retrieving the collection",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection(
        [FromBody] MediaCollectionRequest request,
        CancellationToken ct)
    {
        try
        {
            var collection = await _mediaService.CreateCollectionAsync(request, ct);
            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Create Failed",
                Status = 500,
                Detail = "An error occurred while creating the collection",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Update collection
    /// </summary>
    [HttpPut("collections/{id}")]
    public async Task<IActionResult> UpdateCollection(
        Guid id,
        [FromBody] MediaCollectionRequest request,
        CancellationToken ct)
    {
        try
        {
            var collection = await _mediaService.UpdateCollectionAsync(id, request, ct);
            return Ok(collection);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Collection Not Found",
                Status = 404,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Update Failed",
                Status = 500,
                Detail = "An error occurred while updating the collection",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Delete collection
    /// </summary>
    [HttpDelete("collections/{id}")]
    public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediaService.DeleteCollectionAsync(id, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Delete Failed",
                Status = 500,
                Detail = "An error occurred while deleting the collection",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get all tags
    /// </summary>
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(CancellationToken ct)
    {
        try
        {
            var tags = await _mediaService.GetAllTagsAsync(ct);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Tags",
                Status = 500,
                Detail = "An error occurred while retrieving tags",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get storage statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        try
        {
            var stats = await _mediaService.GetStorageStatsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage stats, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Statistics",
                Status = 500,
                Detail = "An error occurred while retrieving storage statistics",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Track media usage
    /// </summary>
    [HttpPost("{id}/track-usage")]
    public async Task<IActionResult> TrackUsage(
        Guid id,
        [FromBody] TrackUsageRequest request,
        CancellationToken ct)
    {
        try
        {
            await _mediaService.TrackMediaUsageAsync(id, request.ProjectId, request.ProjectName, ct);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking usage for media {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Tracking Usage",
                Status = 500,
                Detail = "An error occurred while tracking media usage",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Get media usage information
    /// </summary>
    [HttpGet("{id}/usage")]
    public async Task<IActionResult> GetUsage(Guid id, CancellationToken ct)
    {
        try
        {
            var usage = await _mediaService.GetMediaUsageAsync(id, ct);
            return Ok(usage);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Media Not Found",
                Status = 404,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage for media {Id}, CorrelationId: {CorrelationId}",
                id, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Usage",
                Status = 500,
                Detail = "An error occurred while retrieving media usage",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Check for duplicate media
    /// </summary>
    [HttpPost("check-duplicate")]
    public async Task<IActionResult> CheckDuplicate(
        [FromForm] IFormFile file,
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

            using var stream = file.OpenReadStream();
            var result = await _mediaService.CheckForDuplicateAsync(stream, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Check Failed",
                Status = 500,
                Detail = "An error occurred while checking for duplicates",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Initiate chunked upload session
    /// </summary>
    [HttpPost("upload/initiate")]
    public async Task<IActionResult> InitiateChunkedUpload(
        [FromBody] InitiateUploadRequest request,
        CancellationToken ct)
    {
        try
        {
            var session = await _mediaService.InitiateChunkedUploadAsync(
                request.FileName,
                request.TotalSize,
                request.TotalChunks,
                ct);

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating chunked upload, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Initiation Failed",
                Status = 500,
                Detail = "An error occurred while initiating chunked upload",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Upload a chunk
    /// </summary>
    [HttpPost("upload/{sessionId}/chunk/{chunkIndex}")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB chunk limit
    public async Task<IActionResult> UploadChunk(
        Guid sessionId,
        int chunkIndex,
        [FromForm] IFormFile chunk,
        CancellationToken ct)
    {
        try
        {
            if (chunk == null || chunk.Length == 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Chunk",
                    Status = 400,
                    Detail = "No chunk data was uploaded",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            using var stream = chunk.OpenReadStream();
            await _mediaService.UploadChunkAsync(sessionId, chunkIndex, stream, ct);

            return Ok(new { success = true, chunkIndex });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Session",
                Status = 400,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading chunk {ChunkIndex} for session {SessionId}, CorrelationId: {CorrelationId}",
                chunkIndex, sessionId, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Upload Failed",
                Status = 500,
                Detail = "An error occurred while uploading the chunk",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Complete chunked upload
    /// </summary>
    [HttpPost("upload/{sessionId}/complete")]
    public async Task<IActionResult> CompleteChunkedUpload(
        Guid sessionId,
        [FromBody] MediaUploadRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediaService.CompleteChunkedUploadAsync(sessionId, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Session",
                Status = 400,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing chunked upload for session {SessionId}, CorrelationId: {CorrelationId}",
                sessionId, HttpContext.TraceIdentifier);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Completion Failed",
                Status = 500,
                Detail = "An error occurred while completing the upload",
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}

/// <summary>
/// Request to track media usage
/// </summary>
public record TrackUsageRequest(string ProjectId, string? ProjectName);

/// <summary>
/// Request to initiate chunked upload
/// </summary>
public record InitiateUploadRequest(string FileName, long TotalSize, int TotalChunks);
