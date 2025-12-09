using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for expanding queries into semantic variations for better RAG retrieval.
/// Uses LLM to generate intelligent query variations and falls back to basic expansion.
/// </summary>
public class QueryExpansionService
{
    private readonly ILogger<QueryExpansionService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly bool _enableLlmExpansion;

    public QueryExpansionService(
        ILogger<QueryExpansionService> logger,
        ILlmProvider llmProvider,
        bool enableLlmExpansion = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _enableLlmExpansion = enableLlmExpansion;
    }

    /// <summary>
    /// Expands a query into multiple semantic variations for better RAG retrieval
    /// </summary>
    /// <param name="originalQuery">The original query to expand</param>
    /// <param name="context">Optional context to guide expansion</param>
    /// <param name="maxVariations">Maximum number of variations to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of query variations including the original query</returns>
    public async Task<List<string>> ExpandQueryAsync(
        string originalQuery,
        string? context = null,
        int maxVariations = 5,
        CancellationToken ct = default)
    {
        var variations = new List<string> { originalQuery };

        // Add basic keyword extraction (fallback)
        variations.AddRange(ExtractKeyPhrases(originalQuery));

        if (!_enableLlmExpansion)
        {
            _logger.LogDebug("LLM query expansion disabled, using basic expansion only");
            return variations.Distinct().Take(maxVariations).ToList();
        }

        try
        {
            var llmVariations = await GenerateLlmVariationsAsync(originalQuery, context, ct)
                .ConfigureAwait(false);
            variations.AddRange(llmVariations);
            _logger.LogDebug("LLM generated {Count} query variations", llmVariations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM query expansion failed, falling back to basic expansion");
        }

        return variations.Distinct().Take(maxVariations).ToList();
    }

    private async Task<List<string>> GenerateLlmVariationsAsync(
        string query,
        string? context,
        CancellationToken ct)
    {
        var prompt = $@"Generate 4 alternative search queries that would help find relevant documents for the following topic. Focus on:
1. Synonyms and related terms
2. More specific aspects of the topic
3. Broader context
4. Different phrasings

Original query: {query}
{(context != null ? $"Context: {context}" : "")}

Return ONLY a JSON array of strings, no explanation:
[""query1"", ""query2"", ""query3"", ""query4""]";

        var response = await _llmProvider.GenerateChatCompletionAsync(
            "You are a search query expansion assistant. Return only valid JSON.",
            prompt,
            new LlmParameters(Temperature: 0.7, MaxTokens: 200),
            ct).ConfigureAwait(false);

        try
        {
            // Extract JSON array from response
            var jsonStart = response.IndexOf('[');
            var jsonEnd = response.LastIndexOf(']') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonArray = response.Substring(jsonStart, jsonEnd - jsonStart);
                var queries = JsonSerializer.Deserialize<List<string>>(jsonArray);
                return queries ?? new List<string>();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM query expansion response");
        }

        return new List<string>();
    }

    /// <summary>
    /// Extracts key phrases from the query using simple heuristics
    /// </summary>
    private List<string> ExtractKeyPhrases(string query)
    {
        var phrases = new List<string>();
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Extract noun phrases (simple heuristic: consecutive capitalized or long words)
        var currentPhrase = new List<string>();
        foreach (var word in words)
        {
            var cleanWord = word.Trim(',', '.', '!', '?', '"', '\'');
            if (cleanWord.Length == 0)
            {
                continue;
            }

            if (cleanWord.Length > 4 || char.IsUpper(cleanWord[0]))
            {
                currentPhrase.Add(cleanWord);
            }
            else if (currentPhrase.Count > 0)
            {
                if (currentPhrase.Count >= 2)
                {
                    phrases.Add(string.Join(" ", currentPhrase));
                }
                currentPhrase.Clear();
            }
        }

        if (currentPhrase.Count >= 2)
        {
            phrases.Add(string.Join(" ", currentPhrase));
        }

        // Add individual significant words
        phrases.AddRange(words
            .Select(w => w.Trim(',', '.', '!', '?', '"', '\''))
            .Where(w => w.Length > 5)
            .Take(3));

        return phrases;
    }
}
