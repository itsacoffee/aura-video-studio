using System.Collections.Generic;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Result of analyzing a script for dialogue and characters.
/// </summary>
public record DialogueAnalysis(
    IReadOnlyList<DialogueLine> Lines,
    IReadOnlyList<DetectedCharacter> Characters,
    bool HasMultipleCharacters);

/// <summary>
/// A single line of dialogue with metadata.
/// </summary>
public record DialogueLine(
    int StartIndex,
    int EndIndex,
    string Text,
    string? CharacterName,
    DialogueType Type,
    EmotionHint? Emotion);

/// <summary>
/// A character detected in the script.
/// </summary>
public record DetectedCharacter(
    string Name,
    string SuggestedVoiceType,
    int LineCount);

/// <summary>
/// Type of dialogue content.
/// </summary>
public enum DialogueType
{
    /// <summary>Third-person narration.</summary>
    Narration,
    /// <summary>Direct speech from a character.</summary>
    Dialogue,
    /// <summary>Quoted text or citations.</summary>
    Quote,
    /// <summary>Internal thought or monologue.</summary>
    InternalThought
}

/// <summary>
/// Emotional tone hints for TTS synthesis.
/// </summary>
public enum EmotionHint
{
    /// <summary>No specific emotion.</summary>
    Neutral,
    /// <summary>Excited, enthusiastic tone.</summary>
    Excited,
    /// <summary>Sad, melancholic tone.</summary>
    Sad,
    /// <summary>Angry, frustrated tone.</summary>
    Angry,
    /// <summary>Curious, questioning tone.</summary>
    Curious,
    /// <summary>Calm, peaceful tone.</summary>
    Calm
}
