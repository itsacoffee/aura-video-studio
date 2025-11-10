using System;
using System.Globalization;
using Aura.Core.Services.FFmpeg.Filters;

namespace Aura.Core.Models.VideoEffects;

/// <summary>
/// Transition effect for smooth transitions between clips
/// </summary>
public class TransitionEffect : VideoEffect
{
    /// <summary>
    /// Type of transition
    /// </summary>
    public TransitionBuilder.TransitionType TransitionType { get; set; } = TransitionBuilder.TransitionType.Fade;

    /// <summary>
    /// Easing function for the transition
    /// </summary>
    public EasingFunction Easing { get; set; } = EasingFunction.Linear;

    /// <summary>
    /// Custom transition color (for fade transitions)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Offset time for the transition (where it starts relative to clip end)
    /// </summary>
    public double Offset { get; set; }

    public TransitionEffect()
    {
        Type = EffectType.Transition;
        Name = "Transition";
        Description = "Smooth transition between video clips";
    }

    public override string ToFFmpegFilter()
    {
        var durationStr = Duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = Offset.ToString("F3", CultureInfo.InvariantCulture);
        var transitionName = TransitionType.ToString().ToLowerInvariant();
        var easingName = Easing.ToString().ToLowerInvariant();

        // Build the xfade filter with all parameters
        var filter = $"xfade=transition={transitionName}:duration={durationStr}:offset={offsetStr}";

        // Add easing if not linear
        if (Easing != EasingFunction.Linear)
        {
            filter += $":easing={easingName}";
        }

        // Apply intensity if not full
        if (Math.Abs(Intensity - 1.0) > 0.001)
        {
            filter = $"{filter}[xfaded];[xfaded]colorchannelmixer=aa={Intensity.ToString(CultureInfo.InvariantCulture)}";
        }

        return filter;
    }
}

/// <summary>
/// 3D transition effect with depth
/// </summary>
public class Transition3DEffect : VideoEffect
{
    /// <summary>
    /// 3D transition style
    /// </summary>
    public enum Style3D
    {
        Cube,
        Flip,
        Rotate,
        Zoom,
        Door,
        Spin
    }

    /// <summary>
    /// 3D style for the transition
    /// </summary>
    public Style3D Style { get; set; } = Style3D.Flip;

    /// <summary>
    /// Rotation axis (x, y, z)
    /// </summary>
    public string Axis { get; set; } = "y";

    /// <summary>
    /// Perspective depth
    /// </summary>
    public double Perspective { get; set; } = 1000.0;

    public Transition3DEffect()
    {
        Type = EffectType.Transition;
        Name = "3D Transition";
        Description = "3D transition with depth effects";
        Category = EffectCategory.Modern;
    }

    public override string ToFFmpegFilter()
    {
        // For 3D transitions, we use custom expressions with perspective transform
        var duration = Duration.ToString(CultureInfo.InvariantCulture);
        
        return Style switch
        {
            Style3D.Flip => $"perspective=sense=destination:eval=frame:interpolation=linear:x='W/2+W/2*sin((t/{duration})*PI)':y='H/2':w='W':h='H'",
            Style3D.Rotate => $"rotate=a='2*PI*t/{duration}':fillcolor=black",
            Style3D.Zoom => $"zoompan=z='if(lte(zoom,1),1,max(1,zoom-0.01))':d={duration}:s=1920x1080",
            _ => $"xfade=transition=fade:duration={duration}:offset={StartTime.ToString(CultureInfo.InvariantCulture)}"
        };
    }
}
