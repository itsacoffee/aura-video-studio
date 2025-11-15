namespace Aura.Core.Errors;

/// <summary>
/// Categorizes errors into high-level groups for consistent handling and documentation
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Network connectivity issues (DNS, TLS, timeouts, unreachable)
    /// </summary>
    Network,

    /// <summary>
    /// Input validation failures
    /// </summary>
    Validation,

    /// <summary>
    /// Provider-specific errors (LLM, TTS, Image generation)
    /// </summary>
    Provider,

    /// <summary>
    /// FFmpeg and video rendering errors
    /// </summary>
    Rendering,

    /// <summary>
    /// Resource-related errors (disk space, memory, permissions)
    /// </summary>
    Resource,

    /// <summary>
    /// Configuration errors (CORS, base URL, missing settings)
    /// </summary>
    Configuration,

    /// <summary>
    /// Authentication and authorization errors
    /// </summary>
    Authentication,

    /// <summary>
    /// Unknown or unhandled errors
    /// </summary>
    Unknown
}

/// <summary>
/// Comprehensive error codes for network and connectivity issues
/// </summary>
public static class NetworkErrorCodes
{
    public const string BackendUnreachable = "NET001_BackendUnreachable";
    public const string DnsResolutionFailed = "NET002_DnsResolutionFailed";
    public const string TlsHandshakeFailed = "NET003_TlsHandshakeFailed";
    public const string NetworkTimeout = "NET004_NetworkTimeout";
    public const string NetworkUnreachable = "NET005_NetworkUnreachable";
    public const string CorsMisconfigured = "NET006_CorsMisconfigured";
    public const string ProviderUnavailable = "NET007_ProviderUnavailable";
    public const string ConnectionRefused = "NET008_ConnectionRefused";
    public const string CertificateValidationFailed = "NET009_CertificateValidationFailed";
    public const string ProxyAuthenticationFailed = "NET010_ProxyAuthenticationFailed";
}

/// <summary>
/// Comprehensive error codes for validation issues
/// </summary>
public static class ValidationErrorCodes
{
    public const string InvalidInput = "VAL001_InvalidInput";
    public const string MissingRequiredField = "VAL002_MissingRequiredField";
    public const string InvalidFormat = "VAL003_InvalidFormat";
    public const string ValueOutOfRange = "VAL004_ValueOutOfRange";
    public const string InvalidApiKey = "VAL005_InvalidApiKey";
    public const string InvalidModel = "VAL006_InvalidModel";
    public const string InvalidConfiguration = "VAL007_InvalidConfiguration";
    public const string InvalidPath = "VAL008_InvalidPath";
    public const string InvalidJsonFormat = "VAL009_InvalidJsonFormat";
}

/// <summary>
/// Error codes for provider authentication issues
/// </summary>
public static class AuthenticationErrorCodes
{
    public const string ApiKeyMissing = "AUTH001_ApiKeyMissing";
    public const string ApiKeyInvalid = "AUTH002_ApiKeyInvalid";
    public const string ApiKeyExpired = "AUTH003_ApiKeyExpired";
    public const string InsufficientPermissions = "AUTH004_InsufficientPermissions";
    public const string QuotaExceeded = "AUTH005_QuotaExceeded";
    public const string RateLimitExceeded = "AUTH006_RateLimitExceeded";
}

/// <summary>
/// Error codes for configuration issues
/// </summary>
public static class ConfigurationErrorCodes
{
    public const string BaseUrlMisconfigured = "CFG001_BaseUrlMisconfigured";
    public const string CorsMisconfigured = "CFG002_CorsMisconfigured";
    public const string EnvironmentMisconfigured = "CFG003_EnvironmentMisconfigured";
    public const string MissingConfiguration = "CFG004_MissingConfiguration";
    public const string InvalidConfiguration = "CFG005_InvalidConfiguration";
}
