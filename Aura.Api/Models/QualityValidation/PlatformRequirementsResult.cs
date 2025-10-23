namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Result of platform-specific requirements validation
/// </summary>
public record PlatformRequirementsResult : QualityValidationResponse
{
    /// <summary>
    /// Target platform name (e.g., "YouTube", "TikTok", "Instagram")
    /// </summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if video meets platform resolution requirements
    /// </summary>
    public bool MeetsResolutionRequirements { get; init; }

    /// <summary>
    /// Indicates if video meets platform aspect ratio requirements
    /// </summary>
    public bool MeetsAspectRatioRequirements { get; init; }

    /// <summary>
    /// Indicates if video meets platform duration limits
    /// </summary>
    public bool MeetsDurationRequirements { get; init; }

    /// <summary>
    /// Indicates if video meets platform file size limits
    /// </summary>
    public bool MeetsFileSizeRequirements { get; init; }

    /// <summary>
    /// Indicates if video meets platform codec requirements
    /// </summary>
    public bool MeetsCodecRequirements { get; init; }

    /// <summary>
    /// Video file size in bytes
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Video duration in seconds
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Video codec detected
    /// </summary>
    public string Codec { get; init; } = string.Empty;

    /// <summary>
    /// Recommended optimizations for the platform
    /// </summary>
    public List<string> RecommendedOptimizations { get; init; } = new();
}
