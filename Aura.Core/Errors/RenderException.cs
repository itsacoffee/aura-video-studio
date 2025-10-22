using System;

namespace Aura.Core.Errors;

/// <summary>
/// Exception thrown when video rendering or composition fails.
/// This wraps FFmpeg-related errors and provides user-friendly error messages.
/// </summary>
public class RenderException : AuraException
{
    /// <summary>
    /// Job ID associated with the render operation if applicable
    /// </summary>
    public string? JobId { get; }

    /// <summary>
    /// Exit code from the rendering process if applicable
    /// </summary>
    public int? ExitCode { get; }

    /// <summary>
    /// Standard error output from the rendering process (truncated)
    /// </summary>
    public string? ProcessOutput { get; }

    /// <summary>
    /// Category of the render error for more specific handling
    /// </summary>
    public RenderErrorCategory Category { get; }

    public RenderException(
        string message,
        RenderErrorCategory category,
        string? userMessage = null,
        string? jobId = null,
        int? exitCode = null,
        string? processOutput = null,
        string? correlationId = null,
        string[]? suggestedActions = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(
            message,
            GenerateErrorCode(category, exitCode),
            userMessage ?? GenerateUserMessage(category, message),
            correlationId,
            suggestedActions ?? GenerateDefaultSuggestedActions(category),
            isTransient,
            innerException)
    {
        JobId = jobId;
        ExitCode = exitCode;
        ProcessOutput = processOutput;
        Category = category;

        // Add render context
        if (!string.IsNullOrEmpty(jobId))
        {
            WithContext("jobId", jobId);
        }
        if (exitCode.HasValue)
        {
            WithContext("exitCode", exitCode.Value);
        }
        WithContext("category", category.ToString());
    }

    private static string GenerateErrorCode(RenderErrorCategory category, int? exitCode)
    {
        var baseCode = category switch
        {
            RenderErrorCategory.FfmpegNotFound => "E302",
            RenderErrorCategory.FfmpegCorrupted => "E303",
            RenderErrorCategory.ProcessFailed => "E304",
            RenderErrorCategory.EncoderNotAvailable => "E305",
            RenderErrorCategory.InvalidInput => "E306",
            RenderErrorCategory.PermissionDenied => "E307",
            RenderErrorCategory.Timeout => "E308",
            RenderErrorCategory.HardwareEncoderFailed => "E310",
            RenderErrorCategory.Cancelled => "E311",
            _ => "E399"
        };

        return exitCode.HasValue ? $"{baseCode}-{exitCode}" : baseCode;
    }

    private static string GenerateUserMessage(RenderErrorCategory category, string message)
    {
        return category switch
        {
            RenderErrorCategory.FfmpegNotFound =>
                "FFmpeg is not installed or not found. Please install FFmpeg to render videos.",
            RenderErrorCategory.FfmpegCorrupted =>
                "FFmpeg binary appears to be corrupted. Please reinstall FFmpeg.",
            RenderErrorCategory.EncoderNotAvailable =>
                "Required video encoder is not available in your FFmpeg installation.",
            RenderErrorCategory.InvalidInput =>
                "Invalid input provided to the rendering engine.",
            RenderErrorCategory.PermissionDenied =>
                "Permission denied when accessing files for rendering.",
            RenderErrorCategory.Timeout =>
                "Rendering operation timed out. The video may be too long or complex.",
            RenderErrorCategory.HardwareEncoderFailed =>
                "Hardware encoder failed. Falling back to software encoding may help.",
            RenderErrorCategory.Cancelled =>
                "Rendering was cancelled by user.",
            RenderErrorCategory.ProcessFailed =>
                "Video rendering failed. Please check the input files and try again.",
            _ => message
        };
    }

    private static string[] GenerateDefaultSuggestedActions(RenderErrorCategory category)
    {
        return category switch
        {
            RenderErrorCategory.FfmpegNotFound => new[]
            {
                "Install FFmpeg via Download Center in Settings",
                "Attach existing FFmpeg installation",
                "Add FFmpeg to system PATH"
            },
            RenderErrorCategory.FfmpegCorrupted => new[]
            {
                "Reinstall FFmpeg via Download Center",
                "Check system dependencies (Visual C++ Redistributable)",
                "Verify FFmpeg binary integrity"
            },
            RenderErrorCategory.EncoderNotAvailable => new[]
            {
                "Use software encoder (x264) instead of hardware encoder",
                "Download FFmpeg with full codec support",
                "Check encoder requirements in render settings"
            },
            RenderErrorCategory.InvalidInput => new[]
            {
                "Verify input files are valid and not corrupted",
                "Regenerate audio and image assets",
                "Check input file formats"
            },
            RenderErrorCategory.PermissionDenied => new[]
            {
                "Check file and directory permissions",
                "Ensure no other application is using the files",
                "Try running with administrator privileges"
            },
            RenderErrorCategory.Timeout => new[]
            {
                "Try with shorter content",
                "Reduce video quality or resolution",
                "Check system resources (CPU, memory, disk)",
                "Close other resource-intensive applications"
            },
            RenderErrorCategory.HardwareEncoderFailed => new[]
            {
                "Update GPU drivers",
                "Switch to software encoder (x264) in settings",
                "Check GPU compatibility",
                "Reduce encoding quality or resolution"
            },
            RenderErrorCategory.Cancelled => new[]
            {
                "Restart the rendering operation if it was cancelled unintentionally"
            },
            RenderErrorCategory.ProcessFailed => new[]
            {
                "Check input files are valid",
                "Review render settings",
                "Try with different encoder settings",
                "Check FFmpeg logs for detailed error information"
            },
            _ => new[] { "Review technical details and retry" }
        };
    }

    /// <summary>
    /// Creates a RenderException from an FFmpeg exception
    /// </summary>
    public static RenderException FromFfmpegException(FfmpegException ffmpegEx, string? jobId = null)
    {
        var category = ffmpegEx.Category switch
        {
            FfmpegErrorCategory.NotFound => RenderErrorCategory.FfmpegNotFound,
            FfmpegErrorCategory.Corrupted => RenderErrorCategory.FfmpegCorrupted,
            FfmpegErrorCategory.EncoderNotFound => RenderErrorCategory.EncoderNotAvailable,
            FfmpegErrorCategory.InvalidInput => RenderErrorCategory.InvalidInput,
            FfmpegErrorCategory.PermissionDenied => RenderErrorCategory.PermissionDenied,
            FfmpegErrorCategory.Timeout => RenderErrorCategory.Timeout,
            FfmpegErrorCategory.Crashed => RenderErrorCategory.FfmpegCorrupted,
            _ => RenderErrorCategory.ProcessFailed
        };

        return new RenderException(
            ffmpegEx.Message,
            category,
            jobId: jobId ?? ffmpegEx.JobId,
            exitCode: ffmpegEx.ExitCode,
            processOutput: ffmpegEx.Stderr,
            correlationId: ffmpegEx.CorrelationId,
            suggestedActions: ffmpegEx.SuggestedActions,
            innerException: ffmpegEx);
    }

    /// <summary>
    /// Creates a RenderException for hardware encoder failures
    /// </summary>
    public static RenderException HardwareEncoderFailed(string encoderName, string? jobId = null, string? correlationId = null, Exception? innerException = null)
    {
        return new RenderException(
            $"Hardware encoder '{encoderName}' failed",
            RenderErrorCategory.HardwareEncoderFailed,
            $"Hardware encoder '{encoderName}' is not available or failed. Consider using software encoding instead.",
            jobId,
            correlationId: correlationId,
            innerException: innerException);
    }

    /// <summary>
    /// Creates a RenderException for cancellation
    /// </summary>
    public static RenderException Cancelled(string? jobId = null, string? correlationId = null)
    {
        return new RenderException(
            "Rendering was cancelled",
            RenderErrorCategory.Cancelled,
            jobId: jobId,
            correlationId: correlationId,
            isTransient: false);
    }

    public override Dictionary<string, object> ToErrorResponse()
    {
        var response = base.ToErrorResponse();
        response["category"] = Category.ToString();
        if (!string.IsNullOrEmpty(JobId))
        {
            response["jobId"] = JobId;
        }
        if (ExitCode.HasValue)
        {
            response["exitCode"] = ExitCode.Value;
        }
        return response;
    }
}

/// <summary>
/// Categories of rendering errors
/// </summary>
public enum RenderErrorCategory
{
    FfmpegNotFound,
    FfmpegCorrupted,
    ProcessFailed,
    EncoderNotAvailable,
    InvalidInput,
    PermissionDenied,
    Timeout,
    HardwareEncoderFailed,
    Cancelled,
    Unknown
}
