using Aura.Core.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for accessing run telemetry data
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ILogger<TelemetryController> _logger;
    private readonly RunTelemetryCollector _telemetryCollector;
    
    public TelemetryController(
        ILogger<TelemetryController> logger,
        RunTelemetryCollector telemetryCollector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryCollector = telemetryCollector ?? throw new ArgumentNullException(nameof(telemetryCollector));
    }
    
    /// <summary>
    /// Get telemetry data for a specific job
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <returns>RunTelemetryCollection with all telemetry records and summary</returns>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(RunTelemetryCollection), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetJobTelemetry(string jobId)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            _logger.LogInformation(
                "Getting telemetry for job {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest(new
                {
                    type = "https://docs.aura.studio/errors/E400",
                    title = "Invalid Request",
                    status = 400,
                    detail = "Job ID is required",
                    correlationId
                });
            }
            
            var telemetry = _telemetryCollector.LoadTelemetry(jobId);
            
            if (telemetry == null)
            {
                _logger.LogWarning(
                    "Telemetry not found for job {JobId}, CorrelationId: {CorrelationId}",
                    jobId, correlationId);
                
                return NotFound(new
                {
                    type = "https://docs.aura.studio/errors/E404",
                    title = "Telemetry Not Found",
                    status = 404,
                    detail = $"No telemetry data found for job {jobId}",
                    correlationId,
                    guidance = "Telemetry may not be available for jobs that failed early or were created before telemetry was enabled"
                });
            }
            
            _logger.LogInformation(
                "Returning telemetry for job {JobId} with {RecordCount} records, CorrelationId: {CorrelationId}",
                jobId, telemetry.Records.Count, correlationId);
            
            return Ok(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error retrieving telemetry for job {JobId}, CorrelationId: {CorrelationId}",
                jobId, correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E500",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while retrieving telemetry data",
                correlationId
            });
        }
    }
    
    /// <summary>
    /// Get current telemetry schema version and documentation
    /// </summary>
    [HttpGet("schema")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetSchema()
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogDebug("Schema endpoint called, CorrelationId: {CorrelationId}", correlationId);
        
        return Ok(new
        {
            version = "1.0",
            schemaUrl = "https://aura.studio/schemas/run-telemetry/v1",
            description = "Unified telemetry schema for video generation run stages",
            stages = new[]
            {
                "brief", "plan", "script", "ssml", "tts", "visuals", "render", "post"
            },
            resultStatuses = new[] { "ok", "warn", "error" },
            selectionSources = new[] { "default", "pinned", "cli", "fallback" },
            documentation = "https://docs.aura.studio/telemetry/run-telemetry-v1"
        });
    }
}
