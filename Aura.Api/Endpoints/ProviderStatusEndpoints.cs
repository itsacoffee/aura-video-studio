using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Aura.Api.Endpoints;

/// <summary>
/// Provider status endpoints for real-time provider availability monitoring
/// </summary>
public static class ProviderStatusEndpoints
{
    /// <summary>
    /// Maps provider status endpoints to the API route group
    /// </summary>
    public static IEndpointRouteBuilder MapProviderStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        // Get comprehensive provider status for all provider types
        group.MapGet("/providers/status", async (
            OllamaDetectionService? ollamaDetection,
            StableDiffusionDetectionService? sdDetection,
            IServiceProvider serviceProvider,
            LlmProviderFactory llmFactory,
            CancellationToken ct) =>
        {
            try
            {
                var loggerFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<ProviderStatusEndpoints>();

                // Get LLM provider status
                var llmProviders = new List<ProviderStatusDto>();
                var availableLlmProviders = llmFactory.CreateAvailableProviders(loggerFactory);

                // Check Ollama
                if (ollamaDetection != null)
                {
                    try
                    {
                        var ollamaStatus = await ollamaDetection.GetStatusAsync(ct).ConfigureAwait(false);
                        var isAvailable = ollamaStatus.IsRunning && (ollamaStatus.Models?.Count ?? 0) > 0;
                        llmProviders.Add(new ProviderStatusDto(
                            Name: "Ollama",
                            Available: isAvailable,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: isAvailable ? null : (ollamaStatus.ErrorMessage ?? "Ollama service not running or no models installed"),
                            Details: ollamaStatus.IsRunning ? $"Running with {ollamaStatus.Models?.Count ?? 0} models" : null
                        ));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error checking Ollama status");
                        llmProviders.Add(new ProviderStatusDto(
                            Name: "Ollama",
                            Available: false,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: ex.Message
                        ));
                    }
                }
                else
                {
                    llmProviders.Add(new ProviderStatusDto(
                        Name: "Ollama",
                        Available: false,
                        Tier: "local",
                        LastChecked: DateTime.UtcNow,
                        ErrorMessage: "Ollama detection service not available"
                    ));
                }

                // Check other LLM providers (OpenAI, Anthropic, etc.)
                foreach (var (name, provider) in availableLlmProviders)
                {
                    if (name == "Ollama") continue; // Already handled above

                    try
                    {
                        var tier = GetLlmProviderTier(name);
                        var isAvailable = name == "RuleBased" || await CheckLlmProviderAvailabilityAsync(provider, name, ct).ConfigureAwait(false);
                        
                        llmProviders.Add(new ProviderStatusDto(
                            Name: name,
                            Available: isAvailable,
                            Tier: tier,
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: isAvailable ? null : "Provider not configured or unavailable"
                        ));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error checking LLM provider {Provider}", name);
                        llmProviders.Add(new ProviderStatusDto(
                            Name: name,
                            Available: false,
                            Tier: GetLlmProviderTier(name),
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: ex.Message
                        ));
                    }
                }

                // Get TTS provider status
                var ttsProviders = new List<ProviderStatusDto>();
                var ttsProviderServices = serviceProvider.GetServices<ITtsProvider>().Where(p => p != null).ToList();

                foreach (var ttsProvider in ttsProviderServices)
                {
                    try
                    {
                        var providerName = ttsProvider.GetType().Name.Replace("Provider", "").Replace("Tts", "");
                        var tier = GetTtsProviderTier(providerName);
                        
                        // Check if provider is healthy/available
                        var isAvailable = await CheckTtsProviderAvailabilityAsync(ttsProvider, providerName, ct).ConfigureAwait(false);
                        
                        ttsProviders.Add(new ProviderStatusDto(
                            Name: providerName,
                            Available: isAvailable,
                            Tier: tier,
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: isAvailable ? null : "Provider not available or not configured"
                        ));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error checking TTS provider status");
                    }
                }

                // Get image provider status
                var imageProviders = new List<ProviderStatusDto>();

                // Check Stable Diffusion
                if (sdDetection != null)
                {
                    try
                    {
                        var sdStatus = await sdDetection.DetectStableDiffusionAsync(ct).ConfigureAwait(false);
                        imageProviders.Add(new ProviderStatusDto(
                            Name: "StableDiffusion",
                            Available: sdStatus.IsRunning,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: sdStatus.IsRunning ? null : (sdStatus.ErrorMessage ?? "Stable Diffusion WebUI not running"),
                            Details: sdStatus.IsRunning ? "WebUI is running" : null
                        ));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error checking Stable Diffusion status");
                        imageProviders.Add(new ProviderStatusDto(
                            Name: "StableDiffusion",
                            Available: false,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: ex.Message
                        ));
                    }
                }

                // Add stock image providers (always available)
                imageProviders.Add(new ProviderStatusDto(
                    Name: "Pexels",
                    Available: true,
                    Tier: "free",
                    LastChecked: DateTime.UtcNow
                ));
                imageProviders.Add(new ProviderStatusDto(
                    Name: "Pixabay",
                    Available: true,
                    Tier: "free",
                    LastChecked: DateTime.UtcNow
                ));
                imageProviders.Add(new ProviderStatusDto(
                    Name: "Unsplash",
                    Available: true,
                    Tier: "free",
                    LastChecked: DateTime.UtcNow
                ));

                return Results.Ok(new
                {
                    llm = llmProviders,
                    tts = ttsProviders,
                    images = imageProviders,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving provider status");
                return Results.Problem("Error retrieving provider status", statusCode: 500);
            }
        })
        .WithName("GetProviderStatus")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get comprehensive provider status";
            operation.Description = "Returns real-time status for all LLM, TTS, and image providers including availability, tier, and error messages.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        return endpoints;
    }

    private static string GetLlmProviderTier(string providerName)
    {
        return providerName switch
        {
            "Ollama" => "local",
            "RuleBased" => "free",
            "OpenAI" or "Anthropic" or "Gemini" or "Azure" => "paid",
            _ => "unknown"
        };
    }

    private static string GetTtsProviderTier(string providerName)
    {
        return providerName switch
        {
            "Piper" or "Mimic3" or "Windows" => "local",
            "EdgeTTS" => "free",
            "ElevenLabs" or "PlayHT" or "Azure" or "OpenAI" => "paid",
            _ => "unknown"
        };
    }

    private static async Task<bool> CheckLlmProviderAvailabilityAsync(ILlmProvider provider, string providerName, CancellationToken ct)
    {
        // RuleBased is always available
        if (providerName == "RuleBased")
        {
            return true;
        }

        // For Ollama, check IsServiceAvailableAsync if available
        if (providerName == "Ollama")
        {
            try
            {
                var providerType = provider.GetType();
                var availabilityMethod = providerType.GetMethod("IsServiceAvailableAsync", 
                    new[] { typeof(CancellationToken), typeof(bool) });

                if (availabilityMethod != null)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(2));

                    var task = (Task<bool>)availabilityMethod.Invoke(provider, new object[] { cts.Token, false })!;
                    return await task.ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // For API key providers, assume available if registered (they'll fail gracefully if keys are invalid)
        return true;
    }

    private static async Task<bool> CheckTtsProviderAvailabilityAsync(ITtsProvider provider, string providerName, CancellationToken ct)
    {
        try
        {
            // Check if provider has IsHealthyAsync method (like PiperTtsProvider)
            var providerType = provider.GetType();
            var healthMethod = providerType.GetMethod("IsHealthyAsync", new[] { typeof(CancellationToken) });

            if (healthMethod != null)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(2));

                var task = (Task<bool>)healthMethod.Invoke(provider, new object[] { cts.Token })!;
                return await task.ConfigureAwait(false);
            }

            // Fallback: try to get available voices
            var voices = await provider.GetAvailableVoicesAsync().ConfigureAwait(false);
            return voices != null && voices.Count > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// DTO for provider status information
/// </summary>
public record ProviderStatusDto(
    string Name,
    bool Available,
    string Tier,
    DateTime LastChecked,
    string? ErrorMessage = null,
    string? Details = null
);

