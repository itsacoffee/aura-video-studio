using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>
    /// Save API keys securely to storage
    /// </summary>
    Task SaveKeysAsync(Dictionary<string, string> keys, CancellationToken ct = default);

    /// <summary>
    /// Set a single API key
    /// </summary>
    Task SetKeyAsync(string providerName, string apiKey, CancellationToken ct = default);

    /// <summary>
    /// Delete an API key
    /// </summary>
    Task DeleteKeyAsync(string providerName, CancellationToken ct = default);

    /// <summary>
    /// Reload keys from disk
    /// </summary>
    void Reload();
}
