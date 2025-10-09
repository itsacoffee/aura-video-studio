using System.Collections.Generic;

namespace Aura.Core.Configuration;

/// <summary>
/// Interface for secure API key storage and retrieval
/// </summary>
public interface IKeyStore
{
    /// <summary>
    /// Get an API key for the specified provider
    /// </summary>
    string? GetKey(string providerName);

    /// <summary>
    /// Get all stored API keys (for validation purposes)
    /// </summary>
    Dictionary<string, string> GetAllKeys();

    /// <summary>
    /// Check if OfflineOnly mode is enabled
    /// </summary>
    bool IsOfflineOnly();
}
