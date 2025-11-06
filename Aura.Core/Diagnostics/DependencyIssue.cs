using System;
using System.Collections.Generic;

namespace Aura.Core.Diagnostics;

/// <summary>
/// Severity level for a dependency issue
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning that doesn't block functionality
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error that blocks some functionality
    /// </summary>
    Error
}

/// <summary>
/// Category of dependency issue
/// </summary>
public enum IssueCategory
{
    FFmpeg,
    Network,
    Provider,
    Storage,
    System,
    Runtime
}

/// <summary>
/// Represents a dependency or system issue found during scanning
/// </summary>
public class DependencyIssue
{
    /// <summary>
    /// Unique identifier for this issue type
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Category of the issue
    /// </summary>
    public IssueCategory Category { get; set; }
    
    /// <summary>
    /// Severity level
    /// </summary>
    public IssueSeverity Severity { get; set; }
    
    /// <summary>
    /// Human-readable title
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Remediation steps for user
    /// </summary>
    public string Remediation { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional documentation URL
    /// </summary>
    public string? DocsUrl { get; set; }
    
    /// <summary>
    /// Related setting key if applicable
    /// </summary>
    public string? RelatedSettingKey { get; set; }
    
    /// <summary>
    /// Actionable fix identifier (e.g., "install-ffmpeg")
    /// </summary>
    public string? ActionId { get; set; }
    
    /// <summary>
    /// Additional metadata for the issue
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// System information collected during scan
/// </summary>
public class SystemInfo
{
    public string Platform { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public long TotalMemoryMb { get; set; }
    public GpuInfo? Gpu { get; set; }
}

/// <summary>
/// GPU information
/// </summary>
public class GpuInfo
{
    public string Vendor { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int VramMb { get; set; }
    public bool SupportsHardwareAcceleration { get; set; }
}

/// <summary>
/// Result of a dependency scan
/// </summary>
public class DependencyScanResult
{
    /// <summary>
    /// When the scan was performed
    /// </summary>
    public DateTime ScanTime { get; set; }
    
    /// <summary>
    /// Duration of the scan
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// System information
    /// </summary>
    public SystemInfo SystemInfo { get; set; } = new();
    
    /// <summary>
    /// List of issues found
    /// </summary>
    public List<DependencyIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// Whether the scan completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Overall health status
    /// </summary>
    public bool HasErrors => Issues.Exists(i => i.Severity == IssueSeverity.Error);
    
    /// <summary>
    /// Overall health status
    /// </summary>
    public bool HasWarnings => Issues.Exists(i => i.Severity == IssueSeverity.Warning);
    
    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }
}
