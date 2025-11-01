using System;
using System.Collections.Generic;

namespace Aura.Core.Models.UserPreferences;

/// <summary>
/// Extended audience profile with granular user-defined parameters
/// Provides complete control over audience characteristics beyond preset profiles
/// </summary>
public class CustomAudienceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? BaseProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCustom { get; set; } = true;
    
    // Granular Age Control
    public int MinAge { get; set; } = 18;
    public int MaxAge { get; set; } = 65;
    
    // Education Level with Custom Definitions
    public string EducationLevel { get; set; } = string.Empty;
    public string? EducationLevelDescription { get; set; }
    
    // Cultural Sensitivities (user-defined)
    public List<string> CulturalSensitivities { get; set; } = new();
    public List<string> TopicsToAvoid { get; set; } = new();
    public List<string> TopicsToEmphasize { get; set; } = new();
    
    // Language Complexity
    public int VocabularyLevel { get; set; } = 5; // 1-10 scale
    public string SentenceStructurePreference { get; set; } = "Mixed"; // Simple, Mixed, Complex
    public int ReadingLevel { get; set; } = 8; // Grade level
    
    // Content Appropriateness Thresholds (0-10 scale)
    public int ViolenceThreshold { get; set; } = 3;
    public int ProfanityThreshold { get; set; } = 2;
    public int SexualContentThreshold { get; set; } = 1;
    public int ControversialTopicsThreshold { get; set; } = 5;
    
    // Humor Style
    public string HumorStyle { get; set; } = "Moderate"; // None, Light, Moderate, Heavy
    public int SarcasmLevel { get; set; } = 3; // 0-10 scale
    public List<string> JokeTypes { get; set; } = new(); // Puns, Wordplay, Observational, etc.
    public List<string> CulturalHumorPreferences { get; set; } = new();
    
    // Formality Level (0-10 scale, 0=very casual, 10=extremely formal)
    public int FormalityLevel { get; set; } = 5;
    
    // Attention Span and Pacing
    public int AttentionSpanSeconds { get; set; } = 300; // Average attention span
    public string PacingPreference { get; set; } = "Medium"; // Slow, Medium, Fast, VeryFast
    public int InformationDensity { get; set; } = 5; // 1-10 scale
    
    // Technical Depth
    public int TechnicalDepthTolerance { get; set; } = 5; // 0-10 scale
    public int JargonAcceptability { get; set; } = 5; // 0-10 scale
    public List<string> FamiliarTechnicalTerms { get; set; } = new();
    
    // Emotional Tone
    public string EmotionalTone { get; set; } = "Neutral"; // Serious, Neutral, Uplifting, Dramatic, Inspirational
    public int EmotionalIntensity { get; set; } = 5; // 0-10 scale
    
    // Call-to-Action
    public int CtaAggressiveness { get; set; } = 5; // 0-10 scale (0=subtle, 10=very aggressive)
    public string CtaStyle { get; set; } = "Conversational"; // Direct, Conversational, Suggestive
    
    // Brand Voice
    public string? BrandVoiceGuidelines { get; set; }
    public List<string> BrandToneKeywords { get; set; } = new();
    public string? BrandPersonality { get; set; }
    
    // Metadata
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
