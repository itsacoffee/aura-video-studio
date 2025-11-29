namespace Aura.Core.Models.Visual;

/// <summary>
/// Represents relevance scoring for an image matched to a scene.
/// </summary>
public record ImageSceneRelevanceScore
{
    /// <summary>
    /// Unique identifier of the image (typically from stock provider).
    /// </summary>
    public string ImageId { get; init; } = string.Empty;

    /// <summary>
    /// Overall relevance score (0-100) for how well the image matches the scene.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Score contribution from keyword matching (0-50).
    /// </summary>
    public double KeywordMatchScore { get; init; }

    /// <summary>
    /// Score contribution from scene heading relevance (0-25).
    /// </summary>
    public double HeadingRelevanceScore { get; init; }

    /// <summary>
    /// Score contribution from style/context alignment (0-25).
    /// </summary>
    public double StyleAlignmentScore { get; init; }

    /// <summary>
    /// Keywords from the scene that matched image metadata.
    /// </summary>
    public IReadOnlyList<string> MatchedKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Human-readable explanation of the scoring.
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Whether this score meets the minimum threshold for selection.
    /// </summary>
    public bool MeetsThreshold { get; init; }

    /// <summary>
    /// Creates a score indicating a failed or irrelevant match.
    /// </summary>
    public static ImageSceneRelevanceScore NoMatch(string imageId) => new()
    {
        ImageId = imageId,
        Score = 0,
        KeywordMatchScore = 0,
        HeadingRelevanceScore = 0,
        StyleAlignmentScore = 0,
        MatchedKeywords = Array.Empty<string>(),
        Reasoning = "No relevant match found",
        MeetsThreshold = false
    };

    /// <summary>
    /// Creates a basic score for fallback scenarios.
    /// </summary>
    public static ImageSceneRelevanceScore BasicMatch(string imageId, double baseScore) => new()
    {
        ImageId = imageId,
        Score = baseScore,
        KeywordMatchScore = baseScore / 2,
        HeadingRelevanceScore = baseScore / 4,
        StyleAlignmentScore = baseScore / 4,
        MatchedKeywords = Array.Empty<string>(),
        Reasoning = "Basic match using fallback scoring",
        MeetsThreshold = baseScore >= 60.0
    };
}
