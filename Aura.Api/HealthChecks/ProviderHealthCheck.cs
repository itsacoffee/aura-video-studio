using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aura.Api.HealthChecks;

/// <summary>
/// Health check that validates provider configuration and availability
/// Checks that at least one provider of each type (LLM, TTS, Video) is properly configured
/// </summary>
public class ProviderHealthCheck : IHealthCheck
{
    private readonly ILogger<ProviderHealthCheck> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly IEnumerable<ILlmProvider> _llmProviders;
    private readonly IEnumerable<ITtsProvider> _ttsProviders;
    private readonly IVideoComposer _videoComposer;

    public ProviderHealthCheck(
        ILogger<ProviderHealthCheck> logger,
        ProviderSettings providerSettings,
        IEnumerable<ILlmProvider> llmProviders,
        IEnumerable<ITtsProvider> ttsProviders,
        IVideoComposer videoComposer)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        _llmProviders = llmProviders;
        _ttsProviders = ttsProviders;
        _videoComposer = videoComposer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        try
        {
            var data = new Dictionary<string, object>();
            var warnings = new List<string>();

            var llmProvidersFiltered = _llmProviders.Where(p => p != null).ToList();
            var ttsProvidersFiltered = _ttsProviders.Where(p => p != null).ToList();

            data["llm_providers_available"] = llmProvidersFiltered.Count;
            data["tts_providers_available"] = ttsProvidersFiltered.Count;
            data["video_composer_available"] = _videoComposer != null;

            if (llmProvidersFiltered.Count > 0)
            {
                data["llm_providers"] = llmProvidersFiltered
                    .Select(p => p.GetType().Name.Replace("Provider", ""))
                    .ToArray();
            }

            if (ttsProvidersFiltered.Count > 0)
            {
                data["tts_providers"] = ttsProvidersFiltered
                    .Select(p => p.GetType().Name.Replace("Provider", ""))
                    .ToArray();
            }

            CheckApiKeys(data, warnings);

            if (llmProvidersFiltered.Count == 0)
            {
                warnings.Add("No LLM providers configured - script generation will not work");
            }

            if (ttsProvidersFiltered.Count == 0)
            {
                warnings.Add("No TTS providers configured - audio generation will not work");
            }

            if (_videoComposer == null)
            {
                return HealthCheckResult.Unhealthy(
                    "Video composer not available - critical for rendering",
                    data: data);
            }

            if (warnings.Count > 0)
            {
                data["warnings"] = warnings.ToArray();
                return HealthCheckResult.Degraded(
                    $"Provider configuration has {warnings.Count} warning(s)",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "All provider types are properly configured",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking provider health");
            return HealthCheckResult.Unhealthy(
                "Error checking provider configuration",
                exception: ex);
        }
    }

    private void CheckApiKeys(Dictionary<string, object> data, List<string> warnings)
    {
        var apiKeyStatus = new Dictionary<string, bool>();

        apiKeyStatus["openai"] = !string.IsNullOrWhiteSpace(_providerSettings.GetOpenAiApiKey());
        apiKeyStatus["azure_openai"] = !string.IsNullOrWhiteSpace(_providerSettings.GetAzureOpenAiApiKey());
        apiKeyStatus["gemini"] = !string.IsNullOrWhiteSpace(_providerSettings.GetGeminiApiKey());
        apiKeyStatus["elevenlabs"] = !string.IsNullOrWhiteSpace(_providerSettings.GetElevenLabsApiKey());
        apiKeyStatus["playht"] = !string.IsNullOrWhiteSpace(_providerSettings.GetPlayHTApiKey());
        apiKeyStatus["azure_speech"] = !string.IsNullOrWhiteSpace(_providerSettings.GetAzureSpeechKey());

        data["api_keys_configured"] = apiKeyStatus;

        var configuredCount = apiKeyStatus.Count(kv => kv.Value);
        data["api_keys_count"] = configuredCount;

        if (configuredCount == 0)
        {
            warnings.Add("No API keys configured - only offline providers will work");
        }
    }
}
