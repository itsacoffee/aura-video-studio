using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// FFmpeg command execution record
/// </summary>
public record FFmpegCommandRecord
{
    public string JobId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Command { get; init; } = string.Empty;
    public string[] Arguments { get; init; } = Array.Empty<string>();
    public string WorkingDirectory { get; init; } = string.Empty;
    public Dictionary<string, string> Environment { get; init; } = new();
    public EncoderInfo Encoder { get; init; } = new();
    public int? ExitCode { get; init; }
    public TimeSpan? Duration { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }
}

/// <summary>
/// Encoder information
/// </summary>
public record EncoderInfo
{
    public string Name { get; init; } = string.Empty;
    public bool IsHardwareAccelerated { get; init; }
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Service for logging FFmpeg commands for debugging and support
/// </summary>
public class FFmpegCommandLogger
{
    private readonly ILogger<FFmpegCommandLogger> _logger;
    private readonly string _logDirectory;

    public FFmpegCommandLogger(ILogger<FFmpegCommandLogger> logger, string? logDirectory = null)
    {
        _logger = logger;
        
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "FFmpegLogs");

        EnsureLogDirectory();
    }

    /// <summary>
    /// Logs an FFmpeg command execution
    /// </summary>
    public async Task LogCommandAsync(FFmpegCommandRecord record)
    {
        try
        {
            var fileName = $"{record.JobId}_{record.Timestamp:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_logDirectory, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(record, options);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation(
                "FFmpeg command logged: JobId={JobId}, CorrelationId={CorrelationId}, Success={Success}, Path={Path}",
                record.JobId, record.CorrelationId, record.Success, filePath);

            CleanupOldLogs();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log FFmpeg command for JobId={JobId}", record.JobId);
        }
    }

    /// <summary>
    /// Retrieves logged commands for a specific job
    /// </summary>
    public async Task<List<FFmpegCommandRecord>> GetCommandsByJobIdAsync(string jobId)
    {
        var records = new List<FFmpegCommandRecord>();

        try
        {
            var files = Directory.GetFiles(_logDirectory, $"{jobId}_*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var record = JsonSerializer.Deserialize<FFmpegCommandRecord>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read FFmpeg log file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve FFmpeg commands for JobId={JobId}", jobId);
        }

        return records;
    }

    /// <summary>
    /// Retrieves logged commands by correlation ID
    /// </summary>
    public async Task<List<FFmpegCommandRecord>> GetCommandsByCorrelationIdAsync(string correlationId)
    {
        var records = new List<FFmpegCommandRecord>();

        try
        {
            var files = Directory.GetFiles(_logDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var record = JsonSerializer.Deserialize<FFmpegCommandRecord>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (record?.CorrelationId == correlationId)
                    {
                        records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read FFmpeg log file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve FFmpeg commands for CorrelationId={CorrelationId}", correlationId);
        }

        return records;
    }

    /// <summary>
    /// Gets the most recent command record
    /// </summary>
    public async Task<FFmpegCommandRecord?> GetMostRecentCommandAsync()
    {
        try
        {
            var files = Directory.GetFiles(_logDirectory, "*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .FirstOrDefault();

            if (files == null) return null;

            var json = await File.ReadAllTextAsync(files);
            return JsonSerializer.Deserialize<FFmpegCommandRecord>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve most recent FFmpeg command");
            return null;
        }
    }

    /// <summary>
    /// Generates a support diagnostic report
    /// </summary>
    public async Task<string> GenerateSupportReportAsync(string jobId)
    {
        var commands = await GetCommandsByJobIdAsync(jobId);

        if (commands.Count == 0)
        {
            return $"No FFmpeg commands found for JobId: {jobId}";
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== FFmpeg Support Report ===");
        report.AppendLine($"Job ID: {jobId}");
        report.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        report.AppendLine($"Total Commands: {commands.Count}");
        report.AppendLine();

        foreach (var cmd in commands.OrderBy(c => c.Timestamp))
        {
            report.AppendLine($"--- Command #{commands.IndexOf(cmd) + 1} ---");
            report.AppendLine($"Timestamp: {cmd.Timestamp:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Correlation ID: {cmd.CorrelationId}");
            report.AppendLine($"Success: {cmd.Success}");
            
            if (cmd.ExitCode.HasValue)
                report.AppendLine($"Exit Code: {cmd.ExitCode.Value}");
            
            if (cmd.Duration.HasValue)
                report.AppendLine($"Duration: {cmd.Duration.Value.TotalSeconds:F2}s");

            report.AppendLine($"Encoder: {cmd.Encoder.Name} ({(cmd.Encoder.IsHardwareAccelerated ? "Hardware" : "Software")})");
            report.AppendLine($"Working Directory: {cmd.WorkingDirectory}");
            
            if (!string.IsNullOrEmpty(cmd.OutputPath))
                report.AppendLine($"Output Path: {cmd.OutputPath}");

            report.AppendLine("Command:");
            report.AppendLine($"  {cmd.Command}");
            
            if (cmd.Arguments.Length > 0)
            {
                report.AppendLine("Arguments:");
                foreach (var arg in cmd.Arguments)
                {
                    report.AppendLine($"  {arg}");
                }
            }

            if (!string.IsNullOrEmpty(cmd.ErrorMessage))
            {
                report.AppendLine("Error:");
                report.AppendLine($"  {cmd.ErrorMessage}");
            }

            report.AppendLine();
        }

        return report.ToString();
    }

    private void EnsureLogDirectory()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
                _logger.LogInformation("Created FFmpeg log directory: {Directory}", _logDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create FFmpeg log directory: {Directory}", _logDirectory);
        }
    }

    private void CleanupOldLogs()
    {
        try
        {
            var retentionDays = 30;
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldFiles = Directory.GetFiles(_logDirectory, "*.json")
                .Where(f => File.GetCreationTimeUtc(f) < cutoffDate)
                .ToArray();

            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old FFmpeg log: {File}", file);
                }
            }

            if (oldFiles.Length > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old FFmpeg log files", oldFiles.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old FFmpeg logs");
        }
    }
}
