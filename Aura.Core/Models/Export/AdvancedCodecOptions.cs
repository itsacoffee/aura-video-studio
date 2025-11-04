using System;

namespace Aura.Core.Models.Export;

/// <summary>
/// Color depth for video encoding
/// </summary>
public enum ColorDepth
{
    /// <summary>8-bit color depth (standard)</summary>
    EightBit = 8,
    
    /// <summary>10-bit color depth (higher quality, HDR support)</summary>
    TenBit = 10,
    
    /// <summary>12-bit color depth (professional)</summary>
    TwelveBit = 12
}

/// <summary>
/// Color space standard
/// </summary>
public enum ColorSpaceStandard
{
    /// <summary>Rec.709 - Standard HD (sRGB equivalent)</summary>
    Rec709,
    
    /// <summary>Rec.2020 - Ultra HD, HDR</summary>
    Rec2020,
    
    /// <summary>DCI-P3 - Digital cinema, wider gamut than Rec.709</summary>
    DciP3,
    
    /// <summary>Rec.601 - Standard definition</summary>
    Rec601
}

/// <summary>
/// HDR transfer function
/// </summary>
public enum HdrTransferFunction
{
    /// <summary>No HDR (SDR)</summary>
    None,
    
    /// <summary>PQ (Perceptual Quantizer) - HDR10, HDR10+</summary>
    PQ,
    
    /// <summary>HLG (Hybrid Log-Gamma) - Broadcast HDR</summary>
    HLG,
    
    /// <summary>Dolby Vision</summary>
    DolbyVision
}

/// <summary>
/// HDR tone mapping options
/// </summary>
public enum ToneMappingMode
{
    /// <summary>No tone mapping</summary>
    None,
    
    /// <summary>Simple linear mapping</summary>
    Linear,
    
    /// <summary>Reinhard tone mapping</summary>
    Reinhard,
    
    /// <summary>Hable (Uncharted 2) tone mapping</summary>
    Hable,
    
    /// <summary>Mobius tone mapping</summary>
    Mobius,
    
    /// <summary>ACES tone mapping</summary>
    Aces
}

/// <summary>
/// Advanced codec options for HDR and high-quality encoding
/// </summary>
public record AdvancedCodecOptions
{
    /// <summary>
    /// Color depth (8-bit, 10-bit, 12-bit)
    /// </summary>
    public ColorDepth ColorDepth { get; init; } = ColorDepth.EightBit;
    
    /// <summary>
    /// Color space standard
    /// </summary>
    public ColorSpaceStandard ColorSpaceStandard { get; init; } = ColorSpaceStandard.Rec709;
    
    /// <summary>
    /// HDR transfer function
    /// </summary>
    public HdrTransferFunction HdrTransferFunction { get; init; } = HdrTransferFunction.None;
    
    /// <summary>
    /// Tone mapping mode for HDR to SDR conversion
    /// </summary>
    public ToneMappingMode ToneMappingMode { get; init; } = ToneMappingMode.None;
    
    /// <summary>
    /// Maximum content light level (MaxCLL) in nits for HDR
    /// </summary>
    public int? MaxContentLightLevel { get; init; }
    
    /// <summary>
    /// Maximum frame average light level (MaxFALL) in nits for HDR
    /// </summary>
    public int? MaxFrameAverageLightLevel { get; init; }
    
    /// <summary>
    /// Master display color primaries for HDR
    /// </summary>
    public ColorPrimaries? MasterDisplayPrimaries { get; init; }
    
    /// <summary>
    /// Whether this is an HDR export
    /// </summary>
    public bool IsHdr => HdrTransferFunction != HdrTransferFunction.None;
    
    /// <summary>
    /// Whether this requires 10-bit or higher encoding
    /// </summary>
    public bool Requires10Bit => ColorDepth >= ColorDepth.TenBit || IsHdr;
}

/// <summary>
/// Color primaries for HDR mastering display
/// </summary>
public record ColorPrimaries
{
    /// <summary>Red primary (x, y)</summary>
    public (double X, double Y) Red { get; init; }
    
    /// <summary>Green primary (x, y)</summary>
    public (double X, double Y) Green { get; init; }
    
    /// <summary>Blue primary (x, y)</summary>
    public (double X, double Y) Blue { get; init; }
    
    /// <summary>White point (x, y)</summary>
    public (double X, double Y) White { get; init; }
    
    /// <summary>Minimum luminance in nits</summary>
    public double MinLuminance { get; init; }
    
    /// <summary>Maximum luminance in nits</summary>
    public double MaxLuminance { get; init; }
    
    /// <summary>
    /// Standard DCI-P3 D65 primaries
    /// </summary>
    public static ColorPrimaries DciP3 => new()
    {
        Red = (0.680, 0.320),
        Green = (0.265, 0.690),
        Blue = (0.150, 0.060),
        White = (0.3127, 0.3290),
        MinLuminance = 0.0001,
        MaxLuminance = 1000
    };
    
    /// <summary>
    /// Standard Rec.2020 primaries
    /// </summary>
    public static ColorPrimaries Rec2020 => new()
    {
        Red = (0.708, 0.292),
        Green = (0.170, 0.797),
        Blue = (0.131, 0.046),
        White = (0.3127, 0.3290),
        MinLuminance = 0.0001,
        MaxLuminance = 1000
    };
}

/// <summary>
/// Extension methods for advanced codec options
/// </summary>
public static class AdvancedCodecOptionsExtensions
{
    /// <summary>
    /// Get FFmpeg pixel format for the codec options
    /// </summary>
    public static string GetPixelFormat(this AdvancedCodecOptions options)
    {
        if (options.ColorDepth == ColorDepth.TenBit)
        {
            return options.ColorSpaceStandard switch
            {
                ColorSpaceStandard.Rec2020 => "yuv420p10le",
                _ => "yuv420p10le"
            };
        }
        
        return "yuv420p";
    }
    
    /// <summary>
    /// Get FFmpeg color space parameter
    /// </summary>
    public static string GetColorSpace(this AdvancedCodecOptions options)
    {
        return options.ColorSpaceStandard switch
        {
            ColorSpaceStandard.Rec709 => "bt709",
            ColorSpaceStandard.Rec2020 => "bt2020nc",
            ColorSpaceStandard.DciP3 => "bt2020nc",
            ColorSpaceStandard.Rec601 => "bt601",
            _ => "bt709"
        };
    }
    
    /// <summary>
    /// Get FFmpeg color transfer parameter for HDR
    /// </summary>
    public static string? GetColorTransfer(this AdvancedCodecOptions options)
    {
        return options.HdrTransferFunction switch
        {
            HdrTransferFunction.PQ => "smpte2084",
            HdrTransferFunction.HLG => "arib-std-b67",
            HdrTransferFunction.None => null,
            _ => null
        };
    }
    
    /// <summary>
    /// Get FFmpeg color primaries parameter
    /// </summary>
    public static string GetColorPrimaries(this AdvancedCodecOptions options)
    {
        return options.ColorSpaceStandard switch
        {
            ColorSpaceStandard.Rec709 => "bt709",
            ColorSpaceStandard.Rec2020 => "bt2020",
            ColorSpaceStandard.DciP3 => "bt2020",
            ColorSpaceStandard.Rec601 => "bt470bg",
            _ => "bt709"
        };
    }
}
