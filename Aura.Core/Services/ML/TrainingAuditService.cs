using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ML;

/// <summary>
/// Service for auditing and tracking ML training runs
/// </summary>
public class TrainingAuditService
{
    private readonly ILogger<TrainingAuditService> _logger;
    private readonly string _auditLogDirectory;
    private readonly string _auditLogPath;

    public TrainingAuditService(
        ILogger<TrainingAuditService> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        var auraDataDir = providerSettings.GetAuraDataDirectory();
        _auditLogDirectory = Path.Combine(auraDataDir, "ML", "AuditLogs");
        _auditLogPath = Path.Combine(_auditLogDirectory, "training-audit.jsonl");
    }

    /// <summary>
    /// Record a training run in the audit log
    /// </summary>
    public async Task RecordTrainingRunAsync(
        TrainingAuditRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_auditLogDirectory);

            var json = JsonSerializer.Serialize(record);
            
            await using var writer = new StreamWriter(_auditLogPath, append: true);
            await writer.WriteLineAsync(json).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Recorded training run {JobId} in audit log: Status={Status}, Samples={Samples}",
                record.JobId, record.Status, record.AnnotationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record training run {JobId} in audit log", record.JobId);
        }
    }

    /// <summary>
    /// Get training history from audit log
    /// </summary>
    public async Task<List<TrainingAuditRecord>> GetTrainingHistoryAsync(
        int maxRecords = 50,
        CancellationToken cancellationToken = default)
    {
        var records = new List<TrainingAuditRecord>();

        try
        {
            if (!File.Exists(_auditLogPath))
            {
                _logger.LogDebug("Audit log file not found, returning empty history");
                return records;
            }

            var lines = await File.ReadAllLinesAsync(_auditLogPath, cancellationToken).ConfigureAwait(false);
            var reversedLines = lines.AsEnumerable().Reverse().Take(maxRecords);
            
            foreach (var line in reversedLines)
            {
                try
                {
                    var record = JsonSerializer.Deserialize<TrainingAuditRecord>(line);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse audit log line: {Line}", line);
                }
            }

            _logger.LogDebug("Retrieved {Count} training history records", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve training history");
        }

        return records;
    }

    /// <summary>
    /// Get training statistics from audit log
    /// </summary>
    public async Task<TrainingStatistics> GetTrainingStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = new TrainingStatistics();

        try
        {
            var history = await GetTrainingHistoryAsync(1000, cancellationToken).ConfigureAwait(false);

            stats.TotalTrainingRuns = history.Count;
            stats.SuccessfulRuns = history.Count(r => r.Status == "Completed");
            stats.FailedRuns = history.Count(r => r.Status == "Failed");
            stats.CancelledRuns = history.Count(r => r.Status == "Cancelled");

            if (history.Count != 0)
            {
                stats.OldestRun = history.Last().StartedAt;
                stats.NewestRun = history.First().StartedAt;
            }

            var completedRuns = history.Where(r => r.Status == "Completed" && r.DurationMinutes > 0).ToList();
            if (completedRuns.Count != 0)
            {
                stats.AverageTrainingTimeMinutes = completedRuns.Average(r => r.DurationMinutes);
                stats.TotalTrainingTimeMinutes = completedRuns.Sum(r => r.DurationMinutes);
            }

            _logger.LogDebug("Computed training statistics: Total={Total}, Success={Success}, Failed={Failed}",
                stats.TotalTrainingRuns, stats.SuccessfulRuns, stats.FailedRuns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute training statistics");
        }

        return stats;
    }

    /// <summary>
    /// Clear audit log (for testing or maintenance)
    /// </summary>
    public Task ClearAuditLogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(_auditLogPath))
            {
                File.Delete(_auditLogPath);
                _logger.LogInformation("Cleared audit log");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear audit log");
            throw;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Record of a single training run for audit trail
/// </summary>
public record TrainingAuditRecord(
    string JobId,
    string UserId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    int AnnotationCount,
    string? ModelName,
    string? ModelPath,
    double? Loss,
    int? Epochs,
    double DurationMinutes,
    string? ErrorMessage,
    string? Notes,
    SystemInfo SystemInfo);

/// <summary>
/// System information at time of training
/// </summary>
public record SystemInfo(
    bool HasGpu,
    string? GpuName,
    double TotalRamGb,
    string? OsVersion);

/// <summary>
/// Statistics about training runs
/// </summary>
public class TrainingStatistics
{
    public int TotalTrainingRuns { get; set; }
    public int SuccessfulRuns { get; set; }
    public int FailedRuns { get; set; }
    public int CancelledRuns { get; set; }
    public DateTime? OldestRun { get; set; }
    public DateTime? NewestRun { get; set; }
    public double AverageTrainingTimeMinutes { get; set; }
    public double TotalTrainingTimeMinutes { get; set; }
}
