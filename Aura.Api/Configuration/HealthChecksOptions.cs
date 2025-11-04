namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for health check thresholds and timeouts.
/// </summary>
public sealed class HealthChecksOptions
{
    /// <summary>
    /// Minimum free disk space in GB before health check reports degraded status.
    /// </summary>
    public double DiskSpaceThresholdGB { get; set; } = 1.0;

    /// <summary>
    /// Critical free disk space in GB before health check reports unhealthy status.
    /// </summary>
    public double DiskSpaceCriticalGB { get; set; } = 0.5;

    /// <summary>
    /// Maximum time allowed for health checks to complete.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
