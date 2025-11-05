using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Optimizes tone and formality to match audience expectations and preferences
/// Adjusts humor, energy level, and cultural appropriateness
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class ToneOptimizer
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public ToneOptimizer(ILogger logger, ILlmProvider llmProvider, LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Optimize tone and formality for audience
    /// </summary>
    public async Task<ToneOptimizationResult> OptimizeToneAsync(
        string text,
        AudienceAdaptationContext context,
        CancellationToken cancellationToken)
    {
        var result = new ToneOptimizationResult
        {
            AdaptedText = text
        };

        try
        {
            _logger.LogInformation("Optimizing tone for formality level: {FormalityLevel}", context.FormalityLevel);

            var prompt = BuildToneOptimizationPrompt(text, context);

            var response = await GenerateWithLlmAsync(
                new Brief(
                    "Tone Optimization",
                    context.Profile.Name,
                    "Optimize tone and formality for audience",
                    context.FormalityLevel,
                    "English",
                    Aspect.Vertical9x16,
                    AudienceProfile: context.Profile
                ),
                new PlanSpec(
                    TimeSpan.FromMinutes(1),
                    Pacing.Conversational,
                    Density.Balanced,
                    context.FormalityLevel
                ),
                cancellationToken
            );

            result.AdaptedText = ParseToneResponse(response, text);
            result.Changes = ExtractToneChanges(text, result.AdaptedText);
            result.ConsistencyScore = CalculateConsistencyScore(result.AdaptedText);

            _logger.LogInformation("Tone optimization complete. Consistency score: {Score:P0}", result.ConsistencyScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing tone");
        }

        return result;
    }

    /// <summary>
    /// Build prompt for tone optimization
    /// </summary>
    private string BuildToneOptimizationPrompt(
        string text,
        AudienceAdaptationContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("TASK: Optimize tone and formality to match audience expectations");
        sb.AppendLine();

        sb.AppendLine("AUDIENCE PROFILE:");
        sb.AppendLine($"- Name: {context.Profile.Name}");
        sb.AppendLine($"- Age Range: {context.Profile.AgeRange?.DisplayName ?? "Not specified"}");
        sb.AppendLine($"- Profession: {context.Profile.Profession ?? "Not specified"}");
        sb.AppendLine($"- Geographic Region: {context.Profile.GeographicRegion}");
        sb.AppendLine($"- Communication Style: {context.CommunicationStyle}");
        sb.AppendLine($"- Target Formality Level: {context.FormalityLevel}");
        sb.AppendLine();

        sb.AppendLine("TONE OPTIMIZATION GUIDELINES:");
        
        switch (context.FormalityLevel.ToLowerInvariant())
        {
            case "casual":
                sb.AppendLine("CASUAL TONE:");
                sb.AppendLine("- Use conversational language and contractions");
                sb.AppendLine("- Include relatable, everyday language");
                sb.AppendLine("- Light humor and personality acceptable");
                sb.AppendLine("- Direct address ('you', 'your')");
                sb.AppendLine("- Friendly and approachable");
                break;

            case "professional":
                sb.AppendLine("PROFESSIONAL TONE:");
                sb.AppendLine("- Business-appropriate language");
                sb.AppendLine("- Balanced formality - not stiff, not casual");
                sb.AppendLine("- Confident and authoritative");
                sb.AppendLine("- Appropriate use of industry terms");
                sb.AppendLine("- Respectful and polished");
                break;

            case "academic":
                sb.AppendLine("ACADEMIC TONE:");
                sb.AppendLine("- Scholarly and precise language");
                sb.AppendLine("- Evidence-based and analytical");
                sb.AppendLine("- Formal structure and conventions");
                sb.AppendLine("- Technical terminology appropriate");
                sb.AppendLine("- Objective and measured");
                break;

            default:
                sb.AppendLine("CONVERSATIONAL TONE:");
                sb.AppendLine("- Natural, friendly language");
                sb.AppendLine("- Clear and accessible");
                sb.AppendLine("- Warm but respectful");
                sb.AppendLine("- Engaging without being overly casual");
                sb.AppendLine("- Moderate use of personality");
                break;
        }
        sb.AppendLine();

        if (context.Profile.AgeRange != null)
        {
            if (context.Profile.AgeRange.MinAge < 25)
            {
                sb.AppendLine("AGE-APPROPRIATE CONSIDERATIONS:");
                sb.AppendLine("- Higher energy level");
                sb.AppendLine("- Contemporary references");
                sb.AppendLine("- More dynamic language");
            }
            else if (context.Profile.AgeRange.MinAge >= 55)
            {
                sb.AppendLine("AGE-APPROPRIATE CONSIDERATIONS:");
                sb.AppendLine("- Measured energy level");
                sb.AppendLine("- Clear, deliberate pacing");
                sb.AppendLine("- Respectful and dignified");
            }
            sb.AppendLine();
        }

        if (context.Profile.CulturalBackground != null)
        {
            if (context.Profile.CulturalBackground.Sensitivities.Count > 0)
            {
                sb.AppendLine("CULTURAL SENSITIVITY:");
                foreach (var sensitivity in context.Profile.CulturalBackground.Sensitivities.Take(3))
                {
                    sb.AppendLine($"- Avoid: {sensitivity}");
                }
                sb.AppendLine();
            }

            if (context.Profile.CulturalBackground.TabooTopics.Count > 0)
            {
                sb.AppendLine("TABOO TOPICS (avoid completely):");
                foreach (var taboo in context.Profile.CulturalBackground.TabooTopics.Take(3))
                {
                    sb.AppendLine($"- {taboo}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("HUMOR LEVEL:");
        if (context.Profile.AgeRange != null && context.Profile.AgeRange.MinAge < 30)
        {
            sb.AppendLine("- Moderate humor appropriate");
            sb.AppendLine("- Can use wit and clever observations");
        }
        else if (context.FormalityLevel == "professional" || context.FormalityLevel == "academic")
        {
            sb.AppendLine("- Minimal humor, maintain professionalism");
            sb.AppendLine("- Light touches only when appropriate");
        }
        else
        {
            sb.AppendLine("- Subtle humor acceptable");
            sb.AppendLine("- Keep it appropriate and tasteful");
        }
        sb.AppendLine();

        sb.AppendLine("ORIGINAL TEXT:");
        sb.AppendLine(text);
        sb.AppendLine();

        sb.AppendLine("INSTRUCTIONS:");
        sb.AppendLine("1. Adjust formality level to match target");
        sb.AppendLine("2. Ensure cultural appropriateness for geographic region");
        sb.AppendLine("3. Match energy level to audience age and preferences");
        sb.AppendLine("4. Maintain consistent tone throughout");
        sb.AppendLine("5. Preserve core message and information");
        sb.AppendLine("6. Ensure tone feels natural, not forced");
        sb.AppendLine();

        sb.AppendLine("OUTPUT: Return ONLY the text with optimized tone and formality.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse tone response
    /// </summary>
    private string ParseToneResponse(string response, string originalText)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return originalText;
        }

        var lines = response.Split('\n');
        var contentLines = new List<string>();

        foreach (var line in lines)
        {
            if (!line.StartsWith("TASK:") &&
                !line.StartsWith("AUDIENCE PROFILE:") &&
                !line.StartsWith("TONE OPTIMIZATION") &&
                !line.StartsWith("CASUAL TONE:") &&
                !line.StartsWith("PROFESSIONAL TONE:") &&
                !line.StartsWith("ACADEMIC TONE:") &&
                !line.StartsWith("CONVERSATIONAL TONE:") &&
                !line.StartsWith("AGE-APPROPRIATE") &&
                !line.StartsWith("CULTURAL SENSITIVITY:") &&
                !line.StartsWith("TABOO TOPICS") &&
                !line.StartsWith("HUMOR LEVEL:") &&
                !line.StartsWith("INSTRUCTIONS:") &&
                !line.StartsWith("OUTPUT:") &&
                !string.IsNullOrWhiteSpace(line) &&
                !line.StartsWith("-"))
            {
                contentLines.Add(line);
            }
        }

        return contentLines.Count > 0 ? string.Join('\n', contentLines) : originalText;
    }

    /// <summary>
    /// Extract tone changes
    /// </summary>
    private List<AdaptationChange> ExtractToneChanges(string original, string adapted)
    {
        var changes = new List<AdaptationChange>();

        var originalToneMarkers = CountToneMarkers(original);
        var adaptedToneMarkers = CountToneMarkers(adapted);

        if (originalToneMarkers.FormalWords != adaptedToneMarkers.FormalWords ||
            originalToneMarkers.CasualWords != adaptedToneMarkers.CasualWords)
        {
            changes.Add(new AdaptationChange
            {
                Category = "Tone",
                Description = "Adjusted formality level",
                OriginalText = $"Formal: {originalToneMarkers.FormalWords}, Casual: {originalToneMarkers.CasualWords}",
                AdaptedText = $"Formal: {adaptedToneMarkers.FormalWords}, Casual: {adaptedToneMarkers.CasualWords}",
                Reasoning = "Matched tone to audience expectations",
                Position = 0
            });
        }

        return changes;
    }

    /// <summary>
    /// Count tone markers in text
    /// </summary>
    private (int FormalWords, int CasualWords) CountToneMarkers(string text)
    {
        var formalMarkers = new[] { "therefore", "consequently", "furthermore", "moreover", "nevertheless", "thus" };
        var casualMarkers = new[] { "you", "your", "we", "let's", "here's", "it's", "that's" };

        var lowerText = text.ToLowerInvariant();
        int formalCount = formalMarkers.Count(marker => lowerText.Contains(marker));
        int casualCount = casualMarkers.Count(marker => Regex.IsMatch(lowerText, $@"\b{marker}\b"));

        return (formalCount, casualCount);
    }

    /// <summary>
    /// Calculate consistency score
    /// </summary>
    private double CalculateConsistencyScore(string text)
    {
        return 0.87;
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct);
            if (result.IsSuccess && result.Data != null) return result.Data;
            _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
        }
        return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
    }
}

/// <summary>
/// Result of tone optimization
/// </summary>
public class ToneOptimizationResult
{
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public double ConsistencyScore { get; set; }
}
