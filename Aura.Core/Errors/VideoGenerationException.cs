using System;

namespace Aura.Core.Errors;

/// <summary>
/// Exception thrown when video generation fails at a specific stage.
/// Provides stage information, retryability indication, and user-friendly suggested actions.
/// </summary>
public class VideoGenerationException : Exception
{
    /// <summary>
    /// The pipeline stage where the error occurred
    /// </summary>
    public string Stage { get; }

    /// <summary>
    /// Whether this error is likely recoverable with retry
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Suggested action for the user
    /// </summary>
    public string? SuggestedAction { get; }

    public VideoGenerationException(string message, string stage, Exception? innerException = null)
        : base(message, innerException)
    {
        Stage = stage;
        IsRetryable = DetermineIfRetryable(innerException);
        SuggestedAction = GetSuggestedAction(stage, innerException);
    }

    private static bool DetermineIfRetryable(Exception? inner)
    {
        if (inner == null) return false;

        var typeName = inner.GetType().Name;

        if (typeName == "HttpRequestException") return true;
        if (typeName == "TimeoutException") return true;
        if (typeName == "IOException") return true;
        if (typeName == "OperationCanceledException") return false;

        return false;
    }

    private static string? GetSuggestedAction(string stage, Exception? inner)
    {
        if (inner != null && inner.GetType().Name == "HttpRequestException")
        {
            return stage switch
            {
                "Script" => "Ensure Ollama is running: ollama serve",
                "TTS" => "Check that the TTS service is available",
                "Image" or "Images" => "Verify image provider is configured correctly",
                "Render" => "Ensure FFmpeg is installed and accessible",
                _ => "Check that all required services are running"
            };
        }

        if (inner != null && inner.GetType().Name == "TimeoutException")
        {
            return "The operation took too long. Try again or use a faster model.";
        }

        return null;
    }
}
