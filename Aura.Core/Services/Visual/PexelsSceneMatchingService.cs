using System.Text.RegularExpressions;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.StockMedia;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service that provides intelligent scene matching for Pexels image searches.
/// Extracts keywords, builds optimized queries, applies orientation filtering, and scores results.
/// </summary>
public class PexelsSceneMatchingService
{
    private readonly ILogger<PexelsSceneMatchingService> _logger;
    private readonly VisualKeywordExtractor _keywordExtractor;
    private readonly PexelsMatchingConfig _config;

    /// <summary>
    /// Common stop words to filter from significant word extraction.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by"
    };

    public PexelsSceneMatchingService(
        ILogger<PexelsSceneMatchingService> logger,
        VisualKeywordExtractor keywordExtractor,
        PexelsMatchingConfig config)
    {
        _logger = logger;
        _keywordExtractor = keywordExtractor;
        _config = config;
    }

    /// <summary>
    /// Performs intelligent scene matching to find relevant Pexels images for a scene.
    /// </summary>
    /// <param name="scene">The scene to find images for.</param>
    /// <param name="aspect">Aspect ratio of the video.</param>
    /// <param name="style">Visual style (optional).</param>
    /// <param name="pexelsSearchFunc">Function to execute Pexels search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Scored and filtered stock media results.</returns>
    public async Task<IReadOnlyList<ScoredStockMediaResult>> FindMatchingImagesAsync(
        Scene scene,
        Aspect aspect,
        string? style,
        Func<StockMediaSearchRequest, CancellationToken, Task<List<StockMediaResult>>> pexelsSearchFunc,
        CancellationToken ct = default)
    {
        if (!_config.EnableSemanticMatching)
        {
            _logger.LogDebug(
                "Semantic matching disabled for scene {SceneIndex}, using basic search",
                scene.Index);
            return await FallbackSearchAsync(scene, pexelsSearchFunc, ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Starting intelligent scene matching for scene {SceneIndex}: {Heading}",
            scene.Index,
            scene.Heading);

        // Step 1: Extract keywords from scene
        var keywords = _keywordExtractor.ExtractKeywords(
            scene.Heading,
            scene.Script,
            _config.MaxKeywordsInQuery,
            _config.VisualTermBoost);

        _logger.LogDebug(
            "Extracted keywords for scene {SceneIndex}: {Keywords}",
            scene.Index,
            string.Join(", ", keywords));

        // Step 2: Build optimized search query
        var searchQuery = _keywordExtractor.BuildSearchQuery(
            keywords,
            scene.Heading,
            style,
            _config.MaxKeywordsInQuery);

        _logger.LogDebug(
            "Built search query for scene {SceneIndex}: {Query}",
            scene.Index,
            searchQuery);

        // Step 3: Determine orientation from aspect ratio
        string? orientation = null;
        if (_config.UseOrientationFiltering)
        {
            orientation = MapAspectToOrientation(aspect);
            _logger.LogDebug(
                "Using orientation filter for scene {SceneIndex}: {Orientation}",
                scene.Index,
                orientation);
        }

        // Step 4: Execute Pexels search with intelligent parameters
        var searchRequest = new StockMediaSearchRequest
        {
            Query = searchQuery,
            Type = StockMediaType.Image,
            Count = _config.MaxCandidatesPerScene,
            Orientation = orientation,
            SafeSearchEnabled = true
        };

        var results = await pexelsSearchFunc(searchRequest, ct).ConfigureAwait(false);

        if (results.Count == 0 && _config.FallbackToBasicSearch)
        {
            _logger.LogWarning(
                "No results from intelligent search for scene {SceneIndex}, falling back to basic search",
                scene.Index);
            return await FallbackSearchAsync(scene, pexelsSearchFunc, ct).ConfigureAwait(false);
        }

        // Step 5: Score results for relevance
        var scoredResults = ScoreResults(results, keywords, scene.Heading, style);

        // Step 6: Filter by minimum threshold
        var filteredResults = scoredResults
            .Where(r => r.RelevanceScore.MeetsThreshold)
            .OrderByDescending(r => r.RelevanceScore.Score)
            .ToList();

        _logger.LogInformation(
            "Scene {SceneIndex} matching complete: {Total} candidates, {Filtered} passed threshold",
            scene.Index,
            scoredResults.Count,
            filteredResults.Count);

        return filteredResults;
    }

    /// <summary>
    /// Scores a list of stock media results for relevance to scene keywords and context.
    /// </summary>
    public IReadOnlyList<ScoredStockMediaResult> ScoreResults(
        IReadOnlyList<StockMediaResult> results,
        IReadOnlyList<string> keywords,
        string? heading,
        string? style)
    {
        var scored = new List<ScoredStockMediaResult>();

        foreach (var result in results)
        {
            var relevanceScore = CalculateRelevanceScore(result, keywords, heading, style);
            scored.Add(new ScoredStockMediaResult
            {
                Result = result,
                RelevanceScore = relevanceScore
            });
        }

        return scored.OrderByDescending(r => r.RelevanceScore.Score).ToList();
    }

    /// <summary>
    /// Calculates relevance score for a single image result.
    /// </summary>
    private ImageSceneRelevanceScore CalculateRelevanceScore(
        StockMediaResult result,
        IReadOnlyList<string> keywords,
        string? heading,
        string? style)
    {
        // Start with base score
        var keywordMatchScore = 0.0;
        var headingRelevanceScore = 0.0;
        var styleAlignmentScore = 0.0;
        var matchedKeywords = new List<string>();

        // Get searchable text from result metadata
        var resultText = BuildResultText(result);

        // Calculate keyword match score (up to 50 points)
        foreach (var keyword in keywords)
        {
            if (ContainsKeyword(resultText, keyword))
            {
                matchedKeywords.Add(keyword);
                keywordMatchScore += 50.0 / keywords.Count;
            }
        }

        // Calculate heading relevance score (up to 25 points)
        if (!string.IsNullOrEmpty(heading))
        {
            var headingWords = ExtractSignificantWords(heading);
            var headingMatches = headingWords.Count(hw => ContainsKeyword(resultText, hw));
            headingRelevanceScore = headingWords.Count > 0
                ? (25.0 * headingMatches / headingWords.Count)
                : 0.0;
        }

        // Calculate style alignment score (up to 25 points)
        if (!string.IsNullOrEmpty(style))
        {
            var styleWords = ExtractSignificantWords(style);
            var styleMatches = styleWords.Count(sw => ContainsKeyword(resultText, sw));
            styleAlignmentScore = styleWords.Count > 0
                ? (25.0 * styleMatches / styleWords.Count)
                : _config.BaseRelevanceScore / 4; // Default credit if style not specified
        }
        else
        {
            styleAlignmentScore = _config.BaseRelevanceScore / 4; // Default credit
        }

        var totalScore = _config.BaseRelevanceScore + keywordMatchScore + headingRelevanceScore + styleAlignmentScore;
        totalScore = Math.Min(100.0, totalScore); // Cap at 100

        var reasoning = BuildReasoning(matchedKeywords, keywordMatchScore, headingRelevanceScore, styleAlignmentScore);

        return new ImageSceneRelevanceScore
        {
            ImageId = result.Id,
            Score = totalScore,
            KeywordMatchScore = keywordMatchScore,
            HeadingRelevanceScore = headingRelevanceScore,
            StyleAlignmentScore = styleAlignmentScore,
            MatchedKeywords = matchedKeywords,
            Reasoning = reasoning,
            MeetsThreshold = totalScore >= _config.MinimumRelevanceScore
        };
    }

    /// <summary>
    /// Performs a basic fallback search without intelligent query building.
    /// </summary>
    private async Task<IReadOnlyList<ScoredStockMediaResult>> FallbackSearchAsync(
        Scene scene,
        Func<StockMediaSearchRequest, CancellationToken, Task<List<StockMediaResult>>> pexelsSearchFunc,
        CancellationToken ct)
    {
        // Use simple heading-based search
        var query = !string.IsNullOrWhiteSpace(scene.Heading)
            ? scene.Heading
            : ExtractSimpleQuery(scene.Script);

        var searchRequest = new StockMediaSearchRequest
        {
            Query = query,
            Type = StockMediaType.Image,
            Count = _config.MaxCandidatesPerScene,
            SafeSearchEnabled = true
        };

        var results = await pexelsSearchFunc(searchRequest, ct).ConfigureAwait(false);

        // Apply basic scoring
        return results.Select(r => new ScoredStockMediaResult
        {
            Result = r,
            RelevanceScore = ImageSceneRelevanceScore.BasicMatch(r.Id, _config.BaseRelevanceScore)
        }).ToList();
    }

    /// <summary>
    /// Maps video aspect ratio to Pexels orientation parameter.
    /// </summary>
    private static string MapAspectToOrientation(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.Vertical9x16 => "portrait",
            Aspect.Square1x1 => "square",
            Aspect.Widescreen16x9 => "landscape",
            _ => "landscape"
        };
    }

    /// <summary>
    /// Builds searchable text from result metadata.
    /// </summary>
    private static string BuildResultText(StockMediaResult result)
    {
        var parts = new List<string>();

        if (result.Metadata.TryGetValue("photographer", out var photographer) && !string.IsNullOrEmpty(photographer))
        {
            parts.Add(photographer);
        }

        if (result.Metadata.TryGetValue("alt", out var alt) && !string.IsNullOrEmpty(alt))
        {
            parts.Add(alt);
        }

        // Include the ID in case it contains descriptive information
        parts.Add(result.Id);

        return string.Join(" ", parts).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if result text contains a keyword (case-insensitive, word boundary aware).
    /// Uses word boundary regex to avoid partial matches (e.g., 'art' matching 'heart').
    /// </summary>
    private static bool ContainsKeyword(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            return false;

        // Use word boundary matching to avoid partial matches
        var pattern = $@"\b{Regex.Escape(keyword)}\b";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Extracts significant words from text (filters stop words).
    /// </summary>
    private static IReadOnlyList<string> ExtractSignificantWords(string text)
    {
        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '-', '_', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Extracts a simple search query from script text.
    /// </summary>
    private static string ExtractSimpleQuery(string? script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return "background";

        var words = script.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 4)
            .Take(3);

        return string.Join(" ", words);
    }

    /// <summary>
    /// Builds human-readable reasoning for the score.
    /// </summary>
    private static string BuildReasoning(
        List<string> matchedKeywords,
        double keywordScore,
        double headingScore,
        double styleScore)
    {
        var parts = new List<string>();

        if (matchedKeywords.Count > 0)
        {
            parts.Add($"Matched keywords: {string.Join(", ", matchedKeywords)}");
        }

        parts.Add($"Keyword match: {keywordScore:F1}/50");
        parts.Add($"Heading relevance: {headingScore:F1}/25");
        parts.Add($"Style alignment: {styleScore:F1}/25");

        return string.Join(". ", parts);
    }
}

/// <summary>
/// Stock media result with relevance scoring.
/// </summary>
public record ScoredStockMediaResult
{
    /// <summary>
    /// The original stock media result.
    /// </summary>
    public StockMediaResult Result { get; init; } = null!;

    /// <summary>
    /// Relevance score for this result.
    /// </summary>
    public ImageSceneRelevanceScore RelevanceScore { get; init; } = null!;
}
