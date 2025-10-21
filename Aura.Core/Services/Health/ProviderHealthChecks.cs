using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Health;

/// <summary>
/// Static health check functions for each provider type
/// </summary>
public static class ProviderHealthChecks
{
    /// <summary>
    /// Health check for LLM providers
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateLlmHealthCheck(
        ILlmProvider provider,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                var brief = new Brief(
                    Topic: "test",
                    Audience: null,
                    Goal: null,
                    Tone: "professional",
                    Language: "en",
                    Aspect: Aspect.Widescreen16x9
                );

                var spec = new PlanSpec(
                    TargetDuration: TimeSpan.FromSeconds(10),
                    Pacing: Pacing.Conversational,
                    Density: Density.Balanced,
                    Style: "professional"
                );

                var script = await provider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
                return !string.IsNullOrWhiteSpace(script) && script.Length > 10;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "LLM health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for TTS providers
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateTtsHealthCheck(
        ITtsProvider provider,
        ILogger logger,
        string outputDirectory)
    {
        return async (ct) =>
        {
            try
            {
                var lines = new[]
                {
                    new ScriptLine(1, "test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
                };

                var spec = new VoiceSpec(
                    VoiceName: "default",
                    Rate: 1.0,
                    Pitch: 1.0,
                    Pause: PauseStyle.Natural
                );

                var audioPath = await provider.SynthesizeAsync(lines, spec, ct).ConfigureAwait(false);
                
                if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
                    return false;

                var fileInfo = new FileInfo(audioPath);
                var isValid = fileInfo.Length > 1024; // At least 1KB

                // Clean up test file
                try { File.Delete(audioPath); } catch { }

                return isValid;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "TTS health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for Ollama provider
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateOllamaHealthCheck(
        HttpClient httpClient,
        string ollamaUrl,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                var response = await httpClient.GetAsync($"{ollamaUrl}/api/tags", ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Ollama health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for OpenAI provider
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateOpenAiHealthCheck(
        HttpClient httpClient,
        string apiKey,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "OpenAI health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for Stable Diffusion provider
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateStableDiffusionHealthCheck(
        HttpClient httpClient,
        string sdUrl,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                var response = await httpClient.GetAsync($"{sdUrl}/sdapi/v1/sd-models", ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Stable Diffusion health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for Azure OpenAI provider
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateAzureOpenAiHealthCheck(
        HttpClient httpClient,
        string endpoint,
        string apiKey,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/openai/deployments?api-version=2023-05-15");
                request.Headers.Add("api-key", apiKey);

                var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Azure OpenAI health check failed");
                return false;
            }
        };
    }

    /// <summary>
    /// Health check for Gemini provider
    /// </summary>
    public static Func<CancellationToken, Task<bool>> CreateGeminiHealthCheck(
        HttpClient httpClient,
        string apiKey,
        ILogger logger)
    {
        return async (ct) =>
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
                var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Gemini health check failed");
                return false;
            }
        };
    }
}
