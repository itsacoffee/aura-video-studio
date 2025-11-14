using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for validating API keys by testing actual provider connections
/// </summary>
public class KeyValidationService : IKeyValidationService
{
    private readonly ILogger<KeyValidationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeyValidationService(
        ILogger<KeyValidationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Test an API key by making a real connection to the provider
    /// </summary>
    public async Task<KeyValidationResult> TestApiKeyAsync(
        string provider, 
        string apiKey, 
        CancellationToken ct)
    {
        var providerLower = provider.ToLowerInvariant();

        try
        {
            _logger.LogInformation("Testing API key for provider: {Provider}", 
                SecretMaskingService.SanitizeForLogging(provider));

            return providerLower switch
            {
                "openai" => await TestOpenAIKeyAsync(apiKey, ct).ConfigureAwait(false),
                "anthropic" => await TestAnthropicKeyAsync(apiKey, ct).ConfigureAwait(false),
                "gemini" or "google" => await TestGeminiKeyAsync(apiKey, ct).ConfigureAwait(false),
                "elevenlabs" => await TestElevenLabsKeyAsync(apiKey, ct).ConfigureAwait(false),
                "stabilityai" or "stability" => await TestStabilityAIKeyAsync(apiKey, ct).ConfigureAwait(false),
                "playht" => await TestPlayHTKeyAsync(apiKey, ct).ConfigureAwait(false),
                "pexels" => await TestPexelsKeyAsync(apiKey, ct).ConfigureAwait(false),
                "azure" => await TestAzureKeyAsync(apiKey, ct).ConfigureAwait(false),
                _ => new KeyValidationResult
                {
                    IsValid = false,
                    Message = $"Provider '{provider}' is not supported for API key testing",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = provider,
                        ["reason"] = "unsupported"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing API key for provider: {Provider}", provider);
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing API key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = provider,
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestOpenAIKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("https://api.openai.com/v1/models", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "OpenAI API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "openai",
                        ["status"] = "connected"
                    }
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"OpenAI API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "openai",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (TaskCanceledException)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = "OpenAI API request timed out. Please check your internet connection.",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "openai",
                    ["error"] = "timeout"
                }
            };
        }
        catch (HttpRequestException ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Network error testing OpenAI key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "openai",
                    ["error"] = "network"
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestAnthropicKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.Timeout = TimeSpan.FromSeconds(15);

            // Test with a minimal completion request
            var requestBody = new
            {
                model = "claude-3-haiku-20240307",
                max_tokens = 1,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                "https://api.anthropic.com/v1/messages", 
                content, 
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "Anthropic API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "anthropic",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Anthropic API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "anthropic",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (TaskCanceledException)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = "Anthropic API request timed out. Please check your internet connection.",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "anthropic",
                    ["error"] = "timeout"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing Anthropic key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "anthropic",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestGeminiKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            // Test with models list endpoint
            var response = await client.GetAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}",
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "Google Gemini API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "gemini",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Gemini API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "gemini",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing Gemini key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "gemini",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestElevenLabsKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("https://api.elevenlabs.io/v1/voices", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "ElevenLabs API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "elevenlabs",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"ElevenLabs API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "elevenlabs",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing ElevenLabs key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "elevenlabs",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestStabilityAIKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync(
                "https://api.stability.ai/v1/user/account",
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "Stability AI API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "stabilityai",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Stability AI API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "stabilityai",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing Stability AI key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "stabilityai",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestPlayHTKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync("https://api.play.ht/api/v2/voices", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "PlayHT API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "playht",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"PlayHT API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "playht",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing PlayHT key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "playht",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestPexelsKeyAsync(string apiKey, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", apiKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            // Test with a simple search query (1 result, minimal data transfer)
            var response = await client.GetAsync(
                "https://api.pexels.com/v1/search?query=nature&per_page=1",
                ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new KeyValidationResult
                {
                    IsValid = true,
                    Message = "Pexels API key is valid and working",
                    Details = new Dictionary<string, string>
                    {
                        ["provider"] = "pexels",
                        ["status"] = "connected"
                    }
                };
            }

            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Pexels API key validation failed: {response.StatusCode}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "pexels",
                    ["status_code"] = ((int)response.StatusCode).ToString(),
                    ["reason"] = response.ReasonPhrase ?? "Unknown error"
                }
            };
        }
        catch (TaskCanceledException)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = "Pexels API request timed out. Please check your internet connection.",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "pexels",
                    ["error"] = "timeout"
                }
            };
        }
        catch (Exception ex)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = $"Error testing Pexels key: {ex.Message}",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "pexels",
                    ["error"] = ex.GetType().Name
                }
            };
        }
    }

    private async Task<KeyValidationResult> TestAzureKeyAsync(string apiKey, CancellationToken ct)
    {
        await Task.CompletedTask;
        // Azure requires both key and endpoint, so we can only do basic validation
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length < 20)
        {
            return new KeyValidationResult
            {
                IsValid = false,
                Message = "Azure API key appears to be invalid (too short)",
                Details = new Dictionary<string, string>
                {
                    ["provider"] = "azure",
                    ["validation"] = "format"
                }
            };
        }

        return new KeyValidationResult
        {
            IsValid = true,
            Message = "Azure API key format is valid (endpoint required for full test)",
            Details = new Dictionary<string, string>
            {
                ["provider"] = "azure",
                ["validation"] = "format_only",
                ["note"] = "Full connection test requires endpoint URL"
            }
        };
    }
}
