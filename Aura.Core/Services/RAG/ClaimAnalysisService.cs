using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aura.Core.Models.RAG;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.RAG;

/// <summary>
/// Service for analyzing reference material to extract verifiable claims
/// that can be used for grounded script generation.
/// </summary>
public class ClaimAnalysisService
{
    private readonly ILogger<ClaimAnalysisService> _logger;

    /// <summary>
    /// Patterns that indicate a statement contains a factual claim
    /// </summary>
    private static readonly string[] FactPatterns = new[]
    {
        @"\d+\s*%",                                                    // Percentages
        @"\d+\s*(million|billion|thousand)",                          // Numbers with scale
        @"(study|research|survey|report)\s+(found|showed|indicates)", // Research findings
        @"according to",                                               // Attribution
        @"(increase|decrease|grew|declined)\s+by"                     // Quantitative changes
    };

    public ClaimAnalysisService(ILogger<ClaimAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts verifiable claims from RAG context that can be used in script generation.
    /// </summary>
    /// <param name="ragContext">The RAG context containing reference chunks</param>
    /// <returns>List of verifiable claims with their citation numbers</returns>
    public List<VerifiableClaim> ExtractVerifiableClaims(RagContext ragContext)
    {
        ArgumentNullException.ThrowIfNull(ragContext);

        var claims = new List<VerifiableClaim>();

        _logger.LogDebug("Extracting verifiable claims from {ChunkCount} chunks", ragContext.Chunks.Count);

        foreach (var chunk in ragContext.Chunks)
        {
            var sentences = chunk.Content.Split(
                new[] { '.', '!', '?' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var sentence in sentences)
            {
                var claim = AnalyzeSentence(sentence, chunk.CitationNumber);
                if (claim != null)
                {
                    claims.Add(claim);
                }
            }
        }

        _logger.LogInformation("Extracted {ClaimCount} verifiable claims from reference material", claims.Count);

        return claims;
    }

    /// <summary>
    /// Analyzes a single sentence to determine if it contains a verifiable claim.
    /// </summary>
    private VerifiableClaim? AnalyzeSentence(string sentence, int citationNumber)
    {
        if (string.IsNullOrWhiteSpace(sentence) || sentence.Length < 10)
        {
            return null;
        }

        foreach (var pattern in FactPatterns)
        {
            if (Regex.IsMatch(sentence, pattern, RegexOptions.IgnoreCase))
            {
                return new VerifiableClaim
                {
                    Statement = sentence.Trim(),
                    CitationNumber = citationNumber,
                    ClaimType = DetermineClaimType(sentence)
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the type of claim based on sentence content.
    /// </summary>
    private static ClaimType DetermineClaimType(string sentence)
    {
        if (Regex.IsMatch(sentence, @"\d+\s*%", RegexOptions.IgnoreCase))
        {
            return ClaimType.Statistic;
        }

        if (Regex.IsMatch(sentence, @"(study|research)", RegexOptions.IgnoreCase))
        {
            return ClaimType.ResearchFinding;
        }

        if (Regex.IsMatch(sentence, @"(said|stated|wrote|quoted)", RegexOptions.IgnoreCase))
        {
            return ClaimType.Quote;
        }

        return ClaimType.GeneralFact;
    }

    /// <summary>
    /// Formats extracted claims for inclusion in a prompt.
    /// </summary>
    /// <param name="claims">List of verifiable claims</param>
    /// <param name="maxClaims">Maximum number of claims to include</param>
    /// <returns>Formatted string for prompt inclusion</returns>
    public string FormatClaimsForPrompt(List<VerifiableClaim> claims, int maxClaims = 10)
    {
        if (claims == null || claims.Count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine();
        sb.AppendLine("# Pre-Verified Claims You Can Use");
        sb.AppendLine();
        sb.AppendLine("The following claims have been extracted from the reference material and can be used confidently:");
        sb.AppendLine();

        foreach (var claim in claims.Take(maxClaims))
        {
            sb.AppendLine($"- \"{claim.Statement}\" [Citation {claim.CitationNumber}]");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Represents a verifiable claim extracted from reference material.
/// </summary>
public record VerifiableClaim
{
    /// <summary>
    /// The claim statement text
    /// </summary>
    public required string Statement { get; init; }

    /// <summary>
    /// The citation number from the source material
    /// </summary>
    public required int CitationNumber { get; init; }

    /// <summary>
    /// The type of claim
    /// </summary>
    public ClaimType ClaimType { get; init; }
}

/// <summary>
/// Types of verifiable claims
/// </summary>
public enum ClaimType
{
    /// <summary>
    /// Statistical data (percentages, numbers)
    /// </summary>
    Statistic,

    /// <summary>
    /// Finding from research or studies
    /// </summary>
    ResearchFinding,

    /// <summary>
    /// General factual statement
    /// </summary>
    GeneralFact,

    /// <summary>
    /// Direct quote from a source
    /// </summary>
    Quote
}
