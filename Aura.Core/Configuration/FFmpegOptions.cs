using System.ComponentModel.DataAnnotations;

namespace Aura.Core.Configuration;

/// <summary>
/// Configuration options for FFmpeg path resolution and validation
/// </summary>
public class FFmpegOptions
{
    /// <summary>
    /// Explicit path to ffmpeg executable. Empty means auto-detect.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Explicit path to ffprobe executable. Empty means auto-detect.
    /// </summary>
    public string ProbeExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Search paths to check for FFmpeg installation.
    /// Checked in order before falling back to PATH environment variable.
    /// </summary>
    [MinLength(1, ErrorMessage = "At least one search path must be configured")]
    public List<string> SearchPaths { get; set; } = new();

    /// <summary>
    /// Minimum required FFmpeg version (e.g., "4.0.0").
    /// Empty means no version requirement.
    /// </summary>
    public string RequireMinimumVersion { get; set; } = string.Empty;

    /// <summary>
    /// Validates the options and returns default values if needed
    /// </summary>
    public static FFmpegOptions GetDefaultOptions()
    {
        var options = new FFmpegOptions
        {
            SearchPaths = GetDefaultSearchPaths()
        };
        return options;
    }

    /// <summary>
    /// Returns platform-specific default search paths
    /// </summary>
    private static List<string> GetDefaultSearchPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            paths.AddRange(new[]
            {
                @"C:\Program Files\ffmpeg\bin",
                @"C:\ffmpeg\bin",
                @"%LOCALAPPDATA%\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-7.1-full_build\bin",
                @"%ProgramFiles%\ffmpeg\bin",
                @"%ProgramFiles(x86)%\ffmpeg\bin",
                @"%USERPROFILE%\Downloads\ffmpeg\bin"
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            paths.AddRange(new[]
            {
                "/usr/bin",
                "/usr/local/bin",
                "/snap/bin",
                "/opt/ffmpeg/bin",
                "$HOME/.local/bin"
            });
        }
        else if (OperatingSystem.IsMacOS())
        {
            paths.AddRange(new[]
            {
                "/usr/local/bin",
                "/opt/homebrew/bin",
                "/opt/local/bin",
                "/usr/bin",
                "$HOME/.local/bin"
            });
        }

        return paths;
    }
}
