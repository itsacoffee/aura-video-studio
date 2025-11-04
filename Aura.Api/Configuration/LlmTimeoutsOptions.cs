namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for LLM operation timeouts.
/// </summary>
public sealed class LlmTimeoutsOptions
{
    /// <summary>
    /// Timeout in seconds for script generation operations.
    /// </summary>
    public int ScriptGenerationTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Timeout in seconds for script refinement operations.
    /// </summary>
    public int ScriptRefinementTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Timeout in seconds for visual prompt generation.
    /// </summary>
    public int VisualPromptTimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// Timeout in seconds for narration optimization.
    /// </summary>
    public int NarrationOptimizationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout in seconds for pacing analysis.
    /// </summary>
    public int PacingAnalysisTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Timeout in seconds for scene importance evaluation.
    /// </summary>
    public int SceneImportanceTimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// Timeout in seconds for content complexity analysis.
    /// </summary>
    public int ContentComplexityTimeoutSeconds { get; set; } = 45;

    /// <summary>
    /// Timeout in seconds for narrative arc analysis.
    /// </summary>
    public int NarrativeArcTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Warning threshold as percentage of timeout (0.0 to 1.0).
    /// </summary>
    public double WarningThresholdPercentage { get; set; } = 0.5;
}
