using System;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Configuration for voice enhancement processing
/// </summary>
public record VoiceEnhancementConfig
{
    /// <summary>
    /// Enable noise reduction
    /// </summary>
    public bool EnableNoiseReduction { get; init; } = true;

    /// <summary>
    /// Noise reduction strength (0.0 - 1.0)
    /// </summary>
    public double NoiseReductionStrength { get; init; } = 0.7;

    /// <summary>
    /// Enable voice equalization
    /// </summary>
    public bool EnableEqualization { get; init; } = true;

    /// <summary>
    /// Equalization preset
    /// </summary>
    public EqualizationPreset EqualizationPreset { get; init; } = EqualizationPreset.Balanced;

    /// <summary>
    /// Enable prosody adjustment
    /// </summary>
    public bool EnableProsodyAdjustment { get; init; } = false;

    /// <summary>
    /// Prosody settings
    /// </summary>
    public ProsodySettings? Prosody { get; init; }

    /// <summary>
    /// Enable emotional tone enhancement
    /// </summary>
    public bool EnableEmotionEnhancement { get; init; } = false;

    /// <summary>
    /// Target emotion
    /// </summary>
    public EmotionTarget? TargetEmotion { get; init; }
}

/// <summary>
/// Prosody adjustment settings
/// </summary>
public record ProsodySettings
{
    /// <summary>
    /// Pitch adjustment in semitones (-12 to +12)
    /// </summary>
    public double PitchShift { get; init; } = 0.0;

    /// <summary>
    /// Rate/speed multiplier (0.5 to 2.0)
    /// </summary>
    public double RateMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Emphasis level (0.0 to 1.0)
    /// </summary>
    public double EmphasisLevel { get; init; } = 0.5;

    /// <summary>
    /// Volume adjustment in dB (-20 to +20)
    /// </summary>
    public double VolumeAdjustment { get; init; } = 0.0;

    /// <summary>
    /// Pause duration multiplier (0.5 to 2.0)
    /// </summary>
    public double PauseDurationMultiplier { get; init; } = 1.0;
}

/// <summary>
/// Emotion detection and targeting
/// </summary>
public record EmotionTarget
{
    /// <summary>
    /// Target emotion type
    /// </summary>
    public EmotionType Emotion { get; init; } = EmotionType.Neutral;

    /// <summary>
    /// Intensity of the emotion (0.0 to 1.0)
    /// </summary>
    public double Intensity { get; init; } = 0.5;
}

/// <summary>
/// Voice equalization presets
/// </summary>
public enum EqualizationPreset
{
    /// <summary>
    /// Flat response, no EQ
    /// </summary>
    Flat,

    /// <summary>
    /// Balanced, slight enhancement
    /// </summary>
    Balanced,

    /// <summary>
    /// Warm, emphasis on low-mid frequencies
    /// </summary>
    Warm,

    /// <summary>
    /// Bright, emphasis on high frequencies
    /// </summary>
    Bright,

    /// <summary>
    /// Radio/podcast optimized
    /// </summary>
    Broadcast,

    /// <summary>
    /// Phone/low-bandwidth optimized
    /// </summary>
    Telephone,

    /// <summary>
    /// Custom EQ settings
    /// </summary>
    Custom
}

/// <summary>
/// Emotion types for voice synthesis
/// </summary>
public enum EmotionType
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Excited,
    Calm,
    Fearful,
    Surprised,
    Confident,
    Empathetic
}

/// <summary>
/// Result of voice enhancement processing
/// </summary>
public record VoiceEnhancementResult
{
    /// <summary>
    /// Path to the enhanced audio file
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Processing duration in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Quality metrics
    /// </summary>
    public VoiceQualityMetrics? QualityMetrics { get; init; }

    /// <summary>
    /// Any warnings or messages from processing
    /// </summary>
    public string[] Messages { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Voice quality metrics
/// </summary>
public record VoiceQualityMetrics
{
    /// <summary>
    /// Signal-to-noise ratio (dB)
    /// </summary>
    public double SignalToNoiseRatio { get; init; }

    /// <summary>
    /// Peak level (dB)
    /// </summary>
    public double PeakLevel { get; init; }

    /// <summary>
    /// RMS level (dB)
    /// </summary>
    public double RmsLevel { get; init; }

    /// <summary>
    /// LUFS loudness
    /// </summary>
    public double Lufs { get; init; }

    /// <summary>
    /// Detected emotion
    /// </summary>
    public EmotionType? DetectedEmotion { get; init; }

    /// <summary>
    /// Emotion confidence (0.0 to 1.0)
    /// </summary>
    public double EmotionConfidence { get; init; }

    /// <summary>
    /// Clarity score (0.0 to 1.0)
    /// </summary>
    public double ClarityScore { get; init; }
}

/// <summary>
/// Voice profile for consistent voice characteristics
/// </summary>
public record VoiceProfile
{
    /// <summary>
    /// Unique identifier for the profile
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Profile name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Voice descriptor
    /// </summary>
    public required VoiceDescriptor Voice { get; init; }

    /// <summary>
    /// Enhancement configuration
    /// </summary>
    public VoiceEnhancementConfig? EnhancementConfig { get; init; }

    /// <summary>
    /// Description of the profile
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public string[] Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;
}
