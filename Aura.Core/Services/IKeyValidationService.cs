using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services;

/// <summary>
/// Service for validating API keys by testing actual provider connections
/// </summary>
public interface IKeyValidationService
{
    /// <summary>
    /// Test an API key by making a real connection to the provider
    /// </summary>
    /// <param name="provider">Provider name (e.g., "openai", "anthropic", "elevenlabs")</param>
    /// <param name="apiKey">The API key to test</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with success status and detailed message</returns>
    Task<KeyValidationResult> TestApiKeyAsync(string provider, string apiKey, CancellationToken ct);
}

/// <summary>
/// Result of API key validation test
/// </summary>
public class KeyValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public Dictionary<string, string> Details { get; set; } = new();
}
