using System;

namespace Aura.Core.Models.Download;

/// <summary>
/// Represents a download mirror with health tracking and priority
/// </summary>
public class DownloadMirror
{
    /// <summary>
    /// Unique identifier for the mirror
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the mirror
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mirror URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Priority for mirror selection (lower number = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Current health status of the mirror
    /// </summary>
    public MirrorHealthStatus HealthStatus { get; set; } = MirrorHealthStatus.Unknown;

    /// <summary>
    /// Last time the mirror was checked
    /// </summary>
    public DateTime? LastChecked { get; set; }

    /// <summary>
    /// Last time the mirror was successfully used
    /// </summary>
    public DateTime? LastSuccess { get; set; }

    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double? AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Whether this mirror is currently enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional metadata for the mirror (e.g., region, CDN provider)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Health status of a download mirror
/// </summary>
public enum MirrorHealthStatus
{
    /// <summary>
    /// Health status is unknown (not yet checked)
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Mirror is healthy and responding
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Mirror is experiencing issues but may still work
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Mirror is unavailable or failing
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// Mirror is disabled by configuration
    /// </summary>
    Disabled = 4
}
