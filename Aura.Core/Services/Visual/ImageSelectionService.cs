using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Models.Visual;
using Aura.Core.Services.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for selecting optimal images from multiple sources based on visual prompts and aesthetic scoring
/// </summary>
public class ImageSelectionService
{
    private readonly ILogger<ImageSelectionService> _logger;
    private readonly StockImageService _stockImageService;
    private readonly AestheticScoringService _scoringService;
    private readonly VisualKeywordExtractor? _keywordExtractor;

    public ImageSelectionService(
        ILogger<ImageSelectionService> logger,
        StockImageService stockImageService,
        AestheticScoringService scoringService)
        : this(logger, stockImageService, scoringService, null)
    {
    }

    public ImageSelectionService(
        ILogger<ImageSelectionService> logger,
        StockImageService stockImageService,
        AestheticScoringService scoringService,
        VisualKeywordExtractor? keywordExtractor)
    {
        _logger = logger;
        _stockImageService = stockImageService;
        _scoringService = scoringService;
        _keywordExtractor = keywordExtractor;
    }

    /// <summary>
    /// Select best image for a scene based on visual prompt
    /// </summary>
    public async Task<ImageSelectionResult> SelectImageForSceneAsync(
        VisualPrompt prompt,
        ImageSelectionConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new ImageSelectionConfig();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting image selection for scene {SceneIndex} with {CandidateCount} candidates",
            prompt.SceneIndex, config.CandidatesPerScene);

        var candidates = await GenerateCandidatesAsync(prompt, config, ct).ConfigureAwait(false);

        var scoredCandidates = await _scoringService.ScoreAndRankCandidatesAsync(
            candidates,
            prompt,
            config.MinimumAestheticThreshold,
            ct).ConfigureAwait(false);

        var passingCandidates = scoredCandidates
            .Where(c => c.OverallScore >= config.MinimumAestheticThreshold)
            .ToList();

        var selected = passingCandidates.FirstOrDefault();
        var meetsCriteria = selected != null && _scoringService.MeetsCriteria(selected, config.MinimumAestheticThreshold);

        var warnings = new List<string>();
        if (!meetsCriteria)
        {
            warnings.Add("No candidates met the minimum aesthetic threshold");
        }
        if (passingCandidates.Count < 3)
        {
            warnings.Add($"Only {passingCandidates.Count} candidates passed threshold");
        }

        stopwatch.Stop();

        var result = new ImageSelectionResult
        {
            SceneIndex = prompt.SceneIndex,
            SelectedImage = selected,
            Candidates = scoredCandidates.Take(config.CandidatesPerScene).ToList(),
            MinimumAestheticThreshold = config.MinimumAestheticThreshold,
            NarrativeKeywords = prompt.NarrativeKeywords,
            SelectionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
            MeetsCriteria = meetsCriteria,
            Warnings = warnings
        };

        _logger.LogInformation(
            "Image selection complete for scene {SceneIndex}. Selected: {Selected}, Score: {Score:F1}, Time: {Time:F0}ms",
            prompt.SceneIndex,
            selected != null,
            selected?.OverallScore ?? 0,
            result.SelectionTimeMs);

        return result;
    }

    /// <summary>
    /// Generate candidate images from multiple sources
    /// </summary>
    private async Task<IReadOnlyList<ImageCandidate>> GenerateCandidatesAsync(
        VisualPrompt prompt,
        ImageSelectionConfig config,
        CancellationToken ct)
    {
        var candidates = new List<ImageCandidate>();

        var searchQuery = BuildSearchQuery(prompt);

        try
        {
            var stockImages = await _stockImageService.SearchStockImagesAsync(
                searchQuery,
                config.CandidatesPerScene * 2,
                ct).ConfigureAwait(false);

            foreach (var stock in stockImages.Take(config.CandidatesPerScene))
            {
                var licensing = CreateLicensingInfo(stock);
                var candidate = new ImageCandidate
                {
                    ImageUrl = stock.FullSizeUrl,
                    Source = stock.Source,
                    Width = stock.Width,
                    Height = stock.Height,
                    Licensing = licensing,
                    Reasoning = $"Stock image from {stock.Source}",
                    GenerationLatencyMs = 0
                };

                candidates.Add(candidate);
            }

            _logger.LogInformation("Generated {Count} stock image candidates", candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve stock images for scene {SceneIndex}", prompt.SceneIndex);
        }

        if (candidates.Count == 0)
        {
            _logger.LogWarning("No candidates generated, creating fallback");
            candidates.Add(CreateFallbackCandidate(prompt));
        }

        return candidates;
    }

    /// <summary>
    /// Build search query from visual prompt
    /// </summary>
    private string BuildSearchQuery(VisualPrompt prompt)
    {
        // Use intelligent keyword extraction if available
        if (_keywordExtractor != null)
        {
            var keywords = _keywordExtractor.ExtractKeywords(
                prompt.Subject,
                prompt.DetailedDescription,
                maxKeywords: 5);

            if (keywords.Count > 0)
            {
                var style = prompt.StyleKeywords?.FirstOrDefault(k =>
                    !k.Contains("quality") && !k.Contains("professional"));

                var query = _keywordExtractor.BuildSearchQuery(keywords, prompt.Subject, style);
                _logger.LogDebug("Built intelligent search query: {Query}", query);
                return query;
            }
        }

        // Fallback to basic query building
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(prompt.Subject))
        {
            queryParts.Add(prompt.Subject);
        }

        if (prompt.NarrativeKeywords != null && prompt.NarrativeKeywords.Count > 0)
        {
            queryParts.AddRange(prompt.NarrativeKeywords.Take(3));
        }

        if (prompt.StyleKeywords != null && prompt.StyleKeywords.Count > 0)
        {
            var styleKeyword = prompt.StyleKeywords.FirstOrDefault(k =>
                !k.Contains("quality") && !k.Contains("professional"));
            if (styleKeyword != null)
            {
                queryParts.Add(styleKeyword);
            }
        }

        if (prompt.Lighting != null && prompt.Lighting.TimeOfDay != "day")
        {
            queryParts.Add(prompt.Lighting.TimeOfDay);
        }

        var finalQuery = string.Join(" ", queryParts);
        if (string.IsNullOrEmpty(finalQuery))
        {
            var words = prompt.DetailedDescription.Split(' ');
            finalQuery = string.Join(" ", words.Take(Math.Min(5, words.Length)));
        }

        _logger.LogDebug("Built search query: {Query}", finalQuery);
        return finalQuery;
    }

    /// <summary>
    /// Create licensing information from stock image
    /// </summary>
    private LicensingInfo CreateLicensingInfo(StockImage stock)
    {
        var licenseType = stock.Source switch
        {
            "Pexels" => "Pexels License",
            "Pixabay" => "Pixabay License",
            "Unsplash" => "Unsplash License",
            _ => "Unknown"
        };

        var licenseUrl = stock.Source switch
        {
            "Pexels" => "https://www.pexels.com/license/",
            "Pixabay" => "https://pixabay.com/service/license/",
            "Unsplash" => "https://unsplash.com/license",
            _ => null
        };

        return new LicensingInfo
        {
            LicenseType = licenseType,
            LicenseUrl = licenseUrl,
            CommercialUseAllowed = true,
            AttributionRequired = stock.Source == "Unsplash",
            CreatorName = stock.Photographer,
            CreatorUrl = stock.PhotographerUrl,
            SourcePlatform = stock.Source,
            Attribution = stock.Photographer != null && stock.Source == "Unsplash"
                ? $"Photo by {stock.Photographer} on {stock.Source}"
                : null
        };
    }

    /// <summary>
    /// Create fallback candidate when no real images available
    /// </summary>
    private ImageCandidate CreateFallbackCandidate(VisualPrompt prompt)
    {
        _logger.LogWarning("Creating fallback candidate for scene {SceneIndex}", prompt.SceneIndex);

        return new ImageCandidate
        {
            ImageUrl = "fallback://solid-color",
            Source = "Fallback",
            Width = 1920,
            Height = 1080,
            AestheticScore = 40.0,
            KeywordCoverageScore = 30.0,
            QualityScore = 50.0,
            OverallScore = 40.0,
            Reasoning = "Fallback solid color image - no candidates available",
            Licensing = new LicensingInfo
            {
                LicenseType = "Internal",
                CommercialUseAllowed = true,
                AttributionRequired = false,
                SourcePlatform = "Aura"
            },
            GenerationLatencyMs = 0
        };
    }

    /// <summary>
    /// Select images for all scenes in batch
    /// </summary>
    public async Task<IReadOnlyList<ImageSelectionResult>> SelectImagesForScenesAsync(
        IReadOnlyList<VisualPrompt> prompts,
        ImageSelectionConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new ImageSelectionConfig();
        var results = new List<ImageSelectionResult>();

        _logger.LogInformation("Selecting images for {Count} scenes", prompts.Count);

        foreach (var prompt in prompts)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var result = await SelectImageForSceneAsync(prompt, config, ct).ConfigureAwait(false);
            results.Add(result);
        }

        var successCount = results.Count(r => r.MeetsCriteria);
        _logger.LogInformation(
            "Batch selection complete. {Success}/{Total} scenes met criteria",
            successCount, results.Count);

        return results;
    }
}
