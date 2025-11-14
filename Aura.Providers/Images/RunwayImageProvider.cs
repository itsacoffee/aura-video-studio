using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Runway ML image provider for cloud-based text-to-image generation
/// </summary>
public class RunwayImageProvider
{
    private readonly ILogger<RunwayImageProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public RunwayImageProvider(
        ILogger<RunwayImageProvider> logger,
        HttpClient httpClient,
        string? apiKey,
        string? baseUrl = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.runwayml.com";
    }

    public async Task<string?> GenerateImageAsync(
        string prompt,
        int width = 1024,
        int height = 1024,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Runway ML API key not configured");
            return null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/images");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("Accept", "application/json");

            var requestBody = new
            {
                prompt = prompt,
                width = width,
                height = height,
                num_outputs = 1
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Runway ML request failed: {Status} - {Content}", response.StatusCode, content);
                return null;
            }

            var result = JsonSerializer.Deserialize<RunwayResponse>(content);
            if (result?.Images != null && result.Images.Count > 0)
            {
                return result.Images[0];
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with Runway ML");
            return null;
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return false;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/health");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private sealed class RunwayResponse
    {
        public List<string>? Images { get; set; }
    }
}
