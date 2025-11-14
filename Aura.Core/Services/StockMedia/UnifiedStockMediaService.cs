using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// Unified service for searching stock media across multiple providers with
/// candidate merging, deduplication, and content safety filtering
/// </summary>
public class UnifiedStockMediaService
{
    private readonly ILogger<UnifiedStockMediaService> _logger;
    private readonly IEnumerable<IEnhancedStockProvider> _providers;
    private readonly PerceptualHashService _hashService;
    private readonly ContentSafetyFilterService _safetyService;

    public UnifiedStockMediaService(
        ILogger<UnifiedStockMediaService> logger,
        IEnumerable<IEnhancedStockProvider> providers,
        PerceptualHashService hashService,
        ContentSafetyFilterService safetyService)
    {
        _logger = logger;
        _providers = providers;
        _hashService = hashService;
        _safetyService = safetyService;
    }

    /// <summary>
    /// Searches across multiple stock media providers and returns merged results
    /// </summary>
    public async Task<StockMediaSearchResponse> SearchAsync(
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Unified search for: {Query} across {ProviderCount} providers",
            request.Query, request.Providers.Count);

        var providerToUse = GetProvidersToSearch(request);
        var allResults = new List<StockMediaResult>();
        var resultsByProvider = new Dictionary<StockMediaProvider, int>();

        var searchTasks = providerToUse.Select(provider =>
            SearchProviderAsync(provider, request, ct));

        var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);

        foreach (var (provider, providerResults) in results)
        {
            allResults.AddRange(providerResults);
            resultsByProvider[provider] = providerResults.Count;
        }

        var filteredResults = await ApplySafetyFiltersAsync(allResults, request, ct).ConfigureAwait(false);
        
        var dedupedResults = DeduplicateResults(filteredResults);
        
        var scoredResults = ScoreRelevance(dedupedResults, request.Query);
        
        var sortedResults = scoredResults
            .OrderByDescending(r => r.RelevanceScore)
            .Take(request.Count)
            .ToList();

        _logger.LogInformation(
            "Unified search returned {Count} results (filtered from {Total})",
            sortedResults.Count, allResults.Count);

        return new StockMediaSearchResponse
        {
            Results = sortedResults,
            TotalResults = allResults.Count,
            Page = request.Page,
            PerPage = request.Count,
            ResultsByProvider = resultsByProvider
        };
    }

    /// <summary>
    /// Gets rate limit status for all providers
    /// </summary>
    public Dictionary<StockMediaProvider, RateLimitStatus> GetRateLimitStatus()
    {
        var status = new Dictionary<StockMediaProvider, RateLimitStatus>();

        foreach (var provider in _providers)
        {
            status[provider.ProviderName] = provider.GetRateLimitStatus();
        }

        return status;
    }

    /// <summary>
    /// Validates API keys for all configured providers
    /// </summary>
    public async Task<Dictionary<StockMediaProvider, bool>> ValidateProvidersAsync(
        CancellationToken ct)
    {
        var results = new Dictionary<StockMediaProvider, bool>();

        foreach (var provider in _providers)
        {
            try
            {
                var isValid = await provider.ValidateAsync(ct).ConfigureAwait(false);
                results[provider.ProviderName] = isValid;
                
                _logger.LogInformation(
                    "Provider {Provider} validation: {Result}",
                    provider.ProviderName, isValid ? "Success" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating provider {Provider}", provider.ProviderName);
                results[provider.ProviderName] = false;
            }
        }

        return results;
    }

    private IEnumerable<IEnhancedStockProvider> GetProvidersToSearch(StockMediaSearchRequest request)
    {
        if (request.Providers.Count == 0)
        {
            return _providers;
        }

        return _providers.Where(p => request.Providers.Contains(p.ProviderName));
    }

    private async Task<(StockMediaProvider Provider, List<StockMediaResult> Results)> SearchProviderAsync(
        IEnhancedStockProvider provider,
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.Type == StockMediaType.Video && !provider.SupportsVideo)
            {
                _logger.LogInformation(
                    "Skipping {Provider} for video search (not supported)",
                    provider.ProviderName);
                return (provider.ProviderName, new List<StockMediaResult>());
            }

            var results = await provider.SearchAsync(request, ct).ConfigureAwait(false);
            
            var updatedResults = new List<StockMediaResult>();
            foreach (var result in results)
            {
                if (string.IsNullOrEmpty(result.PerceptualHash))
                {
                    var hash = _hashService.GenerateHash(
                        result.FullSizeUrl,
                        result.Width,
                        result.Height);
                    updatedResults.Add(result with { PerceptualHash = hash });
                }
                else
                {
                    updatedResults.Add(result);
                }
            }

            return (provider.ProviderName, updatedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching provider {Provider}", provider.ProviderName);
            return (provider.ProviderName, new List<StockMediaResult>());
        }
    }

    private async Task<List<StockMediaResult>> ApplySafetyFiltersAsync(
        List<StockMediaResult> results,
        StockMediaSearchRequest request,
        CancellationToken ct)
    {
        if (!request.SafeSearchEnabled)
        {
            return results;
        }

        var filtered = new List<StockMediaResult>();

        foreach (var result in results)
        {
            var isSafe = await _safetyService.IsContentSafeAsync(
                result.Licensing.Attribution ?? string.Empty,
                ct).ConfigureAwait(false);

            if (isSafe)
            {
                filtered.Add(result);
            }
            else
            {
                _logger.LogDebug(
                    "Filtered out result {Id} from {Provider} due to safety concerns",
                    result.Id, result.Provider);
            }
        }

        return filtered;
    }

    private List<StockMediaResult> DeduplicateResults(List<StockMediaResult> results)
    {
        var deduplicated = new List<StockMediaResult>();
        var seenHashes = new HashSet<string>();

        foreach (var result in results)
        {
            if (string.IsNullOrEmpty(result.PerceptualHash))
            {
                deduplicated.Add(result);
                continue;
            }

            var isDuplicate = seenHashes.Any(hash =>
                _hashService.IsDuplicate(hash, result.PerceptualHash, threshold: 0.90));

            if (!isDuplicate)
            {
                deduplicated.Add(result);
                seenHashes.Add(result.PerceptualHash);
            }
            else
            {
                _logger.LogDebug(
                    "Removed duplicate result {Id} from {Provider}",
                    result.Id, result.Provider);
            }
        }

        _logger.LogInformation(
            "Deduplication: {Original} results → {Deduplicated} unique results",
            results.Count, deduplicated.Count);

        return deduplicated;
    }

    private List<StockMediaResult> ScoreRelevance(
        List<StockMediaResult> results,
        string query)
    {
        var queryTerms = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return results.Select(result =>
        {
            var score = CalculateRelevanceScore(result, queryTerms);
            return result with { RelevanceScore = score };
        }).ToList();
    }

    private double CalculateRelevanceScore(
        StockMediaResult result,
        string[] queryTerms)
    {
        double score = 0.5;

        var attribution = (result.Licensing.Attribution ?? string.Empty).ToLowerInvariant();
        var metadata = string.Join(" ", result.Metadata.Values).ToLowerInvariant();

        foreach (var term in queryTerms)
        {
            if (attribution.Contains(term))
                score += 0.1;
            if (metadata.Contains(term))
                score += 0.05;
        }

        if (result.Width >= 1920 && result.Height >= 1080)
            score += 0.1;
        else if (result.Width >= 1280 && result.Height >= 720)
            score += 0.05;

        if (result.Type == StockMediaType.Video && result.Duration.HasValue)
        {
            var duration = result.Duration.Value.TotalSeconds;
            if (duration >= 5 && duration <= 30)
                score += 0.1;
        }

        if (result.Licensing.CommercialUseAllowed)
            score += 0.05;

        if (!result.Licensing.AttributionRequired)
            score += 0.05;

        return Math.Min(1.0, score);
    }
}
