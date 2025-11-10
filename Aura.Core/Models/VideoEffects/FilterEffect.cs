using System;
using System.Globalization;
using System.Linq;

namespace Aura.Core.Models.VideoEffects;

/// <summary>
/// Color correction filter effect
/// </summary>
public class ColorCorrectionEffect : VideoEffect
{
    /// <summary>
    /// Brightness adjustment (-1.0 to 1.0)
    /// </summary>
    public double Brightness { get; set; } = 0.0;

    /// <summary>
    /// Contrast adjustment (-1.0 to 1.0)
    /// </summary>
    public double Contrast { get; set; } = 0.0;

    /// <summary>
    /// Saturation adjustment (-1.0 to 1.0)
    /// </summary>
    public double Saturation { get; set; } = 0.0;

    /// <summary>
    /// Hue shift in degrees (0 to 360)
    /// </summary>
    public double Hue { get; set; } = 0.0;

    /// <summary>
    /// Gamma correction (0.1 to 10.0)
    /// </summary>
    public double Gamma { get; set; } = 1.0;

    /// <summary>
    /// Temperature adjustment (-100 to 100)
    /// </summary>
    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Tint adjustment (-100 to 100)
    /// </summary>
    public double Tint { get; set; } = 0.0;

    public ColorCorrectionEffect()
    {
        Type = EffectType.ColorCorrection;
        Name = "Color Correction";
        Description = "Adjust colors, brightness, and contrast";
        Category = EffectCategory.ColorGrading;
    }

    public override string ToFFmpegFilter()
    {
        var filters = new System.Collections.Generic.List<string>();

        // Apply brightness and contrast using eq filter
        if (Math.Abs(Brightness) > 0.001 || Math.Abs(Contrast) > 0.001)
        {
            var brightness = Brightness.ToString(CultureInfo.InvariantCulture);
            var contrast = (1.0 + Contrast).ToString(CultureInfo.InvariantCulture);
            filters.Add($"eq=brightness={brightness}:contrast={contrast}");
        }

        // Apply saturation using eq filter
        if (Math.Abs(Saturation) > 0.001)
        {
            var saturation = (1.0 + Saturation).ToString(CultureInfo.InvariantCulture);
            filters.Add($"eq=saturation={saturation}");
        }

        // Apply hue adjustment
        if (Math.Abs(Hue) > 0.001)
        {
            var hue = Hue.ToString(CultureInfo.InvariantCulture);
            filters.Add($"hue=h={hue}");
        }

        // Apply gamma correction
        if (Math.Abs(Gamma - 1.0) > 0.001)
        {
            var gamma = Gamma.ToString(CultureInfo.InvariantCulture);
            filters.Add($"eq=gamma={gamma}");
        }

        // Apply temperature using colorchannelmixer
        if (Math.Abs(Temperature) > 0.1)
        {
            var tempFactor = 1.0 + (Temperature / 100.0);
            filters.Add($"colorchannelmixer=rr={tempFactor.ToString(CultureInfo.InvariantCulture)}");
        }

        return filters.Any() ? string.Join(",", filters) : "copy";
    }
}

/// <summary>
/// Blur filter effect
/// </summary>
public class BlurEffect : VideoEffect
{
    /// <summary>
    /// Blur types
    /// </summary>
    public enum BlurType
    {
        Gaussian,
        Box,
        Motion,
        Radial,
        Zoom
    }

    /// <summary>
    /// Type of blur
    /// </summary>
    public BlurType Type { get; set; } = BlurType.Gaussian;

    /// <summary>
    /// Blur strength (0 to 100)
    /// </summary>
    public double Strength { get; set; } = 5.0;

    /// <summary>
    /// Motion blur angle (for motion blur)
    /// </summary>
    public double Angle { get; set; } = 0.0;

    /// <summary>
    /// Center X for radial/zoom blur (0.0 to 1.0)
    /// </summary>
    public double CenterX { get; set; } = 0.5;

    /// <summary>
    /// Center Y for radial/zoom blur (0.0 to 1.0)
    /// </summary>
    public double CenterY { get; set; } = 0.5;

    public BlurEffect()
    {
        this.Type = EffectType.Filter;
        Name = "Blur";
        Description = "Apply various blur effects";
        Category = EffectCategory.Blur;
    }

    public override string ToFFmpegFilter()
    {
        var strength = Math.Max(0, Math.Min(100, Strength));
        var luma = Math.Max(0.01, strength / 10.0).ToString(CultureInfo.InvariantCulture);
        
        return Type switch
        {
            BlurType.Gaussian => $"gblur=sigma={luma}",
            BlurType.Box => $"boxblur=luma_radius={luma}:luma_power=1",
            BlurType.Motion => $"smartblur=luma_radius={luma}:luma_strength={Intensity.ToString(CultureInfo.InvariantCulture)}",
            BlurType.Radial => $"rotate=a='2*PI*{Angle.ToString(CultureInfo.InvariantCulture)}/360':fillcolor=black,gblur=sigma={luma}",
            BlurType.Zoom => $"zoompan=z='1+{(strength/100.0).ToString(CultureInfo.InvariantCulture)}':d=1:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)',gblur=sigma={luma}",
            _ => $"gblur=sigma={luma}"
        };
    }
}

/// <summary>
/// Vintage/retro filter effect
/// </summary>
public class VintageEffect : VideoEffect
{
    /// <summary>
    /// Vintage styles
    /// </summary>
    public enum VintageStyle
    {
        Sepia,
        OldFilm,
        VHS,
        Polaroid,
        Faded,
        BlackAndWhite
    }

    /// <summary>
    /// Vintage style
    /// </summary>
    public VintageStyle Style { get; set; } = VintageStyle.Sepia;

    /// <summary>
    /// Grain/noise amount (0 to 1)
    /// </summary>
    public double Grain { get; set; } = 0.3;

    /// <summary>
    /// Vignette strength (0 to 1)
    /// </summary>
    public double Vignette { get; set; } = 0.5;

    /// <summary>
    /// Dust and scratches effect (0 to 1)
    /// </summary>
    public double Scratches { get; set; } = 0.0;

    public VintageEffect()
    {
        Type = EffectType.Filter;
        Name = "Vintage";
        Description = "Apply vintage and retro effects";
        Category = EffectCategory.Vintage;
    }

    public override string ToFFmpegFilter()
    {
        var filters = new System.Collections.Generic.List<string>();

        // Base vintage effect
        filters.Add(Style switch
        {
            VintageStyle.Sepia => "colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131",
            VintageStyle.OldFilm => "curves=vintage,colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131",
            VintageStyle.VHS => "eq=contrast=1.3:brightness=-0.1,noise=alls=20:allf=t+u",
            VintageStyle.Polaroid => "curves=color_negative,eq=saturation=1.2:contrast=1.1",
            VintageStyle.Faded => "eq=saturation=0.6:gamma=1.2",
            VintageStyle.BlackAndWhite => "hue=s=0",
            _ => "copy"
        });

        // Add grain/noise
        if (Grain > 0.001)
        {
            var grainAmount = (int)(Grain * 50);
            filters.Add($"noise=alls={grainAmount}:allf=t+u");
        }

        // Add vignette
        if (Vignette > 0.001)
        {
            var vignetteStr = Vignette.ToString(CultureInfo.InvariantCulture);
            filters.Add($"vignette=angle=PI/4:mode=forward:eval=frame:dither=1:aspect=16/9");
        }

        return string.Join(",", filters);
    }
}

/// <summary>
/// Sharpen filter effect
/// </summary>
public class SharpenEffect : VideoEffect
{
    /// <summary>
    /// Sharpen strength (0 to 10)
    /// </summary>
    public double Strength { get; set; } = 1.0;

    public SharpenEffect()
    {
        Type = EffectType.Filter;
        Name = "Sharpen";
        Description = "Sharpen video details";
        Category = EffectCategory.Basic;
    }

    public override string ToFFmpegFilter()
    {
        var luma = Math.Max(0.1, Math.Min(10, Strength)).ToString(CultureInfo.InvariantCulture);
        var chroma = (Strength * 0.5).ToString(CultureInfo.InvariantCulture);
        return $"unsharp=luma_msize_x=5:luma_msize_y=5:luma_amount={luma}:chroma_msize_x=5:chroma_msize_y=5:chroma_amount={chroma}";
    }
}

/// <summary>
/// Chromatic aberration effect
/// </summary>
public class ChromaticAberrationEffect : VideoEffect
{
    /// <summary>
    /// Aberration strength (0 to 10)
    /// </summary>
    public double Strength { get; set; } = 2.0;

    /// <summary>
    /// Aberration angle in degrees
    /// </summary>
    public double Angle { get; set; } = 0.0;

    public ChromaticAberrationEffect()
    {
        Type = EffectType.Filter;
        Name = "Chromatic Aberration";
        Description = "Color fringing effect";
        Category = EffectCategory.Artistic;
    }

    public override string ToFFmpegFilter()
    {
        var strength = Strength.ToString(CultureInfo.InvariantCulture);
        // Split RGB channels and offset them slightly
        return $"split=3[r][g][b];[r]lutrgb=r=val:g=0:b=0[r];[g]lutrgb=r=0:g=val:b=0,crop=iw-{strength}:ih:0:0[g];[b]lutrgb=r=0:g=0:b=val,crop=iw-{strength}:ih:{strength}:0[b];[r][g]blend=all_mode=addition[rg];[rg][b]blend=all_mode=addition";
    }
}
