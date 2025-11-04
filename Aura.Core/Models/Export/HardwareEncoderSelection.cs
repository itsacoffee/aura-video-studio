using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Export;

/// <summary>
/// Hardware encoder preferences based on hardware tier and GPU vendor
/// </summary>
public static class HardwareEncoderSelection
{
    /// <summary>
    /// Gets the recommended encoder for a preset based on hardware capabilities
    /// </summary>
    public static string GetRecommendedEncoder(
        ExportPreset preset,
        HardwareTier tier,
        string? gpuVendor,
        bool hardwareAccelerationEnabled)
    {
        if (!hardwareAccelerationEnabled || tier == HardwareTier.D)
        {
            return GetSoftwareEncoder(preset);
        }

        var preferredEncoder = GetHardwareEncoder(preset.VideoCodec, gpuVendor, tier);
        return preferredEncoder ?? GetSoftwareEncoder(preset);
    }

    /// <summary>
    /// Gets hardware encoder based on codec, GPU vendor, and tier
    /// </summary>
    private static string? GetHardwareEncoder(string codec, string? gpuVendor, HardwareTier tier)
    {
        if (string.IsNullOrEmpty(gpuVendor))
        {
            return null;
        }

        var vendor = gpuVendor.ToLowerInvariant();

        return codec.ToLowerInvariant() switch
        {
            "libx264" or "h264" => GetH264HardwareEncoder(vendor, tier),
            "libx265" or "h265" or "hevc" => GetH265HardwareEncoder(vendor, tier),
            _ => null
        };
    }

    private static string? GetH264HardwareEncoder(string vendor, HardwareTier tier)
    {
        if (vendor.Contains("nvidia"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "h264_nvenc",
                HardwareTier.C => "h264_nvenc",
                _ => null
            };
        }

        if (vendor.Contains("amd"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "h264_amf",
                HardwareTier.C => "h264_amf",
                _ => null
            };
        }

        if (vendor.Contains("intel"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "h264_qsv",
                HardwareTier.C => "h264_qsv",
                _ => null
            };
        }

        return null;
    }

    private static string? GetH265HardwareEncoder(string vendor, HardwareTier tier)
    {
        if (vendor.Contains("nvidia"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "hevc_nvenc",
                _ => null
            };
        }

        if (vendor.Contains("amd"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "hevc_amf",
                _ => null
            };
        }

        if (vendor.Contains("intel"))
        {
            return tier switch
            {
                HardwareTier.A or HardwareTier.B => "hevc_qsv",
                _ => null
            };
        }

        return null;
    }

    private static string GetSoftwareEncoder(ExportPreset preset)
    {
        return preset.VideoCodec;
    }

    /// <summary>
    /// Gets encoder quality preset based on hardware tier and export quality
    /// </summary>
    public static string GetEncoderPreset(HardwareTier tier, QualityLevel quality, bool isHardwareEncoder)
    {
        if (isHardwareEncoder)
        {
            return GetHardwareEncoderPreset(tier, quality);
        }

        return GetSoftwareEncoderPreset(tier, quality);
    }

    private static string GetHardwareEncoderPreset(HardwareTier tier, QualityLevel quality)
    {
        return quality switch
        {
            QualityLevel.Draft => "fast",
            QualityLevel.Good => "medium",
            QualityLevel.High => "slow",
            QualityLevel.Maximum => "slow",
            _ => "medium"
        };
    }

    private static string GetSoftwareEncoderPreset(HardwareTier tier, QualityLevel quality)
    {
        return (tier, quality) switch
        {
            (HardwareTier.D, QualityLevel.Draft) => "ultrafast",
            (HardwareTier.D, _) => "veryfast",
            (HardwareTier.C, QualityLevel.Draft) => "veryfast",
            (HardwareTier.C, QualityLevel.Good) => "fast",
            (HardwareTier.C, _) => "medium",
            (HardwareTier.B, QualityLevel.Draft) => "fast",
            (HardwareTier.B, QualityLevel.Good) => "medium",
            (HardwareTier.B, QualityLevel.High) => "slow",
            (HardwareTier.B, QualityLevel.Maximum) => "slower",
            (HardwareTier.A, QualityLevel.Draft) => "medium",
            (HardwareTier.A, QualityLevel.Good) => "slow",
            (HardwareTier.A, QualityLevel.High) => "slower",
            (HardwareTier.A, QualityLevel.Maximum) => "veryslow",
            _ => "medium"
        };
    }

    /// <summary>
    /// Gets additional encoder parameters based on tier
    /// </summary>
    public static Dictionary<string, string> GetEncoderParameters(
        HardwareTier tier,
        QualityLevel quality,
        bool isHardwareEncoder)
    {
        var parameters = new Dictionary<string, string>();

        if (isHardwareEncoder)
        {
            parameters["rc"] = "vbr";
            
            if (quality == QualityLevel.Maximum)
            {
                parameters["qmin"] = "15";
                parameters["qmax"] = "25";
            }
        }
        else
        {
            var crf = quality switch
            {
                QualityLevel.Draft => "28",
                QualityLevel.Good => "23",
                QualityLevel.High => "20",
                QualityLevel.Maximum => "18",
                _ => "23"
            };
            
            parameters["crf"] = crf;
        }

        return parameters;
    }
}
