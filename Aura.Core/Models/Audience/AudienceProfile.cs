using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audience;

/// <summary>
/// Comprehensive audience profile with rich demographic and psychographic characteristics
/// Replaces simple string-based audience specification with detailed, structured data
/// </summary>
public class AudienceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;

    // Demographics
    public AgeRange? AgeRange { get; set; }
    public EducationLevel? EducationLevel { get; set; }
    public string? Profession { get; set; }
    public string? Industry { get; set; }
    public ExpertiseLevel? ExpertiseLevel { get; set; }
    public IncomeBracket? IncomeBracket { get; set; }
    public GeographicRegion? GeographicRegion { get; set; }
    public LanguageFluency? LanguageFluency { get; set; }

    // Psychographics
    public List<string> Interests { get; set; } = new();
    public List<string> PainPoints { get; set; } = new();
    public List<string> Motivations { get; set; } = new();
    public CulturalBackground? CulturalBackground { get; set; }

    // Learning & Content Preferences
    public LearningStyle? PreferredLearningStyle { get; set; }
    public AttentionSpan? AttentionSpan { get; set; }
    public TechnicalComfort? TechnicalComfort { get; set; }
    public AccessibilityNeeds? AccessibilityNeeds { get; set; }

    // Metadata
    public string? Description { get; set; }
    public bool IsTemplate { get; set; }
    public List<string> Tags { get; set; } = new();
    
    // Organization and favorites
    public bool IsFavorite { get; set; }
    public string? FolderPath { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Age range with content filtering
/// </summary>
public class AgeRange
{
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ContentRating ContentRating { get; set; }

    public static readonly AgeRange Children = new() { MinAge = 0, MaxAge = 12, DisplayName = "Children (<13)", ContentRating = ContentRating.ChildSafe };
    public static readonly AgeRange Teens = new() { MinAge = 13, MaxAge = 17, DisplayName = "Teens (13-17)", ContentRating = ContentRating.TeenAppropriate };
    public static readonly AgeRange YoungAdults = new() { MinAge = 18, MaxAge = 24, DisplayName = "Young Adults (18-24)", ContentRating = ContentRating.Adult };
    public static readonly AgeRange Adults = new() { MinAge = 25, MaxAge = 34, DisplayName = "Adults (25-34)", ContentRating = ContentRating.Adult };
    public static readonly AgeRange MiddleAged = new() { MinAge = 35, MaxAge = 54, DisplayName = "Middle-Aged (35-54)", ContentRating = ContentRating.Adult };
    public static readonly AgeRange Seniors = new() { MinAge = 55, MaxAge = 120, DisplayName = "Seniors (55+)", ContentRating = ContentRating.Adult };
}

/// <summary>
/// Content rating for age-appropriate filtering
/// </summary>
public enum ContentRating
{
    ChildSafe,
    TeenAppropriate,
    Adult
}

/// <summary>
/// Education level
/// </summary>
public enum EducationLevel
{
    HighSchool,
    SomeCollege,
    AssociateDegree,
    BachelorDegree,
    MasterDegree,
    Doctorate,
    Vocational,
    SelfTaught,
    InProgress
}

/// <summary>
/// Expertise level in the subject matter
/// </summary>
public enum ExpertiseLevel
{
    CompleteBeginner,
    Novice,
    Intermediate,
    Advanced,
    Expert,
    Professional
}

/// <summary>
/// Income bracket (optional, for purchase-focused content)
/// </summary>
public enum IncomeBracket
{
    NotSpecified,
    LowIncome,
    MiddleIncome,
    UpperMiddleIncome,
    HighIncome
}

/// <summary>
/// Geographic region
/// </summary>
public enum GeographicRegion
{
    Global,
    NorthAmerica,
    Europe,
    Asia,
    LatinAmerica,
    MiddleEast,
    Africa,
    Oceania
}

/// <summary>
/// Language fluency level
/// </summary>
public class LanguageFluency
{
    public string Language { get; set; } = "English";
    public FluencyLevel Level { get; set; }
}

/// <summary>
/// Fluency level
/// </summary>
public enum FluencyLevel
{
    Native,
    Fluent,
    Intermediate,
    Beginner
}

/// <summary>
/// Cultural background and sensitivities
/// </summary>
public class CulturalBackground
{
    public List<string> Sensitivities { get; set; } = new();
    public List<string> TabooTopics { get; set; } = new();
    public CommunicationStyle PreferredCommunicationStyle { get; set; }
}

/// <summary>
/// Communication style preferences
/// </summary>
public enum CommunicationStyle
{
    Direct,
    Indirect,
    Formal,
    Casual,
    Humorous,
    Professional
}

/// <summary>
/// Learning style preferences
/// </summary>
public enum LearningStyle
{
    Visual,
    Auditory,
    Kinesthetic,
    ReadingWriting,
    Multimodal
}

/// <summary>
/// Attention span characteristics
/// </summary>
public class AttentionSpan
{
    public TimeSpan PreferredDuration { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public static readonly AttentionSpan Short = new() { PreferredDuration = TimeSpan.FromMinutes(2), DisplayName = "Short (1-3 min)" };
    public static readonly AttentionSpan Medium = new() { PreferredDuration = TimeSpan.FromMinutes(6), DisplayName = "Medium (3-10 min)" };
    public static readonly AttentionSpan Long = new() { PreferredDuration = TimeSpan.FromMinutes(15), DisplayName = "Long (10+ min)" };
}

/// <summary>
/// Technical comfort level
/// </summary>
public enum TechnicalComfort
{
    NonTechnical,
    BasicUser,
    Moderate,
    TechSavvy,
    Expert
}

/// <summary>
/// Accessibility needs
/// </summary>
public class AccessibilityNeeds
{
    public bool RequiresCaptions { get; set; }
    public bool RequiresAudioDescriptions { get; set; }
    public bool RequiresHighContrast { get; set; }
    public bool RequiresSimplifiedLanguage { get; set; }
    public bool RequiresLargeText { get; set; }
}
