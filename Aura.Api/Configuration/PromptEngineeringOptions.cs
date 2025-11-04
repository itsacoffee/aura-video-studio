namespace Aura.Api.Configuration;

/// <summary>
/// Configuration options for prompt engineering and customization.
/// </summary>
public sealed class PromptEngineeringOptions
{
    /// <summary>
    /// Enables prompt customization features.
    /// </summary>
    public bool EnableCustomization { get; set; } = true;

    /// <summary>
    /// Default prompt version to use.
    /// </summary>
    public string DefaultPromptVersion { get; set; } = "default-v1";

    /// <summary>
    /// Maximum length for custom instructions.
    /// </summary>
    public int MaxCustomInstructionsLength { get; set; } = 5000;

    /// <summary>
    /// Enables chain-of-thought prompting technique.
    /// </summary>
    public bool EnableChainOfThought { get; set; } = true;

    /// <summary>
    /// Enables quality metrics collection.
    /// </summary>
    public bool EnableQualityMetrics { get; set; } = true;

    /// <summary>
    /// List of available prompt versions.
    /// </summary>
    public List<PromptVersion> AvailableVersions { get; set; } = new();
}

/// <summary>
/// Represents a specific prompt version configuration.
/// </summary>
public sealed class PromptVersion
{
    /// <summary>
    /// Version identifier.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the version.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the version's characteristics.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is the default version.
    /// </summary>
    public bool IsDefault { get; set; }
}
