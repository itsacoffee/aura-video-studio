using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Voice selection criteria for intelligent voice matching
/// </summary>
public record VoiceSelectionCriteria
{
    /// <summary>
    /// Preferred voice gender
    /// </summary>
    public VoiceGender? PreferredGender { get; init; }

    /// <summary>
    /// Preferred locale/language (e.g., "en-US", "es-ES")
    /// </summary>
    public string? PreferredLocale { get; init; }

    /// <summary>
    /// Required voice features
    /// </summary>
    public VoiceFeatures RequiredFeatures { get; init; } = VoiceFeatures.Basic;

    /// <summary>
    /// Desired voice characteristics
    /// </summary>
    public VoiceCharacteristics? Characteristics { get; init; }

    /// <summary>
    /// Maximum acceptable latency in milliseconds
    /// </summary>
    public int? MaxLatencyMs { get; init; }

    /// <summary>
    /// Whether offline capability is required
    /// </summary>
    public bool RequireOffline { get; init; }

    /// <summary>
    /// Preferred providers in priority order
    /// </summary>
    public VoiceProvider[] PreferredProviders { get; init; } = Array.Empty<VoiceProvider>();

    /// <summary>
    /// Excluded providers
    /// </summary>
    public VoiceProvider[] ExcludedProviders { get; init; } = Array.Empty<VoiceProvider>();

    /// <summary>
    /// Content type for voice optimization
    /// </summary>
    public ContentType ContentType { get; init; } = ContentType.General;

    /// <summary>
    /// Target audience for voice selection
    /// </summary>
    public AudienceType? TargetAudience { get; init; }

    /// <summary>
    /// Maximum cost per character (for cost-sensitive selection)
    /// </summary>
    public double? MaxCostPerChar { get; init; }
}

/// <summary>
/// Voice characteristics for fine-grained matching
/// </summary>
public record VoiceCharacteristics
{
    /// <summary>
    /// Age range of voice
    /// </summary>
    public AgeRange? Age { get; init; }

    /// <summary>
    /// Voice tone/mood
    /// </summary>
    public VoiceTone? Tone { get; init; }

    /// <summary>
    /// Accent/dialect preference
    /// </summary>
    public string? Accent { get; init; }

    /// <summary>
    /// Voice pace (words per minute)
    /// </summary>
    public int? PaceWpm { get; init; }

    /// <summary>
    /// Voice pitch range
    /// </summary>
    public PitchRange? Pitch { get; init; }

    /// <summary>
    /// Whether the voice should sound professional/formal
    /// </summary>
    public bool? Professional { get; init; }

    /// <summary>
    /// Whether the voice should be expressive/emotional
    /// </summary>
    public bool? Expressive { get; init; }
}

/// <summary>
/// Voice selection result with scoring
/// </summary>
public record VoiceSelectionResult
{
    /// <summary>
    /// Selected voice descriptor
    /// </summary>
    public required VoiceDescriptor Voice { get; init; }

    /// <summary>
    /// Provider for this voice
    /// </summary>
    public required VoiceProvider Provider { get; init; }

    /// <summary>
    /// Match score (0.0 to 1.0, higher is better)
    /// </summary>
    public required double MatchScore { get; init; }

    /// <summary>
    /// Why this voice was selected
    /// </summary>
    public required string SelectionReason { get; init; }

    /// <summary>
    /// Estimated cost per character (if applicable)
    /// </summary>
    public double? CostPerChar { get; init; }

    /// <summary>
    /// Estimated latency in milliseconds
    /// </summary>
    public int? EstimatedLatencyMs { get; init; }

    /// <summary>
    /// Alternative voices that were considered
    /// </summary>
    public VoiceAlternative[] Alternatives { get; init; } = Array.Empty<VoiceAlternative>();

    /// <summary>
    /// Selection confidence (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Alternative voice option
/// </summary>
public record VoiceAlternative
{
    public required VoiceDescriptor Voice { get; init; }
    public required double MatchScore { get; init; }
    public required string Reason { get; init; }
}

/// <summary>
/// Content type for voice optimization
/// </summary>
public enum ContentType
{
    General,
    Educational,
    News,
    Entertainment,
    Commercial,
    Narration,
    Conversation,
    CustomerService,
    Audiobook,
    Podcast
}

/// <summary>
/// Target audience type
/// </summary>
public enum AudienceType
{
    Children,
    Teens,
    YoungAdults,
    Adults,
    Seniors,
    Professional,
    Casual,
    Technical
}

/// <summary>
/// Age range for voice
/// </summary>
public enum AgeRange
{
    Child,      // 5-12
    Teen,       // 13-19
    YoungAdult, // 20-35
    MiddleAged, // 36-55
    Senior      // 56+
}

/// <summary>
/// Voice tone/mood
/// </summary>
public enum VoiceTone
{
    Neutral,
    Friendly,
    Professional,
    Authoritative,
    Casual,
    Warm,
    Energetic,
    Calm,
    Serious,
    Playful,
    Empathetic,
    Confident
}

/// <summary>
/// Pitch range
/// </summary>
public enum PitchRange
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Voice catalog for browsing available voices
/// </summary>
public record VoiceCatalog
{
    /// <summary>
    /// All available voices
    /// </summary>
    public required VoiceDescriptor[] Voices { get; init; }

    /// <summary>
    /// Voices grouped by provider
    /// </summary>
    public Dictionary<VoiceProvider, VoiceDescriptor[]> VoicesByProvider { get; init; } = new();

    /// <summary>
    /// Voices grouped by locale
    /// </summary>
    public Dictionary<string, VoiceDescriptor[]> VoicesByLocale { get; init; } = new();

    /// <summary>
    /// Voices grouped by gender
    /// </summary>
    public Dictionary<VoiceGender, VoiceDescriptor[]> VoicesByGender { get; init; } = new();

    /// <summary>
    /// Featured/recommended voices
    /// </summary>
    public VoiceDescriptor[] FeaturedVoices { get; init; } = Array.Empty<VoiceDescriptor>();

    /// <summary>
    /// Total number of voices
    /// </summary>
    public int TotalCount => Voices.Length;

    /// <summary>
    /// When the catalog was last updated
    /// </summary>
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Filters voices by criteria
    /// </summary>
    public VoiceDescriptor[] Filter(VoiceSelectionCriteria criteria)
    {
        var filtered = Voices.AsEnumerable();

        if (criteria.PreferredGender.HasValue)
        {
            filtered = filtered.Where(v => v.Gender == criteria.PreferredGender.Value);
        }

        if (!string.IsNullOrEmpty(criteria.PreferredLocale))
        {
            filtered = filtered.Where(v => v.Locale.StartsWith(criteria.PreferredLocale, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.RequireOffline)
        {
            filtered = filtered.Where(v => v.Provider is VoiceProvider.Piper or VoiceProvider.Mimic3 or VoiceProvider.WindowsSAPI);
        }

        if (criteria.PreferredProviders.Length > 0)
        {
            filtered = filtered.Where(v => criteria.PreferredProviders.Contains(v.Provider));
        }

        if (criteria.ExcludedProviders.Length > 0)
        {
            filtered = filtered.Where(v => !criteria.ExcludedProviders.Contains(v.Provider));
        }

        if (criteria.RequiredFeatures != VoiceFeatures.None)
        {
            filtered = filtered.Where(v => (v.SupportedFeatures & criteria.RequiredFeatures) == criteria.RequiredFeatures);
        }

        return filtered.ToArray();
    }
}
