using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.Ollama;

/// <summary>
/// Represents a tool call request from the Ollama LLM
/// </summary>
public record OllamaToolCall
{
    /// <summary>
    /// Function to be called
    /// </summary>
    [JsonPropertyName("function")]
    public OllamaFunctionCall Function { get; init; } = new();
}

/// <summary>
/// Details of the function call requested by the LLM
/// </summary>
public record OllamaFunctionCall
{
    /// <summary>
    /// Name of the function to call
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Arguments for the function as a JSON string
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Parse arguments as a typed object
    /// </summary>
    public T? ParseArguments<T>() where T : class
    {
        if (string.IsNullOrWhiteSpace(Arguments))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(Arguments, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse arguments as a dictionary
    /// </summary>
    public Dictionary<string, JsonElement>? ParseArgumentsAsDictionary()
    {
        if (string.IsNullOrWhiteSpace(Arguments))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Arguments, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Response message from Ollama that may contain tool calls
/// </summary>
public record OllamaMessageWithToolCalls
{
    /// <summary>
    /// Role of the message (e.g., "assistant")
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }

    /// <summary>
    /// Tool calls requested by the LLM
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCall>? ToolCalls { get; init; }
}
