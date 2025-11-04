namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for performance monitoring and telemetry.
/// </summary>
public sealed class PerformanceOptions
{
    /// <summary>
    /// Threshold in milliseconds for identifying slow requests.
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Threshold in milliseconds for identifying very slow requests.
    /// </summary>
    public int VerySlowRequestThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Enables detailed telemetry collection.
    /// </summary>
    public bool EnableDetailedTelemetry { get; set; } = true;

    /// <summary>
    /// Sample rate for telemetry (0.0 to 1.0, where 1.0 is 100%).
    /// </summary>
    public double SampleRate { get; set; } = 1.0;
}
