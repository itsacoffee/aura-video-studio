using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Azure-specific TTS options with full SSML feature support
/// </summary>
public class AzureTtsOptions
{
    /// <summary>
    /// Speaking rate: -1.0 (0%, very slow) to 2.0 (300%, very fast). Default: 0.0 (100%)
    /// Maps to SSML prosody rate attribute
    /// </summary>
    public double Rate { get; set; }

    /// <summary>
    /// Voice pitch: -0.5 (-50%, very low) to 0.5 (+50%, very high). Default: 0.0 (100%)
    /// Maps to SSML prosody pitch attribute
    /// </summary>
    public double Pitch { get; set; }

    /// <summary>
    /// Volume level: 0.0 (0%, silent) to 2.0 (200%, very loud). Default: 1.0 (100%)
    /// Maps to SSML prosody volume attribute
    /// </summary>
    public double Volume { get; set; } = 1.0;

    /// <summary>
    /// Speaking style from the voice's available styles (e.g., "cheerful", "sad", "angry")
    /// Must be one of the styles in VoiceDescriptor.AvailableStyles
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Style degree: 0.01 (subtle) to 2.0 (exaggerated). Default: 1.0 (normal)
    /// Controls how much the style affects the speech
    /// </summary>
    public double StyleDegree { get; set; } = 1.0;

    /// <summary>
    /// Role play option from the voice's available roles (e.g., "Girl", "Boy", "SeniorMale")
    /// Must be one of the roles in VoiceDescriptor.AvailableRoles
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Audio effect to apply to the synthesized speech
    /// </summary>
    public AzureAudioEffect AudioEffect { get; set; } = AzureAudioEffect.None;

    /// <summary>
    /// Default emphasis level for text
    /// </summary>
    public EmphasisLevel Emphasis { get; set; } = EmphasisLevel.None;

    /// <summary>
    /// Custom breaks (pauses) to insert at specific positions in the text
    /// </summary>
    public List<BreakPoint>? CustomBreaks { get; set; }

    /// <summary>
    /// Prosody contour for custom pitch patterns (SSML contour format)
    /// Example: "(0%,+20Hz) (10%,+30Hz) (40%,+10Hz)"
    /// </summary>
    public string? ProsodyContour { get; set; }

    /// <summary>
    /// Phoneme specifications for specific words (word -> phoneme mapping)
    /// Used for custom pronunciation
    /// </summary>
    public Dictionary<string, string>? Phonemes { get; set; }

    /// <summary>
    /// Say-as interpretations for specific text segments
    /// </summary>
    public Dictionary<string, SayAsInterpretation>? SayAsHints { get; set; }
}

/// <summary>
/// Audio effects available in Azure TTS
/// </summary>
public enum AzureAudioEffect
{
    None,
    EqTelecom,
    EqCar,
    Reverb
}

/// <summary>
/// Emphasis levels for text
/// </summary>
public enum EmphasisLevel
{
    None,
    Reduced,
    Moderate,
    Strong
}

/// <summary>
/// Break (pause) point in the text
/// </summary>
public class BreakPoint
{
    /// <summary>
    /// Position in the text (character index) where the break should occur
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Duration of the break in milliseconds (0-5000)
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Strength of the break (alternative to DurationMs)
    /// </summary>
    public BreakStrength? Strength { get; set; }
}

/// <summary>
/// Break strength options
/// </summary>
public enum BreakStrength
{
    None,
    XWeak,
    Weak,
    Medium,
    Strong,
    XStrong
}

/// <summary>
/// Say-as interpretation hint
/// </summary>
public class SayAsInterpretation
{
    /// <summary>
    /// Text to apply the interpretation to
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// How to interpret the text
    /// </summary>
    public required SayAsType InterpretAs { get; set; }

    /// <summary>
    /// Optional format for date/time interpretations
    /// </summary>
    public string? Format { get; set; }
}

/// <summary>
/// Say-as interpretation types
/// </summary>
public enum SayAsType
{
    Date,
    Time,
    Telephone,
    Cardinal,
    Ordinal,
    Digits,
    Fraction,
    Unit,
    Address,
    SpellOut
}
