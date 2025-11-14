using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentVerification;

/// <summary>
/// Service for generating proper source attributions
/// </summary>
public class SourceAttributionService
{
    private readonly ILogger<SourceAttributionService> _logger;

    public SourceAttributionService(ILogger<SourceAttributionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate citations for sources
    /// </summary>
    public async Task<List<string>> GenerateCitationsAsync(
        List<SourceAttribution> sources,
        CitationFormat format = CitationFormat.APA,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} citations in {Format} format", 
            sources.Count, format);

        await Task.Delay(10, ct).ConfigureAwait(false); // Simulate processing

        var citations = sources.Select(source => GenerateCitation(source, format)).ToList();
        return citations;
    }

    /// <summary>
    /// Generate a single citation
    /// </summary>
    public string GenerateCitation(SourceAttribution source, CitationFormat format)
    {
        return format switch
        {
            CitationFormat.APA => GenerateAPACitation(source),
            CitationFormat.MLA => GenerateMLACitation(source),
            CitationFormat.Chicago => GenerateChicagoCitation(source),
            CitationFormat.Harvard => GenerateHarvardCitation(source),
            _ => GenerateAPACitation(source)
        };
    }

    /// <summary>
    /// Validate source credibility
    /// </summary>
    public async Task<SourceValidationResult> ValidateSourceAsync(
        SourceAttribution source,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Validating source: {Name}", source.Name);

        await Task.Delay(50, ct).ConfigureAwait(false); // Simulate validation

        var isValid = !string.IsNullOrWhiteSpace(source.Url) && 
                     source.CredibilityScore >= 0.5;
        
        var issues = new List<string>();
        
        if (string.IsNullOrWhiteSpace(source.Url))
        {
            issues.Add("Missing source URL");
        }
        
        if (source.CredibilityScore < 0.5)
        {
            issues.Add($"Low credibility score: {source.CredibilityScore:P0}");
        }
        
        if (!source.PublishedDate.HasValue)
        {
            issues.Add("Missing publication date");
        }
        else if ((DateTime.UtcNow - source.PublishedDate.Value).TotalDays > 1825) // 5 years
        {
            issues.Add("Source is more than 5 years old");
        }

        return new SourceValidationResult(
            IsValid: isValid,
            Issues: issues,
            CredibilityScore: source.CredibilityScore,
            RecommendedAction: isValid ? "Accept" : "Review manually"
        );
    }

    /// <summary>
    /// Deduplicate sources
    /// </summary>
    public List<SourceAttribution> DeduplicateSources(List<SourceAttribution> sources)
    {
        _logger.LogDebug("Deduplicating {Count} sources", sources.Count);

        var deduplicated = sources
            .GroupBy(s => new { s.Url, s.Name })
            .Select(g => g.OrderByDescending(s => s.CredibilityScore).First())
            .ToList();

        _logger.LogDebug("Reduced to {Count} unique sources", deduplicated.Count);

        return deduplicated;
    }

    /// <summary>
    /// Rank sources by credibility
    /// </summary>
    public List<SourceAttribution> RankSourcesByCredibility(List<SourceAttribution> sources)
    {
        return sources
            .OrderByDescending(s => s.CredibilityScore)
            .ThenByDescending(s => s.PublishedDate)
            .ToList();
    }

    private string GenerateAPACitation(SourceAttribution source)
    {
        var author = source.Author ?? source.Name;
        var year = source.PublishedDate?.Year.ToString() ?? "n.d.";
        var title = source.Name;
        var url = source.Url;

        return $"{author}. ({year}). {title}. Retrieved from {url}";
    }

    private string GenerateMLACitation(SourceAttribution source)
    {
        var author = source.Author ?? source.Name;
        var title = source.Name;
        var url = source.Url;
        var accessDate = DateTime.UtcNow.ToString("d MMM yyyy");

        return $"{author}. \"{title}.\" {url}. Accessed {accessDate}.";
    }

    private string GenerateChicagoCitation(SourceAttribution source)
    {
        var author = source.Author ?? source.Name;
        var title = source.Name;
        var url = source.Url;
        var accessDate = DateTime.UtcNow.ToString("MMMM d, yyyy");

        return $"{author}. \"{title}.\" Accessed {accessDate}. {url}.";
    }

    private string GenerateHarvardCitation(SourceAttribution source)
    {
        var author = source.Author ?? source.Name;
        var year = source.PublishedDate?.Year.ToString() ?? "n.d.";
        var title = source.Name;
        var url = source.Url;

        return $"{author} ({year}) {title}. Available at: {url}";
    }
}

/// <summary>
/// Citation format options
/// </summary>
public enum CitationFormat
{
    APA,
    MLA,
    Chicago,
    Harvard
}

/// <summary>
/// Result of source validation
/// </summary>
public record SourceValidationResult(
    bool IsValid,
    List<string> Issues,
    double CredibilityScore,
    string RecommendedAction
);
