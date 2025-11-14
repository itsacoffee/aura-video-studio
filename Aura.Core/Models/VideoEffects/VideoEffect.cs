using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.VideoEffects;

/// <summary>
/// Types of video effects
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectType
{
    Transition,
    Filter,
    TextAnimation,
    Overlay,
    Transform,
    ColorCorrection,
    AudioEffect,
    Composite
}

/// <summary>
/// Categories of video effects
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectCategory
{
    Basic,
    Artistic,
    ColorGrading,
    Blur,
    Vintage,
    Modern,
    Cinematic,
    Custom
}

/// <summary>
/// Easing functions for animations
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EasingFunction
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    Bounce,
    Elastic
}

/// <summary>
/// Base class for all video effects
/// </summary>
public abstract class VideoEffect
{
    /// <summary>
    /// Unique identifier for the effect
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the effect
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of what the effect does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of effect
    /// </summary>
    public EffectType Type { get; set; }

    /// <summary>
    /// Category for organizing effects
    /// </summary>
    public EffectCategory Category { get; set; } = EffectCategory.Basic;

    /// <summary>
    /// Start time in the timeline (seconds)
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// Duration of the effect (seconds)
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// Effect intensity/opacity (0.0 to 1.0)
    /// </summary>
    public double Intensity { get; set; } = 1.0;

    /// <summary>
    /// Whether the effect is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Layer/track index for the effect
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Keyframes for animating effect parameters
    /// </summary>
    public List<Keyframe> Keyframes { get; set; } = new();

    /// <summary>
    /// Custom parameters for the effect
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Tags for searching and filtering effects
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Generate FFmpeg filter string for this effect
    /// </summary>
    public abstract string ToFFmpegFilter();

    /// <summary>
    /// Validate effect parameters
    /// </summary>
    public virtual bool Validate(out string? errorMessage)
    {
        if (Duration <= 0)
        {
            errorMessage = "Duration must be greater than 0";
            return false;
        }

        if (StartTime < 0)
        {
            errorMessage = "Start time must be non-negative";
            return false;
        }

        if (Intensity < 0 || Intensity > 1)
        {
            errorMessage = "Intensity must be between 0 and 1";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// Keyframe for animating effect parameters
/// </summary>
public class Keyframe
{
    /// <summary>
    /// Time in seconds within the effect duration
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// Parameter name being animated
    /// </summary>
    public required string ParameterName { get; set; }

    /// <summary>
    /// Value at this keyframe
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    /// Easing function to use to reach this keyframe
    /// </summary>
    public EasingFunction Easing { get; set; } = EasingFunction.Linear;

    /// <summary>
    /// Optional interpolation mode
    /// </summary>
    public string? InterpolationMode { get; set; }
}

/// <summary>
/// Preset configuration for a video effect
/// </summary>
public class EffectPreset
{
    /// <summary>
    /// Unique identifier for the preset
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the preset
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the preset
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of the preset
    /// </summary>
    public EffectCategory Category { get; set; }

    /// <summary>
    /// Thumbnail image URL for the preset
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Whether this is a built-in preset
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Whether this preset is favorited by the user
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Tags for searching
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Effects included in this preset
    /// </summary>
    public List<VideoEffect> Effects { get; set; } = new();

    /// <summary>
    /// Preset parameters that can be adjusted
    /// </summary>
    public Dictionary<string, EffectParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Usage count for analytics
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Parameter definition for an effect
/// </summary>
public class EffectParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Display label
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Parameter description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parameter type (number, color, boolean, text, etc.)
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Default value
    /// </summary>
    public required object DefaultValue { get; set; }

    /// <summary>
    /// Current value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Minimum value (for numeric parameters)
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Maximum value (for numeric parameters)
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Step size (for numeric parameters)
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Possible values (for enum parameters)
    /// </summary>
    public List<string>? Options { get; set; }

    /// <summary>
    /// Whether this parameter can be keyframed
    /// </summary>
    public bool Animatable { get; set; } = true;

    /// <summary>
    /// Unit label (e.g., "px", "%", "deg")
    /// </summary>
    public string? Unit { get; set; }
}

/// <summary>
/// Effect stack containing multiple effects applied in order
/// </summary>
public class EffectStack
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Effects in the stack (applied in order)
    /// </summary>
    public List<VideoEffect> Effects { get; set; } = new();

    /// <summary>
    /// Overall blend mode for the stack
    /// </summary>
    public string BlendMode { get; set; } = "normal";

    /// <summary>
    /// Overall opacity for the entire stack (0.0 to 1.0)
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// Whether the stack is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Generate combined FFmpeg filter for all effects
    /// </summary>
    public string ToFFmpegFilter()
    {
        var filters = new List<string>();
        
        foreach (var effect in Effects)
        {
            if (effect.Enabled)
            {
                filters.Add(effect.ToFFmpegFilter());
            }
        }

        return string.Join(",", filters);
    }
}
