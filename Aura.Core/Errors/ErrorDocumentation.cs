using System.Collections.Generic;

namespace Aura.Core.Errors;

/// <summary>
/// Provides documentation URLs and user-friendly help for error codes
/// </summary>
public static class ErrorDocumentation
{
    private static readonly Dictionary<string, ErrorDocumentationInfo> _errorDocs = new()
    {
        // Provider Errors (E100-E200)
        ["E100"] = new(
            "LLM Provider Error",
            "There was an issue with the language model provider (OpenAI, Anthropic, etc.)",
            "https://docs.aura.video/troubleshooting/provider-errors#llm-errors"
        ),
        ["E100-401"] = new(
            "LLM Authentication Failed",
            "The API key for your language model provider is invalid or expired",
            "https://docs.aura.video/troubleshooting/provider-errors#authentication"
        ),
        ["E100-429"] = new(
            "LLM Rate Limit Exceeded",
            "You've exceeded the rate limit for your language model provider",
            "https://docs.aura.video/troubleshooting/provider-errors#rate-limits"
        ),
        ["E200"] = new(
            "TTS Provider Error",
            "There was an issue with the text-to-speech provider",
            "https://docs.aura.video/troubleshooting/provider-errors#tts-errors"
        ),
        ["E200-401"] = new(
            "TTS Authentication Failed",
            "The API key for your text-to-speech provider is invalid or expired",
            "https://docs.aura.video/troubleshooting/provider-errors#authentication"
        ),
        ["E400"] = new(
            "Visual Provider Error",
            "There was an issue with the image generation provider",
            "https://docs.aura.video/troubleshooting/provider-errors#visual-errors"
        ),
        ["E400-401"] = new(
            "Visual Provider Authentication Failed",
            "The API key for your image generation provider is invalid or expired",
            "https://docs.aura.video/troubleshooting/provider-errors#authentication"
        ),
        ["E500"] = new(
            "Rendering Error",
            "There was an issue rendering your video",
            "https://docs.aura.video/troubleshooting/rendering-errors"
        ),
        
        // Validation Errors (E001-E003)
        ["E001"] = new(
            "Validation Error",
            "The input provided does not meet the required criteria",
            "https://docs.aura.video/troubleshooting/validation-errors"
        ),
        ["E002"] = new(
            "Invalid Input",
            "One or more input parameters are invalid or missing",
            "https://docs.aura.video/troubleshooting/validation-errors#invalid-input"
        ),
        ["E003"] = new(
            "Access Denied",
            "You don't have permission to perform this operation",
            "https://docs.aura.video/troubleshooting/access-errors"
        ),
        
        // FFmpeg Errors (FFmpeg-specific)
        ["FFmpegNotFound"] = new(
            "FFmpeg Not Found",
            "FFmpeg is required for video rendering but was not found on your system",
            "https://docs.aura.video/setup/dependencies#ffmpeg"
        ),
        ["FFmpegCorrupted"] = new(
            "FFmpeg Corrupted",
            "The FFmpeg installation appears to be corrupted or incomplete",
            "https://docs.aura.video/troubleshooting/ffmpeg-errors#corrupted"
        ),
        ["FFmpegFailed"] = new(
            "FFmpeg Processing Failed",
            "FFmpeg encountered an error while processing your video",
            "https://docs.aura.video/troubleshooting/ffmpeg-errors#processing"
        ),
        
        // Resource Errors
        ["OutOfDiskSpace"] = new(
            "Insufficient Disk Space",
            "There is not enough disk space to complete this operation",
            "https://docs.aura.video/troubleshooting/resource-errors#disk-space"
        ),
        ["OutputDirectoryNotWritable"] = new(
            "Output Directory Not Writable",
            "The specified output directory cannot be written to",
            "https://docs.aura.video/troubleshooting/resource-errors#permissions"
        ),
        
        // API Key Errors
        ["MissingApiKey"] = new(
            "Missing API Key",
            "A required API key has not been configured",
            "https://docs.aura.video/setup/api-keys"
        ),
        ["MissingApiKey:OPENAI_KEY"] = new(
            "Missing OpenAI API Key",
            "OpenAI API key is required for this operation",
            "https://docs.aura.video/setup/api-keys#openai"
        ),
        ["MissingApiKey:ANTHROPIC_KEY"] = new(
            "Missing Anthropic API Key",
            "Anthropic API key is required for this operation",
            "https://docs.aura.video/setup/api-keys#anthropic"
        ),
        ["MissingApiKey:ELEVENLABS_KEY"] = new(
            "Missing ElevenLabs API Key",
            "ElevenLabs API key is required for text-to-speech",
            "https://docs.aura.video/setup/api-keys#elevenlabs"
        ),
        ["MissingApiKey:STABILITY_KEY"] = new(
            "Missing Stability AI API Key",
            "Stability AI API key is required for image generation",
            "https://docs.aura.video/setup/api-keys#stability"
        ),
        
        // Network Errors
        ["TransientNetworkFailure"] = new(
            "Network Connection Error",
            "Unable to connect to the remote service",
            "https://docs.aura.video/troubleshooting/network-errors"
        ),
        
        // GPU/Hardware Errors
        ["RequiresNvidiaGPU"] = new(
            "NVIDIA GPU Required",
            "This operation requires an NVIDIA GPU with CUDA support",
            "https://docs.aura.video/system-requirements#gpu"
        ),
        
        // Platform Errors
        ["UnsupportedOS"] = new(
            "Unsupported Operating System",
            "This feature is not supported on your operating system",
            "https://docs.aura.video/system-requirements#os"
        ),
        
        // Operation Errors
        ["E998"] = new(
            "Operation Cancelled",
            "The operation was cancelled by the user or system",
            "https://docs.aura.video/troubleshooting/general-errors#cancellation"
        ),
        ["E997"] = new(
            "Not Implemented",
            "This feature is not yet implemented",
            "https://docs.aura.video/roadmap"
        ),
        ["E999"] = new(
            "Unexpected Error",
            "An unexpected error occurred",
            "https://docs.aura.video/troubleshooting/general-errors"
        ),
        
        // Resilience/Circuit Breaker Errors
        ["CIRCUIT_OPEN"] = new(
            "Service Temporarily Unavailable",
            "The service is temporarily unavailable due to repeated failures. It will automatically recover.",
            "https://docs.aura.video/troubleshooting/resilience#circuit-breaker"
        ),
        ["RETRY_EXHAUSTED"] = new(
            "Retry Attempts Exhausted",
            "All retry attempts have been exhausted for this operation",
            "https://docs.aura.video/troubleshooting/resilience#retries"
        ),
    };

    /// <summary>
    /// Gets documentation information for a specific error code
    /// </summary>
    public static ErrorDocumentationInfo? GetDocumentation(string errorCode)
    {
        if (_errorDocs.TryGetValue(errorCode, out var doc))
        {
            return doc;
        }

        // Try to find a base error code (e.g., "E100" from "E100-429")
        var baseCode = errorCode.Split('-')[0];
        if (_errorDocs.TryGetValue(baseCode, out var baseDoc))
        {
            return baseDoc;
        }

        // Try to find a category code (e.g., "MissingApiKey" from "MissingApiKey:OPENAI_KEY")
        var categoryCode = errorCode.Split(':')[0];
        if (_errorDocs.TryGetValue(categoryCode, out var categoryDoc))
        {
            return categoryDoc;
        }

        return null;
    }

    /// <summary>
    /// Checks if documentation exists for an error code
    /// </summary>
    public static bool HasDocumentation(string errorCode)
    {
        return GetDocumentation(errorCode) != null;
    }

    /// <summary>
    /// Gets a fallback documentation URL for unknown errors
    /// </summary>
    public static string GetFallbackUrl()
    {
        return "https://docs.aura.video/troubleshooting";
    }
}

/// <summary>
/// Information about error documentation
/// </summary>
public record ErrorDocumentationInfo(
    string Title,
    string Description,
    string Url
);
