using Aura.Core.Models;

namespace Aura.Tests.TestDataBuilders;

/// <summary>
/// Builder for creating test API key configurations
/// </summary>
public class ApiKeyBuilder
{
    private string _provider = "openai";
    private string _key = "sk-test-key-12345";
    private bool _isValid = true;
    private DateTime? _validatedAt;
    private string? _errorMessage;
    private Dictionary<string, object> _metadata = new();

    public ApiKeyBuilder ForProvider(string provider)
    {
        _provider = provider;
        return this;
    }

    public ApiKeyBuilder WithKey(string key)
    {
        _key = key;
        return this;
    }

    public ApiKeyBuilder Valid()
    {
        _isValid = true;
        _validatedAt = DateTime.UtcNow;
        _errorMessage = null;
        return this;
    }

    public ApiKeyBuilder Invalid(string errorMessage = "Invalid API key")
    {
        _isValid = false;
        _errorMessage = errorMessage;
        return this;
    }

    public ApiKeyBuilder WithMetadata(string key, object value)
    {
        _metadata[key] = value;
        return this;
    }

    public ApiKeyConfig Build()
    {
        return new ApiKeyConfig
        {
            Provider = _provider,
            Key = _key,
            IsValid = _isValid,
            ValidatedAt = _validatedAt,
            ErrorMessage = _errorMessage,
            Metadata = _metadata
        };
    }
}

public class ApiKeyConfig
{
    public string Provider { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
