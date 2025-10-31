using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Audience;

/// <summary>
/// Comprehensive profile of target audience for content adaptation
/// </summary>
public record AudienceProfile
{
    /// <summary>
    /// Unique identifier for the audience profile
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Target education level (high school, undergraduate, graduate, expert)
    /// </summary>
    public EducationLevel EducationLevel { get; init; } = EducationLevel.Undergraduate;

    /// <summary>
    /// Expertise level in the topic domain (novice, intermediate, advanced, expert)
    /// </summary>
    public ExpertiseLevel ExpertiseLevel { get; init; } = ExpertiseLevel.Intermediate;

    /// <summary>
    /// Primary age range of audience
    /// </summary>
    public AgeRange AgeRange { get; init; } = AgeRange.Adult25to34;

    /// <summary>
    /// Geographic/cultural region for cultural references
    /// </summary>
    public string? GeographicRegion { get; init; }

    /// <summary>
    /// Professional/interest domain (e.g., "technology", "healthcare", "education")
    /// </summary>
    public string? ProfessionalDomain { get; init; }

    /// <summary>
    /// Specific interests and topics the audience cares about
    /// </summary>
    public List<string> Interests { get; init; } = new();

    /// <summary>
    /// Preferred formality level (casual, conversational, professional, academic)
    /// </summary>
    public FormalityLevel FormalityLevel { get; init; } = FormalityLevel.Conversational;

    /// <summary>
    /// Preferred content energy level (low, medium, high)
    /// </summary>
    public EnergyLevel EnergyLevel { get; init; } = EnergyLevel.Medium;

    /// <summary>
    /// Average attention span in seconds
    /// </summary>
    public int AttentionSpanSeconds { get; init; } = 180;

    /// <summary>
    /// Learning style preferences
    /// </summary>
    public LearningStyle LearningStyle { get; init; } = LearningStyle.Balanced;

    /// <summary>
    /// Cultural considerations and sensitivities
    /// </summary>
    public List<string> CulturalConsiderations { get; init; } = new();

    /// <summary>
    /// Primary language
    /// </summary>
    public string Language { get; init; } = "English";

    /// <summary>
    /// Whether audience prefers technical jargon or plain language
    /// </summary>
    public bool PrefersTechnicalLanguage { get; init; } = false;

    /// <summary>
    /// Cognitive load capacity (0-100, higher means can handle more complexity)
    /// </summary>
    public int CognitiveLoadCapacity { get; init; } = 70;
}

/// <summary>
/// Education level categories
/// </summary>
public enum EducationLevel
{
    HighSchool,
    Undergraduate,
    Graduate,
    Expert
}

/// <summary>
/// Expertise level in the topic domain
/// </summary>
public enum ExpertiseLevel
{
    Novice,
    Intermediate,
    Advanced,
    Expert
}

/// <summary>
/// Age range categories
/// </summary>
public enum AgeRange
{
    Teen13to17,
    YoungAdult18to24,
    Adult25to34,
    Adult35to44,
    Adult45to54,
    Senior55Plus
}

/// <summary>
/// Content formality levels
/// </summary>
public enum FormalityLevel
{
    Casual,
    Conversational,
    Professional,
    Academic
}

/// <summary>
/// Content energy levels
/// </summary>
public enum EnergyLevel
{
    Low,
    Medium,
    High
}

/// <summary>
/// Learning style preferences
/// </summary>
public enum LearningStyle
{
    Visual,
    Auditory,
    Balanced,
    Detail
}
