using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Adjusts vocabulary complexity to match audience education and expertise level.
/// Uses LLM to intelligently simplify or technicalize language while preserving meaning.
/// </summary>
public class VocabularyLevelAdjuster
{
    private readonly ILogger<VocabularyLevelAdjuster> _logger;
    private readonly ILlmProvider _llmProvider;

    public VocabularyLevelAdjuster(
        ILogger<VocabularyLevelAdjuster> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Adjusts vocabulary level to match audience profile
    /// </summary>
    public async Task<(string AdaptedText, AdaptationChange Change)> AdjustVocabularyAsync(
        string content,
        AudienceProfile profile,
        bool addDefinitions,
        AdaptationAggressiveness aggressiveness,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Adjusting vocabulary for {Education} level, {Expertise} expertise",
            profile.EducationLevel, profile.ExpertiseLevel);

        var targetReadingLevel = GetTargetReadingLevel(profile);
        var shouldSimplify = !profile.PrefersTechnicalLanguage && 
                           (profile.EducationLevel == EducationLevel.HighSchool || 
                            profile.ExpertiseLevel == ExpertiseLevel.Novice);

        var prompt = BuildVocabularyAdjustmentPrompt(
            content,
            profile,
            targetReadingLevel,
            shouldSimplify,
            addDefinitions,
            aggressiveness);

        try
        {
            var brief = new Brief(
                prompt,
                null,
                null,
                "informative",
                "English",
                Aspect.Wide);
            
            var planSpec = new PlanSpec(
                TimeSpan.FromMinutes(1),
                Pacing.Conversational,
                Density.Balanced,
                "adaptation");
            
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            var adaptedText = ExtractAdaptedText(response);

            var changeType = shouldSimplify ? 
                AdaptationChangeType.VocabularySimplification : 
                AdaptationChangeType.VocabularyTechnification;

            var change = new AdaptationChange
            {
                Type = changeType,
                OriginalText = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                AdaptedText = adaptedText.Length > 100 ? adaptedText.Substring(0, 100) + "..." : adaptedText,
                Reason = $"Adjusted vocabulary to {targetReadingLevel} grade level for {profile.EducationLevel} audience",
                Position = 0
            };

            return (adaptedText, change);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vocabulary adjustment failed, returning original content");
            return (content, new AdaptationChange
            {
                Type = AdaptationChangeType.VocabularySimplification,
                OriginalText = "",
                AdaptedText = "",
                Reason = "No adjustment needed",
                Position = 0
            });
        }
    }

    private static string GetTargetReadingLevel(AudienceProfile profile)
    {
        return profile.EducationLevel switch
        {
            EducationLevel.HighSchool => profile.ExpertiseLevel == ExpertiseLevel.Advanced ? "10th" : "8th-9th",
            EducationLevel.Undergraduate => profile.ExpertiseLevel >= ExpertiseLevel.Advanced ? "14th" : "12th-13th",
            EducationLevel.Graduate => "16th+",
            EducationLevel.Expert => "18th+ (professional/academic)",
            _ => "12th"
        };
    }

    private static string BuildVocabularyAdjustmentPrompt(
        string content,
        AudienceProfile profile,
        string targetReadingLevel,
        bool shouldSimplify,
        bool addDefinitions,
        AdaptationAggressiveness aggressiveness)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert content adapter specializing in adjusting vocabulary complexity.");
        sb.AppendLine();
        sb.AppendLine("TASK: Adjust the vocabulary in the following content to match the target audience.");
        sb.AppendLine();
        sb.AppendLine("TARGET AUDIENCE:");
        sb.AppendLine($"- Education Level: {profile.EducationLevel}");
        sb.AppendLine($"- Expertise Level: {profile.ExpertiseLevel}");
        sb.AppendLine($"- Target Reading Level: {targetReadingLevel} grade");
        sb.AppendLine($"- Prefers Technical Language: {profile.PrefersTechnicalLanguage}");
        sb.AppendLine();

        sb.AppendLine("ADAPTATION STYLE:");
        sb.AppendLine(aggressiveness switch
        {
            AdaptationAggressiveness.Subtle => "- Make minimal changes, preserve original style where possible",
            AdaptationAggressiveness.Moderate => "- Balance between audience fit and original style",
            AdaptationAggressiveness.Aggressive => "- Prioritize perfect audience fit over original style",
            _ => "- Use balanced approach"
        });
        sb.AppendLine();

        if (shouldSimplify)
        {
            sb.AppendLine("SIMPLIFICATION GUIDELINES:");
            sb.AppendLine("- Replace complex/technical terms with simpler alternatives");
            sb.AppendLine("- Break down jargon into plain language");
            sb.AppendLine("- Use shorter, clearer sentence structures");
            sb.AppendLine("- Prefer common words over rare or specialized vocabulary");
            if (addDefinitions)
            {
                sb.AppendLine("- Add brief definitions for unavoidable technical terms in parentheses");
            }
        }
        else
        {
            sb.AppendLine("TECHNICALIZATION GUIDELINES:");
            sb.AppendLine("- Use field-specific terminology where appropriate");
            sb.AppendLine("- Assume domain knowledge and use precise technical language");
            sb.AppendLine("- Replace general terms with more specific technical equivalents");
            sb.AppendLine("- Maintain professional/academic vocabulary level");
        }

        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Preserve the core meaning and message");
        sb.AppendLine("- Maintain the natural flow and readability");
        sb.AppendLine("- Keep the same structure and organization");
        sb.AppendLine("- Return ONLY the adapted content, no explanations");
        sb.AppendLine();
        sb.AppendLine("CONTENT TO ADAPT:");
        sb.AppendLine(content);

        return sb.ToString();
    }

    private static string ExtractAdaptedText(string llmResponse)
    {
        var response = llmResponse.Trim();
        
        if (response.StartsWith("```", StringComparison.Ordinal))
        {
            var lines = response.Split('\n');
            var contentLines = new System.Collections.Generic.List<string>();
            var inCodeBlock = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }
                if (inCodeBlock || !line.StartsWith("```", StringComparison.Ordinal))
                {
                    contentLines.Add(line);
                }
            }

            return string.Join("\n", contentLines).Trim();
        }

        return response;
    }
}
