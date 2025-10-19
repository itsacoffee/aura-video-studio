using System;
using System.IO;
using Aura.Core.Models;

namespace Aura.Core.Errors;

/// <summary>
/// Maps exceptions to standardized error taxonomy with codes, messages, and remediation guidance
/// </summary>
public static class ErrorMapper
{
    /// <summary>
    /// Maps an exception to a JobStepError with taxonomy code
    /// </summary>
    public static JobStepError MapException(Exception ex, string? correlationId = null, string? stepName = null)
    {
        return ex switch
        {
            FileNotFoundException => new JobStepError
            {
                Code = "FFmpegNotFound",
                Message = "FFmpeg executable not found. Please install FFmpeg to continue.",
                Remediation = "Install FFmpeg from Settings → Dependencies or visit ffmpeg.org",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            UnauthorizedAccessException when ex.Message.Contains("output") || ex.Message.Contains("write") => new JobStepError
            {
                Code = "OutputDirectoryNotWritable",
                Message = "Cannot write to output directory. Please check permissions.",
                Remediation = "Ensure the output directory exists and is writable, or change the output path in Settings",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            IOException ioEx when IsOutOfDiskSpace(ioEx) => new JobStepError
            {
                Code = "OutOfDiskSpace",
                Message = "Insufficient disk space to complete the operation.",
                Remediation = "Free up disk space and try again",
                Details = new { exception = ioEx.Message, step = stepName, correlationId }
            },
            
            TimeoutException => new JobStepError
            {
                Code = $"StepTimeout:{stepName ?? "Unknown"}",
                Message = $"Step '{stepName}' timed out. The operation took too long to complete.",
                Remediation = "Retry the operation or check system resources",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            HttpRequestException or System.Net.WebException => new JobStepError
            {
                Code = "TransientNetworkFailure",
                Message = "Network request failed. Please check your internet connection.",
                Remediation = "Check network connectivity and retry",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            ArgumentException when ex.Message.Contains("API key") || ex.Message.Contains("key") => new JobStepError
            {
                Code = ExtractApiKeyCode(ex.Message),
                Message = ex.Message,
                Remediation = "Add the required API key in Settings → Providers",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            InvalidOperationException when ex.Message.Contains("GPU") || ex.Message.Contains("CUDA") => new JobStepError
            {
                Code = "RequiresNvidiaGPU",
                Message = "This operation requires an NVIDIA GPU with CUDA support.",
                Remediation = "Use CPU-based providers or install NVIDIA GPU drivers",
                Details = new { exception = ex.Message, step = stepName, correlationId }
            },
            
            PlatformNotSupportedException => new JobStepError
            {
                Code = $"UnsupportedOS:{Environment.OSVersion.Platform}",
                Message = $"This feature is not supported on {Environment.OSVersion.Platform}.",
                Remediation = "Use an alternative provider or upgrade to a supported platform",
                Details = new { exception = ex.Message, step = stepName, correlationId, os = Environment.OSVersion.ToString() }
            },
            
            _ => new JobStepError
            {
                Code = "UnknownError",
                Message = ex.Message,
                Remediation = "Check logs for details and contact support if the issue persists",
                Details = new { exception = ex.ToString(), step = stepName, correlationId }
            }
        };
    }
    
    /// <summary>
    /// Creates an error for a missing API key
    /// </summary>
    public static JobStepError MissingApiKey(string keyName, string? correlationId = null)
    {
        return new JobStepError
        {
            Code = $"MissingApiKey:{keyName}",
            Message = $"Required API key '{keyName}' is not configured.",
            Remediation = $"Add {keyName} in Settings → Providers",
            Details = new { keyName, correlationId }
        };
    }
    
    /// <summary>
    /// Creates an error for invalid input
    /// </summary>
    public static JobStepError InvalidInput(string fieldName, string reason, string? correlationId = null)
    {
        return new JobStepError
        {
            Code = $"InvalidInput:{fieldName}",
            Message = $"Invalid input for '{fieldName}': {reason}",
            Remediation = "Correct the input and try again",
            Details = new { fieldName, reason, correlationId }
        };
    }
    
    /// <summary>
    /// Creates an error for FFmpeg failure
    /// </summary>
    public static JobStepError FFmpegFailed(int exitCode, string? stderr = null, string? correlationId = null)
    {
        return new JobStepError
        {
            Code = $"FFmpegFailedExitCode:{exitCode}",
            Message = $"FFmpeg process failed with exit code {exitCode}.",
            Remediation = "Check FFmpeg installation and input files. View technical details for more information.",
            Details = new { exitCode, stderr = stderr?.Substring(0, Math.Min(stderr.Length, 1000)), correlationId }
        };
    }
    
    private static bool IsOutOfDiskSpace(IOException ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("disk") && (message.Contains("full") || message.Contains("space"));
    }
    
    private static string ExtractApiKeyCode(string message)
    {
        // Try to extract key name from message like "Missing API key: STABLE_KEY"
        var parts = message.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length >= 2)
        {
            var keyPart = parts[^1].Trim();
            if (!string.IsNullOrWhiteSpace(keyPart))
            {
                return $"MissingApiKey:{keyPart}";
            }
        }
        return "MissingApiKey:Unknown";
    }
}
