using Aura.Core.Models;
using Aura.Core.Models.Export;
using Xunit;

namespace Aura.Tests.Export;

public class HardwareEncoderSelectionTests
{
    [Theory]
    [InlineData(HardwareTier.A, "NVIDIA", "h264_nvenc")]
    [InlineData(HardwareTier.B, "NVIDIA", "h264_nvenc")]
    [InlineData(HardwareTier.C, "NVIDIA", "h264_nvenc")]
    [InlineData(HardwareTier.D, "NVIDIA", "libx264")]
    public void GetRecommendedEncoder_WithNVIDIA_ReturnsCorrectEncoder(
        HardwareTier tier, 
        string vendor, 
        string expectedEncoder)
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            tier, 
            vendor, 
            hardwareAccelerationEnabled: true);

        // Assert
        Assert.Equal(expectedEncoder, encoder);
    }

    [Theory]
    [InlineData(HardwareTier.A, "AMD", "h264_amf")]
    [InlineData(HardwareTier.B, "AMD", "h264_amf")]
    [InlineData(HardwareTier.C, "AMD", "h264_amf")]
    [InlineData(HardwareTier.D, "AMD", "libx264")]
    public void GetRecommendedEncoder_WithAMD_ReturnsCorrectEncoder(
        HardwareTier tier, 
        string vendor, 
        string expectedEncoder)
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            tier, 
            vendor, 
            hardwareAccelerationEnabled: true);

        // Assert
        Assert.Equal(expectedEncoder, encoder);
    }

    [Theory]
    [InlineData(HardwareTier.A, "Intel", "h264_qsv")]
    [InlineData(HardwareTier.B, "Intel", "h264_qsv")]
    [InlineData(HardwareTier.C, "Intel", "h264_qsv")]
    [InlineData(HardwareTier.D, "Intel", "libx264")]
    public void GetRecommendedEncoder_WithIntel_ReturnsCorrectEncoder(
        HardwareTier tier, 
        string vendor, 
        string expectedEncoder)
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            tier, 
            vendor, 
            hardwareAccelerationEnabled: true);

        // Assert
        Assert.Equal(expectedEncoder, encoder);
    }

    [Fact]
    public void GetRecommendedEncoder_WithHardwareDisabled_ReturnsSoftwareEncoder()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            HardwareTier.A, 
            "NVIDIA", 
            hardwareAccelerationEnabled: false);

        // Assert
        Assert.Equal("libx264", encoder);
    }

    [Theory]
    [InlineData(HardwareTier.A, QualityLevel.Draft, true, "fast")]
    [InlineData(HardwareTier.A, QualityLevel.High, true, "slow")]
    [InlineData(HardwareTier.D, QualityLevel.Draft, false, "ultrafast")]
    [InlineData(HardwareTier.A, QualityLevel.Maximum, false, "veryslow")]
    public void GetEncoderPreset_ReturnsCorrectPreset(
        HardwareTier tier,
        QualityLevel quality,
        bool isHardwareEncoder,
        string expectedPreset)
    {
        // Act
        var preset = HardwareEncoderSelection.GetEncoderPreset(tier, quality, isHardwareEncoder);

        // Assert
        Assert.Equal(expectedPreset, preset);
    }

    [Fact]
    public void GetEncoderParameters_ForSoftwareEncoder_IncludesCRF()
    {
        // Act
        var parameters = HardwareEncoderSelection.GetEncoderParameters(
            HardwareTier.B, 
            QualityLevel.High, 
            isHardwareEncoder: false);

        // Assert
        Assert.Contains("crf", parameters.Keys);
        Assert.Equal("20", parameters["crf"]);
    }

    [Fact]
    public void GetEncoderParameters_ForHardwareEncoder_IncludesRC()
    {
        // Act
        var parameters = HardwareEncoderSelection.GetEncoderParameters(
            HardwareTier.A, 
            QualityLevel.High, 
            isHardwareEncoder: true);

        // Assert
        Assert.Contains("rc", parameters.Keys);
        Assert.Equal("vbr", parameters["rc"]);
    }

    [Fact]
    public void GetRecommendedEncoder_ForH265Preset_ReturnsHEVCEncoder()
    {
        // Arrange
        var preset = ExportPresets.YouTube4K;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            HardwareTier.A, 
            "NVIDIA", 
            hardwareAccelerationEnabled: true);

        // Assert
        Assert.Equal("hevc_nvenc", encoder);
    }

    [Fact]
    public void GetRecommendedEncoder_ForH265WithLowTier_FallsBackToSoftware()
    {
        // Arrange
        var preset = ExportPresets.YouTube4K;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoder(
            preset, 
            HardwareTier.C, 
            "NVIDIA", 
            hardwareAccelerationEnabled: true);

        // Assert
        Assert.Equal("libx265", encoder);
    }

    [Theory]
    [InlineData("NVIDIA", HardwareTier.A, true)]
    [InlineData("NVIDIA", HardwareTier.B, true)]
    [InlineData("NVIDIA", HardwareTier.C, false)]
    [InlineData("AMD", HardwareTier.A, true)]
    [InlineData("AMD", HardwareTier.B, true)]
    [InlineData("AMD", HardwareTier.C, false)]
    [InlineData("Intel", HardwareTier.A, true)]
    [InlineData("Intel", HardwareTier.B, false)]
    public void SupportsHardware10Bit_ReturnsCorrectValue(string vendor, HardwareTier tier, bool expected)
    {
        // Act
        var supports10Bit = HardwareEncoderSelection.SupportsHardware10Bit(vendor, tier);

        // Assert
        Assert.Equal(expected, supports10Bit);
    }

    [Fact]
    public void GetRecommendedEncoderWithAdvancedOptions_WithHdr10Bit_SelectsCorrectEncoder()
    {
        // Arrange
        var preset = HdrPresets.YouTube4KHdr10;
        var advancedOptions = preset.AdvancedOptions;

        // Act - High-end NVIDIA card should support 10-bit
        var encoder = HardwareEncoderSelection.GetRecommendedEncoderWithAdvancedOptions(
            preset,
            HardwareTier.A,
            "NVIDIA",
            hardwareAccelerationEnabled: true,
            advancedOptions);

        // Assert - Should use hardware encoder for 10-bit on tier A
        Assert.Equal("hevc_nvenc", encoder);
    }

    [Fact]
    public void GetRecommendedEncoderWithAdvancedOptions_WithHdr10BitLowTier_FallsBackToSoftware()
    {
        // Arrange
        var preset = HdrPresets.YouTube4KHdr10;
        var advancedOptions = preset.AdvancedOptions;

        // Act - Low-end hardware doesn't support 10-bit
        var encoder = HardwareEncoderSelection.GetRecommendedEncoderWithAdvancedOptions(
            preset,
            HardwareTier.C,
            "NVIDIA",
            hardwareAccelerationEnabled: true,
            advancedOptions);

        // Assert - Should fall back to software encoder
        Assert.Equal("libx265", encoder);
    }

    [Fact]
    public void GetRecommendedEncoderWithAdvancedOptions_WithoutAdvancedOptions_UsesStandardLogic()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoderWithAdvancedOptions(
            preset,
            HardwareTier.A,
            "NVIDIA",
            hardwareAccelerationEnabled: true,
            advancedOptions: null);

        // Assert - Should use standard encoder selection
        Assert.Equal("h264_nvenc", encoder);
    }

    [Fact]
    public void GetRecommendedEncoderWithAdvancedOptions_With8BitOptions_UsesHardwareEncoder()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var advancedOptions = new AdvancedCodecOptions { ColorDepth = ColorDepth.EightBit };

        // Act
        var encoder = HardwareEncoderSelection.GetRecommendedEncoderWithAdvancedOptions(
            preset,
            HardwareTier.A,
            "NVIDIA",
            hardwareAccelerationEnabled: true,
            advancedOptions);

        // Assert
        Assert.Equal("h264_nvenc", encoder);
    }
}
