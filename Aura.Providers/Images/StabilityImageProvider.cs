using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Stability AI image provider for cloud-based text-to-image generation
/// </summary>
public class StabilityImageProvider
{
    private readonly ILogger<StabilityImageProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _baseUrl;

    public StabilityImageProvider(
        ILogger<StabilityImageProvider> logger,
        HttpClient httpClient,
        string? apiKey,
        string? baseUrl = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.stability.ai";
    }

    public async Task<string?> GenerateImageAsync(
        string prompt,
        int width = 1024,
        int height = 1024,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Stability AI API key not configured");
            return null;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("Accept", "application/json");

            var requestBody = new
            {
                text_prompts = new[]
                {
                    new { text = prompt, weight = 1.0 }
                },
                cfg_scale = 7,
                height = height,
                width = width,
                samples = 1,
                steps = 30
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stability AI request failed: {Status} - {Content}", response.StatusCode, content);
                return null;
            }

            var result = JsonSerializer.Deserialize<StabilityResponse>(content);
            if (result?.Artifacts != null && result.Artifacts.Count > 0)
            {
                // Return base64 image or URL
                return result.Artifacts[0].Base64;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with Stability AI");
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
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v1/user/account");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private sealed class StabilityResponse
    {
        public List<Artifact>? Artifacts { get; set; }
    }

    private sealed class Artifact
    {
        public string? Base64 { get; set; }
        public string? FinishReason { get; set; }
        public int Seed { get; set; }
    }
}
