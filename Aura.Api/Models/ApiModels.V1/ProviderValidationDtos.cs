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
/// Request model for testing OpenAI script generation
/// </summary>
public record TestOpenAIGenerationRequest(
    string ApiKey,
    string Model = "gpt-4o-mini",
    string? BaseUrl = null,
    string? OrganizationId = null,
    string? ProjectId = null);

/// <summary>
/// Request model for validating ElevenLabs API key
/// </summary>
public record ValidateElevenLabsKeyRequest(string ApiKey);

/// <summary>
/// Request model for validating PlayHT API key
/// </summary>
public record ValidatePlayHTKeyRequest(string ApiKey);

/// <summary>
/// Generic provider API key validation request
/// </summary>
public record ValidateProviderKeyRequest(
    string Provider,
    string ApiKey);

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
    long? ResponseTimeMs = null,
    string? DiagnosticInfo = null);

/// <summary>
/// Provider status information for validation
/// </summary>
public record ProviderValidationStatusDto(
    string Name,
    bool IsConfigured,
    bool IsAvailable,
    string Status,
    string? LastValidated = null,
    string? ErrorMessage = null);

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

/// <summary>
/// Enhanced provider validation request with partial config support
/// </summary>
public record EnhancedProviderValidationRequest(
    string Provider,
    Dictionary<string, string?> Configuration,
    bool PartialValidation = false,
    string? CorrelationId = null);

/// <summary>
/// Field-level validation error
/// </summary>
public record FieldValidationError(
    string FieldName,
    string ErrorCode,
    string ErrorMessage,
    string? SuggestedFix = null);

/// <summary>
/// Enhanced provider validation response with field-level errors
/// </summary>
public record EnhancedProviderValidationResponse(
    bool IsValid,
    string Status,
    string Provider,
    List<FieldValidationError>? FieldErrors = null,
    Dictionary<string, bool>? FieldValidationStatus = null,
    string? OverallMessage = null,
    string? CorrelationId = null,
    ValidationDetails? Details = null);

/// <summary>
/// Provider status with per-field validation tracking (enhanced version)
/// </summary>
public record EnhancedProviderStatusDto(
    string Name,
    bool IsConfigured,
    bool IsAvailable,
    string Status,
    Dictionary<string, string>? ConfiguredFields = null,
    Dictionary<string, bool>? FieldValidationStatus = null,
    string? LastValidated = null,
    string? ErrorMessage = null,
    int RetryCount = 0,
    DateTime? LastAttempt = null);

/// <summary>
/// Partial configuration save request
/// </summary>
public record SavePartialConfigurationRequest(
    string Provider,
    Dictionary<string, string?> PartialConfiguration,
    string? CorrelationId = null);
