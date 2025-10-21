using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Errors;

namespace Aura.Core.Validation;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : AuraException
{
    public List<string> Issues { get; }

    public ValidationException(string message, List<string> issues, string? correlationId = null)
        : base(
            message,
            "E001",
            GenerateUserMessage(issues),
            correlationId,
            GenerateSuggestedActions(issues),
            isTransient: false)
    {
        Issues = issues;
        WithContext("issueCount", issues.Count);
        WithContext("issues", issues);
    }

    private static string GenerateUserMessage(List<string> issues)
    {
        if (issues.Count == 1)
        {
            return $"Validation failed: {issues[0]}";
        }
        return $"Validation failed with {issues.Count} issues. Please review and correct the input.";
    }

    private static string[] GenerateSuggestedActions(List<string> issues)
    {
        var actions = new List<string> { "Review the validation errors below" };
        
        // Add specific suggestions based on common validation issues
        var issuesText = string.Join(" ", issues).ToLowerInvariant();
        
        if (issuesText.Contains("api key") || issuesText.Contains("provider"))
        {
            actions.Add("Configure required API keys in Settings → Providers");
        }
        
        if (issuesText.Contains("ffmpeg"))
        {
            actions.Add("Install or configure FFmpeg in Settings → Dependencies");
        }
        
        if (issuesText.Contains("disk") || issuesText.Contains("space"))
        {
            actions.Add("Free up disk space before proceeding");
        }
        
        actions.Add("Correct the issues and try again");
        
        return actions.ToArray();
    }

    public override Dictionary<string, object> ToErrorResponse()
    {
        var response = base.ToErrorResponse();
        response["validationIssues"] = Issues;
        return response;
    }
}
