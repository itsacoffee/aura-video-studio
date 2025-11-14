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
                cancellationToken);
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
        
        var brief = LlmRequestHelper.CreateAnalysisBrief(
            "Cultural analysis",
            "Cultural experts",
            "Analyze cultural appropriateness"
        );

        var spec = LlmRequestHelper.CreateAnalysisPlanSpec();

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            return ParseCulturalAnalysis(response, targetLanguage.Code, targetRegion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cultural analysis failed");
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
        
        var brief = LlmRequestHelper.CreateAnalysisBrief(
            "Idiom adaptation",
            "Translation experts",
            "Adapt idioms and expressions"
        );

        var spec = LlmRequestHelper.CreateAnalysisPlanSpec();

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            var parsedAdaptations = ParseIdiomAdaptations(response, line.SceneIndex);
            adaptations.AddRange(parsedAdaptations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idiom adaptation failed for line {Index}", line.SceneIndex);
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
        sb.AppendLine("Analyze the following translated text for idioms and expressions that may not work in the target culture.");
        sb.AppendLine($"Target region: {culturalContext.TargetRegion}");
        sb.AppendLine($"Formality level: {culturalContext.TargetFormality}");
        sb.AppendLine();
        sb.AppendLine("For any idioms or culturally-specific expressions, suggest culturally-appropriate alternatives.");
        sb.AppendLine();
        sb.AppendLine("Text:");
        sb.AppendLine(text);
        sb.AppendLine();
        sb.AppendLine("Provide adaptations in this format:");
        sb.AppendLine("Original: [phrase]");
        sb.AppendLine("Adapted: [culturally appropriate alternative]");
        sb.AppendLine("Reason: [explanation]");

        return sb.ToString();
    }

    private string BuildCulturalAnalysisPrompt(string content, LanguageInfo targetLanguage, string targetRegion)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Analyze the following content for cultural appropriateness in {targetLanguage.Name} ({targetRegion}).");
        sb.AppendLine();
        sb.AppendLine("Consider:");
        sb.AppendLine("1. Cultural sensitivities and taboo topics");
        sb.AppendLine("2. Appropriateness of humor and tone");
        sb.AppendLine("3. Visual elements that may be culturally sensitive");
        sb.AppendLine("4. Religious or political references");
        sb.AppendLine("5. Gender and social norms");
        sb.AppendLine();

        if (targetLanguage.CulturalSensitivities.Count != 0)
        {
            sb.AppendLine("Known cultural sensitivities:");
            foreach (var sensitivity in targetLanguage.CulturalSensitivities)
            {
                sb.AppendLine($"- {sensitivity}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Content:");
        sb.AppendLine(content);
        sb.AppendLine();
        sb.AppendLine("Provide analysis with:");
        sb.AppendLine("- Overall cultural sensitivity score (0-100)");
        sb.AppendLine("- Specific issues identified");
        sb.AppendLine("- Recommendations for improvement");

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
