using System;
using System.Globalization;

namespace Aura.Core.Timeline.Overlays;

/// <summary>
/// Type of text overlay
/// </summary>
public enum OverlayType
{
    Title,
    LowerThird,
    Callout
}

/// <summary>
/// Safe area alignment preset
/// </summary>
public enum SafeAreaAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Custom
}

/// <summary>
/// Represents a text overlay with position, timing, and styling
/// </summary>
public record OverlayModel
{
    public string Id { get; init; }
    public OverlayType Type { get; init; }
    public string Text { get; init; }
    public TimeSpan InTime { get; init; }
    public TimeSpan OutTime { get; init; }
    public SafeAreaAlignment Alignment { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int FontSize { get; init; }
    public string FontColor { get; init; }
    public string? BackgroundColor { get; init; }
    public float BackgroundOpacity { get; init; }
    public int BorderWidth { get; init; }
    public string? BorderColor { get; init; }

    public TimeSpan Duration => OutTime - InTime;

    public OverlayModel(
        string id,
        OverlayType type,
        string text,
        TimeSpan inTime,
        TimeSpan outTime,
        SafeAreaAlignment alignment = SafeAreaAlignment.BottomCenter,
        int x = 0,
        int y = 0,
        int fontSize = 48,
        string fontColor = "white",
        string? backgroundColor = null,
        float backgroundOpacity = 0.8f,
        int borderWidth = 0,
        string? borderColor = null)
    {
        Id = id;
        Type = type;
        Text = text;
        InTime = inTime;
        OutTime = outTime;
        Alignment = alignment;
        X = x;
        Y = y;
        FontSize = fontSize;
        FontColor = fontColor;
        BackgroundColor = backgroundColor;
        BackgroundOpacity = backgroundOpacity;
        BorderWidth = borderWidth;
        BorderColor = borderColor;
    }

    /// <summary>
    /// Get position coordinates based on safe area alignment and video dimensions
    /// </summary>
    public (int x, int y) GetPosition(int videoWidth, int videoHeight)
    {
        const int safeMargin = 50;

        return Alignment switch
        {
            SafeAreaAlignment.TopLeft => (safeMargin, safeMargin),
            SafeAreaAlignment.TopCenter => (videoWidth / 2, safeMargin),
            SafeAreaAlignment.TopRight => (videoWidth - safeMargin, safeMargin),
            SafeAreaAlignment.MiddleLeft => (safeMargin, videoHeight / 2),
            SafeAreaAlignment.MiddleCenter => (videoWidth / 2, videoHeight / 2),
            SafeAreaAlignment.MiddleRight => (videoWidth - safeMargin, videoHeight / 2),
            SafeAreaAlignment.BottomLeft => (safeMargin, videoHeight - safeMargin),
            SafeAreaAlignment.BottomCenter => (videoWidth / 2, videoHeight - safeMargin),
            SafeAreaAlignment.BottomRight => (videoWidth - safeMargin, videoHeight - safeMargin),
            SafeAreaAlignment.Custom => (X, Y),
            _ => (videoWidth / 2, videoHeight - safeMargin)
        };
    }

    /// <summary>
    /// Convert overlay to FFmpeg drawtext filter (culture-invariant)
    /// </summary>
    public string ToDrawTextFilter(int videoWidth, int videoHeight)
    {
        var (x, y) = GetPosition(videoWidth, videoHeight);
        var escapedText = EscapeFFmpegText(Text);

        var filter = $"drawtext=text='{escapedText}'";
        filter += $":fontsize={FontSize.ToString(CultureInfo.InvariantCulture)}";
        filter += $":fontcolor={FontColor}";
        filter += $":x={x.ToString(CultureInfo.InvariantCulture)}";
        filter += $":y={y.ToString(CultureInfo.InvariantCulture)}";

        if (!string.IsNullOrEmpty(BackgroundColor))
        {
            var bgOpacity = BackgroundOpacity.ToString("F2", CultureInfo.InvariantCulture);
            filter += $":box=1:boxcolor={BackgroundColor}@{bgOpacity}";
            filter += ":boxborderw=10";
        }

        if (BorderWidth > 0 && !string.IsNullOrEmpty(BorderColor))
        {
            filter += $":borderw={BorderWidth.ToString(CultureInfo.InvariantCulture)}";
            filter += $":bordercolor={BorderColor}";
        }

        var inSeconds = InTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        var outSeconds = OutTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        filter += $":enable='between(t,{inSeconds},{outSeconds})'";

        return filter;
    }

    /// <summary>
    /// Convert overlay to FFmpeg drawbox filter for background (culture-invariant)
    /// </summary>
    public string? ToDrawBoxFilter(int videoWidth, int videoHeight)
    {
        if (string.IsNullOrEmpty(BackgroundColor)) return null;

        var (x, y) = GetPosition(videoWidth, videoHeight);
        
        var textWidth = Text.Length * FontSize / 2;
        var textHeight = FontSize + 20;

        var boxX = x - textWidth / 2;
        var boxY = y - textHeight / 2;

        var bgOpacity = BackgroundOpacity.ToString("F2", CultureInfo.InvariantCulture);
        var filter = $"drawbox=x={boxX.ToString(CultureInfo.InvariantCulture)}";
        filter += $":y={boxY.ToString(CultureInfo.InvariantCulture)}";
        filter += $":w={textWidth.ToString(CultureInfo.InvariantCulture)}";
        filter += $":h={textHeight.ToString(CultureInfo.InvariantCulture)}";
        filter += $":color={BackgroundColor}@{bgOpacity}";
        filter += ":t=fill";

        var inSeconds = InTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        var outSeconds = OutTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        filter += $":enable='between(t,{inSeconds},{outSeconds})'";

        return filter;
    }

    /// <summary>
    /// Escape text for FFmpeg filter syntax
    /// </summary>
    private static string EscapeFFmpegText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace(":", "\\:")
            .Replace("%", "\\%");
    }

    /// <summary>
    /// Create a default title overlay
    /// </summary>
    public static OverlayModel CreateTitle(string text, TimeSpan inTime, TimeSpan outTime)
    {
        return new OverlayModel(
            id: Guid.NewGuid().ToString("N"),
            type: OverlayType.Title,
            text: text,
            inTime: inTime,
            outTime: outTime,
            alignment: SafeAreaAlignment.TopCenter,
            fontSize: 72,
            fontColor: "white",
            backgroundColor: "black",
            backgroundOpacity: 0.7f,
            borderWidth: 2,
            borderColor: "white"
        );
    }

    /// <summary>
    /// Create a default lower-third overlay
    /// </summary>
    public static OverlayModel CreateLowerThird(string text, TimeSpan inTime, TimeSpan outTime)
    {
        return new OverlayModel(
            id: Guid.NewGuid().ToString("N"),
            type: OverlayType.LowerThird,
            text: text,
            inTime: inTime,
            outTime: outTime,
            alignment: SafeAreaAlignment.BottomLeft,
            fontSize: 36,
            fontColor: "white",
            backgroundColor: "0x000080",
            backgroundOpacity: 0.85f,
            borderWidth: 0
        );
    }

    /// <summary>
    /// Create a default callout overlay
    /// </summary>
    public static OverlayModel CreateCallout(string text, TimeSpan inTime, TimeSpan outTime)
    {
        return new OverlayModel(
            id: Guid.NewGuid().ToString("N"),
            type: OverlayType.Callout,
            text: text,
            inTime: inTime,
            outTime: outTime,
            alignment: SafeAreaAlignment.MiddleRight,
            fontSize: 48,
            fontColor: "yellow",
            backgroundColor: "black",
            backgroundOpacity: 0.8f,
            borderWidth: 3,
            borderColor: "yellow"
        );
    }
}
