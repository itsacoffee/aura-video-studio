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
/// Adapts content pacing and information density based on audience expertise level
/// Beginners get slower pacing with more explanation, experts get denser content
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class PacingAdapter
{
    private readonly ILogger _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public PacingAdapter(ILogger logger, ILlmProvider llmProvider, LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Adapt pacing to match audience expertise
    /// </summary>
    public async Task<PacingAdaptationResult> AdaptPacingAsync(
        string text,
        AudienceAdaptationContext context,
        CancellationToken cancellationToken)
    {
        var result = new PacingAdaptationResult
        {
            AdaptedText = text,
            PacingMultiplier = context.PacingMultiplier
        };

        try
        {
            _logger.LogInformation("Adapting pacing with multiplier: {Multiplier:F2}", context.PacingMultiplier);

            var prompt = BuildPacingAdaptationPrompt(text, context);

            var response = await GenerateWithLlmAsync(
                new Brief(
                    "Pacing Adaptation",
                    context.Profile.Name,
                    "Adjust content pacing and density for audience",
                    "analytical",
                    "English",
                    Aspect.Vertical9x16,
                    AudienceProfile: context.Profile
                ),
                new PlanSpec(
                    TimeSpan.FromMinutes(1),
                    Pacing.Conversational,
                    Density.Balanced,
                    "analytical"
                ),
                cancellationToken
            );

            result.AdaptedText = ParsePacingResponse(response, text);
            result.Changes = ExtractPacingChanges(text, result.AdaptedText);

            _logger.LogInformation("Pacing adaptation complete. Changes: {ChangeCount}", result.Changes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adapting pacing");
        }

        return result;
    }

    /// <summary>
    /// Build prompt for pacing adaptation
    /// </summary>
    private string BuildPacingAdaptationPrompt(
        string text,
        AudienceAdaptationContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("TASK: Adapt content pacing and information density for audience expertise level");
        sb.AppendLine();

        sb.AppendLine("AUDIENCE PROFILE:");
        sb.AppendLine($"- Name: {context.Profile.Name}");
        sb.AppendLine($"- Expertise Level: {context.Profile.ExpertiseLevel}");
        sb.AppendLine($"- Attention Span: {context.Profile.AttentionSpan?.DisplayName ?? "Medium"}");
        sb.AppendLine($"- Technical Comfort: {context.Profile.TechnicalComfort}");
        sb.AppendLine($"- Pacing Multiplier: {context.PacingMultiplier:F2}x");
        sb.AppendLine();

        sb.AppendLine("PACING ADAPTATION STRATEGY:");

        if (context.Profile.ExpertiseLevel == ExpertiseLevel.CompleteBeginner ||
            context.Profile.ExpertiseLevel == ExpertiseLevel.Novice)
        {
            sb.AppendLine("BEGINNER AUDIENCE - SLOWER, MORE DETAILED:");
            sb.AppendLine("- Add more explanation and context for each concept");
            sb.AppendLine("- Include step-by-step breakdowns");
            sb.AppendLine("- Repeat key points in different ways");
            sb.AppendLine("- Add transition phrases and signposting");
            sb.AppendLine("- Break complex ideas into smaller chunks");
            sb.AppendLine("- Target: 20-30% longer content for same topic");
            sb.AppendLine("- Assume NO prior knowledge");
        }
        else if (context.Profile.ExpertiseLevel == ExpertiseLevel.Expert ||
                 context.Profile.ExpertiseLevel == ExpertiseLevel.Professional)
        {
            sb.AppendLine("EXPERT AUDIENCE - FASTER, MORE DENSE:");
            sb.AppendLine("- Remove basic explanations and assume knowledge");
            sb.AppendLine("- Jump directly to advanced concepts");
            sb.AppendLine("- Increase information density");
            sb.AppendLine("- Remove repetition and redundancy");
            sb.AppendLine("- Use shorthand and assumed context");
            sb.AppendLine("- Target: 20-25% shorter content for same topic");
            sb.AppendLine("- Assume strong foundation");
        }
        else
        {
            sb.AppendLine("INTERMEDIATE AUDIENCE - BALANCED:");
            sb.AppendLine("- Provide moderate explanation");
            sb.AppendLine("- Balance detail with efficiency");
            sb.AppendLine("- Include key context without over-explaining");
            sb.AppendLine("- Use reasonable pacing");
            sb.AppendLine("- Assume basic knowledge, explain intermediate concepts");
        }
        sb.AppendLine();

        if (context.Profile.AttentionSpan != null)
        {
            if (context.Profile.AttentionSpan.PreferredDuration < TimeSpan.FromMinutes(3))
            {
                sb.AppendLine("ATTENTION SPAN: SHORT - Keep sections brief, maintain high engagement");
            }
            else if (context.Profile.AttentionSpan.PreferredDuration > TimeSpan.FromMinutes(10))
            {
                sb.AppendLine("ATTENTION SPAN: LONG - Can use detailed exploration, deeper dives allowed");
            }
            sb.AppendLine();
        }

        sb.AppendLine("ORIGINAL TEXT:");
        sb.AppendLine(text);
        sb.AppendLine();

        sb.AppendLine("INSTRUCTIONS:");
        sb.AppendLine("1. Analyze current information density");
        sb.AppendLine("2. Adjust pacing to match expertise level");
        sb.AppendLine("3. Add or remove explanatory content as needed");
        sb.AppendLine("4. Ensure scene durations match attention span");
        sb.AppendLine("5. Maintain core message and educational value");
        sb.AppendLine("6. Keep natural flow and transitions");
        sb.AppendLine();

        sb.AppendLine("OUTPUT: Return ONLY the adjusted text with appropriate pacing.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse pacing response
    /// </summary>
    private string ParsePacingResponse(string response, string originalText)
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
                !line.StartsWith("PACING ADAPTATION") &&
                !line.StartsWith("BEGINNER AUDIENCE") &&
                !line.StartsWith("EXPERT AUDIENCE") &&
                !line.StartsWith("INTERMEDIATE AUDIENCE") &&
                !line.StartsWith("ATTENTION SPAN:") &&
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
    /// Extract pacing changes
    /// </summary>
    private List<AdaptationChange> ExtractPacingChanges(string original, string adapted)
    {
        var changes = new List<AdaptationChange>();

        var originalWordCount = CountWords(original);
        var adaptedWordCount = CountWords(adapted);

        if (Math.Abs(originalWordCount - adaptedWordCount) > originalWordCount * 0.05)
        {
            changes.Add(new AdaptationChange
            {
                Category = "Pacing",
                Description = adaptedWordCount > originalWordCount 
                    ? $"Added explanation (+{adaptedWordCount - originalWordCount} words)" 
                    : $"Reduced redundancy (-{originalWordCount - adaptedWordCount} words)",
                OriginalText = $"{originalWordCount} words",
                AdaptedText = $"{adaptedWordCount} words",
                Reasoning = "Adjusted information density to match audience expertise level",
                Position = 0
            });
        }

        return changes;
    }

    /// <summary>
    /// Count words in text
    /// </summary>
    private int CountWords(string text)
    {
        return Regex.Split(text, @"\s+").Count(w => !string.IsNullOrWhiteSpace(w));
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
/// Result of pacing adaptation
/// </summary>
public class PacingAdaptationResult
{
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public double PacingMultiplier { get; set; }
}
