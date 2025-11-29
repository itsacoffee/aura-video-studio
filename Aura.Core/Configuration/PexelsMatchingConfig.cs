namespace Aura.Core.Configuration;

/// <summary>
/// Configuration for intelligent Pexels scene matching behavior.
/// </summary>
public class PexelsMatchingConfig
{
    /// <summary>
    /// Enable semantic keyword extraction and intelligent query building.
    /// When false, uses basic keyword search.
    /// </summary>
    public bool EnableSemanticMatching { get; set; } = true;

    /// <summary>
    /// Minimum relevance score (0-100) required for an image to be included in results.
    /// Images below this threshold are filtered out.
    /// </summary>
    public double MinimumRelevanceScore { get; set; } = 60.0;

    /// <summary>
    /// Maximum number of candidate images to fetch per scene before scoring.
    /// Higher values may improve selection quality but increase API usage.
    /// </summary>
    public int MaxCandidatesPerScene { get; set; } = 8;

    /// <summary>
    /// Apply Pexels orientation filter based on video aspect ratio.
    /// 16:9 maps to "landscape", 9:16 maps to "portrait".
    /// </summary>
    public bool UseOrientationFiltering { get; set; } = true;

    /// <summary>
    /// Fall back to basic search if semantic matching returns no results.
    /// </summary>
    public bool FallbackToBasicSearch { get; set; } = true;

    /// <summary>
    /// Maximum number of keywords to include in search query.
    /// Limits query complexity for better Pexels API results.
    /// </summary>
    public int MaxKeywordsInQuery { get; set; } = 5;

    /// <summary>
    /// Weight multiplier for visual-related terms in keyword extraction (1.0-2.0).
    /// Higher values prioritize visual terms like "landscape", "modern", "bright".
    /// </summary>
    public double VisualTermBoost { get; set; } = 1.5;

    /// <summary>
    /// Base relevance score assigned to all images (0-100).
    /// Additional scoring factors are applied on top of this.
    /// </summary>
    public double BaseRelevanceScore { get; set; } = 50.0;

    /// <summary>
    /// Creates a default configuration with recommended values.
    /// </summary>
    public static PexelsMatchingConfig Default => new();

    /// <summary>
    /// Creates a minimal configuration with semantic matching disabled.
    /// Used for fallback scenarios or when simpler matching is preferred.
    /// </summary>
    public static PexelsMatchingConfig Minimal => new()
    {
        EnableSemanticMatching = false,
        MaxCandidatesPerScene = 3,
        UseOrientationFiltering = false,
        FallbackToBasicSearch = true
    };
}
