using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.Errors;

/// <summary>
/// Categorized error types for FFmpeg operations
/// </summary>
public enum FfmpegErrorCategory
{
    NotFound,
    Corrupted,
    ProcessFailed,
    EncoderNotFound,
    InvalidInput,
    PermissionDenied,
    Timeout,
    Crashed,
    Unknown
}

/// <summary>
/// Structured exception for FFmpeg-related errors with categorization and remediation guidance
/// </summary>
public class FfmpegException : Exception
{
    public FfmpegErrorCategory Category { get; }
    public int? ExitCode { get; }
    public string? Stderr { get; }
    public string? JobId { get; }
    public string? CorrelationId { get; }
    public string[] SuggestedActions { get; }
    public string ErrorCode { get; }

    public FfmpegException(
        string message,
        FfmpegErrorCategory category,
        int? exitCode = null,
        string? stderr = null,
        string? jobId = null,
        string? correlationId = null,
        string[]? suggestedActions = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Category = category;
        ExitCode = exitCode;
        Stderr = stderr;
        JobId = jobId;
        CorrelationId = correlationId;
        SuggestedActions = suggestedActions ?? Array.Empty<string>();
        ErrorCode = GenerateErrorCode(category, exitCode);
    }

    private static string GenerateErrorCode(FfmpegErrorCategory category, int? exitCode)
    {
        var categoryCode = category switch
        {
            FfmpegErrorCategory.NotFound => "E302",
            FfmpegErrorCategory.Corrupted => "E303",
            FfmpegErrorCategory.ProcessFailed => "E304",
            FfmpegErrorCategory.EncoderNotFound => "E305",
            FfmpegErrorCategory.InvalidInput => "E306",
            FfmpegErrorCategory.PermissionDenied => "E307",
            FfmpegErrorCategory.Timeout => "E308",
            FfmpegErrorCategory.Crashed => "E309",
            _ => "E399"
        };

        return exitCode.HasValue ? $"{categoryCode}-{exitCode}" : categoryCode;
    }

    /// <summary>
    /// Creates an FFmpeg not found exception
    /// </summary>
    public static FfmpegException NotFound(string? path = null, string? correlationId = null)
    {
        var message = string.IsNullOrEmpty(path)
            ? "FFmpeg binary not found"
            : $"FFmpeg binary not found at {path}";

        return new FfmpegException(
            message,
            FfmpegErrorCategory.NotFound,
            correlationId: correlationId,
            suggestedActions: new[]
            {
                "Install FFmpeg via Download Center",
                "Attach an existing FFmpeg installation using 'Attach Existing'",
                "Add FFmpeg to system PATH",
                "Click 'Rescan' if FFmpeg was recently installed"
            });
    }

    /// <summary>
    /// Creates an FFmpeg process failed exception with stderr analysis
    /// </summary>
    public static FfmpegException FromProcessFailure(
        int exitCode,
        string? stderr,
        string? jobId = null,
        string? correlationId = null)
    {
        var category = AnalyzeErrorCategory(exitCode, stderr);
        var message = GenerateFriendlyMessage(category, exitCode, stderr);
        var actions = GenerateSuggestedActions(category, stderr);

        // Truncate stderr for storage (keep last 64KB)
        const int MaxStderrLength = 64 * 1024;
        var truncatedStderr = stderr != null && stderr.Length > MaxStderrLength
            ? "... (truncated)\n" + stderr.Substring(stderr.Length - MaxStderrLength)
            : stderr;

        return new FfmpegException(
            message,
            category,
            exitCode,
            truncatedStderr,
            jobId,
            correlationId,
            actions);
    }

    /// <summary>
    /// Analyzes stderr output and exit code to determine error category
    /// </summary>
    private static FfmpegErrorCategory AnalyzeErrorCategory(int exitCode, string? stderr)
    {
        if (string.IsNullOrEmpty(stderr))
        {
            if (exitCode < 0 || exitCode == -1073741515 || exitCode == -1094995529)
                return FfmpegErrorCategory.Crashed;
            
            return FfmpegErrorCategory.ProcessFailed;
        }

        var stderrLower = stderr.ToLowerInvariant();

        // Check for specific error patterns
        if (stderrLower.Contains("encoder") && (stderrLower.Contains("not found") || stderrLower.Contains("unknown encoder")))
            return FfmpegErrorCategory.EncoderNotFound;

        if (stderrLower.Contains("permission denied") || stderrLower.Contains("access is denied"))
            return FfmpegErrorCategory.PermissionDenied;

        if (stderrLower.Contains("invalid data") || 
            stderrLower.Contains("moov atom not found") ||
            stderrLower.Contains("could not find codec") ||
            stderrLower.Contains("invalid argument"))
            return FfmpegErrorCategory.InvalidInput;

        if (stderrLower.Contains("corrupted") || stderrLower.Contains("header"))
            return FfmpegErrorCategory.Corrupted;

        if (exitCode < 0 || exitCode == -1073741515 || exitCode == -1094995529)
            return FfmpegErrorCategory.Crashed;

        return FfmpegErrorCategory.ProcessFailed;
    }

    /// <summary>
    /// Generates user-friendly error message based on category
    /// </summary>
    private static string GenerateFriendlyMessage(FfmpegErrorCategory category, int exitCode, string? stderr)
    {
        return category switch
        {
            FfmpegErrorCategory.Crashed => 
                $"FFmpeg crashed unexpectedly (exit code: {exitCode}). This usually indicates a corrupted binary or missing system dependencies.",
            
            FfmpegErrorCategory.EncoderNotFound => 
                "Required video encoder not available in your FFmpeg installation.",
            
            FfmpegErrorCategory.PermissionDenied => 
                "FFmpeg cannot access the required files. Permission denied.",
            
            FfmpegErrorCategory.InvalidInput => 
                "Input file appears to be corrupted or in an unsupported format.",
            
            FfmpegErrorCategory.Corrupted => 
                "FFmpeg detected corrupted or invalid data in the input file.",
            
            FfmpegErrorCategory.ProcessFailed => 
                $"FFmpeg process failed with exit code {exitCode}.",
            
            _ => $"FFmpeg operation failed with exit code {exitCode}."
        };
    }

    /// <summary>
    /// Generates context-specific remediation suggestions
    /// </summary>
    private static string[] GenerateSuggestedActions(FfmpegErrorCategory category, string? stderr)
    {
        var actions = new List<string>();

        switch (category)
        {
            case FfmpegErrorCategory.Crashed:
                actions.Add("FFmpeg binary may be corrupted - try reinstalling or repairing");
                actions.Add("Check system dependencies (Visual C++ Redistributable on Windows)");
                actions.Add("If using hardware encoding (NVENC), try software encoding (x264) instead");
                break;

            case FfmpegErrorCategory.EncoderNotFound:
                actions.Add("Required encoder not available in your FFmpeg build");
                actions.Add("Use software encoder (x264) in render settings");
                actions.Add("Download FFmpeg with encoder support via Download Center");
                break;

            case FfmpegErrorCategory.PermissionDenied:
                actions.Add("Check file permissions on input/output paths");
                actions.Add("Ensure no other application is using the files");
                actions.Add("Try running with administrator privileges");
                break;

            case FfmpegErrorCategory.InvalidInput:
                actions.Add("Verify input files are valid and not corrupted");
                actions.Add("Try re-generating the audio/video files");
                actions.Add("Check if files are in a supported format");
                break;

            case FfmpegErrorCategory.Corrupted:
                actions.Add("Input file may be corrupted or incomplete");
                actions.Add("Try re-generating the source files");
                actions.Add("Verify TTS and image providers are working correctly");
                break;

            case FfmpegErrorCategory.Timeout:
                actions.Add("Operation took too long - try with shorter content");
                actions.Add("Check system resources (CPU, disk space)");
                actions.Add("Reduce video quality or resolution");
                break;

            default:
                actions.Add("Review FFmpeg log for detailed error information");
                actions.Add("Try with different render settings");
                actions.Add("Verify input files are valid");
                break;
        }

        // Add stderr-specific suggestions
        if (!string.IsNullOrEmpty(stderr))
        {
            var stderrLower = stderr.ToLowerInvariant();
            
            if (stderrLower.Contains("disk") && stderrLower.Contains("space"))
            {
                actions.Add("Free up disk space and try again");
            }

            if (stderrLower.Contains("memory") || stderrLower.Contains("malloc"))
            {
                actions.Add("Close other applications to free up memory");
            }

            if (stderrLower.Contains("gpu") || stderrLower.Contains("cuda") || stderrLower.Contains("nvenc"))
            {
                actions.Add("Update GPU drivers or switch to software encoding");
            }
        }

        return actions.ToArray();
    }

    /// <summary>
    /// Parses common FFmpeg error patterns from stderr
    /// </summary>
    public static Dictionary<string, string> ParseErrorPatterns(string? stderr)
    {
        var patterns = new Dictionary<string, string>();
        
        if (string.IsNullOrEmpty(stderr))
            return patterns;

        // Extract specific error messages
        var lines = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Look for lines starting with error indicators
            if (trimmed.StartsWith("Error", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("[error]", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                // Extract error type and message
                var parts = trimmed.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    patterns[key] = value;
                }
            }
        }

        return patterns;
    }
}
