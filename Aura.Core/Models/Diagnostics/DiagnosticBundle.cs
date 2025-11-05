using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Diagnostics;

/// <summary>
/// Comprehensive diagnostic bundle for job failures
/// </summary>
public record DiagnosticBundle
{
    /// <summary>
    /// Unique identifier for this bundle
    /// </summary>
    public string BundleId { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Job ID this bundle is for
    /// </summary>
    public required string JobId { get; init; }
    
    /// <summary>
    /// When the bundle was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the bundle expires (default 1 hour)
    /// </summary>
    public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddHours(1);
    
    /// <summary>
    /// Path to the generated ZIP file
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
    
    /// <summary>
    /// File name of the bundle
    /// </summary>
    public string FileName { get; init; } = string.Empty;
    
    /// <summary>
    /// Size of the bundle in bytes
    /// </summary>
    public long SizeBytes { get; init; }
    
    /// <summary>
    /// Manifest describing bundle contents
    /// </summary>
    public BundleManifest? Manifest { get; init; }
}

/// <summary>
/// Manifest describing the contents of a diagnostic bundle
/// </summary>
public record BundleManifest
{
    /// <summary>
    /// Schema version for compatibility
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";
    
    /// <summary>
    /// Job information
    /// </summary>
    public required JobInfo Job { get; init; }
    
    /// <summary>
    /// System profile at time of failure
    /// </summary>
    public SystemProfile? SystemProfile { get; init; }
    
    /// <summary>
    /// Timeline of job execution with durations
    /// </summary>
    public required List<TimelineEntry> Timeline { get; init; }
    
    /// <summary>
    /// Model decisions made during execution
    /// </summary>
    public List<ModelDecision> ModelDecisions { get; init; } = new();
    
    /// <summary>
    /// FFmpeg commands executed
    /// </summary>
    public List<FFmpegCommand> FFmpegCommands { get; init; } = new();
    
    /// <summary>
    /// Export manifests
    /// </summary>
    public List<ExportManifest> ExportManifests { get; init; } = new();
    
    /// <summary>
    /// Cost report if available
    /// </summary>
    public CostReportSummary? CostReport { get; init; }
    
    /// <summary>
    /// Anonymized log entries
    /// </summary>
    public List<LogEntry> Logs { get; init; } = new();
    
    /// <summary>
    /// Files included in the bundle
    /// </summary>
    public List<BundleFile> Files { get; init; } = new();
}

/// <summary>
/// Job information for the bundle
/// </summary>
public record JobInfo
{
    public required string JobId { get; init; }
    public required string Status { get; init; }
    public required string Stage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public string? CorrelationId { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Timeline entry showing stage execution
/// </summary>
public record TimelineEntry
{
    public required string Stage { get; init; }
    public required string CorrelationId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double DurationSeconds => CompletedAt.HasValue 
        ? (CompletedAt.Value - StartedAt).TotalSeconds 
        : 0;
    public string Status { get; init; } = "Unknown";
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Model decision made during execution
/// </summary>
public record ModelDecision
{
    public DateTime Timestamp { get; init; }
    public required string DecisionPoint { get; init; }
    public required string SelectedModel { get; init; }
    public required string Provider { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Dictionary<string, object> Criteria { get; init; } = new();
    public string? CorrelationId { get; init; }
}

/// <summary>
/// FFmpeg command executed
/// </summary>
public record FFmpegCommand
{
    public DateTime Timestamp { get; init; }
    public required string Command { get; init; }
    public int ExitCode { get; init; }
    public double DurationSeconds { get; init; }
    public string? ErrorOutput { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Export manifest information
/// </summary>
public record ExportManifest
{
    public DateTime Timestamp { get; init; }
    public required string OutputPath { get; init; }
    public required string Format { get; init; }
    public long SizeBytes { get; init; }
    public string? Resolution { get; init; }
    public int? FrameRate { get; init; }
    public string? Codec { get; init; }
}

/// <summary>
/// Cost report summary for the bundle
/// </summary>
public record CostReportSummary
{
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = "USD";
    public Dictionary<string, decimal> CostByStage { get; init; } = new();
    public Dictionary<string, decimal> CostByProvider { get; init; } = new();
    public int TotalTokens { get; init; }
    public bool WithinBudget { get; init; } = true;
}

/// <summary>
/// Log entry with redacted sensitive data
/// </summary>
public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
    public string? CorrelationId { get; init; }
    public string? Exception { get; init; }
}

/// <summary>
/// File included in the bundle
/// </summary>
public record BundleFile
{
    public required string FileName { get; init; }
    public required string Description { get; init; }
    public long SizeBytes { get; init; }
}

/// <summary>
/// System profile information
/// </summary>
public record SystemProfile
{
    public string MachineName { get; init; } = string.Empty;
    public string OSVersion { get; init; } = string.Empty;
    public int ProcessorCount { get; init; }
    public string DotNetVersion { get; init; } = string.Empty;
    public int WorkingSetBytes { get; init; }
    public HardwareInfo? Hardware { get; init; }
}

/// <summary>
/// Hardware information
/// </summary>
public record HardwareInfo
{
    public int LogicalCores { get; init; }
    public int PhysicalCores { get; init; }
    public int RamGB { get; init; }
    public string? GpuVendor { get; init; }
    public string? GpuModel { get; init; }
    public string Tier { get; init; } = string.Empty;
}
