using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ScriptEnhancement;

/// <summary>
/// Represents the type of storytelling framework to apply
/// </summary>
public enum StoryFrameworkType
{
    HeroJourney,          // Hero's Journey (Monomyth)
    ThreeAct,             // Classic 3-Act Structure
    ProblemSolution,      // Problem-Solution framework
    AIDA,                 // Attention-Interest-Desire-Action
    BeforeAfter,          // Before-After-Bridge
    Comparison,           // Comparison/Contrast
    Chronological,        // Time-based narrative
    CauseEffect           // Cause and Effect
}

/// <summary>
/// Emotional tone/target for a scene or segment
/// </summary>
public enum EmotionalTone
{
    Neutral,
    Curious,
    Excited,
    Concerned,
    Hopeful,
    Satisfied,
    Inspired,
    Empowered,
    Entertained,
    Thoughtful,
    Urgent,
    Relieved
}

/// <summary>
/// Type of script enhancement suggestion
/// </summary>
public enum SuggestionType
{
    Structure,            // Narrative structure improvements
    Hook,                 // Opening hook enhancement
    Dialog,               // Natural language improvements
    Pacing,               // Timing and rhythm
    Emotion,              // Emotional impact
    Clarity,              // Readability and understanding
    Engagement,           // Audience connection
    Transition,           // Scene connections
    FactCheck,            // Accuracy concerns
    Tone,                 // Voice and style
    Pronunciation,        // TTS optimization
    Callback              // Internal references
}

/// <summary>
/// Analysis of a script's structure and quality
/// </summary>
public record ScriptAnalysis(
    double StructureScore,        // 0-100: How well-structured the narrative is
    double EmotionalCurveScore,   // 0-100: Quality of emotional journey
    double EngagementScore,       // 0-100: Audience connection strength
    double ClarityScore,          // 0-100: Readability and clarity
    double HookStrength,          // 0-100: Opening hook effectiveness
    List<string> Issues,          // Identified problems
    List<string> Strengths,       // Positive aspects
    StoryFrameworkType? DetectedFramework,  // Detected narrative structure
    List<EmotionalPoint> EmotionalCurve,    // Emotional arc through video
    Dictionary<string, double> ReadabilityMetrics,  // Flesch-Kincaid, etc.
    DateTime AnalyzedAt
);

/// <summary>
/// A point on the emotional curve
/// </summary>
public record EmotionalPoint(
    double TimePosition,          // 0-1 position in video
    EmotionalTone Tone,
    double Intensity,             // 0-100: Strength of emotion
    string? Context               // What's happening at this point
);

/// <summary>
/// Represents a single enhancement suggestion
/// </summary>
public record EnhancementSuggestion(
    string SuggestionId,
    SuggestionType Type,
    int? SceneIndex,              // Null for script-wide suggestions
    int? LineNumber,              // Specific line if applicable
    string OriginalText,
    string SuggestedText,
    string Explanation,           // Why this improves the script
    double ConfidenceScore,       // 0-100: AI confidence in this suggestion
    List<string> Benefits,        // Expected improvements
    DateTime CreatedAt
);

/// <summary>
/// Represents an applied storytelling framework
/// </summary>
public record StoryFramework(
    StoryFrameworkType Type,
    Dictionary<string, string> Elements,  // Framework elements and their content
    List<string> MissingElements,         // Elements not present in script
    double ComplianceScore,               // 0-100: How well script follows framework
    string ApplicationNotes
);

/// <summary>
/// Version of a script with metadata
/// </summary>
public record ScriptVersion(
    string VersionId,
    int VersionNumber,
    string Script,
    string? ChangesSummary,
    List<string> AppliedSuggestionIds,
    string? Author,               // "user" or "ai"
    DateTime CreatedAt,
    Dictionary<string, object>? Metadata
);

/// <summary>
/// Profile describing tone and voice characteristics
/// </summary>
public record ToneProfile(
    double FormalityLevel,        // 0-100: Casual to formal
    double EnergyLevel,           // 0-100: Calm to energetic
    double EmotionLevel,          // 0-100: Neutral to emotional
    List<string> PersonalityTraits,  // e.g., "friendly", "authoritative", "humorous"
    string? BrandVoice,           // Consistency with brand
    Dictionary<string, object>? CustomAttributes
);

/// <summary>
/// Emotional arc optimization for entire video
/// </summary>
public record EmotionalArc(
    List<EmotionalPoint> TargetCurve,     // Desired emotional journey
    double CurveSmoothnessScore,          // 0-100: How smooth transitions are
    double VarietyScore,                  // 0-100: Emotional range/variety
    List<string> PeakMoments,             // Descriptions of emotional peaks
    List<string> ValleyMoments,           // Calm/neutral segments
    string ArcStrategy,                   // Overall approach (e.g., "build tension to climax")
    DateTime GeneratedAt
);

/// <summary>
/// Request to analyze a script with detailed enhancement options
/// </summary>
public record ScriptAnalysisRequest(
    string Script,
    string? ContentType,          // Tutorial, Entertainment, Educational, etc.
    string? TargetAudience,
    string? CurrentTone
);

/// <summary>
/// Response from detailed script analysis
/// </summary>
public record ScriptAnalysisResponse(
    bool Success,
    ScriptAnalysis? Analysis,
    string? ErrorMessage
);

/// <summary>
/// Request to enhance a script with AI
/// </summary>
public record ScriptEnhanceRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    string? DesiredTone,
    List<SuggestionType>? FocusAreas,     // Specific areas to enhance
    bool AutoApply = false,               // Auto-apply high-confidence suggestions
    StoryFrameworkType? TargetFramework = null
);

/// <summary>
/// Response from script AI enhancement
/// </summary>
public record ScriptEnhanceResponse(
    bool Success,
    string? EnhancedScript,
    List<EnhancementSuggestion> Suggestions,
    string? ChangesSummary,
    ScriptAnalysis? BeforeAnalysis,
    ScriptAnalysis? AfterAnalysis,
    string? ErrorMessage
);

/// <summary>
/// Request to optimize opening hook
/// </summary>
public record OptimizeHookRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    int TargetSeconds = 15         // Hook length target
);

/// <summary>
/// Response from hook optimization
/// </summary>
public record OptimizeHookResponse(
    bool Success,
    string? OptimizedHook,
    double HookStrengthBefore,
    double HookStrengthAfter,
    List<string> Techniques,       // Applied techniques (curiosity gap, promise, etc.)
    string? Explanation,
    string? ErrorMessage
);

/// <summary>
/// Request to analyze/optimize emotional arc
/// </summary>
public record EmotionalArcRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    string? DesiredJourney         // e.g., "curiosity → tension → satisfaction"
);

/// <summary>
/// Response from emotional arc analysis
/// </summary>
public record EmotionalArcResponse(
    bool Success,
    EmotionalArc? CurrentArc,
    EmotionalArc? OptimizedArc,
    List<EnhancementSuggestion> Suggestions,
    string? ErrorMessage
);

/// <summary>
/// Request to enhance audience connection
/// </summary>
public record AudienceConnectionRequest(
    string Script,
    string? TargetAudience,
    string? ContentType
);

/// <summary>
/// Response from audience connection enhancement
/// </summary>
public record AudienceConnectionResponse(
    bool Success,
    string? EnhancedScript,
    List<EnhancementSuggestion> Suggestions,
    double ConnectionScoreBefore,
    double ConnectionScoreAfter,
    string? ErrorMessage
);

/// <summary>
/// Request for fact-checking
/// </summary>
public record FactCheckRequest(
    string Script,
    bool IncludeSources = true
);

/// <summary>
/// Fact-checking finding
/// </summary>
public record FactCheckFinding(
    string ClaimText,
    string Verification,          // "verified", "uncertain", "disputed", "incorrect"
    double ConfidenceScore,
    string? Source,
    string? Explanation,
    string? Suggestion
);

/// <summary>
/// Response from fact-checking
/// </summary>
public record FactCheckResponse(
    bool Success,
    List<FactCheckFinding> Findings,
    int TotalClaims,
    int VerifiedClaims,
    int UncertainClaims,
    string? DisclaimerSuggestion,
    string? ErrorMessage
);

/// <summary>
/// Request to adjust tone
/// </summary>
public record ToneAdjustRequest(
    string Script,
    ToneProfile TargetTone,
    string? ContentType
);

/// <summary>
/// Response from tone adjustment
/// </summary>
public record ToneAdjustResponse(
    bool Success,
    string? AdjustedScript,
    ToneProfile? OriginalTone,
    ToneProfile? AchievedTone,
    List<EnhancementSuggestion> Changes,
    string? ErrorMessage
);

/// <summary>
/// Request to apply specific storytelling framework
/// </summary>
public record ApplyFrameworkRequest(
    string Script,
    StoryFrameworkType Framework,
    string? ContentType,
    string? TargetAudience
);

/// <summary>
/// Response from framework application
/// </summary>
public record ApplyFrameworkResponse(
    bool Success,
    string? EnhancedScript,
    StoryFramework? AppliedFramework,
    List<EnhancementSuggestion> Suggestions,
    string? ErrorMessage
);

/// <summary>
/// Request for individual suggestions
/// </summary>
public record GetSuggestionsRequest(
    string Script,
    string? ContentType,
    string? TargetAudience,
    List<SuggestionType>? FilterTypes,
    int? MaxSuggestions
);

/// <summary>
/// Response with suggestions
/// </summary>
public record GetSuggestionsResponse(
    bool Success,
    List<EnhancementSuggestion> Suggestions,
    int TotalCount,
    string? ErrorMessage
);

/// <summary>
/// Request to compare script versions
/// </summary>
public record CompareVersionsRequest(
    string VersionA,
    string VersionB,
    bool IncludeAnalysis = true
);

/// <summary>
/// A diff between two versions
/// </summary>
public record ScriptDiff(
    string Type,                  // "added", "removed", "modified"
    int LineNumber,
    string? OldText,
    string? NewText,
    string? Context
);

/// <summary>
/// Response from version comparison
/// </summary>
public record CompareVersionsResponse(
    bool Success,
    List<ScriptDiff> Differences,
    ScriptAnalysis? AnalysisA,
    ScriptAnalysis? AnalysisB,
    Dictionary<string, double> ImprovementMetrics,  // e.g., "clarity": +15.5
    string? ErrorMessage
);
