using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Profiles;

/// <summary>
/// Represents a user profile with unique settings and preferences
/// </summary>
public record UserProfile(
    string ProfileId,
    string UserId,
    string ProfileName,
    string? Description,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime LastUsed,
    DateTime UpdatedAt
);

/// <summary>
/// Complete profile preferences including all categories
/// </summary>
public record ProfilePreferences(
    string ProfileId,
    string? ContentType,
    TonePreferences? Tone,
    VisualPreferences? Visual,
    AudioPreferences? Audio,
    EditingPreferences? Editing,
    PlatformPreferences? Platform,
    AIBehaviorSettings? AIBehavior
);

/// <summary>
/// Tone and voice preferences for content
/// </summary>
public record TonePreferences(
    int Formality,                  // 0 (very casual) to 100 (very formal)
    int Energy,                     // 0 (calm) to 100 (high-energy)
    List<string>? PersonalityTraits, // e.g., ["authoritative", "friendly", "quirky"]
    string? CustomDescription       // User's custom tone description
);

/// <summary>
/// Visual style and aesthetic preferences
/// </summary>
public record VisualPreferences(
    string? Aesthetic,              // cinematic, corporate, vibrant, minimalist, documentary
    string? ColorPalette,           // warm, cool, monochrome, vibrant, pastel, brand
    string? ShotTypePreference,     // preference for close-ups vs. wide shots
    string? CompositionStyle,       // rule of thirds, centered, dynamic
    string? PacingPreference,       // fast cuts vs. longer shots
    string? BRollUsage             // minimal, moderate, heavy
);

/// <summary>
/// Audio and music preferences
/// </summary>
public record AudioPreferences(
    List<string>? MusicGenres,      // Preferred music genres
    int MusicEnergy,                // 0 (calm) to 100 (energetic)
    string? MusicProminence,        // subtle background, balanced, prominent feature
    string? SoundEffectsUsage,      // none, minimal, moderate, heavy
    string? VoiceStyle,             // authoritative, warm, energetic, calm (for TTS)
    string? AudioMixing             // voice-focused, balanced with music
);

/// <summary>
/// Editing style and pacing preferences
/// </summary>
public record EditingPreferences(
    int Pacing,                     // 0 (slow/deliberate) to 100 (fast/dynamic)
    int CutFrequency,               // 0 (long takes) to 100 (quick cuts)
    string? TransitionStyle,        // simple cuts, subtle fades, dynamic transitions
    string? EffectUsage,            // none, subtle, moderate, prominent
    int SceneDuration,              // Average scene duration preference (seconds)
    string? EditingPhilosophy       // invisible editing, stylized editing
);

/// <summary>
/// Platform-specific targeting preferences
/// </summary>
public record PlatformPreferences(
    string? PrimaryPlatform,        // YouTube, TikTok, Instagram, etc.
    List<string>? SecondaryPlatforms,
    string? AspectRatio,            // 16:9, 9:16, 1:1, 4:5
    int? TargetDurationSeconds,     // Target video length
    string? AudienceDemographic     // Description of target audience
);

/// <summary>
/// AI assistance behavior settings
/// </summary>
public record AIBehaviorSettings(
    int AssistanceLevel,            // 0 (minimal) to 100 (highly proactive)
    string? SuggestionVerbosity,    // brief, moderate, detailed
    bool AutoApplySuggestions,      // Whether AI can auto-apply suggestions
    string? SuggestionFrequency,    // only when asked, moderate, proactive
    int CreativityLevel,            // 0 (conservative) to 100 (experimental)
    List<string>? OverridePermissions // Which AI decisions require approval
);

/// <summary>
/// Records a user's decision on an AI suggestion
/// </summary>
public record DecisionRecord(
    string RecordId,
    string ProfileId,
    string SuggestionType,          // e.g., "tone_adjustment", "visual_style"
    string Decision,                // accepted, rejected, modified
    DateTime Timestamp,
    Dictionary<string, object>? Context // Additional context about the suggestion
);

/// <summary>
/// Profile template with pre-configured preferences
/// </summary>
public record ProfileTemplate(
    string TemplateId,
    string Name,
    string Description,
    string Category,                // gaming, corporate, educational, etc.
    ProfilePreferences DefaultPreferences
);

/// <summary>
/// Request to create a new profile
/// </summary>
public record CreateProfileRequest(
    string UserId,
    string ProfileName,
    string? Description,
    string? FromTemplateId
);

/// <summary>
/// Request to update profile metadata
/// </summary>
public record UpdateProfileRequest(
    string? ProfileName,
    string? Description
);

/// <summary>
/// Request to update profile preferences
/// </summary>
public record UpdatePreferencesRequest(
    string? ContentType,
    TonePreferences? Tone,
    VisualPreferences? Visual,
    AudioPreferences? Audio,
    EditingPreferences? Editing,
    PlatformPreferences? Platform,
    AIBehaviorSettings? AIBehavior
);

/// <summary>
/// Request to record a user decision
/// </summary>
public record RecordDecisionRequest(
    string ProfileId,
    string SuggestionType,
    string Decision,
    Dictionary<string, object>? Context
);

/// <summary>
/// Response with profile summary
/// </summary>
public record ProfileSummaryResponse(
    string ProfileId,
    string ProfileName,
    string? Description,
    bool IsDefault,
    bool IsActive,
    DateTime LastUsed,
    string? ContentType
);
