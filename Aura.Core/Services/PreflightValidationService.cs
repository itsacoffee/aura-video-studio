using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for validating provider API keys and connectivity
/// </summary>
public class PreflightValidationService
{
    private readonly ILogger<PreflightValidationService> _logger;
    private readonly IKeyStore _keyStore;
    private readonly HttpClient _httpClient;

    public PreflightValidationService(
        ILogger<PreflightValidationService> logger,
        IKeyStore keyStore,
        HttpClient httpClient)
    {
        _logger = logger;
        _keyStore = keyStore;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Test an API key for a specific provider
    /// </summary>
    public async Task<ProviderTestResult> TestProviderAsync(
        string providerName,
        string? apiKey = null,
        CancellationToken ct = default)
    {
        var key = apiKey ?? _keyStore.GetKey(providerName);
        
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ProviderTestResult
            {
                Provider = providerName,
                Success = false,
                Message = "API key not set"
            };
        }

        try
        {
            var maskedKey = SecretMaskingService.MaskApiKey(key);
            _logger.LogInformation("Testing {Provider} with key {MaskedKey}", providerName, maskedKey);

            return providerName.ToLowerInvariant() switch
            {
                "openai" => await TestOpenAIAsync(key, ct).ConfigureAwait(false),
                "anthropic" => await TestAnthropicAsync(key, ct).ConfigureAwait(false),
                "elevenlabs" => await TestElevenLabsAsync(key, ct).ConfigureAwait(false),
                "stabilityai" => await TestStabilityAIAsync(key, ct).ConfigureAwait(false),
                "pexels" => await TestPexelsAsync(key, ct).ConfigureAwait(false),
                "pixabay" => await TestPixabayAsync(key, ct).ConfigureAwait(false),
                "unsplash" => await TestUnsplashAsync(key, ct).ConfigureAwait(false),
                _ => new ProviderTestResult
                {
                    Provider = providerName,
                    Success = false,
                    Message = "Provider validation not implemented"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error testing {Provider}", providerName);
            return new ProviderTestResult
            {
                Provider = providerName,
                Success = false,
                Message = $"Connection error: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestOpenAIAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "openai",
                    Success = true,
                    Message = "OpenAI API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "openai",
                Success = false,
                Message = $"OpenAI API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "openai",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestAnthropicAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "anthropic",
                    Success = true,
                    Message = "Anthropic API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "anthropic",
                Success = false,
                Message = $"Anthropic API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "anthropic",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestElevenLabsAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
            request.Headers.Add("xi-api-key", apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "elevenlabs",
                    Success = true,
                    Message = "ElevenLabs API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "elevenlabs",
                Success = false,
                Message = $"ElevenLabs API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "elevenlabs",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestStabilityAIAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.stability.ai/v1/user/account");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "stabilityai",
                    Success = true,
                    Message = "Stability AI API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "stabilityai",
                Success = false,
                Message = $"Stability AI API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "stabilityai",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestPexelsAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.pexels.com/v1/curated?per_page=1");
            request.Headers.Add("Authorization", apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "pexels",
                    Success = true,
                    Message = "Pexels API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "pexels",
                Success = false,
                Message = $"Pexels API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "pexels",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestPixabayAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var url = $"https://pixabay.com/api/?key={apiKey}&q=test&per_page=3";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "pixabay",
                    Success = true,
                    Message = "Pixabay API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "pixabay",
                Success = false,
                Message = $"Pixabay API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "pixabay",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<ProviderTestResult> TestUnsplashAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.unsplash.com/photos?per_page=1");
            request.Headers.Add("Authorization", $"Client-ID {apiKey}");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return new ProviderTestResult
                {
                    Provider = "unsplash",
                    Success = true,
                    Message = "Unsplash API key is valid"
                };
            }

            return new ProviderTestResult
            {
                Provider = "unsplash",
                Success = false,
                Message = $"Unsplash API returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResult
            {
                Provider = "unsplash",
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result of provider API key test
/// </summary>
public record ProviderTestResult
{
    public string Provider { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime TestedAt { get; init; } = DateTime.UtcNow;
}
