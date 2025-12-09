using System.Collections.Generic;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Individual cost breakdown item for a generation estimate
/// </summary>
public record CostBreakdownItem
{
    /// <summary>
    /// Stage or provider name (e.g., "Script Generation", "ElevenLabs TTS")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Provider name (e.g., "OpenAI", "ElevenLabs")
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Cost for this item
    /// </summary>
    public required decimal Cost { get; init; }

    /// <summary>
    /// Whether this provider is free
    /// </summary>
    public required bool IsFree { get; init; }

    /// <summary>
    /// Number of units (tokens, characters, images)
    /// </summary>
    public required int Units { get; init; }

    /// <summary>
    /// Unit type (e.g., "tokens", "characters", "images")
    /// </summary>
    public required string UnitType { get; init; }
}

/// <summary>
/// Confidence level of the cost estimate
/// </summary>
public enum CostEstimateConfidence
{
    /// <summary>
    /// High confidence - based on actual pricing and accurate estimates
    /// </summary>
    High,

    /// <summary>
    /// Medium confidence - some assumptions made
    /// </summary>
    Medium,

    /// <summary>
    /// Low confidence - significant assumptions or fallback pricing used
    /// </summary>
    Low
}

/// <summary>
/// Complete cost estimate for a video generation
/// </summary>
public record GenerationCostEstimate
{
    /// <summary>
    /// Estimated LLM/script generation cost
    /// </summary>
    public required decimal LlmCost { get; init; }

    /// <summary>
    /// Estimated TTS cost
    /// </summary>
    public required decimal TtsCost { get; init; }

    /// <summary>
    /// Estimated image generation cost
    /// </summary>
    public required decimal ImageCost { get; init; }

    /// <summary>
    /// Total estimated cost
    /// </summary>
    public required decimal TotalCost { get; init; }

    /// <summary>
    /// Currency code (e.g., "USD")
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Detailed cost breakdown by stage/provider
    /// </summary>
    public required List<CostBreakdownItem> Breakdown { get; init; }

    /// <summary>
    /// Whether all providers used are free (local/offline)
    /// </summary>
    public required bool IsFreeGeneration { get; init; }

    /// <summary>
    /// Confidence level of the estimate
    /// </summary>
    public required CostEstimateConfidence Confidence { get; init; }

    /// <summary>
    /// Budget check result if budget tracking is enabled
    /// </summary>
    public BudgetCheckResult? BudgetCheck { get; init; }
}

/// <summary>
/// Request to estimate generation cost
/// </summary>
public record GenerationCostEstimateRequest
{
    /// <summary>
    /// Estimated script length in characters
    /// </summary>
    public required int EstimatedScriptLength { get; init; }

    /// <summary>
    /// Number of scenes to generate
    /// </summary>
    public required int SceneCount { get; init; }

    /// <summary>
    /// LLM provider name (e.g., "OpenAI", "Anthropic", "Ollama")
    /// </summary>
    public required string LlmProvider { get; init; }

    /// <summary>
    /// LLM model name (e.g., "gpt-4o-mini", "claude-3-haiku")
    /// </summary>
    public required string LlmModel { get; init; }

    /// <summary>
    /// TTS provider name (e.g., "ElevenLabs", "Piper", "Windows")
    /// </summary>
    public required string TtsProvider { get; init; }

    /// <summary>
    /// Image provider name (e.g., "StableDiffusion", "Pexels", "Placeholder")
    /// </summary>
    public string? ImageProvider { get; init; }
}
