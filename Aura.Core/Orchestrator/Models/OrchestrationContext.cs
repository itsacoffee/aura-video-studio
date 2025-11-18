using System;
using System.Collections.Generic;
using Aura.Core.Configuration;
using Aura.Core.Models;

namespace Aura.Core.Orchestrator.Models;

/// <summary>
/// Comprehensive context passed to all LLM-driven stages for optimal orchestration.
/// Contains brief, specifications, provider profile, hardware capabilities, and platform targeting.
/// </summary>
public sealed class OrchestrationContext
{
    /// <summary>
    /// The creative brief defining topic, audience, goal, tone
    /// </summary>
    public Brief Brief { get; init; }

    /// <summary>
    /// Planning specifications for pacing, duration, density
    /// </summary>
    public PlanSpec PlanSpec { get; init; }

    /// <summary>
    /// Active provider profile defining which providers are available
    /// </summary>
    public ProviderProfile ActiveProfile { get; init; }

    /// <summary>
    /// System hardware profile for performance-aware decisions
    /// </summary>
    public SystemProfile Hardware { get; init; }

    /// <summary>
    /// Provider settings with configuration details
    /// </summary>
    public ProviderSettings ProviderSettings { get; init; }

    /// <summary>
    /// Target platform (e.g., "YouTube", "TikTok", "LinkedIn", "General")
    /// Influences pacing, format, and content style
    /// </summary>
    public string TargetPlatform { get; init; } = "General";

    /// <summary>
    /// Primary language for content (e.g., "en-US", "es-MX", "ja-JP")
    /// </summary>
    public string PrimaryLanguage { get; init; } = "en-US";

    /// <summary>
    /// Secondary languages for localization hints
    /// </summary>
    public string[] SecondaryLanguages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether to use advanced visual generation (SD, DALL-E, etc.)
    /// </summary>
    public bool UseAdvancedVisuals { get; init; }

    /// <summary>
    /// Whether to use premium TTS providers (ElevenLabs, PlayHT, etc.)
    /// </summary>
    public bool UsePremiumTts { get; init; }

    /// <summary>
    /// Whether cost optimization is a priority
    /// </summary>
    public bool BudgetSensitive { get; init; }

    /// <summary>
    /// Custom tags for additional context (brand style, campaign type, etc.)
    /// </summary>
    public Dictionary<string, string> CustomTags { get; init; } = new();

    /// <summary>
    /// Job identifier for tracking
    /// </summary>
    public string? JobId { get; init; }

    /// <summary>
    /// Correlation identifier for distributed tracing
    /// </summary>
    public string? CorrelationId { get; init; }

    public OrchestrationContext(
        Brief brief,
        PlanSpec planSpec,
        ProviderProfile activeProfile,
        SystemProfile hardware,
        ProviderSettings providerSettings)
    {
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(planSpec);
        ArgumentNullException.ThrowIfNull(activeProfile);
        ArgumentNullException.ThrowIfNull(hardware);
        ArgumentNullException.ThrowIfNull(providerSettings);

        Brief = brief;
        PlanSpec = planSpec;
        ActiveProfile = activeProfile;
        Hardware = hardware;
        ProviderSettings = providerSettings;
    }

    /// <summary>
    /// Creates a summary string for LLM context injection
    /// </summary>
    public string ToContextSummary()
    {
        var summary = $@"Video Generation Context:
- Topic: {Brief.Topic}
- Target Platform: {TargetPlatform}
- Duration: {PlanSpec.TargetDuration.TotalSeconds}s
- Tone: {Brief.Tone}
- Audience: {Brief.Audience ?? "General"}
- Language: {PrimaryLanguage}
- Hardware: {Hardware.Tier} ({Hardware.RamGB}GB RAM, {Hardware.LogicalCores} cores)
- Profile: {ActiveProfile.Name} ({ActiveProfile.Tier})
- Budget Sensitive: {BudgetSensitive}
- Advanced Visuals: {UseAdvancedVisuals}
- Premium TTS: {UsePremiumTts}";

        if (CustomTags.Count > 0)
        {
            summary += "\nCustom Tags:\n";
            foreach (var tag in CustomTags)
            {
                summary += $"  - {tag.Key}: {tag.Value}\n";
            }
        }

        return summary;
    }
}
