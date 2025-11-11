using System.Text.Json.Serialization;

namespace Aura.Core.Models.Diagnostics;

/// <summary>
/// Client-side error report submitted from the frontend
/// </summary>
public class ClientErrorReport
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("technicalDetails")]
    public string? TechnicalDetails { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("userAction")]
    public string? UserAction { get; set; }

    [JsonPropertyName("browserInfo")]
    public BrowserInfo? BrowserInfo { get; set; }

    [JsonPropertyName("appState")]
    public Dictionary<string, object>? AppState { get; set; }

    [JsonPropertyName("logs")]
    public List<ClientLogEntry>? Logs { get; set; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    [JsonPropertyName("userDescription")]
    public string? UserDescription { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Browser information from the client
/// </summary>
public class BrowserInfo
{
    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}

/// <summary>
/// Client-side log entry
/// </summary>
public class ClientLogEntry
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("component")]
    public string? Component { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }

    [JsonPropertyName("error")]
    public ClientErrorInfo? Error { get; set; }
}

/// <summary>
/// Client-side error information
/// </summary>
public class ClientErrorInfo
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("stack")]
    public string? Stack { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
