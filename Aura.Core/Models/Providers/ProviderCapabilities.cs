using System.Collections.Generic;

namespace Aura.Core.Models.Providers;

/// <summary>
/// Describes the capabilities and metadata of an LLM provider.
/// Used for pre-flight validation and user guidance.
/// </summary>
public class ProviderCapabilities
{
    /// <summary>
    /// Display name of the provider
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this provider supports translation operations
    /// </summary>
    public bool SupportsTranslation { get; set; }

    /// <summary>
    /// Whether this provider supports streaming responses
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Whether this provider runs locally (vs. cloud API)
    /// </summary>
    public bool IsLocalModel { get; set; }

    /// <summary>
    /// Maximum context length supported (0 if not applicable)
    /// </summary>
    public int MaxContextLength { get; set; }

    /// <summary>
    /// Recommended temperature range for this provider
    /// </summary>
    public string RecommendedTemperature { get; set; } = "0.3-0.7";

    /// <summary>
    /// Known limitations of this provider that users should be aware of
    /// </summary>
    public List<string> KnownLimitations { get; set; } = new();
}
