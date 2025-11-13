using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Enhanced visual asset selector with keyword extraction and semantic matching
/// </summary>
public class VisualSelectorService
{
    private readonly ILogger<VisualSelectorService> _logger;
    private readonly IImageProvider? _imageProvider;
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
        "been", "being", "have", "has", "had", "do", "does", "did", "will",
        "would", "should", "could", "may", "might", "can", "this", "that",
        "these", "those", "it", "its", "i", "you", "we", "they", "them"
    };

    public VisualSelectorService(
        ILogger<VisualSelectorService> logger,
        IImageProvider? imageProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageProvider = imageProvider;
    }

    /// <summary>
    /// Selects visual assets for scenes using keyword extraction and provider calls
    /// </summary>
    public async Task<VisualSelectionResult> SelectVisualsForScenesAsync(
        IReadOnlyList<Scene> scenes,
        VisualSpec visualSpec,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Selecting visuals for {SceneCount} scenes", scenes.Count);

        var result = new VisualSelectionResult
        {
            SceneAssets = new Dictionary<int, IReadOnlyList<Asset>>()
        };

        foreach (var scene in scenes)
        {
            try
            {
                var assets = await SelectVisualForSceneAsync(
                    scene,
                    visualSpec,
                    cancellationToken).ConfigureAwait(false);

                result.SceneAssets[scene.Index] = assets;
                result.SuccessfulSelections++;

                _logger.LogDebug(
                    "Selected {AssetCount} assets for scene {SceneIndex}",
                    assets.Count,
                    scene.Index);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to select visual for scene {SceneIndex}, using fallback",
                    scene.Index);

                result.SceneAssets[scene.Index] = CreateFallbackAssets(scene);
                result.FallbackSelections++;
            }
        }

        _logger.LogInformation(
            "Visual selection complete: {Successful} successful, {Fallback} fallback",
            result.SuccessfulSelections,
            result.FallbackSelections);

        return result;
    }

    private async Task<IReadOnlyList<Aura.Core.Models.Asset>> SelectVisualForSceneAsync(
        Scene scene,
        VisualSpec visualSpec,
        CancellationToken cancellationToken)
    {
        // Extract keywords from scene content
        var keywords = ExtractKeywords(scene.Heading, scene.Script);

        _logger.LogDebug(
            "Extracted keywords for scene {SceneIndex}: {Keywords}",
            scene.Index,
            string.Join(", ", keywords));

        // Create enhanced visual spec with keywords
        var enhancedSpec = new VisualSpec(
            Style: visualSpec.Style,
            Aspect: visualSpec.Aspect,
            Keywords: keywords.ToArray()
        );

        // Try to get assets from provider
        if (_imageProvider != null)
        {
            try
            {
                var assets = await _imageProvider.FetchOrGenerateAsync(
                    scene,
                    enhancedSpec,
                    cancellationToken).ConfigureAwait(false);

                if (assets != null && assets.Count > 0)
                {
                    return assets;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Image provider failed for scene {SceneIndex}, falling back",
                    scene.Index);
            }
        }

        // Fallback to default assets
        return CreateFallbackAssets(scene);
    }

    /// <summary>
    /// Extracts meaningful keywords from heading and script
    /// </summary>
    public List<string> ExtractKeywords(string heading, string script)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract from heading (higher priority)
        if (!string.IsNullOrWhiteSpace(heading))
        {
            var headingWords = ExtractWordsFromText(heading);
            foreach (var word in headingWords.Take(5)) // Top 5 from heading
            {
                keywords.Add(word);
            }
        }

        // Extract from script
        if (!string.IsNullOrWhiteSpace(script))
        {
            var scriptWords = ExtractWordsFromText(script);
            
            // Add nouns and important words from script
            foreach (var word in scriptWords.Take(10))
            {
                if (!keywords.Contains(word))
                {
                    keywords.Add(word);
                    if (keywords.Count >= 8) // Limit to 8 keywords total
                        break;
                }
            }
        }

        return keywords.ToList();
    }

    private List<string> ExtractWordsFromText(string text)
    {
        // Remove punctuation and split into words
        var cleanText = Regex.Replace(text, @"[^\w\s]", " ");
        var words = cleanText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Filter out stop words and short words, count frequency
        var wordFrequency = words
            .Where(w => w.Length >= 3 && !StopWords.Contains(w))
            .GroupBy(w => w.ToLowerInvariant())
            .Select(g => new { Word = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Word.Length)
            .Select(x => x.Word)
            .ToList();

        return wordFrequency;
    }

    private IReadOnlyList<Aura.Core.Models.Asset> CreateFallbackAssets(Scene scene)
    {
        // Create empty list as fallback - composition will use default backgrounds
        return Array.Empty<Aura.Core.Models.Asset>();
    }
}

/// <summary>
/// Result of visual selection operation
/// </summary>
public class VisualSelectionResult
{
    public Dictionary<int, IReadOnlyList<Aura.Core.Models.Asset>> SceneAssets { get; set; } = new();
    public int SuccessfulSelections { get; set; }
    public int FallbackSelections { get; set; }

    public int TotalScenes => SuccessfulSelections + FallbackSelections;
    public double SuccessRate => TotalScenes > 0 ? (SuccessfulSelections / (double)TotalScenes) * 100.0 : 0.0;
}
