using System.Text.Json.Serialization;

namespace Aura.Core.Models.Ollama;

/// <summary>
/// Defines a tool (function) that can be called by the Ollama LLM
/// </summary>
public record OllamaToolDefinition
{
    /// <summary>
    /// Type of the tool (always "function" for function calling)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    /// <summary>
    /// Function definition including name, description, and parameters
    /// </summary>
    [JsonPropertyName("function")]
    public OllamaFunctionDefinition Function { get; init; } = new();
}

/// <summary>
/// Defines a function that can be called by the LLM
/// </summary>
public record OllamaFunctionDefinition
{
    /// <summary>
    /// Name of the function (e.g., "get_research_data")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the function does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// JSON schema defining the function parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    public OllamaFunctionParameters Parameters { get; init; } = new();
}

/// <summary>
/// JSON schema for function parameters
/// </summary>
public record OllamaFunctionParameters
{
    /// <summary>
    /// Type of the parameters object (always "object")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "object";

    /// <summary>
    /// Dictionary of property definitions
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, OllamaPropertyDefinition> Properties { get; init; } = new();

    /// <summary>
    /// List of required property names
    /// </summary>
    [JsonPropertyName("required")]
    public List<string> Required { get; init; } = new();
}

/// <summary>
/// Defines a single property in the function parameters
/// </summary>
public record OllamaPropertyDefinition
{
    /// <summary>
    /// Type of the property (e.g., "string", "number", "boolean")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Description of the property
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Enum values if the property is an enum
    /// </summary>
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; init; }
}
