using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentSafety;

/// <summary>
/// Comprehensive safety policy with granular controls
/// </summary>
public class SafetyPolicy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsEnabled { get; set; } = true;
    public bool AllowUserOverride { get; set; } = true;
    public SafetyPolicyPreset Preset { get; set; } = SafetyPolicyPreset.Moderate;
    
    public Dictionary<SafetyCategoryType, SafetyCategory> Categories { get; set; } = new();
    public List<KeywordRule> KeywordRules { get; set; } = new();
    public List<TopicFilter> TopicFilters { get; set; } = new();
    public BrandSafetySettings? BrandSafety { get; set; }
    public AgeAppropriatenessSettings? AgeSettings { get; set; }
    public CulturalSensitivitySettings? CulturalSettings { get; set; }
    public ComplianceSettings? ComplianceSettings { get; set; }
    
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Preset safety policy levels
/// </summary>
public enum SafetyPolicyPreset
{
    Unrestricted = 0,
    Minimal = 1,
    Moderate = 2,
    Strict = 3,
    Custom = 4
}

/// <summary>
/// Individual safety category with threshold and action rules
/// </summary>
public class SafetyCategory
{
    public SafetyCategoryType Type { get; set; }
    public int Threshold { get; set; }
    public bool IsEnabled { get; set; } = true;
    public SafetyAction DefaultAction { get; set; } = SafetyAction.Warn;
    public Dictionary<int, SafetyAction> SeverityActions { get; set; } = new();
    public string? CustomGuidelines { get; set; }
}

/// <summary>
/// Types of safety categories
/// </summary>
public enum SafetyCategoryType
{
    Profanity = 0,
    Violence = 1,
    SexualContent = 2,
    HateSpeech = 3,
    DrugAlcohol = 4,
    ControversialTopics = 5,
    Copyright = 6,
    SelfHarm = 7,
    GraphicImagery = 8,
    Misinformation = 9
}

/// <summary>
/// Actions to take when content is flagged
/// </summary>
public enum SafetyAction
{
    Block = 0,
    Warn = 1,
    RequireReview = 2,
    AutoFix = 3,
    AddDisclaimer = 4,
    LogOnly = 5
}

/// <summary>
/// Keyword-based filtering rule
/// </summary>
public class KeywordRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Keyword { get; set; } = string.Empty;
    public KeywordMatchType MatchType { get; set; } = KeywordMatchType.WholeWord;
    public bool IsCaseSensitive { get; set; }
    public SafetyAction Action { get; set; } = SafetyAction.Warn;
    public string? Replacement { get; set; }
    public List<string> ContextExceptions { get; set; } = new();
    public bool IsRegex { get; set; }
}

/// <summary>
/// How to match keywords
/// </summary>
public enum KeywordMatchType
{
    WholeWord = 0,
    Substring = 1,
    Regex = 2
}

/// <summary>
/// Topic-based filtering
/// </summary>
public class TopicFilter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Topic { get; set; } = string.Empty;
    public bool IsBlocked { get; set; } = true;
    public double ConfidenceThreshold { get; set; } = 0.7;
    public SafetyAction Action { get; set; } = SafetyAction.Warn;
    public List<string> Subtopics { get; set; } = new();
    public List<string> AllowedContexts { get; set; } = new();
}

/// <summary>
/// Brand safety and compliance settings
/// </summary>
public class BrandSafetySettings
{
    public List<string> RequiredKeywords { get; set; } = new();
    public List<string> BannedCompetitors { get; set; } = new();
    public List<string> BrandTerminology { get; set; } = new();
    public string? BrandVoiceGuidelines { get; set; }
    public List<string> RequiredDisclaimers { get; set; } = new();
    public int MinBrandVoiceScore { get; set; } = 70;
}

/// <summary>
/// Age appropriateness controls
/// </summary>
public class AgeAppropriatenessSettings
{
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; } = 100;
    public ContentRating TargetRating { get; set; } = ContentRating.General;
    public bool RequireParentalGuidance { get; set; }
    public List<string> AgeSpecificRestrictions { get; set; } = new();
}

/// <summary>
/// Content rating systems
/// </summary>
public enum ContentRating
{
    General = 0,
    ParentalGuidance = 1,
    Teen = 2,
    Mature = 3,
    Adult = 4
}

/// <summary>
/// Cultural sensitivity settings
/// </summary>
public class CulturalSensitivitySettings
{
    public List<string> TargetRegions { get; set; } = new();
    public Dictionary<string, List<string>> CulturalTaboos { get; set; } = new();
    public bool AvoidStereotypes { get; set; } = true;
    public bool RequireInclusiveLanguage { get; set; } = true;
    public List<string> ReligiousSensitivities { get; set; } = new();
}

/// <summary>
/// Legal and compliance settings
/// </summary>
public class ComplianceSettings
{
    public List<string> RequiredDisclosures { get; set; } = new();
    public bool CoppaCompliant { get; set; }
    public bool GdprCompliant { get; set; }
    public bool FtcCompliant { get; set; }
    public List<string> IndustryRegulations { get; set; } = new();
    public Dictionary<string, string> AutoDisclosures { get; set; } = new();
}
