using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for file system operations
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;

    public FilesController(ILogger<FilesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get file statistics (size, modified date, etc.)
    /// </summary>
    [HttpGet("stat")]
    public IActionResult GetFileStat([FromQuery] string path)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;

            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogWarning("[{CorrelationId}] File stat request rejected: Path is required", correlationId);
                return BadRequest(new
                {
                    error = "Path is required",
                    correlationId
                });
            }

            if (!System.IO.File.Exists(path))
            {
                _logger.LogWarning("[{CorrelationId}] File not found: {Path}", correlationId, path);
                return NotFound(new
                {
                    error = $"File not found: {path}",
                    path,
                    correlationId
                });
            }

            var fileInfo = new FileInfo(path);

            _logger.LogDebug("[{CorrelationId}] File stat retrieved: {Path}, Size: {Size} bytes", 
                correlationId, path, fileInfo.Length);

            return Ok(new
            {
                path = fileInfo.FullName,
                size = fileInfo.Length,
                created = fileInfo.CreationTimeUtc,
                modified = fileInfo.LastWriteTimeUtc,
                extension = fileInfo.Extension,
                exists = true,
                correlationId
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogWarning(ex, "[{CorrelationId}] Access denied to file: {Path}", correlationId, path);
            return StatusCode(403, new
            {
                error = "Access denied",
                path,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogError(ex, "[{CorrelationId}] Failed to get file stats for: {Path}", correlationId, path);
            return StatusCode(500, new
            {
                error = "Failed to get file information",
                details = ex.Message,
                correlationId
            });
        }
    }
}

