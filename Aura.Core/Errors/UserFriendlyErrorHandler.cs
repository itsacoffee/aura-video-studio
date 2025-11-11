using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Errors;

/// <summary>
/// Converts technical errors into user-friendly messages with actionable guidance
/// </summary>
public class UserFriendlyErrorHandler
{
    private readonly ILogger<UserFriendlyErrorHandler> _logger;
    
    public UserFriendlyErrorHandler(ILogger<UserFriendlyErrorHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert an exception to a user-friendly error message
    /// </summary>
    public UserFriendlyError ConvertToUserFriendly(Exception exception, string? context = null)
    {
        _logger.LogDebug("Converting exception to user-friendly error: {Type}", exception.GetType().Name);

        return exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx, context),
            ProviderException providerEx => HandleProviderException(providerEx, context),
            PipelineException pipelineEx => HandlePipelineException(pipelineEx, context),
            UnauthorizedAccessException => HandleUnauthorizedAccess(context),
            OutOfMemoryException => HandleOutOfMemory(context),
            TimeoutException => HandleTimeout(context),
            OperationCanceledException => HandleCancellation(context),
            _ => HandleGenericException(exception, context)
        };
    }

    private UserFriendlyError HandleValidationException(ValidationException ex, string? context)
    {
        var message = new StringBuilder("The request could not be completed due to validation errors:");
        
        foreach (var issue in ex.Issues)
        {
            message.AppendLine($"\n• {issue}");
        }

        return new UserFriendlyError
        {
            Title = "Validation Error",
            Message = message.ToString(),
            Severity = ErrorSeverity.Warning,
            Suggestions = new List<string>
            {
                "Check that all required fields are filled in correctly",
                "Ensure values are within acceptable ranges",
                "Review the error details and correct any issues"
            },
            TechnicalDetails = ex.ToString()
        };
    }

    private UserFriendlyError HandleProviderException(ProviderException ex, string? context)
    {
        var suggestions = new List<string>();
        var message = $"The {ex.Type} provider ({ex.ProviderName}) encountered an error.";

        // Add provider-specific suggestions
        switch (ex.Type)
        {
            case ProviderType.Llm:
                suggestions.Add("Check that your API key is valid and has sufficient credits");
                suggestions.Add("Verify your internet connection");
                suggestions.Add("Try using a different LLM provider");
                if (ex.SpecificErrorCode?.Contains("rate_limit") == true)
                {
                    message += " Rate limit exceeded.";
                    suggestions.Clear();
                    suggestions.Add("Wait a few minutes before trying again");
                    suggestions.Add("Reduce the frequency of requests");
                }
                break;

            case ProviderType.Tts:
                suggestions.Add("Check that your TTS provider credentials are configured");
                suggestions.Add("Verify the selected voice is available");
                suggestions.Add("Try a different voice or TTS provider");
                break;

            case ProviderType.Image:
                suggestions.Add("Verify your image provider settings");
                suggestions.Add("Check that the required model is available");
                suggestions.Add("Try simplifying the image description");
                break;
        }

        return new UserFriendlyError
        {
            Title = $"{ex.Type} Provider Error",
            Message = message,
            Severity = ErrorSeverity.Error,
            Suggestions = suggestions,
            TechnicalDetails = $"Provider: {ex.ProviderName}\nError Code: {ex.SpecificErrorCode}\n{ex}",
            ProviderName = ex.ProviderName
        };
    }

    private UserFriendlyError HandlePipelineException(PipelineException ex, string? context)
    {
        var message = new StringBuilder($"Video generation failed during the {ex.StageName} stage.");

        if (ex.CompletedTasks > 0)
        {
            message.AppendLine($"\n\nProgress: {ex.CompletedTasks} of {ex.TotalTasks} tasks completed.");
        }

        if (ex.ProviderFailures.Count > 0)
        {
            message.AppendLine("\n\nProvider Errors:");
            foreach (var providerError in ex.ProviderFailures)
            {
                message.AppendLine($"• {providerError.ProviderName}: {providerError.Message}");
            }
        }

        var suggestions = new List<string>
        {
            "Try running the generation again",
            "Check your provider settings and API keys",
            "Simplify the video brief or reduce target duration"
        };

        if (ex.ElapsedBeforeFailure.HasValue && ex.ElapsedBeforeFailure.Value.TotalMinutes > 5)
        {
            suggestions.Add("The operation ran for a while - check system resources and available disk space");
        }

        return new UserFriendlyError
        {
            Title = "Video Generation Failed",
            Message = message.ToString(),
            Severity = ErrorSeverity.Error,
            Suggestions = suggestions,
            TechnicalDetails = ex.ToString(),
            CanRetry = true
        };
    }

    private UserFriendlyError HandleUnauthorizedAccess(string? context)
    {
        return new UserFriendlyError
        {
            Title = "Access Denied",
            Message = "The application does not have permission to access the required resource.",
            Severity = ErrorSeverity.Error,
            Suggestions = new List<string>
            {
                "Run the application as administrator (if required)",
                "Check file and folder permissions",
                "Ensure the output directory is writable"
            }
        };
    }

    private UserFriendlyError HandleOutOfMemory(string? context)
    {
        return new UserFriendlyError
        {
            Title = "Out of Memory",
            Message = "The system ran out of available memory during video processing.",
            Severity = ErrorSeverity.Critical,
            Suggestions = new List<string>
            {
                "Close other applications to free up memory",
                "Reduce video resolution or duration",
                "Process fewer scenes simultaneously",
                "Restart the application and try again"
            },
            CanRetry = true
        };
    }

    private UserFriendlyError HandleTimeout(string? context)
    {
        return new UserFriendlyError
        {
            Title = "Operation Timeout",
            Message = $"The operation took too long to complete{(context != null ? $" during {context}" : "")}.",
            Severity = ErrorSeverity.Warning,
            Suggestions = new List<string>
            {
                "Check your internet connection",
                "Try again with a shorter video duration",
                "The provider service may be experiencing delays"
            },
            CanRetry = true
        };
    }

    private UserFriendlyError HandleCancellation(string? context)
    {
        return new UserFriendlyError
        {
            Title = "Operation Cancelled",
            Message = "The operation was cancelled by the user or system.",
            Severity = ErrorSeverity.Info,
            Suggestions = new List<string>
            {
                "You can start a new generation at any time"
            }
        };
    }

    private UserFriendlyError HandleGenericException(Exception ex, string? context)
    {
        var message = $"An unexpected error occurred{(context != null ? $" during {context}" : "")}.";
        
        // Add specific hints based on exception message
        var suggestions = new List<string>
        {
            "Try the operation again",
            "Check the application logs for more details"
        };

        var exceptionMessage = ex.Message.ToLowerInvariant();

        if (exceptionMessage.Contains("network") || exceptionMessage.Contains("connection"))
        {
            suggestions.Insert(0, "Check your internet connection");
        }
        else if (exceptionMessage.Contains("disk") || exceptionMessage.Contains("space"))
        {
            suggestions.Insert(0, "Check available disk space");
        }
        else if (exceptionMessage.Contains("file") || exceptionMessage.Contains("path"))
        {
            suggestions.Insert(0, "Verify file paths and permissions");
        }

        return new UserFriendlyError
        {
            Title = "Unexpected Error",
            Message = message,
            Severity = ErrorSeverity.Error,
            Suggestions = suggestions,
            TechnicalDetails = ex.ToString(),
            CanRetry = true
        };
    }
}

/// <summary>
/// User-friendly error representation
/// </summary>
public class UserFriendlyError
{
    public string Title { get; set; } = "Error";
    public string Message { get; set; } = "";
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    public List<string> Suggestions { get; set; } = new();
    public string? TechnicalDetails { get; set; }
    public string? ProviderName { get; set; }
    public bool CanRetry { get; set; }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
