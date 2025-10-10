using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images.Validators;

/// <summary>
/// Validator for Stable Diffusion image provider
/// </summary>
public class StableDiffusionImageValidator : ProviderValidator
{
    private readonly ILogger<StableDiffusionImageValidator> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _configUrl;

    public override string ProviderName => "StableDiffusion";

    public StableDiffusionImageValidator(
        ILogger<StableDiffusionImageValidator> logger,
        HttpClient httpClient,
        string configUrl = "http://127.0.0.1:7860")
    {
        _logger = logger;
        _httpClient = httpClient;
        _configUrl = configUrl;
    }

    public override async Task<ProviderValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if SD WebUI is running by hitting the models endpoint
            var response = await _httpClient.GetAsync($"{_configUrl}/sdapi/v1/sd-models", ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new ProviderValidationResult
                {
                    IsAvailable = false,
                    ProviderName = ProviderName,
                    Details = $"SD WebUI returned status {response.StatusCode}",
                    ErrorMessage = $"Stable Diffusion WebUI not responding properly: {response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            
            // Check if there are any models installed
            if (content == "[]" || string.IsNullOrWhiteSpace(content))
            {
                return new ProviderValidationResult
                {
                    IsAvailable = false,
                    ProviderName = ProviderName,
                    Details = "No models installed",
                    ErrorMessage = "Stable Diffusion WebUI has no models installed"
                };
            }

            return new ProviderValidationResult
            {
                IsAvailable = true,
                ProviderName = ProviderName,
                Details = "Stable Diffusion is available with models"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot connect to Stable Diffusion WebUI at {Url}", _configUrl);
            return new ProviderValidationResult
            {
                IsAvailable = false,
                ProviderName = ProviderName,
                Details = "Cannot connect to SD WebUI",
                ErrorMessage = $"Stable Diffusion WebUI not running at {_configUrl}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Stable Diffusion provider");
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
