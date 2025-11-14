using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Aura.Core.Models.StockMedia;
using Aura.Core.Services.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// Enforces content safety policies on stock media searches
/// Integrates with ContentSafetyService for unified policy enforcement
/// </summary>
public class StockMediaSafetyEnforcer
{
    private readonly ILogger<StockMediaSafetyEnforcer> _logger;
    private readonly ContentSafetyService _contentSafetyService;
    private readonly ContentSafetyFilterService _filterService;

    public StockMediaSafetyEnforcer(
        ILogger<StockMediaSafetyEnforcer> logger,
        ContentSafetyService contentSafetyService,
        ContentSafetyFilterService filterService)
    {
        _logger = logger;
        _contentSafetyService = contentSafetyService;
        _filterService = filterService;
    }

    /// <summary>
    /// Validates and sanitizes a stock media search query
    /// </summary>
    public async Task<StockMediaQueryValidationResult> ValidateAndSanitizeQueryAsync(
        string query,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating stock media query with policy {PolicyName}", policy.Name);

        try
        {
            var result = new StockMediaQueryValidationResult
            {
                OriginalQuery = query
            };

            var (isValid, reason) = _filterService.ValidateQuery(query);
            if (!isValid)
            {
                result.IsValid = false;
                result.ValidationMessage = reason ?? "Query validation failed";
                result.SanitizedQuery = _filterService.SanitizeQuery(query);
                return result;
            }

            var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
                Guid.NewGuid().ToString(),
                query,
                policy,
                ct).ConfigureAwait(false);

            result.IsValid = analysisResult.IsSafe;
            result.AnalysisResult = analysisResult;

            if (!result.IsValid)
            {
                result.SanitizedQuery = await GenerateSafeQueryAsync(query, analysisResult, ct).ConfigureAwait(false);
                result.ValidationMessage = GenerateValidationMessage(analysisResult);
                result.Alternatives = GenerateQueryAlternatives(query, analysisResult);
            }
            else
            {
                result.SanitizedQuery = query;
                result.ValidationMessage = "Query is safe for stock media search";
            }

            _logger.LogInformation(
                "Query validation complete. Valid: {IsValid}, Sanitized: {Sanitized}",
                result.IsValid,
                result.SanitizedQuery);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating stock media query");
            throw;
        }
    }

    /// <summary>
    /// Filters stock media results based on safety policy
    /// </summary>
    public async Task<List<StockMediaResult>> FilterResultsAsync(
        List<StockMediaResult> results,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Filtering {Count} stock media results", results.Count);

        var filterTasks = results.Select(async result =>
        {
            var isSafe = await IsResultSafeAsync(result, policy, ct).ConfigureAwait(false);
            return new { Result = result, IsSafe = isSafe };
        });

        var filterResults = await Task.WhenAll(filterTasks).ConfigureAwait(false);

        var safeResults = filterResults
            .Where(r => r.IsSafe)
            .Select(r => r.Result)
            .ToList();

        var filteredCount = results.Count - safeResults.Count;
        if (filteredCount > 0)
        {
            _logger.LogDebug("Filtered out {Count} results", filteredCount);
        }

        _logger.LogInformation(
            "Filtered results: {SafeCount}/{TotalCount} passed safety checks",
            safeResults.Count,
            results.Count);

        return safeResults;
    }

    /// <summary>
    /// Records a stock media safety decision for audit trail
    /// </summary>
    public async Task<SafetyAuditLog> RecordSearchDecisionAsync(
        string query,
        SafetyPolicy policy,
        SafetyAnalysisResult? analysisResult,
        bool wasOverridden,
        string? userId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Recording stock media search decision for query: {Query}", query);

        var auditLog = new SafetyAuditLog
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            ContentId = $"stock-query-{Guid.NewGuid()}",
            PolicyId = policy.Id,
            UserId = userId ?? "system",
            ContentType = "StockMediaQuery",
            Decision = wasOverridden ? SafetyDecision.Approved : SafetyDecision.Rejected,
            DecisionReason = wasOverridden ? "User override in Advanced Mode" : "Policy enforcement"
        };

        if (analysisResult != null)
        {
            auditLog.AnalysisResult = analysisResult;
            if (wasOverridden)
            {
                auditLog.OverriddenViolations = analysisResult.Violations
                    .Select(v => v.Id)
                    .ToList();
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);

        _logger.LogInformation("Stock media search decision recorded: {AuditId}", auditLog.Id);

        return auditLog;
    }

    /// <summary>
    /// Gets stock media search recommendations with safety in mind
    /// </summary>
    public async Task<StockMediaSafetyRecommendation> GetSafeSearchRecommendationAsync(
        string sceneDescription,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating safe search recommendation for scene");

        try
        {
            var analysisResult = await _contentSafetyService.AnalyzeContentAsync(
                Guid.NewGuid().ToString(),
                sceneDescription,
                policy,
                ct).ConfigureAwait(false);

            var recommendation = new StockMediaSafetyRecommendation
            {
                OriginalDescription = sceneDescription,
                IsSafe = analysisResult.IsSafe,
                RecommendedQuery = GenerateSafeSearchQuery(sceneDescription, analysisResult),
                SafetyGuidelines = GenerateSearchGuidelines(analysisResult, policy),
                KeywordsToAvoid = ExtractProblematicKeywords(analysisResult),
                SuggestedFilters = GenerateSuggestedFilters(analysisResult, policy)
            };

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating safe search recommendation");
            throw;
        }
    }

    private async Task<bool> IsResultSafeAsync(
        StockMediaResult result,
        SafetyPolicy policy,
        CancellationToken ct)
    {
        var textToAnalyze = string.Join(" ", result.Metadata.Values);

        if (string.IsNullOrWhiteSpace(textToAnalyze))
        {
            return true;
        }

        return await _filterService.IsContentSafeAsync(textToAnalyze, ct).ConfigureAwait(false);
    }

    private async Task<string> GenerateSafeQueryAsync(
        string originalQuery,
        SafetyAnalysisResult analysisResult,
        CancellationToken ct)
    {
        var sanitized = _filterService.SanitizeQuery(originalQuery);

        foreach (var violation in analysisResult.Violations)
        {
            if (!string.IsNullOrEmpty(violation.MatchedContent))
            {
                var safeReplacement = GetSafeQueryReplacement(violation.Category);
                sanitized = sanitized.Replace(violation.MatchedContent, safeReplacement, StringComparison.OrdinalIgnoreCase);
            }
        }

        sanitized = sanitized.Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "nature landscape";
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return sanitized;
    }

    private string GenerateValidationMessage(SafetyAnalysisResult analysisResult)
    {
        var blockingViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.Block)
            .ToList();

        if (blockingViolations.Count == 0)
        {
            return "Query has warnings but can be used with modifications.";
        }

        var categories = blockingViolations
            .Select(v => v.Category.ToString())
            .Distinct()
            .Take(3);

        return $"Query blocked due to: {string.Join(", ", categories)}. Use suggested alternative or modify your search.";
    }

    private List<string> GenerateQueryAlternatives(string originalQuery, SafetyAnalysisResult analysisResult)
    {
        var alternatives = new List<string>();

        var words = originalQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var safeWords = words.Where(w => !IsProblematicWord(w, analysisResult)).ToList();

        if (safeWords.Count > 0)
        {
            alternatives.Add(string.Join(" ", safeWords));
        }

        alternatives.Add("nature scenery");
        alternatives.Add("professional photography");
        alternatives.Add("abstract patterns");

        return alternatives.Distinct().Take(3).ToList();
    }

    private bool IsProblematicWord(string word, SafetyAnalysisResult analysisResult)
    {
        return analysisResult.Violations.Any(v =>
            !string.IsNullOrEmpty(v.MatchedContent) &&
            v.MatchedContent.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateSafeSearchQuery(string sceneDescription, SafetyAnalysisResult analysisResult)
    {
        if (analysisResult.IsSafe)
        {
            var words = sceneDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Take(5));
        }

        var safeWords = sceneDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !IsProblematicWord(w, analysisResult))
            .Take(5);

        var query = string.Join(" ", safeWords);
        return string.IsNullOrWhiteSpace(query) ? "professional stock photo" : query;
    }

    private List<string> GenerateSearchGuidelines(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        var guidelines = new List<string>
        {
            "Use descriptive, family-friendly keywords",
            "Avoid overly specific or sensitive terms",
            "Focus on visual elements rather than controversial topics"
        };

        if (analysisResult.Violations.Any(v => v.Category == SafetyCategoryType.Violence))
        {
            guidelines.Add("Avoid keywords related to conflict or violence");
        }

        if (analysisResult.Violations.Any(v => v.Category == SafetyCategoryType.SexualContent))
        {
            guidelines.Add("Keep searches appropriate for all ages");
        }

        if (policy.BrandSafety != null && policy.BrandSafety.RequiredKeywords.Count > 0)
        {
            guidelines.Add($"Consider including brand keywords: {string.Join(", ", policy.BrandSafety.RequiredKeywords.Take(3))}");
        }

        return guidelines;
    }

    private List<string> ExtractProblematicKeywords(SafetyAnalysisResult analysisResult)
    {
        return analysisResult.Violations
            .Where(v => !string.IsNullOrEmpty(v.MatchedContent))
            .Select(v => v.MatchedContent!)
            .Distinct()
            .Take(10)
            .ToList();
    }

    private Dictionary<string, string> GenerateSuggestedFilters(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        var filters = new Dictionary<string, string>
        {
            ["safeSearch"] = "enabled",
            ["orientation"] = "landscape"
        };

        if (policy.AgeSettings != null)
        {
            filters["targetRating"] = policy.AgeSettings.TargetRating.ToString();
        }

        return filters;
    }

    private string GetSafeQueryReplacement(SafetyCategoryType category)
    {
        return category switch
        {
            SafetyCategoryType.Profanity => "appropriate",
            SafetyCategoryType.Violence => "peaceful",
            SafetyCategoryType.SexualContent => "family-friendly",
            SafetyCategoryType.HateSpeech => "inclusive",
            SafetyCategoryType.DrugAlcohol => "healthy",
            SafetyCategoryType.ControversialTopics => "educational",
            SafetyCategoryType.SelfHarm => "supportive",
            SafetyCategoryType.GraphicImagery => "artistic",
            _ => "appropriate"
        };
    }
}

/// <summary>
/// Result of stock media query validation
/// </summary>
public class StockMediaQueryValidationResult
{
    public string OriginalQuery { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public string SanitizedQuery { get; set; } = string.Empty;
    public SafetyAnalysisResult? AnalysisResult { get; set; }
    public List<string> Alternatives { get; set; } = new();
}

/// <summary>
/// Safe search recommendation for stock media
/// </summary>
public class StockMediaSafetyRecommendation
{
    public string OriginalDescription { get; set; } = string.Empty;
    public bool IsSafe { get; set; }
    public string RecommendedQuery { get; set; } = string.Empty;
    public List<string> SafetyGuidelines { get; set; } = new();
    public List<string> KeywordsToAvoid { get; set; } = new();
    public Dictionary<string, string> SuggestedFilters { get; set; } = new();
}
