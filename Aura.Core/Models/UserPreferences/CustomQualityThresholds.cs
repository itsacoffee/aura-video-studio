using System;
using System.Collections.Generic;

namespace Aura.Core.Models.UserPreferences;

/// <summary>
/// User-defined quality thresholds and validation rules
/// Allows customization of what is considered acceptable quality
/// </summary>
public class CustomQualityThresholds
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Skip Validation Option
    public bool SkipValidation { get; set; } = false;
    
    // Script Quality
    public int MinScriptWordCount { get; set; } = 50;
    public int MaxScriptWordCount { get; set; } = 5000;
    public int AcceptableGrammarErrors { get; set; } = 3;
    public List<string> RequiredKeywords { get; set; } = new();
    public List<string> ExcludedKeywords { get; set; } = new();
    
    // Image Quality
    public int MinImageResolutionWidth { get; set; } = 1280;
    public int MinImageResolutionHeight { get; set; } = 720;
    public double MinImageClarityScore { get; set; } = 0.7; // 0-1 scale
    public bool AllowLowQualityImages { get; set; } = false;
    
    // Audio Quality
    public int MinAudioBitrate { get; set; } = 128; // kbps
    public double MinAudioClarity { get; set; } = 0.8; // 0-1 scale
    public double MaxBackgroundNoise { get; set; } = 0.2; // 0-1 scale
    public bool RequireStereo { get; set; } = false;
    
    // Subtitle Accuracy
    public double MinSubtitleAccuracy { get; set; } = 0.95; // 0-1 scale
    public bool RequireSubtitles { get; set; } = false;
    
    // Brand Compliance
    public List<BrandComplianceRule> BrandComplianceRules { get; set; } = new();
    
    // Custom Quality Metrics
    public Dictionary<string, double> CustomMetricThresholds { get; set; } = new();
    
    // Scoring Weights (what matters most to the user)
    public QualityWeights Weights { get; set; } = new();
    
    // Metadata
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Brand compliance rule
/// </summary>
public class BrandComplianceRule
{
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty; // ColorPalette, ToneCheck, TerminologyCheck
    public Dictionary<string, string> Parameters { get; set; } = new();
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Quality scoring weights (sum should equal 1.0)
/// </summary>
public class QualityWeights
{
    public double ScriptQuality { get; set; } = 0.3;
    public double VisualQuality { get; set; } = 0.25;
    public double AudioQuality { get; set; } = 0.25;
    public double BrandCompliance { get; set; } = 0.1;
    public double Engagement { get; set; } = 0.1;
}

/// <summary>
/// Visual style customization
/// </summary>
public class CustomVisualStyle
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Color Palette
    public List<string> ColorPalette { get; set; } = new(); // Hex codes
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? AccentColor { get; set; }
    
    // Visual Complexity (0-10 scale)
    public int VisualComplexity { get; set; } = 5;
    
    // Artistic Style
    public string ArtisticStyle { get; set; } = "Photorealistic"; // Photorealistic, Illustrated, 3D, Abstract, Minimalist
    
    // Composition Rules
    public string CompositionPreference { get; set; } = "RuleOfThirds"; // RuleOfThirds, Centered, Dynamic, Asymmetric
    
    // Lighting
    public string LightingPreference { get; set; } = "Natural"; // Bright, Moody, Dramatic, Natural, Studio
    
    // Camera Angles
    public List<string> PreferredCameraAngles { get; set; } = new(); // EyeLevel, High, Low, Dutch, BirdsEye
    
    // Text Overlays
    public TextOverlayStyle TextOverlay { get; set; } = new();
    
    // Transitions
    public string TransitionStyle { get; set; } = "Dissolve"; // Cut, Dissolve, Wipe, Fade, Zoom
    public int TransitionDurationMs { get; set; } = 500;
    
    // Reference Images
    public List<string> ReferenceImagePaths { get; set; } = new();
    
    // Metadata
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
    public int UsageCount { get; set; }
}

/// <summary>
/// Text overlay style configuration
/// </summary>
public class TextOverlayStyle
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 48;
    public string FontColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 0.7;
    public string Animation { get; set; } = "FadeIn"; // None, FadeIn, SlideIn, TypeWriter
}
