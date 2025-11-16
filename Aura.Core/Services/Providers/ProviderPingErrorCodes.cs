namespace Aura.Core.Services.Providers;

/// <summary>
/// Canonical provider ping/validation error codes to keep diagnostics consistent.
/// </summary>
public static class ProviderPingErrorCodes
{
    public const string MissingApiKey = "MissingApiKey";
    public const string InvalidApiKey = "InvalidApiKey";
    public const string RateLimited = "RateLimited";
    public const string ProviderUnavailable = "ProviderUnavailable";
    public const string UnsupportedProvider = "UnsupportedProvider";
    public const string NetworkError = "NetworkError";
    public const string Timeout = "Timeout";
    public const string ConfigurationMissing = "ConfigurationMissing";
    public const string BadRequest = "BadRequest";
    public const string Success = "Success";
    public const string Unknown = "UnknownError";
}

