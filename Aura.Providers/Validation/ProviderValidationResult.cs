namespace Aura.Providers.Validation;

/// <summary>
/// Result of validating a single provider
/// </summary>
public record ProviderValidationResult
{
    /// <summary>
    /// Name of the provider (e.g., "OpenAI", "Ollama", "StableDiffusion")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether the provider is reachable and working
    /// </summary>
    public bool Ok { get; init; }

    /// <summary>
    /// Details about the validation (success message or error)
    /// </summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// Time taken to validate in milliseconds
    /// </summary>
    public long ElapsedMs { get; init; }
}

/// <summary>
/// Collection of validation results for multiple providers
/// </summary>
public record ValidationResponse
{
    /// <summary>
    /// List of individual provider validation results
    /// </summary>
    public ProviderValidationResult[] Results { get; init; } = Array.Empty<ProviderValidationResult>();

    /// <summary>
    /// Whether all validations were successful
    /// </summary>
    public bool Ok { get; init; }
}
