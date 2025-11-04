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
/// Service for generating structured critique of scripts using rubric-based evaluation
/// </summary>
public class CriticService : ICriticProvider
{
    private readonly ILogger<CriticService> _logger;
    private readonly ILlmProvider _llmProvider;
    private const int AverageWordsPerMinute = 150;

    public CriticService(
        ILogger<CriticService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <inheritdoc/>
    public async Task<CritiqueResult> CritiqueScriptAsync(
        string script,
        Brief brief,
        PlanSpec spec,
        IReadOnlyList<RefinementRubric> rubrics,
        ScriptQualityMetrics? currentMetrics,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating structured critique for script (length: {Length} chars)", script.Length);

            var critiquePrompt = BuildStructuredCritiquePrompt(script, brief, spec, rubrics, currentMetrics);
            var rawCritique = await _llmProvider.CompleteAsync(critiquePrompt, ct);

            var result = ParseCritiqueResponse(rawCritique, rubrics);
            
            var timingAnalysis = await AnalyzeTimingFitAsync(script, spec.TargetDuration, ct);
            
            return result with { TimingAnalysis = timingAnalysis, RawCritique = rawCritique };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate critique");
            return CreateFallbackCritique();
        }
    }

    /// <inheritdoc/>
    public async Task<TimingAnalysis> AnalyzeTimingFitAsync(
        string script,
        TimeSpan targetDuration,
        CancellationToken ct)
    {
        await Task.CompletedTask;

        var wordCount = CountWords(script);
        var targetMinutes = targetDuration.TotalMinutes;
        var targetWordCount = (int)(targetMinutes * AverageWordsPerMinute);
        
        var variance = ((double)(wordCount - targetWordCount) / targetWordCount) * 100;
        var withinRange = Math.Abs(variance) <= 15.0;

        string? recommendation = null;
        if (variance > 15)
        {
            recommendation = $"Script is too long. Consider condensing by approximately {wordCount - targetWordCount} words.";
        }
        else if (variance < -15)
        {
            recommendation = $"Script is too short. Consider expanding by approximately {targetWordCount - wordCount} words.";
        }

        return new TimingAnalysis
        {
            WordCount = wordCount,
            TargetWordCount = targetWordCount,
            Variance = variance,
            WithinAcceptableRange = withinRange,
            Recommendation = recommendation
        };
    }

    private string BuildStructuredCritiquePrompt(
        string script,
        Brief brief,
        PlanSpec spec,
        IReadOnlyList<RefinementRubric> rubrics,
        ScriptQualityMetrics? currentMetrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# SCRIPT CRITIQUE TASK");
        sb.AppendLine();
        sb.AppendLine("You are an expert script critic. Evaluate the following video script using the provided rubrics.");
        sb.AppendLine();
        sb.AppendLine("## SCRIPT TO EVALUATE");
        sb.AppendLine("```");
        sb.AppendLine(script);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## CONTEXT");
        sb.AppendLine($"- **Topic**: {brief.Topic}");
        sb.AppendLine($"- **Audience**: {brief.Audience ?? "general"}");
        sb.AppendLine($"- **Goal**: {brief.Goal ?? "inform and engage"}");
        sb.AppendLine($"- **Tone**: {brief.Tone}");
        sb.AppendLine($"- **Target Duration**: {spec.TargetDuration.TotalMinutes:F1} minutes");
        sb.AppendLine($"- **Pacing**: {spec.Pacing}");
        sb.AppendLine();

        if (currentMetrics != null)
        {
            sb.AppendLine("## CURRENT QUALITY SCORES");
            sb.AppendLine($"- Overall: {currentMetrics.OverallScore:F1}/100");
            sb.AppendLine($"- Narrative Coherence: {currentMetrics.NarrativeCoherence:F1}/100");
            sb.AppendLine($"- Pacing: {currentMetrics.PacingAppropriateness:F1}/100");
            sb.AppendLine($"- Audience Alignment: {currentMetrics.AudienceAlignment:F1}/100");
            sb.AppendLine($"- Visual Clarity: {currentMetrics.VisualClarity:F1}/100");
            sb.AppendLine($"- Engagement: {currentMetrics.EngagementPotential:F1}/100");
            sb.AppendLine();
        }

        sb.AppendLine("## EVALUATION RUBRICS");
        sb.AppendLine();
        foreach (var rubric in rubrics)
        {
            sb.AppendLine($"### {rubric.Name} (Weight: {rubric.Weight}, Target: {rubric.TargetThreshold}/100)");
            sb.AppendLine(rubric.Description);
            sb.AppendLine();
            foreach (var criterion in rubric.Criteria)
            {
                sb.AppendLine($"**{criterion.Name}**: {criterion.Description}");
                sb.AppendLine(criterion.ScoringGuideline);
                sb.AppendLine();
            }
        }

        sb.AppendLine("## OUTPUT FORMAT");
        sb.AppendLine();
        sb.AppendLine("Provide your critique in the following structured format:");
        sb.AppendLine();
        sb.AppendLine("### SCORES");
        foreach (var rubric in rubrics)
        {
            sb.AppendLine($"{rubric.Name}: [0-100]");
        }
        sb.AppendLine();
        sb.AppendLine("### ISSUES");
        sb.AppendLine("- [Category] - [Severity: High/Medium/Low] - [Description] - [Location]");
        sb.AppendLine();
        sb.AppendLine("### STRENGTHS");
        sb.AppendLine("- [What works well and should be preserved]");
        sb.AppendLine();
        sb.AppendLine("### SUGGESTIONS");
        sb.AppendLine("- [ChangeType: rewrite/expand/condense/reorder] - [Target: section/line] - [Suggestion] - [Expected Impact]");
        sb.AppendLine();
        sb.AppendLine("Provide detailed, actionable feedback that will help improve the script.");

        return sb.ToString();
    }

    private CritiqueResult ParseCritiqueResponse(string response, IReadOnlyList<RefinementRubric> rubrics)
    {
        var result = new CritiqueResult();
        var rubricScores = new Dictionary<string, double>();
        var issues = new List<CritiqueIssue>();
        var strengths = new List<string>();
        var suggestions = new List<CritiqueSuggestion>();

        try
        {
            foreach (var rubric in rubrics)
            {
                var score = ExtractRubricScore(response, rubric.Name);
                if (score.HasValue)
                {
                    rubricScores[rubric.Name] = score.Value;
                }
                else
                {
                    rubricScores[rubric.Name] = 75.0;
                }
            }

            var overallScore = rubrics.Any() 
                ? rubricScores.Sum(kvp => kvp.Value * rubrics.First(r => r.Name == kvp.Key).Weight) / rubrics.Sum(r => r.Weight)
                : 75.0;

            issues = ExtractIssues(response);
            strengths = ExtractStrengths(response);
            suggestions = ExtractSuggestions(response);

            return new CritiqueResult
            {
                OverallScore = overallScore,
                RubricScores = rubricScores,
                Issues = issues,
                Strengths = strengths,
                Suggestions = suggestions
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse critique response, using defaults");
            return CreateFallbackCritique();
        }
    }

    private double? ExtractRubricScore(string text, string rubricName)
    {
        var pattern = $@"{Regex.Escape(rubricName)}:\s*(\d+(?:\.\d+)?)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        return null;
    }

    private List<CritiqueIssue> ExtractIssues(string text)
    {
        var issues = new List<CritiqueIssue>();
        
        var issuesSection = ExtractSection(text, "ISSUES");
        if (string.IsNullOrWhiteSpace(issuesSection))
        {
            return issues;
        }

        var pattern = @"[-•*]\s*\[?([^\]]+)\]?\s*-\s*\[?Severity:\s*([^\]]+)\]?\s*-\s*([^-]+?)(?:\s*-\s*\[?Location:\s*([^\]]+)\]?)?(?=\n[-•*]|\n\n|$)";
        var matches = Regex.Matches(issuesSection, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            issues.Add(new CritiqueIssue
            {
                Category = match.Groups[1].Value.Trim(),
                Severity = match.Groups[2].Value.Trim(),
                Description = match.Groups[3].Value.Trim(),
                Location = match.Groups.Count > 4 ? match.Groups[4].Value.Trim() : null
            });
        }

        return issues;
    }

    private List<string> ExtractStrengths(string text)
    {
        var strengths = new List<string>();
        
        var strengthsSection = ExtractSection(text, "STRENGTHS");
        if (string.IsNullOrWhiteSpace(strengthsSection))
        {
            return strengths;
        }

        var pattern = @"[-•*]\s*(.+?)(?=\n[-•*]|\n\n|$)";
        var matches = Regex.Matches(strengthsSection, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var strength = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(strength))
            {
                strengths.Add(strength);
            }
        }

        return strengths;
    }

    private List<CritiqueSuggestion> ExtractSuggestions(string text)
    {
        var suggestions = new List<CritiqueSuggestion>();
        
        var suggestionsSection = ExtractSection(text, "SUGGESTIONS");
        if (string.IsNullOrWhiteSpace(suggestionsSection))
        {
            return suggestions;
        }

        var pattern = @"[-•*]\s*\[?ChangeType:\s*([^\]]+)\]?\s*-\s*\[?Target:\s*([^\]]+)\]?\s*-\s*([^-]+?)(?:\s*-\s*\[?Expected Impact:\s*([^\]]+)\]?)?(?=\n[-•*]|\n\n|$)";
        var matches = Regex.Matches(suggestionsSection, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            suggestions.Add(new CritiqueSuggestion
            {
                ChangeType = match.Groups[1].Value.Trim(),
                Target = match.Groups[2].Value.Trim(),
                Suggestion = match.Groups[3].Value.Trim(),
                ExpectedImpact = match.Groups.Count > 4 ? match.Groups[4].Value.Trim() : string.Empty
            });
        }

        return suggestions;
    }

    private string ExtractSection(string text, string sectionHeader)
    {
        var pattern = $@"###?\s*{Regex.Escape(sectionHeader)}\s*(.*?)(?=###?\s+[A-Z]|\z)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        return match.Success ? match.Groups[1].Value : string.Empty;
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

    private CritiqueResult CreateFallbackCritique()
    {
        return new CritiqueResult
        {
            OverallScore = 75.0,
            RubricScores = new Dictionary<string, double>
            {
                ["Clarity"] = 75.0,
                ["Coherence"] = 75.0,
                ["Timing"] = 75.0
            },
            Issues = new List<CritiqueIssue>(),
            Strengths = new List<string> { "Script structure appears sound" },
            Suggestions = new List<CritiqueSuggestion>()
        };
    }
}
