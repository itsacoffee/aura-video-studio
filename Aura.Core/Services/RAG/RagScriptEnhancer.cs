using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for enhancing briefs with RAG-retrieved context for grounded script generation
/// </summary>
public class RagScriptEnhancer
{
    private readonly ILogger<RagScriptEnhancer> _logger;
    private readonly RagContextBuilder _contextBuilder;

    public RagScriptEnhancer(
        ILogger<RagScriptEnhancer> logger,
        RagContextBuilder contextBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
    }

    /// <summary>
    /// Enhances a brief with RAG context if RAG is enabled
    /// </summary>
    /// <param name="brief">Original brief</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Enhanced brief with RAG context injected into PromptModifiers, or original if RAG disabled</returns>
    public async Task<(Brief enhancedBrief, RagContext? ragContext)> EnhanceBriefWithRagAsync(
        Brief brief,
        CancellationToken ct)
    {
        if (brief.RagConfiguration == null || !brief.RagConfiguration.Enabled)
        {
            _logger.LogDebug("RAG is disabled for this brief, skipping enhancement");
            return (brief, null);
        }

        try
        {
            _logger.LogInformation(
                "Enhancing brief with RAG context: Topic={Topic}, TopK={TopK}, MinScore={MinScore}",
                brief.Topic, brief.RagConfiguration.TopK, brief.RagConfiguration.MinimumScore);

            // Use query expansion for better retrieval
            var queryVariations = BuildRagQueryVariations(brief);
            _logger.LogInformation("Using {Count} query variations for RAG retrieval", queryVariations.Count);

            var ragConfig = new RagConfig
            {
                Enabled = true,
                TopK = brief.RagConfiguration.TopK,
                MinimumScore = brief.RagConfiguration.MinimumScore,
                MaxContextTokens = brief.RagConfiguration.MaxContextTokens,
                IncludeCitations = brief.RagConfiguration.IncludeCitations
            };

            // Build context from multiple query variations and merge results for better coverage
            var allChunks = new System.Collections.Generic.Dictionary<string, Aura.Core.Models.RAG.ContextChunk>();
            var allCitations = new System.Collections.Generic.Dictionary<string, Aura.Core.Models.RAG.Citation>();
            var citationNumber = 1;

            foreach (var query in queryVariations)
            {
                try
                {
                    var context = await _contextBuilder.BuildContextAsync(query, ragConfig, ct).ConfigureAwait(false);
                    
                    // Merge chunks, avoiding duplicates by content hash
                    foreach (var chunk in context.Chunks)
                    {
                        var chunkKey = $"{chunk.Source}_{chunk.Section}_{chunk.PageNumber}_{chunk.Content.GetHashCode()}";
                        if (!allChunks.ContainsKey(chunkKey))
                        {
                            allChunks[chunkKey] = chunk;
                            
                            // Map citations
                            var citationKey = $"{chunk.Source}_{chunk.Section}_{chunk.PageNumber}";
                            if (!allCitations.ContainsKey(citationKey))
                            {
                                allCitations[citationKey] = new Aura.Core.Models.RAG.Citation
                                {
                                    Number = citationNumber++,
                                    Source = chunk.Source,
                                    Section = chunk.Section,
                                    PageNumber = chunk.PageNumber
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve context for query variation: {Query}", query);
                }
            }

            // Re-rank chunks by relevance score and take top K
            var topChunks = allChunks.Values
                .OrderByDescending(c => c.RelevanceScore)
                .Take(brief.RagConfiguration.TopK)
                .ToList();

            // Update citation numbers in chunks
            var citationMap = allCitations.ToDictionary(c => $"{c.Value.Source}_{c.Value.Section}_{c.Value.PageNumber}", c => c.Value.Number);
            var updatedChunks = topChunks.Select(chunk =>
            {
                var citationKey = $"{chunk.Source}_{chunk.Section}_{chunk.PageNumber}";
                var newCitationNumber = citationMap.ContainsKey(citationKey) ? citationMap[citationKey] : 1;
                return chunk with { CitationNumber = newCitationNumber };
            }).ToList();

            // Build merged RAG context
            var formattedContext = FormatMergedContext(updatedChunks, allCitations.Values.ToList(), ragConfig.IncludeCitations);
            var totalTokens = EstimateTokenCount(formattedContext);

            var ragContext = new Aura.Core.Models.RAG.RagContext
            {
                Query = BuildRagQuery(brief),
                Chunks = updatedChunks,
                Citations = allCitations.Values.OrderBy(c => c.Number).ToList(),
                FormattedContext = formattedContext,
                TotalTokens = totalTokens
            };

            if (ragContext.Chunks.Count == 0)
            {
                _logger.LogWarning("No relevant RAG context found for query: {Query}", ragContext.Query);
                return (brief, ragContext);
            }

            _logger.LogInformation(
                "Retrieved {ChunkCount} relevant chunks ({TokenCount} tokens) from RAG index",
                ragContext.Chunks.Count, ragContext.TotalTokens);

            var enhancedBrief = EnhanceBriefWithContext(brief, ragContext);

            return (enhancedBrief, ragContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enhance brief with RAG context, continuing without RAG");
            return (brief, null);
        }
    }

    /// <summary>
    /// Builds a search query from the brief for RAG retrieval with intelligent expansion
    /// </summary>
    private string BuildRagQuery(Brief brief)
    {
        var queryParts = new System.Collections.Generic.List<string> { brief.Topic };

        if (!string.IsNullOrWhiteSpace(brief.Goal))
        {
            queryParts.Add(brief.Goal);
        }

        if (!string.IsNullOrWhiteSpace(brief.Audience))
        {
            queryParts.Add($"for {brief.Audience}");
        }

        // Add key terms from topic for better retrieval
        var topicWords = brief.Topic.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3) // Filter out short words
            .Take(5); // Take top 5 meaningful words
        
        queryParts.AddRange(topicWords);

        return string.Join(" ", queryParts);
    }

    /// <summary>
    /// Builds multiple query variations for better RAG retrieval (query expansion)
    /// </summary>
    private System.Collections.Generic.List<string> BuildRagQueryVariations(Brief brief)
    {
        var queries = new System.Collections.Generic.List<string>();
        
        // Base query
        queries.Add(BuildRagQuery(brief));
        
        // Topic-focused query
        queries.Add(brief.Topic);
        
        // Goal-focused query
        if (!string.IsNullOrWhiteSpace(brief.Goal))
        {
            queries.Add($"{brief.Topic} {brief.Goal}");
        }
        
        // Audience-specific query
        if (!string.IsNullOrWhiteSpace(brief.Audience))
        {
            queries.Add($"{brief.Topic} {brief.Audience}");
        }
        
        // Extract key concepts from topic
        var topicWords = brief.Topic.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Distinct()
            .Take(3);
        
        if (topicWords.Any())
        {
            queries.Add(string.Join(" ", topicWords));
        }

        return queries.Distinct().ToList();
    }

    /// <summary>
    /// Enhances the brief by injecting RAG context into PromptModifiers
    /// </summary>
    private Brief EnhanceBriefWithContext(Brief brief, RagContext ragContext)
    {
        var ragInstructions = FormatRagInstructions(ragContext);

        var existingModifiers = brief.PromptModifiers;
        var existingInstructions = existingModifiers?.AdditionalInstructions ?? string.Empty;

        var combinedInstructions = string.IsNullOrWhiteSpace(existingInstructions)
            ? ragInstructions
            : $"{existingInstructions}\n\n{ragInstructions}";

        var enhancedModifiers = new PromptModifiers(
            AdditionalInstructions: combinedInstructions,
            ExampleStyle: existingModifiers?.ExampleStyle,
            EnableChainOfThought: existingModifiers?.EnableChainOfThought ?? false,
            PromptVersion: existingModifiers?.PromptVersion);

        return brief with { PromptModifiers = enhancedModifiers };
    }

    /// <summary>
    /// Formats RAG context into prompt instructions
    /// </summary>
    private string FormatRagInstructions(RagContext ragContext)
    {
        var instructions = new System.Text.StringBuilder();

        instructions.AppendLine("# Reference Material");
        instructions.AppendLine();
        instructions.AppendLine("Use the following reference material to ground your script. " +
                              "Ensure all factual claims are supported by these sources and include citations.");
        instructions.AppendLine();
        instructions.Append(ragContext.FormattedContext);

        if (ragContext.Citations.Count > 0)
        {
            instructions.AppendLine();
            instructions.AppendLine();
            instructions.AppendLine("# Citations");
            instructions.AppendLine();
            instructions.AppendLine("Include inline citations in your script using the format [Citation N] " +
                                  "where N is the citation number from above.");
            instructions.AppendLine();
            instructions.AppendLine("Available citations:");
            foreach (var citation in ragContext.Citations)
            {
                instructions.AppendLine($"  [{citation.Number}] {citation.Source}" +
                    (citation.Section != null ? $" - {citation.Section}" : "") +
                    (citation.PageNumber.HasValue ? $" (p. {citation.PageNumber})" : ""));
            }
        }

        return instructions.ToString();
    }

    /// <summary>
    /// Applies "tighten claims" validation pass to ensure all facts are properly cited
    /// </summary>
    public async Task<(string enhancedScript, System.Collections.Generic.List<string> warnings)> TightenClaimsAsync(
        string script,
        RagContext ragContext,
        CancellationToken ct)
    {
        await Task.CompletedTask;
        var warnings = new System.Collections.Generic.List<string>();

        if (ragContext == null || ragContext.Citations.Count == 0)
        {
            warnings.Add("No RAG citations available for claim verification");
            return (script, warnings);
        }

        _logger.LogInformation("Performing 'tighten claims' validation pass");

        var citationPattern = @"\[Citation \d+\]";
        var citationMatches = System.Text.RegularExpressions.Regex.Matches(script, citationPattern);

        if (citationMatches.Count == 0)
        {
            warnings.Add("Script contains no citations despite RAG being enabled. " +
                       "Consider reviewing factual claims for proper source attribution.");
        }
        else
        {
            _logger.LogInformation("Script contains {CitationCount} citations", citationMatches.Count);
        }

        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var factualKeywords = new[] { "research", "study", "data", "statistic", "percent", "found", "according" };

        foreach (var sentence in sentences)
        {
            var lowerSentence = sentence.ToLowerInvariant();
            var containsFactualKeyword = Array.Exists(factualKeywords, 
                keyword => lowerSentence.Contains(keyword));

            if (containsFactualKeyword && !System.Text.RegularExpressions.Regex.IsMatch(sentence, citationPattern))
            {
                warnings.Add($"Potential uncited factual claim: \"{sentence.Trim()}...\"");
            }
        }

        return (script, warnings);
    }

    /// <summary>
    /// Formats merged context from multiple query variations
    /// </summary>
    private string FormatMergedContext(
        System.Collections.Generic.List<Aura.Core.Models.RAG.ContextChunk> chunks,
        System.Collections.Generic.List<Aura.Core.Models.RAG.Citation> citations,
        bool includeCitations)
    {
        var instructions = new System.Text.StringBuilder();

        instructions.AppendLine("# Reference Material");
        instructions.AppendLine();
        instructions.AppendLine("The following information has been retrieved from project documents using multiple query variations for comprehensive coverage. " +
                              "Use this material to ground your script. Ensure all factual claims are supported by these sources and include citations.");
        instructions.AppendLine();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            instructions.AppendLine($"## Reference {i + 1}");

            if (includeCitations)
            {
                instructions.Append($"Source: {chunk.Source}");

                if (!string.IsNullOrEmpty(chunk.Section))
                {
                    instructions.Append($" - Section: {chunk.Section}");
                }

                if (chunk.PageNumber.HasValue)
                {
                    instructions.Append($" - Page: {chunk.PageNumber}");
                }

                instructions.AppendLine($" [Citation {chunk.CitationNumber}]");
            }

            instructions.AppendLine();
            instructions.AppendLine(chunk.Content);
            instructions.AppendLine();
        }

        return instructions.ToString();
    }

    /// <summary>
    /// Estimate token count for chunks
    /// </summary>
    private int EstimateTokenCount(System.Collections.Generic.List<Aura.Core.Models.RAG.ContextChunk> chunks)
    {
        var totalChars = chunks.Sum(c => c.Content.Length);
        return totalChars / 4; // Rough approximation: 4 chars per token
    }
}
