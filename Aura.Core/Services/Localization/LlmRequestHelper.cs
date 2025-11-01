using System;
using Aura.Core.Models;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Helper class for creating standardized Brief and PlanSpec objects for LLM requests
/// Reduces duplication across localization services
/// </summary>
internal static class LlmRequestHelper
{
    /// <summary>
    /// Create a standard Brief for analysis tasks
    /// </summary>
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
    /// Create a standard PlanSpec for analysis tasks
    /// </summary>
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
    /// Create a standard Brief for translation tasks
    /// </summary>
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
    /// Create a standard PlanSpec for translation tasks
    /// </summary>
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
