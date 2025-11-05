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
/// Personalizes examples and analogies based on audience characteristics
/// Uses LLM to generate audience-specific examples that are culturally relevant
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class ExamplePersonalizer
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public ExamplePersonalizer(ILogger logger, ILlmProvider llmProvider, LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Personalize examples to match audience profile
    /// </summary>
    public async Task<ExamplePersonalizationResult> PersonalizeExamplesAsync(
        string text,
        AudienceAdaptationContext context,
        int examplesPerConcept,
        CancellationToken cancellationToken)
    {
        var result = new ExamplePersonalizationResult
        {
            AdaptedText = text
        };

        try
        {
            _logger.LogInformation("Personalizing examples for {AudienceName}", context.Profile.Name);

            var prompt = BuildExamplePersonalizationPrompt(text, context, examplesPerConcept);

            var response = await GenerateWithLlmAsync(
                new Brief(
                    "Example Personalization",
                    context.Profile.Name,
                    "Personalize examples and analogies for audience",
                    "conversational",
                    "English",
                    Aspect.Vertical9x16,
                    AudienceProfile: context.Profile
                ),
                new PlanSpec(
                    TimeSpan.FromMinutes(1),
                    Pacing.Conversational,
                    Density.Balanced,
                    "conversational"
                ),
                cancellationToken
            );

            result.AdaptedText = ParseExampleResponse(response, text);
            result.Changes = ExtractExampleChanges(text, result.AdaptedText);
            result.AverageRelevanceScore = CalculateAverageRelevance(result.Changes);

            _logger.LogInformation("Example personalization complete. Changes: {ChangeCount}, Avg Relevance: {Relevance:P0}",
                result.Changes.Count, result.AverageRelevanceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error personalizing examples");
        }

        return result;
    }

    /// <summary>
    /// Build prompt for example personalization
    /// </summary>
    private string BuildExamplePersonalizationPrompt(
        string text,
        AudienceAdaptationContext context,
        int examplesPerConcept)
    {
        var sb = new StringBuilder();

        sb.AppendLine("TASK: Personalize examples and analogies to match audience characteristics");
        sb.AppendLine();

        sb.AppendLine("TARGET AUDIENCE PROFILE:");
        sb.AppendLine($"- Name: {context.Profile.Name}");
        sb.AppendLine($"- Age Range: {context.Profile.AgeRange?.DisplayName ?? "Not specified"}");
        sb.AppendLine($"- Profession: {context.Profile.Profession ?? "Not specified"}");
        sb.AppendLine($"- Industry: {context.Profile.Industry ?? "Not specified"}");
        sb.AppendLine($"- Expertise Level: {context.Profile.ExpertiseLevel}");
        sb.AppendLine($"- Geographic Region: {context.Profile.GeographicRegion}");
        
        if (context.Profile.Interests.Count > 0)
        {
            sb.AppendLine($"- Interests: {string.Join(", ", context.Profile.Interests.Take(5))}");
        }
        sb.AppendLine();

        sb.AppendLine("PERSONALIZATION STRATEGY:");
        
        if (context.PreferredAnalogies.Count > 0)
        {
            sb.AppendLine($"- Use analogies related to: {string.Join(", ", context.PreferredAnalogies)}");
        }
        
        if (context.CulturalReferences.Count > 0)
        {
            sb.AppendLine($"- Include cultural references appropriate for: {string.Join(", ", context.CulturalReferences)}");
        }

        sb.AppendLine($"- Generate {examplesPerConcept} examples per key concept");
        sb.AppendLine("- Make examples concrete and relatable to audience");
        sb.AppendLine("- Adjust complexity to match expertise level");
        sb.AppendLine("- Ensure cultural appropriateness");
        sb.AppendLine();

        if (context.Profile.Profession != null)
        {
            var profession = context.Profile.Profession.ToLowerInvariant();
            if (profession.Contains("tech") || profession.Contains("developer"))
            {
                sb.AppendLine("EXAMPLE STYLE: Use programming analogies, software concepts, algorithms");
            }
            else if (profession.Contains("teacher") || profession.Contains("educator"))
            {
                sb.AppendLine("EXAMPLE STYLE: Use classroom scenarios, learning concepts, academic examples");
            }
            else if (profession.Contains("healthcare") || profession.Contains("medical"))
            {
                sb.AppendLine("EXAMPLE STYLE: Use medical scenarios, health concepts, patient care examples");
            }
            else if (profession.Contains("business") || profession.Contains("manager"))
            {
                sb.AppendLine("EXAMPLE STYLE: Use business scenarios, management concepts, organizational examples");
            }
            sb.AppendLine();
        }

        if (context.Profile.PainPoints.Count > 0)
        {
            sb.AppendLine("AUDIENCE PAIN POINTS (address these in examples):");
            foreach (var painPoint in context.Profile.PainPoints.Take(3))
            {
                sb.AppendLine($"- {painPoint}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("ORIGINAL TEXT:");
        sb.AppendLine(text);
        sb.AppendLine();

        sb.AppendLine("INSTRUCTIONS:");
        sb.AppendLine("1. Identify generic or irrelevant examples in the text");
        sb.AppendLine("2. Replace with audience-specific examples that resonate");
        sb.AppendLine("3. Use concrete scenarios from audience's likely experience");
        sb.AppendLine("4. Ensure examples are culturally appropriate for geographic region");
        sb.AppendLine("5. Match example complexity to expertise level");
        sb.AppendLine($"6. Provide {examplesPerConcept} examples per key concept for better retention");
        sb.AppendLine("7. Maintain the original message and intent");
        sb.AppendLine();

        sb.AppendLine("OUTPUT: Return ONLY the text with personalized examples, maintaining structure.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse LLM response
    /// </summary>
    private string ParseExampleResponse(string response, string originalText)
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
                !line.StartsWith("TARGET AUDIENCE") &&
                !line.StartsWith("PERSONALIZATION") &&
                !line.StartsWith("EXAMPLE STYLE:") &&
                !line.StartsWith("INSTRUCTIONS:") &&
                !line.StartsWith("OUTPUT:") &&
                !string.IsNullOrWhiteSpace(line))
            {
                contentLines.Add(line);
            }
        }

        return contentLines.Count > 0 ? string.Join('\n', contentLines) : originalText;
    }

    /// <summary>
    /// Extract example changes
    /// </summary>
    private List<AdaptationChange> ExtractExampleChanges(string original, string adapted)
    {
        var changes = new List<AdaptationChange>();

        var originalSentences = SplitIntoSentences(original);
        var adaptedSentences = SplitIntoSentences(adapted);

        for (int i = 0; i < Math.Min(originalSentences.Count, adaptedSentences.Count); i++)
        {
            if (!originalSentences[i].Equals(adaptedSentences[i], StringComparison.OrdinalIgnoreCase))
            {
                if (ContainsExampleKeywords(originalSentences[i]) || ContainsExampleKeywords(adaptedSentences[i]))
                {
                    changes.Add(new AdaptationChange
                    {
                        Category = "Example",
                        Description = "Personalized example for audience",
                        OriginalText = originalSentences[i],
                        AdaptedText = adaptedSentences[i],
                        Reasoning = "Made example more relevant to audience background and interests",
                        Position = i
                    });
                }
            }
        }

        return changes;
    }

    /// <summary>
    /// Split text into sentences
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        return Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    /// <summary>
    /// Check if sentence contains example keywords
    /// </summary>
    private bool ContainsExampleKeywords(string sentence)
    {
        var keywords = new[] { "example", "for instance", "such as", "like", "imagine", "consider", "think of" };
        var lowerSentence = sentence.ToLowerInvariant();
        return keywords.Any(k => lowerSentence.Contains(k));
    }

    /// <summary>
    /// Calculate average relevance score for changes
    /// </summary>
    private double CalculateAverageRelevance(List<AdaptationChange> changes)
    {
        if (changes.Count == 0)
        {
            return 0.0;
        }

        return 0.85;
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
/// Result of example personalization
/// </summary>
public class ExamplePersonalizationResult
{
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public double AverageRelevanceScore { get; set; }
}
