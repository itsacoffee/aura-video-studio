using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Download;

namespace Aura.Core.Services.Download;

/// <summary>
/// Configuration for download mirrors
/// </summary>
public class MirrorConfiguration
{
    /// <summary>
    /// Get default FFmpeg mirrors
    /// </summary>
    public static List<DownloadMirror> GetDefaultFfmpegMirrors()
    {
        return new List<DownloadMirror>
        {
            new DownloadMirror
            {
                Id = "github-primary",
                Name = "GitHub Releases (Primary)",
                Url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip",
                Priority = 1,
                IsEnabled = true,
                HealthStatus = MirrorHealthStatus.Unknown
            },
            new DownloadMirror
            {
                Id = "github-fallback",
                Name = "GitHub Releases (Fallback)",
                Url = "https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip",
                Priority = 2,
                IsEnabled = true,
                HealthStatus = MirrorHealthStatus.Unknown
            },
            new DownloadMirror
            {
                Id = "ffmpeg-org",
                Name = "FFmpeg.org Official",
                Url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
                Priority = 3,
                IsEnabled = true,
                HealthStatus = MirrorHealthStatus.Unknown
            }
        };
    }

    /// <summary>
    /// Get default Ollama mirrors
    /// </summary>
    public static List<DownloadMirror> GetDefaultOllamaMirrors()
    {
        return new List<DownloadMirror>
        {
            new DownloadMirror
            {
                Id = "ollama-github",
                Name = "Ollama GitHub Releases",
                Url = "https://github.com/ollama/ollama/releases/download/v0.1.19/ollama-windows-amd64.zip",
                Priority = 1,
                IsEnabled = true,
                HealthStatus = MirrorHealthStatus.Unknown
            }
        };
    }

    /// <summary>
    /// Get all default mirrors for a component
    /// </summary>
    public static List<DownloadMirror> GetMirrorsForComponent(string componentName)
    {
        return componentName.ToLowerInvariant() switch
        {
            "ffmpeg" => GetDefaultFfmpegMirrors(),
            "ollama" => GetDefaultOllamaMirrors(),
            _ => new List<DownloadMirror>()
        };
    }

    /// <summary>
    /// Validate mirror configuration
    /// </summary>
    public static bool ValidateMirrors(List<DownloadMirror> mirrors)
    {
        if (mirrors == null || mirrors.Count == 0)
        {
            return false;
        }

        // Check for duplicate IDs
        var distinctIds = mirrors.Select(m => m.Id).Distinct().Count();
        if (distinctIds != mirrors.Count)
        {
            return false;
        }

        // Check that all mirrors have required fields
        return mirrors.All(m =>
            !string.IsNullOrWhiteSpace(m.Id) &&
            !string.IsNullOrWhiteSpace(m.Name) &&
            !string.IsNullOrWhiteSpace(m.Url));
    }
}
