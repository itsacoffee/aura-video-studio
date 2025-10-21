using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Suggests relevant visual assets for video scenes using AI-powered analysis
/// </summary>
public class VisualAssetSuggester
{
    private readonly ILogger<VisualAssetSuggester> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly IStockProvider? _stockProvider;
    private readonly ConcurrentDictionary<string, (List<AssetSuggestion> suggestions, DateTime timestamp)> _cache;

    public VisualAssetSuggester(
        ILogger<VisualAssetSuggester> logger, 
        ILlmProvider llmProvider,
        IStockProvider? stockProvider = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stockProvider = stockProvider;
        _cache = new ConcurrentDictionary<string, (List<AssetSuggestion>, DateTime)>();
    }

    /// <summary>
    /// Suggests visual assets for a specific scene
    /// </summary>
    public async Task<List<AssetSuggestion>> SuggestAssetsForSceneAsync(
        string sceneHeading, 
        string sceneScript, 
        CancellationToken ct = default)
    {
        // Sanitize heading for logging (remove newlines to prevent log forging)
        var sanitizedHeading = sceneHeading.Replace('\n', ' ').Replace('\r', ' ');
        _logger.LogInformation("Suggesting assets for scene: {Heading}", sanitizedHeading);

        // Check cache first (cache for 1 hour)
        var cacheKey = $"assets_{sceneHeading}_{sceneScript.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out var cachedEntry))
        {
            if (DateTime.UtcNow - cachedEntry.timestamp < TimeSpan.FromHours(1))
            {
                _logger.LogDebug("Returning cached asset suggestions");
                return cachedEntry.suggestions;
            }
        }

        try
        {
            // Create prompt for LLM
            var prompt = $@"For a video scene about '{sceneHeading}' with narration '{sceneScript}', 
suggest 3-5 relevant visual assets (images or short video clips) that would best illustrate this content. 
Provide search keywords and brief description of what each asset should show.

Format your response as:
ASSET 1:
Keywords: [comma-separated keywords]
Description: [brief description]

ASSET 2:
Keywords: [comma-separated keywords]
Description: [brief description]

And so on...";

            // Create a minimal brief and plan spec for the LLM call
            var brief = new Brief(
                Topic: "Visual Asset Suggestion",
                Audience: null,
                Goal: null,
                Tone: "descriptive",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(5),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: prompt
            );

            // Call LLM for suggestions
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);

            // Parse suggestions
            var suggestions = ParseAssetSuggestions(response);

            // If we have a stock provider, search for actual assets
            if (_stockProvider != null)
            {
                foreach (var suggestion in suggestions)
                {
                    try
                    {
                        var assets = await _stockProvider.SearchAsync(suggestion.Keyword, 3, ct).ConfigureAwait(false);
                        
                        var matches = assets.Select((asset, index) => new AssetMatch(
                            FilePath: asset.PathOrUrl,
                            Url: asset.PathOrUrl,
                            RelevanceScore: CalculateRelevanceScore(suggestion.Keyword, asset, index),
                            ThumbnailUrl: asset.PathOrUrl
                        )).ToList();

                        // Update suggestion with matches
                        var updatedSuggestion = suggestion with { Matches = matches };
                        var index = suggestions.IndexOf(suggestion);
                        suggestions[index] = updatedSuggestion;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to search for assets with keyword: {Keyword}", suggestion.Keyword);
                    }
                }
            }

            // Rank by relevance
            suggestions = suggestions.OrderByDescending(s => 
                s.Matches.Any() ? s.Matches.Max(m => m.RelevanceScore) : 0
            ).ToList();

            // Cache the results
            _cache[cacheKey] = (suggestions, DateTime.UtcNow);

            _logger.LogInformation("Generated {Count} asset suggestions", suggestions.Count);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting assets for scene");
            
            // Return default suggestions
            return new List<AssetSuggestion>
            {
                new AssetSuggestion(
                    Keyword: sceneHeading,
                    Description: $"Generic visuals for {sceneHeading}",
                    Matches: new List<AssetMatch>()
                )
            };
        }
    }

    /// <summary>
    /// Suggests assets for multiple scenes in parallel
    /// </summary>
    public async Task<Dictionary<int, List<AssetSuggestion>>> SuggestAssetsForScenesAsync(
        List<Scene> scenes,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Suggesting assets for {Count} scenes in parallel", scenes.Count);

        var tasks = scenes.Select(async scene =>
        {
            var suggestions = await SuggestAssetsForSceneAsync(scene.Heading, scene.Script, ct).ConfigureAwait(false);
            return (scene.Index, suggestions);
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.ToDictionary(r => r.Index, r => r.suggestions);
    }

    private List<AssetSuggestion> ParseAssetSuggestions(string response)
    {
        var suggestions = new List<AssetSuggestion>();

        // Split by ASSET markers
        var assetPattern = @"ASSET\s+\d+:";
        var assetBlocks = Regex.Split(response, assetPattern);

        foreach (var block in assetBlocks.Skip(1)) // Skip first empty block
        {
            var keywords = ExtractField(block, "Keywords:");
            var description = ExtractField(block, "Description:");

            if (!string.IsNullOrEmpty(keywords))
            {
                suggestions.Add(new AssetSuggestion(
                    Keyword: keywords,
                    Description: description ?? "Suggested visual asset",
                    Matches: new List<AssetMatch>()
                ));
            }
        }

        // If parsing failed, create default suggestions
        if (suggestions.Count == 0)
        {
            suggestions.Add(new AssetSuggestion(
                Keyword: "generic stock footage",
                Description: "Default visual asset",
                Matches: new List<AssetMatch>()
            ));
        }

        return suggestions;
    }

    private string? ExtractField(string block, string fieldName)
    {
        var pattern = $@"{fieldName}\s*(.+?)(?:\n|$)";
        var match = Regex.Match(block, pattern, RegexOptions.IgnoreCase);
        
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private double CalculateRelevanceScore(string keyword, Asset asset, int position)
    {
        // Base score decreases with position (first result = higher score)
        var baseScore = 100 - (position * 10);

        // Adjust based on asset metadata if available
        if (asset.PathOrUrl.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        {
            baseScore += 10;
        }

        return Math.Clamp(baseScore, 0, 100);
    }
}
