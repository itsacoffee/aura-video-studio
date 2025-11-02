using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aura.Core.Services.FFmpeg;

/// <summary>
/// Validates and sanitizes FFmpeg commands to prevent security vulnerabilities
/// </summary>
public static class FFmpegCommandValidator
{
    // Whitelist of allowed FFmpeg options
    private static readonly HashSet<string> AllowedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "-i", "-c:v", "-c:a", "-b:v", "-b:a", "-r", "-s", "-pix_fmt",
        "-vf", "-af", "-f", "-y", "-n", "-t", "-ss", "-to",
        "-codec", "-acodec", "-vcodec", "-preset", "-crf", "-maxrate",
        "-bufsize", "-g", "-keyint_min", "-sc_threshold", "-threads",
        "-filter:v", "-filter:a", "-map", "-metadata", "-movflags",
        "-shortest", "-vsync", "-async", "-fps_mode", "-an", "-vn",
        "-hwaccel", "-hwaccel_device", "-hwaccel_output_format",
        "-quality", "-tune", "-profile", "-level", "-rc", "-qmin", "-qmax",
        "-refs", "-coder", "-flags", "-lookahead", "-spatial_aq", "-temporal_aq",
        "-profile:v", "-level:v", "-x264opts", "-x265-params"
    };

    // Dangerous patterns that should never appear in arguments
    private static readonly string[] DangerousPatterns = new[]
    {
        "file://",
        "pipe:",
        "concat:",
        "subfile:",
        "crypto:",
        "http://",
        "https://",
        "ftp://",
        "rtmp://",
        "tcp://",
        "udp://",
        "|",
        "&",
        ";",
        "`",
        "$(",
        "${",
        "&&",
        "||",
        "\n",
        "\r"
    };

    /// <summary>
    /// Validates FFmpeg command arguments for security issues
    /// </summary>
    /// <param name="arguments">The FFmpeg arguments to validate</param>
    /// <param name="workingDirectory">The allowed working directory for file paths</param>
    /// <returns>True if arguments are safe, false otherwise</returns>
    public static bool ValidateArguments(string arguments, string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return false;
        }

        // Check for dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (arguments.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Parse arguments into tokens
        var tokens = ParseArguments(arguments);

        // Validate each option
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // If it's an option (starts with -)
            if (token.StartsWith("-"))
            {
                // Check if option is in whitelist
                if (!AllowedOptions.Contains(token))
                {
                    return false;
                }

                // Special validation for filter options
                if (token == "-vf" || token == "-filter:v" || token == "-af" || token == "-filter:a")
                {
                    if (i + 1 < tokens.Count)
                    {
                        var filterValue = tokens[i + 1];
                        if (!ValidateFilterValue(filterValue))
                        {
                            return false;
                        }
                    }
                }
            }
            // If it's a file path (doesn't start with -)
            else if (workingDirectory != null && !token.StartsWith("-"))
            {
                // Validate file paths if working directory is provided
                if (!ValidateFilePath(token, workingDirectory))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Sanitizes text for use in FFmpeg drawtext filter
    /// </summary>
    public static string SanitizeDrawText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Escape special characters for drawtext filter
        var escaped = text
            .Replace("\\", "\\\\")  // Backslash must be first
            .Replace(":", "\\:")
            .Replace("'", "\\'")
            .Replace("%", "\\%")
            .Replace("\n", "\\n")
            .Replace("\r", string.Empty);

        // Remove control characters
        var cleaned = new StringBuilder();
        foreach (var c in escaped)
        {
            if (!char.IsControl(c) || c == '\\' || c == 'n')
            {
                cleaned.Append(c);
            }
        }

        return cleaned.ToString();
    }

    /// <summary>
    /// Validates a filter value for dangerous content
    /// </summary>
    private static bool ValidateFilterValue(string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
        {
            return false;
        }

        // Check for command injection attempts in filter values
        var dangerousFilterPatterns = new[]
        {
            "movie=",      // Can load arbitrary files
            "amovie=",     // Can load arbitrary audio files
            "lavfi=",      // Can reference other filters
            "sendcmd=",    // Can send commands
            "concat=",     // Can concatenate with arbitrary files
        };

        foreach (var pattern in dangerousFilterPatterns)
        {
            if (filterValue.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that a file path is within the allowed working directory
    /// </summary>
    private static bool ValidateFilePath(string path, string workingDirectory)
    {
        try
        {
            // Check for path traversal attempts
            if (path.Contains("..", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check for absolute path indicators that shouldn't be there
            if (Path.IsPathRooted(path) && !path.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Get full path
            var fullPath = Path.IsPathRooted(path) 
                ? Path.GetFullPath(path) 
                : Path.GetFullPath(Path.Combine(workingDirectory, path));

            // Ensure the resolved path is within the working directory
            if (!fullPath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
        catch
        {
            // If path parsing fails, reject it
            return false;
        }
    }

    /// <summary>
    /// Parses FFmpeg arguments into tokens, respecting quotes
    /// </summary>
    private static List<string> ParseArguments(string arguments)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < arguments.Length; i++)
        {
            char c = arguments[i];

            if ((c == '"' || c == '\'') && (i == 0 || arguments[i - 1] != '\\'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
                else
                {
                    currentToken.Append(c);
                }
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
            }
            else
            {
                currentToken.Append(c);
            }
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    /// <summary>
    /// Escapes a file path for safe use in FFmpeg arguments
    /// </summary>
    public static string EscapeFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // If path contains spaces or special characters, wrap in quotes
        if (path.Contains(' ') || path.Contains('(') || path.Contains(')'))
        {
            // Escape any existing quotes
            path = path.Replace("\"", "\\\"");
            return $"\"{path}\"";
        }

        return path;
    }
}
