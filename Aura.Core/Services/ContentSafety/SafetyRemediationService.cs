using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Service for providing safety remediation strategies and user-friendly explanations
/// Helps users understand safety blocks and offers actionable alternatives
/// </summary>
public class SafetyRemediationService
{
    private readonly ILogger<SafetyRemediationService> _logger;
    private readonly ContentSafetyService _safetyService;

    public SafetyRemediationService(
        ILogger<SafetyRemediationService> logger,
        ContentSafetyService safetyService)
    {
        _logger = logger;
        _safetyService = safetyService;
    }

    /// <summary>
    /// Generates a comprehensive remediation report for safety violations
    /// </summary>
    public async Task<SafetyRemediationReport> GenerateRemediationReportAsync(
        string contentId,
        string content,
        SafetyAnalysisResult analysisResult,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating remediation report for content {ContentId}", contentId);

        try
        {
            var report = new SafetyRemediationReport
            {
                ContentId = contentId,
                AnalysisResult = analysisResult,
                Summary = GenerateSummary(analysisResult, policy),
                DetailedExplanation = await GenerateDetailedExplanationAsync(analysisResult, policy, ct),
                RemediationStrategies = GenerateRemediationStrategies(content, analysisResult),
                Alternatives = await GenerateAlternativesAsync(content, analysisResult, ct),
                UserOptions = GenerateUserOptions(analysisResult, policy),
                RecommendedAction = DetermineRecommendedAction(analysisResult, policy)
            };

            _logger.LogInformation(
                "Remediation report generated with {StrategyCount} strategies and {AlternativeCount} alternatives",
                report.RemediationStrategies.Count,
                report.Alternatives.Count);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating remediation report");
            throw;
        }
    }

    /// <summary>
    /// Explains why content was blocked in user-friendly language
    /// </summary>
    public string ExplainSafetyBlock(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        var blockingViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.Block)
            .ToList();

        if (blockingViolations.Count == 0)
        {
            return "Content passed all safety checks.";
        }

        var explanation = "🛡️ **Safety Check Failed**\n\n";
        explanation += $"Your content was flagged by the **{policy.Name}** safety policy.\n\n";
        explanation += "**Issues Found:**\n\n";

        foreach (var violation in blockingViolations.Take(5))
        {
            explanation += $"• **{violation.Category}**: {violation.Reason}\n";

            if (!string.IsNullOrEmpty(violation.MatchedContent))
            {
                explanation += $"  _(Found: \"{violation.MatchedContent}\")_\n";
            }

            if (!string.IsNullOrEmpty(violation.SuggestedFix))
            {
                explanation += $"  _💡 Suggestion: {violation.SuggestedFix}_\n";
            }

            explanation += "\n";
        }

        if (blockingViolations.Count > 5)
        {
            explanation += $"_...and {blockingViolations.Count - 5} more issues._\n\n";
        }

        if (policy.AllowUserOverride)
        {
            explanation += "**Your Options:**\n";
            explanation += "1. Modify your content to address the issues\n";
            explanation += "2. Use one of the suggested alternatives\n";
            explanation += "3. Override this decision (Advanced Mode only)\n";
        }
        else
        {
            explanation += "**Next Steps:**\n";
            explanation += "This policy does not allow overrides. Please modify your content to comply with safety guidelines.\n";
        }

        return explanation;
    }

    /// <summary>
    /// Suggests modifications to make content compliant
    /// </summary>
    public async Task<List<ContentModification>> SuggestModificationsAsync(
        string content,
        SafetyAnalysisResult analysisResult,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating content modifications");

        var modifications = new List<ContentModification>();

        foreach (var violation in analysisResult.Violations.OrderByDescending(v => v.SeverityScore))
        {
            var modification = new ContentModification
            {
                ViolationId = violation.Id,
                Category = violation.Category,
                Description = $"Replace '{violation.MatchedContent}' to address {violation.Category}",
                OriginalText = violation.MatchedContent ?? string.Empty,
                ModifiedText = violation.SuggestedFix ?? GetDefaultReplacement(violation.Category),
                Reason = violation.Reason,
                Impact = CalculateImpact(violation)
            };

            modifications.Add(modification);
        }

        await Task.CompletedTask;
        return modifications.Take(10).ToList();
    }

    private string GenerateSummary(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        if (analysisResult.IsSafe)
        {
            return "✅ Content passed all safety checks.";
        }

        var blockCount = analysisResult.Violations.Count(v => v.RecommendedAction == SafetyAction.Block);
        var warnCount = analysisResult.Violations.Count(v => v.RecommendedAction == SafetyAction.Warn);

        var summary = $"⚠️ Safety issues detected: {blockCount} blocking, {warnCount} warnings. ";

        if (policy.AllowUserOverride)
        {
            summary += "Override available in Advanced Mode.";
        }
        else
        {
            summary += "Modification required.";
        }

        return summary;
    }

    private async Task<string> GenerateDetailedExplanationAsync(
        SafetyAnalysisResult analysisResult,
        SafetyPolicy policy,
        CancellationToken ct)
    {
        var explanation = "## Safety Analysis Results\n\n";

        explanation += $"**Overall Safety Score:** {analysisResult.OverallSafetyScore}/100\n";
        explanation += $"**Policy Applied:** {policy.Name}\n";
        explanation += $"**Analysis Time:** {analysisResult.AnalyzedAt:yyyy-MM-dd HH:mm:ss UTC}\n\n";

        if (analysisResult.Violations.Count > 0)
        {
            explanation += "### Violations\n\n";

            foreach (var violation in analysisResult.Violations.Take(10))
            {
                explanation += $"**{violation.Category}** (Severity: {violation.SeverityScore}/10)\n";
                explanation += $"- **Issue:** {violation.Reason}\n";
                explanation += $"- **Action:** {violation.RecommendedAction}\n";

                if (!string.IsNullOrEmpty(violation.SuggestedFix))
                {
                    explanation += $"- **Fix:** {violation.SuggestedFix}\n";
                }

                explanation += "\n";
            }
        }

        if (analysisResult.Warnings.Count > 0)
        {
            explanation += "### Warnings\n\n";

            foreach (var warning in analysisResult.Warnings.Take(5))
            {
                explanation += $"- **{warning.Category}:** {warning.Message}\n";
            }

            explanation += "\n";
        }

        if (analysisResult.CategoryScores.Count > 0)
        {
            explanation += "### Category Scores\n\n";

            foreach (var (category, score) in analysisResult.CategoryScores.OrderByDescending(kvp => kvp.Value))
            {
                var emoji = score > 7 ? "🔴" : score > 4 ? "🟡" : "🟢";
                explanation += $"{emoji} **{category}:** {score}/10\n";
            }
        }

        await Task.CompletedTask;
        return explanation;
    }

    private List<RemediationStrategy> GenerateRemediationStrategies(
        string content,
        SafetyAnalysisResult analysisResult)
    {
        var strategies = new List<RemediationStrategy>();

        var autoFixViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.AutoFix)
            .ToList();

        if (autoFixViolations.Count > 0)
        {
            strategies.Add(new RemediationStrategy
            {
                Name = "Automatic Fixes",
                Description = $"Apply {autoFixViolations.Count} automatic fixes to make content compliant",
                Difficulty = "Easy",
                SuccessLikelihood = 85,
                Steps = new List<string>
                {
                    "Review suggested fixes",
                    "Click 'Apply Automatic Fixes'",
                    "Verify the modified content"
                }
            });
        }

        var blockViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.Block)
            .ToList();

        if (blockViolations.Count > 0)
        {
            strategies.Add(new RemediationStrategy
            {
                Name = "Manual Revision",
                Description = "Manually revise content to address blocking violations",
                Difficulty = "Medium",
                SuccessLikelihood = 90,
                Steps = new List<string>
                {
                    "Review each violation and its context",
                    "Rewrite or remove problematic sections",
                    "Use suggested alternatives",
                    "Re-check content after modifications"
                }
            });
        }

        if (analysisResult.SuggestedFixes.Count > 0)
        {
            strategies.Add(new RemediationStrategy
            {
                Name = "Use Suggested Alternatives",
                Description = "Replace content with pre-generated safe alternatives",
                Difficulty = "Easy",
                SuccessLikelihood = 95,
                Steps = new List<string>
                {
                    "Review alternative versions",
                    "Select the most appropriate alternative",
                    "Copy and use the alternative content"
                }
            });
        }

        strategies.Add(new RemediationStrategy
        {
            Name = "Start Fresh",
            Description = "Create new content from scratch with safety guidelines in mind",
            Difficulty = "Medium",
            SuccessLikelihood = 100,
            Steps = new List<string>
            {
                "Review safety policy guidelines",
                "Create new content avoiding flagged categories",
                "Use family-friendly language and themes",
                "Test with safety checker before submission"
            }
        });

        return strategies;
    }

    private async Task<List<string>> GenerateAlternativesAsync(
        string content,
        SafetyAnalysisResult analysisResult,
        CancellationToken ct)
    {
        var alternatives = new List<string>();

        var mainViolations = analysisResult.Violations
            .OrderByDescending(v => v.SeverityScore)
            .Take(3)
            .ToList();

        foreach (var violation in mainViolations)
        {
            var alternative = GenerateAlternativeContent(content, violation);
            if (!string.IsNullOrEmpty(alternative) && alternative != content)
            {
                alternatives.Add(alternative);
            }
        }

        if (alternatives.Count == 0)
        {
            alternatives.Add(GenerateSafeGenericAlternative(content));
        }

        await Task.CompletedTask;
        return alternatives.Distinct().Take(5).ToList();
    }

    private List<UserOption> GenerateUserOptions(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        var options = new List<UserOption>();

        var hasAutoFixes = analysisResult.Violations.Any(v => v.RecommendedAction == SafetyAction.AutoFix);
        if (hasAutoFixes)
        {
            options.Add(new UserOption
            {
                Id = "apply-auto-fixes",
                Label = "Apply Automatic Fixes",
                Description = "Automatically apply suggested fixes to make content compliant",
                IsRecommended = true,
                RequiresAdvancedMode = false
            });
        }

        options.Add(new UserOption
        {
            Id = "use-alternative",
            Label = "Use Alternative Version",
            Description = "Select from suggested alternative content versions",
            IsRecommended = true,
            RequiresAdvancedMode = false
        });

        options.Add(new UserOption
        {
            Id = "manual-edit",
            Label = "Manually Edit Content",
            Description = "Edit the content yourself to address the issues",
            IsRecommended = false,
            RequiresAdvancedMode = false
        });

        if (policy.AllowUserOverride)
        {
            options.Add(new UserOption
            {
                Id = "override",
                Label = "Override Safety Check",
                Description = "Proceed with the original content despite safety warnings",
                IsRecommended = false,
                RequiresAdvancedMode = true
            });
        }

        options.Add(new UserOption
        {
            Id = "cancel",
            Label = "Cancel Operation",
            Description = "Cancel this operation and return to previous step",
            IsRecommended = false,
            RequiresAdvancedMode = false
        });

        return options;
    }

    private string DetermineRecommendedAction(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        if (analysisResult.IsSafe)
        {
            return "proceed";
        }

        var hasAutoFixes = analysisResult.Violations.Any(v => v.RecommendedAction == SafetyAction.AutoFix);
        if (hasAutoFixes)
        {
            return "apply-auto-fixes";
        }

        if (analysisResult.SuggestedFixes.Count > 0)
        {
            return "use-alternative";
        }

        return "manual-edit";
    }

    private string GenerateAlternativeContent(string content, SafetyViolation violation)
    {
        var alternative = content;

        if (!string.IsNullOrEmpty(violation.MatchedContent) && !string.IsNullOrEmpty(violation.SuggestedFix))
        {
            alternative = alternative.Replace(violation.MatchedContent, violation.SuggestedFix);
        }
        else if (!string.IsNullOrEmpty(violation.MatchedContent))
        {
            var replacement = GetDefaultReplacement(violation.Category);
            alternative = alternative.Replace(violation.MatchedContent, replacement);
        }

        return alternative;
    }

    private string GenerateSafeGenericAlternative(string content)
    {
        return "Create family-friendly content appropriate for all audiences, focusing on positive themes and educational value.";
    }

    private string GetDefaultReplacement(SafetyCategoryType category)
    {
        return category switch
        {
            SafetyCategoryType.Profanity => "[appropriate language]",
            SafetyCategoryType.Violence => "peaceful conflict resolution",
            SafetyCategoryType.SexualContent => "age-appropriate content",
            SafetyCategoryType.HateSpeech => "respectful discussion",
            SafetyCategoryType.DrugAlcohol => "healthy lifestyle",
            SafetyCategoryType.ControversialTopics => "balanced perspective",
            SafetyCategoryType.SelfHarm => "mental health support",
            SafetyCategoryType.GraphicImagery => "appropriate visuals",
            _ => "[safe content]"
        };
    }

    private const int HighSeverityThreshold = 8;
    private const int MediumSeverityThreshold = 5;

    private string CalculateImpact(SafetyViolation violation)
    {
        return violation.SeverityScore switch
        {
            >= HighSeverityThreshold => "High",
            >= MediumSeverityThreshold => "Medium",
            _ => "Low"
        };
    }
}

/// <summary>
/// Comprehensive remediation report
/// </summary>
public class SafetyRemediationReport
{
    public string ContentId { get; set; } = string.Empty;
    public SafetyAnalysisResult? AnalysisResult { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string DetailedExplanation { get; set; } = string.Empty;
    public List<RemediationStrategy> RemediationStrategies { get; set; } = new();
    public List<string> Alternatives { get; set; } = new();
    public List<UserOption> UserOptions { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// A strategy for remediating safety violations
/// </summary>
public class RemediationStrategy
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int SuccessLikelihood { get; set; }
    public List<string> Steps { get; set; } = new();
}

/// <summary>
/// A modification suggestion for content
/// </summary>
public class ContentModification
{
    public string ViolationId { get; set; } = string.Empty;
    public SafetyCategoryType Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string ModifiedText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}

/// <summary>
/// User option for handling safety violations
/// </summary>
public class UserOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public bool RequiresAdvancedMode { get; set; }
}
