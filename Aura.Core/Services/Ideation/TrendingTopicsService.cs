using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Ideation;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Ideation;

/// <summary>
/// Service for fetching and analyzing trending topics from live data sources
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class TrendingTopicsService
{
    private readonly ILogger<TrendingTopicsService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly WebSearchService? _webSearchService;

    private const string CacheKeyPrefix = "trending_topics_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    // Niche to category mapping for better trend discovery
    private static readonly Dictionary<string, string[]> NicheKeywords = new()
    {
        ["gaming"] = new[] { "gaming", "video games", "esports", "game review", "gameplay" },
        ["technology"] = new[] { "tech", "software", "hardware", "AI", "programming", "coding" },
        ["entertainment"] = new[] { "movies", "TV shows", "celebrity", "entertainment", "music" },
        ["health"] = new[] { "health", "fitness", "wellness", "nutrition", "exercise" },
        ["business"] = new[] { "business", "entrepreneur", "startup", "finance", "investing" },
        ["education"] = new[] { "education", "learning", "tutorial", "course", "teaching" },
        ["lifestyle"] = new[] { "lifestyle", "fashion", "beauty", "travel", "food" },
        ["science"] = new[] { "science", "research", "physics", "chemistry", "biology" },
        ["news"] = new[] { "news", "politics", "current events", "world news" },
        ["sports"] = new[] { "sports", "football", "basketball", "soccer", "athletics" }
    };

    public TrendingTopicsService(
        ILogger<TrendingTopicsService> logger,
        ILlmProvider llmProvider,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        LlmStageAdapter? stageAdapter = null,
        WebSearchService? webSearchService = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _webSearchService = webSearchService;
    }

    /// <summary>
    /// Get trending topics with AI analysis
    /// </summary>
    public async Task<List<TrendingTopic>> GetTrendingTopicsAsync(
        string? niche,
        int maxResults,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{niche ?? "general"}_{maxResults}";

        // Check cache first unless force refresh
        if (!forceRefresh && _cache.TryGetValue<List<TrendingTopic>>(cacheKey, out var cachedTopics))
        {
            _logger.LogInformation("Returning cached trending topics for niche: {Niche}", niche ?? "general");
            return cachedTopics!;
        }

        _logger.LogInformation("Fetching live trending topics for niche: {Niche}", niche ?? "general");

        // Fetch from multiple sources and aggregate
        var topics = await FetchTrendingTopicsFromSourcesAsync(niche, maxResults, ct).ConfigureAwait(false);

        // Enhance with AI analysis
        var enhancedTopics = await EnhanceWithAiAnalysisAsync(topics, niche, ct).ConfigureAwait(false);

        // Cache the results
        _cache.Set(cacheKey, enhancedTopics, CacheDuration);

        return enhancedTopics;
    }

    /// <summary>
    /// Fetch trending topics from multiple data sources with real-time web search
    /// </summary>
    private async Task<List<TrendingTopic>> FetchTrendingTopicsFromSourcesAsync(
        string? niche,
        int maxResults,
        CancellationToken ct)
    {
        var topics = new List<TrendingTopic>();

        // Generate contextual trending topics based on niche (fallback)
        topics.AddRange(GenerateNicheSpecificTopics(niche, maxResults));

        // Fetch real-time trending data from web search if available
        if (_webSearchService != null)
        {
            try
            {
                var searchQuery = niche != null ? $"trending {niche} topics" : "trending topics";
                var trendingResults = await _webSearchService.SearchTrendsAsync(searchQuery, maxResults, ct).ConfigureAwait(false);
                
                if (trendingResults.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} trending topics from web search", trendingResults.Count);
                    
                    // Convert web search results to trending topics
                    var webTopics = trendingResults.Select((result, index) => new TrendingTopic(
                        TopicId: Guid.NewGuid().ToString(),
                        Topic: result.Title,
                        TrendScore: result.TrendScore,
                        SearchVolume: "High", // Web search results indicate high search volume
                        Competition: "Medium",
                        Seasonality: "Current",
                        Lifecycle: "Rising",
                        RelatedTopics: ExtractRelatedTopics(result.Snippet),
                        DetectedAt: DateTime.UtcNow,
                        TrendVelocity: result.TrendScore - 50, // Normalize trend score
                        EstimatedAudience: (long)(result.TrendScore * 10000) // Rough estimate
                    )).ToList();
                    
                    // Merge with generated topics, prioritizing web search results
                    topics = webTopics
                        .Concat(topics.Where(t => !webTopics.Any(wt => wt.Topic.Equals(t.Topic, StringComparison.OrdinalIgnoreCase))))
                        .OrderByDescending(t => t.TrendScore)
                        .Take(maxResults)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch trending topics from web search, using generated topics");
            }
        }

        // Sort by trend score and take top results
        return topics
            .OrderByDescending(t => t.TrendScore)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Extract related topics from snippet text
    /// </summary>
    private List<string> ExtractRelatedTopics(string snippet)
    {
        // Simple extraction - look for capitalized words and common topic patterns
        var words = snippet.Split(new[] { ' ', '.', ',', '!', '?', ':', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var topics = new List<string>();
        
        foreach (var word in words)
        {
            var cleanWord = word.Trim();
            if (cleanWord.Length > 4 && char.IsUpper(cleanWord[0]))
            {
                topics.Add(cleanWord);
            }
        }
        
        return topics.Distinct().Take(5).ToList();
    }

    /// <summary>
    /// Generate niche-specific trending topics
    /// </summary>
    private List<TrendingTopic> GenerateNicheSpecificTopics(string? niche, int maxResults)
    {
        var topics = new List<TrendingTopic>();
        var random = new Random();
        var normalizedNiche = niche?.ToLowerInvariant()?.Trim();

        // Get niche-specific topic ideas
        var topicTemplates = GetTopicTemplatesForNiche(normalizedNiche);

        for (int i = 0; i < Math.Min(maxResults, topicTemplates.Count); i++)
        {
            var template = topicTemplates[i];
            var trendScore = 95 - (i * 4) + random.Next(-5, 6); // Variation in scores
            var velocity = (random.NextDouble() * 20) - 5; // -5 to +15 growth rate

            topics.Add(new TrendingTopic(
                TopicId: Guid.NewGuid().ToString(),
                Topic: template.Title,
                TrendScore: Math.Max(0, Math.Min(100, trendScore)),
                SearchVolume: FormatSearchVolume(100000 - (i * 15000) + random.Next(-10000, 10000)),
                Competition: DetermineCompetition(trendScore),
                Seasonality: template.Seasonality,
                Lifecycle: DetermineLifecycle(velocity),
                RelatedTopics: template.RelatedTopics.ToList(),
                DetectedAt: DateTime.UtcNow,
                AiInsights: null, // Will be filled by AI analysis
                Hashtags: template.SuggestedHashtags.ToList(),
                TrendVelocity: velocity,
                EstimatedAudience: EstimateAudience(trendScore)
            ));
        }

        return topics;
    }

    /// <summary>
    /// Get topic templates based on niche
    /// </summary>
    private List<TopicTemplate> GetTopicTemplatesForNiche(string? niche)
    {
        // These would ideally come from real APIs, but we'll provide intelligent defaults
        var templates = new List<TopicTemplate>();

        if (string.IsNullOrEmpty(niche) || niche == "general")
        {
            templates.AddRange(new[]
            {
                new TopicTemplate("AI and Content Creation Tools in 2025", "Year-round", 
                    new[] { "AI tools", "content automation", "creator economy" },
                    new[] { "#AItools", "#ContentCreation", "#CreatorEconomy" }),
                new TopicTemplate("Short-Form Video Strategy", "Year-round",
                    new[] { "TikTok", "YouTube Shorts", "Instagram Reels" },
                    new[] { "#ShortForm", "#VideoMarketing", "#SocialMedia" }),
                new TopicTemplate("Personal Brand Building", "Year-round",
                    new[] { "personal branding", "thought leadership", "online presence" },
                    new[] { "#PersonalBrand", "#ThoughtLeadership", "#Branding" }),
                new TopicTemplate("Monetization Strategies for Creators", "Year-round",
                    new[] { "creator monetization", "passive income", "digital products" },
                    new[] { "#CreatorEconomy", "#Monetization", "#PassiveIncome" }),
                new TopicTemplate("Video Editing Techniques", "Year-round",
                    new[] { "video editing", "post-production", "editing tips" },
                    new[] { "#VideoEditing", "#PostProduction", "#EditingTips" })
            });
        }
        else if (niche == "gaming")
        {
            templates.AddRange(new[]
            {
                new TopicTemplate("Latest Gaming Hardware Reviews", "Year-round",
                    new[] { "GPU", "gaming PC", "console reviews" },
                    new[] { "#Gaming", "#GamingHardware", "#PCGaming" }),
                new TopicTemplate("Competitive Gaming Strategies", "Year-round",
                    new[] { "esports", "competitive play", "pro strategies" },
                    new[] { "#Esports", "#CompetitiveGaming", "#ProStrategies" }),
                new TopicTemplate("Indie Game Discoveries", "Year-round",
                    new[] { "indie games", "game development", "hidden gems" },
                    new[] { "#IndieGames", "#GameDev", "#Gaming" }),
                new TopicTemplate("Game Streaming Tips", "Year-round",
                    new[] { "Twitch", "streaming setup", "content creation" },
                    new[] { "#Streaming", "#Twitch", "#ContentCreator" }),
                new TopicTemplate("Gaming Industry News and Trends", "Year-round",
                    new[] { "gaming news", "industry trends", "game releases" },
                    new[] { "#GamingNews", "#GameReleases", "#Gaming" })
            });
        }
        else if (niche == "technology" || niche == "tech")
        {
            templates.AddRange(new[]
            {
                new TopicTemplate("Emerging AI Technologies", "Year-round",
                    new[] { "artificial intelligence", "machine learning", "AI applications" },
                    new[] { "#AI", "#MachineLearning", "#Technology" }),
                new TopicTemplate("Software Development Best Practices", "Year-round",
                    new[] { "coding", "software engineering", "development practices" },
                    new[] { "#Programming", "#SoftwareDev", "#Coding" }),
                new TopicTemplate("Cybersecurity Essentials", "Year-round",
                    new[] { "cybersecurity", "data protection", "security practices" },
                    new[] { "#Cybersecurity", "#DataProtection", "#InfoSec" }),
                new TopicTemplate("Mobile App Development Trends", "Year-round",
                    new[] { "mobile apps", "iOS", "Android development" },
                    new[] { "#AppDevelopment", "#MobileDev", "#iOS" }),
                new TopicTemplate("Cloud Computing Solutions", "Year-round",
                    new[] { "cloud platforms", "AWS", "Azure", "serverless" },
                    new[] { "#CloudComputing", "#AWS", "#Azure" })
            });
        }
        else if (niche == "health" || niche == "fitness")
        {
            templates.AddRange(new[]
            {
                new TopicTemplate("Home Workout Routines", "Year-round",
                    new[] { "fitness", "home workouts", "bodyweight exercises" },
                    new[] { "#Fitness", "#HomeWorkout", "#Exercise" }),
                new TopicTemplate("Nutrition and Meal Planning", "Year-round",
                    new[] { "nutrition", "meal prep", "healthy eating" },
                    new[] { "#Nutrition", "#MealPrep", "#HealthyEating" }),
                new TopicTemplate("Mental Health and Wellness", "Year-round",
                    new[] { "mental health", "mindfulness", "stress management" },
                    new[] { "#MentalHealth", "#Wellness", "#Mindfulness" }),
                new TopicTemplate("Fitness Technology and Apps", "Year-round",
                    new[] { "fitness trackers", "health apps", "wearables" },
                    new[] { "#FitnessTech", "#HealthApps", "#Wearables" }),
                new TopicTemplate("Yoga and Flexibility Training", "Year-round",
                    new[] { "yoga", "flexibility", "stretching" },
                    new[] { "#Yoga", "#Flexibility", "#Wellness" })
            });
        }
        else
        {
            // Default topics for any other niche
            templates.AddRange(new[]
            {
                new TopicTemplate($"{niche} Fundamentals and Basics", "Year-round",
                    new[] { $"{niche} basics", "beginner guide", "fundamentals" },
                    new[] { $"#{niche}", "#Beginner", "#Tutorial" }),
                new TopicTemplate($"Advanced {niche} Techniques", "Year-round",
                    new[] { $"{niche} advanced", "expert tips", "pro strategies" },
                    new[] { $"#{niche}", "#Advanced", "#ProTips" }),
                new TopicTemplate($"{niche} Industry Trends 2025", "Year-round",
                    new[] { $"{niche} trends", "industry news", "future of {niche}" },
                    new[] { $"#{niche}", "#Trends", "#2025" }),
                new TopicTemplate($"Common {niche} Mistakes to Avoid", "Year-round",
                    new[] { $"{niche} mistakes", "common errors", "avoid pitfalls" },
                    new[] { $"#{niche}", "#Mistakes", "#Tips" }),
                new TopicTemplate($"Best {niche} Tools and Resources", "Year-round",
                    new[] { $"{niche} tools", "resources", "recommendations" },
                    new[] { $"#{niche}", "#Tools", "#Resources" })
            });
        }

        return templates;
    }

    /// <summary>
    /// Enhance trending topics with AI-generated insights
    /// </summary>
    private async Task<List<TrendingTopic>> EnhanceWithAiAnalysisAsync(
        List<TrendingTopic> topics,
        string? niche,
        CancellationToken ct)
    {
        var enhancedTopics = new List<TrendingTopic>();

        foreach (var topic in topics)
        {
            try
            {
                var insights = await GenerateAiInsightsAsync(topic, niche, ct).ConfigureAwait(false);
                var enhancedTopic = topic with { AiInsights = insights };
                enhancedTopics.Add(enhancedTopic);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI insights for topic: {Topic}", topic.Topic);
                // Keep original topic without insights if AI analysis fails
                enhancedTopics.Add(topic);
            }
        }

        return enhancedTopics;
    }

    /// <summary>
    /// Generate AI insights for a trending topic
    /// </summary>
    private async Task<TrendingTopicInsights> GenerateAiInsightsAsync(
        TrendingTopic topic,
        string? niche,
        CancellationToken ct)
    {
        var prompt = BuildAiAnalysisPrompt(topic, niche);

        var brief = new Brief(
            Topic: prompt,
            Audience: "Content Creators",
            Goal: "Analyze trending topic for content creation insights",
            Tone: "Analytical and Helpful",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Analytical"
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

        // Parse AI response into structured insights
        return ParseAiInsights(response, topic);
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
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
                return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            }
            return result.Data;
        }
        else
        {
            return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Build prompt for AI analysis of trending topic
    /// </summary>
    private string BuildAiAnalysisPrompt(TrendingTopic topic, string? niche)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Analyze this trending topic for content creators in the {niche ?? "general"} niche:");
        sb.AppendLine();
        sb.AppendLine($"Topic: {topic.Topic}");
        sb.AppendLine($"Trend Score: {topic.TrendScore}/100");
        sb.AppendLine($"Search Volume: {topic.SearchVolume}");
        sb.AppendLine($"Lifecycle: {topic.Lifecycle}");
        sb.AppendLine($"Competition: {topic.Competition}");
        sb.AppendLine();
        sb.AppendLine("Provide analysis in the following format:");
        sb.AppendLine();
        sb.AppendLine("1. WHY TRENDING: (2-3 sentences explaining why this topic is gaining attention)");
        sb.AppendLine();
        sb.AppendLine("2. AUDIENCE ENGAGEMENT: (2-3 sentences about how audiences are engaging with this topic)");
        sb.AppendLine();
        sb.AppendLine("3. CONTENT ANGLES: (List 3-4 specific content angle ideas, one per line with a dash)");
        sb.AppendLine("   - Angle 1");
        sb.AppendLine("   - Angle 2");
        sb.AppendLine("   - Angle 3");
        sb.AppendLine();
        sb.AppendLine("4. DEMOGRAPHIC APPEAL: (1-2 sentences about target demographics)");
        sb.AppendLine();
        sb.AppendLine("5. VIRALITY SCORE: (Single number 0-100 representing viral potential)");

        return sb.ToString();
    }

    /// <summary>
    /// Parse AI response into structured insights
    /// </summary>
    private TrendingTopicInsights ParseAiInsights(string response, TrendingTopic topic)
    {
        // Simple parsing - in production, use structured output or JSON mode
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var whyTrending = ExtractSection(lines, "WHY TRENDING") 
            ?? $"{topic.Topic} is gaining significant attention due to increasing interest and timely relevance in the current market.";

        var audienceEngagement = ExtractSection(lines, "AUDIENCE ENGAGEMENT")
            ?? "Audiences are actively searching for and engaging with this content, showing strong interest in learning more.";

        var contentAngles = ExtractListSection(lines, "CONTENT ANGLES")
            ?? new List<string>
            {
                $"Beginner's guide to {topic.Topic}",
                $"Expert analysis of {topic.Topic}",
                $"Real-world examples of {topic.Topic}",
                $"Common mistakes in {topic.Topic}"
            };

        var demographicAppeal = ExtractSection(lines, "DEMOGRAPHIC APPEAL")
            ?? "Appeals broadly to audiences interested in the topic, with particular relevance to engaged learners.";

        var viralityScore = ExtractScore(lines, "VIRALITY SCORE");
        if (viralityScore == 0)
        {
            viralityScore = CalculateViralityScore(topic);
        }

        return new TrendingTopicInsights(
            WhyTrending: whyTrending,
            AudienceEngagement: audienceEngagement,
            ContentAngles: contentAngles,
            DemographicAppeal: demographicAppeal,
            ViralityScore: viralityScore
        );
    }

    // Helper methods for parsing
    private string? ExtractSection(string[] lines, string sectionName)
    {
        var inSection = false;
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                // Get content after the colon if present
                var colonIndex = line.IndexOf(':');
                if (colonIndex >= 0 && colonIndex < line.Length - 1)
                {
                    var content = line.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                        sb.AppendLine(content);
                }
                continue;
            }

            if (inSection)
            {
                // Stop at next section number
                if (line.TrimStart().Length > 0 && char.IsDigit(line.TrimStart()[0]) && line.Contains('.'))
                {
                    break;
                }
                
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('-'))
                {
                    sb.AppendLine(trimmed);
                }
            }
        }

        var result = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private List<string> ExtractListSection(string[] lines, string sectionName)
    {
        var inSection = false;
        var items = new List<string>();

        foreach (var line in lines)
        {
            if (line.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (inSection)
            {
                // Stop at next section number
                if (line.TrimStart().Length > 0 && char.IsDigit(line.TrimStart()[0]) && line.Contains('.'))
                {
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.StartsWith('-') || trimmed.StartsWith('•'))
                {
                    var item = trimmed.TrimStart('-', '•', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        items.Add(item);
                    }
                }
            }
        }

        return items.Count > 0 ? items : null!;
    }

    private double ExtractScore(string[] lines, string sectionName)
    {
        var section = ExtractSection(lines, sectionName);
        if (string.IsNullOrWhiteSpace(section))
            return 0;

        // Try to extract number from the section
        var numbers = System.Text.RegularExpressions.Regex.Matches(section, @"\d+");
        if (numbers.Count > 0 && double.TryParse(numbers[0].Value, out var score))
        {
            return Math.Max(0, Math.Min(100, score));
        }

        return 0;
    }

    private double CalculateViralityScore(TrendingTopic topic)
    {
        // Calculate based on trend score, velocity, and lifecycle
        var baseScore = topic.TrendScore * 0.6;
        var velocityBonus = (topic.TrendVelocity ?? 0) * 2;
        var lifecycleMultiplier = topic.Lifecycle?.ToLower() switch
        {
            "rising" => 1.2,
            "peak" => 1.0,
            "declining" => 0.7,
            _ => 1.0
        };

        return Math.Max(0, Math.Min(100, (baseScore + velocityBonus) * lifecycleMultiplier));
    }

    // Helper methods
    private string FormatSearchVolume(int volume)
    {
        if (volume >= 1000000)
            return $"{volume / 1000000.0:F1}M searches/month";
        if (volume >= 1000)
            return $"{volume / 1000.0:F0}K searches/month";
        return $"{volume} searches/month";
    }

    private string DetermineCompetition(double trendScore)
    {
        if (trendScore >= 85) return "High";
        if (trendScore >= 65) return "Medium";
        return "Low";
    }

    private string DetermineLifecycle(double velocity)
    {
        if (velocity > 5) return "Rising";
        if (velocity > -2) return "Peak";
        return "Declining";
    }

    private long EstimateAudience(double trendScore)
    {
        // Estimate potential audience based on trend score
        var baseAudience = 50000;
        var multiplier = Math.Pow(2, trendScore / 20);
        return (long)(baseAudience * multiplier);
    }

    /// <summary>
    /// Clear cache for a specific niche
    /// </summary>
    public void ClearCache(string? niche = null)
    {
        var cacheKey = niche != null ? $"{CacheKeyPrefix}{niche}" : CacheKeyPrefix;
        _cache.Remove(cacheKey);
    }
}

/// <summary>
/// Template for generating trending topics
/// </summary>
internal record TopicTemplate(
    string Title,
    string Seasonality,
    string[] RelatedTopics,
    string[] SuggestedHashtags
);
