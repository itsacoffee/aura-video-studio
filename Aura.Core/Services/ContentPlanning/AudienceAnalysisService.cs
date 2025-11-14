using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentPlanning;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentPlanning;

/// <summary>
/// Service for analyzing audience demographics and interests
/// </summary>
public class AudienceAnalysisService
{
    private readonly ILogger<AudienceAnalysisService> _logger;

    public AudienceAnalysisService(ILogger<AudienceAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes audience for a given platform and category
    /// </summary>
    public async Task<AudienceAnalysisResponse> AnalyzeAudienceAsync(
        AudienceAnalysisRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing audience for platform: {Platform}, category: {Category}",
            request.Platform, request.Category);

        await Task.Delay(100, ct).ConfigureAwait(false);

        var insights = GenerateAudienceInsights(request);
        var recommendations = GenerateRecommendations(insights, request);

        return new AudienceAnalysisResponse
        {
            Insights = insights,
            Recommendations = recommendations,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets demographic breakdown for a specific platform
    /// </summary>
    public async Task<Demographics> GetDemographicsAsync(
        string platform,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting demographics for platform: {Platform}", platform);

        await Task.Delay(50, ct).ConfigureAwait(false);

        return platform.ToLower() switch
        {
            "youtube" => new Demographics
            {
                AgeDistribution = new()
                {
                    ["18-24"] = 0.20,
                    ["25-34"] = 0.35,
                    ["35-44"] = 0.25,
                    ["45-54"] = 0.12,
                    ["55+"] = 0.08
                },
                GenderDistribution = new()
                {
                    ["Male"] = 0.55,
                    ["Female"] = 0.44,
                    ["Other"] = 0.01
                },
                LocationDistribution = new()
                {
                    ["North America"] = 0.45,
                    ["Europe"] = 0.30,
                    ["Asia"] = 0.15,
                    ["Other"] = 0.10
                }
            },
            "tiktok" => new Demographics
            {
                AgeDistribution = new()
                {
                    ["13-17"] = 0.25,
                    ["18-24"] = 0.42,
                    ["25-34"] = 0.22,
                    ["35-44"] = 0.08,
                    ["45+"] = 0.03
                },
                GenderDistribution = new()
                {
                    ["Male"] = 0.43,
                    ["Female"] = 0.56,
                    ["Other"] = 0.01
                },
                LocationDistribution = new()
                {
                    ["North America"] = 0.35,
                    ["Asia"] = 0.40,
                    ["Europe"] = 0.20,
                    ["Other"] = 0.05
                }
            },
            "instagram" => new Demographics
            {
                AgeDistribution = new()
                {
                    ["18-24"] = 0.32,
                    ["25-34"] = 0.34,
                    ["35-44"] = 0.18,
                    ["45-54"] = 0.10,
                    ["55+"] = 0.06
                },
                GenderDistribution = new()
                {
                    ["Male"] = 0.48,
                    ["Female"] = 0.51,
                    ["Other"] = 0.01
                },
                LocationDistribution = new()
                {
                    ["North America"] = 0.40,
                    ["Europe"] = 0.25,
                    ["Asia"] = 0.20,
                    ["South America"] = 0.10,
                    ["Other"] = 0.05
                }
            },
            _ => GenerateGenericDemographics()
        };
    }

    /// <summary>
    /// Gets top interests for a target audience
    /// </summary>
    public async Task<List<string>> GetTopInterestsAsync(
        string category,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting top interests for category: {Category}", category);

        await Task.Delay(50, ct).ConfigureAwait(false);

        var interestsByCategory = new Dictionary<string, List<string>>
        {
            ["Technology"] = new() { "AI & Machine Learning", "Software Development", "Gadgets", "Cloud Computing", "Cybersecurity" },
            ["Gaming"] = new() { "Esports", "Game Reviews", "Let's Plays", "Gaming News", "Game Development" },
            ["Fitness"] = new() { "Workout Routines", "Nutrition", "Wellness", "Sports", "Yoga" },
            ["Education"] = new() { "Tutorials", "Science", "History", "Language Learning", "Study Tips" },
            ["Entertainment"] = new() { "Movies", "Music", "Comedy", "Vlogs", "Challenges" },
            ["Lifestyle"] = new() { "Fashion", "Travel", "Food", "Home Decor", "Personal Development" }
        };

        return interestsByCategory.GetValueOrDefault(category, new List<string>
        {
            "General Content", "Trending Topics", "How-To Guides", "Reviews", "Entertainment"
        });
    }

    private AudienceInsight GenerateAudienceInsights(AudienceAnalysisRequest request)
    {
        var platform = request.Platform ?? "General";
        var demographics = GetDemographicsAsync(platform).Result;
        var topInterests = GetTopInterestsAsync(request.Category ?? "General").Result;

        return new AudienceInsight
        {
            Platform = platform,
            Demographics = demographics,
            TopInterests = topInterests,
            PreferredContentTypes = GetPreferredContentTypes(platform),
            EngagementRate = 3.5 + Random.Shared.NextDouble() * 4.5, // 3.5% - 8%
            BestPostingTimes = GetBestPostingTimes(platform)
        };
    }

    private List<string> GetPreferredContentTypes(string platform)
    {
        return platform.ToLower() switch
        {
            "youtube" => new() { "Long-form videos", "Tutorials", "Reviews", "Vlogs", "Educational content" },
            "tiktok" => new() { "Short-form videos", "Trends", "Challenges", "Quick tips", "Entertainment" },
            "instagram" => new() { "Reels", "Stories", "Carousel posts", "IGTV", "Live streams" },
            _ => new() { "Mixed content", "Videos", "Images", "Stories" }
        };
    }

    private Dictionary<string, double> GetBestPostingTimes(string platform)
    {
        return platform.ToLower() switch
        {
            "youtube" => new()
            {
                ["Monday 14:00"] = 82,
                ["Wednesday 18:00"] = 90,
                ["Saturday 12:00"] = 85
            },
            "tiktok" => new()
            {
                ["Tuesday 19:00"] = 88,
                ["Friday 21:00"] = 92,
                ["Sunday 16:00"] = 86
            },
            "instagram" => new()
            {
                ["Monday 11:00"] = 84,
                ["Wednesday 19:00"] = 87,
                ["Friday 14:00"] = 83
            },
            _ => new()
            {
                ["Weekday 12:00"] = 75,
                ["Weekday 18:00"] = 80
            }
        };
    }

    private List<string> GenerateRecommendations(AudienceInsight insights, AudienceAnalysisRequest request)
    {
        var recommendations = new List<string>();

        var dominantAge = insights.Demographics.AgeDistribution.OrderByDescending(kv => kv.Value).First().Key;
        recommendations.Add($"Target the {dominantAge} age group which represents the largest segment");

        if (insights.EngagementRate > 6.0)
        {
            recommendations.Add("Strong engagement rate - maintain current content style");
        }
        else if (insights.EngagementRate < 4.0)
        {
            recommendations.Add("Consider adjusting content to boost engagement");
        }

        recommendations.Add($"Focus on {insights.PreferredContentTypes.First()} which performs best on {insights.Platform}");

        var topTime = insights.BestPostingTimes.OrderByDescending(kv => kv.Value).First();
        recommendations.Add($"Schedule posts around {topTime.Key} for optimal reach");

        if (insights.TopInterests.Count != 0)
        {
            recommendations.Add($"Incorporate topics like {insights.TopInterests.First()} to align with audience interests");
        }

        return recommendations;
    }

    private Demographics GenerateGenericDemographics()
    {
        return new Demographics
        {
            AgeDistribution = new()
            {
                ["18-24"] = 0.25,
                ["25-34"] = 0.30,
                ["35-44"] = 0.20,
                ["45-54"] = 0.15,
                ["55+"] = 0.10
            },
            GenderDistribution = new()
            {
                ["Male"] = 0.50,
                ["Female"] = 0.49,
                ["Other"] = 0.01
            },
            LocationDistribution = new()
            {
                ["North America"] = 0.40,
                ["Europe"] = 0.30,
                ["Asia"] = 0.20,
                ["Other"] = 0.10
            }
        };
    }
}
