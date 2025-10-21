using System;
using System.Collections.Generic;

namespace Aura.Core.AI.Aesthetics;

/// <summary>
/// Common models and enums for visual aesthetics enhancement
/// </summary>

public enum ColorMood
{
    Cinematic,
    Vibrant,
    Warm,
    Cool,
    Dramatic,
    Natural,
    Vintage,
    HighContrast,
    LowKey,
    HighKey
}

public enum TimeOfDay
{
    Dawn,
    Morning,
    Midday,
    Afternoon,
    Sunset,
    Dusk,
    Night,
    Unknown
}

public enum QualityLevel
{
    Excellent,
    Good,
    Acceptable,
    Poor,
    Unacceptable
}

public enum CompositionRule
{
    RuleOfThirds,
    GoldenRatio,
    CenterComposition,
    SymmetricalBalance,
    LeadingLines,
    FramingElements,
    NegativeSpace
}

public class ColorGradingProfile
{
    public string Name { get; set; } = string.Empty;
    public ColorMood Mood { get; set; }
    public Dictionary<string, float> ColorAdjustments { get; set; } = new();
    public float Saturation { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;
    public float Brightness { get; set; } = 1.0f;
    public float Temperature { get; set; } = 0.0f;
    public float Tint { get; set; } = 0.0f;
}

public class CompositionAnalysisResult
{
    public CompositionRule SuggestedRule { get; set; }
    public float CompositionScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public Point? FocalPoint { get; set; }
    public Rectangle? SuggestedCrop { get; set; }
    public float BalanceScore { get; set; }
}

public class Point
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class Rectangle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public class QualityMetrics
{
    public float Resolution { get; set; }
    public float Sharpness { get; set; }
    public float NoiseLevel { get; set; }
    public float CompressionQuality { get; set; }
    public float ColorAccuracy { get; set; }
    public QualityLevel OverallQuality { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class VisualCoherenceReport
{
    public float StyleConsistencyScore { get; set; }
    public float ColorConsistencyScore { get; set; }
    public float LightingConsistencyScore { get; set; }
    public float OverallCoherenceScore { get; set; }
    public List<string> Inconsistencies { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class EnhancementParameters
{
    public bool AutoColorGrade { get; set; } = true;
    public bool AutoComposition { get; set; } = true;
    public bool EnhanceQuality { get; set; } = true;
    public bool EnsureCoherence { get; set; } = true;
    public float IntensityLevel { get; set; } = 0.7f; // 0.0 to 1.0
    public ColorMood? PreferredMood { get; set; }
}

public class EnhancementResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ColorGradingProfile? AppliedColorGrading { get; set; }
    public CompositionAnalysisResult? CompositionAnalysis { get; set; }
    public QualityMetrics? QualityMetrics { get; set; }
    public VisualCoherenceReport? CoherenceReport { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SceneVisualContext
{
    public int SceneIndex { get; set; }
    public TimeOfDay TimeOfDay { get; set; }
    public ColorMood DominantMood { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, float> ColorHistogram { get; set; } = new();
}
