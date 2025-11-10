using Aura.Core.Services.Diagnostics;
using Aura.Core.Services.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for error diagnostics, recovery, and troubleshooting
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public class ErrorDiagnosticsController : ControllerBase
{
    private readonly ILogger<ErrorDiagnosticsController> _logger;
    private readonly ErrorLoggingService _errorLoggingService;
    private readonly ErrorRecoveryService _errorRecoveryService;
    private readonly ErrorAggregationService _errorAggregationService;

    public ErrorDiagnosticsController(
        ILogger<ErrorDiagnosticsController> logger,
        ErrorLoggingService errorLoggingService,
        ErrorRecoveryService errorRecoveryService,
        ErrorAggregationService errorAggregationService)
    {
        _logger = logger;
        _errorLoggingService = errorLoggingService;
        _errorRecoveryService = errorRecoveryService;
        _errorAggregationService = errorAggregationService;
    }

    /// <summary>
    /// Get recent errors from the error log
    /// </summary>
    [HttpGet("errors")]
    public async Task<IActionResult> GetRecentErrors(
        [FromQuery] int count = 50,
        [FromQuery] ErrorCategory? category = null)
    {
        try
        {
            var errors = await _errorLoggingService.GetRecentErrorsAsync(count, category);
            return Ok(new
            {
                count = errors.Count,
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent errors");
            return StatusCode(500, new { error = "Failed to retrieve errors" });
        }
    }

    /// <summary>
    /// Search errors by correlation ID
    /// </summary>
    [HttpGet("errors/by-correlation/{correlationId}")]
    public async Task<IActionResult> SearchByCorrelationId(string correlationId)
    {
        try
        {
            var errors = await _errorLoggingService.SearchByCorrelationIdAsync(correlationId);
            return Ok(new
            {
                correlationId,
                count = errors.Count,
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search errors by correlation ID");
            return StatusCode(500, new { error = "Failed to search errors" });
        }
    }

    /// <summary>
    /// Get aggregated error statistics
    /// </summary>
    [HttpGet("errors/stats")]
    public IActionResult GetErrorStatistics([FromQuery] int? hoursAgo = null)
    {
        try
        {
            var timeWindow = hoursAgo.HasValue ? TimeSpan.FromHours(hoursAgo.Value) : (TimeSpan?)null;
            var stats = _errorAggregationService.GetStatistics(timeWindow);
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error statistics");
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    /// <summary>
    /// Get aggregated errors grouped by signature
    /// </summary>
    [HttpGet("errors/aggregated")]
    public IActionResult GetAggregatedErrors(
        [FromQuery] int? hoursAgo = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var timeWindow = hoursAgo.HasValue ? TimeSpan.FromHours(hoursAgo.Value) : (TimeSpan?)null;
            var errors = _errorAggregationService.GetAggregatedErrors(timeWindow, limit);
            
            return Ok(new
            {
                count = errors.Count,
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get aggregated errors");
            return StatusCode(500, new { error = "Failed to get aggregated errors" });
        }
    }

    /// <summary>
    /// Export comprehensive diagnostic information
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportDiagnostics([FromQuery] int? hoursAgo = null)
    {
        try
        {
            var timeWindow = hoursAgo.HasValue ? TimeSpan.FromHours(hoursAgo.Value) : (TimeSpan?)null;
            var filePath = await _errorLoggingService.ExportDiagnosticsAsync(timeWindow);
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            
            return File(fileBytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export diagnostics");
            return StatusCode(500, new { error = "Failed to export diagnostics" });
        }
    }

    /// <summary>
    /// Get recovery guidance for a specific error
    /// </summary>
    [HttpPost("recovery-guide")]
    public IActionResult GetRecoveryGuide([FromBody] ErrorInfoRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ExceptionType) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "ExceptionType and Message are required" });
            }

            // Create a synthetic exception to generate guide
            var exception = CreateExceptionFromRequest(request);
            var guide = _errorRecoveryService.GenerateRecoveryGuide(exception, request.CorrelationId);
            
            return Ok(guide);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recovery guide");
            return StatusCode(500, new { error = "Failed to generate recovery guide" });
        }
    }

    /// <summary>
    /// Attempt automated recovery for an error
    /// </summary>
    [HttpPost("recovery-attempt")]
    public async Task<IActionResult> AttemptRecovery([FromBody] ErrorInfoRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ExceptionType) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { error = "ExceptionType and Message are required" });
            }

            var exception = CreateExceptionFromRequest(request);
            var result = await _errorRecoveryService.AttemptAutomatedRecoveryAsync(exception, request.CorrelationId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attempt recovery");
            return StatusCode(500, new { error = "Failed to attempt recovery" });
        }
    }

    /// <summary>
    /// Clear old error logs
    /// </summary>
    [HttpDelete("errors/cleanup")]
    public async Task<IActionResult> CleanupOldErrors([FromQuery] int daysOld = 30)
    {
        try
        {
            var retentionPeriod = TimeSpan.FromDays(daysOld);
            var deletedCount = await _errorLoggingService.CleanupOldLogsAsync(retentionPeriod);
            
            var aggregationDeleted = _errorAggregationService.ClearOldErrors(retentionPeriod);
            
            return Ok(new
            {
                logFilesDeleted = deletedCount,
                aggregatedErrorsCleared = aggregationDeleted,
                message = $"Cleaned up errors older than {daysOld} days"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old errors");
            return StatusCode(500, new { error = "Failed to cleanup old errors" });
        }
    }

    /// <summary>
    /// Health check for error handling system
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            services = new
            {
                errorLogging = "operational",
                errorRecovery = "operational",
                errorAggregation = "operational"
            }
        });
    }

    private Exception CreateExceptionFromRequest(ErrorInfoRequest request)
    {
        // Try to create the actual exception type if possible
        var exceptionType = Type.GetType(request.ExceptionType);
        
        if (exceptionType != null && typeof(Exception).IsAssignableFrom(exceptionType))
        {
            try
            {
                return (Exception)Activator.CreateInstance(exceptionType, request.Message)!;
            }
            catch
            {
                // Fall through to generic exception
            }
        }

        // Fallback to generic exception
        return new Exception(request.Message);
    }
}

/// <summary>
/// Request model for error information
/// </summary>
public class ErrorInfoRequest
{
    public required string ExceptionType { get; set; }
    public required string Message { get; set; }
    public string? CorrelationId { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}
