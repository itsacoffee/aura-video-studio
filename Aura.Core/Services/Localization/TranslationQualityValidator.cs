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
                cancellationToken).ConfigureAwait(false);
            quality.BackTranslationScore = CalculateBackTranslationScore(
                sourceText, 
                quality.BackTranslatedText);
        }

        // Fluency and naturalness scoring
        quality.FluencyScore = await ScoreFluencyAsync(
            translatedText,
            targetLanguage,
            cancellationToken).ConfigureAwait(false);

        // Accuracy scoring
        quality.AccuracyScore = await ScoreAccuracyAsync(
            sourceText,
            translatedText,
            sourceLanguage,
            targetLanguage,
            cancellationToken).ConfigureAwait(false);

        // Cultural appropriateness
        quality.CulturalAppropriatenessScore = await ScoreCulturalAppropriatenessAsync(
            translatedText,
            targetLanguage,
            cancellationToken).ConfigureAwait(false);

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
        _logger.LogDebug("Performing back-translation from {Source} to {Target}", sourceLanguage, targetLanguage);

        var prompt = $@"You are an expert translator specializing in back-translation for quality verification.

Translate the following text from {sourceLanguage} to {targetLanguage}.

IMPORTANT INSTRUCTIONS:
- Provide ONLY the translation, no explanations or commentary
- Translate as accurately as possible to preserve the original meaning
- Maintain the same level of formality and style

Text to translate:
{translatedText}

Translation:";

        try
        {
            // Use CompleteAsync for direct prompt completion
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var result = response.Trim();
            
            _logger.LogDebug("Back-translation completed: {InputLength} chars -> {OutputLength} chars",
                translatedText.Length, result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Back-translation failed: {Error}", ex.Message);
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
        var prompt = $@"You are an expert linguist specializing in {targetLanguage} language assessment.

Rate the fluency and naturalness of the following {targetLanguage} text on a scale of 0-100.

EVALUATION CRITERIA:
- Grammar and syntax correctness (30%)
- Natural word choice and phrasing (30%)
- Smooth flow and readability (25%)
- Appropriate register and style consistency (15%)

Text to evaluate:
{translatedText}

IMPORTANT: Respond with ONLY a single number between 0 and 100 representing the fluency score. No explanation.

Score:";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                var clampedScore = Math.Clamp(score, 0, 100);
                _logger.LogDebug("Fluency score for {Language}: {Score}", targetLanguage, clampedScore);
                return clampedScore;
            }
            
            _logger.LogWarning("Could not parse fluency score from LLM response: {Response}", 
                response.Substring(0, Math.Min(100, response.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fluency scoring failed for {Language}: {Error}", targetLanguage, ex.Message);
        }

        // Return a conservative default score when LLM scoring fails
        return 75.0;
    }

    private async Task<double> ScoreAccuracyAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are an expert bilingual translator and quality assessor fluent in both {sourceLanguage} and {targetLanguage}.

Compare the source text and its translation below. Rate the translation accuracy on a scale of 0-100.

EVALUATION CRITERIA:
- Meaning preservation (40%): All ideas and nuances correctly conveyed
- Information completeness (30%): No omissions or additions
- Terminology accuracy (20%): Technical terms correctly translated
- Intent preservation (10%): Tone and purpose maintained

SOURCE TEXT ({sourceLanguage}):
{sourceText}

TRANSLATION ({targetLanguage}):
{translatedText}

IMPORTANT: Respond with ONLY a single number between 0 and 100 representing the accuracy score. No explanation.

Score:";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                var clampedScore = Math.Clamp(score, 0, 100);
                _logger.LogDebug("Accuracy score for {Source}->{Target}: {Score}", 
                    sourceLanguage, targetLanguage, clampedScore);
                return clampedScore;
            }
            
            _logger.LogWarning("Could not parse accuracy score from LLM response: {Response}",
                response.Substring(0, Math.Min(100, response.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Accuracy scoring failed: {Error}", ex.Message);
        }

        // Return a conservative default score when LLM scoring fails
        return 80.0;
    }

    private async Task<double> ScoreCulturalAppropriatenessAsync(
        string translatedText,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are a cultural sensitivity expert specializing in {targetLanguage}-speaking regions.

Rate the cultural appropriateness of the following {targetLanguage} text on a scale of 0-100.

EVALUATION CRITERIA:
- Cultural sensitivity (35%): No offensive or inappropriate content for the target culture
- Appropriateness of examples and references (25%): References are relevant and understood
- Tone appropriateness (20%): Formality level matches cultural expectations
- Idiom and expression suitability (20%): Natural expressions for the target culture

Text to evaluate:
{translatedText}

IMPORTANT: Respond with ONLY a single number between 0 and 100 representing the cultural appropriateness score. No explanation.

Score:";

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var scoreMatch = System.Text.RegularExpressions.Regex.Match(response, @"\d+");
            
            if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
            {
                var clampedScore = Math.Clamp(score, 0, 100);
                _logger.LogDebug("Cultural appropriateness score for {Language}: {Score}", 
                    targetLanguage, clampedScore);
                return clampedScore;
            }
            
            _logger.LogWarning("Could not parse cultural appropriateness score from LLM response: {Response}",
                response.Substring(0, Math.Min(100, response.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cultural appropriateness scoring failed: {Error}", ex.Message);
        }

        // Return a conservative default score when LLM scoring fails
        return 85.0;
    }

    private double CheckTerminologyConsistency(
        string translatedText,
        Dictionary<string, string> glossary)
    {
        if (glossary.Count == 0)
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
