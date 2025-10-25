using System.Collections.Generic;

namespace Aura.Api.Models;

/// <summary>
/// Result of API key validation with detailed error information and suggestions
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the API key is valid and functional
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Human-readable error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code for programmatic error handling (e.g., "INVALID_KEY", "INSUFFICIENT_PERMISSIONS")
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Additional account information retrieved during validation (e.g., username, tier, credits)
    /// </summary>
    public Dictionary<string, object>? AccountInfo { get; set; }

    /// <summary>
    /// Helpful suggestions for resolving validation errors
    /// </summary>
    public List<string>? Suggestions { get; set; }

    /// <summary>
    /// URL to documentation or help resources
    /// </summary>
    public string? HelpUrl { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success(Dictionary<string, object>? accountInfo = null)
    {
        return new ValidationResult
        {
            IsValid = true,
            AccountInfo = accountInfo
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult Failure(string errorMessage, string errorCode, List<string>? suggestions = null, string? helpUrl = null)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Suggestions = suggestions,
            HelpUrl = helpUrl
        };
    }
}
