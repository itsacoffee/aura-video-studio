using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Engine for applying cultural localization beyond literal translation
/// Handles idioms, cultural references, humor, and cultural sensitivities
/// </summary>
public class CulturalLocalizationEngine
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;

    // Cultural reference mappings - could be moved to configuration file for easier updates
    private static readonly Dictionary<string, List<CulturalReference>> _culturalReferences = new()
    {
        ["sports"] = new()
        {
            new() { OriginalReference = "NFL", Regions = new() { ["UK"] = "Premier League", ["IN"] = "Cricket", ["JP"] = "Baseball" } },
            new() { OriginalReference = "Super Bowl", Regions = new() { ["UK"] = "FA Cup Final", ["BR"] = "Copa do Mundo", ["AU"] = "AFL Grand Final" } },
            new() { OriginalReference = "World Series", Regions = new() { ["UK"] = "Cricket World Cup", ["EU"] = "Champions League" } },
        },
        ["holidays"] = new()
        {
            new() { OriginalReference = "Thanksgiving", Regions = new() { ["UK"] = "Christmas", ["CN"] = "Mid-Autumn Festival", ["IN"] = "Diwali" } },
            new() { OriginalReference = "Fourth of July", Regions = new() { ["FR"] = "Bastille Day", ["UK"] = "Boxing Day", ["MX"] = "Independence Day" } },
        },
        ["measurements"] = new()
        {
            new() { OriginalReference = "miles", Regions = new() { ["Global"] = "kilometers" } },
            new() { OriginalReference = "feet", Regions = new() { ["Global"] = "meters" } },
            new() { OriginalReference = "pounds", Regions = new() { ["Global"] = "kilograms" } },
            new() { OriginalReference = "Fahrenheit", Regions = new() { ["Global"] = "Celsius" } },
        }
    };

    public CulturalLocalizationEngine(ILogger logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Apply cultural adaptations to translated text
    /// </summary>
    public async Task<List<CulturalAdaptation>> ApplyCulturalAdaptationsAsync(
        List<TranslatedScriptLine> translatedLines,
        CulturalContext culturalContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying cultural adaptations for {Region}", culturalContext.TargetRegion);

        var adaptations = new List<CulturalAdaptation>();

        for (int i = 0; i < translatedLines.Count; i++)
        {
            var line = translatedLines[i];
            
            // Apply automatic cultural reference replacements
            var autoAdaptations = ApplyAutomaticAdaptations(line, culturalContext);
            adaptations.AddRange(autoAdaptations);

            // Apply LLM-powered cultural analysis for idioms and expressions
            var llmAdaptations = await AnalyzeAndAdaptCulturalExpressionsAsync(
                line,
                culturalContext,
                cancellationToken).ConfigureAwait(false);
            adaptations.AddRange(llmAdaptations);

            // Update the line with adaptations
            foreach (var adaptation in autoAdaptations.Concat(llmAdaptations))
            {
                if (adaptation.LineNumber == i)
                {
                    line.TranslatedText = line.TranslatedText.Replace(
                        adaptation.SourcePhrase, 
                        adaptation.AdaptedPhrase);
                    line.AdaptationNotes.Add($"{adaptation.Category}: {adaptation.Reasoning}");
                }
            }
        }

        _logger.LogInformation("Applied {Count} cultural adaptations", adaptations.Count);
        return adaptations;
    }

    /// <summary>
    /// Analyze cultural content for appropriateness
    /// </summary>
    public async Task<CulturalAnalysisResult> AnalyzeCulturalContentAsync(
        string content,
        LanguageInfo targetLanguage,
        string targetRegion,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing cultural content for {Language}/{Region}", 
            targetLanguage.Code, targetRegion);

        var prompt = BuildCulturalAnalysisPrompt(content, targetLanguage, targetRegion);

        try
        {
            // Use CompleteAsync for direct prompt completion
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var result = ParseCulturalAnalysis(response, targetLanguage.Code, targetRegion);
            
            _logger.LogDebug("Cultural analysis complete for {Language}/{Region}: Score={Score}",
                targetLanguage.Code, targetRegion, result.CulturalSensitivityScore);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cultural analysis failed for {Language}/{Region}: {Error}",
                targetLanguage.Code, targetRegion, ex.Message);
            return new CulturalAnalysisResult
            {
                TargetLanguage = targetLanguage.Code,
                TargetRegion = targetRegion,
                CulturalSensitivityScore = 50.0
            };
        }
    }

    private List<CulturalAdaptation> ApplyAutomaticAdaptations(
        TranslatedScriptLine line,
        CulturalContext culturalContext)
    {
        var adaptations = new List<CulturalAdaptation>();

        // Check for known cultural references
        foreach (var category in _culturalReferences)
        {
            foreach (var reference in category.Value)
            {
                if (line.TranslatedText.Contains(reference.OriginalReference, StringComparison.OrdinalIgnoreCase))
                {
                    var replacement = FindBestReplacement(reference, culturalContext.TargetRegion);
                    if (replacement != null)
                    {
                        adaptations.Add(new CulturalAdaptation
                        {
                            Category = category.Key,
                            SourcePhrase = reference.OriginalReference,
                            AdaptedPhrase = replacement,
                            Reasoning = $"Replaced culture-specific {category.Key} reference with local equivalent",
                            LineNumber = line.SceneIndex
                        });
                    }
                }
            }
        }

        return adaptations;
    }

    private async Task<List<CulturalAdaptation>> AnalyzeAndAdaptCulturalExpressionsAsync(
        TranslatedScriptLine line,
        CulturalContext culturalContext,
        CancellationToken cancellationToken)
    {
        var adaptations = new List<CulturalAdaptation>();

        // Check for idioms, humor, and expressions that need cultural adaptation
        var hasIdioms = ContainsPotentialIdioms(line.TranslatedText);
        if (!hasIdioms)
        {
            return adaptations;
        }

        var prompt = BuildIdiomAdaptationPrompt(line.TranslatedText, culturalContext);

        try
        {
            // Use CompleteAsync for direct prompt completion
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var parsedAdaptations = ParseIdiomAdaptations(response, line.SceneIndex);
            adaptations.AddRange(parsedAdaptations);
            
            if (parsedAdaptations.Count > 0)
            {
                _logger.LogDebug("Found {Count} idiom adaptations for line {Index}", 
                    parsedAdaptations.Count, line.SceneIndex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idiom adaptation failed for line {Index}: {Error}", 
                line.SceneIndex, ex.Message);
        }

        return adaptations;
    }

    private string? FindBestReplacement(CulturalReference reference, string targetRegion)
    {
        if (reference.Regions.TryGetValue(targetRegion, out var replacement))
        {
            return replacement;
        }

        // Try to find a global replacement
        if (reference.Regions.TryGetValue("Global", out var globalReplacement))
        {
            return globalReplacement;
        }

        // Try to find regional matches (e.g., "UK" for "Europe")
        var regionPrefixes = new[] { "EU", "NA", "AS", "LA", "ME", "AF" };
        foreach (var prefix in regionPrefixes)
        {
            if (targetRegion.StartsWith(prefix) && 
                reference.Regions.TryGetValue(prefix, out var regionalReplacement))
            {
                return regionalReplacement;
            }
        }

        return null;
    }

    private bool ContainsPotentialIdioms(string text)
    {
        var idiomIndicators = new[]
        {
            "like", "as if", "piece of cake", "break a leg", "hit the nail",
            "under the weather", "costs an arm", "break the ice", "once in a blue moon"
        };

        return idiomIndicators.Any(indicator => 
            text.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }

    private string BuildIdiomAdaptationPrompt(string text, CulturalContext culturalContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"You are an expert cultural linguist specializing in idiom and expression adaptation for the {culturalContext.TargetRegion} region.

TASK: Analyze the following text and identify idioms, expressions, or phrases that may not translate well culturally. For each one, provide a culturally-appropriate alternative.

TARGET AUDIENCE CONTEXT:
- Region: {culturalContext.TargetRegion}
- Formality Level: {culturalContext.TargetFormality}
- Preferred Style: {culturalContext.PreferredStyle}

ANALYSIS GUIDELINES:
1. Look for idioms that are culture-specific and may not be understood
2. Identify expressions that might be offensive or inappropriate in the target culture
3. Find references that need cultural adaptation (sports, holidays, customs, etc.)
4. Consider formality expectations of the target culture
5. Identify humor or sarcasm that may not translate well

TEXT TO ANALYZE:
{text}

OUTPUT FORMAT (provide one entry per idiom/expression found):
Original: [the original phrase]
Adapted: [the culturally appropriate alternative in the same language]
Reason: [brief explanation of why adaptation was needed]

If no adaptations are needed, respond with: ""No adaptations required - text is culturally appropriate.""");

        return sb.ToString();
    }

    private string BuildCulturalAnalysisPrompt(string content, LanguageInfo targetLanguage, string targetRegion)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"You are a cultural sensitivity expert specializing in content localization for {targetLanguage.Name}-speaking audiences in {targetRegion}.

TASK: Analyze the following content for cultural appropriateness and sensitivity.

EVALUATION FRAMEWORK:
1. CULTURAL SENSITIVITIES (25%): Identify content that may be offensive, taboo, or uncomfortable
2. HUMOR AND TONE (20%): Assess whether humor and tone are appropriate for the culture
3. VISUAL REFERENCES (15%): Flag any visual descriptions that may be culturally problematic
4. RELIGIOUS/POLITICAL CONTENT (20%): Identify any references that need careful handling
5. SOCIAL NORMS (20%): Check alignment with gender roles, family structures, and social expectations");

        if (targetLanguage.CulturalSensitivities.Count != 0)
        {
            sb.AppendLine();
            sb.AppendLine("KNOWN CULTURAL SENSITIVITIES FOR THIS REGION:");
            foreach (var sensitivity in targetLanguage.CulturalSensitivities)
            {
                sb.AppendLine($"  • {sensitivity}");
            }
        }

        sb.AppendLine($@"

CONTENT TO ANALYZE:
═══════════════════════════════════════════════════════════════
{content}
═══════════════════════════════════════════════════════════════

REQUIRED OUTPUT:
1. Cultural Sensitivity Score: [0-100] (where 100 = fully appropriate)
2. Issues Found:
   - [Issue 1 description]
   - [Issue 2 description]
   (or ""No issues found"")
3. Recommendations:
   - [Recommendation 1]
   - [Recommendation 2]
   (or ""No changes recommended"")");

        return sb.ToString();
    }

    private List<CulturalAdaptation> ParseIdiomAdaptations(string response, int lineIndex)
    {
        var adaptations = new List<CulturalAdaptation>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? original = null;
        string? adapted = null;
        string? reason = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("Original:", StringComparison.OrdinalIgnoreCase))
            {
                original = line.Substring("Original:".Length).Trim();
            }
            else if (line.StartsWith("Adapted:", StringComparison.OrdinalIgnoreCase))
            {
                adapted = line.Substring("Adapted:".Length).Trim();
            }
            else if (line.StartsWith("Reason:", StringComparison.OrdinalIgnoreCase))
            {
                reason = line.Substring("Reason:".Length).Trim();

                if (original != null && adapted != null && reason != null)
                {
                    adaptations.Add(new CulturalAdaptation
                    {
                        Category = "idioms",
                        SourcePhrase = original,
                        AdaptedPhrase = adapted,
                        Reasoning = reason,
                        LineNumber = lineIndex
                    });

                    original = null;
                    adapted = null;
                    reason = null;
                }
            }
        }

        return adaptations;
    }

    private CulturalAnalysisResult ParseCulturalAnalysis(
        string response, 
        string targetLanguage, 
        string targetRegion)
    {
        var result = new CulturalAnalysisResult
        {
            TargetLanguage = targetLanguage,
            TargetRegion = targetRegion,
            CulturalSensitivityScore = 70.0
        };

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("score", StringComparison.OrdinalIgnoreCase))
            {
                var scoreMatch = System.Text.RegularExpressions.Regex.Match(line, @"\d+");
                if (scoreMatch.Success && double.TryParse(scoreMatch.Value, out var score))
                {
                    result.CulturalSensitivityScore = score;
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Cultural reference with regional alternatives
/// </summary>
internal class CulturalReference
{
    public string OriginalReference { get; set; } = string.Empty;
    public Dictionary<string, string> Regions { get; set; } = new();
}
