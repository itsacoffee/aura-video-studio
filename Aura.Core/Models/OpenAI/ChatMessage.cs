using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.OpenAI;

/// <summary>
/// Represents a chat message in the OpenAI API format.
/// Supports text and audio content for multi-modal interactions.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Role of the message sender (system, user, assistant).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Text content of the message (for simple text-only messages).
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// Structured content parts for multi-modal messages (text, audio, etc.).
    /// Use this for messages that include audio content.
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ContentPart>? ContentParts { get; set; }

    /// <summary>
    /// Audio content in the assistant's response (output only).
    /// Contains base64-encoded audio data and metadata.
    /// </summary>
    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioContent? Audio { get; set; }
}

/// <summary>
/// Represents a content part in a multi-modal message.
/// Can be text, audio input, or other media types.
/// </summary>
public class ContentPart
{
    /// <summary>
    /// Type of content (text, input_audio, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content (when type is "text").
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// Audio input data (when type is "input_audio").
    /// </summary>
    [JsonPropertyName("input_audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AudioInput? InputAudio { get; set; }
}

/// <summary>
/// Represents audio input data for prompts.
/// </summary>
public class AudioInput
{
    /// <summary>
    /// Base64-encoded audio data.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Format of the audio data (wav, mp3, etc.).
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";
}

/// <summary>
/// Represents audio content in a response.
/// Contains the generated audio data and metadata.
/// </summary>
public class AudioContent
{
    /// <summary>
    /// Unique identifier for this audio response.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Base64-encoded audio data.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Transcript of the audio content (if available).
    /// </summary>
    [JsonPropertyName("transcript")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Transcript { get; set; }

    /// <summary>
    /// Timestamp when audio generation started (Unix timestamp).
    /// </summary>
    [JsonPropertyName("expires_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ExpiresAt { get; set; }
}
