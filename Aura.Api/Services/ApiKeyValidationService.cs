using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Service for validating API keys across multiple providers
/// </summary>
public class ApiKeyValidationService
{
    private readonly ILogger<ApiKeyValidationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private const int ValidationTimeoutSeconds = 10;

    public ApiKeyValidationService(
        ILogger<ApiKeyValidationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Validates an OpenAI API key by calling the models endpoint
    /// </summary>
    public async Task<ValidationResult> ValidateOpenAIKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidationResult.Failure(
                "API key is required",
                "MISSING_KEY",
                new List<string> { "Please provide your OpenAI API key" },
                "https://platform.openai.com/api-keys"
            );
        }

        try
        {
            _logger.LogInformation("Validating OpenAI API key (masked: {MaskedKey})", MaskApiKey(apiKey));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OpenAI API key validated successfully");
                return ValidationResult.Success(new Dictionary<string, object>
                {
                    { "provider", "OpenAI" },
                    { "status", "active" }
                });
            }

            return HandleHttpError(response.StatusCode, "OpenAI");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("OpenAI API key validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://status.openai.com/"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API key validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify the API key is correct" },
                "https://platform.openai.com/docs"
            );
        }
    }

    /// <summary>
    /// Validates an Anthropic (Claude) API key by calling the messages endpoint
    /// </summary>
    public async Task<ValidationResult> ValidateAnthropicKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidationResult.Failure(
                "API key is required",
                "MISSING_KEY",
                new List<string> { "Please provide your Anthropic API key" },
                "https://console.anthropic.com/settings/keys"
            );
        }

        try
        {
            _logger.LogInformation("Validating Anthropic API key (masked: {MaskedKey})", MaskApiKey(apiKey));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            // Anthropic uses a minimal test to check key validity
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var testPayload = new
            {
                model = "claude-3-haiku-20240307",
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "test" } }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(testPayload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Anthropic API key validated successfully");
                return ValidationResult.Success(new Dictionary<string, object>
                {
                    { "provider", "Anthropic" },
                    { "status", "active" }
                });
            }

            return HandleHttpError(response.StatusCode, "Anthropic");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Anthropic API key validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://status.anthropic.com/"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic API key validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify the API key is correct" },
                "https://docs.anthropic.com/"
            );
        }
    }

    /// <summary>
    /// Validates a Google Gemini API key by calling the models endpoint
    /// </summary>
    public async Task<ValidationResult> ValidateGeminiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidationResult.Failure(
                "API key is required",
                "MISSING_KEY",
                new List<string> { "Please provide your Google AI Studio API key" },
                "https://makersuite.google.com/app/apikey"
            );
        }

        try
        {
            _logger.LogInformation("Validating Gemini API key (masked: {MaskedKey})", MaskApiKey(apiKey));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            var url = $"https://generativelanguage.googleapis.com/v1/models?key={apiKey}";
            var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gemini API key validated successfully");
                return ValidationResult.Success(new Dictionary<string, object>
                {
                    { "provider", "Google Gemini" },
                    { "status", "active" }
                });
            }

            return HandleHttpError(response.StatusCode, "Gemini");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Gemini API key validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://status.cloud.google.com/"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API key validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify the API key is correct" },
                "https://ai.google.dev/docs"
            );
        }
    }

    /// <summary>
    /// Validates an ElevenLabs API key by calling the user endpoint
    /// </summary>
    public async Task<ValidationResult> ValidateElevenLabsKeyAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ValidationResult.Failure(
                "API key is required",
                "MISSING_KEY",
                new List<string> { "Please provide your ElevenLabs API key" },
                "https://elevenlabs.io/app/settings/api-keys"
            );
        }

        try
        {
            _logger.LogInformation("Validating ElevenLabs API key (masked: {MaskedKey})", MaskApiKey(apiKey));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/user");
            request.Headers.Add("xi-api-key", apiKey);

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var accountInfo = new Dictionary<string, object> { { "provider", "ElevenLabs" }, { "status", "active" } };

                try
                {
                    var userData = JsonSerializer.Deserialize<JsonElement>(content);
                    if (userData.TryGetProperty("subscription", out var subscription))
                    {
                        if (subscription.TryGetProperty("tier", out var tier))
                        {
                            accountInfo["tier"] = tier.GetString() ?? "unknown";
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors, just return basic info
                }

                _logger.LogInformation("ElevenLabs API key validated successfully");
                return ValidationResult.Success(accountInfo);
            }

            return HandleHttpError(response.StatusCode, "ElevenLabs");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("ElevenLabs API key validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://status.elevenlabs.io/"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ElevenLabs API key validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify the API key is correct" },
                "https://docs.elevenlabs.io/"
            );
        }
    }

    /// <summary>
    /// Validates PlayHT API credentials by calling the voices endpoint
    /// </summary>
    public async Task<ValidationResult> ValidatePlayHTKeyAsync(string userId, string secretKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(secretKey))
        {
            return ValidationResult.Failure(
                "Both User ID and Secret Key are required",
                "MISSING_CREDENTIALS",
                new List<string> { "Please provide both your PlayHT User ID and Secret Key" },
                "https://play.ht/app/api-access"
            );
        }

        try
        {
            _logger.LogInformation("Validating PlayHT credentials (userId: {UserId})", userId);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.play.ht/api/v2/voices");
            request.Headers.Add("X-USER-ID", userId);
            request.Headers.Add("AUTHORIZATION", secretKey);

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("PlayHT credentials validated successfully");
                return ValidationResult.Success(new Dictionary<string, object>
                {
                    { "provider", "PlayHT" },
                    { "userId", userId },
                    { "status", "active" }
                });
            }

            return HandleHttpError(response.StatusCode, "PlayHT");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("PlayHT credentials validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://play.ht/status"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PlayHT credentials validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify both User ID and Secret Key are correct" },
                "https://docs.play.ht/"
            );
        }
    }

    /// <summary>
    /// Validates a Replicate API token by calling the account endpoint
    /// </summary>
    public async Task<ValidationResult> ValidateReplicateKeyAsync(string apiToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            return ValidationResult.Failure(
                "API token is required",
                "MISSING_KEY",
                new List<string> { "Please provide your Replicate API token" },
                "https://replicate.com/account/api-tokens"
            );
        }

        try
        {
            _logger.LogInformation("Validating Replicate API token (masked: {MaskedKey})", MaskApiKey(apiToken));

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ValidationTimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.replicate.com/v1/account");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiToken);

            var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var accountInfo = new Dictionary<string, object> { { "provider", "Replicate" }, { "status", "active" } };

                try
                {
                    var userData = JsonSerializer.Deserialize<JsonElement>(content);
                    if (userData.TryGetProperty("username", out var username))
                    {
                        accountInfo["username"] = username.GetString() ?? "unknown";
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors, just return basic info
                }

                _logger.LogInformation("Replicate API token validated successfully");
                return ValidationResult.Success(accountInfo);
            }

            return HandleHttpError(response.StatusCode, "Replicate");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Replicate API token validation timed out");
            return ValidationResult.Failure(
                "Connection timeout. Check your internet connection.",
                "TIMEOUT",
                new List<string> { "Ensure you have a stable internet connection", "Try again in a few moments" },
                "https://status.replicate.com/"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replicate API token validation failed with exception");
            return ValidationResult.Failure(
                $"Validation failed: {ex.Message}",
                "VALIDATION_ERROR",
                new List<string> { "Check your internet connection", "Verify the API token is correct" },
                "https://replicate.com/docs"
            );
        }
    }

    /// <summary>
    /// Handles HTTP error responses and returns appropriate validation results
    /// </summary>
    private ValidationResult HandleHttpError(HttpStatusCode statusCode, string providerName)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => ValidationResult.Failure(
                "Invalid API key. Verify you copied the entire key including prefix.",
                "INVALID_KEY",
                new List<string>
                {
                    "Double-check that you copied the complete API key",
                    "Ensure there are no extra spaces or characters",
                    "Generate a new API key if the issue persists"
                },
                GetProviderDocsUrl(providerName)
            ),
            HttpStatusCode.Forbidden => ValidationResult.Failure(
                "API key valid but lacks permissions. Check your account settings.",
                "INSUFFICIENT_PERMISSIONS",
                new List<string>
                {
                    "Verify your account has the necessary permissions",
                    "Check if your API key has the required scopes",
                    "Ensure your subscription is active"
                },
                GetProviderDocsUrl(providerName)
            ),
            HttpStatusCode.TooManyRequests => ValidationResult.Failure(
                "Rate limit exceeded. Wait 60 seconds and try again.",
                "RATE_LIMIT",
                new List<string>
                {
                    "Wait a minute before trying again",
                    "Check your API usage limits",
                    "Consider upgrading your plan if you frequently hit limits"
                },
                GetProviderDocsUrl(providerName)
            ),
            _ => ValidationResult.Failure(
                $"Validation failed with HTTP {(int)statusCode}: {statusCode}",
                "HTTP_ERROR",
                new List<string>
                {
                    "Check your internet connection",
                    "Verify the API key is correct",
                    $"Check {providerName} status page for outages"
                },
                GetProviderDocsUrl(providerName)
            )
        };
    }

    /// <summary>
    /// Gets the documentation URL for a provider
    /// </summary>
    private string GetProviderDocsUrl(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => "https://platform.openai.com/docs",
            "Anthropic" => "https://docs.anthropic.com/",
            "Gemini" => "https://ai.google.dev/docs",
            "ElevenLabs" => "https://docs.elevenlabs.io/",
            "PlayHT" => "https://docs.play.ht/",
            "Replicate" => "https://replicate.com/docs",
            _ => "https://github.com/Coffee285/aura-video-studio/blob/main/docs/"
        };
    }

    /// <summary>
    /// Masks an API key for logging purposes (shows only first 8 and last 4 characters)
    /// Also sanitizes input to prevent log injection
    /// </summary>
    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "[empty]";
        }

        // Remove any control characters or newlines for safety
        apiKey = System.Text.RegularExpressions.Regex.Replace(apiKey, @"[\r\n\t\x00-\x1F\x7F]", "");

        if (apiKey.Length <= 12)
        {
            return "***";
        }

        return $"{apiKey.Substring(0, 8)}...{apiKey.Substring(apiKey.Length - 4)}";
    }
}
