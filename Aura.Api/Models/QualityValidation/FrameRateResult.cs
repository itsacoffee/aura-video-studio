namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Result of frame rate validation
/// </summary>
public record FrameRateResult : QualityValidationResponse
{
    /// <summary>
    /// Detected average frame rate
    /// </summary>
    public double ActualFPS { get; init; }

    /// <summary>
    /// Expected frame rate
    /// </summary>
    public double ExpectedFPS { get; init; }

    /// <summary>
    /// Frame rate variance/jitter
    /// </summary>
    public double Variance { get; init; }

    /// <summary>
    /// Indicates if frame rate is consistent
    /// </summary>
    public bool IsConsistent { get; init; }

    /// <summary>
    /// Number of dropped frames detected
    /// </summary>
    public int DroppedFrames { get; init; }

    /// <summary>
    /// Total number of frames analyzed
    /// </summary>
    public int TotalFrames { get; init; }

    /// <summary>
    /// Frame rate category (e.g., "24 FPS Cinema", "30 FPS Standard", "60 FPS High")
    /// </summary>
    public string FrameRateCategory { get; init; } = string.Empty;
}
