using System;
using System.Collections.Generic;

namespace Aura.Core.Errors;

/// <summary>
/// Base exception class for all Aura application exceptions.
/// Provides structured error information including error codes, context, and recovery suggestions.
/// </summary>
public abstract class AuraException : Exception
{
    /// <summary>
    /// Unique error code for categorizing and tracking errors
    /// </summary>
    public string ErrorCode { get; protected set; }

    /// <summary>
    /// Correlation ID for tracking this error across systems
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Contextual information about what was being attempted when the error occurred
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// Suggested actions the user can take to resolve or work around the error
    /// </summary>
    public string[] SuggestedActions { get; protected set; }

    /// <summary>
    /// User-friendly error message suitable for display in UI
    /// </summary>
    public string UserMessage { get; protected set; }

    /// <summary>
    /// Indicates if this error is transient and the operation can be retried
    /// </summary>
    public bool IsTransient { get; protected set; }

    protected AuraException(
        string message,
        string errorCode,
        string? userMessage = null,
        string? correlationId = null,
        string[]? suggestedActions = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        CorrelationId = correlationId;
        UserMessage = userMessage ?? message;
        SuggestedActions = suggestedActions ?? Array.Empty<string>();
        IsTransient = isTransient;
        Context = new Dictionary<string, object>();
    }

    /// <summary>
    /// Adds contextual information about the error
    /// </summary>
    public AuraException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple contextual information items
    /// </summary>
    public AuraException WithContext(Dictionary<string, object> context)
    {
        foreach (var kvp in context)
        {
            Context[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Creates a JSON-serializable representation of this exception
    /// </summary>
    public virtual Dictionary<string, object> ToErrorResponse()
    {
        var response = new Dictionary<string, object>
        {
            ["errorCode"] = ErrorCode,
            ["message"] = UserMessage,
            ["technicalDetails"] = Message,
            ["suggestedActions"] = SuggestedActions,
            ["isTransient"] = IsTransient
        };

        if (!string.IsNullOrEmpty(CorrelationId))
        {
            response["correlationId"] = CorrelationId;
        }

        if (Context.Count > 0)
        {
            response["context"] = Context;
        }

        // Add "Learn More" documentation link if available
        var documentation = ErrorDocumentation.GetDocumentation(ErrorCode);
        if (documentation != null)
        {
            response["learnMoreUrl"] = documentation.Url;
            response["errorTitle"] = documentation.Title;
        }
        else
        {
            response["learnMoreUrl"] = ErrorDocumentation.GetFallbackUrl();
        }

        return response;
    }
}
