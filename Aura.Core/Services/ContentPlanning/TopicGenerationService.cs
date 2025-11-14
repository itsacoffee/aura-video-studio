using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.ContentPlanning;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentPlanning;

/// <summary>
/// Service for AI-powered topic generation
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class TopicGenerationService
{
    private readonly ILogger<TopicGenerationService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;

    public TopicGenerationService(
        ILogger<TopicGenerationService> logger,
        ILlmProvider llmProvider,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
    }

    /// <summary>
    /// Generates topic suggestions based on user preferences
    /// </summary>
    public async Task<TopicSuggestionResponse> GenerateTopicsAsync(
        TopicSuggestionRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} topic suggestions for category: {Category}",
            request.Count, request.Category);

        var prompt = BuildTopicGenerationPrompt(request);
        
        try
        {
            var brief = new Brief(
                Topic: prompt,
                Audience: request.TargetAudience ?? "general audience",
                Goal: "Generate engaging video topic ideas",
                Tone: "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "engaging"
            );

            var response = await GenerateWithLlmAsync(brief, planSpec, ct);
            var suggestions = ParseTopicSuggestions(response, request);

            return new TopicSuggestionResponse
            {
                Suggestions = suggestions,
                GeneratedAt = DateTime.UtcNow,
                TotalCount = suggestions.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate topics via LLM, using fallback");
            return GenerateFallbackTopics(request);
        }
    }

    /// <summary>
    /// Generates topics based on current trends
    /// </summary>
    public async Task<List<TopicSuggestion>> GenerateTrendBasedTopicsAsync(
        List<TrendData> trends,
        int count = 5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} trend-based topic suggestions", count);

        await Task.Delay(50, ct);

        return trends
            .OrderByDescending(t => t.TrendScore)
            .Take(count)
            .Select(trend => new TopicSuggestion
            {
                Topic = $"Exploring {trend.Topic}",
                Description = $"Create content about {trend.Topic} while it's trending on {trend.Platform}",
                Category = trend.Category,
                RelevanceScore = trend.TrendScore,
                TrendScore = trend.TrendScore,
                PredictedEngagement = EstimateEngagement(trend),
                Keywords = new List<string> { trend.Topic, trend.Platform, trend.Category },
                RecommendedPlatforms = new List<string> { trend.Platform }
            })
            .ToList();
    }

    private string BuildTopicGenerationPrompt(TopicSuggestionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Generate {request.Count} creative and engaging video topic ideas.");
        
        if (!string.IsNullOrEmpty(request.Category))
        {
            sb.AppendLine($"Category: {request.Category}");
        }

        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            sb.AppendLine($"Target Audience: {request.TargetAudience}");
        }

        if (request.Interests.Count != 0)
        {
            sb.AppendLine($"Interests: {string.Join(", ", request.Interests)}");
        }

        if (request.PreferredPlatforms.Count != 0)
        {
            sb.AppendLine($"Platforms: {string.Join(", ", request.PreferredPlatforms)}");
        }

        sb.AppendLine("Provide diverse, actionable topics that would perform well.");

        return sb.ToString();
    }

    private List<TopicSuggestion> ParseTopicSuggestions(string llmResponse, TopicSuggestionRequest request)
    {
        var suggestions = new List<TopicSuggestion>();
        var lines = llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Take(request.Count))
        {
            var cleanLine = line.Trim().TrimStart('-', '*', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ' ');
            
            if (string.IsNullOrWhiteSpace(cleanLine) || cleanLine.Length < 10)
                continue;

            suggestions.Add(new TopicSuggestion
            {
                Topic = cleanLine.Length > 100 ? cleanLine.Substring(0, 100) : cleanLine,
                Description = cleanLine,
                Category = request.Category ?? "General",
                RelevanceScore = 70 + Random.Shared.NextDouble() * 30,
                TrendScore = 60 + Random.Shared.NextDouble() * 40,
                PredictedEngagement = 50 + Random.Shared.NextDouble() * 50,
                Keywords = request.Interests.ToList(),
                RecommendedPlatforms = request.PreferredPlatforms.ToList()
            });
        }

        return suggestions.Count != 0 ? suggestions : GenerateFallbackTopics(request).Suggestions;
    }

    private TopicSuggestionResponse GenerateFallbackTopics(TopicSuggestionRequest request)
    {
        var templates = new[]
        {
            "How to master {skill} in {time}",
            "Top {number} {category} trends in {year}",
            "{category} tips for beginners",
            "Behind the scenes: {category} process",
            "Common {category} mistakes to avoid",
            "Ultimate guide to {category}",
            "{category} hacks that actually work",
            "Day in the life of a {role}",
            "Comparing {item1} vs {item2}",
            "Is {topic} worth it? Honest review"
        };

        var suggestions = templates.Take(request.Count).Select((template, i) => new TopicSuggestion
        {
            Topic = InterpolateTemplate(template, request),
            Description = $"Content idea based on proven formats and current trends",
            Category = request.Category ?? "General",
            RelevanceScore = 70 + Random.Shared.NextDouble() * 20,
            TrendScore = 60 + Random.Shared.NextDouble() * 30,
            PredictedEngagement = 55 + Random.Shared.NextDouble() * 35,
            Keywords = request.Interests.ToList(),
            RecommendedPlatforms = request.PreferredPlatforms.ToList()
        }).ToList();

        return new TopicSuggestionResponse
        {
            Suggestions = suggestions,
            GeneratedAt = DateTime.UtcNow,
            TotalCount = suggestions.Count
        };
    }

    private string InterpolateTemplate(string template, TopicSuggestionRequest request)
    {
        return template
            .Replace("{skill}", request.Category ?? "your skill")
            .Replace("{category}", request.Category ?? "your niche")
            .Replace("{time}", "30 days")
            .Replace("{number}", Random.Shared.Next(5, 15).ToString())
            .Replace("{year}", DateTime.UtcNow.Year.ToString())
            .Replace("{role}", request.TargetAudience ?? "creator")
            .Replace("{topic}", request.Interests.FirstOrDefault() ?? "this topic")
            .Replace("{item1}", request.Interests.ElementAtOrDefault(0) ?? "option A")
            .Replace("{item2}", request.Interests.ElementAtOrDefault(1) ?? "option B");
    }

    private double EstimateEngagement(TrendData trend)
    {
        var baseEngagement = 50.0;
        
        if (trend.Direction == TrendDirection.Rising)
            baseEngagement += 30;
        else if (trend.Direction == TrendDirection.Declining)
            baseEngagement -= 20;

        baseEngagement += (trend.TrendScore - 50) * 0.5;

        return Math.Max(10, Math.Min(100, baseEngagement));
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct);
        }
    }
}
