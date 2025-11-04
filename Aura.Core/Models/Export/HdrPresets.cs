using System.Collections.Generic;

namespace Aura.Core.Models.Export;

/// <summary>
/// Extended export preset with advanced codec options
/// </summary>
public record AdvancedExportPreset : ExportPreset
{
    /// <summary>
    /// Advanced codec options for HDR and high-quality encoding
    /// </summary>
    public AdvancedCodecOptions? AdvancedOptions { get; init; }
    
    public AdvancedExportPreset(
        string Name,
        string Description,
        Platform Platform,
        string Container,
        string VideoCodec,
        string AudioCodec,
        Resolution Resolution,
        int FrameRate,
        int VideoBitrate,
        int AudioBitrate,
        string PixelFormat,
        string ColorSpace,
        AspectRatio AspectRatio,
        QualityLevel Quality,
        AdvancedCodecOptions? AdvancedOptions = null,
        int? MaxDuration = null)
        : base(Name, Description, Platform, Container, VideoCodec, AudioCodec,
               Resolution, FrameRate, VideoBitrate, AudioBitrate, PixelFormat,
               ColorSpace, AspectRatio, Quality, MaxDuration)
    {
        this.AdvancedOptions = AdvancedOptions;
    }
}

/// <summary>
/// Predefined HDR and advanced export presets
/// </summary>
public static class HdrPresets
{
    /// <summary>
    /// YouTube 4K HDR10 preset (3840x2160, HEVC 10-bit, PQ, Rec.2020, 60fps)
    /// </summary>
    public static AdvancedExportPreset YouTube4KHdr10 => new(
        Name: "YouTube 4K HDR10",
        Description: "4K HDR10 with PQ transfer function for YouTube",
        Platform: Platform.YouTube,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(3840, 2160),
        FrameRate: 60,
        VideoBitrate: 50000,
        AudioBitrate: 256,
        PixelFormat: "yuv420p10le",
        ColorSpace: "bt2020nc",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Maximum,
        AdvancedOptions: new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec2020,
            HdrTransferFunction = HdrTransferFunction.PQ,
            MaxContentLightLevel = 1000,
            MaxFrameAverageLightLevel = 400,
            MasterDisplayPrimaries = ColorPrimaries.Rec2020
        }
    );

    /// <summary>
    /// YouTube 1080p HDR10 preset (1920x1080, HEVC 10-bit, PQ, Rec.2020, 30fps)
    /// </summary>
    public static AdvancedExportPreset YouTube1080pHdr10 => new(
        Name: "YouTube 1080p HDR10",
        Description: "1080p HDR10 with PQ transfer function for YouTube",
        Platform: Platform.YouTube,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(1920, 1080),
        FrameRate: 30,
        VideoBitrate: 16000,
        AudioBitrate: 192,
        PixelFormat: "yuv420p10le",
        ColorSpace: "bt2020nc",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.High,
        AdvancedOptions: new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec2020,
            HdrTransferFunction = HdrTransferFunction.PQ,
            MaxContentLightLevel = 1000,
            MaxFrameAverageLightLevel = 400,
            MasterDisplayPrimaries = ColorPrimaries.Rec2020
        }
    );

    /// <summary>
    /// Generic 4K HLG HDR preset (3840x2160, HEVC 10-bit, HLG, Rec.2020)
    /// </summary>
    public static AdvancedExportPreset Generic4KHlg => new(
        Name: "4K HLG HDR",
        Description: "4K HDR with HLG transfer function for broadcast",
        Platform: Platform.Generic,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(3840, 2160),
        FrameRate: 30,
        VideoBitrate: 40000,
        AudioBitrate: 256,
        PixelFormat: "yuv420p10le",
        ColorSpace: "bt2020nc",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Maximum,
        AdvancedOptions: new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec2020,
            HdrTransferFunction = HdrTransferFunction.HLG,
            MaxContentLightLevel = 1000,
            MaxFrameAverageLightLevel = 400,
            MasterDisplayPrimaries = ColorPrimaries.Rec2020
        }
    );

    /// <summary>
    /// DCI-P3 10-bit preset (wide color gamut, no HDR)
    /// </summary>
    public static AdvancedExportPreset Generic4KDciP3 => new(
        Name: "4K DCI-P3 10-bit",
        Description: "4K with DCI-P3 wide color gamut, 10-bit encoding",
        Platform: Platform.Generic,
        Container: "mp4",
        VideoCodec: "libx265",
        AudioCodec: "aac",
        Resolution: new Resolution(3840, 2160),
        FrameRate: 24,
        VideoBitrate: 35000,
        AudioBitrate: 256,
        PixelFormat: "yuv420p10le",
        ColorSpace: "bt2020nc",
        AspectRatio: AspectRatio.SixteenByNine,
        Quality: QualityLevel.Maximum,
        AdvancedOptions: new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.DciP3,
            HdrTransferFunction = HdrTransferFunction.None,
            MasterDisplayPrimaries = ColorPrimaries.DciP3
        }
    );

    /// <summary>
    /// Get all HDR presets
    /// </summary>
    public static IReadOnlyList<AdvancedExportPreset> All => new[]
    {
        YouTube4KHdr10,
        YouTube1080pHdr10,
        Generic4KHlg,
        Generic4KDciP3
    };

    /// <summary>
    /// Get HDR preset by name
    /// </summary>
    public static AdvancedExportPreset? GetByName(string name)
    {
        foreach (var preset in All)
        {
            if (preset.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }
        return null;
    }
}
