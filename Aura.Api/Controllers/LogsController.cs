using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for retrieving and managing application logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;
    private readonly string _logsDirectory;
    private const int MaxLinesDefault = 500;
    private const int MaxLinesLimit = 5000;

    public LogsController(ILogger<LogsController> logger)
    {
        _logger = logger;
        _logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    }

    /// <summary>
    /// Get application logs with optional filtering
    /// </summary>
    /// <param name="level">Filter by log level (INF, WRN, ERR, FTL)</param>
    /// <param name="correlationId">Filter by correlation ID</param>
    /// <param name="lines">Number of lines to return (default: 500, max: 5000)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Parsed log entries</returns>
    [HttpGet]
    public async Task<ActionResult<LogsResponse>> GetLogs(
        [FromQuery] string? level = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] int? lines = null,
        CancellationToken ct = default)
    {
        try
        {
            var requestedLines = Math.Min(lines ?? MaxLinesDefault, MaxLinesLimit);
            
            _logger.LogInformation(
                "Logs requested - Level: {Level}, CorrelationId: {CorrelationId}, Lines: {Lines}",
                level ?? "all",
                correlationId ?? "all",
                requestedLines
            );

            // Get the most recent log file
            var logFile = GetMostRecentLogFile();
            if (logFile == null)
            {
                return Ok(new LogsResponse
                {
                    Logs = new List<LogEntry>(),
                    File = "No log file found",
                    TotalLines = 0,
                    Message = "No log files available"
                });
            }

            // Read log file
            var allLines = await ReadLogFileLinesAsync(logFile, requestedLines * 2, ct);
            
            // Parse log entries
            var parsedLogs = ParseLogEntries(allLines);

            // Apply filters
            var filteredLogs = ApplyFilters(parsedLogs, level, correlationId);

            // Take requested number of lines
            var resultLogs = filteredLogs.TakeLast(requestedLines).ToList();

            return Ok(new LogsResponse
            {
                Logs = resultLogs,
                File = Path.GetFileName(logFile),
                TotalLines = allLines.Count,
                Message = resultLogs.Count == 0 ? "No logs match the specified filters" : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs");
            return StatusCode(500, new
            {
                Error = "Failed to retrieve logs",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Export logs as a downloadable text file
    /// </summary>
    /// <param name="level">Filter by log level</param>
    /// <param name="correlationId">Filter by correlation ID</param>
    /// <param name="lines">Number of lines to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Text file with logs</returns>
    [HttpGet("export")]
    public async Task<ActionResult> ExportLogs(
        [FromQuery] string? level = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] int? lines = null,
        CancellationToken ct = default)
    {
        try
        {
            var requestedLines = Math.Min(lines ?? MaxLinesDefault, MaxLinesLimit);
            
            _logger.LogInformation("Logs export requested");

            var logFile = GetMostRecentLogFile();
            if (logFile == null)
            {
                return NotFound("No log files available");
            }

            var allLines = await ReadLogFileLinesAsync(logFile, requestedLines * 2, ct);
            var parsedLogs = ParseLogEntries(allLines);
            var filteredLogs = ApplyFilters(parsedLogs, level, correlationId);
            var resultLogs = filteredLogs.TakeLast(requestedLines).ToList();

            // Create text content
            var content = string.Join(Environment.NewLine, resultLogs.Select(l => l.RawLine));
            var fileName = $"aura-logs-{DateTime.Now:yyyyMMdd-HHmmss}.txt";

            return File(
                System.Text.Encoding.UTF8.GetBytes(content),
                "text/plain",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting logs");
            return StatusCode(500, "Failed to export logs");
        }
    }

    /// <summary>
    /// Clear old log files (keeps last 7 days)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Status message</returns>
    [HttpPost("clear")]
    public async Task<ActionResult<object>> ClearOldLogs(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Log cleanup requested");

            if (!Directory.Exists(_logsDirectory))
            {
                return Ok(new { Message = "No logs directory found", FilesDeleted = 0 });
            }

            var files = Directory.GetFiles(_logsDirectory, "*.log", SearchOption.TopDirectoryOnly);
            var cutoffDate = DateTime.Now.AddDays(-7);
            var deletedCount = 0;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested)
                        break;

                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            deletedCount++;
                            _logger.LogInformation("Deleted old log file: {File}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete log file: {File}", file);
                        }
                    }
                }
            }, ct);

            return Ok(new
            {
                Message = $"Cleared {deletedCount} old log files",
                FilesDeleted = deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logs");
            return StatusCode(500, new { Error = "Failed to clear logs" });
        }
    }

    /// <summary>
    /// Get available log files
    /// </summary>
    [HttpGet("files")]
    public ActionResult<IEnumerable<LogFileInfo>> GetLogFiles()
    {
        try
        {
            if (!Directory.Exists(_logsDirectory))
            {
                return Ok(new List<LogFileInfo>());
            }

            var files = Directory.GetFiles(_logsDirectory, "*.log", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(30)
                .Select(f => new LogFileInfo
                {
                    Name = f.Name,
                    SizeBytes = f.Length,
                    LastModified = f.LastWriteTime,
                    Path = f.FullName
                })
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing log files");
            return StatusCode(500, new { Error = "Failed to list log files" });
        }
    }

    /// <summary>
    /// Get the most recent log file
    /// </summary>
    private string? GetMostRecentLogFile()
    {
        if (!Directory.Exists(_logsDirectory))
        {
            return null;
        }

        var files = Directory.GetFiles(_logsDirectory, "aura-api-*.log", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            return null;
        }

        return files
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .First()
            .FullName;
    }

    /// <summary>
    /// Read last N lines from a log file efficiently
    /// </summary>
    private async Task<List<string>> ReadLogFileLinesAsync(string filePath, int maxLines, CancellationToken ct)
    {
        var lines = new List<string>();

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream);

        // Read all lines (for simplicity - can optimize with reverse reading for huge files)
        var allLines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null && !ct.IsCancellationRequested)
        {
            allLines.Add(line);
        }

        // Return last N lines
        return allLines.TakeLast(maxLines).ToList();
    }

    /// <summary>
    /// Parse log file lines into structured log entries
    /// </summary>
    private List<LogEntry> ParseLogEntries(List<string> lines)
    {
        var entries = new List<LogEntry>();
        
        // Serilog format: [2024-01-15 10:30:45.123 +00:00] [INF] [correlation-id] Message {Properties}
        var logPattern = new Regex(@"^\[([\d\-: .+]+)\]\s+\[([A-Z]{3})\]\s+\[([^\]]*)\]\s+(.+)$");

        foreach (var line in lines)
        {
            var match = logPattern.Match(line);
            if (match.Success)
            {
                entries.Add(new LogEntry
                {
                    Timestamp = match.Groups[1].Value.Trim(),
                    Level = match.Groups[2].Value.Trim(),
                    CorrelationId = match.Groups[3].Value.Trim(),
                    Message = match.Groups[4].Value.Trim(),
                    RawLine = line
                });
            }
            else if (entries.Count > 0)
            {
                // Multi-line log entry (stack trace, etc.) - append to previous entry
                entries[entries.Count - 1].Message += Environment.NewLine + line;
                entries[entries.Count - 1].RawLine += Environment.NewLine + line;
            }
        }

        return entries;
    }

    /// <summary>
    /// Apply filters to log entries
    /// </summary>
    private IEnumerable<LogEntry> ApplyFilters(
        List<LogEntry> logs,
        string? level,
        string? correlationId)
    {
        IEnumerable<LogEntry> filtered = logs;

        if (!string.IsNullOrWhiteSpace(level))
        {
            filtered = filtered.Where(l => 
                l.Level.Equals(level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            filtered = filtered.Where(l => 
                l.CorrelationId.Contains(correlationId, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }
}

/// <summary>
/// Response model for logs endpoint
/// </summary>
public class LogsResponse
{
    public List<LogEntry> Logs { get; set; } = new();
    public string File { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Parsed log entry
/// </summary>
public class LogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RawLine { get; set; } = string.Empty;
}

/// <summary>
/// Log file information
/// </summary>
public class LogFileInfo
{
    public string Name { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string Path { get; set; } = string.Empty;
}
