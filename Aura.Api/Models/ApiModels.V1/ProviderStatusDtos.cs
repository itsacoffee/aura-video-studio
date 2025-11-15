namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Provider status with validation information
/// </summary>
public record ProviderStatusDto
{
    /// <summary>
    /// Provider name (e.g., "OpenAI", "ElevenLabs", "Piper")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether required configuration is present (API key, base URL, etc.)
    /// </summary>
    public bool Configured { get; init; }

    /// <summary>
    /// Whether provider is reachable and responding (requires actual validation call)
    /// </summary>
    public bool Reachable { get; init; }

    /// <summary>
    /// Error code if validation failed
    /// Examples: ProviderNotConfigured, ProviderKeyInvalid, ProviderNetworkError, etc.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// User-friendly error message (no stack traces)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Concrete remediation steps
    /// </summary>
    public List<string> HowToFix { get; init; } = new();

    /// <summary>
    /// Last time this provider was validated
    /// </summary>
    public DateTime? LastValidated { get; init; }

    /// <summary>
    /// Provider category (LLM, TTS, Images, etc.)
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Provider tier (Free, Premium, etc.)
    /// </summary>
    public string Tier { get; init; } = string.Empty;
}

/// <summary>
/// Request to validate a specific provider
/// </summary>
public record ValidateProviderRequest
{
    /// <summary>
    /// Provider name to validate
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Response from provider validation
/// </summary>
public record ValidateProviderResponse
{
    public ProviderStatusDto Status { get; init; } = new();
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// List of all provider statuses
/// </summary>
public record ProvidersStatusResponse
{
    public List<ProviderStatusDto> Providers { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public int ConfiguredCount { get; init; }
    public int ReachableCount { get; init; }
}
