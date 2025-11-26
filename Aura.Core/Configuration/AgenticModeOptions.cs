namespace Aura.Core.Configuration;

/// <summary>
/// Configuration options for agentic multi-agent script generation
/// </summary>
public class AgenticModeOptions
{
    /// <summary>
    /// Whether agentic mode is enabled globally
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum number of iterations the orchestrator will attempt
    /// </summary>
    public int MaxIterations { get; set; } = 3;

    /// <summary>
    /// Timeout per iteration in seconds
    /// </summary>
    public int TimeoutPerIterationSeconds { get; set; } = 180;

    /// <summary>
    /// Whether to enable detailed logging of agent interactions
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether to fall back to single-pass generation if agentic mode fails
    /// </summary>
    public bool FallbackToSinglePass { get; set; } = true;
}

