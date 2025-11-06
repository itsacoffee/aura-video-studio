namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Request model for validating OpenAI API key with live network check
/// </summary>
public record ValidateOpenAIKeyRequest(
    string ApiKey,
    string? BaseUrl = null,
    string? OrganizationId = null,
    string? ProjectId = null);

/// <summary>
/// Response model for provider validation with detailed status
/// </summary>
public record ProviderValidationResponse(
    bool IsValid,
    string Status,
    string? Message = null,
    string? CorrelationId = null,
    ValidationDetails? Details = null);

/// <summary>
/// Detailed validation information
/// </summary>
public record ValidationDetails(
    string Provider,
    string KeyFormat,
    bool FormatValid,
    bool? NetworkCheckPassed = null,
    int? HttpStatusCode = null,
    string? ErrorType = null,
    long? ResponseTimeMs = null);

/// <summary>
/// Validation status enum
/// </summary>
public enum ValidationStatus
{
    Valid,
    Invalid,
    NetworkError,
    Unauthorized,
    Forbidden,
    Timeout,
    UnknownError
}
