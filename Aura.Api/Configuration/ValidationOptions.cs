namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for content validation and rate limiting.
/// </summary>
public sealed class ValidationOptions
{
    /// <summary>
    /// Maximum content length in bytes.
    /// </summary>
    public long MaxContentLengthBytes { get; set; } = 10485760; // 10MB

    /// <summary>
    /// Maximum brief length in characters.
    /// </summary>
    public int MaxBriefLength { get; set; } = 10000;

    /// <summary>
    /// Maximum script length in characters.
    /// </summary>
    public int MaxScriptLength { get; set; } = 50000;

    /// <summary>
    /// List of allowed file extensions.
    /// </summary>
    public List<string> AllowedFileExtensions { get; set; } = new()
    {
        ".mp4", ".mp3", ".wav", ".jpg", ".jpeg", ".png",
        ".json", ".srt", ".vtt", ".ass", ".ssa"
    };

    /// <summary>
    /// Rate limit per minute for API requests.
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;

    /// <summary>
    /// Rate limit per hour for API requests.
    /// </summary>
    public int RateLimitPerHour { get; set; } = 1000;
}
