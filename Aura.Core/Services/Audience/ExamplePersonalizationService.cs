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
/// Personalizes examples and analogies to resonate with specific audience characteristics.
/// Generates culturally relevant, domain-specific examples that match audience interests.
/// </summary>
public class ExamplePersonalizationService
{
    private readonly ILogger<ExamplePersonalizationService> _logger;
    private readonly ILlmProvider _llmProvider;

    public ExamplePersonalizationService(
        ILogger<ExamplePersonalizationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Personalizes examples and analogies based on audience profile
    /// </summary>
    public async Task<(string AdaptedText, AdaptationChange Change)> PersonalizeExamplesAsync(
        string content,
        AudienceProfile profile,
        int minExamples,
        int maxExamples,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Personalizing examples for {Domain} audience in {Region}",
            profile.ProfessionalDomain ?? "general", profile.GeographicRegion ?? "global");

        var prompt = BuildExamplePersonalizationPrompt(content, profile, minExamples, maxExamples);

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
                "personalization");
            
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            var adaptedText = ExtractAdaptedText(response);

            var change = new AdaptationChange
            {
                Type = AdaptationChangeType.ExamplePersonalization,
                OriginalText = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                AdaptedText = adaptedText.Length > 100 ? adaptedText.Substring(0, 100) + "..." : adaptedText,
                Reason = BuildChangeReason(profile),
                Position = 0
            };

            return (adaptedText, change);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Example personalization failed, returning original content");
            return (content, new AdaptationChange
            {
                Type = AdaptationChangeType.ExamplePersonalization,
                OriginalText = "",
                AdaptedText = "",
                Reason = "No personalization needed",
                Position = 0
            });
        }
    }

    private static string BuildExamplePersonalizationPrompt(
        string content,
        AudienceProfile profile,
        int minExamples,
        int maxExamples)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert at creating audience-specific examples and analogies.");
        sb.AppendLine();
        sb.AppendLine("TASK: Enhance the content with personalized examples that resonate with the target audience.");
        sb.AppendLine();
        sb.AppendLine("TARGET AUDIENCE CHARACTERISTICS:");
        sb.AppendLine($"- Professional Domain: {profile.ProfessionalDomain ?? "General"}");
        sb.AppendLine($"- Age Range: {profile.AgeRange}");
        sb.AppendLine($"- Geographic Region: {profile.GeographicRegion ?? "Global"}");
        sb.AppendLine($"- Expertise Level: {profile.ExpertiseLevel}");
        
        if (profile.Interests.Count > 0)
        {
            sb.AppendLine($"- Key Interests: {string.Join(", ", profile.Interests)}");
        }

        sb.AppendLine();
        sb.AppendLine("PERSONALIZATION GUIDELINES:");
        
        if (!string.IsNullOrEmpty(profile.ProfessionalDomain))
        {
            sb.AppendLine($"- Use {profile.ProfessionalDomain}-specific examples and scenarios");
            sb.AppendLine($"- Draw analogies from {profile.ProfessionalDomain} contexts");
        }

        if (!string.IsNullOrEmpty(profile.GeographicRegion))
        {
            sb.AppendLine($"- Use culturally relevant references for {profile.GeographicRegion}");
            sb.AppendLine($"- Consider local contexts and customs");
        }

        sb.AppendLine($"- Match complexity to {profile.ExpertiseLevel} expertise level");
        sb.AppendLine($"- Include {minExamples}-{maxExamples} examples per key concept");
        sb.AppendLine("- Make examples concrete, relatable, and memorable");
        sb.AppendLine("- Use real-world scenarios the audience encounters");
        
        if (profile.AgeRange == AgeRange.Teen13to17 || profile.AgeRange == AgeRange.YoungAdult18to24)
        {
            sb.AppendLine("- Use modern, current references and trends");
            sb.AppendLine("- Include digital/social media contexts where appropriate");
        }
        else if (profile.AgeRange == AgeRange.Senior55Plus)
        {
            sb.AppendLine("- Use timeless, universal examples");
            sb.AppendLine("- Avoid overly trendy or ephemeral references");
        }

        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Replace generic examples with personalized ones");
        sb.AppendLine("- Ensure examples directly support the main concepts");
        sb.AppendLine("- Maintain natural flow and integration");
        sb.AppendLine("- Keep the same core message and structure");
        sb.AppendLine("- Return ONLY the enhanced content, no explanations");
        sb.AppendLine();
        sb.AppendLine("CONTENT TO ENHANCE:");
        sb.AppendLine(content);

        return sb.ToString();
    }

    private static string BuildChangeReason(AudienceProfile profile)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrEmpty(profile.ProfessionalDomain))
        {
            parts.Add($"{profile.ProfessionalDomain}-specific examples");
        }

        if (!string.IsNullOrEmpty(profile.GeographicRegion))
        {
            parts.Add($"{profile.GeographicRegion} cultural context");
        }

        parts.Add($"{profile.ExpertiseLevel} expertise level");

        return $"Personalized examples for {string.Join(", ", parts)}";
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
