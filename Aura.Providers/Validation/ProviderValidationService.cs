using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Service for validating multiple providers
/// </summary>
public class ProviderValidationService
{
    private readonly ILogger<ProviderValidationService> _logger;
    private readonly IKeyStore _keyStore;
    private readonly ProviderSettings _providerSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, IProviderValidator> _validators;

    public ProviderValidationService(
        ILoggerFactory loggerFactory,
        IKeyStore keyStore,
        ProviderSettings providerSettings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = loggerFactory.CreateLogger<ProviderValidationService>();
        _keyStore = keyStore;
        _providerSettings = providerSettings;
        _httpClientFactory = httpClientFactory;

        // Initialize validators with a shared HTTP client
        // Each validator handles its own timeout via CancellationTokenSource
        var httpClient = _httpClientFactory.CreateClient();

        _validators = new Dictionary<string, IProviderValidator>
        {
            ["OpenAI"] = new OpenAiValidator(
                loggerFactory.CreateLogger<OpenAiValidator>(),
                httpClient),
            ["ElevenLabs"] = new ElevenLabsValidator(
                loggerFactory.CreateLogger<ElevenLabsValidator>(),
                httpClient),
            ["Ollama"] = new OllamaValidator(
                loggerFactory.CreateLogger<OllamaValidator>(),
                httpClient),
            ["StableDiffusion"] = new StableDiffusionValidator(
                loggerFactory.CreateLogger<StableDiffusionValidator>(),
                httpClient)
        };
    }

    /// <summary>
    /// Validate specified providers (or all if providerNames is null/empty)
    /// </summary>
    public async Task<ValidationResponse> ValidateProvidersAsync(
        string[]? providerNames,
        CancellationToken ct = default)
    {
        var isOfflineOnly = _keyStore.IsOfflineOnly();
        
        // Determine which providers to validate
        var providersToValidate = providerNames?.Length > 0
            ? providerNames
            : _validators.Keys.ToArray();

        _logger.LogInformation("Validating {Count} providers (OfflineOnly: {OfflineOnly})",
            providersToValidate.Length, isOfflineOnly);

        var results = new List<ProviderValidationResult>();

        foreach (var providerName in providersToValidate)
        {
            if (!_validators.TryGetValue(providerName, out var validator))
            {
                _logger.LogWarning("Unknown provider: {Provider}", providerName);
                results.Add(new ProviderValidationResult
                {
                    Name = providerName,
                    Ok = false,
                    Details = "Unknown provider",
                    ElapsedMs = 0
                });
                continue;
            }

            // Check if this is a cloud provider and if offline mode is enabled
            var isCloudProvider = IsCloudProvider(providerName);
            
            if (isOfflineOnly && isCloudProvider)
            {
                _logger.LogInformation("Skipping cloud provider {Provider} in offline mode", providerName);
                results.Add(new ProviderValidationResult
                {
                    Name = providerName,
                    Ok = false,
                    Details = "Offline mode enabled (E307)",
                    ElapsedMs = 0
                });
                continue;
            }

            try
            {
                // Get API key and config URL for the provider
                var apiKey = _keyStore.GetKey(providerName.ToLower());
                var configUrl = GetConfigUrl(providerName);

                var result = await validator.ValidateAsync(apiKey, configUrl, ct);
                results.Add(result);

                _logger.LogInformation("Provider {Provider} validation completed: {Ok}",
                    providerName, result.Ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate provider {Provider}", providerName);
                results.Add(new ProviderValidationResult
                {
                    Name = providerName,
                    Ok = false,
                    Details = $"Validation error: {ex.Message}",
                    ElapsedMs = 0
                });
            }
        }

        var allOk = results.All(r => r.Ok);
        
        return new ValidationResponse
        {
            Results = results.ToArray(),
            Ok = allOk
        };
    }

    private bool IsCloudProvider(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => true,
            "ElevenLabs" => true,
            "Azure" => true,
            "Gemini" => true,
            "PlayHT" => true,
            _ => false
        };
    }

    private string? GetConfigUrl(string providerName)
    {
        return providerName switch
        {
            "Ollama" => _providerSettings.GetOllamaUrl(),
            "StableDiffusion" => _providerSettings.GetStableDiffusionUrl(),
            _ => null
        };
    }
}
