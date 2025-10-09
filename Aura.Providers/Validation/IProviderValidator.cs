using System.Threading;
using System.Threading.Tasks;

namespace Aura.Providers.Validation;

/// <summary>
/// Interface for validating provider connectivity and functionality
/// </summary>
public interface IProviderValidator
{
    /// <summary>
    /// Name of the provider being validated
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Validate the provider's availability and basic functionality
    /// </summary>
    /// <param name="apiKey">Optional API key for cloud providers</param>
    /// <param name="configUrl">Optional configuration URL for local providers</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with status and details</returns>
    Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct);
}
