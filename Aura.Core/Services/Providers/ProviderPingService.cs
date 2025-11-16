using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Provider ping execution result with normalized diagnostics.
/// </summary>
public record CoreProviderPingResult
{
    public string ProviderId { get; init; } = string.Empty;
    public bool Attempted { get; init; }
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public int? StatusCode { get; init; }
    public string? Endpoint { get; init; }
    public long? LatencyMs { get; init; }
}

/// <summary>
/// Performs real provider connectivity checks used by both the API and desktop shell.
/// </summary>
public class ProviderPingService
{
    private const string HttpClientName = nameof(ProviderPingService);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(12);

    private readonly ILogger<ProviderPingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeyStore _keyStore;

    private static readonly IReadOnlyDictionary<string, string> ProviderKeyStoreNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = "OpenAI",
            ["anthropic"] = "Anthropic",
            ["gemini"] = "Gemini",
            ["azureopenai"] = "AzureOpenAI",
            ["elevenlabs"] = "ElevenLabs",
            ["playht"] = "PlayHT",
            ["stabilityai"] = "StabilityAI",
            ["pexels"] = "Pexels",
            ["pixabay"] = "Pixabay",
            ["unsplash"] = "Unsplash"
        };

    private static readonly IReadOnlyDictionary<string, string> ProviderDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai"] = "OpenAI",
            ["anthropic"] = "Anthropic",
            ["gemini"] = "Google Gemini",
            ["azureopenai"] = "Azure OpenAI",
            ["elevenlabs"] = "ElevenLabs",
            ["playht"] = "PlayHT",
            ["stabilityai"] = "Stability AI",
            ["pexels"] = "Pexels",
            ["pixabay"] = "Pixabay",
            ["unsplash"] = "Unsplash",
            ["stablediffusion"] = "Stable Diffusion",
            ["ollama"] = "Ollama"
        };

    /// <summary>
    /// List of provider ids exposed to the API.
    /// </summary>
    public static IReadOnlyList<string> SupportedProviders { get; } = new[]
    {
        "openai",
        "anthropic",
        "gemini",
        "azureopenai",
        "elevenlabs",
        "playht",
        "stabilityai",
        "pexels",
        "pixabay",
        "unsplash",
        "stablediffusion",
        "ollama"
    };

    public ProviderPingService(
        ILogger<ProviderPingService> logger,
        IHttpClientFactory httpClientFactory,
        IKeyStore keyStore)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _keyStore = keyStore;
    }

    public Task<CoreProviderPingResult> PingAsync(
        string provider,
        ProviderPingRequest? request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider name is required", nameof(provider));
        }

        var normalized = NormalizeProviderId(provider);

        return normalized switch
        {
            "openai" => PingOpenAiAsync(request, ct),
            "anthropic" => PingAnthropicAsync(request, ct),
            "gemini" => PingGeminiAsync(request, ct),
            "azureopenai" => PingAzureOpenAiAsync(request, ct),
            "elevenlabs" => PingElevenLabsAsync(request, ct),
            "playht" => PingPlayHtAsync(request, ct),
            "stabilityai" => PingStabilityAiAsync(request, ct),
            "pexels" => PingPexelsAsync(ct),
            "pixabay" => PingPixabayAsync(ct),
            "unsplash" => PingUnsplashAsync(ct),
            "stablediffusion" => PingStableDiffusionAsync(request, ct),
            "ollama" => PingOllamaAsync(request, ct),
            _ => Task.FromResult(CreateUnsupportedResult(normalized))
        };
    }

    private async Task<CoreProviderPingResult> PingOpenAiAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("openai", out var apiKey, out var failure))
        {
            return failure!;
        }

        var endpoint = $"{(request?.Endpoint ?? "https://api.openai.com").TrimEnd('/')}/v1/models";

        return await SendAsync(
            "openai",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                return message;
            },
            ct,
            "OpenAI responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingAnthropicAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("anthropic", out var apiKey, out var failure))
        {
            return failure!;
        }

        var endpoint = $"{(request?.Endpoint ?? "https://api.anthropic.com").TrimEnd('/')}/v1/messages";
        var model = string.IsNullOrWhiteSpace(request?.Model) ? "claude-3-haiku-20240307" : request!.Model!;

        return await SendAsync(
            "anthropic",
            endpoint,
            () =>
            {
                var payload = new
                {
                    model,
                    max_tokens = 1,
                    messages = new[] { new { role = "user", content = "ping" } }
                };

                var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
                message.Headers.Add("x-api-key", apiKey);
                message.Headers.Add("anthropic-version", "2023-06-01");
                message.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");
                return message;
            },
            ct,
            "Anthropic responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingGeminiAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("gemini", out var apiKey, out var failure))
        {
            return failure!;
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";

        return await SendAsync(
            "gemini",
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            ct,
            "Gemini responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingAzureOpenAiAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("azureopenai", out var apiKey, out var failure))
        {
            return failure!;
        }

        var baseEndpoint = request?.Endpoint ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        if (string.IsNullOrWhiteSpace(baseEndpoint))
        {
            return CreateConfigMissingResult("azureopenai", "Azure OpenAI endpoint is not configured. Set AZURE_OPENAI_ENDPOINT or pass an endpoint override.");
        }

        var apiVersion = request?.Parameters != null &&
                         request.Parameters.TryGetValue("apiVersion", out var value) &&
                         !string.IsNullOrWhiteSpace(value)
            ? value!
            : "2023-05-15";

        var endpoint = $"{baseEndpoint.TrimEnd('/')}/openai/deployments?api-version={apiVersion}";

        return await SendAsync(
            "azureopenai",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Add("api-key", apiKey);
                return message;
            },
            ct,
            "Azure OpenAI endpoint responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingElevenLabsAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("elevenlabs", out var apiKey, out var failure))
        {
            return failure!;
        }

        const string endpoint = "https://api.elevenlabs.io/v1/voices";

        return await SendAsync(
            "elevenlabs",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Add("xi-api-key", apiKey);
                return message;
            },
            ct,
            "ElevenLabs responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingPlayHtAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("playht", out var apiKey, out var failure))
        {
            return failure!;
        }

        var userId = request?.Parameters != null &&
                     request.Parameters.TryGetValue("userId", out var fromRequest) &&
                     !string.IsNullOrWhiteSpace(fromRequest)
            ? fromRequest
            : Environment.GetEnvironmentVariable("PLAYHT_USER_ID");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return CreateConfigMissingResult("playht", "PlayHT user ID is not configured. Set PLAYHT_USER_ID or pass parameters.userId.");
        }

        const string endpoint = "https://api.play.ht/api/v2/voices";

        return await SendAsync(
            "playht",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                message.Headers.Add("X-User-ID", userId);
                return message;
            },
            ct,
            "PlayHT responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingStabilityAiAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        if (!TryGetApiKey("stabilityai", out var apiKey, out var failure))
        {
            return failure!;
        }

        const string endpoint = "https://api.stability.ai/v1/user/account";

        return await SendAsync(
            "stabilityai",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                return message;
            },
            ct,
            "Stability AI responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingPexelsAsync(CancellationToken ct)
    {
        if (!TryGetApiKey("pexels", out var apiKey, out var failure))
        {
            return failure!;
        }

        const string endpoint = "https://api.pexels.com/v1/search?query=nature&per_page=1";

        return await SendAsync(
            "pexels",
            endpoint,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
                message.Headers.Add("Authorization", apiKey);
                return message;
            },
            ct,
            "Pexels responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingPixabayAsync(CancellationToken ct)
    {
        if (!TryGetApiKey("pixabay", out var apiKey, out var failure))
        {
            return failure!;
        }

        var endpoint = $"https://pixabay.com/api/?key={apiKey}&q=landscape&per_page=3";

        return await SendAsync(
            "pixabay",
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            ct,
            "Pixabay responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingUnsplashAsync(CancellationToken ct)
    {
        if (!TryGetApiKey("unsplash", out var apiKey, out var failure))
        {
            return failure!;
        }

        var endpoint = $"https://api.unsplash.com/photos/random?query=nature&client_id={apiKey}";

        return await SendAsync(
            "unsplash",
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            ct,
            "Unsplash responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingStableDiffusionAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        var endpointBase = request?.Endpoint ?? Environment.GetEnvironmentVariable("STABLE_DIFFUSION_URL") ?? "http://127.0.0.1:7860";
        var endpoint = $"{endpointBase.TrimEnd('/')}/sdapi/v1/sd-models";

        return await SendAsync(
            "stablediffusion",
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            ct,
            "Stable Diffusion WebUI responded successfully.").ConfigureAwait(false);
    }

    private async Task<CoreProviderPingResult> PingOllamaAsync(ProviderPingRequest? request, CancellationToken ct)
    {
        var endpointBase = request?.Endpoint ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://127.0.0.1:11434";
        var endpoint = $"{endpointBase.TrimEnd('/')}/api/tags";

        return await SendAsync(
            "ollama",
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            ct,
            "Ollama responded successfully.").ConfigureAwait(false);
    }

    private bool TryGetApiKey(string providerId, out string? apiKey, out CoreProviderPingResult? failureResult)
    {
        apiKey = null;
        failureResult = null;

        if (!ProviderKeyStoreNames.TryGetValue(providerId, out var keyName))
        {
            return true;
        }

        apiKey = _keyStore.GetKey(keyName);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return true;
        }

        failureResult = new CoreProviderPingResult
        {
            ProviderId = providerId,
            Attempted = false,
            Success = false,
            ErrorCode = ProviderPingErrorCodes.MissingApiKey,
            Message = $"{GetDisplayName(providerId)} is not configured. Add your API key in Settings → Providers.",
            LatencyMs = null
        };
        return false;
    }

    private async Task<CoreProviderPingResult> SendAsync(
        string providerId,
        string endpoint,
        Func<HttpRequestMessage> requestFactory,
        CancellationToken ct,
        string successMessage)
    {
        var displayName = GetDisplayName(providerId);
        var sanitizedEndpoint = SecretMaskingService.SanitizeForLogging(endpoint);
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.Timeout = DefaultTimeout;

        _logger.LogInformation(
            "[ProviderPing] Connecting to {Provider} at {Endpoint}",
            displayName,
            sanitizedEndpoint);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = requestFactory();
            if (request.RequestUri == null)
            {
                request.RequestUri = new Uri(endpoint);
            }
            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[ProviderPing] {Provider} responded in {Elapsed} ms with {Status}",
                    displayName,
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode);

                return new CoreProviderPingResult
                {
                    ProviderId = providerId,
                    Attempted = true,
                    Success = true,
                    ErrorCode = ProviderPingErrorCodes.Success,
                    Message = successMessage,
                    StatusCode = (int)response.StatusCode,
                    Endpoint = endpoint,
                    LatencyMs = stopwatch.ElapsedMilliseconds
                };
            }

            var errorCode = MapStatusToErrorCode(response.StatusCode);
            var errorMessage = BuildErrorMessage(displayName, response.StatusCode);

            _logger.LogWarning(
                "[ProviderPing] {Provider} returned HTTP {Status} ({ErrorCode}) after {Elapsed} ms",
                displayName,
                (int)response.StatusCode,
                errorCode,
                stopwatch.ElapsedMilliseconds);

            return new CoreProviderPingResult
            {
                ProviderId = providerId,
                Attempted = true,
                Success = false,
                ErrorCode = errorCode,
                Message = errorMessage,
                StatusCode = (int)response.StatusCode,
                Endpoint = endpoint,
                LatencyMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[ProviderPing] {Provider} timed out after {Elapsed} ms",
                displayName,
                stopwatch.ElapsedMilliseconds);

            return new CoreProviderPingResult
            {
                ProviderId = providerId,
                Attempted = true,
                Success = false,
                ErrorCode = ProviderPingErrorCodes.Timeout,
                Message = $"{displayName} did not respond before the {DefaultTimeout.TotalSeconds}s timeout.",
                Endpoint = endpoint,
                LatencyMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "[ProviderPing] Network error contacting {Provider} after {Elapsed} ms",
                displayName,
                stopwatch.ElapsedMilliseconds);

            return new CoreProviderPingResult
            {
                ProviderId = providerId,
                Attempted = true,
                Success = false,
                ErrorCode = ProviderPingErrorCodes.NetworkError,
                Message = $"Network error contacting {displayName}: {ex.Message}",
                Endpoint = endpoint,
                LatencyMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private static string MapStatusToErrorCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ProviderPingErrorCodes.InvalidApiKey,
            (HttpStatusCode)429 => ProviderPingErrorCodes.RateLimited,
            HttpStatusCode.BadRequest => ProviderPingErrorCodes.BadRequest,
            _ when (int)statusCode >= 500 => ProviderPingErrorCodes.ProviderUnavailable,
            _ => ProviderPingErrorCodes.Unknown
        };
    }

    private static string BuildErrorMessage(string providerDisplayName, HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                $"Authentication failed. Verify the {providerDisplayName} API key and permissions.",
            (HttpStatusCode)429 =>
                $"{providerDisplayName} rate limit reached. Wait a moment and try again.",
            HttpStatusCode.BadRequest =>
                $"{providerDisplayName} rejected the request. Double-check configuration values.",
            _ when (int)statusCode >= 500 =>
                $"{providerDisplayName} reported a server-side error ({(int)statusCode}).",
            _ => $"{providerDisplayName} returned HTTP {(int)statusCode}."
        };
    }

    private static string NormalizeProviderId(string provider)
    {
        var trimmed = provider.Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        var lower = trimmed.ToLowerInvariant();
        return lower switch
        {
            "google" => "gemini",
            "stability" => "stabilityai",
            "azure" => "azureopenai",
            "stable-diffusion" or "stable_diffusion" => "stablediffusion",
            _ => lower
        };
    }

    private static string GetDisplayName(string providerId)
    {
        return ProviderDisplayNames.TryGetValue(providerId, out var display)
            ? display
            : providerId;
    }

    private static CoreProviderPingResult CreateUnsupportedResult(string providerId)
    {
        return new CoreProviderPingResult
        {
            ProviderId = providerId,
            Attempted = false,
            Success = false,
            ErrorCode = ProviderPingErrorCodes.UnsupportedProvider,
            Message = $"Provider '{providerId}' does not support connectivity checks yet."
        };
    }

    private static CoreProviderPingResult CreateConfigMissingResult(string providerId, string message)
    {
        return new CoreProviderPingResult
        {
            ProviderId = providerId,
            Attempted = false,
            Success = false,
            ErrorCode = ProviderPingErrorCodes.ConfigurationMissing,
            Message = message
        };
    }
}
