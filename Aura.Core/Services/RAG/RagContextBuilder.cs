using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Builds context from retrieved document chunks for LLM prompts
/// </summary>
public class RagContextBuilder
{
    private readonly ILogger<RagContextBuilder> _logger;
    private readonly VectorIndex _vectorIndex;
    private readonly EmbeddingService _embeddingService;

    public RagContextBuilder(
        ILogger<RagContextBuilder> logger,
        VectorIndex vectorIndex,
        EmbeddingService embeddingService)
    {
        _logger = logger;
        _vectorIndex = vectorIndex;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Build context for a query using RAG
    /// </summary>
    public async Task<RagContext> BuildContextAsync(
        string query,
        RagConfig config,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Building RAG context for query: {Query}", query);

        if (!config.Enabled)
        {
            _logger.LogDebug("RAG is disabled, returning empty context");
            return new RagContext { Query = query };
        }

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct).ConfigureAwait(false);

        var retrievalResult = await _vectorIndex.SearchAsync(
            queryEmbedding,
            query,
            config.TopK,
            config.MinimumScore,
            ct).ConfigureAwait(false);

        if (retrievalResult.Chunks.Count == 0)
        {
            _logger.LogInformation("No relevant chunks found for query");
            return new RagContext { Query = query };
        }

        var contextChunks = new List<ContextChunk>();
        var citations = new List<Citation>();
        var citationNumber = 1;
        var seenSources = new HashSet<string>();

        foreach (var scoredChunk in retrievalResult.Chunks)
        {
            var chunk = scoredChunk.Chunk;
            var sourceKey = $"{chunk.Metadata.Source}_{chunk.Metadata.Section}_{chunk.Metadata.PageNumber}";

            if (!seenSources.Contains(sourceKey))
            {
                citations.Add(new Citation
                {
                    Number = citationNumber,
                    Source = chunk.Metadata.Source,
                    Title = chunk.Metadata.Title,
                    Section = chunk.Metadata.Section,
                    PageNumber = chunk.Metadata.PageNumber
                });

                seenSources.Add(sourceKey);
            }

            var citation = citations.First(c =>
                c.Source == chunk.Metadata.Source &&
                c.Section == chunk.Metadata.Section &&
                c.PageNumber == chunk.Metadata.PageNumber);

            contextChunks.Add(new ContextChunk
            {
                Content = chunk.Content,
                Source = chunk.Metadata.Source,
                Section = chunk.Metadata.Section,
                PageNumber = chunk.Metadata.PageNumber,
                RelevanceScore = scoredChunk.Score,
                CitationNumber = citation.Number
            });

            citationNumber = Math.Max(citationNumber, citation.Number + 1);
        }

        var formattedContext = FormatContext(contextChunks, config.IncludeCitations);
        var totalTokens = EstimateTokenCount(formattedContext);

        if (totalTokens > config.MaxContextTokens)
        {
            _logger.LogWarning(
                "Context exceeds max tokens ({TotalTokens} > {MaxTokens}), truncating",
                totalTokens, config.MaxContextTokens);

            formattedContext = TruncateContext(formattedContext, config.MaxContextTokens);
            totalTokens = EstimateTokenCount(formattedContext);
        }

        _logger.LogInformation(
            "Built RAG context with {ChunkCount} chunks, {CitationCount} citations, {TokenCount} tokens",
            contextChunks.Count, citations.Count, totalTokens);

        return new RagContext
        {
            Query = query,
            Chunks = contextChunks,
            FormattedContext = formattedContext,
            Citations = citations,
            TotalTokens = totalTokens
        };
    }

    private string FormatContext(List<ContextChunk> chunks, bool includeCitations)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Reference Material");
        sb.AppendLine();
        sb.AppendLine("The following information has been retrieved from project documents and should be used as reference when generating content:");
        sb.AppendLine();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            sb.AppendLine($"## Reference {i + 1}");

            if (includeCitations)
            {
                sb.Append($"Source: {chunk.Source}");

                if (!string.IsNullOrEmpty(chunk.Section))
                {
                    sb.Append($" - Section: {chunk.Section}");
                }

                if (chunk.PageNumber.HasValue)
                {
                    sb.Append($" - Page: {chunk.PageNumber}");
                }

                sb.AppendLine($" [Citation {chunk.CitationNumber}]");
            }

            sb.AppendLine();
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string TruncateContext(string context, int maxTokens)
    {
        var estimatedChars = maxTokens * 4;

        if (context.Length <= estimatedChars)
        {
            return context;
        }

        var truncated = context.Substring(0, estimatedChars);
        var lastNewline = truncated.LastIndexOf('\n');

        if (lastNewline > 0)
        {
            truncated = truncated.Substring(0, lastNewline);
        }

        return truncated + "\n\n[Context truncated due to length...]";
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Length / 4;
    }

    /// <summary>
    /// Format citations for inclusion in LLM output
    /// </summary>
    public string FormatCitations(List<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("## References");
        sb.AppendLine();

        foreach (var citation in citations)
        {
            sb.Append($"[{citation.Number}] {citation.Source}");

            if (!string.IsNullOrEmpty(citation.Title))
            {
                sb.Append($" - {citation.Title}");
            }

            if (!string.IsNullOrEmpty(citation.Section))
            {
                sb.Append($", Section: {citation.Section}");
            }

            if (citation.PageNumber.HasValue)
            {
                sb.Append($", Page {citation.PageNumber}");
            }

            if (!string.IsNullOrEmpty(citation.Url))
            {
                sb.Append($" ({citation.Url})");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
