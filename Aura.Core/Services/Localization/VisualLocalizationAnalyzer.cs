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
/// Analyzes visual content for localization needs
/// Flags text-in-images, cultural symbols, and region-specific imagery
/// </summary>
public class VisualLocalizationAnalyzer
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;

    private static readonly Dictionary<string, List<string>> _culturallySensitiveSymbols = new()
    {
        ["colors"] = new() { "red", "white", "black", "green", "yellow" },
        ["gestures"] = new() { "thumbs up", "OK sign", "peace sign", "pointing" },
        ["animals"] = new() { "pig", "dog", "cow", "owl", "snake" },
        ["numbers"] = new() { "4", "13", "666" },
        ["religious"] = new() { "cross", "crescent", "star of david", "om" }
    };

    public VisualLocalizationAnalyzer(ILogger logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Analyze visual localization needs
    /// </summary>
    public async Task<List<VisualLocalizationRecommendation>> AnalyzeVisualLocalizationNeedsAsync(
        List<TranslatedScriptLine> translatedLines,
        LanguageInfo targetLanguage,
        CulturalContext? culturalContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Analyzing visual localization needs for {Language}", targetLanguage.Code);

        var recommendations = new List<VisualLocalizationRecommendation>();

        // Check for text in images
        recommendations.AddRange(DetectTextInImageReferences(translatedLines));

        // Check for culturally sensitive symbols
        recommendations.AddRange(DetectCulturallySensitiveElements(translatedLines, targetLanguage));

        // LLM-powered analysis for complex cases
        var llmRecommendations = await AnalyzeVisualContentWithLlmAsync(
            translatedLines,
            targetLanguage,
            culturalContext,
            cancellationToken).ConfigureAwait(false);
        recommendations.AddRange(llmRecommendations);

        _logger.LogInformation("Found {Count} visual localization recommendations", recommendations.Count);
        return recommendations;
    }

    private List<VisualLocalizationRecommendation> DetectTextInImageReferences(
        List<TranslatedScriptLine> lines)
    {
        var recommendations = new List<VisualLocalizationRecommendation>();

        var textInImageIndicators = new[]
        {
            "sign", "label", "caption", "text", "written", "banner", "poster",
            "menu", "button", "interface", "screen", "display"
        };

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var hasTextReference = textInImageIndicators.Any(indicator =>
                line.TranslatedText.Contains(indicator, StringComparison.OrdinalIgnoreCase));

            if (hasTextReference)
            {
                recommendations.Add(new VisualLocalizationRecommendation
                {
                    ElementType = VisualElementType.TextInImage,
                    Description = "Scene may contain text in images that requires translation",
                    Recommendation = "Ensure any text in visuals is translated or replaced with localized version",
                    Priority = LocalizationPriority.Critical,
                    SceneIndex = i
                });
            }
        }

        return recommendations;
    }

    private List<VisualLocalizationRecommendation> DetectCulturallySensitiveElements(
        List<TranslatedScriptLine> lines,
        LanguageInfo targetLanguage)
    {
        var recommendations = new List<VisualLocalizationRecommendation>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lowerText = line.TranslatedText.ToLowerInvariant();

            foreach (var category in _culturallySensitiveSymbols)
            {
                foreach (var symbol in category.Value)
                {
                    if (lowerText.Contains(symbol))
                    {
                        var priority = DeterminePriority(category.Key, symbol, targetLanguage);
                        
                        recommendations.Add(new VisualLocalizationRecommendation
                        {
                            ElementType = VisualElementType.CulturalSymbol,
                            Description = $"References {symbol} which may have cultural significance in {targetLanguage.Name}",
                            Recommendation = GetCulturalRecommendation(category.Key, symbol, targetLanguage),
                            Priority = priority,
                            SceneIndex = i
                        });
                    }
                }
            }
        }

        return recommendations;
    }

    private async Task<List<VisualLocalizationRecommendation>> AnalyzeVisualContentWithLlmAsync(
        List<TranslatedScriptLine> lines,
        LanguageInfo targetLanguage,
        CulturalContext? culturalContext,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<VisualLocalizationRecommendation>();

        var combinedText = string.Join(" ", lines.Select(l => l.TranslatedText));
        var prompt = BuildVisualAnalysisPrompt(combinedText, targetLanguage, culturalContext);

        var brief = new Brief(
            Topic: "Visual localization",
            Audience: "Cultural experts",
            Goal: "Identify visual elements requiring localization",
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
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken).ConfigureAwait(false);
            var parsedRecommendations = ParseVisualRecommendations(response);
            recommendations.AddRange(parsedRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM visual analysis failed");
        }

        return recommendations;
    }

    private string BuildVisualAnalysisPrompt(
        string content,
        LanguageInfo targetLanguage,
        CulturalContext? culturalContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Analyze the following content for visual elements that may need localization for {targetLanguage.Name} audience.");
        sb.AppendLine();
        sb.AppendLine("Identify:");
        sb.AppendLine("1. Visual imagery that may be culturally inappropriate or insensitive");
        sb.AppendLine("2. Symbols, colors, or gestures with different cultural meanings");
        sb.AppendLine("3. Regional imagery that should be adapted (e.g., landmarks, clothing, architecture)");
        sb.AppendLine("4. Brand logos or products that need localization");
        sb.AppendLine();

        if (culturalContext != null && culturalContext.Sensitivities.Count != 0)
        {
            sb.AppendLine("Cultural sensitivities to consider:");
            foreach (var sensitivity in culturalContext.Sensitivities)
            {
                sb.AppendLine($"- {sensitivity}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Content:");
        sb.AppendLine(content);
        sb.AppendLine();
        sb.AppendLine("Provide recommendations in this format:");
        sb.AppendLine("Element: [visual element]");
        sb.AppendLine("Issue: [cultural concern]");
        sb.AppendLine("Recommendation: [localization suggestion]");
        sb.AppendLine("Priority: [Optional/Recommended/Important/Critical]");

        return sb.ToString();
    }

    private List<VisualLocalizationRecommendation> ParseVisualRecommendations(string response)
    {
        var recommendations = new List<VisualLocalizationRecommendation>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? element = null;
        string? issue = null;
        string? recommendation = null;
        string? priorityStr = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("Element:", StringComparison.OrdinalIgnoreCase))
            {
                element = line.Substring("Element:".Length).Trim();
            }
            else if (line.StartsWith("Issue:", StringComparison.OrdinalIgnoreCase))
            {
                issue = line.Substring("Issue:".Length).Trim();
            }
            else if (line.StartsWith("Recommendation:", StringComparison.OrdinalIgnoreCase))
            {
                recommendation = line.Substring("Recommendation:".Length).Trim();
            }
            else if (line.StartsWith("Priority:", StringComparison.OrdinalIgnoreCase))
            {
                priorityStr = line.Substring("Priority:".Length).Trim();

                if (element != null && recommendation != null)
                {
                    var priority = ParsePriority(priorityStr);
                    
                    recommendations.Add(new VisualLocalizationRecommendation
                    {
                        ElementType = VisualElementType.RegionalImagery,
                        Description = issue ?? element,
                        Recommendation = recommendation,
                        Priority = priority
                    });

                    element = null;
                    issue = null;
                    recommendation = null;
                    priorityStr = null;
                }
            }
        }

        return recommendations;
    }

    private LocalizationPriority ParsePriority(string? priorityStr)
    {
        if (priorityStr == null)
        {
            return LocalizationPriority.Recommended;
        }

        return priorityStr.ToLowerInvariant() switch
        {
            "critical" => LocalizationPriority.Critical,
            "important" => LocalizationPriority.Important,
            "recommended" => LocalizationPriority.Recommended,
            "optional" => LocalizationPriority.Optional,
            _ => LocalizationPriority.Recommended
        };
    }

    private LocalizationPriority DeterminePriority(
        string category,
        string symbol,
        LanguageInfo targetLanguage)
    {
        if (category == "religious" && targetLanguage.CulturalSensitivities.Count != 0)
        {
            return LocalizationPriority.Critical;
        }

        if (category == "gestures")
        {
            return LocalizationPriority.Important;
        }

        return LocalizationPriority.Recommended;
    }

    private string GetCulturalRecommendation(
        string category,
        string symbol,
        LanguageInfo targetLanguage)
    {
        return category switch
        {
            "colors" => $"Verify color symbolism in {targetLanguage.Name} culture. {symbol} may have different cultural meanings.",
            "gestures" => $"The gesture '{symbol}' may be offensive or have different meaning in {targetLanguage.Name} culture. Consider alternative visuals.",
            "animals" => $"The animal '{symbol}' may have cultural or religious significance in {targetLanguage.Name} culture. Review appropriateness.",
            "numbers" => $"The number {symbol} may be considered unlucky in {targetLanguage.Name} culture. Consider avoiding or replacing.",
            "religious" => $"Religious symbol '{symbol}' requires careful handling for {targetLanguage.Name} audience. Ensure cultural appropriateness.",
            _ => $"Review cultural appropriateness of '{symbol}' for {targetLanguage.Name} audience."
        };
    }
}
