using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts.Validators;

/// <summary>
/// Validator for ElevenLabs TTS provider
/// </summary>
public class ElevenLabsTtsValidator : ProviderValidator
{
    private readonly ILogger<ElevenLabsTtsValidator> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public override string ProviderName => "ElevenLabs";

    public ElevenLabsTtsValidator(
        ILogger<ElevenLabsTtsValidator> logger,
        HttpClient httpClient,
        string? apiKey)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public override async Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "API key not configured",
                ErrorMessage = "ElevenLabs API key is required"
            };
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices");
            request.Headers.Add("xi-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new ProviderValidationResult
                {
                    IsAvailable = true,
                    ProviderName = ProviderName,
                    Details = "ElevenLabs TTS is available"
                };
            }

            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = $"API returned status {response.StatusCode}",
                ErrorMessage = $"Failed to connect to ElevenLabs API: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate ElevenLabs TTS provider");
            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Connection failed",
                ErrorMessage = ex.Message
            };
        }
    }
}
