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
                var logger = loggerFactory.CreateLogger("ProviderStatusEndpoints");

                // Get LLM provider status
                var llmProviders = new List<ProviderStatusDto>();
                var availableLlmProviders = llmFactory.CreateAvailableProviders(loggerFactory);

                // Check Ollama
                if (ollamaDetection != null)
                {
                    try
                    {
                        var ollamaStatus = await ollamaDetection.GetStatusAsync(ct).ConfigureAwait(false);
                        var models = ollamaStatus.IsRunning ? await ollamaDetection.GetModelsAsync(ct).ConfigureAwait(false) : new List<OllamaModel>();
                        var modelsCount = models?.Count ?? 0;
                        var isAvailable = ollamaStatus.IsRunning && modelsCount > 0;
                        var errorMessage = isAvailable ? null : (ollamaStatus.ErrorMessage ?? "Ollama service not running or no models installed");
                        var howToFix = isAvailable ? null : new List<string>
                        {
                            "1. Download and install Ollama from https://ollama.ai/download",
                            "2. Start the Ollama service (it should run in the background)",
                            "3. Pull at least one model: Open terminal and run 'ollama pull llama3.1'",
                            "4. Verify Ollama is running: Check system tray or run 'ollama list' in terminal",
                            "5. Return here and click 'Validate' to check again"
                        };
                        llmProviders.Add(new ProviderStatusDto(
                            Name: "Ollama",
                            Available: isAvailable,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: errorMessage,
                            Details: ollamaStatus.IsRunning ? $"Running with {modelsCount} models" : null,
                            HowToFix: howToFix
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
                            ErrorMessage: ex.Message,
                            HowToFix: new List<string>
                            {
                                "1. Download and install Ollama from https://ollama.ai/download",
                                "2. Start the Ollama service",
                                "3. Pull at least one model: 'ollama pull llama3.1'",
                                "4. Verify installation and try again"
                            }
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
                        ErrorMessage: "Ollama detection service not available",
                        HowToFix: new List<string>
                        {
                            "1. Ensure the backend service is running",
                            "2. Restart the application if the issue persists",
                            "3. Check application logs for more details"
                        }
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
                        var providerErrorMessage = isAvailable ? null : GetProviderErrorMessage(name);
                        var providerHowToFix = isAvailable ? null : GetProviderHowToFix(name);

                        llmProviders.Add(new ProviderStatusDto(
                            Name: name,
                            Available: isAvailable,
                            Tier: tier,
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: providerErrorMessage,
                            HowToFix: providerHowToFix
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
                            ErrorMessage: ex.Message,
                            HowToFix: GetProviderHowToFix(name)
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

                        var ttsErrorMessage = isAvailable ? null : GetTtsProviderErrorMessage(providerName);
                        var ttsHowToFix = isAvailable ? null : GetTtsProviderHowToFix(providerName);

                        ttsProviders.Add(new ProviderStatusDto(
                            Name: providerName,
                            Available: isAvailable,
                            Tier: tier,
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: ttsErrorMessage,
                            HowToFix: ttsHowToFix
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
                        var sdAvailable = sdStatus.IsRunning;
                        var sdErrorMessage = sdAvailable ? null : (sdStatus.ErrorMessage ?? "Stable Diffusion WebUI not running");
                        var sdHowToFix = sdAvailable ? null : new List<string>
                        {
                            "1. Install Stable Diffusion WebUI (see https://github.com/AUTOMATIC1111/stable-diffusion-webui)",
                            "2. Start the WebUI service (usually runs on http://127.0.0.1:7860)",
                            "3. Ensure the WebUI is accessible and running",
                            "4. Configure the WebUI URL in Settings if using a custom port"
                        };

                        imageProviders.Add(new ProviderStatusDto(
                            Name: "StableDiffusion",
                            Available: sdAvailable,
                            Tier: "local",
                            LastChecked: DateTime.UtcNow,
                            ErrorMessage: sdErrorMessage,
                            Details: sdAvailable ? "WebUI is running" : null,
                            HowToFix: sdHowToFix
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
                            ErrorMessage: ex.Message,
                            HowToFix: new List<string>
                            {
                                "1. Install Stable Diffusion WebUI",
                                "2. Start the WebUI service",
                                "3. Verify it's accessible and try again"
                            }
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

    private static string GetProviderErrorMessage(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => "OpenAI API key not configured or invalid",
            "Anthropic" => "Anthropic API key not configured or invalid",
            "Gemini" => "Google Gemini API key not configured or invalid",
            "Azure" => "Azure OpenAI API key not configured or invalid",
            "RuleBased" => "Rule-based provider is always available",
            _ => "Provider not configured or unavailable"
        };
    }

    private static List<string> GetProviderHowToFix(string providerName)
    {
        return providerName switch
        {
            "OpenAI" => new List<string>
            {
                "1. Create an account at https://platform.openai.com/signup",
                "2. Add a payment method to your account",
                "3. Go to API Keys section and create a new secret key",
                "4. Copy the key (starts with 'sk-' or 'sk-proj-')",
                "5. Paste it in the API key field above and click 'Validate'"
            },
            "Anthropic" => new List<string>
            {
                "1. Create an account at https://console.anthropic.com",
                "2. Add a payment method",
                "3. Navigate to API Keys section and create a new key",
                "4. Copy the key (starts with 'sk-ant-')",
                "5. Paste it in the API key field above and click 'Validate'"
            },
            "Gemini" => new List<string>
            {
                "1. Sign in with your Google account",
                "2. Navigate to https://makersuite.google.com/app/apikey",
                "3. Click 'Create API Key'",
                "4. Copy the generated key (39 characters)",
                "5. Paste it in the API key field above and click 'Validate'"
            },
            "Azure" => new List<string>
            {
                "1. Create an Azure account and set up Azure OpenAI service",
                "2. Get your API key from Azure Portal",
                "3. Configure the endpoint URL and API key",
                "4. Paste the key in the API key field above and click 'Validate'"
            },
            _ => new List<string>
            {
                "1. Check if the provider requires an API key",
                "2. Configure the API key in Settings → Providers",
                "3. Verify the key is valid and try again"
            }
        };
    }

    private static string GetTtsProviderErrorMessage(string providerName)
    {
        return providerName switch
        {
            "ElevenLabs" => "ElevenLabs API key not configured or invalid",
            "PlayHT" => "PlayHT API credentials not configured or invalid",
            "Piper" => "Piper TTS not installed or not available",
            "Mimic3" => "Mimic3 TTS service not running",
            "Windows" => "Windows TTS should be available on Windows systems",
            _ => "Provider not available or not configured"
        };
    }

    private static List<string> GetTtsProviderHowToFix(string providerName)
    {
        return providerName switch
        {
            "ElevenLabs" => new List<string>
            {
                "1. Create an account at https://elevenlabs.io/sign-up",
                "2. Subscribe to a plan (or use free tier)",
                "3. Go to Profile Settings → API Key section",
                "4. Copy your API key (32-character hex string)",
                "5. Paste it in the API key field above and click 'Validate'"
            },
            "PlayHT" => new List<string>
            {
                "1. Create an account at https://play.ht/signup",
                "2. Subscribe to a plan",
                "3. Navigate to Settings → API Credentials",
                "4. Copy both User ID and Secret Key",
                "5. Paste them in the API key fields above and click 'Validate'"
            },
            "Piper" => new List<string>
            {
                "1. Install Piper via Settings → Download Center → Engines",
                "2. Download at least one voice model",
                "3. Piper will be automatically detected when installed",
                "4. Click 'Mark as Ready' once installation is complete"
            },
            "Mimic3" => new List<string>
            {
                "1. Install Mimic3 server (see Download Center in Settings)",
                "2. Start the Mimic3 service (runs on port 59125)",
                "3. Leave the Mimic3 service running while Aura is open",
                "4. Click 'Mark as Ready' once the service is running"
            },
            _ => new List<string>
            {
                "1. Check if the provider requires installation or API key",
                "2. Configure the provider in Settings → Providers",
                "3. Verify the configuration and try again"
            }
        };
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
    string? Details = null,
    List<string>? HowToFix = null
);

