namespace Aura.Api.Models;

/// <summary>
/// First-run status model
/// </summary>
public class FirstRunStatus
{
    public bool HasCompletedFirstRun { get; set; }
    public string? CompletedAt { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// Request model for testing API keys
/// </summary>
public class TestApiKeyRequest
{
    public string Provider { get; set; } = "";
    /// <summary>
    /// Optional: provide key to test without saving. If empty, tests stored key.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// Request model for validating file paths
/// </summary>
public class ValidatePathRequest
{
    public string Path { get; set; } = "";
}

/// <summary>
/// Request model for setting Ollama model
/// </summary>
public class SetOllamaModelRequest
{
    public string Model { get; set; } = "";
}

/// <summary>
/// Request model for setting API keys
/// </summary>
public class SetApiKeyRequest
{
    public string Provider { get; set; } = "";
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Request model for rotating API keys
/// </summary>
public class RotateApiKeyRequest
{
    public string Provider { get; set; } = "";
    public string NewApiKey { get; set; } = "";
    public bool TestBeforeSaving { get; set; } = true;
}

/// <summary>
/// Response model for KeyVault encryption information
/// </summary>
public class KeyVaultEncryptionInfo
{
    public string Platform { get; set; } = "";
    public string Method { get; set; } = "";
    public string Scope { get; set; } = "";
}

/// <summary>
/// Response model for KeyVault storage information
/// </summary>
public class KeyVaultStorageInfo
{
    public string Location { get; set; } = "";
    public bool Encrypted { get; set; }
    public bool FileExists { get; set; }
}

/// <summary>
/// Response model for KeyVault metadata
/// </summary>
public class KeyVaultMetadata
{
    public int ConfiguredKeysCount { get; set; }
    public string? LastModified { get; set; }
}

/// <summary>
/// Response model for KeyVault info endpoint
/// </summary>
public class KeyVaultInfoResponse
{
    public bool Success { get; set; }
    public KeyVaultEncryptionInfo Encryption { get; set; } = new();
    public KeyVaultStorageInfo Storage { get; set; } = new();
    public KeyVaultMetadata Metadata { get; set; } = new();
    public string Status { get; set; } = "healthy";
}

/// <summary>
/// Response model for KeyVault diagnostics endpoint
/// </summary>
public class KeyVaultDiagnosticsResponse
{
    public bool Success { get; set; }
    public bool RedactionCheckPassed { get; set; }
    public List<string> Checks { get; set; } = new();
    public string? Message { get; set; }
}
