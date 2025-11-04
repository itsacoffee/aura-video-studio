using Aura.Core.Models.Export;
using Aura.Core.Services.FFmpeg;
using Xunit;

namespace Aura.Tests.FFmpeg;

public class FFmpegCommandBuilderAdvancedTests
{
    [Fact]
    public void SetColorSpace_AddsColorSpaceOption()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .SetColorSpace("bt2020nc")
            .Build();

        // Assert
        Assert.Contains("-colorspace bt2020nc", command);
    }

    [Fact]
    public void SetColorTransfer_AddsColorTransferOption()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .SetColorTransfer("smpte2084")
            .Build();

        // Assert
        Assert.Contains("-color_trc smpte2084", command);
    }

    [Fact]
    public void SetColorPrimaries_AddsColorPrimariesOption()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .SetColorPrimaries("bt2020")
            .Build();

        // Assert
        Assert.Contains("-color_primaries bt2020", command);
    }

    [Fact]
    public void SetHdrMetadata_AddsX265ParamsOption()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .SetHdrMetadata(1000, 400)
            .Build();

        // Assert
        Assert.Contains("-x265-params \"max-cll=1000,400\"", command);
    }

    [Fact]
    public void ApplyAdvancedCodecOptions_WithHdr10_SetsAllHdrParameters()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var advancedOptions = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec2020,
            HdrTransferFunction = HdrTransferFunction.PQ,
            MaxContentLightLevel = 1000,
            MaxFrameAverageLightLevel = 400
        };

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .ApplyAdvancedCodecOptions(advancedOptions)
            .Build();

        // Assert
        Assert.Contains("-pix_fmt yuv420p10le", command);
        Assert.Contains("-colorspace bt2020nc", command);
        Assert.Contains("-color_trc smpte2084", command);
        Assert.Contains("-color_primaries bt2020", command);
        Assert.Contains("-x265-params \"max-cll=1000,400\"", command);
    }

    [Fact]
    public void ApplyAdvancedCodecOptions_WithHlg_SetsHlgTransferFunction()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var advancedOptions = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec2020,
            HdrTransferFunction = HdrTransferFunction.HLG,
            MaxContentLightLevel = 1000,
            MaxFrameAverageLightLevel = 400
        };

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .ApplyAdvancedCodecOptions(advancedOptions)
            .Build();

        // Assert
        Assert.Contains("-pix_fmt yuv420p10le", command);
        Assert.Contains("-color_trc arib-std-b67", command);
    }

    [Fact]
    public void ApplyAdvancedCodecOptions_WithDciP3NoHdr_Sets10BitButNoHdrMetadata()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var advancedOptions = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit,
            ColorSpaceStandard = ColorSpaceStandard.DciP3,
            HdrTransferFunction = HdrTransferFunction.None
        };

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx265")
            .ApplyAdvancedCodecOptions(advancedOptions)
            .Build();

        // Assert
        Assert.Contains("-pix_fmt yuv420p10le", command);
        Assert.DoesNotContain("-color_trc", command);
        Assert.DoesNotContain("-x265-params", command);
    }

    [Fact]
    public void ApplyAdvancedCodecOptions_WithStandard8Bit_UsesStandardPixelFormat()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var advancedOptions = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.EightBit,
            ColorSpaceStandard = ColorSpaceStandard.Rec709,
            HdrTransferFunction = HdrTransferFunction.None
        };

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx264")
            .ApplyAdvancedCodecOptions(advancedOptions)
            .Build();

        // Assert
        Assert.Contains("-pix_fmt yuv420p", command);
        Assert.Contains("-colorspace bt709", command);
        Assert.Contains("-color_primaries bt709", command);
    }

    [Fact]
    public void ApplyAdvancedCodecOptions_WithYouTube4KHdr10Preset_GeneratesCompleteHdrCommand()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder();
        var preset = HdrPresets.YouTube4KHdr10;
        var advancedOptions = preset.AdvancedOptions;

        // Act
        var command = builder
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec(preset.VideoCodec)
            .SetResolution(preset.Resolution.Width, preset.Resolution.Height)
            .SetFrameRate(preset.FrameRate)
            .SetVideoBitrate(preset.VideoBitrate)
            .ApplyAdvancedCodecOptions(advancedOptions!)
            .Build();

        // Assert
        Assert.Contains("libx265", command);
        Assert.Contains("-s 3840x2160", command);
        Assert.Contains("-r 60", command);
        Assert.Contains("-b:v 50000k", command);
        Assert.Contains("-pix_fmt yuv420p10le", command);
        Assert.Contains("-colorspace bt2020nc", command);
        Assert.Contains("-color_trc smpte2084", command);
        Assert.Contains("-color_primaries bt2020", command);
        Assert.Contains("-x265-params \"max-cll=1000,400\"", command);
    }
}
