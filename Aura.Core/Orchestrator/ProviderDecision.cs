using System.Collections.Generic;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Deterministic provider decision DTO that captures the full decision-making context
/// including the selected provider, priority rank, and fallback chain.
/// </summary>
public record ProviderDecision
{
    /// <summary>
    /// The name of the selected provider (e.g., "OpenAI", "Ollama", "RuleBased")
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Priority rank of the selected provider (1 = highest priority, higher numbers = lower priority)
    /// Used to determine which provider was selected in the fallback chain
    /// </summary>
    public int PriorityRank { get; init; }

    /// <summary>
    /// Complete downgrade/fallback chain showing all providers that would be tried in order
    /// First element is the highest priority, last is the guaranteed fallback
    /// </summary>
    public string[] DowngradeChain { get; init; } = System.Array.Empty<string>();

    /// <summary>
    /// Reason for the selection decision
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is a fallback selection (i.e., not the first choice in the chain)
    /// </summary>
    public bool IsFallback { get; init; }

    /// <summary>
    /// Stage for which the provider was selected (e.g., "Script", "TTS", "Visuals")
    /// </summary>
    public string Stage { get; init; } = string.Empty;

    /// <summary>
    /// What the selection fell back from (if applicable)
    /// </summary>
    public string? FallbackFrom { get; init; }
}
