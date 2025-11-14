using Aura.Core.Models.Diagnostics;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for handling crash reports from clients
/// </summary>
public class CrashReportService
{
    private readonly ILogger<CrashReportService> _logger;
    private readonly string _crashReportsDirectory;
    private readonly int _maxReportsToKeep = 100;

    public CrashReportService(ILogger<CrashReportService> logger)
    {
        _logger = logger;
        _crashReportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashReports");
        Directory.CreateDirectory(_crashReportsDirectory);
    }

    /// <summary>
    /// Save a client error report
    /// </summary>
    public async Task<string> SaveClientErrorReportAsync(ClientErrorReport report, CancellationToken cancellationToken = default)
    {
        try
        {
            var reportId = report.Id;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            var fileName = $"client-error-{timestamp}-{reportId}.json";
            var filePath = Path.Combine(_crashReportsDirectory, fileName);

            // Log the error
            _logger.LogError(
                "Client error report received: {Severity} - {Title} | {Message} | CorrelationId: {CorrelationId}",
                report.Severity,
                report.Title,
                report.Message,
                report.CorrelationId ?? "N/A");

            // Log technical details if available
            if (!string.IsNullOrEmpty(report.TechnicalDetails))
            {
                _logger.LogError("Technical details: {Details}", report.TechnicalDetails);
            }

            // Log stack trace if available
            if (!string.IsNullOrEmpty(report.StackTrace))
            {
                _logger.LogError("Stack trace: {StackTrace}", report.StackTrace);
            }

            // Log browser info
            if (report.BrowserInfo != null)
            {
                _logger.LogInformation(
                    "Browser: {UserAgent} | Platform: {Platform} | Language: {Language}",
                    report.BrowserInfo.UserAgent,
                    report.BrowserInfo.Platform,
                    report.BrowserInfo.Language);
            }

            // Save full report to file
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Client error report saved to {FilePath}", filePath);

            // Cleanup old reports
            await CleanupOldReportsAsync().ConfigureAwait(false);

            return reportId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save client error report");
            throw;
        }
    }

    /// <summary>
    /// Get all crash reports
    /// </summary>
    public async Task<List<ClientErrorReport>> GetReportsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var reports = new List<ClientErrorReport>();
            var files = Directory.GetFiles(_crashReportsDirectory, "client-error-*.json")
                .OrderByDescending(f => new FileInfo(f).CreationTimeUtc)
                .Take(limit);

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                    var report = JsonSerializer.Deserialize<ClientErrorReport>(json);
                    if (report != null)
                    {
                        reports.Add(report);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read crash report from {File}", file);
                }
            }

            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get crash reports");
            return new List<ClientErrorReport>();
        }
    }

    /// <summary>
    /// Get a specific crash report by ID
    /// </summary>
    public async Task<ClientErrorReport?> GetReportByIdAsync(string reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var files = Directory.GetFiles(_crashReportsDirectory, $"client-error-*-{reportId}.json");
            if (files.Length == 0)
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(files[0], cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ClientErrorReport>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get crash report {ReportId}", reportId);
            return null;
        }
    }

    /// <summary>
    /// Delete a crash report
    /// </summary>
    public async Task<bool> DeleteReportAsync(string reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var files = Directory.GetFiles(_crashReportsDirectory, $"client-error-*-{reportId}.json");
            if (files.Length == 0)
            {
                return false;
            }

            await Task.Run(() => File.Delete(files[0]), cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleted crash report {ReportId}", reportId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete crash report {ReportId}", reportId);
            return false;
        }
    }

    /// <summary>
    /// Get crash report statistics
    /// </summary>
    public async Task<CrashReportStatistics> GetStatisticsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - timeWindow;
            var reports = await GetReportsAsync(1000, cancellationToken).ConfigureAwait(false);

            var recentReports = reports
                .Where(r => DateTime.TryParse(r.Timestamp, out var ts) && ts >= cutoffTime)
                .ToList();

            var statistics = new CrashReportStatistics
            {
                TotalReports = recentReports.Count,
                CriticalErrors = recentReports.Count(r => r.Severity?.ToLower() == "critical"),
                Errors = recentReports.Count(r => r.Severity?.ToLower() == "error"),
                Warnings = recentReports.Count(r => r.Severity?.ToLower() == "warning"),
                UniqueUsers = recentReports
                    .Where(r => !string.IsNullOrEmpty(r.CorrelationId))
                    .Select(r => r.CorrelationId)
                    .Distinct()
                    .Count(),
                TopErrors = recentReports
                    .GroupBy(r => r.Title)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new ErrorSummary
                    {
                        Title = g.Key,
                        Count = g.Count(),
                        LastOccurrence = g.Max(r => DateTime.TryParse(r.Timestamp, out var ts) ? ts : DateTime.MinValue)
                    })
                    .ToList(),
                TimeWindow = timeWindow
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get crash report statistics");
            return new CrashReportStatistics { TimeWindow = timeWindow };
        }
    }

    /// <summary>
    /// Export crash reports as a compressed archive
    /// </summary>
    public async Task<string> ExportReportsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            var exportFileName = $"crash-reports-export-{timestamp}.zip";
            var exportPath = Path.Combine(_crashReportsDirectory, exportFileName);

            var reports = await GetReportsAsync(1000, cancellationToken).ConfigureAwait(false);

            // Filter by date range if specified
            if (startDate.HasValue || endDate.HasValue)
            {
                reports = reports.Where(r =>
                {
                    if (!DateTime.TryParse(r.Timestamp, out var ts))
                        return false;

                    if (startDate.HasValue && ts < startDate.Value)
                        return false;

                    if (endDate.HasValue && ts > endDate.Value)
                        return false;

                    return true;
                }).ToList();
            }

            // Create temporary directory for export
            var tempDir = Path.Combine(Path.GetTempPath(), $"crash-reports-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Copy report files to temp directory
                foreach (var report in reports)
                {
                    var fileName = $"{report.Severity}-{report.Id}.json";
                    var filePath = Path.Combine(tempDir, fileName);
                    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
                }

                // Create ZIP archive
                if (File.Exists(exportPath))
                {
                    File.Delete(exportPath);
                }

                ZipFile.CreateFromDirectory(tempDir, exportPath, CompressionLevel.Optimal, false);

                _logger.LogInformation("Exported {Count} crash reports to {Path}", reports.Count, exportPath);

                return exportPath;
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export crash reports");
            throw;
        }
    }

    /// <summary>
    /// Cleanup old crash reports
    /// </summary>
    private async Task CleanupOldReportsAsync()
    {
        try
        {
            var files = Directory.GetFiles(_crashReportsDirectory, "client-error-*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            if (files.Count <= _maxReportsToKeep)
            {
                return;
            }

            var filesToDelete = files.Skip(_maxReportsToKeep);
            foreach (var file in filesToDelete)
            {
                try
                {
                    await Task.Run(() => file.Delete()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old crash report {File}", file.FullName);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old crash reports", filesToDelete.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old crash reports");
        }
    }
}

/// <summary>
/// Crash report statistics
/// </summary>
public class CrashReportStatistics
{
    public int TotalReports { get; set; }
    public int CriticalErrors { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int UniqueUsers { get; set; }
    public List<ErrorSummary> TopErrors { get; set; } = new();
    public TimeSpan TimeWindow { get; set; }
}

/// <summary>
/// Error summary for statistics
/// </summary>
public class ErrorSummary
{
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
}
