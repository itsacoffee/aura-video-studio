using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Visual;

/// <summary>
/// Selection state for a scene's visual asset
/// </summary>
public enum SelectionState
{
    Pending,
    Accepted,
    Rejected,
    Replaced
}

/// <summary>
/// Persisted selection for a scene
/// </summary>
public record SceneVisualSelection
{
    /// <summary>
    /// Unique identifier for this selection
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Job ID this selection belongs to
    /// </summary>
    public string JobId { get; init; } = string.Empty;

    /// <summary>
    /// Scene index
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Currently selected candidate
    /// </summary>
    public ImageCandidate? SelectedCandidate { get; init; }

    /// <summary>
    /// All candidates generated for this scene
    /// </summary>
    public IReadOnlyList<ImageCandidate> Candidates { get; init; } = Array.Empty<ImageCandidate>();

    /// <summary>
    /// Selection state
    /// </summary>
    public SelectionState State { get; init; }

    /// <summary>
    /// User's rejection reason if rejected
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Timestamp of selection
    /// </summary>
    public DateTime SelectedAt { get; init; }

    /// <summary>
    /// User who made the selection
    /// </summary>
    public string? SelectedBy { get; init; }

    /// <summary>
    /// Visual prompt used to generate candidates
    /// </summary>
    public VisualPrompt? Prompt { get; init; }

    /// <summary>
    /// Selection metadata and telemetry
    /// </summary>
    public SelectionMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Metadata about the selection process
/// </summary>
public record SelectionMetadata
{
    /// <summary>
    /// Total generation time for all candidates (ms)
    /// </summary>
    public double TotalGenerationTimeMs { get; init; }

    /// <summary>
    /// Number of regeneration attempts
    /// </summary>
    public int RegenerationCount { get; init; }

    /// <summary>
    /// Whether auto-selection was used
    /// </summary>
    public bool AutoSelected { get; init; }

    /// <summary>
    /// Confidence score for auto-selection (0-100)
    /// </summary>
    public double? AutoSelectionConfidence { get; init; }

    /// <summary>
    /// LLM assistance was used for prompt refinement
    /// </summary>
    public bool LlmAssistedRefinement { get; init; }

    /// <summary>
    /// Original prompt before LLM refinement
    /// </summary>
    public string? OriginalPrompt { get; init; }

    /// <summary>
    /// Trace ID for diagnostic correlation
    /// </summary>
    public string? TraceId { get; init; }
}

/// <summary>
/// Request to refine a visual prompt using LLM
/// </summary>
public record PromptRefinementRequest
{
    /// <summary>
    /// Current visual prompt
    /// </summary>
    public required VisualPrompt CurrentPrompt { get; init; }

    /// <summary>
    /// Current candidates and their scores
    /// </summary>
    public IReadOnlyList<ImageCandidate> CurrentCandidates { get; init; } = Array.Empty<ImageCandidate>();

    /// <summary>
    /// Specific issues to address
    /// </summary>
    public IReadOnlyList<string> IssuesDetected { get; init; } = Array.Empty<string>();

    /// <summary>
    /// User's feedback or desired changes
    /// </summary>
    public string? UserFeedback { get; init; }
}

/// <summary>
/// LLM's refined prompt suggestion
/// </summary>
public record PromptRefinementResult
{
    /// <summary>
    /// Refined visual prompt
    /// </summary>
    public required VisualPrompt RefinedPrompt { get; init; }

    /// <summary>
    /// Explanation of changes made
    /// </summary>
    public string Explanation { get; init; } = string.Empty;

    /// <summary>
    /// Specific improvements applied
    /// </summary>
    public IReadOnlyList<string> Improvements { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Expected quality improvement (0-100)
    /// </summary>
    public double ExpectedImprovement { get; init; }

    /// <summary>
    /// Confidence in refinement (0-100)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Auto-selection decision result
/// </summary>
public record AutoSelectionDecision
{
    /// <summary>
    /// Whether auto-selection should proceed
    /// </summary>
    public bool ShouldAutoSelect { get; init; }

    /// <summary>
    /// Selected candidate if auto-selecting
    /// </summary>
    public ImageCandidate? SelectedCandidate { get; init; }

    /// <summary>
    /// Confidence score (0-100)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Reasoning for decision
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Threshold used for decision
    /// </summary>
    public double ThresholdUsed { get; init; }
}
