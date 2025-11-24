using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Ideation;

/// <summary>
/// Service for performing web searches to gather real-time information
/// Supports Google Custom Search API, NewsAPI, and other search providers
/// </summary>
public class WebSearchService
{
    private readonly ILogger<WebSearchService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    private const string GoogleCustomSearchBaseUrl = "https://www.googleapis.com/customsearch/v1";
    private const string NewsApiBaseUrl = "https://newsapi.org/v2";
    private const string SerpApiBaseUrl = "https://serpapi.com/search.json";
    
    public WebSearchService(
        ILogger<WebSearchService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Search the web for current information about a topic
    /// </summary>
    public async Task<WebSearchResult> SearchAsync(
        string query,
        int maxResults = 10,
        SearchSource source = SearchSource.Auto,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Searching web for: {Query} (max results: {MaxResults}, source: {Source})", 
            query, maxResults, source);

        // Auto-select best available source
        if (source == SearchSource.Auto)
        {
            source = DetermineBestSource();
        }

        try
        {
            return source switch
            {
                SearchSource.GoogleCustomSearch => await SearchGoogleCustomAsync(query, maxResults, ct).ConfigureAwait(false),
                SearchSource.NewsApi => await SearchNewsApiAsync(query, maxResults, ct).ConfigureAwait(false),
                SearchSource.SerpApi => await SearchSerpApiAsync(query, maxResults, ct).ConfigureAwait(false),
                _ => await SearchGoogleCustomAsync(query, maxResults, ct).ConfigureAwait(false)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Web search failed with primary source {Source}, attempting fallback", source);
            // Try fallback sources
            return await TryFallbackSearchAsync(query, maxResults, source, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Search Google Trends for trending information
    /// </summary>
    public async Task<List<TrendingSearchResult>> SearchTrendsAsync(
        string query,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Searching Google Trends for: {Query}", query);
        
        // Use SerpApi for Google Trends if available, otherwise use web search as proxy
        var serpApiKey = _configuration["SerpApi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(serpApiKey))
        {
            try
            {
                return await SearchTrendsViaSerpApiAsync(query, maxResults, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SerpApi trends search failed, using web search as fallback");
            }
        }

        // Fallback: Use regular web search with "trending" modifier
        var searchResult = await SearchAsync($"trending {query}", maxResults, SearchSource.Auto, ct).ConfigureAwait(false);
        return searchResult.Items.Select(item => new TrendingSearchResult(
            Title: item.Title,
            Snippet: item.Snippet,
            Url: item.Url,
            RelevanceScore: item.RelevanceScore,
            TrendScore: CalculateTrendScore(item)
        )).ToList();
    }

    /// <summary>
    /// Get competitive intelligence by searching for similar content
    /// </summary>
    public async Task<CompetitiveIntelligenceResult> GetCompetitiveIntelligenceAsync(
        string topic,
        int maxResults = 20,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Gathering competitive intelligence for: {Topic}", topic);

        var queries = new[]
        {
            topic,
            $"\"{topic}\" video",
            $"{topic} tutorial",
            $"{topic} guide",
            $"{topic} explained"
        };

        var allResults = new List<WebSearchItem>();
        var seenUrls = new HashSet<string>();

        foreach (var query in queries)
        {
            try
            {
                var result = await SearchAsync(query, maxResults / queries.Length, SearchSource.Auto, ct).ConfigureAwait(false);
                foreach (var item in result.Items)
                {
                    if (!seenUrls.Contains(item.Url))
                    {
                        allResults.Add(item);
                        seenUrls.Add(item.Url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search for competitive intelligence query: {Query}", query);
            }
        }

        return AnalyzeCompetitiveIntelligence(allResults, topic);
    }

    /// <summary>
    /// Analyze content gaps by comparing search results
    /// </summary>
    public async Task<ContentGapAnalysisResult> AnalyzeContentGapsAsync(
        string topic,
        List<string>? relatedTopics = null,
        int maxResults = 15,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content gaps for: {Topic}", topic);

        relatedTopics ??= new List<string>();

        // Search for main topic
        var mainTopicResults = await SearchAsync(topic, maxResults, SearchSource.Auto, ct).ConfigureAwait(false);
        
        // Search for related topics
        var relatedResults = new List<WebSearchItem>();
        foreach (var relatedTopic in relatedTopics.Take(5))
        {
            try
            {
                var result = await SearchAsync(relatedTopic, maxResults / (relatedTopics.Count + 1), 
                    SearchSource.Auto, ct).ConfigureAwait(false);
                relatedResults.AddRange(result.Items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search related topic: {Topic}", relatedTopic);
            }
        }

        return AnalyzeContentGaps(mainTopicResults.Items, relatedResults, topic);
    }

    #region Private Methods

    private SearchSource DetermineBestSource()
    {
        // Check which API keys are available
        var googleApiKey = _configuration["Google:CustomSearchApiKey"];
        var googleCx = _configuration["Google:CustomSearchEngineId"];
        var newsApiKey = _configuration["NewsApi:ApiKey"];
        var serpApiKey = _configuration["SerpApi:ApiKey"];

        if (!string.IsNullOrWhiteSpace(serpApiKey))
        {
            return SearchSource.SerpApi; // SerpApi is most comprehensive
        }
        if (!string.IsNullOrWhiteSpace(googleApiKey) && !string.IsNullOrWhiteSpace(googleCx))
        {
            return SearchSource.GoogleCustomSearch;
        }
        if (!string.IsNullOrWhiteSpace(newsApiKey))
        {
            return SearchSource.NewsApi;
        }

        _logger.LogWarning("No web search API keys configured. Web search features will be limited.");
        return SearchSource.GoogleCustomSearch; // Default, will fail gracefully
    }

    private async Task<WebSearchResult> SearchGoogleCustomAsync(
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var apiKey = _configuration["Google:CustomSearchApiKey"];
        var cx = _configuration["Google:CustomSearchEngineId"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cx))
        {
            throw new InvalidOperationException(
                "Google Custom Search API key or Engine ID not configured. " +
                "Set Google:CustomSearchApiKey and Google:CustomSearchEngineId in configuration.");
        }

        var httpClient = _httpClientFactory.CreateClient();
        var numResults = Math.Min(maxResults, 10); // Google Custom Search max is 10 per request
        
        var url = $"{GoogleCustomSearchBaseUrl}?key={Uri.EscapeDataString(apiKey)}" +
                  $"&cx={Uri.EscapeDataString(cx)}" +
                  $"&q={Uri.EscapeDataString(query)}" +
                  $"&num={numResults}";

        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var items = new List<WebSearchItem>();
        if (root.TryGetProperty("items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                items.Add(new WebSearchItem(
                    Title: item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Snippet: item.TryGetProperty("snippet", out var snippet) ? snippet.GetString() ?? "" : "",
                    Url: item.TryGetProperty("link", out var link) ? link.GetString() ?? "" : "",
                    RelevanceScore: 85.0, // Google results are generally relevant
                    PublishedDate: ExtractPublishedDate(item)
                ));
            }
        }

        return new WebSearchResult(
            Query: query,
            Items: items,
            TotalResults: root.TryGetProperty("searchInformation", out var searchInfo) &&
                         searchInfo.TryGetProperty("totalResults", out var total) 
                         ? total.GetString() ?? "0" 
                         : "0",
            Source: "Google Custom Search"
        );
    }

    private async Task<WebSearchResult> SearchNewsApiAsync(
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var apiKey = _configuration["NewsApi:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "NewsAPI key not configured. Set NewsApi:ApiKey in configuration.");
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        var url = $"{NewsApiBaseUrl}/everything?q={Uri.EscapeDataString(query)}" +
                  $"&pageSize={Math.Min(maxResults, 100)}" +
                  "&sortBy=relevancy" +
                  "&language=en";

        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var items = new List<WebSearchItem>();
        if (root.TryGetProperty("articles", out var articles))
        {
            foreach (var article in articles.EnumerateArray())
            {
                items.Add(new WebSearchItem(
                    Title: article.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Snippet: article.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                    Url: article.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "",
                    RelevanceScore: 80.0,
                    PublishedDate: article.TryGetProperty("publishedAt", out var pubDate) 
                        ? DateTime.TryParse(pubDate.GetString(), out var dt) ? dt : null 
                        : null
                ));
            }
        }

        return new WebSearchResult(
            Query: query,
            Items: items,
            TotalResults: root.TryGetProperty("totalResults", out var total) 
                ? total.GetInt32().ToString() 
                : "0",
            Source: "NewsAPI"
        );
    }

    private async Task<WebSearchResult> SearchSerpApiAsync(
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var apiKey = _configuration["SerpApi:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "SerpAPI key not configured. Set SerpApi:ApiKey in configuration.");
        }

        var httpClient = _httpClientFactory.CreateClient();
        var url = $"{SerpApiBaseUrl}?engine=google&q={Uri.EscapeDataString(query)}" +
                  $"&api_key={Uri.EscapeDataString(apiKey)}" +
                  $"&num={Math.Min(maxResults, 100)}";

        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var items = new List<WebSearchItem>();
        if (root.TryGetProperty("organic_results", out var organicResults))
        {
            foreach (var result in organicResults.EnumerateArray().Take(maxResults))
            {
                items.Add(new WebSearchItem(
                    Title: result.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Snippet: result.TryGetProperty("snippet", out var snippet) ? snippet.GetString() ?? "" : "",
                    Url: result.TryGetProperty("link", out var link) ? link.GetString() ?? "" : "",
                    RelevanceScore: result.TryGetProperty("relevance_score", out var relScore) 
                        ? relScore.GetDouble() * 100 
                        : 75.0,
                    PublishedDate: ExtractPublishedDate(result)
                ));
            }
        }

        return new WebSearchResult(
            Query: query,
            Items: items,
            TotalResults: root.TryGetProperty("search_information", out var searchInfo) &&
                         searchInfo.TryGetProperty("total_results", out var total) 
                         ? total.GetInt64().ToString() 
                         : "0",
            Source: "SerpAPI"
        );
    }

    private async Task<List<TrendingSearchResult>> SearchTrendsViaSerpApiAsync(
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var apiKey = _configuration["SerpApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("SerpAPI key not configured");
        }

        var httpClient = _httpClientFactory.CreateClient();
        var url = $"{SerpApiBaseUrl}?engine=google_trends&q={Uri.EscapeDataString(query)}" +
                  $"&api_key={Uri.EscapeDataString(apiKey)}";

        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var results = new List<TrendingSearchResult>();
        if (root.TryGetProperty("trending_searches", out var trending))
        {
            foreach (var item in trending.EnumerateArray().Take(maxResults))
            {
                results.Add(new TrendingSearchResult(
                    Title: item.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    Snippet: item.TryGetProperty("snippet", out var snippet) ? snippet.GetString() ?? "" : "",
                    Url: item.TryGetProperty("link", out var link) ? link.GetString() ?? "" : "",
                    RelevanceScore: 90.0,
                    TrendScore: item.TryGetProperty("trending_score", out var score) 
                        ? score.GetDouble() 
                        : 85.0
                ));
            }
        }

        return results;
    }

    private async Task<WebSearchResult> TryFallbackSearchAsync(
        string query,
        int maxResults,
        SearchSource failedSource,
        CancellationToken ct)
    {
        var fallbackSources = new[] { SearchSource.NewsApi, SearchSource.GoogleCustomSearch, SearchSource.SerpApi };
        
        foreach (var source in fallbackSources)
        {
            if (source == failedSource) continue;

            try
            {
                return source switch
                {
                    SearchSource.NewsApi => await SearchNewsApiAsync(query, maxResults, ct).ConfigureAwait(false),
                    SearchSource.GoogleCustomSearch => await SearchGoogleCustomAsync(query, maxResults, ct).ConfigureAwait(false),
                    SearchSource.SerpApi => await SearchSerpApiAsync(query, maxResults, ct).ConfigureAwait(false),
                    _ => throw new NotSupportedException($"Source {source} not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback search with {Source} also failed", source);
            }
        }

        // All sources failed - return empty result
        _logger.LogError("All web search sources failed for query: {Query}", query);
        return new WebSearchResult(
            Query: query,
            Items: new List<WebSearchItem>(),
            TotalResults: "0",
            Source: "None (all sources failed)"
        );
    }

    private DateTime? ExtractPublishedDate(JsonElement item)
    {
        // Try various date field names
        var dateFields = new[] { "publishedAt", "publishDate", "date", "published_date", "pubDate" };
        foreach (var field in dateFields)
        {
            if (item.TryGetProperty(field, out var dateProp))
            {
                var dateStr = dateProp.GetString();
                if (DateTime.TryParse(dateStr, out var date))
                {
                    return date;
                }
            }
        }
        return null;
    }

    private double CalculateTrendScore(WebSearchItem item)
    {
        var score = 50.0; // Base score
        
        // Boost if recent
        if (item.PublishedDate.HasValue)
        {
            var daysAgo = (DateTime.UtcNow - item.PublishedDate.Value).TotalDays;
            if (daysAgo < 7) score += 30;
            else if (daysAgo < 30) score += 15;
        }
        
        // Boost based on relevance
        score += item.RelevanceScore * 0.2;
        
        return Math.Min(100, score);
    }

    private CompetitiveIntelligenceResult AnalyzeCompetitiveIntelligence(
        List<WebSearchItem> results,
        string topic)
    {
        var domains = results
            .Select(r => new Uri(r.Url).Host)
            .GroupBy(h => h)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new DomainAnalysis(
                Domain: g.Key,
                ContentCount: g.Count(),
                AverageRelevance: results.Where(r => new Uri(r.Url).Host == g.Key)
                    .Average(r => r.RelevanceScore)
            ))
            .ToList();

        var contentTypes = results
            .Select(r => DetectContentType(r))
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        var commonKeywords = ExtractCommonKeywords(results, topic);

        return new CompetitiveIntelligenceResult(
            Topic: topic,
            TotalCompetitors: domains.Count,
            TopDomains: domains,
            ContentTypeDistribution: contentTypes,
            CommonKeywords: commonKeywords,
            AverageRelevance: results.Any() ? results.Average(r => r.RelevanceScore) : 0,
            SaturationLevel: CalculateSaturationLevel(results.Count)
        );
    }

    private ContentGapAnalysisResult AnalyzeContentGaps(
        List<WebSearchItem> mainTopicResults,
        List<WebSearchItem> relatedResults,
        string topic)
    {
        var mainKeywords = ExtractKeywords(mainTopicResults);
        var relatedKeywords = ExtractKeywords(relatedResults);
        
        var gapKeywords = relatedKeywords
            .Where(k => !mainKeywords.Contains(k.Key))
            .OrderByDescending(k => k.Value)
            .Take(10)
            .Select(k => k.Key)
            .ToList();

        var oversaturatedTopics = mainTopicResults
            .GroupBy(r => ExtractPrimaryTopic(r))
            .Where(g => g.Count() > 3)
            .Select(g => g.Key)
            .ToList();

        var uniqueAngles = IdentifyUniqueAngles(mainTopicResults, relatedResults);

        return new ContentGapAnalysisResult(
            Topic: topic,
            GapKeywords: gapKeywords,
            OversaturatedTopics: oversaturatedTopics,
            UniqueAngles: uniqueAngles,
            OpportunityScore: CalculateOpportunityScore(gapKeywords.Count, oversaturatedTopics.Count),
            RecommendedFocus: gapKeywords.Take(3).ToList()
        );
    }

    private string DetectContentType(WebSearchItem item)
    {
        var url = item.Url.ToLowerInvariant();
        var title = item.Title.ToLowerInvariant();
        var snippet = item.Snippet.ToLowerInvariant();

        if (url.Contains("youtube.com") || title.Contains("video") || snippet.Contains("watch"))
            return "Video";
        if (url.Contains("blog") || title.Contains("blog") || snippet.Contains("article"))
            return "Blog";
        if (title.Contains("tutorial") || snippet.Contains("how to") || snippet.Contains("step by step"))
            return "Tutorial";
        if (title.Contains("guide") || snippet.Contains("complete guide"))
            return "Guide";
        if (title.Contains("review") || snippet.Contains("review"))
            return "Review";
        
        return "General";
    }

    private Dictionary<string, int> ExtractCommonKeywords(List<WebSearchItem> results, string topic)
    {
        var keywords = new Dictionary<string, int>();
        var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };
        
        foreach (var result in results)
        {
            var text = $"{result.Title} {result.Snippet}".ToLowerInvariant();
            var words = text.Split(new[] { ' ', '.', ',', '!', '?', ':', ';', '-', '(', ')' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                var cleanWord = word.Trim().Trim('"', '\'', '`');
                if (cleanWord.Length > 3 && !stopWords.Contains(cleanWord) && cleanWord != topic.ToLowerInvariant())
                {
                    keywords[cleanWord] = keywords.GetValueOrDefault(cleanWord, 0) + 1;
                }
            }
        }

        return keywords
            .OrderByDescending(k => k.Value)
            .Take(20)
            .ToDictionary(k => k.Key, k => k.Value);
    }

    private Dictionary<string, int> ExtractKeywords(List<WebSearchItem> results)
    {
        return ExtractCommonKeywords(results, "");
    }

    private string ExtractPrimaryTopic(WebSearchItem item)
    {
        // Extract the main topic from title (first few words)
        var words = item.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(5));
    }

    private List<string> IdentifyUniqueAngles(List<WebSearchItem> mainResults, List<WebSearchItem> relatedResults)
    {
        var mainAngles = mainResults.Select(r => ExtractAngle(r)).ToList();
        var relatedAngles = relatedResults.Select(r => ExtractAngle(r)).ToList();
        
        return relatedAngles
            .Where(a => !mainAngles.Contains(a))
            .Distinct()
            .Take(5)
            .ToList();
    }

    private string ExtractAngle(WebSearchItem item)
    {
        var title = item.Title.ToLowerInvariant();
        var snippet = item.Snippet.ToLowerInvariant();
        var text = $"{title} {snippet}";

        if (text.Contains("beginner") || text.Contains("getting started"))
            return "Beginner-Friendly";
        if (text.Contains("advanced") || text.Contains("expert"))
            return "Advanced";
        if (text.Contains("comparison") || text.Contains("vs"))
            return "Comparison";
        if (text.Contains("case study") || text.Contains("real world"))
            return "Case Study";
        if (text.Contains("mistakes") || text.Contains("common errors"))
            return "Mistakes to Avoid";
        if (text.Contains("tips") || text.Contains("tricks"))
            return "Tips & Tricks";
        
        return "General";
    }

    private string CalculateSaturationLevel(int contentCount)
    {
        if (contentCount < 10) return "Low";
        if (contentCount < 30) return "Medium";
        if (contentCount < 50) return "High";
        return "Very High";
    }

    private double CalculateOpportunityScore(int gapCount, int saturatedCount)
    {
        var score = 50.0;
        score += gapCount * 5; // More gaps = more opportunity
        score -= saturatedCount * 3; // More saturation = less opportunity
        return Math.Clamp(score, 0, 100);
    }

    #endregion
}

/// <summary>
/// Search source options
/// </summary>
public enum SearchSource
{
    Auto,
    GoogleCustomSearch,
    NewsApi,
    SerpApi
}

/// <summary>
/// Result of a web search
/// </summary>
public record WebSearchResult(
    string Query,
    List<WebSearchItem> Items,
    string TotalResults,
    string Source
);

/// <summary>
/// Individual search result item
/// </summary>
public record WebSearchItem(
    string Title,
    string Snippet,
    string Url,
    double RelevanceScore,
    DateTime? PublishedDate = null
);

/// <summary>
/// Trending search result
/// </summary>
public record TrendingSearchResult(
    string Title,
    string Snippet,
    string Url,
    double RelevanceScore,
    double TrendScore
);

/// <summary>
/// Competitive intelligence analysis result
/// </summary>
public record CompetitiveIntelligenceResult(
    string Topic,
    int TotalCompetitors,
    List<DomainAnalysis> TopDomains,
    Dictionary<string, int> ContentTypeDistribution,
    Dictionary<string, int> CommonKeywords,
    double AverageRelevance,
    string SaturationLevel
);

/// <summary>
/// Domain analysis for competitive intelligence
/// </summary>
public record DomainAnalysis(
    string Domain,
    int ContentCount,
    double AverageRelevance
);

/// <summary>
/// Content gap analysis result
/// </summary>
public record ContentGapAnalysisResult(
    string Topic,
    List<string> GapKeywords,
    List<string> OversaturatedTopics,
    List<string> UniqueAngles,
    double OpportunityScore,
    List<string> RecommendedFocus
);

