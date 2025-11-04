using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for LLM-assisted refinement of visual prompts
/// </summary>
public class VisualPromptRefinementService
{
    private readonly ILogger<VisualPromptRefinementService> _logger;
    private readonly ILlmProvider _llmProvider;

    public VisualPromptRefinementService(
        ILogger<VisualPromptRefinementService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Refine a visual prompt based on current results and issues
    /// </summary>
    public async Task<PromptRefinementResult> RefinePromptAsync(
        PromptRefinementRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Refining visual prompt for scene {SceneIndex}, current candidates: {Count}",
            request.CurrentPrompt.SceneIndex,
            request.CurrentCandidates.Count);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(request);

        try
        {
            var response = await _llmProvider.CompleteAsync(
                systemPrompt + "\n\n" + userPrompt,
                ct);

            var result = ParseRefinementResponse(response, request.CurrentPrompt);

            _logger.LogInformation(
                "Prompt refinement completed. Confidence: {Confidence:F1}, Expected improvement: {Improvement:F1}",
                result.Confidence,
                result.ExpectedImprovement);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refine prompt");

            return new PromptRefinementResult
            {
                RefinedPrompt = request.CurrentPrompt,
                Explanation = "Refinement failed, using original prompt",
                Confidence = 0,
                ExpectedImprovement = 0
            };
        }
    }

    /// <summary>
    /// Suggest refinements based on low scores
    /// </summary>
    public async Task<IReadOnlyList<string>> SuggestImprovementsAsync(
        VisualPrompt prompt,
        IReadOnlyList<ImageCandidate> candidates,
        CancellationToken ct = default)
    {
        var suggestions = new List<string>();

        var topScore = candidates.OrderByDescending(c => c.OverallScore).FirstOrDefault()?.OverallScore ?? 0;

        if (topScore < 60.0)
        {
            suggestions.Add("Overall quality is low. Consider more specific subject and composition details.");
        }

        var avgKeywordScore = candidates.Any()
            ? candidates.Average(c => c.KeywordCoverageScore)
            : 0;

        if (avgKeywordScore < 40.0)
        {
            suggestions.Add("Keyword coverage is poor. Add more narrative keywords from the scene text.");
        }

        var avgAestheticScore = candidates.Any()
            ? candidates.Average(c => c.AestheticScore)
            : 0;

        if (avgAestheticScore < 50.0)
        {
            suggestions.Add("Aesthetic quality is low. Specify lighting, composition, and style more clearly.");
        }

        if (string.IsNullOrWhiteSpace(prompt.Subject))
        {
            suggestions.Add("No clear subject defined. Add a specific subject or focus point.");
        }

        if (prompt.ColorPalette.Count == 0)
        {
            suggestions.Add("No color palette specified. Add 3-5 specific colors for better visual consistency.");
        }

        if (string.IsNullOrWhiteSpace(prompt.CompositionGuidelines))
        {
            suggestions.Add("No composition guidelines. Consider rule of thirds, leading lines, or other principles.");
        }

        _logger.LogDebug("Generated {Count} improvement suggestions", suggestions.Count);

        return suggestions;
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert visual director and cinematographer. Your task is to refine visual prompts for image generation to improve quality, aesthetic appeal, and narrative alignment.

Analyze the current prompt, the scores of generated candidates, and any issues detected. Then suggest specific improvements to:
1. Subject clarity and framing
2. Composition and visual flow
3. Lighting and mood
4. Style keywords and aesthetic direction
5. Narrative keyword coverage

Provide concrete, actionable refinements that will improve candidate quality scores.

Respond in JSON format with:
{
  ""refinedPrompt"": {
    ""detailedDescription"": ""improved description"",
    ""subject"": ""clear subject"",
    ""framing"": ""framing guidance"",
    ""compositionGuidelines"": ""composition details"",
    ""lighting"": {
      ""mood"": ""lighting mood"",
      ""direction"": ""light direction"",
      ""quality"": ""light quality"",
      ""timeOfDay"": ""time of day""
    },
    ""colorPalette"": [""color1"", ""color2"", ""color3""],
    ""styleKeywords"": [""keyword1"", ""keyword2""],
    ""narrativeKeywords"": [""keyword1"", ""keyword2""]
  },
  ""explanation"": ""brief explanation of changes"",
  ""improvements"": [""improvement1"", ""improvement2""],
  ""expectedImprovement"": 75.0,
  ""confidence"": 85.0
}";
    }

    private string BuildUserPrompt(PromptRefinementRequest request)
    {
        var prompt = request.CurrentPrompt;
        var topCandidates = request.CurrentCandidates
            .OrderByDescending(c => c.OverallScore)
            .Take(3)
            .ToList();

        var promptJson = JsonSerializer.Serialize(new
        {
            sceneIndex = prompt.SceneIndex,
            detailedDescription = prompt.DetailedDescription,
            subject = prompt.Subject,
            framing = prompt.Framing,
            compositionGuidelines = prompt.CompositionGuidelines,
            lighting = new
            {
                mood = prompt.Lighting.Mood,
                direction = prompt.Lighting.Direction,
                quality = prompt.Lighting.Quality,
                timeOfDay = prompt.Lighting.TimeOfDay
            },
            colorPalette = prompt.ColorPalette,
            styleKeywords = prompt.StyleKeywords,
            narrativeKeywords = prompt.NarrativeKeywords,
            style = prompt.Style.ToString(),
            qualityTier = prompt.QualityTier.ToString()
        }, new JsonSerializerOptions { WriteIndented = true });

        var scoresJson = JsonSerializer.Serialize(
            topCandidates.Select(c => new
            {
                overallScore = c.OverallScore,
                aestheticScore = c.AestheticScore,
                keywordScore = c.KeywordCoverageScore,
                qualityScore = c.QualityScore,
                rejectionReasons = c.RejectionReasons
            }),
            new JsonSerializerOptions { WriteIndented = true });

        var issuesText = request.IssuesDetected.Any()
            ? string.Join("\n- ", request.IssuesDetected)
            : "None specified";

        var feedbackText = !string.IsNullOrWhiteSpace(request.UserFeedback)
            ? request.UserFeedback
            : "None provided";

        return $@"Current Visual Prompt:
{promptJson}

Top 3 Candidate Scores:
{scoresJson}

Issues Detected:
- {issuesText}

User Feedback:
{feedbackText}

Please analyze this visual prompt and the resulting candidate scores, then provide a refined prompt that addresses the issues and improves overall quality. Focus on making the subject, composition, lighting, and style more specific and visually compelling.";
    }

    private PromptRefinementResult ParseRefinementResponse(string response, VisualPrompt originalPrompt)
    {
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var refinedPromptEl = root.GetProperty("refinedPrompt");
            var lightingEl = refinedPromptEl.GetProperty("lighting");

            var refinedPrompt = originalPrompt with
            {
                DetailedDescription = refinedPromptEl.GetProperty("detailedDescription").GetString() ?? originalPrompt.DetailedDescription,
                Subject = refinedPromptEl.TryGetProperty("subject", out var subject) ? subject.GetString() ?? originalPrompt.Subject : originalPrompt.Subject,
                Framing = refinedPromptEl.TryGetProperty("framing", out var framing) ? framing.GetString() ?? originalPrompt.Framing : originalPrompt.Framing,
                CompositionGuidelines = refinedPromptEl.GetProperty("compositionGuidelines").GetString() ?? originalPrompt.CompositionGuidelines,
                Lighting = new LightingSetup
                {
                    Mood = lightingEl.GetProperty("mood").GetString() ?? "neutral",
                    Direction = lightingEl.GetProperty("direction").GetString() ?? "front",
                    Quality = lightingEl.GetProperty("quality").GetString() ?? "soft",
                    TimeOfDay = lightingEl.GetProperty("timeOfDay").GetString() ?? "day"
                },
                ColorPalette = JsonSerializer.Deserialize<List<string>>(refinedPromptEl.GetProperty("colorPalette").GetRawText()) ?? new List<string>(),
                StyleKeywords = JsonSerializer.Deserialize<List<string>>(refinedPromptEl.GetProperty("styleKeywords").GetRawText()) ?? new List<string>(),
                NarrativeKeywords = JsonSerializer.Deserialize<List<string>>(refinedPromptEl.GetProperty("narrativeKeywords").GetRawText()) ?? new List<string>()
            };

            var explanation = root.GetProperty("explanation").GetString() ?? string.Empty;
            var improvements = JsonSerializer.Deserialize<List<string>>(root.GetProperty("improvements").GetRawText()) ?? new List<string>();
            var expectedImprovement = root.GetProperty("expectedImprovement").GetDouble();
            var confidence = root.GetProperty("confidence").GetDouble();

            return new PromptRefinementResult
            {
                RefinedPrompt = refinedPrompt,
                Explanation = explanation,
                Improvements = improvements,
                ExpectedImprovement = expectedImprovement,
                Confidence = confidence
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse refinement response, using fallback");

            return new PromptRefinementResult
            {
                RefinedPrompt = originalPrompt with
                {
                    DetailedDescription = originalPrompt.DetailedDescription + " (high quality, professional, detailed)"
                },
                Explanation = "Applied basic quality improvements",
                Improvements = new List<string> { "Enhanced quality keywords" },
                ExpectedImprovement = 10.0,
                Confidence = 50.0
            };
        }
    }
}
