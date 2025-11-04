using Aura.Core.Models;
using Aura.Core.Models.Export;
using Xunit;

namespace Aura.Tests.Export;

public class AdvancedCodecOptionsTests
{
    [Fact]
    public void AdvancedCodecOptions_Default_IsNotHdr()
    {
        // Arrange
        var options = new AdvancedCodecOptions();

        // Assert
        Assert.False(options.IsHdr);
        Assert.Equal(ColorDepth.EightBit, options.ColorDepth);
        Assert.Equal(ColorSpaceStandard.Rec709, options.ColorSpaceStandard);
        Assert.Equal(HdrTransferFunction.None, options.HdrTransferFunction);
    }

    [Fact]
    public void AdvancedCodecOptions_WithHdrPQ_IsHdr()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.PQ
        };

        // Assert
        Assert.True(options.IsHdr);
    }

    [Fact]
    public void AdvancedCodecOptions_WithHdrHLG_IsHdr()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.HLG
        };

        // Assert
        Assert.True(options.IsHdr);
    }

    [Fact]
    public void AdvancedCodecOptions_WithTenBit_Requires10Bit()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit
        };

        // Assert
        Assert.True(options.Requires10Bit);
    }

    [Fact]
    public void AdvancedCodecOptions_WithHdr_Requires10Bit()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.PQ,
            ColorDepth = ColorDepth.EightBit
        };

        // Assert
        Assert.True(options.Requires10Bit);
    }

    [Fact]
    public void GetPixelFormat_EightBit_ReturnsYuv420p()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.EightBit
        };

        // Act
        var pixelFormat = options.GetPixelFormat();

        // Assert
        Assert.Equal("yuv420p", pixelFormat);
    }

    [Fact]
    public void GetPixelFormat_TenBit_ReturnsYuv420p10le()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorDepth = ColorDepth.TenBit
        };

        // Act
        var pixelFormat = options.GetPixelFormat();

        // Assert
        Assert.Equal("yuv420p10le", pixelFormat);
    }

    [Fact]
    public void GetColorSpace_Rec709_ReturnsBt709()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorSpaceStandard = ColorSpaceStandard.Rec709
        };

        // Act
        var colorSpace = options.GetColorSpace();

        // Assert
        Assert.Equal("bt709", colorSpace);
    }

    [Fact]
    public void GetColorSpace_Rec2020_ReturnsBt2020nc()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorSpaceStandard = ColorSpaceStandard.Rec2020
        };

        // Act
        var colorSpace = options.GetColorSpace();

        // Assert
        Assert.Equal("bt2020nc", colorSpace);
    }

    [Fact]
    public void GetColorTransfer_PQ_ReturnsSmpte2084()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.PQ
        };

        // Act
        var transfer = options.GetColorTransfer();

        // Assert
        Assert.Equal("smpte2084", transfer);
    }

    [Fact]
    public void GetColorTransfer_HLG_ReturnsAribStdB67()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.HLG
        };

        // Act
        var transfer = options.GetColorTransfer();

        // Assert
        Assert.Equal("arib-std-b67", transfer);
    }

    [Fact]
    public void GetColorTransfer_None_ReturnsNull()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            HdrTransferFunction = HdrTransferFunction.None
        };

        // Act
        var transfer = options.GetColorTransfer();

        // Assert
        Assert.Null(transfer);
    }

    [Fact]
    public void GetColorPrimaries_Rec709_ReturnsBt709()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorSpaceStandard = ColorSpaceStandard.Rec709
        };

        // Act
        var primaries = options.GetColorPrimaries();

        // Assert
        Assert.Equal("bt709", primaries);
    }

    [Fact]
    public void GetColorPrimaries_Rec2020_ReturnsBt2020()
    {
        // Arrange
        var options = new AdvancedCodecOptions
        {
            ColorSpaceStandard = ColorSpaceStandard.Rec2020
        };

        // Act
        var primaries = options.GetColorPrimaries();

        // Assert
        Assert.Equal("bt2020", primaries);
    }

    [Fact]
    public void ColorPrimaries_DciP3_HasCorrectValues()
    {
        // Act
        var primaries = ColorPrimaries.DciP3;

        // Assert
        Assert.Equal(0.680, primaries.Red.X, precision: 3);
        Assert.Equal(0.320, primaries.Red.Y, precision: 3);
        Assert.Equal(1000, primaries.MaxLuminance);
    }

    [Fact]
    public void ColorPrimaries_Rec2020_HasCorrectValues()
    {
        // Act
        var primaries = ColorPrimaries.Rec2020;

        // Assert
        Assert.Equal(0.708, primaries.Red.X, precision: 3);
        Assert.Equal(0.292, primaries.Red.Y, precision: 3);
        Assert.Equal(1000, primaries.MaxLuminance);
    }

    [Fact]
    public void HdrPresets_YouTube4KHdr10_IsConfiguredCorrectly()
    {
        // Act
        var preset = HdrPresets.YouTube4KHdr10;

        // Assert
        Assert.Equal("YouTube 4K HDR10", preset.Name);
        Assert.NotNull(preset.AdvancedOptions);
        Assert.Equal(ColorDepth.TenBit, preset.AdvancedOptions.ColorDepth);
        Assert.Equal(HdrTransferFunction.PQ, preset.AdvancedOptions.HdrTransferFunction);
        Assert.Equal(ColorSpaceStandard.Rec2020, preset.AdvancedOptions.ColorSpaceStandard);
        Assert.True(preset.AdvancedOptions.IsHdr);
        Assert.Equal(1000, preset.AdvancedOptions.MaxContentLightLevel);
    }

    [Fact]
    public void HdrPresets_YouTube1080pHdr10_IsConfiguredCorrectly()
    {
        // Act
        var preset = HdrPresets.YouTube1080pHdr10;

        // Assert
        Assert.Equal("YouTube 1080p HDR10", preset.Name);
        Assert.NotNull(preset.AdvancedOptions);
        Assert.Equal(ColorDepth.TenBit, preset.AdvancedOptions.ColorDepth);
        Assert.True(preset.AdvancedOptions.IsHdr);
    }

    [Fact]
    public void HdrPresets_Generic4KHlg_UsesHLG()
    {
        // Act
        var preset = HdrPresets.Generic4KHlg;

        // Assert
        Assert.NotNull(preset.AdvancedOptions);
        Assert.Equal(HdrTransferFunction.HLG, preset.AdvancedOptions.HdrTransferFunction);
    }

    [Fact]
    public void HdrPresets_Generic4KDciP3_IsNotHdr()
    {
        // Act
        var preset = HdrPresets.Generic4KDciP3;

        // Assert
        Assert.NotNull(preset.AdvancedOptions);
        Assert.False(preset.AdvancedOptions.IsHdr);
        Assert.Equal(ColorSpaceStandard.DciP3, preset.AdvancedOptions.ColorSpaceStandard);
        Assert.Equal(ColorDepth.TenBit, preset.AdvancedOptions.ColorDepth);
    }

    [Fact]
    public void HdrPresets_All_ReturnsMultiplePresets()
    {
        // Act
        var presets = HdrPresets.All;

        // Assert
        Assert.NotEmpty(presets);
        Assert.True(presets.Count >= 4);
    }

    [Fact]
    public void HdrPresets_GetByName_FindsExistingPreset()
    {
        // Act
        var preset = HdrPresets.GetByName("YouTube 4K HDR10");

        // Assert
        Assert.NotNull(preset);
        Assert.Equal("YouTube 4K HDR10", preset.Name);
    }

    [Fact]
    public void HdrPresets_GetByName_ReturnsNullForUnknown()
    {
        // Act
        var preset = HdrPresets.GetByName("Non-Existent Preset");

        // Assert
        Assert.Null(preset);
    }
}
