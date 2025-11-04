using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Visual;

/// <summary>
/// Candidate image with scoring information
/// </summary>
public record ImageCandidate
{
    /// <summary>
    /// URL or path to the image
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// Source provider (e.g., "StableDiffusion", "Pexels", "Pixabay")
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Overall aesthetic score (0-100)
    /// </summary>
    public double AestheticScore { get; init; }

    /// <summary>
    /// Narrative keyword coverage score (0-100)
    /// </summary>
    public double KeywordCoverageScore { get; init; }

    /// <summary>
    /// Technical quality score (0-100)
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// Combined weighted score (0-100)
    /// </summary>
    public double OverallScore { get; init; }

    /// <summary>
    /// Reason for score/selection
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Licensing information
    /// </summary>
    public LicensingInfo? Licensing { get; init; }

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Rejection reasons if not selected
    /// </summary>
    public IReadOnlyList<string> RejectionReasons { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Generation latency in milliseconds
    /// </summary>
    public double GenerationLatencyMs { get; init; }
}

/// <summary>
/// Licensing information for an image
/// </summary>
public record LicensingInfo
{
    /// <summary>
    /// License type (e.g., "CC0", "Pexels License", "Pixabay License", "Commercial")
    /// </summary>
    public string LicenseType { get; init; } = string.Empty;

    /// <summary>
    /// Attribution text if required
    /// </summary>
    public string? Attribution { get; init; }

    /// <summary>
    /// URL to license terms
    /// </summary>
    public string? LicenseUrl { get; init; }

    /// <summary>
    /// Whether commercial use is allowed
    /// </summary>
    public bool CommercialUseAllowed { get; init; }

    /// <summary>
    /// Whether attribution is required
    /// </summary>
    public bool AttributionRequired { get; init; }

    /// <summary>
    /// Creator/photographer name
    /// </summary>
    public string? CreatorName { get; init; }

    /// <summary>
    /// Creator profile URL
    /// </summary>
    public string? CreatorUrl { get; init; }

    /// <summary>
    /// Source platform (e.g., "Pexels", "Unsplash", "StableDiffusion")
    /// </summary>
    public string SourcePlatform { get; init; } = string.Empty;
}

/// <summary>
/// Result of image selection process
/// </summary>
public record ImageSelectionResult
{
    /// <summary>
    /// Scene index this selection is for
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Selected image (winner)
    /// </summary>
    public ImageCandidate? SelectedImage { get; init; }

    /// <summary>
    /// All candidates considered (top-N)
    /// </summary>
    public IReadOnlyList<ImageCandidate> Candidates { get; init; } = Array.Empty<ImageCandidate>();

    /// <summary>
    /// Minimum aesthetic threshold that was applied
    /// </summary>
    public double MinimumAestheticThreshold { get; init; }

    /// <summary>
    /// Narrative keywords used for matching
    /// </summary>
    public IReadOnlyList<string> NarrativeKeywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Total selection time in milliseconds
    /// </summary>
    public double SelectionTimeMs { get; init; }

    /// <summary>
    /// Whether selection met all criteria
    /// </summary>
    public bool MeetsCriteria { get; init; }

    /// <summary>
    /// Warnings or notes about the selection
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Configuration for image selection process
/// </summary>
public record ImageSelectionConfig
{
    /// <summary>
    /// Minimum aesthetic score to consider (0-100)
    /// </summary>
    public double MinimumAestheticThreshold { get; init; } = 60.0;

    /// <summary>
    /// Number of candidates to generate per scene
    /// </summary>
    public int CandidatesPerScene { get; init; } = 5;

    /// <summary>
    /// Weight for aesthetic score in overall ranking (0-1)
    /// </summary>
    public double AestheticWeight { get; init; } = 0.4;

    /// <summary>
    /// Weight for keyword coverage in overall ranking (0-1)
    /// </summary>
    public double KeywordWeight { get; init; } = 0.4;

    /// <summary>
    /// Weight for technical quality in overall ranking (0-1)
    /// </summary>
    public double QualityWeight { get; init; } = 0.2;

    /// <summary>
    /// Prefer local/generated images over stock
    /// </summary>
    public bool PreferGeneratedImages { get; init; } = true;

    /// <summary>
    /// Maximum generation time per image in seconds
    /// </summary>
    public int MaxGenerationTimeSeconds { get; init; } = 30;
}
