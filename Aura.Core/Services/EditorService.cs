using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for applying targeted edits to scripts based on critique
/// </summary>
public class EditorService : IEditorProvider
{
    private readonly ILogger<EditorService> _logger;
    private readonly ILlmProvider _llmProvider;
    private const int AverageWordsPerMinute = 150;

    public EditorService(
        ILogger<EditorService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <inheritdoc/>
    public async Task<EditResult> EditScriptAsync(
        string script,
        CritiqueResult critique,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Applying edits to script based on {SuggestionCount} suggestions", critique.Suggestions.Count);

            var editPrompt = BuildEditPrompt(script, critique, brief, spec);
            var editedScript = await _llmProvider.CompleteAsync(editPrompt, ct);

            if (string.IsNullOrWhiteSpace(editedScript))
            {
                _logger.LogWarning("Editor returned empty script, using original");
                return new EditResult
                {
                    EditedScript = script,
                    Success = false,
                    ErrorMessage = "Editor returned empty result"
                };
            }

            editedScript = CleanScriptOutput(editedScript);

            var validationResult = await ValidateSchemaAsync(editedScript, spec, ct);

            if (!validationResult.MeetsDurationConstraints)
            {
                _logger.LogWarning(
                    "Edited script duration ({Estimated}) exceeds target ({Target}), attempting to adjust",
                    validationResult.EstimatedDuration,
                    validationResult.TargetDuration);

                editedScript = await AdjustForDurationAsync(editedScript, spec, ct);
                validationResult = await ValidateSchemaAsync(editedScript, spec, ct);
            }

            return new EditResult
            {
                EditedScript = editedScript,
                AppliedEdits = ExtractAppliedEdits(critique),
                Success = true,
                ValidationResult = validationResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit script");
            return new EditResult
            {
                EditedScript = script,
                Success = false,
                ErrorMessage = $"Edit error: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> ApplyEditsAsync(
        string script,
        IReadOnlyList<ScriptEdit> edits,
        CancellationToken ct)
    {
        await Task.CompletedTask;

        var result = script;
        
        foreach (var edit in edits.OrderBy(e => e.Target))
        {
            try
            {
                result = edit.EditType.ToLowerInvariant() switch
                {
                    "replace" when edit.OriginalText != null && edit.NewText != null => 
                        result.Replace(edit.OriginalText, edit.NewText),
                    "insert" when edit.NewText != null => 
                        InsertText(result, edit.Target, edit.NewText),
                    "delete" when edit.OriginalText != null => 
                        result.Replace(edit.OriginalText, string.Empty),
                    _ => result
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply edit {EditType} at {Target}", edit.EditType, edit.Target);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<SchemaValidationResult> ValidateSchemaAsync(
        string script,
        PlanSpec spec,
        CancellationToken ct)
    {
        await Task.CompletedTask;

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(script))
        {
            errors.Add("Script is empty");
        }

        var wordCount = CountWords(script);
        var estimatedDuration = EstimateDuration(wordCount);
        var targetDuration = spec.TargetDuration;

        var durationVariance = Math.Abs((estimatedDuration - targetDuration).TotalSeconds / targetDuration.TotalSeconds);
        var meetsDurationConstraints = durationVariance <= 0.20;

        if (!meetsDurationConstraints)
        {
            warnings.Add($"Duration variance {durationVariance:P0} exceeds 20% threshold");
        }

        var hasSceneStructure = HasSceneStructure(script);
        if (!hasSceneStructure)
        {
            warnings.Add("Script lacks clear scene structure");
        }

        var isValid = errors.Count == 0;

        return new SchemaValidationResult
        {
            IsValid = isValid,
            Errors = errors,
            Warnings = warnings,
            MeetsDurationConstraints = meetsDurationConstraints,
            EstimatedDuration = estimatedDuration,
            TargetDuration = targetDuration
        };
    }

    private string BuildEditPrompt(
        string script,
        CritiqueResult critique,
        Brief brief,
        PlanSpec spec)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# SCRIPT EDITING TASK");
        sb.AppendLine();
        sb.AppendLine("You are an expert script editor. Apply the critique feedback to improve the following script.");
        sb.AppendLine();
        sb.AppendLine("## ORIGINAL SCRIPT");
        sb.AppendLine("```text");
        var sanitizedScript = script.Replace("```", "'''");
        sb.AppendLine(sanitizedScript);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## CRITIQUE FEEDBACK");
        sb.AppendLine();
        sb.AppendLine($"**Overall Score**: {critique.OverallScore:F1}/100");
        sb.AppendLine();

        if (critique.Issues.Any())
        {
            sb.AppendLine("### Issues to Address");
            foreach (var issue in critique.Issues.OrderByDescending(i => i.Severity))
            {
                sb.AppendLine($"- **{issue.Category}** ({issue.Severity}): {issue.Description}");
                if (!string.IsNullOrEmpty(issue.Location))
                {
                    sb.AppendLine($"  Location: {issue.Location}");
                }
            }
            sb.AppendLine();
        }

        if (critique.Strengths.Any())
        {
            sb.AppendLine("### Strengths to Preserve");
            foreach (var strength in critique.Strengths)
            {
                sb.AppendLine($"- {strength}");
            }
            sb.AppendLine();
        }

        if (critique.Suggestions.Any())
        {
            sb.AppendLine("### Specific Suggestions");
            foreach (var suggestion in critique.Suggestions)
            {
                sb.AppendLine($"- **{suggestion.ChangeType}** at {suggestion.Target}:");
                sb.AppendLine($"  {suggestion.Suggestion}");
                if (!string.IsNullOrEmpty(suggestion.ExpectedImpact))
                {
                    sb.AppendLine($"  Expected impact: {suggestion.ExpectedImpact}");
                }
            }
            sb.AppendLine();
        }

        if (critique.TimingAnalysis != null && !critique.TimingAnalysis.WithinAcceptableRange)
        {
            sb.AppendLine("### Timing Adjustment Required");
            sb.AppendLine($"- Current: {critique.TimingAnalysis.WordCount} words");
            sb.AppendLine($"- Target: {critique.TimingAnalysis.TargetWordCount} words");
            sb.AppendLine($"- {critique.TimingAnalysis.Recommendation}");
            sb.AppendLine();
        }

        sb.AppendLine("## EDITING INSTRUCTIONS");
        sb.AppendLine();
        sb.AppendLine("1. Address all high-severity issues");
        sb.AppendLine("2. Implement suggested improvements");
        sb.AppendLine("3. Preserve identified strengths");
        sb.AppendLine("4. Maintain the script's core message and tone");
        sb.AppendLine("5. Ensure natural flow and readability");
        sb.AppendLine($"6. Target duration: {spec.TargetDuration.TotalMinutes:F1} minutes ({EstimateTargetWordCount(spec.TargetDuration)} words)");
        sb.AppendLine();
        sb.AppendLine("## OUTPUT");
        sb.AppendLine();
        sb.AppendLine("Provide the complete revised script. Do not include explanations or metadata, only the script itself.");

        return sb.ToString();
    }

    private string CleanScriptOutput(string script)
    {
        script = Regex.Replace(script, @"^```.*?\n", string.Empty, RegexOptions.Multiline);
        script = Regex.Replace(script, @"\n```\s*$", string.Empty, RegexOptions.Multiline);
        
        script = script.Trim();

        return script;
    }

    private async Task<string> AdjustForDurationAsync(string script, PlanSpec spec, CancellationToken ct)
    {
        var wordCount = CountWords(script);
        var targetWordCount = EstimateTargetWordCount(spec.TargetDuration);

        if (wordCount > targetWordCount)
        {
            var sanitizedScript = script.Replace("```", "'''");
            var adjustmentPrompt = $@"Condense the following script from {wordCount} to approximately {targetWordCount} words while preserving key points:

```text
{sanitizedScript}
```

Provide only the condensed script, no explanations.";

            var adjusted = await _llmProvider.CompleteAsync(adjustmentPrompt, ct);
            return CleanScriptOutput(adjusted);
        }

        return script;
    }

    private List<ScriptEdit> ExtractAppliedEdits(CritiqueResult critique)
    {
        return critique.Suggestions.Select(s => new ScriptEdit
        {
            EditType = s.ChangeType,
            Target = s.Target,
            NewText = s.Suggestion,
            Reason = s.ExpectedImpact
        }).ToList();
    }

    private string InsertText(string script, string target, string newText)
    {
        var targetIndex = script.IndexOf(target, StringComparison.OrdinalIgnoreCase);
        if (targetIndex >= 0)
        {
            return script.Insert(targetIndex + target.Length, "\n" + newText);
        }
        
        return script + "\n" + newText;
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var words = Regex.Split(text, @"\s+");
        return words.Count(w => !string.IsNullOrWhiteSpace(w));
    }

    private TimeSpan EstimateDuration(int wordCount)
    {
        var minutes = (double)wordCount / AverageWordsPerMinute;
        return TimeSpan.FromMinutes(minutes);
    }

    private int EstimateTargetWordCount(TimeSpan duration)
    {
        return (int)(duration.TotalMinutes * AverageWordsPerMinute);
    }

    private bool HasSceneStructure(string script)
    {
        var sceneMarkers = new[] { "Scene", "SCENE", "---", "###", "**" };
        return sceneMarkers.Any(marker => script.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
