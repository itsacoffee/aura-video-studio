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

            var query = BuildRagQuery(brief);

            var ragConfig = new RagConfig
            {
                Enabled = true,
                TopK = brief.RagConfiguration.TopK,
                MinimumScore = brief.RagConfiguration.MinimumScore,
                MaxContextTokens = brief.RagConfiguration.MaxContextTokens,
                IncludeCitations = brief.RagConfiguration.IncludeCitations
            };

            var ragContext = await _contextBuilder.BuildContextAsync(query, ragConfig, ct);

            if (ragContext.Chunks.Count == 0)
            {
                _logger.LogWarning("No relevant RAG context found for query: {Query}", query);
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
    /// Builds a search query from the brief for RAG retrieval
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

        return string.Join(" ", queryParts);
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
        var factuaKeywords = new[] { "research", "study", "data", "statistic", "percent", "found", "according" };

        foreach (var sentence in sentences)
        {
            var lowerSentence = sentence.ToLowerInvariant();
            var containsFactualKeyword = Array.Exists(factuaKeywords, 
                keyword => lowerSentence.Contains(keyword));

            if (containsFactualKeyword && !System.Text.RegularExpressions.Regex.IsMatch(sentence, citationPattern))
            {
                warnings.Add($"Potential uncited factual claim: \"{sentence.Trim()}...\"");
            }
        }

        return (script, warnings);
    }
}
