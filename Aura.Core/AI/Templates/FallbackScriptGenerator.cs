using System;
using Aura.Core.Models;
using Aura.Core.Templates;

namespace Aura.Core.AI.Templates;

/// <summary>
/// Generates template-based scripts as fallback when LLM generation fails
/// Provides reliable, structured scripts for all video types
/// </summary>
public class FallbackScriptGenerator
{
    /// <summary>
    /// Generates a template-based script for the given brief and spec
    /// </summary>
    public string Generate(Brief brief, PlanSpec spec)
    {
        var videoType = DetermineVideoType(brief);
        var targetWordCount = CalculateTargetWordCount(spec);

        // Use existing ScriptTemplates for most types
        var template = videoType switch
        {
            VideoType.Educational => ScriptTemplates.GenerateFromTemplate(
                Aura.Core.Templates.VideoType.Educational, brief.Topic, targetWordCount),
            VideoType.Tutorial => ScriptTemplates.GenerateFromTemplate(
                Aura.Core.Templates.VideoType.Tutorial, brief.Topic, targetWordCount),
            VideoType.Marketing => ScriptTemplates.GenerateFromTemplate(
                Aura.Core.Templates.VideoType.Marketing, brief.Topic, targetWordCount),
            VideoType.Entertainment => GenerateEntertainmentTemplate(brief, spec),
            _ => ScriptTemplates.GenerateFromTemplate(
                Aura.Core.Templates.VideoType.General, brief.Topic, targetWordCount)
        };

        return template;
    }

    /// <summary>
    /// Determines video type from brief
    /// </summary>
    private VideoType DetermineVideoType(Brief brief)
    {
        var topicLower = brief.Topic.ToLowerInvariant();
        var toneLower = brief.Tone?.ToLowerInvariant() ?? "";

        // Check for entertainment indicators
        if (topicLower.Contains("story") || topicLower.Contains("narrative") ||
            topicLower.Contains("journey") || topicLower.Contains("adventure") ||
            toneLower.Contains("entertaining") || toneLower.Contains("fun"))
        {
            return VideoType.Entertainment;
        }

        // Use ScriptTemplates logic for other types
        var detectedType = ScriptTemplates.DetermineVideoType(brief.Topic);
        
        return detectedType switch
        {
            Aura.Core.Templates.VideoType.Educational => VideoType.Educational,
            Aura.Core.Templates.VideoType.Tutorial => VideoType.Tutorial,
            Aura.Core.Templates.VideoType.Marketing => VideoType.Marketing,
            _ => VideoType.General
        };
    }

    /// <summary>
    /// Calculates target word count based on duration
    /// </summary>
    private int CalculateTargetWordCount(PlanSpec spec)
    {
        // 150 words per minute = 2.5 words per second
        return (int)(spec.TargetDuration.TotalSeconds * 2.5);
    }

    /// <summary>
    /// Generates entertainment-style template script
    /// </summary>
    private string GenerateEntertainmentTemplate(Brief brief, PlanSpec spec)
    {
        var sections = new System.Collections.Generic.List<string>();
        var targetWordCount = CalculateTargetWordCount(spec);

        sections.Add($"# {brief.Topic}");
        sections.Add("");
        sections.Add("## Opening");
        sections.Add($"Let me tell you about {brief.Topic}. This is a story that will surprise you, entertain you, and maybe even change how you think about things. Sit back, relax, and let's dive into this journey together.");
        sections.Add("");

        sections.Add("## The Story Begins");
        sections.Add($"It all started with {brief.Topic}. What seemed like a simple concept turned into something much more interesting. The twists and turns along the way make this a story worth telling, and I'm excited to share it with you.");
        sections.Add("");

        if (targetWordCount > 200)
        {
            sections.Add("## The Plot Thickens");
            sections.Add("As we dig deeper, things get more fascinating. The layers of complexity reveal themselves, and what you thought you knew might just surprise you. This is where the real magic happens.");
            sections.Add("");
        }

        sections.Add("## The Payoff");
        sections.Add($"And that's the story of {brief.Topic}. Sometimes the best stories are the ones that take us on unexpected journeys. I hope you enjoyed this as much as I enjoyed sharing it with you. Thanks for watching!");
        sections.Add("");

        return string.Join("\n", sections);
    }
}

/// <summary>
/// Video types for fallback generation
/// </summary>
public enum VideoType
{
    Educational,
    Tutorial,
    Marketing,
    Entertainment,
    General
}

