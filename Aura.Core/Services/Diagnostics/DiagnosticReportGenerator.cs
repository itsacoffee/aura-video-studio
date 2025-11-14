using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aura.Core.Hardware;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for generating comprehensive diagnostic reports
/// </summary>
public class DiagnosticReportGenerator
{
    private readonly ILogger<DiagnosticReportGenerator> _logger;
    private readonly ErrorAggregationService _errorAggregation;
    private readonly PerformanceTrackingService _performanceTracking;
    private readonly IHardwareDetector? _hardwareDetector;
    private readonly string _logsDirectory;
    private readonly string _reportsDirectory;

    public DiagnosticReportGenerator(
        ILogger<DiagnosticReportGenerator> logger,
        ErrorAggregationService errorAggregation,
        PerformanceTrackingService performanceTracking,
        IHardwareDetector? hardwareDetector = null)
    {
        _logger = logger;
        _errorAggregation = errorAggregation;
        _performanceTracking = performanceTracking;
        _hardwareDetector = hardwareDetector;

        _logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        _reportsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagnosticReports");
        Directory.CreateDirectory(_reportsDirectory);
    }

    /// <summary>
    /// Generate a comprehensive diagnostic report as a ZIP file
    /// </summary>
    public async Task<DiagnosticReportResult> GenerateReportAsync(CancellationToken cancellationToken = default)
    {
        var reportId = Guid.NewGuid().ToString("N");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var reportName = $"diagnostic-report-{timestamp}-{reportId}";
        var reportDir = Path.Combine(_reportsDirectory, reportName);
        var zipPath = Path.Combine(_reportsDirectory, $"{reportName}.zip");

        try
        {
            _logger.LogInformation("Generating diagnostic report {ReportId}", reportId);

            Directory.CreateDirectory(reportDir);

            // 1. Collect system information
            await GenerateSystemInfoAsync(reportDir, cancellationToken).ConfigureAwait(false);

            // 2. Collect error summary
            await GenerateErrorSummaryAsync(reportDir, cancellationToken).ConfigureAwait(false);

            // 3. Collect performance metrics
            await GeneratePerformanceReportAsync(reportDir, cancellationToken).ConfigureAwait(false);

            // 4. Collect recent logs (last 1000 entries)
            await CollectRecentLogsAsync(reportDir, cancellationToken).ConfigureAwait(false);

            // 5. Collect FFmpeg version info
            await GenerateFFmpegInfoAsync(reportDir, cancellationToken).ConfigureAwait(false);

            // 6. Create ZIP file
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(reportDir, zipPath, CompressionLevel.Optimal, false);

            // Clean up temporary directory
            Directory.Delete(reportDir, true);

            _logger.LogInformation("Diagnostic report generated successfully: {ZipPath}", zipPath);

            return new DiagnosticReportResult
            {
                ReportId = reportId,
                FilePath = zipPath,
                FileName = Path.GetFileName(zipPath),
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                SizeBytes = new FileInfo(zipPath).Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate diagnostic report");
            
            // Clean up on failure
            try
            {
                if (Directory.Exists(reportDir))
                {
                    Directory.Delete(reportDir, true);
                }
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            throw;
        }
    }

    /// <summary>
    /// Generate system information file
    /// </summary>
    private async Task GenerateSystemInfoAsync(string outputDir, CancellationToken cancellationToken)
    {
        var systemInfo = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["machineName"] = Environment.MachineName,
            ["osVersion"] = Environment.OSVersion.ToString(),
            ["is64BitOS"] = Environment.Is64BitOperatingSystem,
            ["is64BitProcess"] = Environment.Is64BitProcess,
            ["processorCount"] = Environment.ProcessorCount,
            ["dotnetVersion"] = Environment.Version.ToString(),
            ["workingSet"] = Environment.WorkingSet,
            ["systemDirectory"] = Environment.SystemDirectory,
            ["currentDirectory"] = Environment.CurrentDirectory
        };

        // Add hardware info if available
        if (_hardwareDetector != null)
        {
            try
            {
                var hardware = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
                systemInfo["hardware"] = new
                {
                    logicalCores = hardware.LogicalCores,
                    physicalCores = hardware.PhysicalCores,
                    ramGB = hardware.RamGB,
                    gpu = hardware.Gpu != null ? $"{hardware.Gpu.Vendor} {hardware.Gpu.Model}" : null,
                    tier = hardware.Tier.ToString(),
                    enableNVENC = hardware.EnableNVENC,
                    enableSD = hardware.EnableSD
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect hardware information");
                systemInfo["hardwareError"] = ex.Message;
            }
        }

        var json = JsonSerializer.Serialize(systemInfo, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "system-info.json"), json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate error summary file
    /// </summary>
    private async Task GenerateErrorSummaryAsync(string outputDir, CancellationToken cancellationToken)
    {
        var errors = _errorAggregation.GetAggregatedErrors(TimeSpan.FromDays(7), limit: 100);
        var statistics = _errorAggregation.GetStatistics(TimeSpan.FromDays(7));

        var errorSummary = new
        {
            statistics,
            topErrors = errors.Take(20).Select(e => new
            {
                signature = e.Signature,
                exceptionType = e.ExceptionType,
                message = e.Message,
                count = e.Count,
                firstSeen = e.FirstSeen,
                lastSeen = e.LastSeen,
                sampleCorrelationId = e.SampleCorrelationId
            })
        };

        var json = JsonSerializer.Serialize(errorSummary, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "error-summary.json"), json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate performance report file
    /// </summary>
    private async Task GeneratePerformanceReportAsync(string outputDir, CancellationToken cancellationToken)
    {
        var metrics = _performanceTracking.GetMetrics();
        var slowOps = _performanceTracking.GetSlowOperations(limit: 50);

        var performanceReport = new
        {
            metrics = metrics.Take(50).Select(m => new
            {
                operationName = m.OperationName,
                count = m.Count,
                averageDurationMs = m.AverageDuration.TotalMilliseconds,
                minDurationMs = m.MinDuration.TotalMilliseconds,
                maxDurationMs = m.MaxDuration.TotalMilliseconds,
                p50DurationMs = m.P50Duration.TotalMilliseconds,
                p95DurationMs = m.P95Duration.TotalMilliseconds,
                p99DurationMs = m.P99Duration.TotalMilliseconds,
                lastExecuted = m.LastExecuted,
                slowOperationCount = m.SlowOperationCount
            }),
            slowOperations = slowOps.Select(so => new
            {
                operationName = so.OperationName,
                durationMs = so.Duration.TotalMilliseconds,
                timestamp = so.Timestamp,
                correlationId = so.CorrelationId
            })
        };

        var json = JsonSerializer.Serialize(performanceReport, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "performance-report.json"), json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Collect recent log entries
    /// </summary>
    private async Task CollectRecentLogsAsync(string outputDir, CancellationToken cancellationToken)
    {
        try
        {
            if (!Directory.Exists(_logsDirectory))
            {
                _logger.LogWarning("Logs directory not found: {LogsDirectory}", _logsDirectory);
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "logs-not-found.txt"),
                    $"Logs directory not found: {_logsDirectory}",
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            var logFiles = Directory.GetFiles(_logsDirectory, "*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(3) // Last 3 log files
                .ToList();

            if (logFiles.Count == 0)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "no-logs.txt"),
                    "No log files found",
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            // Copy log files with redacted sensitive data
            foreach (var logFile in logFiles)
            {
                var content = await File.ReadAllTextAsync(logFile.FullName, cancellationToken).ConfigureAwait(false);
                var redactedContent = RedactSensitiveData(content);
                
                var outputPath = Path.Combine(outputDir, $"log-{logFile.Name}");
                await File.WriteAllTextAsync(outputPath, redactedContent, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect log files");
            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "log-collection-error.txt"),
                $"Error: {ex.Message}",
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Generate FFmpeg version information
    /// </summary>
    private async Task GenerateFFmpegInfoAsync(string outputDir, CancellationToken cancellationToken)
    {
        try
        {
            var ffmpegPaths = new[]
            {
                "ffmpeg",
                "ffmpeg.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin", "ffmpeg.exe")
            };

            string? ffmpegPath = null;
            foreach (var path in ffmpegPaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                        
                        if (process.ExitCode == 0)
                        {
                            ffmpegPath = path;
                            await File.WriteAllTextAsync(
                                Path.Combine(outputDir, "ffmpeg-version.txt"),
                                output,
                                cancellationToken).ConfigureAwait(false);
                            break;
                        }
                    }
                }
                catch
                {
                    // Try next path
                    continue;
                }
            }

            if (ffmpegPath == null)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "ffmpeg-not-found.txt"),
                    "FFmpeg not found in standard locations",
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get FFmpeg version");
            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "ffmpeg-error.txt"),
                $"Error: {ex.Message}",
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Redact sensitive data from logs (API keys, tokens, etc.)
    /// </summary>
    private string RedactSensitiveData(string content)
    {
        // Redact API keys (various patterns)
        content = Regex.Replace(content, @"sk-[a-zA-Z0-9]{32,}", "[REDACTED-API-KEY]");
        content = Regex.Replace(content, @"Bearer\s+[a-zA-Z0-9\-_\.]{20,}", "Bearer [REDACTED-TOKEN]");
        content = Regex.Replace(content, @"""apiKey""\s*:\s*""[^""]+""", @"""apiKey"": ""[REDACTED]""", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"""api_key""\s*:\s*""[^""]+""", @"""api_key"": ""[REDACTED]""", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"""password""\s*:\s*""[^""]+""", @"""password"": ""[REDACTED]""", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"""token""\s*:\s*""[^""]+""", @"""token"": ""[REDACTED]""", RegexOptions.IgnoreCase);

        return content;
    }

    /// <summary>
    /// Clean up expired reports
    /// </summary>
    public int CleanupExpiredReports(TimeSpan expirationTime)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - expirationTime;
            var files = Directory.GetFiles(_reportsDirectory, "diagnostic-report-*.zip");
            int deleted = 0;

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffTime)
                {
                    File.Delete(file);
                    deleted++;
                }
            }

            if (deleted > 0)
            {
                _logger.LogInformation("Cleaned up {DeletedCount} expired diagnostic reports", deleted);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired reports");
            return 0;
        }
    }

    /// <summary>
    /// Get path to a report by ID
    /// </summary>
    public string? GetReportPath(string reportId)
    {
        var files = Directory.GetFiles(_reportsDirectory, $"*{reportId}.zip");
        return files.FirstOrDefault();
    }
}

/// <summary>
/// Result of generating a diagnostic report
/// </summary>
public class DiagnosticReportResult
{
    public string ReportId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long SizeBytes { get; set; }
}
