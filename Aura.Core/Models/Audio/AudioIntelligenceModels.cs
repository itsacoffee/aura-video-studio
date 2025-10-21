using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audio;

/// <summary>
/// Emotion/mood for music selection
/// </summary>
public enum MusicMood
{
    Neutral,
    Happy,
    Sad,
    Energetic,
    Calm,
    Dramatic,
    Tense,
    Uplifting,
    Melancholic,
    Mysterious,
    Playful,
    Serious,
    Romantic,
    Epic,
    Ambient
}

/// <summary>
/// Music genre classification
/// </summary>
public enum MusicGenre
{
    Cinematic,
    Electronic,
    Rock,
    Pop,
    Ambient,
    Classical,
    Jazz,
    HipHop,
    Folk,
    Corporate,
    Orchestral,
    Indie,
    LoFi,
    Motivational
}

/// <summary>
/// Energy level for music intensity
/// </summary>
public enum EnergyLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Emotional delivery for TTS
/// </summary>
public enum EmotionalDelivery
{
    Neutral,
    Excited,
    Serious,
    Warm,
    Friendly,
    Professional,
    Urgent,
    Calm,
    Enthusiastic,
    Authoritative
}

/// <summary>
/// Sound effect type classification
/// </summary>
public enum SoundEffectType
{
    Transition,
    Impact,
    Whoosh,
    Click,
    UI,
    Ambient,
    Nature,
    Technology,
    Action,
    Notification
}

/// <summary>
/// Represents a music track with metadata
/// </summary>
public record MusicTrack(
    string TrackId,
    string Title,
    string? Artist,
    MusicGenre Genre,
    MusicMood Mood,
    EnergyLevel Energy,
    int BPM,
    TimeSpan Duration,
    string FilePath,
    List<double>? BeatTimestamps,
    Dictionary<string, object>? Metadata
);

/// <summary>
/// Voice direction parameters for TTS
/// </summary>
public record VoiceDirection(
    string LineId,
    EmotionalDelivery Emotion,
    List<string> EmphasisWords,
    double PaceMultiplier,      // 0.5-2.0, where 1.0 is normal
    string Tone,                // e.g., "conversational", "formal"
    List<PausePoint> Pauses,
    Dictionary<string, string>? PronunciationGuide
);

/// <summary>
/// Pause point in narration
/// </summary>
public record PausePoint(
    int CharacterPosition,
    TimeSpan Duration
);

/// <summary>
/// Sound effect suggestion with timing
/// </summary>
public record SoundEffect(
    string EffectId,
    SoundEffectType Type,
    string Description,
    TimeSpan Timestamp,
    TimeSpan Duration,
    double Volume,              // 0-100
    string Purpose,
    string? FilePath
);

/// <summary>
/// Audio mixing parameters and suggestions
/// </summary>
public record AudioMixing(
    double MusicVolume,         // 0-100
    double NarrationVolume,     // 0-100
    double SoundEffectsVolume,  // 0-100
    DuckingSettings Ducking,
    EqualizationSettings EQ,
    CompressionSettings Compression,
    bool Normalize,
    double TargetLUFS
);

/// <summary>
/// Music ducking settings
/// </summary>
public record DuckingSettings(
    double DuckDepthDb,         // How much to reduce music (negative dB)
    TimeSpan AttackTime,
    TimeSpan ReleaseTime,
    double Threshold
);

/// <summary>
/// EQ settings for voice clarity
/// </summary>
public record EqualizationSettings(
    double HighPassFrequency,   // Hz, typically 80-100 for voice
    double PresenceBoost,       // dB, typically 2-4 dB around 3-5kHz
    double DeEsserReduction     // dB, typically -3 to -6 dB around 7kHz
);

/// <summary>
/// Compression settings for dynamic range
/// </summary>
public record CompressionSettings(
    double Threshold,           // dB
    double Ratio,              // e.g., 3:1
    TimeSpan AttackTime,
    TimeSpan ReleaseTime,
    double MakeupGain           // dB
);

/// <summary>
/// Beat marker for synchronization
/// </summary>
public record BeatMarker(
    double Timestamp,           // seconds
    double Strength,            // 0-1, confidence/intensity
    bool IsDownbeat,            // True if major beat
    int MusicalPhrase          // Which phrase this beat belongs to
);

/// <summary>
/// Music generation prompt for AI music tools
/// </summary>
public record MusicPrompt(
    string PromptId,
    MusicMood Mood,
    MusicGenre Genre,
    EnergyLevel Energy,
    TimeSpan TargetDuration,
    int? TargetBPM,
    string Instrumentation,     // e.g., "piano, strings, light percussion"
    string Style,               // Additional style descriptors
    string? ReferenceTrackId,   // Similar to this track
    DateTime CreatedAt
);

/// <summary>
/// Music recommendation with scoring
/// </summary>
public record MusicRecommendation(
    MusicTrack Track,
    double RelevanceScore,      // 0-100
    string Reasoning,           // Why this track was recommended
    List<string> MatchingAttributes,
    TimeSpan? SuggestedStartTime,
    TimeSpan? SuggestedDuration
);

/// <summary>
/// Audio continuity check results
/// </summary>
public record AudioContinuity(
    double StyleConsistencyScore,   // 0-100
    double VolumeConsistencyScore,  // 0-100
    double ToneConsistencyScore,    // 0-100
    List<string> Issues,
    List<string> Suggestions,
    DateTime CheckedAt
);

/// <summary>
/// Audio-visual synchronization analysis
/// </summary>
public record SyncAnalysis(
    List<SyncPoint> SyncPoints,
    double OverallSyncScore,    // 0-100
    List<string> Recommendations,
    DateTime AnalyzedAt
);

/// <summary>
/// A synchronization point between audio and visual
/// </summary>
public record SyncPoint(
    TimeSpan Timestamp,
    string AudioEvent,          // e.g., "beat", "word emphasis", "sound effect"
    string VisualEvent,         // e.g., "scene transition", "text appear", "animation"
    double Offset,              // seconds, negative means audio is early
    bool IsAligned              // True if offset is acceptable
);

/// <summary>
/// Script analysis for audio requirements
/// </summary>
public record ScriptAudioAnalysis(
    List<MusicMood> EmotionalArc,       // Mood per scene/segment
    List<EnergyLevel> EnergyProgression,
    List<SoundEffectSuggestion> SoundEffects,
    List<VoiceDirectionHint> VoiceHints,
    string OverallTone,
    TimeSpan EstimatedDuration
);

/// <summary>
/// Sound effect suggestion from script analysis
/// </summary>
public record SoundEffectSuggestion(
    int SceneIndex,
    TimeSpan EstimatedTimestamp,
    SoundEffectType Type,
    string Trigger,             // What in the script triggered this
    string Description,
    double Confidence           // 0-100
);

/// <summary>
/// Voice direction hint from script analysis
/// </summary>
public record VoiceDirectionHint(
    int SceneIndex,
    string Context,
    EmotionalDelivery SuggestedEmotion,
    List<string> KeyWords,      // Words that should be emphasized
    string Reasoning
);

/// <summary>
/// Music library search parameters
/// </summary>
public record MusicSearchParams(
    MusicMood? Mood = null,
    MusicGenre? Genre = null,
    EnergyLevel? Energy = null,
    int? MinBPM = null,
    int? MaxBPM = null,
    TimeSpan? MinDuration = null,
    TimeSpan? MaxDuration = null,
    string? SearchQuery = null,
    int? Limit = 50
);

/// <summary>
/// Request to analyze script for audio needs
/// </summary>
public record AnalyzeScriptRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    List<TimeSpan>? SceneDurations
);

/// <summary>
/// Request for music suggestions
/// </summary>
public record SuggestMusicRequest(
    MusicMood Mood,
    MusicGenre? PreferredGenre,
    EnergyLevel Energy,
    TimeSpan Duration,
    string? Context,
    int? MaxResults = 10
);

/// <summary>
/// Request for beat detection
/// </summary>
public record DetectBeatsRequest(
    string FilePath,
    int? MinBPM = 60,
    int? MaxBPM = 200
);

/// <summary>
/// Request for voice direction
/// </summary>
public record VoiceDirectionRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    List<string>? KeyMessages
);

/// <summary>
/// Request for sound effect suggestions
/// </summary>
public record SoundEffectRequest(
    string Script,
    List<TimeSpan>? SceneDurations,
    string? ContentType
);

/// <summary>
/// Request for mixing suggestions
/// </summary>
public record MixingSuggestionsRequest(
    string ContentType,
    bool HasNarration,
    bool HasMusic,
    bool HasSoundEffects,
    double? TargetLUFS = -14.0
);

/// <summary>
/// Request for AI music generation prompt
/// </summary>
public record MusicPromptRequest(
    MusicMood Mood,
    MusicGenre Genre,
    EnergyLevel Energy,
    TimeSpan Duration,
    string? AdditionalContext
);

/// <summary>
/// Request for sync analysis
/// </summary>
public record SyncAnalysisRequest(
    List<TimeSpan> AudioBeatTimestamps,
    List<TimeSpan> VisualTransitionTimestamps,
    TimeSpan VideoDuration
);

/// <summary>
/// Request for continuity check
/// </summary>
public record ContinuityCheckRequest(
    List<string> AudioSegmentPaths,
    string? TargetStyle
);
