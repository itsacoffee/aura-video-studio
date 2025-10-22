using System;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Comprehensive voice descriptor with full capability information
/// </summary>
public record VoiceDescriptor
{
    /// <summary>
    /// Unique voice identifier (e.g., Azure: "en-US-JennyNeural", ElevenLabs: voice ID)
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name for the voice
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// TTS provider for this voice
    /// </summary>
    public required VoiceProvider Provider { get; init; }

    /// <summary>
    /// Language-region code (e.g., "en-US", "es-ES")
    /// </summary>
    public required string Locale { get; init; }

    /// <summary>
    /// Voice gender
    /// </summary>
    public VoiceGender Gender { get; init; }

    /// <summary>
    /// Voice type (Neural vs Standard)
    /// </summary>
    public VoiceType VoiceType { get; init; }

    /// <summary>
    /// Available speaking styles specific to this voice (e.g., "cheerful", "sad", "angry")
    /// </summary>
    public string[] AvailableStyles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Available role play options specific to this voice (e.g., "Girl", "Boy", "SeniorMale")
    /// </summary>
    public string[] AvailableRoles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Features supported by this voice
    /// </summary>
    public VoiceFeatures SupportedFeatures { get; init; }

    /// <summary>
    /// User-friendly description of the voice
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Link to sample audio if available
    /// </summary>
    public string? SampleUrl { get; init; }

    /// <summary>
    /// Local name in the voice's native language
    /// </summary>
    public string? LocalName { get; init; }
}

/// <summary>
/// TTS provider enumeration
/// </summary>
public enum VoiceProvider
{
    Azure,
    ElevenLabs,
    PlayHT,
    WindowsSAPI,
    Piper,
    Mimic3,
    Mock
}

/// <summary>
/// Voice gender enumeration
/// </summary>
public enum VoiceGender
{
    Male,
    Female,
    Neutral
}

/// <summary>
/// Voice type (technology used)
/// </summary>
public enum VoiceType
{
    Neural,
    Standard
}

/// <summary>
/// Features supported by a voice (flags)
/// </summary>
[Flags]
public enum VoiceFeatures
{
    None = 0,
    Pitch = 1 << 0,
    Rate = 1 << 1,
    Volume = 1 << 2,
    Emphasis = 1 << 3,
    Breaks = 1 << 4,
    Prosody = 1 << 5,
    AudioEffects = 1 << 6,
    Styles = 1 << 7,
    Roles = 1 << 8,
    Phonemes = 1 << 9,
    SayAs = 1 << 10,
    
    // Common combinations
    Basic = Rate | Pitch | Volume,
    Standard = Basic | Breaks | Emphasis,
    Advanced = Standard | Prosody | AudioEffects,
    Full = Advanced | Styles | Roles | Phonemes | SayAs
}
