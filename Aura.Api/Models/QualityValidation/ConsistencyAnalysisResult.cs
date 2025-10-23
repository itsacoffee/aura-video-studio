namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Result of content consistency analysis across frames
/// </summary>
public record ConsistencyAnalysisResult : QualityValidationResponse
{
    /// <summary>
    /// Overall consistency score (0-100)
    /// </summary>
    public int ConsistencyScore { get; init; }

    /// <summary>
    /// Number of scene changes detected
    /// </summary>
    public int SceneChanges { get; init; }

    /// <summary>
    /// Indicates if there are abrupt transitions
    /// </summary>
    public bool HasAbruptTransitions { get; init; }

    /// <summary>
    /// Color consistency score (0-100)
    /// </summary>
    public int ColorConsistency { get; init; }

    /// <summary>
    /// Brightness consistency score (0-100)
    /// </summary>
    public int BrightnessConsistency { get; init; }

    /// <summary>
    /// Indicates if there are flickering issues
    /// </summary>
    public bool HasFlickering { get; init; }

    /// <summary>
    /// Average motion smoothness (0-100, higher is smoother)
    /// </summary>
    public int MotionSmoothness { get; init; }

    /// <summary>
    /// Detected artifacts or anomalies
    /// </summary>
    public List<string> DetectedArtifacts { get; init; } = new();
}
