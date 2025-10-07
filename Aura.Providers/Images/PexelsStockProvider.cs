using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Stock image provider that fetches free images from Pexels API.
/// </summary>
public class PexelsStockProvider : IStockProvider
{
    private readonly ILogger<PexelsStockProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public PexelsStockProvider(
        ILogger<PexelsStockProvider> logger,
        HttpClient httpClient,
        string? apiKey = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        }
    }

    public async Task<IReadOnlyList<Asset>> SearchAsync(string query, int count, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Pexels API key not provided, returning empty results");
            return Array.Empty<Asset>();
        }

        _logger.LogInformation("Searching Pexels for: {Query} (count: {Count})", query, count);

        try
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page={Math.Min(count, 80)}";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var assets = new List<Asset>();

            if (doc.RootElement.TryGetProperty("photos", out var photos))
            {
                foreach (var photo in photos.EnumerateArray())
                {
                    if (assets.Count >= count)
                        break;

                    string? photoUrl = null;
                    if (photo.TryGetProperty("src", out var src) &&
                        src.TryGetProperty("large2x", out var large2x))
                    {
                        photoUrl = large2x.GetString();
                    }

                    string? photographer = null;
                    if (photo.TryGetProperty("photographer", out var photoProp))
                    {
                        photographer = photoProp.GetString();
                    }

                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        assets.Add(new Asset(
                            Kind: "image",
                            PathOrUrl: photoUrl,
                            License: "Pexels License (Free to use)",
                            Attribution: photographer != null ? $"Photo by {photographer} on Pexels" : "Photo from Pexels"
                        ));
                    }
                }
            }

            _logger.LogInformation("Found {Count} images on Pexels", assets.Count);
            return assets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Pexels for {Query}", query);
            return Array.Empty<Asset>();
        }
    }
}
