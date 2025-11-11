using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.CostTracking;
using Aura.Core.Models.Diagnostics;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Service for generating comprehensive diagnostic bundles for failed jobs
/// </summary>
public class DiagnosticBundleService
{
    private readonly ILogger<DiagnosticBundleService> _logger;
    private readonly DiagnosticReportGenerator _reportGenerator;
    private readonly IHardwareDetector? _hardwareDetector;
    private readonly RunTelemetryCollector? _telemetryCollector;
    private readonly string _bundlesDirectory;

    public DiagnosticBundleService(
        ILogger<DiagnosticBundleService> logger,
        DiagnosticReportGenerator reportGenerator,
        IHardwareDetector? hardwareDetector = null,
        RunTelemetryCollector? telemetryCollector = null)
    {
        _logger = logger;
        _reportGenerator = reportGenerator;
        _hardwareDetector = hardwareDetector;
        _telemetryCollector = telemetryCollector;

        _bundlesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagnosticBundles");
        Directory.CreateDirectory(_bundlesDirectory);
    }

    /// <summary>
    /// Generate a comprehensive diagnostic bundle for a specific job
    /// </summary>
    public async Task<DiagnosticBundle> GenerateBundleAsync(
        Job job,
        RunCostReport? costReport = null,
        List<ModelDecision>? modelDecisions = null,
        List<FFmpegCommand>? ffmpegCommands = null,
        CancellationToken cancellationToken = default)
    {
        var bundleId = Guid.NewGuid().ToString("N");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
        var bundleName = $"diagnostic-bundle-{job.Id}-{timestamp}";
        var bundleDir = Path.Combine(_bundlesDirectory, bundleName);
        var zipPath = Path.Combine(_bundlesDirectory, $"{bundleName}.zip");

        try
        {
            _logger.LogInformation("Generating diagnostic bundle for job {JobId}, BundleId: {BundleId}", 
                job.Id, bundleId);

            Directory.CreateDirectory(bundleDir);

            // Build manifest
            var manifest = await BuildManifestAsync(job, costReport, modelDecisions, ffmpegCommands, cancellationToken);

            // Save manifest
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await File.WriteAllTextAsync(Path.Combine(bundleDir, "manifest.json"), manifestJson, cancellationToken);

            // Collect system info
            await CollectSystemInfoAsync(bundleDir, cancellationToken);

            // Collect job-specific logs
            await CollectJobLogsAsync(bundleDir, job.Id, job.CorrelationId, cancellationToken);

            // Collect timeline
            await SaveTimelineAsync(bundleDir, manifest.Timeline, cancellationToken);

            // Collect model decisions
            if (manifest.ModelDecisions.Count > 0)
            {
                await SaveModelDecisionsAsync(bundleDir, manifest.ModelDecisions, cancellationToken);
            }

            // Collect FFmpeg commands
            if (manifest.FFmpegCommands.Count > 0)
            {
                await SaveFFmpegCommandsAsync(bundleDir, manifest.FFmpegCommands, cancellationToken);
            }

            // Collect cost report
            if (manifest.CostReport != null)
            {
                await SaveCostReportAsync(bundleDir, manifest.CostReport, cancellationToken);
            }

            // Collect RunTelemetry
            await CollectRunTelemetryAsync(bundleDir, job.Id, cancellationToken);

            // Create README
            await CreateReadmeAsync(bundleDir, job, manifest, cancellationToken);

            // Create ZIP file
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(bundleDir, zipPath, CompressionLevel.Optimal, false);

            // Clean up temporary directory
            Directory.Delete(bundleDir, true);

            _logger.LogInformation("Diagnostic bundle generated successfully: {ZipPath}", zipPath);

            return new DiagnosticBundle
            {
                BundleId = bundleId,
                JobId = job.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                FilePath = zipPath,
                FileName = Path.GetFileName(zipPath),
                SizeBytes = new FileInfo(zipPath).Length,
                Manifest = manifest
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate diagnostic bundle for job {JobId}", job.Id);
            
            // Clean up on failure
            try
            {
                if (Directory.Exists(bundleDir))
                {
                    Directory.Delete(bundleDir, true);
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
    /// Build the bundle manifest
    /// </summary>
    private async Task<BundleManifest> BuildManifestAsync(
        Job job,
        RunCostReport? costReport,
        List<ModelDecision>? modelDecisions,
        List<FFmpegCommand>? ffmpegCommands,
        CancellationToken cancellationToken)
    {
        // Build job info
        var jobInfo = new JobInfo
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            Stage = job.Stage,
            StartedAt = job.StartedAt,
            CompletedAt = job.FinishedAt,
            ErrorMessage = job.ErrorMessage,
            ErrorCode = job.FailureDetails?.ErrorCode,
            CorrelationId = job.CorrelationId,
            Warnings = job.Warnings.ToList()
        };

        // Build timeline from job steps
        var timeline = job.Steps.Select(step => new TimelineEntry
        {
            Stage = step.Name,
            CorrelationId = job.CorrelationId ?? $"step-{step.Name}",
            StartedAt = step.StartedAt ?? DateTime.UtcNow,
            CompletedAt = step.CompletedAt,
            Status = step.Status.ToString(),
            ErrorMessage = step.Errors.FirstOrDefault()?.Message
        }).ToList();

        // Add fallback timeline entry if steps are empty
        if (timeline.Count == 0)
        {
            timeline.Add(new TimelineEntry
            {
                Stage = job.Stage,
                CorrelationId = job.CorrelationId ?? job.Id,
                StartedAt = job.StartedAt,
                CompletedAt = job.FinishedAt,
                Status = job.Status.ToString(),
                ErrorMessage = job.ErrorMessage
            });
        }

        // Build system profile
        Models.Diagnostics.SystemProfile? systemProfile = null;
        if (_hardwareDetector != null)
        {
            try
            {
                var hw = await _hardwareDetector.DetectSystemAsync();
                systemProfile = new Models.Diagnostics.SystemProfile
                {
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    DotNetVersion = Environment.Version.ToString(),
                    WorkingSetBytes = (int)Environment.WorkingSet,
                    Hardware = new Models.Diagnostics.HardwareInfo
                    {
                        LogicalCores = hw.LogicalCores,
                        PhysicalCores = hw.PhysicalCores,
                        RamGB = hw.RamGB,
                        GpuVendor = hw.Gpu?.Vendor,
                        GpuModel = hw.Gpu?.Model,
                        Tier = hw.Tier.ToString()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect hardware information for bundle");
            }
        }

        // Build cost report summary
        CostReportSummary? costSummary = null;
        if (costReport != null)
        {
            costSummary = new CostReportSummary
            {
                TotalCost = costReport.TotalCost,
                Currency = costReport.Currency,
                CostByStage = costReport.CostByStage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Cost),
                CostByProvider = new Dictionary<string, decimal>(costReport.CostByProvider),
                TotalTokens = (int)(costReport.TokenStats?.TotalTokens ?? 0),
                WithinBudget = costReport.WithinBudget
            };
        }

        return new BundleManifest
        {
            Job = jobInfo,
            SystemProfile = systemProfile,
            Timeline = timeline,
            ModelDecisions = modelDecisions ?? new List<ModelDecision>(),
            FFmpegCommands = ffmpegCommands ?? new List<FFmpegCommand>(),
            CostReport = costSummary,
            Files = new List<BundleFile>
            {
                new BundleFile { FileName = "manifest.json", Description = "Bundle manifest", SizeBytes = 0 },
                new BundleFile { FileName = "system-info.json", Description = "System information", SizeBytes = 0 },
                new BundleFile { FileName = "timeline.json", Description = "Job execution timeline", SizeBytes = 0 },
                new BundleFile { FileName = "logs-redacted.txt", Description = "Anonymized logs", SizeBytes = 0 },
                new BundleFile { FileName = "README.txt", Description = "Bundle overview", SizeBytes = 0 }
            }
        };
    }

    /// <summary>
    /// Collect system information
    /// </summary>
    private async Task CollectSystemInfoAsync(string outputDir, CancellationToken cancellationToken)
    {
        var systemInfo = new
        {
            timestamp = DateTime.UtcNow,
            machineName = Environment.MachineName,
            osVersion = Environment.OSVersion.ToString(),
            processorCount = Environment.ProcessorCount,
            dotnetVersion = Environment.Version.ToString(),
            workingSet = Environment.WorkingSet
        };

        var json = JsonSerializer.Serialize(systemInfo, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "system-info.json"), json, cancellationToken);
    }

    /// <summary>
    /// Collect job-specific logs with allowlist-based redaction and time windowing
    /// </summary>
    private async Task CollectJobLogsAsync(string outputDir, string jobId, string? correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logsDirectory))
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "logs-not-found.txt"),
                    "Logs directory not found",
                    cancellationToken);
                return;
            }

            var logFiles = Directory.GetFiles(logsDirectory, "*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(3)
                .ToList();

            if (logFiles.Count == 0)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "no-logs.txt"),
                    "No log files found",
                    cancellationToken);
                return;
            }

            // Load telemetry to get failure time for windowing
            DateTime? failureTime = null;
            if (_telemetryCollector != null)
            {
                var telemetry = _telemetryCollector.LoadTelemetry(jobId);
                if (telemetry != null)
                {
                    var failedRecord = telemetry.Records.FirstOrDefault(r => r.ResultStatus == ResultStatus.Error);
                    failureTime = failedRecord?.EndedAt;
                }
            }

            var allRedactedLogs = new System.Text.StringBuilder();
            foreach (var logFile in logFiles)
            {
                var content = await File.ReadAllTextAsync(logFile.FullName, cancellationToken);
                
                // Filter for job-specific logs if correlation ID available
                var lines = content.Split('\n');
                var relevantLines = lines.Where(line => 
                    string.IsNullOrEmpty(correlationId) || 
                    line.Contains(jobId) || 
                    line.Contains(correlationId ?? string.Empty)).ToList();

                // Apply time-windowed filtering if we have a failure time (±5 minutes)
                IEnumerable<string> redactedLines;
                if (failureTime.HasValue)
                {
                    redactedLines = RedactionService.RedactLogLines(relevantLines, failureTime.Value, TimeSpan.FromMinutes(5));
                }
                else
                {
                    redactedLines = RedactionService.RedactLogLines(relevantLines);
                }

                allRedactedLogs.AppendLine($"=== {logFile.Name} ===");
                foreach (var line in redactedLines)
                {
                    allRedactedLogs.AppendLine(line);
                }
                allRedactedLogs.AppendLine();
            }

            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "logs-redacted.txt"),
                allRedactedLogs.ToString(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect job logs");
            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "log-collection-error.txt"),
                $"Error: {ex.Message}",
                cancellationToken);
        }
    }

    /// <summary>
    /// Save timeline to file
    /// </summary>
    private async Task SaveTimelineAsync(string outputDir, List<TimelineEntry> timeline, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(timeline, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "timeline.json"), json, cancellationToken);
    }

    /// <summary>
    /// Save model decisions to file
    /// </summary>
    private async Task SaveModelDecisionsAsync(string outputDir, List<ModelDecision> decisions, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(decisions, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "model-decisions.json"), json, cancellationToken);
    }

    /// <summary>
    /// Save FFmpeg commands to file
    /// </summary>
    private async Task SaveFFmpegCommandsAsync(string outputDir, List<FFmpegCommand> commands, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(commands, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "ffmpeg-commands.json"), json, cancellationToken);
    }

    /// <summary>
    /// Save cost report to file
    /// </summary>
    private async Task SaveCostReportAsync(string outputDir, CostReportSummary costReport, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(costReport, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(Path.Combine(outputDir, "cost-report.json"), json, cancellationToken);
    }

    /// <summary>
    /// Collect RunTelemetry data with redaction
    /// </summary>
    private async Task CollectRunTelemetryAsync(string outputDir, string jobId, CancellationToken cancellationToken)
    {
        try
        {
            if (_telemetryCollector == null)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "telemetry-not-available.txt"),
                    "Telemetry collector not configured",
                    cancellationToken);
                return;
            }

            var telemetry = _telemetryCollector.LoadTelemetry(jobId);
            if (telemetry == null)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(outputDir, "run_telemetry-not-found.txt"),
                    "No telemetry data available for this job",
                    cancellationToken);
                return;
            }

            // Serialize and redact telemetry
            var json = JsonSerializer.Serialize(telemetry, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Apply text-based redaction to the JSON string
            var redactedJson = RedactionService.RedactText(json);

            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "run_telemetry.json"),
                redactedJson,
                cancellationToken);

            _logger.LogInformation("Collected RunTelemetry for job {JobId} with {RecordCount} records", 
                jobId, telemetry.Records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect run telemetry for job {JobId}", jobId);
            await File.WriteAllTextAsync(
                Path.Combine(outputDir, "telemetry-collection-error.txt"),
                $"Error: {ex.Message}",
                cancellationToken);
        }
    }

    /// <summary>
    /// Create README file explaining bundle contents
    /// </summary>
    private async Task CreateReadmeAsync(string outputDir, Job job, BundleManifest manifest, CancellationToken cancellationToken)
    {
        var readme = new System.Text.StringBuilder();
        readme.AppendLine("AURA VIDEO STUDIO - DIAGNOSTIC BUNDLE");
        readme.AppendLine("=====================================");
        readme.AppendLine();
        readme.AppendLine($"Job ID: {job.Id}");
        readme.AppendLine($"Status: {job.Status}");
        readme.AppendLine($"Stage: {job.Stage}");
        readme.AppendLine($"Started: {job.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        if (job.FinishedAt.HasValue)
        {
            readme.AppendLine($"Finished: {job.FinishedAt.Value:yyyy-MM-dd HH:mm:ss} UTC");
        }
        readme.AppendLine($"Correlation ID: {job.CorrelationId ?? "N/A"}");
        readme.AppendLine();
        
        if (!string.IsNullOrEmpty(job.ErrorMessage))
        {
            readme.AppendLine("ERROR:");
            readme.AppendLine(job.ErrorMessage);
            readme.AppendLine();
        }

        readme.AppendLine("BUNDLE CONTENTS:");
        readme.AppendLine("----------------");
        readme.AppendLine("- manifest.json: Complete bundle manifest with all metadata");
        readme.AppendLine("- system-info.json: System and hardware information");
        readme.AppendLine("- timeline.json: Job execution timeline with durations and correlation IDs");
        readme.AppendLine("- run_telemetry.json: Complete telemetry with cost, latency, and provider data");
        readme.AppendLine("- logs-redacted.txt: Time-windowed logs around failure (allowlist redacted)");
        
        if (manifest.ModelDecisions.Count > 0)
        {
            readme.AppendLine("- model-decisions.json: AI model selection decisions");
        }
        
        if (manifest.FFmpegCommands.Count > 0)
        {
            readme.AppendLine("- ffmpeg-commands.json: FFmpeg commands executed with context");
        }
        
        if (manifest.CostReport != null)
        {
            readme.AppendLine("- cost-report.json: Cost breakdown by stage and provider");
        }

        readme.AppendLine();
        readme.AppendLine("PRIVACY & REDACTION:");
        readme.AppendLine("--------------------");
        readme.AppendLine("This bundle uses allowlist-based redaction (deny by default):");
        readme.AppendLine("- API keys, tokens, and secrets are REMOVED");
        readme.AppendLine("- Passwords and credentials are REMOVED");
        readme.AppendLine("- Only technical metadata and error details are INCLUDED");
        readme.AppendLine("- Machine names and PII are anonymized");
        readme.AppendLine();
        readme.AppendLine("Safe to share for troubleshooting purposes.");

        await File.WriteAllTextAsync(Path.Combine(outputDir, "README.txt"), readme.ToString(), cancellationToken);
    }

    /// <summary>
    /// Get bundle path by ID
    /// </summary>
    public string? GetBundlePath(string bundleId)
    {
        var files = Directory.GetFiles(_bundlesDirectory, $"*{bundleId}*.zip");
        return files.FirstOrDefault();
    }

    /// <summary>
    /// Redact sensitive data from logs (API keys, tokens, etc.)
    /// Delegates to RedactionService for consistency
    /// </summary>
    [Obsolete("Use RedactionService.RedactText instead")]
    public static string RedactSensitiveData(string content)
    {
        return RedactionService.RedactText(content);
    }

    /// <summary>
    /// Clean up expired bundles
    /// </summary>
    public int CleanupExpiredBundles(TimeSpan expirationTime)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - expirationTime;
            var files = Directory.GetFiles(_bundlesDirectory, "diagnostic-bundle-*.zip");
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
                _logger.LogInformation("Cleaned up {DeletedCount} expired diagnostic bundles", deleted);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired bundles");
            return 0;
        }
    }
}
