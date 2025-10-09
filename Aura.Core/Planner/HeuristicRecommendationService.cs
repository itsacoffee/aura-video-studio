using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Planner;

/// <summary>
/// Deterministic heuristic-based recommendation service
/// Used as fallback when LLM is unavailable or in offline mode
/// </summary>
public class HeuristicRecommendationService : IRecommendationService
{
    private readonly ILogger<HeuristicRecommendationService> _logger;

    public HeuristicRecommendationService(ILogger<HeuristicRecommendationService> logger)
    {
        _logger = logger;
    }

    public Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating heuristic recommendations for topic: {Topic}", request.Brief.Topic);

        var sceneCount = CalculateSceneCount(request.PlanSpec.TargetDuration, request.Constraints);
        var shotsPerScene = CalculateShotsPerScene(request.PlanSpec.Pacing);
        var bRollPercentage = CalculateBRollPercentage(request.PlanSpec.Density, request.Constraints);
        var overlayDensity = CalculateOverlayDensity(request.PlanSpec.Density);
        var readingLevel = CalculateReadingLevel(request.Brief.Audience, request.Constraints);
        var voice = CalculateVoiceRecommendations(request.PlanSpec.Pacing, request.Brief.Tone);
        var music = CalculateMusicRecommendations(request.PlanSpec.Pacing, request.Brief.Tone);
        var captions = CalculateCaptionStyle(request.Brief.Aspect);
        var outline = GenerateOutline(request.Brief, sceneCount);
        var thumbnailPrompt = GenerateThumbnailPrompt(request.Brief);
        var seo = GenerateSeoRecommendations(request.Brief);

        var recommendations = new PlannerRecommendations(
            Outline: outline,
            SceneCount: sceneCount,
            ShotsPerScene: shotsPerScene,
            BRollPercentage: bRollPercentage,
            OverlayDensity: overlayDensity,
            ReadingLevel: readingLevel,
            Voice: voice,
            Music: music,
            Captions: captions,
            ThumbnailPrompt: thumbnailPrompt,
            Seo: seo);

        _logger.LogInformation("Generated recommendations: {SceneCount} scenes, {ShotsPerScene} shots/scene",
            sceneCount, shotsPerScene);

        return Task.FromResult(recommendations);
    }

    private int CalculateSceneCount(TimeSpan duration, RecommendationConstraints? constraints)
    {
        // Base calculation: 1 scene per 30-45 seconds
        int baseCount = (int)Math.Ceiling(duration.TotalSeconds / 35);
        
        // Apply constraints
        if (constraints?.MinSceneCount != null)
            baseCount = Math.Max(baseCount, constraints.MinSceneCount.Value);
        
        if (constraints?.MaxSceneCount != null)
            baseCount = Math.Min(baseCount, constraints.MaxSceneCount.Value);
        
        // Clamp to reasonable bounds
        return Math.Clamp(baseCount, 3, 20);
    }

    private int CalculateShotsPerScene(Pacing pacing)
    {
        return pacing switch
        {
            Pacing.Chill => 2,
            Pacing.Conversational => 3,
            Pacing.Fast => 4,
            _ => 3
        };
    }

    private double CalculateBRollPercentage(Density density, RecommendationConstraints? constraints)
    {
        double percentage = density switch
        {
            Density.Sparse => 15.0,
            Density.Balanced => 30.0,
            Density.Dense => 50.0,
            _ => 30.0
        };

        if (constraints?.MaxBRollPercentage != null)
            percentage = Math.Min(percentage, constraints.MaxBRollPercentage.Value);

        return percentage;
    }

    private int CalculateOverlayDensity(Density density)
    {
        return density switch
        {
            Density.Sparse => 1,
            Density.Balanced => 3,
            Density.Dense => 5,
            _ => 3
        };
    }

    private int CalculateReadingLevel(string? audience, RecommendationConstraints? constraints)
    {
        // Reading level in years of education (8-16)
        int level = audience?.ToLowerInvariant() switch
        {
            "children" => 8,
            "teens" or "teenagers" => 10,
            "general" => 12,
            "professional" => 14,
            "academic" => 16,
            _ => 12
        };

        if (constraints?.MaxReadingLevel != null)
            level = Math.Min(level, constraints.MaxReadingLevel.Value);

        return Math.Clamp(level, 8, 16);
    }

    private VoiceRecommendations CalculateVoiceRecommendations(Pacing pacing, string tone)
    {
        double rate = pacing switch
        {
            Pacing.Chill => 0.85,
            Pacing.Conversational => 1.0,
            Pacing.Fast => 1.15,
            _ => 1.0
        };

        double pitch = tone.ToLowerInvariant() switch
        {
            "energetic" or "exciting" => 1.05,
            "serious" or "professional" => 0.98,
            "casual" or "friendly" => 1.02,
            _ => 1.0
        };

        string style = tone.ToLowerInvariant() switch
        {
            "energetic" or "exciting" => "Enthusiastic",
            "serious" or "professional" => "Professional",
            "casual" or "friendly" => "Conversational",
            _ => "Neutral"
        };

        return new VoiceRecommendations(rate, pitch, style);
    }

    private MusicRecommendations CalculateMusicRecommendations(Pacing pacing, string tone)
    {
        string tempo = pacing switch
        {
            Pacing.Chill => "Slow (60-80 BPM)",
            Pacing.Conversational => "Moderate (90-110 BPM)",
            Pacing.Fast => "Fast (120-140 BPM)",
            _ => "Moderate (90-110 BPM)"
        };

        string genre = tone.ToLowerInvariant() switch
        {
            "energetic" or "exciting" => "Electronic/Upbeat",
            "serious" or "professional" => "Corporate/Minimal",
            "casual" or "friendly" => "Acoustic/Folk",
            _ => "Ambient/Background"
        };

        // Standard intensity curve for most videos
        string intensityCurve = "Intro: Medium, Build: High, Mid: Medium, Outro: High";

        return new MusicRecommendations(tempo, intensityCurve, genre);
    }

    private CaptionStyle CalculateCaptionStyle(Aspect aspect)
    {
        string position = aspect switch
        {
            Aspect.Vertical9x16 => "Center",
            Aspect.Square1x1 => "Bottom-Center",
            Aspect.Widescreen16x9 => "Bottom-Center",
            _ => "Bottom-Center"
        };

        string fontSize = aspect switch
        {
            Aspect.Vertical9x16 => "Large",
            Aspect.Square1x1 => "Medium",
            Aspect.Widescreen16x9 => "Medium",
            _ => "Medium"
        };

        bool highlightKeywords = true; // Generally helpful for engagement

        return new CaptionStyle(position, fontSize, highlightKeywords);
    }

    private string GenerateOutline(Brief brief, int sceneCount)
    {
        var lines = new System.Collections.Generic.List<string>
        {
            $"# {brief.Topic}",
            "",
            "## Outline",
            "",
            "1. **Introduction** (Hook + Preview)"
        };

        // Generate middle sections
        for (int i = 2; i < sceneCount; i++)
        {
            lines.Add($"{i}. **Section {i - 1}** (Key Point)");
        }

        lines.Add($"{sceneCount}. **Conclusion** (Summary + CTA)");

        return string.Join("\n", lines);
    }

    private string GenerateThumbnailPrompt(Brief brief)
    {
        string basePrompt = $"Eye-catching thumbnail for video about '{brief.Topic}'. ";
        
        string styleHint = brief.Tone.ToLowerInvariant() switch
        {
            "energetic" or "exciting" => "Bold colors, dynamic composition, high contrast. ",
            "serious" or "professional" => "Clean design, professional look, clear text. ",
            "casual" or "friendly" => "Warm colors, approachable feel, inviting composition. ",
            _ => "Clear, engaging, with readable text overlay. "
        };

        string aspectHint = brief.Aspect switch
        {
            Aspect.Vertical9x16 => "Vertical format 9:16. ",
            Aspect.Square1x1 => "Square format 1:1. ",
            _ => "Widescreen format 16:9. "
        };

        return basePrompt + styleHint + aspectHint + "Include large text, face if relevant, high visual interest.";
    }

    private SeoRecommendations GenerateSeoRecommendations(Brief brief)
    {
        // Generate SEO-friendly title (under 60 chars ideally)
        string topic = string.IsNullOrEmpty(brief.Topic) ? "" : brief.Topic;
        string title = topic.Length <= 60 
            ? topic 
            : topic.Substring(0, 57) + "...";

        // Generate description
        string description = $"Learn about {brief.Topic}. " +
            $"This video covers everything you need to know about {brief.Topic} " +
            $"in a {brief.Tone.ToLowerInvariant()} and easy-to-understand way. " +
            $"Perfect for {brief.Audience ?? "everyone"} looking to understand this topic better.";

        // Generate tags based on topic keywords
        var tags = GenerateTags(brief.Topic, brief.Audience);

        return new SeoRecommendations(title, description, tags);
    }

    private string[] GenerateTags(string topic, string? audience)
    {
        var tags = new System.Collections.Generic.List<string>();
        
        // Add topic-based tags
        var topicWords = topic.Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(5);
        tags.AddRange(topicWords);

        // Add audience tag if specified
        if (!string.IsNullOrWhiteSpace(audience))
        {
            tags.Add(audience.ToLowerInvariant());
        }

        // Add generic helpful tags
        tags.AddRange(new[] { "tutorial", "guide", "howto", "educational" });

        return tags.Distinct().Take(10).ToArray();
    }
}
