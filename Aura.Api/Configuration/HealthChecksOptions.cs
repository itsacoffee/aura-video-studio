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
    /// Memory usage warning threshold in MB.
    /// </summary>
    public double MemoryWarningThresholdMB { get; set; } = 1024.0;

    /// <summary>
    /// Memory usage critical threshold in MB.
    /// </summary>
    public double MemoryCriticalThresholdMB { get; set; } = 2048.0;

    /// <summary>
    /// Database response time warning threshold in milliseconds.
    /// </summary>
    public int DatabaseWarningThresholdMs { get; set; } = 500;

    /// <summary>
    /// Database response time critical threshold in milliseconds.
    /// </summary>
    public int DatabaseCriticalThresholdMs { get; set; } = 2000;

    /// <summary>
    /// Maximum time allowed for health checks to complete.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Enable detailed health check logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Enable automatic recovery attempts on unhealthy status.
    /// </summary>
    public bool EnableAutoRecovery { get; set; } = true;
}
