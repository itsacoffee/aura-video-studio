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
/// Optimizes tone and formality level to match audience expectations and preferences.
/// Adjusts energy level, humor style, and cultural appropriateness.
/// </summary>
public class ToneAndFormalityOptimizer
{
    private readonly ILogger<ToneAndFormalityOptimizer> _logger;
    private readonly ILlmProvider _llmProvider;

    public ToneAndFormalityOptimizer(
        ILogger<ToneAndFormalityOptimizer> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Optimizes tone and formality based on audience profile
    /// </summary>
    public async Task<(string AdaptedText, AdaptationChange Change)> OptimizeToneAsync(
        string content,
        AudienceProfile profile,
        AdaptationAggressiveness aggressiveness,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Optimizing tone for {Formality} formality, {Energy} energy, {Age} age group",
            profile.FormalityLevel, profile.EnergyLevel, profile.AgeRange);

        var prompt = BuildToneOptimizationPrompt(content, profile, aggressiveness);

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
                "tone");
            
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            var adaptedText = ExtractAdaptedText(response);

            var change = new AdaptationChange
            {
                Type = AdaptationChangeType.ToneAdjustment,
                OriginalText = content.Length > 100 ? content.Substring(0, 100) + "..." : content,
                AdaptedText = adaptedText.Length > 100 ? adaptedText.Substring(0, 100) + "..." : adaptedText,
                Reason = $"Adjusted to {profile.FormalityLevel} formality with {profile.EnergyLevel} energy for {profile.AgeRange}",
                Position = 0
            };

            return (adaptedText, change);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tone optimization failed, returning original content");
            return (content, new AdaptationChange
            {
                Type = AdaptationChangeType.ToneAdjustment,
                OriginalText = "",
                AdaptedText = "",
                Reason = "No tone adjustment needed",
                Position = 0
            });
        }
    }

    private static string BuildToneOptimizationPrompt(
        string content,
        AudienceProfile profile,
        AdaptationAggressiveness aggressiveness)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are an expert at adapting content tone and formality to match audience expectations.");
        sb.AppendLine();
        sb.AppendLine("TASK: Adjust the tone and formality of the content to match the target audience.");
        sb.AppendLine();
        sb.AppendLine("AUDIENCE CHARACTERISTICS:");
        sb.AppendLine($"- Formality Level: {profile.FormalityLevel}");
        sb.AppendLine($"- Energy Level: {profile.EnergyLevel}");
        sb.AppendLine($"- Age Range: {profile.AgeRange}");
        sb.AppendLine($"- Geographic Region: {profile.GeographicRegion ?? "Global"}");
        
        if (profile.CulturalConsiderations.Count > 0)
        {
            sb.AppendLine($"- Cultural Considerations: {string.Join(", ", profile.CulturalConsiderations)}");
        }

        sb.AppendLine();
        sb.AppendLine($"ADAPTATION INTENSITY: {aggressiveness}");
        sb.AppendLine();

        sb.AppendLine("FORMALITY GUIDELINES:");
        switch (profile.FormalityLevel)
        {
            case FormalityLevel.Casual:
                sb.AppendLine("- Use informal, friendly language");
                sb.AppendLine("- Contractions are encouraged (you're, we'll, don't)");
                sb.AppendLine("- Conversational phrases and colloquialisms OK");
                sb.AppendLine("- Personal pronouns (you, we, I) preferred");
                sb.AppendLine("- Light humor and relatable references");
                break;

            case FormalityLevel.Conversational:
                sb.AppendLine("- Professional but approachable tone");
                sb.AppendLine("- Some contractions acceptable");
                sb.AppendLine("- Balance between friendly and professional");
                sb.AppendLine("- Direct address (you, we) is fine");
                sb.AppendLine("- Occasional humor appropriate");
                break;

            case FormalityLevel.Professional:
                sb.AppendLine("- Formal, businesslike language");
                sb.AppendLine("- Limited or no contractions");
                sb.AppendLine("- Avoid slang and colloquialisms");
                sb.AppendLine("- Maintain respectful distance");
                sb.AppendLine("- Humor should be subtle and professional");
                break;

            case FormalityLevel.Academic:
                sb.AppendLine("- Scholarly, precise language");
                sb.AppendLine("- No contractions");
                sb.AppendLine("- Technical terminology expected");
                sb.AppendLine("- Objective, impersonal tone");
                sb.AppendLine("- No humor or colloquial expressions");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("ENERGY LEVEL GUIDELINES:");
        switch (profile.EnergyLevel)
        {
            case EnergyLevel.Low:
                sb.AppendLine("- Calm, measured delivery");
                sb.AppendLine("- Thoughtful, reflective tone");
                sb.AppendLine("- Avoid exclamation points and emphatic language");
                sb.AppendLine("- Steady, contemplative pacing");
                break;

            case EnergyLevel.Medium:
                sb.AppendLine("- Balanced, engaging energy");
                sb.AppendLine("- Mix of enthusiasm and thoughtfulness");
                sb.AppendLine("- Moderate use of emphasis");
                sb.AppendLine("- Varied but controlled pacing");
                break;

            case EnergyLevel.High:
                sb.AppendLine("- Enthusiastic, dynamic delivery");
                sb.AppendLine("- Energetic language and expressions");
                sb.AppendLine("- Exclamation points for emphasis appropriate");
                sb.AppendLine("- Fast-paced, exciting tone");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("AGE-APPROPRIATE ADJUSTMENTS:");
        switch (profile.AgeRange)
        {
            case AgeRange.Teen13to17:
                sb.AppendLine("- Use current, relatable language");
                sb.AppendLine("- References to modern culture appropriate");
                sb.AppendLine("- Avoid condescension");
                sb.AppendLine("- High energy, dynamic style");
                break;

            case AgeRange.YoungAdult18to24:
                sb.AppendLine("- Contemporary references welcome");
                sb.AppendLine("- Balance between youthful and mature");
                sb.AppendLine("- Digital/social media context OK");
                sb.AppendLine("- Energetic but not juvenile");
                break;

            case AgeRange.Adult25to34:
            case AgeRange.Adult35to44:
                sb.AppendLine("- Professional yet engaging");
                sb.AppendLine("- Universal references over generational");
                sb.AppendLine("- Balance career and life contexts");
                break;

            case AgeRange.Adult45to54:
            case AgeRange.Senior55Plus:
                sb.AppendLine("- Respectful, mature tone");
                sb.AppendLine("- Timeless references preferred");
                sb.AppendLine("- Avoid age-specific stereotypes");
                sb.AppendLine("- Clear, dignified delivery");
                break;
        }

        if (!string.IsNullOrEmpty(profile.GeographicRegion))
        {
            sb.AppendLine();
            sb.AppendLine($"CULTURAL CONSIDERATIONS for {profile.GeographicRegion}:");
            sb.AppendLine("- Use culturally appropriate expressions");
            sb.AppendLine("- Avoid region-specific humor that won't translate");
            sb.AppendLine("- Consider cultural values and norms");
        }

        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Maintain the core message and information");
        sb.AppendLine("- Preserve factual accuracy");
        sb.AppendLine("- Ensure consistency throughout");
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
