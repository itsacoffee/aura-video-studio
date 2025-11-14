using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request for visual image selection
/// </summary>
public record ImageSelectionRequest
{
    public int SceneIndex { get; init; }
    public string DetailedDescription { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Framing { get; init; } = string.Empty;
    public List<string> NarrativeKeywords { get; init; } = new();
    public string Style { get; init; } = "Cinematic";
    public string QualityTier { get; init; } = "Standard";
    public ImageSelectionConfigDto? Config { get; init; }
}

/// <summary>
/// Configuration for image selection
/// </summary>
public record ImageSelectionConfigDto
{
    public double MinimumAestheticThreshold { get; init; } = 60.0;
    public int CandidatesPerScene { get; init; } = 5;
    public double AestheticWeight { get; init; } = 0.4;
    public double KeywordWeight { get; init; } = 0.4;
    public double QualityWeight { get; init; } = 0.2;
    public bool PreferGeneratedImages { get; init; } = true;
    public int MaxGenerationTimeSeconds { get; init; } = 30;
}

/// <summary>
/// Image candidate response
/// </summary>
public record ImageCandidateDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public double AestheticScore { get; init; }
    public double KeywordCoverageScore { get; init; }
    public double QualityScore { get; init; }
    public double OverallScore { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public LicensingInfoDto? Licensing { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public List<string> RejectionReasons { get; init; } = new();
    public double GenerationLatencyMs { get; init; }
}

/// <summary>
/// Licensing information
/// </summary>
public record LicensingInfoDto
{
    public string LicenseType { get; init; } = string.Empty;
    public string? Attribution { get; init; }
    public string? LicenseUrl { get; init; }
    public bool CommercialUseAllowed { get; init; }
    public bool AttributionRequired { get; init; }
    public string? CreatorName { get; init; }
    public string? CreatorUrl { get; init; }
    public string SourcePlatform { get; init; } = string.Empty;
}

/// <summary>
/// Image selection result
/// </summary>
public record ImageSelectionResultDto
{
    public int SceneIndex { get; init; }
    public ImageCandidateDto? SelectedImage { get; init; }
    public List<ImageCandidateDto> Candidates { get; init; } = new();
    public double MinimumAestheticThreshold { get; init; }
    public List<string> NarrativeKeywords { get; init; } = new();
    public double SelectionTimeMs { get; init; }
    public bool MeetsCriteria { get; init; }
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Batch image selection request for multiple scenes
/// </summary>
public record BatchImageSelectionRequest
{
    public List<ImageSelectionRequest> Scenes { get; init; } = new();
    public ImageSelectionConfigDto? Config { get; init; }
}

/// <summary>
/// Batch image selection response
/// </summary>
public record BatchImageSelectionResponse
{
    public List<ImageSelectionResultDto> Results { get; init; } = new();
    public int TotalScenes { get; init; }
    public int SuccessfulSelections { get; init; }
    public double TotalSelectionTimeMs { get; init; }
}

/// <summary>
/// Request to accept or replace a selected image
/// </summary>
public record ImageAcceptanceRequest
{
    public int SceneIndex { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string Action { get; init; } = "accept";
    public string? Reason { get; init; }
}

/// <summary>
/// Request to get candidates with caching support
/// </summary>
public record GetCandidatesRequest
{
    public int SceneIndex { get; init; }
    public string DetailedDescription { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Framing { get; init; } = string.Empty;
    public List<string> NarrativeKeywords { get; init; } = new();
    public string Style { get; init; } = "Cinematic";
    public string QualityTier { get; init; } = "Standard";
    public ImageSelectionConfigDto? Config { get; init; }
    public bool UseCache { get; init; } = true;
}

/// <summary>
/// Response with candidates and cache info
/// </summary>
public record GetCandidatesResponse
{
    public string RequestId { get; init; } = string.Empty;
    public ImageSelectionResultDto Result { get; init; } = null!;
    public bool FromCache { get; init; }
    public DateTime? CachedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Request to regenerate candidates
/// </summary>
public record RegenerateCandidatesRequest
{
    public string JobId { get; init; } = string.Empty;
    public int SceneIndex { get; init; }
    public GetCandidatesRequest? RefinedPrompt { get; init; }
    public ImageSelectionConfigDto? Config { get; init; }
    public string? UserId { get; init; }
}

/// <summary>
/// Enhanced candidate with coverage details
/// </summary>
public record EnhancedImageCandidateDto : ImageCandidateDto
{
    public List<string> CoveredKeywords { get; init; } = new();
    public List<string> MissingKeywords { get; init; } = new();
    public double KeywordCoveragePercent { get; init; }
    public string ContentDescription { get; init; } = string.Empty;
}

/// <summary>
/// Project threshold configuration
/// </summary>
public record ThresholdConfigDto
{
    public string ProjectId { get; init; } = string.Empty;
    public double MinimumAestheticThreshold { get; init; } = 60.0;
    public double AutoAcceptThreshold { get; init; } = 85.0;
    public bool EnableAutoSelection { get; init; }
}
