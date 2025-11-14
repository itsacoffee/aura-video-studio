using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audience;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Adjusts vocabulary complexity to match audience education and expertise level
/// Uses LLM to analyze and replace complex terms with simpler alternatives
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class VocabularyLevelAdjuster
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public VocabularyLevelAdjuster(ILogger logger, ILlmProvider llmProvider, LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Adjust vocabulary level to match audience
    /// </summary>
    public async Task<VocabularyAdjustmentResult> AdjustVocabularyAsync(
        string text,
        AudienceAdaptationContext context,
        double aggressiveness,
        CancellationToken cancellationToken)
    {
        var result = new VocabularyAdjustmentResult
        {
            AdaptedText = text
        };

        try
        {
            var prompt = BuildVocabularyAdjustmentPrompt(text, context, aggressiveness);
            
            var response = await GenerateWithLlmAsync(
                new Models.Brief(
                    "Vocabulary Adjustment",
                    context.Profile.Name,
                    "Adjust vocabulary complexity to match audience level",
                    "analytical",
                    "English",
                    Models.Aspect.Vertical9x16,
                    AudienceProfile: context.Profile
                ),
                new Models.PlanSpec(
                    TimeSpan.FromMinutes(1),
                    Models.Pacing.Conversational,
                    Models.Density.Balanced,
                    "analytical"
                ),
                cancellationToken
            ).ConfigureAwait(false);

            result.AdaptedText = ParseVocabularyResponse(response, text);
            result.Changes = ExtractVocabularyChanges(text, result.AdaptedText);

            _logger.LogInformation("Vocabulary adjustment complete. Changes: {ChangeCount}", result.Changes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting vocabulary");
        }

        return result;
    }

    /// <summary>
    /// Build prompt for vocabulary adjustment
    /// </summary>
    private string BuildVocabularyAdjustmentPrompt(
        string text,
        AudienceAdaptationContext context,
        double aggressiveness)
    {
        var sb = new StringBuilder();

        sb.AppendLine("TASK: Adjust vocabulary complexity to match audience level");
        sb.AppendLine();
        sb.AppendLine($"TARGET AUDIENCE:");
        sb.AppendLine($"- Name: {context.Profile.Name}");
        sb.AppendLine($"- Education Level: {context.Profile.EducationLevel}");
        sb.AppendLine($"- Expertise Level: {context.Profile.ExpertiseLevel}");
        sb.AppendLine($"- Technical Comfort: {context.Profile.TechnicalComfort}");
        sb.AppendLine($"- Target Reading Level: Grade {context.TargetReadingLevel:F1}");
        sb.AppendLine();

        sb.AppendLine($"ADJUSTMENT STRATEGY:");
        if (context.Profile.ExpertiseLevel == ExpertiseLevel.Expert || 
            context.Profile.ExpertiseLevel == ExpertiseLevel.Professional)
        {
            sb.AppendLine("- Embrace technical terminology freely");
            sb.AppendLine("- Use field-specific jargon appropriately");
            sb.AppendLine("- Assume advanced knowledge");
            sb.AppendLine("- Keep precise technical terms");
        }
        else if (context.Profile.ExpertiseLevel == ExpertiseLevel.CompleteBeginner ||
                 context.Profile.ExpertiseLevel == ExpertiseLevel.Novice)
        {
            sb.AppendLine("- Replace jargon with plain language");
            sb.AppendLine("- Add brief definitions for necessary technical terms");
            sb.AppendLine("- Use everyday vocabulary");
            sb.AppendLine("- Explain complex concepts simply");
        }
        else
        {
            sb.AppendLine("- Balance technical accuracy with clarity");
            sb.AppendLine("- Introduce technical terms with context");
            sb.AppendLine("- Use accessible analogies");
            sb.AppendLine("- Maintain professional tone while being clear");
        }
        sb.AppendLine();

        if (context.Profile.AccessibilityNeeds?.RequiresSimplifiedLanguage == true)
        {
            sb.AppendLine("ACCESSIBILITY REQUIREMENT: Use simplified language");
            sb.AppendLine("- Short sentences (10-15 words)");
            sb.AppendLine("- Common vocabulary only");
            sb.AppendLine("- Active voice");
            sb.AppendLine("- Clear structure");
            sb.AppendLine();
        }

        sb.AppendLine($"AGGRESSIVENESS: {aggressiveness:P0} (0% = minimal changes, 100% = maximum adaptation)");
        sb.AppendLine();

        sb.AppendLine("ORIGINAL TEXT:");
        sb.AppendLine(text);
        sb.AppendLine();

        sb.AppendLine("INSTRUCTIONS:");
        sb.AppendLine("1. Analyze the text for vocabulary complexity");
        sb.AppendLine("2. Identify words/phrases that don't match the target reading level");
        sb.AppendLine("3. Replace complex terms with appropriate alternatives");
        sb.AppendLine("4. Add definitions where appropriate for the audience level");
        sb.AppendLine("5. Maintain the original meaning and intent");
        sb.AppendLine("6. Preserve the text structure and flow");
        sb.AppendLine();

        sb.AppendLine("OUTPUT: Return ONLY the adjusted text, maintaining the same structure as the original.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse LLM response for adjusted text
    /// </summary>
    private string ParseVocabularyResponse(string response, string originalText)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return originalText;
        }

        var lines = response.Split('\n');
        var adjustedLines = new List<string>();

        foreach (var line in lines)
        {
            if (!line.StartsWith("TASK:") && 
                !line.StartsWith("TARGET AUDIENCE:") &&
                !line.StartsWith("ADJUSTMENT STRATEGY:") &&
                !line.StartsWith("INSTRUCTIONS:") &&
                !line.StartsWith("OUTPUT:") &&
                !string.IsNullOrWhiteSpace(line))
            {
                adjustedLines.Add(line);
            }
        }

        return adjustedLines.Count > 0 ? string.Join('\n', adjustedLines) : originalText;
    }

    /// <summary>
    /// Extract vocabulary changes made
    /// </summary>
    private List<AdaptationChange> ExtractVocabularyChanges(string original, string adapted)
    {
        var changes = new List<AdaptationChange>();

        var originalWords = Regex.Split(original, @"\b").Where(w => Regex.IsMatch(w, @"\w+")).ToList();
        var adaptedWords = Regex.Split(adapted, @"\b").Where(w => Regex.IsMatch(w, @"\w+")).ToList();

        for (int i = 0; i < Math.Min(originalWords.Count, adaptedWords.Count); i++)
        {
            if (!originalWords[i].Equals(adaptedWords[i], StringComparison.OrdinalIgnoreCase))
            {
                changes.Add(new AdaptationChange
                {
                    Category = "Vocabulary",
                    Description = $"Replaced '{originalWords[i]}' with '{adaptedWords[i]}'",
                    OriginalText = originalWords[i],
                    AdaptedText = adaptedWords[i],
                    Reasoning = "Adjusted to match target reading level",
                    Position = i
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Models.Brief brief,
        Models.PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data != null) return result.Data;
            _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
        }
        return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Result of vocabulary adjustment
/// </summary>
public class VocabularyAdjustmentResult
{
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
}
