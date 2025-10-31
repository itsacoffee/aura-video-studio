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
/// Balances cognitive load to match audience capacity.
/// Manages complexity curves, ensures appropriate mental effort per scene,
/// and inserts breather moments for dense content.
/// </summary>
public class CognitiveLoadBalancer
{
    private readonly ILogger<CognitiveLoadBalancer> _logger;
    private readonly ILlmProvider _llmProvider;

    public CognitiveLoadBalancer(
        ILogger<CognitiveLoadBalancer> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Balances cognitive load based on audience capacity
    /// </summary>
    public async Task<(string AdaptedText, AdaptationChange Change)> BalanceLoadAsync(
        string content,
        AudienceProfile profile,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Balancing cognitive load for capacity {Capacity}/100",
            profile.CognitiveLoadCapacity);

        var prompt = BuildLoadBalancingPrompt(content, profile);

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
                "cognitive");
            
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            var adaptedText = ExtractAdaptedText(response);

            var change = new AdaptationChange
            {
                Type = AdaptationChangeType.ComplexityReduction,
                OriginalText = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                AdaptedText = adaptedText.Length > 100 ? adaptedText.Substring(0, 100) + "..." : adaptedText,
                Reason = $"Balanced cognitive load to match capacity {profile.CognitiveLoadCapacity}/100",
                Position = 0
            };

            return (adaptedText, change);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cognitive load balancing failed, returning original content");
            return (content, new AdaptationChange
            {
                Type = AdaptationChangeType.ComplexityReduction,
                OriginalText = "",
                AdaptedText = "",
                Reason = "No load balancing needed",
                Position = 0
            });
        }
    }

    private static string BuildLoadBalancingPrompt(
        string content,
        AudienceProfile profile)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert at managing cognitive load and learning complexity.");
        sb.AppendLine();
        sb.AppendLine("TASK: Adjust the content to match the audience's cognitive load capacity.");
        sb.AppendLine();
        sb.AppendLine("AUDIENCE COGNITIVE PROFILE:");
        sb.AppendLine($"- Cognitive Load Capacity: {profile.CognitiveLoadCapacity}/100");
        sb.AppendLine($"- Expertise Level: {profile.ExpertiseLevel}");
        sb.AppendLine($"- Learning Style: {profile.LearningStyle}");
        sb.AppendLine($"- Education Level: {profile.EducationLevel}");
        sb.AppendLine();

        sb.AppendLine("COGNITIVE LOAD PRINCIPLES:");
        sb.AppendLine("- Working memory can hold 5-9 chunks of information");
        sb.AppendLine("- Complex concepts require more cognitive resources");
        sb.AppendLine("- Abstract ideas are harder to process than concrete examples");
        sb.AppendLine("- Novel information requires more effort than familiar concepts");
        sb.AppendLine("- Multiple simultaneous concepts increase load");
        sb.AppendLine();

        if (profile.CognitiveLoadCapacity < 50)
        {
            sb.AppendLine("LOAD REDUCTION STRATEGIES (Low Capacity):");
            sb.AppendLine("- Break complex ideas into smallest possible chunks");
            sb.AppendLine("- Present only one new concept at a time");
            sb.AppendLine("- Use maximum concrete examples and analogies");
            sb.AppendLine("- Add frequent review and reinforcement");
            sb.AppendLine("- Insert regular 'breather' moments to process");
            sb.AppendLine("- Use scaffolding: build new ideas on familiar foundations");
            sb.AppendLine("- Minimize abstract or theoretical content");
            sb.AppendLine("- Repeat key points in different ways");
        }
        else if (profile.CognitiveLoadCapacity < 70)
        {
            sb.AppendLine("LOAD MANAGEMENT STRATEGIES (Moderate Capacity):");
            sb.AppendLine("- Present 2-3 related concepts per section");
            sb.AppendLine("- Balance abstract and concrete content");
            sb.AppendLine("- Provide examples for complex ideas");
            sb.AppendLine("- Include periodic summaries");
            sb.AppendLine("- Allow processing time between dense sections");
            sb.AppendLine("- Build complexity gradually");
        }
        else
        {
            sb.AppendLine("LOAD OPTIMIZATION STRATEGIES (High Capacity):");
            sb.AppendLine("- Can present multiple related concepts simultaneously");
            sb.AppendLine("- Abstract reasoning is accessible");
            sb.AppendLine("- Fewer examples needed");
            sb.AppendLine("- Can maintain higher information density");
            sb.AppendLine("- Brief pauses sufficient between sections");
            sb.AppendLine("- Can handle complexity curves with steeper slopes");
        }

        sb.AppendLine();
        sb.AppendLine("COMPLEXITY CURVE MANAGEMENT:");
        sb.AppendLine("- Start with accessible concepts to build confidence");
        sb.AppendLine("- Build complexity gradually, not abruptly");
        sb.AppendLine("- Peak complexity should not exceed audience capacity");
        sb.AppendLine("- Include valleys (easier sections) after peaks");
        sb.AppendLine("- End on a consolidating note, not peak complexity");
        sb.AppendLine();

        sb.AppendLine("BREATHER MOMENTS:");
        sb.AppendLine("- After dense/complex sections, insert lighter content");
        sb.AppendLine("- Use concrete examples as cognitive rest stops");
        sb.AppendLine("- Stories and narratives provide processing time");
        sb.AppendLine("- Summaries help consolidate before moving on");
        sb.AppendLine();

        if (profile.LearningStyle == LearningStyle.Visual)
        {
            sb.AppendLine("VISUAL LEARNER ADJUSTMENTS:");
            sb.AppendLine("- Suggest visual moments [VISUAL: description]");
            sb.AppendLine("- Use spatial and visual language");
            sb.AppendLine("- Describe mental models and diagrams");
        }
        else if (profile.LearningStyle == LearningStyle.Detail)
        {
            sb.AppendLine("DETAIL-ORIENTED ADJUSTMENTS:");
            sb.AppendLine("- Provide systematic, step-by-step progressions");
            sb.AppendLine("- Include specific details and precision");
            sb.AppendLine("- Logical sequencing is critical");
        }

        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Preserve all essential information");
        sb.AppendLine("- Maintain logical flow and structure");
        sb.AppendLine("- Balance challenge and accessibility");
        sb.AppendLine("- Ensure no section exceeds capacity");
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
