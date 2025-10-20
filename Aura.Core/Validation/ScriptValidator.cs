using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;

namespace Aura.Core.Validation;

/// <summary>
/// Validates script quality before proceeding with generation
/// </summary>
public class ScriptValidator
{
    /// <summary>
    /// Validates a generated script against quality criteria
    /// </summary>
    /// <param name="script">The script text to validate</param>
    /// <param name="planSpec">The plan specification for the script</param>
    /// <returns>Validation result with issues if any</returns>
    public ValidationResult Validate(string script, PlanSpec planSpec)
    {
        var issues = new List<string>();

        // Check script length - minimum 100 characters
        if (string.IsNullOrWhiteSpace(script) || script.Length < 100)
        {
            issues.Add($"Script too short ({script?.Length ?? 0} characters, minimum 100 characters required).");
        }

        // Check script has a title (starts with "# ")
        var trimmedScript = script?.Trim() ?? string.Empty;
        if (!trimmedScript.StartsWith("# "))
        {
            issues.Add("Script must have a title (starts with '# ').");
        }

        // Check script has at least 2 scenes (marked with "## ")
        var sceneParts = trimmedScript.Split("## ", StringSplitOptions.RemoveEmptyEntries);
        int sceneCount = sceneParts.Length - 1; // Subtract 1 for the title part
        if (sceneCount < 2)
        {
            issues.Add($"Script must have at least 2 scenes (found {sceneCount}). Use '## Scene Name' to mark scenes.");
        }

        // Check word count is approximately correct for target duration
        var words = trimmedScript.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        
        // Expected words: 150 words per minute = 2.5 words per second
        double expectedWords = planSpec.TargetDuration.TotalSeconds * 2.5;
        double difference = Math.Abs(wordCount - expectedWords) / expectedWords;
        
        // Tolerance of 50% deviation
        if (difference > 0.5)
        {
            issues.Add($"Word count significantly off target. Found {wordCount} words, expected approximately {(int)expectedWords} words for {(int)planSpec.TargetDuration.TotalSeconds} seconds.");
        }

        return new ValidationResult(issues.Count == 0, issues);
    }
}
