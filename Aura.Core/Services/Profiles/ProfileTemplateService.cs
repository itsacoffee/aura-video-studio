using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Profiles;

namespace Aura.Core.Services.Profiles;

/// <summary>
/// Provides pre-configured profile templates for common use cases
/// </summary>
public static class ProfileTemplateService
{
    private static readonly List<ProfileTemplate> _templates = new()
    {
        // YouTube Gaming Template
        new ProfileTemplate(
            TemplateId: "youtube-gaming",
            Name: "YouTube Gaming",
            Description: "High-energy gaming content with dynamic editing and engaging commentary",
            Category: "gaming",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "gaming",
                Tone: new TonePreferences(
                    Formality: 20,
                    Energy: 90,
                    PersonalityTraits: new List<string> { "energetic", "humorous", "enthusiastic" },
                    CustomDescription: "Energetic and engaging, keeping viewers excited"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "vibrant",
                    ColorPalette: "vibrant",
                    ShotTypePreference: "dynamic",
                    CompositionStyle: "dynamic",
                    PacingPreference: "fast cuts",
                    BRollUsage: "moderate"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "electronic", "upbeat", "energetic" },
                    MusicEnergy: 85,
                    MusicProminence: "prominent feature",
                    SoundEffectsUsage: "heavy",
                    VoiceStyle: "energetic",
                    AudioMixing: "voice-focused"
                ),
                Editing: new EditingPreferences(
                    Pacing: 85,
                    CutFrequency: 85,
                    TransitionStyle: "dynamic transitions",
                    EffectUsage: "prominent",
                    SceneDuration: 3,
                    EditingPhilosophy: "stylized editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "YouTube",
                    SecondaryPlatforms: new List<string> { "Twitch" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 900,
                    AudienceDemographic: "Young adults, gamers, 18-35"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 70,
                    SuggestionVerbosity: "moderate",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "proactive",
                    CreativityLevel: 75,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        ),

        // Corporate Training Template
        new ProfileTemplate(
            TemplateId: "corporate-training",
            Name: "Corporate Training",
            Description: "Professional training videos with clear, structured content",
            Category: "corporate",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "training",
                Tone: new TonePreferences(
                    Formality: 80,
                    Energy: 40,
                    PersonalityTraits: new List<string> { "authoritative", "professional", "clear" },
                    CustomDescription: "Professional and informative"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "corporate",
                    ColorPalette: "cool",
                    ShotTypePreference: "balanced",
                    CompositionStyle: "rule of thirds",
                    PacingPreference: "moderate",
                    BRollUsage: "moderate"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "corporate", "ambient" },
                    MusicEnergy: 30,
                    MusicProminence: "subtle background",
                    SoundEffectsUsage: "minimal",
                    VoiceStyle: "authoritative",
                    AudioMixing: "voice-focused"
                ),
                Editing: new EditingPreferences(
                    Pacing: 40,
                    CutFrequency: 30,
                    TransitionStyle: "subtle fades",
                    EffectUsage: "subtle",
                    SceneDuration: 8,
                    EditingPhilosophy: "invisible editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "Corporate LMS",
                    SecondaryPlatforms: new List<string> { "YouTube" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 1200,
                    AudienceDemographic: "Corporate employees, professionals"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 60,
                    SuggestionVerbosity: "detailed",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "moderate",
                    CreativityLevel: 30,
                    OverridePermissions: new List<string> { "major_changes", "tone_changes" }
                )
            )
        ),

        // Educational Tutorial Template
        new ProfileTemplate(
            TemplateId: "educational-tutorial",
            Name: "Educational Tutorial",
            Description: "Clear, step-by-step instructional content",
            Category: "educational",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "tutorial",
                Tone: new TonePreferences(
                    Formality: 60,
                    Energy: 55,
                    PersonalityTraits: new List<string> { "friendly", "patient", "encouraging" },
                    CustomDescription: "Clear and approachable"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "documentary",
                    ColorPalette: "natural",
                    ShotTypePreference: "close-ups for detail",
                    CompositionStyle: "rule of thirds",
                    PacingPreference: "moderate",
                    BRollUsage: "heavy"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "ambient", "uplifting" },
                    MusicEnergy: 40,
                    MusicProminence: "subtle background",
                    SoundEffectsUsage: "moderate",
                    VoiceStyle: "warm",
                    AudioMixing: "voice-focused"
                ),
                Editing: new EditingPreferences(
                    Pacing: 45,
                    CutFrequency: 40,
                    TransitionStyle: "simple cuts",
                    EffectUsage: "moderate",
                    SceneDuration: 6,
                    EditingPhilosophy: "invisible editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "YouTube",
                    SecondaryPlatforms: new List<string> { "Udemy", "Skillshare" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 720,
                    AudienceDemographic: "Learners, students, hobbyists"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 65,
                    SuggestionVerbosity: "detailed",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "moderate",
                    CreativityLevel: 40,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        ),

        // Product Review Template
        new ProfileTemplate(
            TemplateId: "product-review",
            Name: "Product Review",
            Description: "Balanced, informative product reviews",
            Category: "review",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "review",
                Tone: new TonePreferences(
                    Formality: 50,
                    Energy: 60,
                    PersonalityTraits: new List<string> { "honest", "analytical", "balanced" },
                    CustomDescription: "Informative and trustworthy"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "cinematic",
                    ColorPalette: "natural",
                    ShotTypePreference: "close-ups for detail",
                    CompositionStyle: "rule of thirds",
                    PacingPreference: "moderate",
                    BRollUsage: "heavy"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "ambient", "modern" },
                    MusicEnergy: 50,
                    MusicProminence: "balanced",
                    SoundEffectsUsage: "minimal",
                    VoiceStyle: "warm",
                    AudioMixing: "balanced with music"
                ),
                Editing: new EditingPreferences(
                    Pacing: 55,
                    CutFrequency: 50,
                    TransitionStyle: "simple cuts",
                    EffectUsage: "subtle",
                    SceneDuration: 5,
                    EditingPhilosophy: "invisible editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "YouTube",
                    SecondaryPlatforms: new List<string> { "Instagram" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 480,
                    AudienceDemographic: "Consumers, tech enthusiasts"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 60,
                    SuggestionVerbosity: "moderate",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "moderate",
                    CreativityLevel: 55,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        ),

        // Vlog/Personal Template
        new ProfileTemplate(
            TemplateId: "vlog-personal",
            Name: "Vlog/Personal",
            Description: "Casual, personal vlog-style content",
            Category: "vlog",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "vlog",
                Tone: new TonePreferences(
                    Formality: 15,
                    Energy: 70,
                    PersonalityTraits: new List<string> { "authentic", "relatable", "casual" },
                    CustomDescription: "Genuine and conversational"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "documentary",
                    ColorPalette: "natural",
                    ShotTypePreference: "medium shots",
                    CompositionStyle: "centered",
                    PacingPreference: "moderate",
                    BRollUsage: "moderate"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "indie", "acoustic", "upbeat" },
                    MusicEnergy: 60,
                    MusicProminence: "balanced",
                    SoundEffectsUsage: "minimal",
                    VoiceStyle: "warm",
                    AudioMixing: "voice-focused"
                ),
                Editing: new EditingPreferences(
                    Pacing: 60,
                    CutFrequency: 55,
                    TransitionStyle: "simple cuts",
                    EffectUsage: "subtle",
                    SceneDuration: 4,
                    EditingPhilosophy: "invisible editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "YouTube",
                    SecondaryPlatforms: new List<string> { "Instagram", "TikTok" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 600,
                    AudienceDemographic: "General audience, followers"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 55,
                    SuggestionVerbosity: "brief",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "moderate",
                    CreativityLevel: 65,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        ),

        // Marketing/Promotional Template
        new ProfileTemplate(
            TemplateId: "marketing-promotional",
            Name: "Marketing/Promotional",
            Description: "Engaging promotional content for products and services",
            Category: "marketing",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "promotional",
                Tone: new TonePreferences(
                    Formality: 55,
                    Energy: 75,
                    PersonalityTraits: new List<string> { "persuasive", "energetic", "professional" },
                    CustomDescription: "Compelling and action-oriented"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "cinematic",
                    ColorPalette: "vibrant",
                    ShotTypePreference: "dynamic",
                    CompositionStyle: "dynamic",
                    PacingPreference: "fast cuts",
                    BRollUsage: "heavy"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "upbeat", "modern", "energetic" },
                    MusicEnergy: 75,
                    MusicProminence: "prominent feature",
                    SoundEffectsUsage: "moderate",
                    VoiceStyle: "energetic",
                    AudioMixing: "balanced with music"
                ),
                Editing: new EditingPreferences(
                    Pacing: 75,
                    CutFrequency: 70,
                    TransitionStyle: "dynamic transitions",
                    EffectUsage: "moderate",
                    SceneDuration: 3,
                    EditingPhilosophy: "stylized editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "Instagram",
                    SecondaryPlatforms: new List<string> { "Facebook", "YouTube", "TikTok" },
                    AspectRatio: "1:1",
                    TargetDurationSeconds: 60,
                    AudienceDemographic: "Target customers, social media users"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 70,
                    SuggestionVerbosity: "moderate",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "proactive",
                    CreativityLevel: 70,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        ),

        // Documentary Template
        new ProfileTemplate(
            TemplateId: "documentary",
            Name: "Documentary",
            Description: "In-depth, cinematic documentary-style content",
            Category: "documentary",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "documentary",
                Tone: new TonePreferences(
                    Formality: 70,
                    Energy: 45,
                    PersonalityTraits: new List<string> { "thoughtful", "informative", "compelling" },
                    CustomDescription: "Thoughtful and engaging"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "cinematic",
                    ColorPalette: "natural",
                    ShotTypePreference: "wide shots",
                    CompositionStyle: "rule of thirds",
                    PacingPreference: "longer shots",
                    BRollUsage: "heavy"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "orchestral", "ambient", "cinematic" },
                    MusicEnergy: 40,
                    MusicProminence: "balanced",
                    SoundEffectsUsage: "moderate",
                    VoiceStyle: "authoritative",
                    AudioMixing: "balanced with music"
                ),
                Editing: new EditingPreferences(
                    Pacing: 35,
                    CutFrequency: 30,
                    TransitionStyle: "subtle fades",
                    EffectUsage: "subtle",
                    SceneDuration: 10,
                    EditingPhilosophy: "invisible editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "YouTube",
                    SecondaryPlatforms: new List<string> { "Vimeo" },
                    AspectRatio: "16:9",
                    TargetDurationSeconds: 1800,
                    AudienceDemographic: "General audience, documentary enthusiasts"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 55,
                    SuggestionVerbosity: "detailed",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "moderate",
                    CreativityLevel: 45,
                    OverridePermissions: new List<string> { "major_changes", "tone_changes" }
                )
            )
        ),

        // Quick Tips/Shorts Template
        new ProfileTemplate(
            TemplateId: "quick-tips-shorts",
            Name: "Quick Tips/Shorts",
            Description: "Fast-paced, vertical short-form content",
            Category: "shorts",
            DefaultPreferences: new ProfilePreferences(
                ProfileId: string.Empty,
                ContentType: "short",
                Tone: new TonePreferences(
                    Formality: 25,
                    Energy: 85,
                    PersonalityTraits: new List<string> { "direct", "energetic", "concise" },
                    CustomDescription: "Quick and punchy"
                ),
                Visual: new VisualPreferences(
                    Aesthetic: "vibrant",
                    ColorPalette: "vibrant",
                    ShotTypePreference: "close-ups",
                    CompositionStyle: "centered",
                    PacingPreference: "fast cuts",
                    BRollUsage: "minimal"
                ),
                Audio: new AudioPreferences(
                    MusicGenres: new List<string> { "upbeat", "trendy" },
                    MusicEnergy: 80,
                    MusicProminence: "prominent feature",
                    SoundEffectsUsage: "heavy",
                    VoiceStyle: "energetic",
                    AudioMixing: "voice-focused"
                ),
                Editing: new EditingPreferences(
                    Pacing: 90,
                    CutFrequency: 90,
                    TransitionStyle: "dynamic transitions",
                    EffectUsage: "prominent",
                    SceneDuration: 2,
                    EditingPhilosophy: "stylized editing"
                ),
                Platform: new PlatformPreferences(
                    PrimaryPlatform: "TikTok",
                    SecondaryPlatforms: new List<string> { "YouTube Shorts", "Instagram Reels" },
                    AspectRatio: "9:16",
                    TargetDurationSeconds: 30,
                    AudienceDemographic: "Mobile users, young audience"
                ),
                AIBehavior: new AIBehaviorSettings(
                    AssistanceLevel: 75,
                    SuggestionVerbosity: "brief",
                    AutoApplySuggestions: false,
                    SuggestionFrequency: "proactive",
                    CreativityLevel: 80,
                    OverridePermissions: new List<string> { "major_changes" }
                )
            )
        )
    };

    /// <summary>
    /// Get all available templates
    /// </summary>
    public static List<ProfileTemplate> GetAllTemplates()
    {
        return new List<ProfileTemplate>(_templates);
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    public static ProfileTemplate? GetTemplate(string templateId)
    {
        return _templates.FirstOrDefault(t => t.TemplateId == templateId);
    }

    /// <summary>
    /// Get templates by category
    /// </summary>
    public static List<ProfileTemplate> GetTemplatesByCategory(string category)
    {
        return _templates.Where(t => t.Category.Equals(category, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
