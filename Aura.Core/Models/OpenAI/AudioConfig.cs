using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.OpenAI;

/// <summary>
/// Configuration for OpenAI audio input/output capabilities.
/// Supports GPT-4o and GPT-4o-audio-preview models with audio modalities.
/// </summary>
public class AudioConfig
{
    /// <summary>
    /// Voice to use for audio output generation.
    /// </summary>
    [JsonPropertyName("voice")]
    public AudioVoice Voice { get; set; } = AudioVoice.Alloy;

    /// <summary>
    /// Audio format for the output.
    /// </summary>
    [JsonPropertyName("format")]
    public AudioFormat Format { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Modalities to enable for the request (text and/or audio).
    /// Default includes both text and audio modalities.
    /// </summary>
    [JsonPropertyName("modalities")]
    public List<string> Modalities { get; set; } = new() { "text", "audio" };
}

/// <summary>
/// Available voices for OpenAI audio output.
/// Each voice has distinct characteristics suitable for different use cases.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AudioVoice
{
    /// <summary>
    /// Alloy voice - neutral and balanced tone
    /// </summary>
    [JsonPropertyName("alloy")]
    Alloy,

    /// <summary>
    /// Echo voice - clear and resonant tone
    /// </summary>
    [JsonPropertyName("echo")]
    Echo,

    /// <summary>
    /// Fable voice - expressive and storytelling tone
    /// </summary>
    [JsonPropertyName("fable")]
    Fable,

    /// <summary>
    /// Onyx voice - deep and authoritative tone
    /// </summary>
    [JsonPropertyName("onyx")]
    Onyx,

    /// <summary>
    /// Nova voice - energetic and engaging tone
    /// </summary>
    [JsonPropertyName("nova")]
    Nova,

    /// <summary>
    /// Shimmer voice - warm and friendly tone
    /// </summary>
    [JsonPropertyName("shimmer")]
    Shimmer
}

/// <summary>
/// Supported audio formats for OpenAI audio output.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AudioFormat
{
    /// <summary>
    /// WAV format - uncompressed, high quality (default for video production)
    /// </summary>
    [JsonPropertyName("wav")]
    Wav,

    /// <summary>
    /// MP3 format - compressed, smaller file size
    /// </summary>
    [JsonPropertyName("mp3")]
    Mp3,

    /// <summary>
    /// FLAC format - lossless compression
    /// </summary>
    [JsonPropertyName("flac")]
    Flac,

    /// <summary>
    /// Opus format - optimized for speech
    /// </summary>
    [JsonPropertyName("opus")]
    Opus,

    /// <summary>
    /// PCM16 format - raw 16-bit PCM audio
    /// </summary>
    [JsonPropertyName("pcm16")]
    Pcm16
}
