using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Providers;

/// <summary>
/// Abstract base class for validating provider availability at runtime
/// </summary>
public abstract class ProviderValidator
{
    /// <summary>
    /// Gets the name of the provider being validated
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Validates that the provider is available and ready to use
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if provider is available, false otherwise</returns>
    public abstract Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of a provider validation check
/// </summary>
public record ProviderValidationResult
{
    /// <summary>
    /// Whether the provider is available and ready
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Name of the provider
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Details about the validation result
    /// </summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
