using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service for converting video formats
/// </summary>
public interface IFormatConversionService
{
    /// <summary>
    /// Check if format conversion is needed
    /// </summary>
    bool IsConversionNeeded(string sourceFormat, string targetFormat);
    
    /// <summary>
    /// Get recommended codec for a container format
    /// </summary>
    string GetRecommendedVideoCodec(string containerFormat);
    
    /// <summary>
    /// Get recommended audio codec for a container format
    /// </summary>
    string GetRecommendedAudioCodec(string containerFormat);
    
    /// <summary>
    /// Validate if a codec is compatible with a container
    /// </summary>
    bool IsCodecCompatible(string codec, string container);
}

/// <summary>
/// Implementation of format conversion service
/// </summary>
public class FormatConversionService : IFormatConversionService
{
    private readonly ILogger<FormatConversionService> _logger;

    public FormatConversionService(ILogger<FormatConversionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsConversionNeeded(string sourceFormat, string targetFormat)
    {
        var normalizedSource = sourceFormat.ToLowerInvariant().TrimStart('.');
        var normalizedTarget = targetFormat.ToLowerInvariant().TrimStart('.');
        
        return normalizedSource != normalizedTarget;
    }

    public string GetRecommendedVideoCodec(string containerFormat)
    {
        return containerFormat.ToLowerInvariant() switch
        {
            "mp4" => "libx264",
            "mov" => "libx264",
            "webm" => "libvpx-vp9",
            "mkv" => "libx264",
            "avi" => "libx264",
            _ => "libx264" // Default to H.264
        };
    }

    public string GetRecommendedAudioCodec(string containerFormat)
    {
        return containerFormat.ToLowerInvariant() switch
        {
            "mp4" => "aac",
            "mov" => "aac",
            "webm" => "libopus",
            "mkv" => "aac",
            "avi" => "mp3",
            _ => "aac" // Default to AAC
        };
    }

    public bool IsCodecCompatible(string codec, string container)
    {
        var normalizedCodec = codec.ToLowerInvariant();
        var normalizedContainer = container.ToLowerInvariant();

        return (normalizedContainer, normalizedCodec) switch
        {
            ("mp4", "libx264" or "libx265" or "h264" or "hevc" or "aac" or "mp3") => true,
            ("mov", "libx264" or "libx265" or "h264" or "hevc" or "prores" or "aac") => true,
            ("webm", "libvpx" or "libvpx-vp9" or "av1" or "libopus" or "vorbis") => true,
            ("mkv", _) => true, // MKV supports most codecs
            ("avi", "libx264" or "mpeg4" or "mp3") => true,
            _ => false
        };
    }
}
