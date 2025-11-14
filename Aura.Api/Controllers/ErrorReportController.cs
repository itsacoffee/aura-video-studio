using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Models.Diagnostics;
using Aura.Core.Services.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for receiving and storing frontend error reports
/// </summary>
[ApiController]
[Route("api/error-report")]
public class ErrorReportController : ControllerBase
{
    private readonly ILogger<ErrorReportController> _logger;
    private readonly CrashReportService _crashReportService;
    private readonly string _errorReportsPath;

    public ErrorReportController(
        ILogger<ErrorReportController> logger,
        CrashReportService crashReportService)
    {
        _logger = logger;
        _crashReportService = crashReportService;
        
        // Store error reports in a dedicated directory
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorReports");
        Directory.CreateDirectory(baseDir);
        _errorReportsPath = baseDir;
    }

    /// <summary>
    /// Receive and store an error report from the frontend
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitErrorReport([FromBody] ClientErrorReport report)
    {
        try
        {
            // Validate the error report
            if (report == null || string.IsNullOrEmpty(report.Message))
            {
                return BadRequest(new { error = "Invalid error report: missing required fields" });
            }

            // Save using crash report service
            var reportId = await _crashReportService.SaveClientErrorReportAsync(report, HttpContext.RequestAborted).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                reportId,
                message = "Error report received and stored successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process error report");
            return StatusCode(500, new { error = "Failed to process error report" });
        }
    }

    /// <summary>
    /// Receive and store a legacy error report from the frontend (backward compatibility)
    /// </summary>
    [HttpPost("legacy")]
    public async Task<IActionResult> SubmitLegacyErrorReport([FromBody] ErrorReportDto report)
    {
        try
        {
            // Validate the error report
            if (report?.Error == null)
            {
                return BadRequest(new { error = "Invalid error report: missing error details" });
            }

            // Sanitize error data to prevent injection attacks
            var sanitizedReport = SanitizeErrorReport(report);

            // Log the error for immediate visibility
            _logger.LogError(
                "Frontend Error Report (Legacy): {ErrorName} - {ErrorMessage}\nURL: {Url}\nUser Description: {Description}",
                sanitizedReport.Error?.Name,
                sanitizedReport.Error?.Message,
                sanitizedReport.Url,
                sanitizedReport.UserDescription ?? "None provided"
            );

            // Store detailed report to file
            var reportId = Guid.NewGuid().ToString();
            var fileName = $"error-report-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}-{reportId}.json";
            var filePath = Path.Combine(_errorReportsPath, fileName);

            var reportData = new
            {
                reportId,
                timestamp = DateTime.UtcNow,
                report = sanitizedReport
            };

            var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await System.IO.File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

            _logger.LogInformation(
                "Error report saved to {FilePath} with ID {ReportId}",
                filePath,
                reportId
            );

            return Ok(new
            {
                success = true,
                reportId,
                message = "Error report received and stored successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process error report");
            return StatusCode(500, new { error = "Failed to process error report" });
        }
    }

    /// <summary>
    /// Get list of error reports (for debugging)
    /// </summary>
    [HttpGet]
    public IActionResult GetErrorReports([FromQuery] int limit = 50)
    {
        try
        {
            var files = Directory.GetFiles(_errorReportsPath, "error-report-*.json");
            var reports = new List<object>();

            // Sort by creation time descending and take the most recent
            var sortedFiles = files
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Take(limit);

            foreach (var file in sortedFiles)
            {
                reports.Add(new
                {
                    filename = file.Name,
                    createdAt = file.CreationTimeUtc,
                    size = file.Length
                });
            }

            return Ok(new
            {
                count = reports.Count,
                reports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve error reports");
            return StatusCode(500, new { error = "Failed to retrieve error reports" });
        }
    }

    /// <summary>
    /// Get a specific error report by ID
    /// </summary>
    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetErrorReport(string reportId)
    {
        try
        {
            var files = Directory.GetFiles(_errorReportsPath, $"*{reportId}.json");
            
            if (files.Length == 0)
            {
                return NotFound(new { error = "Error report not found" });
            }

            var json = await System.IO.File.ReadAllTextAsync(files[0]).ConfigureAwait(false);
            var report = JsonSerializer.Deserialize<object>(json);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve error report {ReportId}", reportId);
            return StatusCode(500, new { error = "Failed to retrieve error report" });
        }
    }

    /// <summary>
    /// Delete old error reports (cleanup endpoint)
    /// </summary>
    [HttpDelete("cleanup")]
    public IActionResult CleanupOldReports([FromQuery] int daysOld = 30)
    {
        try
        {
            var files = Directory.GetFiles(_errorReportsPath, "error-report-*.json");
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var deletedCount = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    System.IO.File.Delete(file);
                    deletedCount++;
                }
            }

            _logger.LogInformation("Cleaned up {Count} error reports older than {Days} days", deletedCount, daysOld);

            return Ok(new
            {
                deletedCount,
                message = $"Deleted {deletedCount} error reports older than {daysOld} days"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old error reports");
            return StatusCode(500, new { error = "Failed to cleanup old error reports" });
        }
    }

    private ErrorReportDto SanitizeErrorReport(ErrorReportDto report)
    {
        // Create a sanitized copy to prevent any potential injection attacks
        return new ErrorReportDto
        {
            Timestamp = report.Timestamp,
            Error = report.Error != null ? new ErrorDetailsDto
            {
                Name = TruncateString(report.Error.Name, 200),
                Message = TruncateString(report.Error.Message, 1000),
                Stack = TruncateString(report.Error.Stack, 5000)
            } : null,
            ComponentStack = TruncateString(report.ComponentStack, 5000),
            Context = report.Context,
            UserAgent = TruncateString(report.UserAgent, 500),
            Url = TruncateString(report.Url, 500),
            UserDescription = TruncateString(report.UserDescription, 2000),
            Logs = report.Logs
        };
    }

    private string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// Error report data transfer object
/// </summary>
public class ErrorReportDto
{
    public string? Timestamp { get; set; }
    public ErrorDetailsDto? Error { get; set; }
    public string? ComponentStack { get; set; }
    public Dictionary<string, object>? Context { get; set; }
    public string? UserAgent { get; set; }
    public string? Url { get; set; }
    public string? UserDescription { get; set; }
    public List<object>? Logs { get; set; }
}

/// <summary>
/// Error details data transfer object
/// </summary>
public class ErrorDetailsDto
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public string? Stack { get; set; }
}
