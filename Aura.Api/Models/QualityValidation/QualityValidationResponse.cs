namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Base response model for quality validation endpoints
/// </summary>
public record QualityValidationResponse
{
    /// <summary>
    /// Indicates whether the validation passed
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Overall quality score (0-100)
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// List of validation issues found
    /// </summary>
    public List<string> Issues { get; init; } = new();

    /// <summary>
    /// List of warnings (non-critical issues)
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}
