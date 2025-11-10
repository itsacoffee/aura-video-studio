using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aura.Core.Services.FFmpeg.Filters;

/// <summary>
/// Builder for creating FFmpeg video transition filters
/// </summary>
public class TransitionBuilder
{
    /// <summary>
    /// Available transition types
    /// </summary>
    public enum TransitionType
    {
        Fade,
        Dissolve,
        WipeLeft,
        WipeRight,
        WipeUp,
        WipeDown,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        CircleOpen,
        CircleClose,
        Pixelize,
        Wipeleft,
        Wiperight,
        Wipeup,
        Wipedown,
        Slideleft,
        Slideright,
        Slideup,
        Slidedown,
        Circlecrop,
        Rectcrop,
        Distance,
        Fadeblack,
        Fadewhite,
        Radial,
        Smoothleft,
        Smoothright,
        Smoothup,
        Smoothdown,
        Vertopen,
        Vertclose,
        Horzopen,
        Horzclose,
        Diagtl,
        Diagtr,
        Diagbl,
        Diagbr
    }

    /// <summary>
    /// Build a crossfade transition filter
    /// </summary>
    public static string BuildCrossfade(double duration, double offset, TransitionType type = TransitionType.Fade)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        var transitionName = type.ToString().ToLowerInvariant();

        return $"xfade=transition={transitionName}:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a custom xfade transition with easing
    /// </summary>
    public static string BuildCustomTransition(
        double duration,
        double offset,
        TransitionType type,
        string easing = "linear")
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        var transitionName = type.ToString().ToLowerInvariant();

        // xfade filter with transition type
        return $"xfade=transition={transitionName}:duration={durationStr}:offset={offsetStr}:easing={easing}";
    }

    /// <summary>
    /// Build a fade in transition
    /// </summary>
    public static string BuildFadeIn(double duration, string type = "in", string color = "black")
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        return $"fade=t={type}:st=0:d={durationStr}:color={color}";
    }

    /// <summary>
    /// Build a fade out transition
    /// </summary>
    public static string BuildFadeOut(double startTime, double duration, string color = "black")
    {
        var startStr = startTime.ToString("F3", CultureInfo.InvariantCulture);
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        return $"fade=t=out:st={startStr}:d={durationStr}:color={color}";
    }

    /// <summary>
    /// Build a wipe transition with direction
    /// </summary>
    public static string BuildWipe(double duration, double offset, string direction = "right")
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        
        var transitionType = direction.ToLowerInvariant() switch
        {
            "left" => "wipeleft",
            "right" => "wiperight",
            "up" => "wipeup",
            "down" => "wipedown",
            _ => "wiperight"
        };

        return $"xfade=transition={transitionType}:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a slide transition with direction
    /// </summary>
    public static string BuildSlide(double duration, double offset, string direction = "right")
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        
        var transitionType = direction.ToLowerInvariant() switch
        {
            "left" => "slideleft",
            "right" => "slideright",
            "up" => "slideup",
            "down" => "slidedown",
            _ => "slideright"
        };

        return $"xfade=transition={transitionType}:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a dissolve transition
    /// </summary>
    public static string BuildDissolve(double duration, double offset)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        return $"xfade=transition=dissolve:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a pixelize transition
    /// </summary>
    public static string BuildPixelize(double duration, double offset, int steps = 20)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        return $"xfade=transition=pixelize:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a circle open/close transition
    /// </summary>
    public static string BuildCircle(double duration, double offset, bool opening = true)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        var transitionType = opening ? "circleopen" : "circleclose";
        return $"xfade=transition={transitionType}:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a radial transition
    /// </summary>
    public static string BuildRadial(double duration, double offset)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var offsetStr = offset.ToString("F3", CultureInfo.InvariantCulture);
        return $"xfade=transition=radial:duration={durationStr}:offset={offsetStr}";
    }

    /// <summary>
    /// Build a chain of transitions for multiple clips
    /// </summary>
    public static List<string> BuildTransitionChain(
        int clipCount,
        double clipDuration,
        double transitionDuration,
        TransitionType defaultTransition = TransitionType.Fade)
    {
        var transitions = new List<string>();

        for (int i = 0; i < clipCount - 1; i++)
        {
            var offset = (i + 1) * clipDuration - transitionDuration;
            transitions.Add(BuildCrossfade(transitionDuration, offset, defaultTransition));
        }

        return transitions;
    }

    /// <summary>
    /// Build a complex filter graph with transitions for multiple inputs
    /// </summary>
    public static string BuildComplexFilterGraph(
        int inputCount,
        double[] clipDurations,
        double[] transitionDurations,
        TransitionType[] transitionTypes)
    {
        if (inputCount != clipDurations.Length)
        {
            throw new ArgumentException("Clip durations count must match input count");
        }

        if (transitionDurations.Length != inputCount - 1)
        {
            throw new ArgumentException("Transition durations count must be inputCount - 1");
        }

        var filterParts = new List<string>();
        double currentOffset = clipDurations[0];

        for (int i = 0; i < inputCount - 1; i++)
        {
            var transitionType = transitionTypes.Length > i 
                ? transitionTypes[i] 
                : TransitionType.Fade;

            var offset = currentOffset - transitionDurations[i];
            var transition = BuildCrossfade(transitionDurations[i], offset, transitionType);
            
            // Build filter chain: [0:v][1:v]xfade...[v01]; [v01][2:v]xfade...[v012]; etc.
            if (i == 0)
            {
                filterParts.Add($"[0:v][1:v]{transition}[v01]");
            }
            else if (i == inputCount - 2)
            {
                filterParts.Add($"[v0{i}][{i + 1}:v]{transition}[vout]");
            }
            else
            {
                filterParts.Add($"[v0{i}][{i + 1}:v]{transition}[v0{i + 1}]");
            }

            currentOffset += clipDurations[i + 1];
        }

        return string.Join(";", filterParts);
    }
}
