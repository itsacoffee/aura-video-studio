using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm.Validators;

/// <summary>
/// Validator for Ollama LLM provider
/// </summary>
public class OllamaLlmValidator : ProviderValidator
{
    private readonly ILogger<OllamaLlmValidator> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _configUrl;

    public override string ProviderName => "Ollama";

    public OllamaLlmValidator(
        ILogger<OllamaLlmValidator> logger,
        HttpClient httpClient,
        string configUrl = "http://127.0.0.1:11434")
    {
        _logger = logger;
        _httpClient = httpClient;
        _configUrl = configUrl;
    }

    public override async Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if Ollama is running by hitting the tags endpoint
            var response = await _httpClient.GetAsync($"{_configUrl}/api/tags", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new ProviderValidationResult
                {
                    IsAvailable = false,
                    ProviderName = ProviderName,
                    Details = $"Ollama returned status {response.StatusCode}",
                    ErrorMessage = $"Ollama not responding properly: {response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            
            // Parse the response to check if there are any models
            try
            {
                var json = JsonDocument.Parse(content);
                if (json.RootElement.TryGetProperty("models", out var models))
                {
                    if (models.GetArrayLength() == 0)
                    {
                        return new ProviderValidationResult
                        {
                            IsAvailable = false,
                            ProviderName = ProviderName,
                            Details = "No models installed",
                            ErrorMessage = "Ollama has no models installed"
                        };
                    }
                }
            }
            catch (JsonException)
            {
                // If parsing fails, assume Ollama is not properly configured
                return new ProviderValidationResult
                {
                    IsAvailable = false,
                    ProviderName = ProviderName,
                    Details = "Invalid response format",
                    ErrorMessage = "Ollama returned invalid response"
                };
            }

            return new ProviderValidationResult
            {
                IsAvailable = true,
                ProviderName = ProviderName,
                Details = "Ollama is available with models"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot connect to Ollama at {Url}", _configUrl);
            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Cannot connect to Ollama",
                ErrorMessage = $"Ollama not running at {_configUrl}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Ollama provider");
            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Validation failed",
                ErrorMessage = ex.Message
            };
        }
    }
}
