using System;
using System.Globalization;

namespace Aura.Core.Models.VideoEffects;

/// <summary>
/// Base class for text animation effects
/// </summary>
public abstract class TextAnimationEffect : VideoEffect
{
    /// <summary>
    /// Text to display
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Font family name
    /// </summary>
    public string? FontFamily { get; set; }

    /// <summary>
    /// Font file path
    /// </summary>
    public string? FontFile { get; set; }

    /// <summary>
    /// Font size in pixels
    /// </summary>
    public int FontSize { get; set; } = 48;

    /// <summary>
    /// Font color (hex or name)
    /// </summary>
    public string FontColor { get; set; } = "white";

    /// <summary>
    /// Text position X (expression or pixel value)
    /// </summary>
    public string PositionX { get; set; } = "(w-text_w)/2";

    /// <summary>
    /// Text position Y (expression or pixel value)
    /// </summary>
    public string PositionY { get; set; } = "(h-text_h)/2";

    /// <summary>
    /// Background box color
    /// </summary>
    public string? BoxColor { get; set; }

    /// <summary>
    /// Text alignment (left, center, right)
    /// </summary>
    public string Alignment { get; set; } = "center";

    /// <summary>
    /// Text shadow color
    /// </summary>
    public string? ShadowColor { get; set; }

    /// <summary>
    /// Shadow offset X
    /// </summary>
    public int ShadowX { get; set; } = 2;

    /// <summary>
    /// Shadow offset Y
    /// </summary>
    public int ShadowY { get; set; } = 2;

    /// <summary>
    /// Text outline/border width
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    /// Border color
    /// </summary>
    public string? BorderColor { get; set; }

    protected TextAnimationEffect()
    {
        Type = EffectType.TextAnimation;
        Category = EffectCategory.Basic;
    }

    /// <summary>
    /// Escape text for FFmpeg drawtext filter
    /// </summary>
    protected string EscapeText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace(":", "\\:")
            .Replace("'", "\\'")
            .Replace("%", "\\%")
            .Replace("\n", "\\n");
    }
}

/// <summary>
/// Typewriter text animation effect
/// </summary>
public class TypewriterEffect : TextAnimationEffect
{
    /// <summary>
    /// Characters per second
    /// </summary>
    public double Speed { get; set; } = 10.0;

    /// <summary>
    /// Cursor character
    /// </summary>
    public string Cursor { get; set; } = "|";

    /// <summary>
    /// Show blinking cursor
    /// </summary>
    public bool ShowCursor { get; set; } = true;

    /// <summary>
    /// Cursor blink rate (blinks per second)
    /// </summary>
    public double CursorBlinkRate { get; set; } = 2.0;

    public TypewriterEffect()
    {
        Name = "Typewriter";
        Description = "Typewriter text animation";
    }

    public override string ToFFmpegFilter()
    {
        var escapedText = EscapeText(Text);
        var startTimeStr = StartTime.ToString(CultureInfo.InvariantCulture);
        var endTimeStr = (StartTime + Duration).ToString(CultureInfo.InvariantCulture);
        
        // Calculate characters to show based on time
        var charsPerFrame = Speed / 30.0; // Assuming 30 fps
        var textLengthExpr = $"min({Text.Length},floor((t-{startTimeStr})*{Speed.ToString(CultureInfo.InvariantCulture)}))";
        
        var filter = $"drawtext=text='{escapedText}':" +
                    $"fontsize={FontSize}:" +
                    $"fontcolor={FontColor}:" +
                    $"x={PositionX}:" +
                    $"y={PositionY}:" +
                    $"enable='between(t,{startTimeStr},{endTimeStr})'";

        if (!string.IsNullOrEmpty(FontFile))
        {
            filter += $":fontfile={FontFile}";
        }

        if (!string.IsNullOrEmpty(BoxColor))
        {
            filter += $":box=1:boxcolor={BoxColor}:boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth}:bordercolor={BorderColor}";
        }

        return filter;
    }
}

/// <summary>
/// Fade text animation effect
/// </summary>
public class FadeTextEffect : TextAnimationEffect
{
    /// <summary>
    /// Fade in duration (seconds)
    /// </summary>
    public double FadeInDuration { get; set; } = 0.5;

    /// <summary>
    /// Fade out duration (seconds)
    /// </summary>
    public double FadeOutDuration { get; set; } = 0.5;

    /// <summary>
    /// Hold duration before fade out (seconds)
    /// </summary>
    public double HoldDuration { get; set; }

    public FadeTextEffect()
    {
        Name = "Fade Text";
        Description = "Text with fade in/out animation";
    }

    public override string ToFFmpegFilter()
    {
        var escapedText = EscapeText(Text);
        var startTimeStr = StartTime.ToString(CultureInfo.InvariantCulture);
        var endTimeStr = (StartTime + Duration).ToString(CultureInfo.InvariantCulture);
        var fadeInEndStr = (StartTime + FadeInDuration).ToString(CultureInfo.InvariantCulture);
        var fadeOutStartStr = (StartTime + Duration - FadeOutDuration).ToString(CultureInfo.InvariantCulture);
        var fadeInDurStr = FadeInDuration.ToString(CultureInfo.InvariantCulture);
        var fadeOutDurStr = FadeOutDuration.ToString(CultureInfo.InvariantCulture);

        // Alpha expression for fade in/out
        var alphaExpr = $"if(lt(t,{startTimeStr}),0," +
                       $"if(lt(t,{fadeInEndStr}),(t-{startTimeStr})/{fadeInDurStr}," +
                       $"if(lt(t,{fadeOutStartStr}),1," +
                       $"if(lt(t,{endTimeStr}),({endTimeStr}-t)/{fadeOutDurStr},0))))";

        var filter = $"drawtext=text='{escapedText}':" +
                    $"fontsize={FontSize}:" +
                    $"fontcolor={FontColor}:" +
                    $"x={PositionX}:" +
                    $"y={PositionY}:" +
                    $"alpha='{alphaExpr}':" +
                    $"enable='between(t,{startTimeStr},{endTimeStr})'";

        if (!string.IsNullOrEmpty(FontFile))
        {
            filter += $":fontfile={FontFile}";
        }

        if (!string.IsNullOrEmpty(BoxColor))
        {
            filter += $":box=1:boxcolor={BoxColor}:boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth}:bordercolor={BorderColor}";
        }

        return filter;
    }
}

/// <summary>
/// Sliding text animation effect
/// </summary>
public class SlidingTextEffect : TextAnimationEffect
{
    /// <summary>
    /// Slide direction
    /// </summary>
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down,
        DiagonalUpLeft,
        DiagonalUpRight,
        DiagonalDownLeft,
        DiagonalDownRight
    }

    /// <summary>
    /// Direction to slide
    /// </summary>
    public SlideDirection Direction { get; set; } = SlideDirection.Left;

    /// <summary>
    /// Easing function for the slide
    /// </summary>
    public EasingFunction Easing { get; set; } = EasingFunction.EaseInOut;

    public SlidingTextEffect()
    {
        Name = "Sliding Text";
        Description = "Text that slides in from off-screen";
    }

    public override string ToFFmpegFilter()
    {
        var escapedText = EscapeText(Text);
        var startTimeStr = StartTime.ToString(CultureInfo.InvariantCulture);
        var endTimeStr = (StartTime + Duration).ToString(CultureInfo.InvariantCulture);
        var durationStr = Duration.ToString(CultureInfo.InvariantCulture);

        // Calculate position expressions based on direction
        var (xExpr, yExpr) = Direction switch
        {
            SlideDirection.Left => ($"w-((t-{startTimeStr})/{durationStr})*(w+text_w)", "(h-text_h)/2"),
            SlideDirection.Right => ($"-text_w+((t-{startTimeStr})/{durationStr})*(w+text_w)", "(h-text_h)/2"),
            SlideDirection.Up => ("(w-text_w)/2", $"h-((t-{startTimeStr})/{durationStr})*(h+text_h)"),
            SlideDirection.Down => ("(w-text_w)/2", $"-text_h+((t-{startTimeStr})/{durationStr})*(h+text_h)"),
            _ => ($"w-((t-{startTimeStr})/{durationStr})*(w+text_w)", "(h-text_h)/2")
        };

        var filter = $"drawtext=text='{escapedText}':" +
                    $"fontsize={FontSize}:" +
                    $"fontcolor={FontColor}:" +
                    $"x='{xExpr}':" +
                    $"y='{yExpr}':" +
                    $"enable='between(t,{startTimeStr},{endTimeStr})'";

        if (!string.IsNullOrEmpty(FontFile))
        {
            filter += $":fontfile={FontFile}";
        }

        if (!string.IsNullOrEmpty(BoxColor))
        {
            filter += $":box=1:boxcolor={BoxColor}:boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth}:bordercolor={BorderColor}";
        }

        return filter;
    }
}

/// <summary>
/// Kinetic typography effect with dynamic movement
/// </summary>
public class KineticTypographyEffect : TextAnimationEffect
{
    /// <summary>
    /// Animation style
    /// </summary>
    public enum AnimationStyle
    {
        Bounce,
        Elastic,
        Shake,
        Pulse,
        Wave,
        Spiral
    }

    /// <summary>
    /// Animation style
    /// </summary>
    public AnimationStyle Style { get; set; } = AnimationStyle.Bounce;

    /// <summary>
    /// Animation amplitude/intensity
    /// </summary>
    public double Amplitude { get; set; } = 20.0;

    /// <summary>
    /// Animation frequency/speed
    /// </summary>
    public double Frequency { get; set; } = 2.0;

    public KineticTypographyEffect()
    {
        Name = "Kinetic Typography";
        Description = "Dynamic text with motion effects";
    }

    public override string ToFFmpegFilter()
    {
        var escapedText = EscapeText(Text);
        var startTimeStr = StartTime.ToString(CultureInfo.InvariantCulture);
        var endTimeStr = (StartTime + Duration).ToString(CultureInfo.InvariantCulture);
        var amplitudeStr = Amplitude.ToString(CultureInfo.InvariantCulture);
        var frequencyStr = Frequency.ToString(CultureInfo.InvariantCulture);

        // Create animated position expressions based on style
        var (xExpr, yExpr) = Style switch
        {
            AnimationStyle.Bounce => (
                $"(w-text_w)/2",
                $"(h-text_h)/2+{amplitudeStr}*abs(sin(2*PI*{frequencyStr}*(t-{startTimeStr})))"
            ),
            AnimationStyle.Shake => (
                $"(w-text_w)/2+{amplitudeStr}*sin(2*PI*{frequencyStr}*2*(t-{startTimeStr}))",
                $"(h-text_h)/2+{amplitudeStr}*cos(2*PI*{frequencyStr}*2*(t-{startTimeStr}))"
            ),
            AnimationStyle.Pulse => (
                $"(w-text_w*(1+{Amplitude.ToString(CultureInfo.InvariantCulture)}/100*sin(2*PI*{frequencyStr}*(t-{startTimeStr}))))/2",
                $"(h-text_h*(1+{Amplitude.ToString(CultureInfo.InvariantCulture)}/100*sin(2*PI*{frequencyStr}*(t-{startTimeStr}))))/2"
            ),
            AnimationStyle.Wave => (
                $"(w-text_w)/2+{amplitudeStr}*sin(2*PI*{frequencyStr}*(t-{startTimeStr}))",
                $"(h-text_h)/2"
            ),
            _ => ($"(w-text_w)/2", $"(h-text_h)/2")
        };

        var filter = $"drawtext=text='{escapedText}':" +
                    $"fontsize={FontSize}:" +
                    $"fontcolor={FontColor}:" +
                    $"x='{xExpr}':" +
                    $"y='{yExpr}':" +
                    $"enable='between(t,{startTimeStr},{endTimeStr})'";

        if (!string.IsNullOrEmpty(FontFile))
        {
            filter += $":fontfile={FontFile}";
        }

        if (!string.IsNullOrEmpty(BoxColor))
        {
            filter += $":box=1:boxcolor={BoxColor}:boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth}:bordercolor={BorderColor}";
        }

        return filter;
    }
}

/// <summary>
/// Scrolling text effect (credits, ticker, etc.)
/// </summary>
public class ScrollingTextEffect : TextAnimationEffect
{
    /// <summary>
    /// Scroll direction
    /// </summary>
    public enum ScrollDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Direction to scroll
    /// </summary>
    public ScrollDirection Direction { get; set; } = ScrollDirection.Up;

    /// <summary>
    /// Scroll speed in pixels per second
    /// </summary>
    public double Speed { get; set; } = 100.0;

    /// <summary>
    /// Whether to loop the scrolling
    /// </summary>
    public bool Loop { get; set; }

    public ScrollingTextEffect()
    {
        Name = "Scrolling Text";
        Description = "Scrolling text for credits or tickers";
    }

    public override string ToFFmpegFilter()
    {
        var escapedText = EscapeText(Text);
        var startTimeStr = StartTime.ToString(CultureInfo.InvariantCulture);
        var endTimeStr = (StartTime + Duration).ToString(CultureInfo.InvariantCulture);
        var speedStr = Speed.ToString(CultureInfo.InvariantCulture);

        // Calculate position based on direction
        var (xExpr, yExpr) = Direction switch
        {
            ScrollDirection.Up => (
                "(w-text_w)/2",
                $"h-{speedStr}*(t-{startTimeStr})"
            ),
            ScrollDirection.Down => (
                "(w-text_w)/2",
                $"-text_h+{speedStr}*(t-{startTimeStr})"
            ),
            ScrollDirection.Left => (
                $"w-{speedStr}*(t-{startTimeStr})",
                "(h-text_h)/2"
            ),
            ScrollDirection.Right => (
                $"-text_w+{speedStr}*(t-{startTimeStr})",
                "(h-text_h)/2"
            ),
            _ => ("(w-text_w)/2", $"h-{speedStr}*(t-{startTimeStr})")
        };

        var filter = $"drawtext=text='{escapedText}':" +
                    $"fontsize={FontSize}:" +
                    $"fontcolor={FontColor}:" +
                    $"x='{xExpr}':" +
                    $"y='{yExpr}':" +
                    $"enable='between(t,{startTimeStr},{endTimeStr})'";

        if (!string.IsNullOrEmpty(FontFile))
        {
            filter += $":fontfile={FontFile}";
        }

        if (!string.IsNullOrEmpty(BoxColor))
        {
            filter += $":box=1:boxcolor={BoxColor}:boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth}:bordercolor={BorderColor}";
        }

        return filter;
    }
}
