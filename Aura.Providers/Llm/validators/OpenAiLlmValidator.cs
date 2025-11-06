using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm.Validators;

/// <summary>
/// Validator for OpenAI LLM provider
/// </summary>
public class OpenAiLlmValidator : ProviderValidator
{
    private readonly ILogger<OpenAiLlmValidator> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public override string ProviderName => "OpenAI";

    public OpenAiLlmValidator(
        ILogger<OpenAiLlmValidator> logger,
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
                ErrorMessage = "OpenAI API key is required"
            };
        }

        try
        {
            // Simple validation: test models endpoint
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new ProviderValidationResult
                {
                    IsAvailable = true,
                    ProviderName = ProviderName,
                    Details = "OpenAI API is available"
                };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ProviderValidationResult
                {
                    IsAvailable = false,
                    ProviderName = ProviderName,
                    Details = "Invalid API key",
                    ErrorMessage = "OpenAI API key is invalid or expired"
                };
            }

            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = $"API returned status {response.StatusCode}",
                ErrorMessage = $"Failed to connect to OpenAI API: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate OpenAI LLM provider");
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
