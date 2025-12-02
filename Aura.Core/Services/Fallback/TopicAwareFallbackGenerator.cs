using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Aura.Core.Services.Fallback;

/// <summary>
/// Generates topic-aware fallback scripts when LLM generation fails.
/// Provides templates for different topic categories (technology, business, education, health)
/// with appropriate language and section structure for each domain.
/// </summary>
public class TopicAwareFallbackGenerator
{
    private static readonly Dictionary<string, TopicTemplate> TopicTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["technology"] = new TopicTemplate
        {
            Intro = "In today's rapidly evolving digital landscape, {topic} represents a significant advancement.",
            Sections = new[] { "Key Concepts", "Current Applications", "Future Implications" },
            Outro = "As technology continues to advance, {topic} will play an increasingly important role."
        },
        ["business"] = new TopicTemplate
        {
            Intro = "Understanding {topic} is essential for modern business success.",
            Sections = new[] { "Core Principles", "Implementation Strategies", "Measuring Success" },
            Outro = "By applying these principles, organizations can leverage {topic} for competitive advantage."
        },
        ["education"] = new TopicTemplate
        {
            Intro = "Learning about {topic} opens new opportunities for growth and development.",
            Sections = new[] { "Fundamentals", "Practical Applications", "Further Learning" },
            Outro = "With this foundation in {topic}, you're ready to explore more advanced concepts."
        },
        ["health"] = new TopicTemplate
        {
            Intro = "Your well-being matters, and understanding {topic} can make a real difference.",
            Sections = new[] { "What You Need to Know", "Practical Tips", "When to Seek Help" },
            Outro = "Remember, taking care of your health is a journey, and knowledge about {topic} is a valuable step."
        },
        ["default"] = new TopicTemplate
        {
            Intro = "Welcome to this overview of {topic}.",
            Sections = new[] { "Introduction", "Key Points", "Summary" },
            Outro = "Thank you for learning about {topic} with us today."
        }
    };

    /// <summary>
    /// Generates a topic-aware fallback script with appropriate template based on topic category.
    /// </summary>
    /// <param name="topic">The main topic for the video</param>
    /// <param name="goal">Optional goal for the video (e.g., "teach the basics")</param>
    /// <param name="audience">Optional target audience (e.g., "beginners")</param>
    /// <param name="targetDuration">Target duration for the video</param>
    /// <param name="sceneCount">Default number of scenes (adjusted based on duration)</param>
    /// <returns>A markdown-formatted script with scenes</returns>
    public string GenerateFallbackScript(
        string topic,
        string? goal = null,
        string? audience = null,
        TimeSpan targetDuration = default,
        int sceneCount = 3)
    {
        var safeTopic = SanitizeTopic(topic);
        var template = DetectTopicCategory(topic);
        var scenes = CalculateSceneCount(targetDuration, sceneCount);

        var script = new StringBuilder();

        // Introduction scene
        script.AppendLine("## Scene 1: Introduction");
        script.AppendLine();
        script.AppendLine(template.Intro.Replace("{topic}", safeTopic));
        if (!string.IsNullOrEmpty(goal))
        {
            script.AppendLine();
            script.AppendLine($"Our goal today is to {goal.ToLower()}.");
        }
        if (!string.IsNullOrEmpty(audience))
        {
            script.AppendLine();
            script.AppendLine($"This content is designed for {audience}.");
        }
        script.AppendLine();

        // Middle sections
        var middleSections = Math.Min(template.Sections.Length, scenes - 2);
        for (int i = 0; i < middleSections; i++)
        {
            script.AppendLine($"## Scene {i + 2}: {template.Sections[i]}");
            script.AppendLine();
            script.AppendLine(GenerateSectionContent(template.Sections[i], safeTopic));
            script.AppendLine();
        }

        // Conclusion scene
        script.AppendLine($"## Scene {scenes}: Conclusion");
        script.AppendLine();
        script.AppendLine(template.Outro.Replace("{topic}", safeTopic));
        script.AppendLine();

        return script.ToString();
    }

    /// <summary>
    /// Detects the topic category based on keywords in the topic string.
    /// </summary>
    private TopicTemplate DetectTopicCategory(string topic)
    {
        var lowerTopic = topic.ToLowerInvariant();

        if (ContainsAny(lowerTopic, "software", "ai", "machine learning", "programming", "computer", "digital", "tech", "app", "algorithm", "code", "developer", "api", "data"))
            return TopicTemplates["technology"];

        if (ContainsAny(lowerTopic, "business", "marketing", "sales", "startup", "entrepreneur", "management", "finance", "strategy", "leadership", "revenue"))
            return TopicTemplates["business"];

        if (ContainsAny(lowerTopic, "learn", "education", "course", "tutorial", "training", "skill", "study", "teach", "student", "class"))
            return TopicTemplates["education"];

        if (ContainsAny(lowerTopic, "health", "fitness", "wellness", "medical", "diet", "exercise", "mental", "nutrition", "therapy", "treatment"))
            return TopicTemplates["health"];

        return TopicTemplates["default"];
    }

    /// <summary>
    /// Checks if the text contains any of the specified keywords.
    /// </summary>
    private static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Generates appropriate content for a specific section based on its name.
    /// </summary>
    private static string GenerateSectionContent(string sectionName, string topic)
    {
        return sectionName.ToLowerInvariant() switch
        {
            "key concepts" => $"Let's explore the fundamental concepts behind {topic}. Understanding these building blocks is essential for grasping the bigger picture.",
            "current applications" => $"Today, {topic} is being applied in numerous ways across various industries, creating real value and solving practical problems.",
            "future implications" => $"Looking ahead, {topic} is poised to evolve further, with emerging trends suggesting exciting possibilities on the horizon.",
            "core principles" => $"The core principles of {topic} provide a solid foundation for successful implementation and sustainable results.",
            "implementation strategies" => $"Implementing {topic} effectively requires a thoughtful approach, considering both immediate needs and long-term goals.",
            "measuring success" => $"Success with {topic} can be measured through key indicators that align with your specific objectives.",
            "fundamentals" => $"The fundamentals of {topic} form the essential knowledge base you'll need to build upon.",
            "practical applications" => $"Putting {topic} into practice involves applying what you've learned to real-world situations.",
            "further learning" => $"To continue your journey with {topic}, consider exploring advanced resources and hands-on experiences.",
            "what you need to know" => $"Here's what you need to know about {topic} to make informed decisions about your well-being.",
            "practical tips" => $"These practical tips for {topic} can help you take action and see real results in your daily life.",
            "when to seek help" => $"Knowing when to seek professional guidance about {topic} is an important part of your wellness journey.",
            "introduction" => $"Let's begin our exploration of {topic} by understanding its core elements and significance.",
            "key points" => $"Here are the key points about {topic} that will help you understand this subject more fully.",
            "summary" => $"In summary, {topic} encompasses important ideas that can be applied in various contexts.",
            _ => $"This section covers important aspects of {topic} that contribute to a comprehensive understanding."
        };
    }

    /// <summary>
    /// Calculates the appropriate scene count based on duration.
    /// Roughly 30 seconds per scene, with a minimum of 3 and maximum of 8 scenes.
    /// </summary>
    private static int CalculateSceneCount(TimeSpan duration, int defaultCount)
    {
        const int MinScenes = 3;
        const int MaxScenes = 8;

        if (duration == default)
            return Math.Max(MinScenes, Math.Min(defaultCount, MaxScenes));

        // Roughly 30 seconds per scene
        var calculated = (int)(duration.TotalSeconds / 30);
        return Math.Max(MinScenes, Math.Min(calculated, MaxScenes));
    }

    /// <summary>
    /// Sanitizes the topic string to prevent script injection and formatting issues.
    /// Removes special characters that could break the markdown script format.
    /// </summary>
    private static string SanitizeTopic(string topic)
    {
        // Remove special characters that could break script format
        var sanitized = Regex.Replace(topic, @"[#*_\[\]<>""\n\r]", "");
        sanitized = sanitized.Replace("'", "'").Trim();
        
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "this topic";
        }

        return sanitized;
    }

    /// <summary>
    /// Template structure for topic-specific script generation.
    /// </summary>
    private sealed record TopicTemplate
    {
        public required string Intro { get; init; }
        public required string[] Sections { get; init; }
        public required string Outro { get; init; }
    }
}
