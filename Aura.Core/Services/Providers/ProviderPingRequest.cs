using System.Collections.Generic;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Optional parameters for provider ping/validation.
/// Secrets such as API keys should never be supplied here (they are loaded from secure storage).
/// </summary>
public record ProviderPingRequest
{
    /// <summary>
    /// Optional model or deployment identifier to validate against.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Optional region or data center hint for region-bound providers.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Optional base endpoint override (e.g., custom Azure endpoint).
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>
    /// Additional provider-specific parameters that are not secrets (e.g., PlayHT user ID).
    /// </summary>
    public Dictionary<string, string?>? Parameters { get; init; }
}

