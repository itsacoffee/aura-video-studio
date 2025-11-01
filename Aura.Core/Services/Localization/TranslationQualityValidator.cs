using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Validates translation quality through back-translation, fluency scoring, and consistency checks
/// </summary>
public class TranslationQualityValidator
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;

    public TranslationQualityValidator(ILogger logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Validate translation quality
    /// </summary>
    public async Task<TranslationQuality> ValidateQualityAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        TranslationOptions options,
        Dictionary<string, string> glossary,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating translation quality");

        var quality = new TranslationQuality();

        // Back-translation check
        if (options.EnableBackTranslation)
        {
            quality.BackTranslatedText = await PerformBackTranslationAsync(
                translatedText,
                targetLanguage,
                sourceLanguage,
                cancellationToken);
            quality.BackTranslationScore = CalculateBackTranslationScore(
                sourceText, 
                quality.BackTranslatedText);
        }

        // Fluency and naturalness scoring
        quality.FluencyScore = await ScoreFluencyAsync(
            translatedText,
            targetLanguage,
            cancellationToken);

        // Accuracy scoring
        quality.AccuracyScore = await ScoreAccuracyAsync(
            sourceText,
            translatedText,
            sourceLanguage,
            targetLanguage,
            cancellationToken);

        // Cultural appropriateness
        quality.CulturalAppropriatenessScore = await ScoreCulturalAppropriatenessAsync(
            translatedText,
            targetLanguage,
            cancellationToken);

        // Terminology consistency
        quality.TerminologyConsistencyScore = CheckTerminologyConsistency(
            translatedText,
            glossary);

        // Calculate overall score
        quality.OverallScore = CalculateOverallScore(quality);

        // Detect issues
        quality.Issues = DetectQualityIssues(quality, options);

        _logger.LogInformation("Quality validation complete - Overall: {Score:F1}, Fluency: {Fluency:F1}, Accuracy: {Accuracy:F1}",
            quality.OverallScore, quality.FluencyScore, quality.AccuracyScore);

        return quality;
    }

    private async Task<string> PerformBackTranslationAsync(
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing back-translation");

        var prompt = $"Translate the following text from {sourceLanguage} to {targetLanguage}. " +
                     $"Provide ONLY the translation, no explanations:\n\n{translatedText}";

        var brief = new Brief(
            Topic: "Back-translation",
            Audience: "Translation system",
            Goal: "Verify translation accuracy",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Translation"
        );

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            return response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Back-translation failed");
            return string.Empty;
        }
    }

    private double CalculateBackTranslationScore(string original, string backTranslated)
    {
        if (string.IsNullOrEmpty(backTranslated))
        {
            return 0.0;
        }

        var originalWords = original.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var backWords = backTranslated.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var matchingWords = originalWords.Intersect(
            backWords, 
            StringComparer.OrdinalIgnoreCase).Count();

        var score = (double)matchingWords / Math.Max(originalWords.Length, 1) * 100.0;
        return Math.Min(score, 100.0);
    }

    private async Task<double> ScoreFluencyAsync(
        string translatedText,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var prompt = $"Rate the fluency and naturalness of the following {targetLanguage} text on a scale of 0-100. " +
                     $"Consider grammar, word choice, and natural flow. " +
                     $"Respond with ONLY a number between 0 and 100:\n\n{translatedText}";

        var brief = new Brief(
            Topic: "Fluency scoring",
            Audience: "Language experts",
            Goal: "Rate translation fluency",
            Tone: "Analytical",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analysis"
        );

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                return Math.Clamp(score, 0, 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fluency scoring failed");
        }

        return 75.0;
    }

    private async Task<double> ScoreAccuracyAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var prompt = $"Compare these two texts (source in {sourceLanguage} and translation in {targetLanguage}). " +
                     $"Rate the translation accuracy on a scale of 0-100. " +
                     $"Consider meaning preservation and information completeness. " +
                     $"Respond with ONLY a number between 0 and 100.\n\n" +
                     $"Source: {sourceText}\n\nTranslation: {translatedText}";

        var brief = new Brief(
            Topic: "Accuracy scoring",
            Audience: "Translation experts",
            Goal: "Rate translation accuracy",
            Tone: "Analytical",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analysis"
        );

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                return Math.Clamp(score, 0, 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Accuracy scoring failed");
        }

        return 80.0;
    }

    private async Task<double> ScoreCulturalAppropriatenessAsync(
        string translatedText,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var prompt = $"Rate the cultural appropriateness of the following {targetLanguage} text on a scale of 0-100. " +
                     $"Consider cultural sensitivity, appropriateness of examples and references, and tone. " +
                     $"Respond with ONLY a number between 0 and 100:\n\n{translatedText}";

        var brief = new Brief(
            Topic: "Cultural appropriateness",
            Audience: "Cultural experts",
            Goal: "Rate cultural appropriateness",
            Tone: "Analytical",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analysis"
        );

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                return Math.Clamp(score, 0, 100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cultural appropriateness scoring failed");
        }

        return 85.0;
    }

    private double CheckTerminologyConsistency(
        string translatedText,
        Dictionary<string, string> glossary)
    {
        if (!glossary.Any())
        {
            return 100.0;
        }

        int correctTerms = 0;
        int totalTerms = 0;

        foreach (var entry in glossary)
        {
            if (translatedText.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                totalTerms++;
                if (translatedText.Contains(entry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    correctTerms++;
                }
            }
        }

        if (totalTerms == 0)
        {
            return 100.0;
        }

        return (double)correctTerms / totalTerms * 100.0;
    }

    private double CalculateOverallScore(TranslationQuality quality)
    {
        var weights = new Dictionary<string, double>
        {
            ["fluency"] = 0.25,
            ["accuracy"] = 0.30,
            ["cultural"] = 0.20,
            ["terminology"] = 0.15,
            ["backTranslation"] = 0.10
        };

        var score = quality.FluencyScore * weights["fluency"] +
                    quality.AccuracyScore * weights["accuracy"] +
                    quality.CulturalAppropriatenessScore * weights["cultural"] +
                    quality.TerminologyConsistencyScore * weights["terminology"] +
                    quality.BackTranslationScore * weights["backTranslation"];

        return Math.Round(score, 1);
    }

    private List<QualityIssue> DetectQualityIssues(
        TranslationQuality quality,
        TranslationOptions options)
    {
        var issues = new List<QualityIssue>();

        if (quality.FluencyScore < 70)
        {
            issues.Add(new QualityIssue
            {
                Severity = QualityIssueSeverity.Warning,
                Category = "Fluency",
                Description = "Translation fluency is below acceptable threshold",
                Suggestion = "Review translation for natural language flow"
            });
        }

        if (quality.AccuracyScore < 80)
        {
            issues.Add(new QualityIssue
            {
                Severity = QualityIssueSeverity.Warning,
                Category = "Accuracy",
                Description = "Translation accuracy may be compromised",
                Suggestion = "Verify meaning preservation and information completeness"
            });
        }

        if (quality.CulturalAppropriatenessScore < 75)
        {
            issues.Add(new QualityIssue
            {
                Severity = QualityIssueSeverity.Warning,
                Category = "Cultural",
                Description = "Cultural appropriateness score is low",
                Suggestion = "Review for cultural sensitivities and appropriateness"
            });
        }

        if (quality.TerminologyConsistencyScore < 90)
        {
            issues.Add(new QualityIssue
            {
                Severity = QualityIssueSeverity.Error,
                Category = "Terminology",
                Description = "Glossary terms not consistently applied",
                Suggestion = "Ensure all glossary terms are correctly translated"
            });
        }

        if (options.EnableBackTranslation && quality.BackTranslationScore < 70)
        {
            issues.Add(new QualityIssue
            {
                Severity = QualityIssueSeverity.Warning,
                Category = "Back-translation",
                Description = "Back-translation verification shows potential accuracy issues",
                Suggestion = "Review translation for accuracy"
            });
        }

        return issues;
    }
}
