using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audio;

/// <summary>
/// Result of narration optimization for TTS synthesis
/// </summary>
public record NarrationOptimizationResult
{
    /// <summary>
    /// Optimized script lines ready for TTS
    /// </summary>
    public required IReadOnlyList<OptimizedScriptLine> OptimizedLines { get; init; }

    /// <summary>
    /// Original script lines before optimization
    /// </summary>
    public required IReadOnlyList<ScriptLine> OriginalLines { get; init; }

    /// <summary>
    /// Overall optimization score (0-100)
    /// </summary>
    public double OptimizationScore { get; init; }

    /// <summary>
    /// Processing time for optimization
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Number of optimizations applied
    /// </summary>
    public int OptimizationsApplied { get; init; }

    /// <summary>
    /// Detected issues that were fixed
    /// </summary>
    public IReadOnlyList<string> IssuesFixed { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Warnings or recommendations
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Script line optimized for TTS synthesis
/// </summary>
public record OptimizedScriptLine
{
    /// <summary>
    /// Scene index
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Original text
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Optimized text for TTS
    /// </summary>
    public required string OptimizedText { get; init; }

    /// <summary>
    /// Start time of the line
    /// </summary>
    public TimeSpan Start { get; init; }

    /// <summary>
    /// Duration of the line
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Detected emotional tone
    /// </summary>
    public NarrationTone? EmotionalTone { get; init; }

    /// <summary>
    /// Confidence score for emotional tone detection (0-1)
    /// </summary>
    public double EmotionConfidence { get; init; }

    /// <summary>
    /// Pronunciation hints for technical terms or proper nouns
    /// </summary>
    public IReadOnlyDictionary<string, string> PronunciationHints { get; init; } = 
        new Dictionary<string, string>();

    /// <summary>
    /// SSML markup if supported by TTS engine
    /// </summary>
    public string? SsmlMarkup { get; init; }

    /// <summary>
    /// Optimizations applied to this line
    /// </summary>
    public IReadOnlyList<OptimizationAction> ActionsApplied { get; init; } = Array.Empty<OptimizationAction>();

    /// <summary>
    /// Was this line modified during optimization?
    /// </summary>
    public bool WasModified => OriginalText != OptimizedText;
}

/// <summary>
/// Emotional tone for TTS expression
/// </summary>
public enum NarrationTone
{
    /// <summary>Neutral, factual delivery</summary>
    Neutral,
    
    /// <summary>Excited, enthusiastic delivery</summary>
    Excited,
    
    /// <summary>Serious, somber delivery</summary>
    Somber,
    
    /// <summary>Urgent, important delivery</summary>
    Urgent,
    
    /// <summary>Relaxed, calm delivery</summary>
    Relaxed,
    
    /// <summary>Cheerful, upbeat delivery</summary>
    Cheerful,
    
    /// <summary>Emphatic, strong delivery</summary>
    Emphatic,
    
    /// <summary>Thoughtful, contemplative delivery</summary>
    Thoughtful,
    
    /// <summary>Warm, friendly delivery</summary>
    Warm,
    
    /// <summary>Professional, formal delivery</summary>
    Professional
}

/// <summary>
/// Type of optimization action applied
/// </summary>
public enum OptimizationAction
{
    /// <summary>Broke long sentence into smaller chunks</summary>
    SentenceSimplification,
    
    /// <summary>Added natural pauses with punctuation</summary>
    PauseInsertion,
    
    /// <summary>Removed or rewrote tongue-twister</summary>
    TongueTwisterRemoval,
    
    /// <summary>Adjusted vocabulary for spoken language</summary>
    VocabularyAdjustment,
    
    /// <summary>Added pronunciation hint</summary>
    PronunciationHint,
    
    /// <summary>Clarified acronym</summary>
    AcronymClarification,
    
    /// <summary>Spelled out number or date</summary>
    NumberSpelling,
    
    /// <summary>Disambiguated homograph</summary>
    HomographDisambiguation,
    
    /// <summary>Added emotional tone tag</summary>
    EmotionalToneTagging,
    
    /// <summary>Adapted language to voice personality</summary>
    VoicePersonalityAdaptation,
    
    /// <summary>Simplified consonant cluster</summary>
    ConsonantClusterSimplification,
    
    /// <summary>Added SSML prosody hints</summary>
    SsmlEnhancement
}

/// <summary>
/// Configuration for narration optimization
/// </summary>
public record NarrationOptimizationConfig
{
    /// <summary>
    /// Maximum sentence length in words before simplification
    /// </summary>
    public int MaxSentenceWords { get; init; } = 25;

    /// <summary>
    /// Enable tongue-twister detection
    /// </summary>
    public bool EnableTongueTwisterDetection { get; init; } = true;

    /// <summary>
    /// Enable emotional tone tagging
    /// </summary>
    public bool EnableEmotionalToneTagging { get; init; } = true;

    /// <summary>
    /// Minimum confidence for emotional tone (0-1)
    /// </summary>
    public double MinEmotionConfidence { get; init; } = 0.75;

    /// <summary>
    /// Enable SSML generation if TTS supports it
    /// </summary>
    public bool EnableSsml { get; init; } = true;

    /// <summary>
    /// Enable voice personality adaptation
    /// </summary>
    public bool EnableVoiceAdaptation { get; init; } = true;

    /// <summary>
    /// Enable pronunciation dictionary
    /// </summary>
    public bool EnablePronunciationHints { get; init; } = true;

    /// <summary>
    /// Enable acronym clarification
    /// </summary>
    public bool EnableAcronymClarification { get; init; } = true;

    /// <summary>
    /// Enable number spelling
    /// </summary>
    public bool EnableNumberSpelling { get; init; } = true;

    /// <summary>
    /// Enable homograph disambiguation
    /// </summary>
    public bool EnableHomographDisambiguation { get; init; } = true;

    /// <summary>
    /// Custom pronunciation dictionary (term -> phonetic hint)
    /// </summary>
    public IReadOnlyDictionary<string, string>? CustomPronunciations { get; init; }
}

/// <summary>
/// TTS compatibility issue detected during validation
/// </summary>
public record TtsCompatibilityIssue
{
    /// <summary>
    /// Type of compatibility issue
    /// </summary>
    public required TtsIssueType Type { get; init; }

    /// <summary>
    /// Problematic text segment
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Position in original text
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// Severity (0-100, higher is more severe)
    /// </summary>
    public int Severity { get; init; }

    /// <summary>
    /// Suggested fix
    /// </summary>
    public string? SuggestedFix { get; init; }

    /// <summary>
    /// Explanation of the issue
    /// </summary>
    public string? Explanation { get; init; }
}

/// <summary>
/// Types of TTS compatibility issues
/// </summary>
public enum TtsIssueType
{
    /// <summary>Ambiguous acronym needs pronunciation</summary>
    AmbiguousAcronym,
    
    /// <summary>Number that should be spelled out</summary>
    NumberSpelling,
    
    /// <summary>Homograph needing context</summary>
    Homograph,
    
    /// <summary>Difficult consonant cluster</summary>
    ConsonantCluster,
    
    /// <summary>Tongue-twister pattern detected</summary>
    TongueTwister,
    
    /// <summary>Sentence too long for natural delivery</summary>
    LongSentence,
    
    /// <summary>Missing natural pauses</summary>
    MissingPauses,
    
    /// <summary>Technical term needing pronunciation</summary>
    TechnicalTerm,
    
    /// <summary>Proper noun needing pronunciation</summary>
    ProperNoun
}
