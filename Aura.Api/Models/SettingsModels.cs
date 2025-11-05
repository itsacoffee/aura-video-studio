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
