using System;
using Aura.Core.Models;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Helper class for creating standardized Brief and PlanSpec objects for LLM requests.
/// 
/// IMPORTANT: For localization services, prefer using ILlmProvider.CompleteAsync() directly
/// with a well-constructed prompt string instead of DraftScriptAsync(), as translation
/// and analysis tasks require specific prompts to be passed to the LLM.
/// 
/// The methods in this class are provided for backwards compatibility but should be
/// considered deprecated for new localization service implementations.
/// </summary>
internal static class LlmRequestHelper
{
    /// <summary>
    /// Create a standard Brief for analysis tasks.
    /// DEPRECATED: Prefer using ILlmProvider.CompleteAsync() with a direct prompt.
    /// </summary>
    [Obsolete("Prefer using ILlmProvider.CompleteAsync() with a direct prompt for localization tasks")]
    public static Brief CreateAnalysisBrief(string topic, string audience, string goal, string tone = "Analytical")
    {
        return new Brief(
            Topic: topic,
            Audience: audience,
            Goal: goal,
            Tone: tone,
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
    }

    /// <summary>
    /// Create a standard PlanSpec for analysis tasks.
    /// DEPRECATED: Prefer using ILlmProvider.CompleteAsync() with a direct prompt.
    /// </summary>
    [Obsolete("Prefer using ILlmProvider.CompleteAsync() with a direct prompt for localization tasks")]
    public static PlanSpec CreateAnalysisPlanSpec(string style = "Analysis")
    {
        return new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: style
        );
    }

    /// <summary>
    /// Create a standard Brief for translation tasks.
    /// DEPRECATED: Prefer using ILlmProvider.CompleteAsync() with a direct prompt.
    /// </summary>
    [Obsolete("Prefer using ILlmProvider.CompleteAsync() with a direct prompt for translation tasks")]
    public static Brief CreateTranslationBrief(string topic, string audience, string goal)
    {
        return new Brief(
            Topic: topic,
            Audience: audience,
            Goal: goal,
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
    }

    /// <summary>
    /// Create a standard PlanSpec for translation tasks.
    /// DEPRECATED: Prefer using ILlmProvider.CompleteAsync() with a direct prompt.
    /// </summary>
    [Obsolete("Prefer using ILlmProvider.CompleteAsync() with a direct prompt for translation tasks")]
    public static PlanSpec CreateTranslationPlanSpec()
    {
        return new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.0),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Translation"
        );
    }
}
