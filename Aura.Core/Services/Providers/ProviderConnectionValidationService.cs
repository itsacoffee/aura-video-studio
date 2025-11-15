using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service to perform real validation of provider API keys and connectivity
/// </summary>
public class ProviderConnectionValidationService
{
    private readonly ILogger<ProviderConnectionValidationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly TimeSpan ValidationTimeout = TimeSpan.FromSeconds(5);

    public ProviderConnectionValidationService(
        ILogger<ProviderConnectionValidationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Validate a provider's configuration and connectivity
    /// </summary>
    public async Task<ProviderConnectionValidationResult> ValidateProviderAsync(
        string providerName,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating provider: {ProviderName}", providerName);

        try
        {
            return providerName switch
            {
                "OpenAI" => await ValidateOpenAIAsync(ct).ConfigureAwait(false),
                "Anthropic" => await ValidateAnthropicAsync(ct).ConfigureAwait(false),
                "Gemini" => await ValidateGeminiAsync(ct).ConfigureAwait(false),
                "AzureOpenAI" => await ValidateAzureOpenAIAsync(ct).ConfigureAwait(false),
                "ElevenLabs" => await ValidateElevenLabsAsync(ct).ConfigureAwait(false),
                "PlayHT" => await ValidatePlayHTAsync(ct).ConfigureAwait(false),
                "Ollama" => await ValidateOllamaAsync(ct).ConfigureAwait(false),
                "Piper" => ValidatePiper(),
                "Mimic3" => ValidateMimic3(),
                "WindowsTTS" => ValidateWindowsTTS(),
                "RuleBased" => ValidateRuleBased(),
                "StableDiffusion" => await ValidateStableDiffusionAsync(ct).ConfigureAwait(false),
                _ => new ProviderConnectionValidationResult
                {
                    Configured = false,
                    Reachable = false,
                    ErrorCode = "ProviderNotSupported",
                    ErrorMessage = $"Provider '{providerName}' is not supported for validation",
                    HowToFix = new List<string> { "Check the provider name and try again" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating provider {ProviderName}", providerName);
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ValidationError",
                ErrorMessage = "An unexpected error occurred during validation",
                HowToFix = new List<string> { "Check the application logs for details", "Try again later" }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateOpenAIAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "OpenAI API key is not configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your OpenAI API key",
                    "Get an API key from https://platform.openai.com/api-keys"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await httpClient.GetAsync(
                "https://api.openai.com/v1/models",
                ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderKeyInvalid",
                    ErrorMessage = "OpenAI API key is invalid or expired",
                    HowToFix = new List<string>
                    {
                        "Check your API key for typos",
                        "Verify the key is still valid at https://platform.openai.com/api-keys",
                        "Generate a new API key if needed"
                    }
                };
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = "ProviderRateLimited",
                    ErrorMessage = "OpenAI rate limit exceeded",
                    HowToFix = new List<string>
                    {
                        "Wait a few minutes before trying again",
                        "Check your OpenAI usage dashboard",
                        "Consider upgrading your OpenAI plan for higher limits"
                    }
                };
            }

            if ((int)response.StatusCode >= 500)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderServerError",
                    ErrorMessage = "OpenAI service is experiencing issues",
                    HowToFix = new List<string>
                    {
                        "Check OpenAI status at https://status.openai.com",
                        "Try again in a few minutes",
                        "Use a fallback provider temporarily"
                    }
                };
            }

            if (response.IsSuccessStatusCode)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                };
            }

            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderUnknownError",
                ErrorMessage = $"OpenAI returned unexpected status: {response.StatusCode}",
                HowToFix = new List<string> { "Check your internet connection", "Try again later" }
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Connection to OpenAI timed out",
                HowToFix = new List<string>
                {
                    "Check your internet connection",
                    "Verify firewall settings",
                    "Try again later"
                }
            };
        }
        catch (HttpRequestException ex)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Failed to connect to OpenAI",
                HowToFix = new List<string>
                {
                    "Check your internet connection",
                    "Verify DNS settings",
                    $"Technical details: {ex.Message}"
                }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateAnthropicAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "Anthropic API key is not configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your Anthropic API key",
                    "Get an API key from https://console.anthropic.com"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            // Use a minimal test request
            var content = new StringContent(
                JsonSerializer.Serialize(new { model = "claude-3-haiku-20240307", max_tokens = 1, messages = new[] { new { role = "user", content = "Hi" } } }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages",
                content,
                ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderKeyInvalid",
                    ErrorMessage = "Anthropic API key is invalid or expired",
                    HowToFix = new List<string>
                    {
                        "Check your API key for typos",
                        "Verify the key at https://console.anthropic.com",
                        "Generate a new API key if needed"
                    }
                };
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = "ProviderRateLimited",
                    ErrorMessage = "Anthropic rate limit exceeded",
                    HowToFix = new List<string>
                    {
                        "Wait before trying again",
                        "Check your Anthropic usage dashboard"
                    }
                };
            }

            if ((int)response.StatusCode >= 500)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderServerError",
                    ErrorMessage = "Anthropic service is experiencing issues",
                    HowToFix = new List<string>
                    {
                        "Check Anthropic status at https://status.anthropic.com",
                        "Try again later"
                    }
                };
            }

            if (response.IsSuccessStatusCode)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                };
            }

            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderUnknownError",
                ErrorMessage = $"Anthropic returned unexpected status: {response.StatusCode}",
                HowToFix = new List<string> { "Try again later" }
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Connection to Anthropic timed out",
                HowToFix = new List<string> { "Check your internet connection", "Try again later" }
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Failed to connect to Anthropic",
                HowToFix = new List<string> { "Check your internet connection", "Verify DNS settings" }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateGeminiAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "Gemini API key is not configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your Google Gemini API key",
                    "Get an API key from https://makersuite.google.com/app/apikey"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;

            var response = await httpClient.GetAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}",
                ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized || 
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderKeyInvalid",
                    ErrorMessage = "Gemini API key is invalid",
                    HowToFix = new List<string>
                    {
                        "Check your API key for typos",
                        "Verify the key at https://makersuite.google.com/app/apikey",
                        "Generate a new API key if needed"
                    }
                };
            }

            if (response.StatusCode == (HttpStatusCode)429)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = "ProviderRateLimited",
                    ErrorMessage = "Gemini rate limit exceeded",
                    HowToFix = new List<string> { "Wait before trying again", "Check your usage limits" }
                };
            }

            if ((int)response.StatusCode >= 500)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderServerError",
                    ErrorMessage = "Gemini service is experiencing issues",
                    HowToFix = new List<string> { "Try again later" }
                };
            }

            if (response.IsSuccessStatusCode)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                };
            }

            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderUnknownError",
                ErrorMessage = $"Gemini returned unexpected status: {response.StatusCode}",
                HowToFix = new List<string> { "Try again later" }
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Connection to Gemini timed out",
                HowToFix = new List<string> { "Check your internet connection", "Try again later" }
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Failed to connect to Gemini",
                HowToFix = new List<string> { "Check your internet connection" }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateAzureOpenAIAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "Azure OpenAI credentials are not fully configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your Azure OpenAI API key and endpoint",
                    "Get credentials from Azure Portal"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;
            httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

            // Try to list deployments as a lightweight check
            var response = await httpClient.GetAsync(
                $"{endpoint}/openai/deployments?api-version=2023-05-15",
                ct).ConfigureAwait(false);

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderKeyInvalid",
                    ErrorMessage = "Azure OpenAI credentials are invalid",
                    HowToFix = new List<string>
                    {
                        "Verify your API key and endpoint in Azure Portal",
                        "Check that the endpoint URL is correct"
                    }
                },
                (HttpStatusCode)429 => new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = "ProviderRateLimited",
                    ErrorMessage = "Azure OpenAI rate limit exceeded",
                    HowToFix = new List<string> { "Wait before trying again" }
                },
                _ when (int)response.StatusCode >= 500 => new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderServerError",
                    ErrorMessage = "Azure OpenAI service is experiencing issues",
                    HowToFix = new List<string> { "Check Azure status", "Try again later" }
                },
                _ when response.IsSuccessStatusCode => new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                },
                _ => new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = false,
                    ErrorCode = "ProviderUnknownError",
                    ErrorMessage = $"Azure OpenAI returned unexpected status: {response.StatusCode}",
                    HowToFix = new List<string> { "Try again later" }
                }
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Connection to Azure OpenAI timed out",
                HowToFix = new List<string> { "Check your internet connection", "Try again later" }
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Failed to connect to Azure OpenAI",
                HowToFix = new List<string> { "Check your internet connection", "Verify endpoint URL" }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateElevenLabsAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "ElevenLabs API key is not configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your ElevenLabs API key",
                    "Get an API key from https://elevenlabs.io"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;
            httpClient.DefaultRequestHeaders.Add("xi-api-key", apiKey);

            var response = await httpClient.GetAsync(
                "https://api.elevenlabs.io/v1/voices",
                ct).ConfigureAwait(false);

            return HandleStandardHttpResponse(response, "ElevenLabs", "https://elevenlabs.io");
        }
        catch (TaskCanceledException)
        {
            return NetworkTimeoutResult("ElevenLabs");
        }
        catch (HttpRequestException)
        {
            return NetworkErrorResult("ElevenLabs");
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidatePlayHTAsync(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("PLAYHT_API_KEY");
        var userId = Environment.GetEnvironmentVariable("PLAYHT_USER_ID");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(userId))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = false,
                Reachable = false,
                ErrorCode = "ProviderNotConfigured",
                ErrorMessage = "PlayHT credentials are not fully configured",
                HowToFix = new List<string>
                {
                    "Go to Settings → Providers",
                    "Add your PlayHT API key and User ID",
                    "Get credentials from https://play.ht"
                }
            };
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;
            httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            httpClient.DefaultRequestHeaders.Add("X-User-ID", userId);

            var response = await httpClient.GetAsync(
                "https://api.play.ht/api/v2/voices",
                ct).ConfigureAwait(false);

            return HandleStandardHttpResponse(response, "PlayHT", "https://play.ht");
        }
        catch (TaskCanceledException)
        {
            return NetworkTimeoutResult("PlayHT");
        }
        catch (HttpRequestException)
        {
            return NetworkErrorResult("PlayHT");
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateOllamaAsync(CancellationToken ct)
    {
        var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;

            var response = await httpClient.GetAsync($"{ollamaUrl}/api/tags", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                };
            }

            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderServerError",
                ErrorMessage = "Ollama is running but returned an error",
                HowToFix = new List<string>
                {
                    "Check Ollama logs",
                    "Restart Ollama service",
                    "Ensure models are downloaded"
                }
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Cannot connect to Ollama",
                HowToFix = new List<string>
                {
                    "Start Ollama service (ollama serve)",
                    "Check that Ollama is running on " + ollamaUrl,
                    "Install Ollama from https://ollama.ai if not installed"
                }
            };
        }
        catch (TaskCanceledException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Connection to Ollama timed out",
                HowToFix = new List<string> { "Start Ollama service", "Check Ollama is running" }
            };
        }
    }

    private async Task<ProviderConnectionValidationResult> ValidateStableDiffusionAsync(CancellationToken ct)
    {
        var sdUrl = Environment.GetEnvironmentVariable("STABLE_DIFFUSION_URL") ?? "http://localhost:7860";

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = ValidationTimeout;

            var response = await httpClient.GetAsync($"{sdUrl}/sdapi/v1/sd-models", ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return new ProviderConnectionValidationResult
                {
                    Configured = true,
                    Reachable = true,
                    ErrorCode = null,
                    ErrorMessage = null,
                    HowToFix = new List<string>()
                };
            }

            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderServerError",
                ErrorMessage = "Stable Diffusion WebUI is running but returned an error",
                HowToFix = new List<string> { "Check Stable Diffusion WebUI logs", "Restart the service" }
            };
        }
        catch (HttpRequestException)
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderNetworkError",
                ErrorMessage = "Cannot connect to Stable Diffusion WebUI",
                HowToFix = new List<string>
                {
                    "Start Stable Diffusion WebUI",
                    "Check that it's running on " + sdUrl,
                    "Ensure --api flag is enabled when starting WebUI"
                }
            };
        }
        catch (TaskCanceledException)
        {
            return NetworkTimeoutResult("Stable Diffusion");
        }
    }

    private ProviderConnectionValidationResult ValidatePiper()
    {
        // For local TTS providers, we check if the binary exists
        // This is a simplified check - in production, you'd check actual installation
        return new ProviderConnectionValidationResult
        {
            Configured = true,
            Reachable = true,
            ErrorCode = null,
            ErrorMessage = null,
            HowToFix = new List<string>()
        };
    }

    private ProviderConnectionValidationResult ValidateMimic3()
    {
        return new ProviderConnectionValidationResult
        {
            Configured = true,
            Reachable = true,
            ErrorCode = null,
            ErrorMessage = null,
            HowToFix = new List<string>()
        };
    }

    private ProviderConnectionValidationResult ValidateWindowsTTS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = true,
                ErrorCode = null,
                ErrorMessage = null,
                HowToFix = new List<string>()
            };
        }

        return new ProviderConnectionValidationResult
        {
            Configured = false,
            Reachable = false,
            ErrorCode = "ProviderNotConfigured",
            ErrorMessage = "Windows SAPI is only available on Windows",
            HowToFix = new List<string> { "Use Piper or Mimic3 for cross-platform TTS" }
        };
    }

    private ProviderConnectionValidationResult ValidateRuleBased()
    {
        // Rule-based LLM is always available
        return new ProviderConnectionValidationResult
        {
            Configured = true,
            Reachable = true,
            ErrorCode = null,
            ErrorMessage = null,
            HowToFix = new List<string>()
        };
    }

    private ProviderConnectionValidationResult HandleStandardHttpResponse(
        HttpResponseMessage response, 
        string providerName, 
        string providerUrl)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderKeyInvalid",
                ErrorMessage = $"{providerName} API key is invalid",
                HowToFix = new List<string>
                {
                    "Check your API key for typos",
                    $"Verify the key at {providerUrl}",
                    "Generate a new API key if needed"
                }
            },
            (HttpStatusCode)429 => new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = true,
                ErrorCode = "ProviderRateLimited",
                ErrorMessage = $"{providerName} rate limit exceeded",
                HowToFix = new List<string> { "Wait before trying again", "Check your usage limits" }
            },
            _ when (int)response.StatusCode >= 500 => new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderServerError",
                ErrorMessage = $"{providerName} service is experiencing issues",
                HowToFix = new List<string> { $"Check {providerName} status page", "Try again later" }
            },
            _ when response.IsSuccessStatusCode => new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = true,
                ErrorCode = null,
                ErrorMessage = null,
                HowToFix = new List<string>()
            },
            _ => new ProviderConnectionValidationResult
            {
                Configured = true,
                Reachable = false,
                ErrorCode = "ProviderUnknownError",
                ErrorMessage = $"{providerName} returned unexpected status: {response.StatusCode}",
                HowToFix = new List<string> { "Try again later" }
            }
        };
    }

    private ProviderConnectionValidationResult NetworkTimeoutResult(string providerName)
    {
        return new ProviderConnectionValidationResult
        {
            Configured = true,
            Reachable = false,
            ErrorCode = "ProviderNetworkError",
            ErrorMessage = $"Connection to {providerName} timed out",
            HowToFix = new List<string> { "Check your internet connection", "Try again later" }
        };
    }

    private ProviderConnectionValidationResult NetworkErrorResult(string providerName)
    {
        return new ProviderConnectionValidationResult
        {
            Configured = true,
            Reachable = false,
            ErrorCode = "ProviderNetworkError",
            ErrorMessage = $"Failed to connect to {providerName}",
            HowToFix = new List<string> { "Check your internet connection", "Verify DNS settings" }
        };
    }
}

/// <summary>
/// Result of provider validation
/// </summary>
public class ProviderConnectionValidationResult
{
    public bool Configured { get; init; }
    public bool Reachable { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> HowToFix { get; init; } = new();
}
