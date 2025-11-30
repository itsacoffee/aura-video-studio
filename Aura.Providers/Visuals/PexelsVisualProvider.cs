using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Aura.Providers.Images;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Visuals;

/// <summary>
/// Visual provider that uses Pexels stock photos for scene images.
/// Pexels provides high-quality free stock photos with flexible licensing.
/// API Documentation: https://www.pexels.com/api/documentation/
/// </summary>
public class PexelsVisualProvider : BaseVisualProvider
{
    private readonly EnhancedPexelsProvider _pexelsProvider;
    private readonly HttpClient _httpClient;

    public PexelsVisualProvider(
        ILogger<PexelsVisualProvider> logger,
        HttpClient httpClient,
        string? apiKey) : base(logger)
    {
        _httpClient = httpClient;
        _pexelsProvider = new EnhancedPexelsProvider(
            Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger<EnhancedPexelsProvider>(),
            httpClient,
            apiKey);
    }

    public override string ProviderName => "Pexels";

    public override bool RequiresApiKey => true;

    public override async Task<string?> GenerateImageAsync(
        string prompt,
        VisualGenerationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("Searching Pexels for: {Prompt}", prompt);

            // Extract keywords from the visual prompt for better search results
            var searchQuery = ExtractSearchKeywords(prompt);
            Logger.LogDebug("Pexels search query: {SearchQuery}", searchQuery);

            var searchRequest = new StockMediaSearchRequest
            {
                Query = searchQuery,
                Count = 3, // Get top 3 results to have options
                Page = 1,
                SafeSearchEnabled = true,
                Orientation = GetOrientation(options.AspectRatio),
                Type = StockMediaType.Image
            };

            var results = await _pexelsProvider.SearchAsync(searchRequest, ct).ConfigureAwait(false);

            if (results == null || results.Count == 0)
            {
                Logger.LogWarning("No Pexels results found for: {Prompt} (query: {SearchQuery})", prompt, searchQuery);
                return null;
            }

            // Select the best result (first one, highest relevance from Pexels API)
            var bestResult = results.First();
            Logger.LogInformation("Selected Pexels image: {Id} by {CreatorName}", bestResult.Id, bestResult.Licensing.CreatorName ?? "Pexels");

            // Download the image at appropriate size
            var downloadUrl = GetBestSizeUrl(bestResult, options);
            var imageBytes = await _pexelsProvider.DownloadMediaAsync(downloadUrl, ct).ConfigureAwait(false);

            if (imageBytes == null || imageBytes.Length == 0)
            {
                Logger.LogWarning("Failed to download Pexels image: {Id}", bestResult.Id);
                return null;
            }

            // Save to temp file
            var extension = GetFileExtension(downloadUrl);
            var tempPath = Path.Combine(Path.GetTempPath(), $"pexels_{Guid.NewGuid()}{extension}");
            await File.WriteAllBytesAsync(tempPath, imageBytes, ct).ConfigureAwait(false);

            Logger.LogInformation("Downloaded Pexels image: {Id} to {Path} ({Size} bytes)", 
                bestResult.Id, tempPath, imageBytes.Length);
            return tempPath;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get image from Pexels for prompt: {Prompt}", prompt);
            return null;
        }
    }

    public override VisualProviderCapabilities GetProviderCapabilities()
    {
        return new VisualProviderCapabilities
        {
            ProviderName = ProviderName,
            SupportsNegativePrompts = false,
            SupportsBatchGeneration = true,
            SupportsStylePresets = false,
            SupportedAspectRatios = new() { "16:9", "9:16", "1:1", "4:3" },
            SupportedStyles = new() { "photorealistic", "natural", "professional", "vibrant" },
            MaxWidth = 6000,
            MaxHeight = 4000,
            IsLocal = false,
            IsFree = true, // Pexels offers free API access
            CostPerImage = 0m,
            Tier = "Free"
        };
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            return await _pexelsProvider.ValidateAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Pexels provider availability check failed");
            return false;
        }
    }

    public override string AdaptPrompt(string prompt, VisualGenerationOptions options)
    {
        return ExtractSearchKeywords(prompt);
    }

    /// <summary>
    /// Extracts relevant search keywords from a visual prompt.
    /// Removes style modifiers and focuses on subject matter for stock photo search.
    /// </summary>
    private static string ExtractSearchKeywords(string prompt)
    {
        // Remove common style prefixes like "modern style: " or "cinematic style: "
        var cleanPrompt = prompt;
        var stylePatterns = new[] { "style:", "cinematic", "modern", "minimal", "playful", "professional" };
        foreach (var pattern in stylePatterns)
        {
            cleanPrompt = cleanPrompt.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        // Split into words and filter
        var words = cleanPrompt.Split(new[] { ' ', ',', '.', ':', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Filter out stop words and very short words, take up to 6 keywords
        var keywords = words
            .Where(w => w.Length > 2 && !IsStopWord(w))
            .Take(6)
            .ToList();

        if (keywords.Count == 0)
        {
            // Fallback: use first few words if filtering removed everything
            keywords = words.Take(3).ToList();
        }

        return string.Join(" ", keywords);
    }

    private static bool IsStopWord(string word)
    {
        var stopWords = new[] 
        { 
            "the", "and", "with", "that", "this", "from", "have", "will", "would", 
            "could", "should", "for", "are", "was", "were", "been", "being", "has",
            "not", "but", "can", "did", "does", "doing", "done", "into", "about"
        };
        return stopWords.Contains(word.ToLowerInvariant());
    }

    private static string? GetOrientation(string aspectRatio)
    {
        return aspectRatio switch
        {
            "16:9" or "4:3" => "landscape",
            "9:16" or "3:4" => "portrait",
            "1:1" => "square",
            _ => null
        };
    }

    /// <summary>
    /// Get the best size URL based on the requested dimensions
    /// </summary>
    private string GetBestSizeUrl(StockMediaResult result, VisualGenerationOptions options)
    {
        // For most video scene images, use the large or medium size
        // FullSizeUrl is typically very large and may be overkill
        if (options.Width > 1920 || options.Height > 1080)
        {
            return result.FullSizeUrl;
        }
        
        // Use thumbnail URL as fallback, but prefer full size for quality
        return result.FullSizeUrl ?? result.ThumbnailUrl;
    }

    private static string GetFileExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var ext = Path.GetExtension(path);
            return string.IsNullOrEmpty(ext) ? ".jpg" : ext;
        }
        catch
        {
            return ".jpg";
        }
    }
}
