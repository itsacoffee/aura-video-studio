using System;
using System.Globalization;

namespace Aura.Core.Services.FFmpeg.Filters;

/// <summary>
/// Builder for creating FFmpeg video effect filters
/// </summary>
public class EffectBuilder
{
    /// <summary>
    /// Build a Ken Burns effect (zoom and pan)
    /// </summary>
    public static string BuildKenBurns(
        double duration,
        int fps = 30,
        double zoomStart = 1.0,
        double zoomEnd = 1.2,
        double panX = 0.0,
        double panY = 0.0,
        int width = 1920,
        int height = 1080)
    {
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        var zoomStartStr = zoomStart.ToString("F3", CultureInfo.InvariantCulture);
        var zoomEndStr = zoomEnd.ToString("F3", CultureInfo.InvariantCulture);
        
        var startX = (0.5 - panX * 0.3).ToString("F3", CultureInfo.InvariantCulture);
        var startY = (0.5 - panY * 0.3).ToString("F3", CultureInfo.InvariantCulture);
        var endX = (0.5 + panX * 0.3).ToString("F3", CultureInfo.InvariantCulture);
        var endY = (0.5 + panY * 0.3).ToString("F3", CultureInfo.InvariantCulture);

        return $"zoompan=z='if(lte(zoom,{zoomStartStr}),{zoomStartStr},if(gte(zoom,{zoomEndStr}),{zoomEndStr},zoom+({zoomEndStr}-{zoomStartStr})/{durationStr}/{fps}))':x='iw/2-(iw/zoom/2)+({endX}-{startX})*on/{durationStr}/{fps}*iw':y='ih/2-(ih/zoom/2)+({endY}-{startY})*on/{durationStr}/{fps}*ih':d={durationStr}*{fps}:s={width}x{height}:fps={fps}";
    }

    /// <summary>
    /// Build a blur effect
    /// </summary>
    public static string BuildBlur(double sigma = 5.0)
    {
        var sigmaStr = sigma.ToString("F3", CultureInfo.InvariantCulture);
        return $"gblur=sigma={sigmaStr}";
    }

    /// <summary>
    /// Build a sharpen effect
    /// </summary>
    public static string BuildSharpen(double luma = 1.5, double chroma = 0.0)
    {
        var lumaStr = luma.ToString("F3", CultureInfo.InvariantCulture);
        var chromaStr = chroma.ToString("F3", CultureInfo.InvariantCulture);
        return $"unsharp=luma_msize_x=5:luma_msize_y=5:luma_amount={lumaStr}:chroma_amount={chromaStr}";
    }

    /// <summary>
    /// Build a brightness/contrast adjustment
    /// </summary>
    public static string BuildBrightnessContrast(double brightness = 0.0, double contrast = 1.0)
    {
        var brightnessStr = brightness.ToString("F3", CultureInfo.InvariantCulture);
        var contrastStr = contrast.ToString("F3", CultureInfo.InvariantCulture);
        return $"eq=brightness={brightnessStr}:contrast={contrastStr}";
    }

    /// <summary>
    /// Build a saturation adjustment
    /// </summary>
    public static string BuildSaturation(double saturation = 1.0)
    {
        var saturationStr = saturation.ToString("F3", CultureInfo.InvariantCulture);
        return $"eq=saturation={saturationStr}";
    }

    /// <summary>
    /// Build a color correction filter
    /// </summary>
    public static string BuildColorCorrection(
        double brightness = 0.0,
        double contrast = 1.0,
        double saturation = 1.0,
        double gamma = 1.0)
    {
        var brightnessStr = brightness.ToString("F3", CultureInfo.InvariantCulture);
        var contrastStr = contrast.ToString("F3", CultureInfo.InvariantCulture);
        var saturationStr = saturation.ToString("F3", CultureInfo.InvariantCulture);
        var gammaStr = gamma.ToString("F3", CultureInfo.InvariantCulture);
        
        return $"eq=brightness={brightnessStr}:contrast={contrastStr}:saturation={saturationStr}:gamma={gammaStr}";
    }

    /// <summary>
    /// Build a vignette effect
    /// </summary>
    public static string BuildVignette(double angle = Math.PI / 5, double intensity = 0.5)
    {
        var angleStr = angle.ToString("F3", CultureInfo.InvariantCulture);
        var mode = "forward";
        var eval = "frame";
        return $"vignette=angle={angleStr}:mode={mode}:eval={eval}";
    }

    /// <summary>
    /// Build a chromatic aberration effect
    /// </summary>
    public static string BuildChromaticAberration(int shift = 2)
    {
        return $"rgbashift=rh={shift}:bh=-{shift}";
    }

    /// <summary>
    /// Build a film grain effect
    /// </summary>
    public static string BuildFilmGrain(int strength = 10)
    {
        return $"noise=alls={strength}:allf=t";
    }

    /// <summary>
    /// Build a motion blur effect
    /// </summary>
    public static string BuildMotionBlur(int radius = 15)
    {
        return $"minterpolate=fps=60:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1";
    }

    /// <summary>
    /// Build a letterbox effect
    /// </summary>
    public static string BuildLetterbox(int width, int height, int targetWidth, int targetHeight, string color = "black")
    {
        return $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2:color={color}";
    }

    /// <summary>
    /// Build a pillarbox effect
    /// </summary>
    public static string BuildPillarbox(int width, int height, int targetWidth, int targetHeight, string color = "black")
    {
        return $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2:color={color}";
    }

    /// <summary>
    /// Build a sepia tone effect
    /// </summary>
    public static string BuildSepia()
    {
        return "colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131";
    }

    /// <summary>
    /// Build a grayscale effect
    /// </summary>
    public static string BuildGrayscale()
    {
        return "hue=s=0";
    }

    /// <summary>
    /// Build a negative effect
    /// </summary>
    public static string BuildNegative()
    {
        return "negate";
    }

    /// <summary>
    /// Build a mirror effect
    /// </summary>
    public static string BuildMirror(string mode = "horizontal")
    {
        return mode.ToLowerInvariant() switch
        {
            "horizontal" => "hflip",
            "vertical" => "vflip",
            "both" => "hflip,vflip",
            _ => "hflip"
        };
    }

    /// <summary>
    /// Build a rotation effect
    /// </summary>
    public static string BuildRotation(double angleDegrees)
    {
        var angleRadians = angleDegrees * Math.PI / 180.0;
        var angleStr = angleRadians.ToString("F6", CultureInfo.InvariantCulture);
        return $"rotate={angleStr}:bilinear=0:fillcolor=black";
    }

    /// <summary>
    /// Build a stabilization effect
    /// </summary>
    public static string BuildStabilization(int shakiness = 5, int smoothing = 10)
    {
        return $"deshake=shakiness={shakiness}:smoothing={smoothing}";
    }

    /// <summary>
    /// Build a denoise effect
    /// </summary>
    public static string BuildDenoise(double lumaSpatial = 2.0, double chromaSpatial = 1.0)
    {
        var lumaStr = lumaSpatial.ToString("F3", CultureInfo.InvariantCulture);
        var chromaStr = chromaSpatial.ToString("F3", CultureInfo.InvariantCulture);
        return $"hqdn3d={lumaStr}:{chromaStr}";
    }

    /// <summary>
    /// Build a picture-in-picture effect
    /// </summary>
    public static string BuildPictureInPicture(
        int overlayIndex,
        string x = "W-w-10",
        string y = "H-h-10",
        double scale = 0.25)
    {
        var scaleStr = scale.ToString("F3", CultureInfo.InvariantCulture);
        return $"[{overlayIndex}:v]scale=iw*{scaleStr}:ih*{scaleStr}[pip];[0:v][pip]overlay={x}:{y}";
    }

    /// <summary>
    /// Build a split-screen effect (2 videos side by side)
    /// </summary>
    public static string BuildSplitScreen(int width, int height, bool horizontal = true)
    {
        if (horizontal)
        {
            // Side by side
            var halfWidth = width / 2;
            return $"[0:v]crop={halfWidth}:{height}:0:0[left];[1:v]crop={halfWidth}:{height}:0:0[right];[left][right]hstack";
        }
        else
        {
            // Top and bottom
            var halfHeight = height / 2;
            return $"[0:v]crop={width}:{halfHeight}:0:0[top];[1:v]crop={width}:{halfHeight}:0:0[bottom];[top][bottom]vstack";
        }
    }

    /// <summary>
    /// Build a slow motion effect
    /// </summary>
    public static string BuildSlowMotion(double speed = 0.5)
    {
        var speedStr = speed.ToString("F3", CultureInfo.InvariantCulture);
        return $"setpts={1.0 / speed:F3}*PTS";
    }

    /// <summary>
    /// Build a fast motion effect
    /// </summary>
    public static string BuildFastMotion(double speed = 2.0)
    {
        var speedStr = (1.0 / speed).ToString("F3", CultureInfo.InvariantCulture);
        return $"setpts={speedStr}*PTS";
    }

    /// <summary>
    /// Build a reverse effect
    /// </summary>
    public static string BuildReverse()
    {
        return "reverse";
    }

    /// <summary>
    /// Build a color key (chroma key/green screen) effect
    /// </summary>
    public static string BuildColorKey(string color = "green", double similarity = 0.3, double blend = 0.1)
    {
        var similarityStr = similarity.ToString("F3", CultureInfo.InvariantCulture);
        var blendStr = blend.ToString("F3", CultureInfo.InvariantCulture);
        return $"colorkey={color}:{similarityStr}:{blendStr}";
    }

    /// <summary>
    /// Build a fade to/from color effect
    /// </summary>
    public static string BuildColorFade(
        double startTime,
        double duration,
        string type = "in",
        string color = "black")
    {
        var startStr = startTime.ToString("F3", CultureInfo.InvariantCulture);
        var durationStr = duration.ToString("F3", CultureInfo.InvariantCulture);
        return $"fade=t={type}:st={startStr}:d={durationStr}:color={color}";
    }
}
