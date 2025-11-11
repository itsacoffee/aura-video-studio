using System.Collections.Generic;

namespace Aura.Core.Errors;

/// <summary>
/// Result of validation with detailed error information and suggestions
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Human-readable error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic error handling
    /// </summary>
    public ValidationErrorCode? ErrorCode { get; set; }

    /// <summary>
    /// Helpful suggestions for resolving validation errors
    /// </summary>
    public string[]? Suggestions { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult
        {
            IsValid = true
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failure(string errorMessage, ValidationErrorCode errorCode, string[]? suggestions = null)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Suggestions = suggestions
        };
    }
}

/// <summary>
/// Error codes for validation failures
/// </summary>
public enum ValidationErrorCode
{
    /// <summary>
    /// API key is missing
    /// </summary>
    MissingApiKey,

    /// <summary>
    /// API key has invalid format
    /// </summary>
    InvalidApiKeyFormat,

    /// <summary>
    /// Configuration is invalid
    /// </summary>
    InvalidConfiguration,

    /// <summary>
    /// Required field is missing
    /// </summary>
    MissingRequiredField,

    /// <summary>
    /// Value is out of valid range
    /// </summary>
    ValueOutOfRange
}
