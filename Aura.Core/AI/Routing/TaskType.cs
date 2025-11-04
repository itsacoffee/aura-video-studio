using System;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Task taxonomy for LLM routing decisions. Different tasks benefit from different models.
/// </summary>
public enum TaskType
{
    /// <summary>
    /// High-level content planning and outline generation.
    /// Requires strong reasoning and structure, moderate context length.
    /// </summary>
    Planning,

    /// <summary>
    /// Script rewriting, refinement, and content polish.
    /// Requires creative capability and language quality.
    /// </summary>
    Rewriting,

    /// <summary>
    /// SSML markup generation for text-to-speech.
    /// Requires precision and format adherence.
    /// </summary>
    SsmlGeneration,

    /// <summary>
    /// Visual prompt generation for image/video generation.
    /// Requires creative description and compositional understanding.
    /// </summary>
    PromptGeneration,

    /// <summary>
    /// Content safety critique and filtering.
    /// Requires policy understanding and classification accuracy.
    /// </summary>
    SafetyCritique,

    /// <summary>
    /// Scene analysis for pacing and importance.
    /// Requires cognitive science understanding.
    /// </summary>
    SceneAnalysis,

    /// <summary>
    /// Narrative coherence and arc validation.
    /// Requires story structure understanding.
    /// </summary>
    NarrativeAnalysis,

    /// <summary>
    /// General-purpose completion tasks.
    /// Default fallback for unspecified tasks.
    /// </summary>
    General
}

/// <summary>
/// Routing constraints for task execution.
/// </summary>
public record RoutingConstraints(
    int RequiredContextLength = 4096,
    int MaxLatencyMs = 30000,
    decimal MaxCostPerRequest = 0.10m,
    bool RequireDeterminism = false,
    double MinQualityScore = 0.7);

/// <summary>
/// Result of a routing decision.
/// </summary>
public record RoutingDecision(
    string ProviderName,
    string ModelName,
    string Reasoning,
    DateTime DecisionTime,
    RoutingMetadata Metadata);

/// <summary>
/// Metadata about the routing decision.
/// </summary>
public record RoutingMetadata(
    int Rank,
    double HealthScore,
    double LatencyScore,
    double CostScore,
    double QualityScore,
    double OverallScore,
    string[] AlternativeProviders);
