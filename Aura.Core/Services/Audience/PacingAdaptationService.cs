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
/// Adapts content pacing and information density based on audience expertise and attention span.
/// Adjusts explanations, repetition, and scene duration to match audience capabilities.
/// </summary>
public class PacingAdaptationService
{
    private readonly ILogger<PacingAdaptationService> _logger;
    private readonly ILlmProvider _llmProvider;

    public PacingAdaptationService(
        ILogger<PacingAdaptationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Adapts content pacing based on audience profile
    /// </summary>
    public async Task<(string AdaptedText, AdaptationChange Change)> AdaptPacingAsync(
        string content,
        AudienceProfile profile,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Adapting pacing for {Expertise} expertise, {Attention}s attention span",
            profile.ExpertiseLevel, profile.AttentionSpanSeconds);

        var pacingStrategy = DeterminePacingStrategy(profile);
        var prompt = BuildPacingAdaptationPrompt(content, profile, pacingStrategy);

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
                "pacing");
            
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            var adaptedText = ExtractAdaptedText(response);

            var change = new AdaptationChange
            {
                Type = AdaptationChangeType.PacingAdjustment,
                OriginalText = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                AdaptedText = adaptedText.Length > 100 ? adaptedText.Substring(0, 100) + "..." : adaptedText,
                Reason = $"Adjusted pacing to {pacingStrategy} for {profile.ExpertiseLevel} audience",
                Position = 0
            };

            return (adaptedText, change);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pacing adaptation failed, returning original content");
            return (content, new AdaptationChange
            {
                Type = AdaptationChangeType.PacingAdjustment,
                OriginalText = "",
                AdaptedText = "",
                Reason = "No pacing adjustment needed",
                Position = 0
            });
        }
    }

    private static string DeterminePacingStrategy(AudienceProfile profile)
    {
        return profile.ExpertiseLevel switch
        {
            ExpertiseLevel.Novice => "slower with detailed explanations",
            ExpertiseLevel.Intermediate => "moderate with balanced detail",
            ExpertiseLevel.Advanced => "brisk with assumed knowledge",
            ExpertiseLevel.Expert => "fast-paced with dense information",
            _ => "moderate"
        };
    }

    private static string BuildPacingAdaptationPrompt(
        string content,
        AudienceProfile profile,
        string pacingStrategy)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert at adapting content pacing and information density.");
        sb.AppendLine();
        sb.AppendLine("TASK: Adjust the pacing and information density to match the audience's capabilities.");
        sb.AppendLine();
        sb.AppendLine("AUDIENCE CHARACTERISTICS:");
        sb.AppendLine($"- Expertise Level: {profile.ExpertiseLevel}");
        sb.AppendLine($"- Attention Span: {profile.AttentionSpanSeconds} seconds");
        sb.AppendLine($"- Learning Style: {profile.LearningStyle}");
        sb.AppendLine($"- Cognitive Load Capacity: {profile.CognitiveLoadCapacity}/100");
        sb.AppendLine();

        sb.AppendLine($"PACING STRATEGY: {pacingStrategy}");
        sb.AppendLine();

        sb.AppendLine("ADAPTATION GUIDELINES:");

        switch (profile.ExpertiseLevel)
        {
            case ExpertiseLevel.Novice:
                sb.AppendLine("- Slow down the pace with more explanation");
                sb.AppendLine("- Break down complex concepts into smaller steps");
                sb.AppendLine("- Add repetition of key points for reinforcement");
                sb.AppendLine("- Include more transitional phrases and context");
                sb.AppendLine("- Provide more foundational information");
                sb.AppendLine("- Expected length increase: 20-30% for same content");
                break;

            case ExpertiseLevel.Intermediate:
                sb.AppendLine("- Maintain moderate pacing with balanced explanations");
                sb.AppendLine("- Provide context but don't over-explain basics");
                sb.AppendLine("- Include some advanced concepts with brief explanations");
                sb.AppendLine("- Balance depth with accessibility");
                break;

            case ExpertiseLevel.Advanced:
                sb.AppendLine("- Increase pace by reducing basic explanations");
                sb.AppendLine("- Assume foundational knowledge");
                sb.AppendLine("- Focus on advanced concepts and nuances");
                sb.AppendLine("- Reduce repetition and transitional content");
                break;

            case ExpertiseLevel.Expert:
                sb.AppendLine("- Maximize information density");
                sb.AppendLine("- Assume extensive domain knowledge");
                sb.AppendLine("- Remove explanatory content for basic concepts");
                sb.AppendLine("- Focus on cutting-edge insights and implications");
                sb.AppendLine("- Expected length reduction: 20-30% for same information");
                break;
        }

        if (profile.AttentionSpanSeconds < 120)
        {
            sb.AppendLine("- Keep segments very concise due to short attention span");
            sb.AppendLine("- Use more frequent engagement hooks");
        }
        else if (profile.AttentionSpanSeconds > 300)
        {
            sb.AppendLine("- Can develop ideas more thoroughly");
            sb.AppendLine("- Audience can handle longer segments");
        }

        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Preserve all key information and insights");
        sb.AppendLine("- Maintain logical flow and structure");
        sb.AppendLine("- Adjust explanatory depth appropriately");
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
