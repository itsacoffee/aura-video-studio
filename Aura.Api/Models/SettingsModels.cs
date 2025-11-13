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

/// <summary>
/// Request model for getting key status
/// </summary>
public class KeyStatusRequest
{
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// Request model for revalidating API key
/// </summary>
public class RevalidateKeyRequest
{
    public string Provider { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
}

/// <summary>
/// Response model for key validation status
/// </summary>
public class KeyStatusResponse
{
    public bool Success { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime? LastValidated { get; set; }
    public DateTime? ValidationStarted { get; set; }
    public int ElapsedMs { get; set; }
    public int RemainingTimeoutMs { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public bool CanRetry { get; set; }
    public bool CanManuallyRevalidate { get; set; }
}

/// <summary>
/// Response model for all keys validation status
/// </summary>
public class AllKeysStatusResponse
{
    public bool Success { get; set; }
    public Dictionary<string, KeyStatusResponse> Statuses { get; set; } = new();
    public int TotalKeys { get; set; }
    public int ValidKeys { get; set; }
    public int InvalidKeys { get; set; }
    public int PendingValidation { get; set; }
}
