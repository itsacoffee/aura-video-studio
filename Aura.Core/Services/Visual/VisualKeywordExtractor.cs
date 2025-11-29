using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service to extract visual keywords from scene text for intelligent image matching.
/// Filters stop words and prioritizes visual/style terms.
/// </summary>
public class VisualKeywordExtractor
{
    private readonly ILogger<VisualKeywordExtractor> _logger;

    /// <summary>
    /// Common English stop words to filter out from keyword extraction.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "up", "about", "into", "through", "during",
        "before", "after", "above", "below", "between", "under", "over", "again",
        "further", "then", "once", "here", "there", "when", "where", "why", "how",
        "all", "each", "every", "both", "few", "more", "most", "other", "some",
        "such", "no", "nor", "not", "only", "own", "same", "so", "than", "too",
        "very", "just", "can", "will", "should", "now", "also", "it", "its",
        "this", "that", "these", "those", "am", "is", "are", "was", "were", "be",
        "been", "being", "have", "has", "had", "do", "does", "did", "having",
        "i", "you", "he", "she", "we", "they", "me", "him", "her", "us", "them",
        "my", "your", "his", "our", "their", "what", "which", "who", "whom"
    };

    /// <summary>
    /// Visual terms that get boosted score in keyword extraction.
    /// </summary>
    private static readonly HashSet<string> VisualTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Nature/Landscape
        "landscape", "nature", "mountain", "ocean", "beach", "forest", "sky",
        "sunset", "sunrise", "clouds", "water", "river", "lake", "garden",
        // Architecture/Urban
        "city", "building", "office", "modern", "urban", "architecture", "interior",
        "exterior", "skyline", "street", "downtown", "industrial",
        // People/Lifestyle
        "people", "person", "team", "business", "professional", "meeting",
        "workplace", "collaboration", "working", "lifestyle",
        // Technology
        "technology", "digital", "computer", "data", "network", "innovation",
        "artificial", "intelligence", "ai", "machine", "software", "code",
        // Medical/Healthcare
        "healthcare", "medical", "hospital", "doctor", "health", "medicine",
        "patient", "clinical", "laboratory", "research",
        // Style descriptors
        "bright", "dark", "colorful", "minimalist", "abstract", "creative",
        "elegant", "clean", "vibrant", "dramatic", "warm", "cool",
        // General visual concepts
        "background", "pattern", "texture", "light", "shadow", "gradient"
    };

    public VisualKeywordExtractor(ILogger<VisualKeywordExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts keywords from scene heading and narration text.
    /// </summary>
    /// <param name="heading">Scene heading/title.</param>
    /// <param name="narration">Scene narration/script text.</param>
    /// <param name="maxKeywords">Maximum number of keywords to return.</param>
    /// <param name="visualTermBoost">Multiplier for visual term scores (1.0-2.0).</param>
    /// <returns>List of extracted keywords ordered by relevance.</returns>
    public IReadOnlyList<string> ExtractKeywords(
        string? heading,
        string? narration,
        int maxKeywords = 5,
        double visualTermBoost = 1.5)
    {
        var wordScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // Extract and score words from heading (higher base weight)
        if (!string.IsNullOrWhiteSpace(heading))
        {
            var headingWords = ExtractWords(heading);
            foreach (var word in headingWords)
            {
                if (!StopWords.Contains(word) && word.Length >= 3)
                {
                    var score = 3.0; // Base heading weight
                    if (VisualTerms.Contains(word))
                    {
                        score *= visualTermBoost;
                    }
                    AddOrUpdateScore(wordScores, word, score);
                }
            }
        }

        // Extract and score words from narration (lower base weight)
        if (!string.IsNullOrWhiteSpace(narration))
        {
            var narrationWords = ExtractWords(narration);
            var wordCounts = narrationWords
                .Where(w => !StopWords.Contains(w) && w.Length >= 3)
                .GroupBy(w => w.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var (word, count) in wordCounts)
            {
                // Frequency-based scoring with diminishing returns
                var frequencyScore = 1.0 + Math.Log(count + 1);
                var positionScore = GetPositionScore(narrationWords, word);
                var score = frequencyScore * positionScore;

                if (VisualTerms.Contains(word))
                {
                    score *= visualTermBoost;
                }

                AddOrUpdateScore(wordScores, word, score);
            }
        }

        var topKeywords = wordScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxKeywords)
            .Select(kvp => kvp.Key)
            .ToList();

        _logger.LogDebug(
            "Extracted {Count} keywords from scene: {Keywords}",
            topKeywords.Count,
            string.Join(", ", topKeywords));

        return topKeywords;
    }

    /// <summary>
    /// Builds a search query combining keywords, style, and heading.
    /// </summary>
    /// <param name="keywords">Extracted keywords.</param>
    /// <param name="heading">Scene heading (optional).</param>
    /// <param name="style">Visual style descriptor (optional).</param>
    /// <param name="maxKeywords">Maximum keywords to include in query.</param>
    /// <returns>Optimized search query string.</returns>
    public string BuildSearchQuery(
        IReadOnlyList<string> keywords,
        string? heading = null,
        string? style = null,
        int maxKeywords = 5)
    {
        var queryParts = new List<string>();

        // Add style if provided and meaningful
        if (!string.IsNullOrWhiteSpace(style) && 
            !style.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            var styleKeyword = ExtractStyleKeyword(style);
            if (!string.IsNullOrEmpty(styleKeyword))
            {
                queryParts.Add(styleKeyword);
            }
        }

        // Add top keywords (limit to avoid overly complex queries)
        queryParts.AddRange(keywords.Take(maxKeywords));

        // Add a condensed heading keyword if it provides unique context
        if (!string.IsNullOrWhiteSpace(heading))
        {
            var headingKeyword = ExtractPrimaryTerm(heading);
            if (!string.IsNullOrEmpty(headingKeyword) && 
                !queryParts.Any(q => q.Equals(headingKeyword, StringComparison.OrdinalIgnoreCase)))
            {
                queryParts.Insert(0, headingKeyword);
            }
        }

        // Limit total query parts
        var finalParts = queryParts.Take(maxKeywords).Distinct(StringComparer.OrdinalIgnoreCase);
        var query = string.Join(" ", finalParts);

        _logger.LogDebug("Built search query: {Query}", query);
        return query;
    }

    /// <summary>
    /// Maps aspect ratio to Pexels orientation parameter.
    /// </summary>
    /// <param name="aspectWidth">Width component of aspect ratio (e.g., 16 for 16:9).</param>
    /// <param name="aspectHeight">Height component of aspect ratio (e.g., 9 for 16:9).</param>
    /// <returns>Pexels orientation value: "landscape", "portrait", or "square".</returns>
    public static string GetOrientationFromAspect(int aspectWidth, int aspectHeight)
    {
        if (aspectWidth == aspectHeight)
        {
            return "square";
        }

        return aspectWidth > aspectHeight ? "landscape" : "portrait";
    }

    /// <summary>
    /// Extracts words from text, handling various separators.
    /// </summary>
    private static IReadOnlyList<string> ExtractWords(string text)
    {
        // Match word characters, allowing hyphens within words
        var matches = Regex.Matches(text, @"\b[a-zA-Z][a-zA-Z'-]*[a-zA-Z]\b|\b[a-zA-Z]{2,}\b");
        return matches.Select(m => m.Value.ToLowerInvariant()).ToList();
    }

    /// <summary>
    /// Calculates position-based score (words appearing earlier get higher scores).
    /// </summary>
    private static double GetPositionScore(IReadOnlyList<string> words, string word)
    {
        var firstIndex = -1;
        for (int i = 0; i < words.Count; i++)
        {
            if (words[i].Equals(word, StringComparison.OrdinalIgnoreCase))
            {
                firstIndex = i;
                break;
            }
        }

        if (firstIndex < 0)
        {
            return 1.0;
        }

        // Words in first quarter get 1.5x, second quarter 1.25x, etc.
        var positionRatio = (double)firstIndex / words.Count;
        return 1.5 - (positionRatio * 0.5);
    }

    /// <summary>
    /// Adds or updates a word score in the dictionary.
    /// </summary>
    private static void AddOrUpdateScore(Dictionary<string, double> scores, string word, double score)
    {
        var lowerWord = word.ToLowerInvariant();
        if (scores.TryGetValue(lowerWord, out var existing))
        {
            scores[lowerWord] = existing + score;
        }
        else
        {
            scores[lowerWord] = score;
        }
    }

    /// <summary>
    /// Extracts the most relevant keyword from a style descriptor.
    /// </summary>
    private string ExtractStyleKeyword(string style)
    {
        var words = ExtractWords(style);
        var visualWord = words.FirstOrDefault(w => VisualTerms.Contains(w));
        return visualWord ?? words.FirstOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Extracts the primary term from a heading.
    /// </summary>
    private string ExtractPrimaryTerm(string heading)
    {
        var words = ExtractWords(heading).Where(w => !StopWords.Contains(w)).ToList();
        var visualWord = words.FirstOrDefault(w => VisualTerms.Contains(w));
        return visualWord ?? words.FirstOrDefault() ?? string.Empty;
    }
}
