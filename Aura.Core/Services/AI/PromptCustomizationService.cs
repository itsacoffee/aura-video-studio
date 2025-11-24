using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Services.Ideation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AI;

/// <summary>
/// Service for customizing and managing prompt templates with user-provided instructions
/// Handles prompt versioning, variable substitution, and security validation
/// </summary>
public class PromptCustomizationService
{
    private readonly ILogger<PromptCustomizationService> _logger;
    private readonly PromptLibrary _promptLibrary;
    private readonly Dictionary<string, PromptVersion> _promptVersions;
    private readonly WebSearchService? _webSearchService;
    private readonly TrendingTopicsService? _trendingTopicsService;

    public PromptCustomizationService(
        ILogger<PromptCustomizationService> logger,
        WebSearchService? webSearchService = null,
        TrendingTopicsService? trendingTopicsService = null)
    {
        _logger = logger;
        _promptLibrary = new PromptLibrary();
        _promptVersions = InitializePromptVersions();
        _webSearchService = webSearchService;
        _trendingTopicsService = trendingTopicsService;
    }

    /// <summary>
    /// Build customized prompt with user modifications and dynamic context injection
    /// </summary>
    public async Task<string> BuildCustomizedPromptAsync(
        Brief brief,
        PlanSpec spec,
        PromptModifiers? modifiers = null,
        CancellationToken ct = default)
    {
        var basePrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

        // Preserve original behavior: if modifiers is null, return base prompt immediately
        // Service injection only occurs when modifiers are provided
        if (modifiers == null)
        {
            return basePrompt;
        }

        var sb = new StringBuilder(basePrompt);

        // Inject dynamic context from web intelligence if available
        if (_webSearchService != null && !string.IsNullOrWhiteSpace(brief.Topic))
        {
            try
            {
                var competitiveIntel = await _webSearchService.GetCompetitiveIntelligenceAsync(
                    brief.Topic, maxResults: 10, ct).ConfigureAwait(false);

                if (competitiveIntel.TopDomains.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("=== MARKET INTELLIGENCE ===");
                    sb.AppendLine($"Content Saturation: {competitiveIntel.SaturationLevel}");
                    sb.AppendLine($"Average Relevance: {competitiveIntel.AverageRelevance:F1}%");
                    sb.AppendLine();
                    sb.AppendLine("Top Competitors:");
                    foreach (var domain in competitiveIntel.TopDomains.Take(3))
                    {
                        sb.AppendLine($"- {domain.Domain} ({domain.ContentCount} pieces)");
                    }
                    sb.AppendLine();
                    sb.AppendLine("Use this intelligence to identify unique angles and avoid oversaturated approaches.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inject competitive intelligence into prompt");
            }
        }

        // Inject trending context if available
        if (_trendingTopicsService != null && !string.IsNullOrWhiteSpace(brief.Topic))
        {
            try
            {
                var trendingTopics = await _trendingTopicsService.GetTrendingTopicsAsync(
                    brief.Topic, maxResults: 3, forceRefresh: false, ct).ConfigureAwait(false);

                if (trendingTopics.Count > 0)
                {
                    sb.AppendLine("=== TRENDING CONTEXT ===");
                    sb.AppendLine("Current trending topics related to this content:");
                    foreach (var topic in trendingTopics)
                    {
                        sb.AppendLine($"- {topic.Topic} (Trend Score: {topic.TrendScore:F1}/100)");
                    }
                    sb.AppendLine();
                    sb.AppendLine("Consider how to leverage or relate to these trends for better engagement.");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inject trending topics into prompt");
            }
        }

        // Apply modifiers (guaranteed to be non-null at this point due to early return above)
        if (modifiers != null)
        {
            if (!string.IsNullOrWhiteSpace(modifiers.AdditionalInstructions))
            {
                var sanitizedInstructions = SanitizeUserInstructions(modifiers.AdditionalInstructions);
                sb.AppendLine();
                sb.AppendLine("USER INSTRUCTIONS:");
                sb.AppendLine(sanitizedInstructions);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(modifiers.ExampleStyle))
            {
                var example = _promptLibrary.GetExampleByName(modifiers.ExampleStyle);
                if (example != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"EXAMPLE STYLE REFERENCE ({example.VideoType} - {example.ExampleName}):");
                    sb.AppendLine($"Description: {example.Description}");
                    sb.AppendLine();
                    sb.AppendLine("Key techniques from example:");
                    foreach (var technique in example.KeyTechniques)
                    {
                        sb.AppendLine($"- {technique}");
                    }
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate prompt preview with variable substitutions visible
    /// </summary>
    public async Task<PromptPreview> GeneratePreviewAsync(
        Brief brief,
        PlanSpec spec,
        PromptModifiers? modifiers = null,
        CancellationToken ct = default)
    {
        var systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
        var userPrompt = await BuildCustomizedPromptAsync(brief, spec, modifiers, ct).ConfigureAwait(false);
        var finalPrompt = $"{systemPrompt}\n\n{userPrompt}";

        var substitutions = new Dictionary<string, string>
        {
            { "{TOPIC}", brief.Topic },
            { "{AUDIENCE}", brief.Audience ?? "General" },
            { "{GOAL}", brief.Goal ?? "Inform" },
            { "{TONE}", brief.Tone },
            { "{DURATION}", $"{spec.TargetDuration.TotalMinutes:F1} minutes" },
            { "{PACING}", spec.Pacing.ToString() },
            { "{DENSITY}", spec.Density.ToString() },
            { "{LANGUAGE}", brief.Language }
        };

        var estimatedTokens = EstimateTokenCount(finalPrompt);

        var version = modifiers?.PromptVersion ?? "default-v1";

        return new PromptPreview(
            SystemPrompt: systemPrompt,
            UserPrompt: userPrompt,
            FinalPrompt: finalPrompt,
            SubstitutedVariables: substitutions,
            PromptVersion: version,
            EstimatedTokens: estimatedTokens);
    }

    /// <summary>
    /// Get available prompt versions
    /// </summary>
    public IReadOnlyDictionary<string, PromptVersion> GetPromptVersions()
    {
        return _promptVersions;
    }

    /// <summary>
    /// Get specific prompt version
    /// </summary>
    public PromptVersion? GetPromptVersion(string version)
    {
        return _promptVersions.TryGetValue(version, out var promptVersion)
            ? promptVersion
            : null;
    }

    /// <summary>
    /// Validate custom instructions for security issues
    /// Returns true if safe, false if potentially malicious
    /// </summary>
    public bool ValidateCustomInstructions(string instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return true;
        }

        var dangerousPatterns = new[]
        {
            @"ignore\s+(all\s+)?previous\s+instructions",
            @"disregard\s+system\s+prompt",
            @"forget\s+(your\s+)?instructions",
            @"new\s+system\s+prompt",
            @"you\s+are\s+now",
            @"act\s+as\s+(if\s+)?you",
            @"pretend\s+to\s+be",
            @"<script>",
            @"javascript:",
            @"eval\(",
            @"exec\(",
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(instructions, pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Potentially malicious pattern detected in custom instructions: {Pattern}", pattern);
                return false;
            }
        }

        if (instructions.Length > 5000)
        {
            _logger.LogWarning("Custom instructions exceed maximum length: {Length} chars", instructions.Length);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sanitize user instructions to prevent prompt injection
    /// </summary>
    private string SanitizeUserInstructions(string instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return string.Empty;
        }

        var sanitized = instructions.Trim();

        sanitized = Regex.Replace(sanitized, @"ignore\s+previous", "consider previous", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"disregard\s+system", "regard system", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"forget\s+your", "remember your", RegexOptions.IgnoreCase);

        sanitized = sanitized.Replace("<", "&lt;").Replace(">", "&gt;");

        if (sanitized.Length > 5000)
        {
            sanitized = sanitized.Substring(0, 5000);
        }

        return sanitized;
    }

    /// <summary>
    /// Estimate token count for a prompt (rough approximation)
    /// </summary>
    private static int EstimateTokenCount(string text)
    {
        var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return (int)(words.Length * 1.3);
    }

    /// <summary>
    /// Initialize available prompt versions
    /// </summary>
    private static Dictionary<string, PromptVersion> InitializePromptVersions()
    {
        return new Dictionary<string, PromptVersion>
        {
            {
                "default-v1",
                new PromptVersion(
                    Version: "default-v1",
                    Name: "Standard Quality",
                    Description: "Balanced approach optimized for most video types",
                    SystemPrompt: EnhancedPromptTemplates.GetSystemPromptForScriptGeneration(),
                    UserPromptTemplate: "Standard user prompt template",
                    Variables: new Dictionary<string, string>
                    {
                        { "TOPIC", "Video topic" },
                        { "AUDIENCE", "Target audience" },
                        { "GOAL", "Content goal" },
                        { "TONE", "Video tone" },
                        { "DURATION", "Target duration" },
                        { "PACING", "Content pacing" },
                        { "DENSITY", "Information density" },
                        { "LANGUAGE", "Target language" }
                    },
                    IsDefault: true)
            },
            {
                "high-engagement-v1",
                new PromptVersion(
                    Version: "high-engagement-v1",
                    Name: "High Engagement",
                    Description: "Optimized for maximum viewer retention and engagement",
                    SystemPrompt: EnhancedPromptTemplates.GetSystemPromptForScriptGeneration() +
                        "\n\nADDITIONAL FOCUS: Prioritize hooks, pattern interrupts, and emotional peaks. " +
                        "Target 90%+ retention through strategic pacing and curiosity gaps.",
                    UserPromptTemplate: "High engagement user prompt template",
                    Variables: new Dictionary<string, string>
                    {
                        { "TOPIC", "Video topic" },
                        { "AUDIENCE", "Target audience" },
                        { "GOAL", "Content goal" },
                        { "TONE", "Video tone" },
                        { "DURATION", "Target duration" },
                        { "PACING", "Content pacing" },
                        { "DENSITY", "Information density" },
                        { "LANGUAGE", "Target language" }
                    },
                    IsDefault: false)
            },
            {
                "educational-deep-v1",
                new PromptVersion(
                    Version: "educational-deep-v1",
                    Name: "Educational Deep Dive",
                    Description: "Comprehensive educational content with detailed explanations",
                    SystemPrompt: EnhancedPromptTemplates.GetSystemPromptForScriptGeneration() +
                        "\n\nADDITIONAL FOCUS: Prioritize clarity and comprehension over entertainment. " +
                        "Include more detailed explanations, examples, and step-by-step breakdowns. " +
                        "Assume audience wants to truly understand the topic.",
                    UserPromptTemplate: "Educational deep dive user prompt template",
                    Variables: new Dictionary<string, string>
                    {
                        { "TOPIC", "Video topic" },
                        { "AUDIENCE", "Target audience" },
                        { "GOAL", "Content goal" },
                        { "TONE", "Video tone" },
                        { "DURATION", "Target duration" },
                        { "PACING", "Content pacing" },
                        { "DENSITY", "Information density" },
                        { "LANGUAGE", "Target language" }
                    },
                    IsDefault: false)
            }
        };
    }

    /// <summary>
    /// Get prompt library for few-shot examples
    /// </summary>
    public PromptLibrary GetPromptLibrary()
    {
        return _promptLibrary;
    }
}
