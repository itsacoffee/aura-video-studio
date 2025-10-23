namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Result of video resolution validation
/// </summary>
public record ResolutionValidationResult : QualityValidationResponse
{
    /// <summary>
    /// Detected video width in pixels
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Detected video height in pixels
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Aspect ratio (e.g., "16:9", "4:3")
    /// </summary>
    public string AspectRatio { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if resolution meets minimum requirements
    /// </summary>
    public bool MeetsMinimumResolution { get; init; }

    /// <summary>
    /// Total pixel count
    /// </summary>
    public int TotalPixels { get; init; }

    /// <summary>
    /// Resolution category (e.g., "SD", "HD", "Full HD", "4K")
    /// </summary>
    public string ResolutionCategory { get; init; } = string.Empty;
}
