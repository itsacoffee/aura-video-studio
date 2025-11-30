using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Visuals;

/// <summary>
/// Local Stable Diffusion provider for self-hosted SD WebUI or ComfyUI
/// Enhanced with connection retry logic and better error handling
/// </summary>
public class LocalStableDiffusionProvider : BaseVisualProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _webUiUrl;
    private readonly bool _isNvidiaGpu;
    private readonly int _vramGB;
    
    // Retry configuration
    private const int MAX_RETRIES = 3;
    private const int INITIAL_RETRY_DELAY_MS = 1000;
    private const int CONNECTION_TIMEOUT_SECONDS = 30;
    private const int GENERATION_TIMEOUT_SECONDS = 180; // Image gen can take a while

    public LocalStableDiffusionProvider(
        ILogger<LocalStableDiffusionProvider> logger,
        HttpClient httpClient,
        string webUiUrl = "http://127.0.0.1:7860",
        bool isNvidiaGpu = true,
        int vramGB = 8) : base(logger)
    {
        _httpClient = httpClient;
        _webUiUrl = webUiUrl.TrimEnd('/');
        _isNvidiaGpu = isNvidiaGpu;
        _vramGB = vramGB;
    }

    public override string ProviderName => "LocalSD";

    public override bool RequiresApiKey => false;

    /// <summary>
    /// Get the WebUI URL being used
    /// </summary>
    public string WebUiUrl => _webUiUrl;

    public override async Task<string?> GenerateImageAsync(
        string prompt,
        VisualGenerationOptions options,
        CancellationToken ct = default)
    {
        if (!_isNvidiaGpu)
        {
            Logger.LogWarning("Local SD requires NVIDIA GPU");
            return null;
        }

        if (_vramGB < 6)
        {
            Logger.LogWarning("Local SD requires at least 6GB VRAM, have {VRAM}GB", _vramGB);
            return null;
        }

        // First check if server is available
        if (!await IsServerAvailableWithRetryAsync(ct).ConfigureAwait(false))
        {
            Logger.LogWarning("Stable Diffusion WebUI server is not available at {Url}", _webUiUrl);
            return null;
        }

        try
        {
            Logger.LogInformation("Generating image with Local SD for prompt: {Prompt}", prompt);

            var adaptedPrompt = AdaptPrompt(prompt, options);
            var model = _vramGB >= 12 ? "sd_xl_base_1.0" : "v1-5-pruned-emaonly";

            var requestBody = new
            {
                prompt = adaptedPrompt,
                negative_prompt = options.NegativePrompts != null && options.NegativePrompts.Length > 0
                    ? string.Join(", ", options.NegativePrompts)
                    : "low quality, blurry, distorted",
                width = options.Width,
                height = options.Height,
                steps = 30,
                cfg_scale = 7,
                sampler_name = "DPM++ 2M Karras",
                override_settings = new
                {
                    sd_model_checkpoint = model
                }
            };

            var result = await SendRequestWithRetryAsync<SdWebUiResponse>(
                HttpMethod.Post,
                $"{_webUiUrl}/sdapi/v1/txt2img",
                requestBody,
                GENERATION_TIMEOUT_SECONDS,
                ct).ConfigureAwait(false);
            
            if (result?.Images != null && result.Images.Count > 0)
            {
                var imageBytes = Convert.FromBase64String(result.Images[0]);
                var tempPath = Path.Combine(Path.GetTempPath(), $"localsd_{Guid.NewGuid()}.png");
                await File.WriteAllBytesAsync(tempPath, imageBytes, ct).ConfigureAwait(false);

                Logger.LogInformation("Local SD image generated successfully: {Path}", tempPath);
                return tempPath;
            }

            Logger.LogWarning("Local SD returned no images");
            return null;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Image generation was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating image with Local SD");
            return null;
        }
    }

    public override VisualProviderCapabilities GetProviderCapabilities()
    {
        return new VisualProviderCapabilities
        {
            ProviderName = ProviderName,
            SupportsNegativePrompts = true,
            SupportsBatchGeneration = true,
            SupportsStylePresets = false,
            SupportedAspectRatios = new() { "16:9", "9:16", "1:1", "4:3", "3:2", "2:3" },
            SupportedStyles = new()
            {
                "photorealistic", "artistic", "anime", "digital-art",
                "fantasy", "realistic", "stylized"
            },
            MaxWidth = _vramGB >= 12 ? 1536 : 1024,
            MaxHeight = _vramGB >= 12 ? 1536 : 1024,
            IsLocal = true,
            IsFree = true,
            CostPerImage = 0m,
            Tier = "Free"
        };
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isNvidiaGpu || _vramGB < 6)
        {
            return false;
        }

        return await IsServerAvailableWithRetryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Check if server is available with automatic retry
    /// </summary>
    private async Task<bool> IsServerAvailableWithRetryAsync(CancellationToken ct)
    {
        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(CONNECTION_TIMEOUT_SECONDS));
                
                var response = await _httpClient.GetAsync(
                    $"{_webUiUrl}/sdapi/v1/sd-models", 
                    cts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                Logger.LogDebug("Health check returned {Status}, attempt {Attempt}/{Max}", 
                    response.StatusCode, attempt + 1, MAX_RETRIES);
            }
            catch (HttpRequestException ex)
            {
                Logger.LogDebug(ex, "Connection to SD WebUI failed, attempt {Attempt}/{Max}", 
                    attempt + 1, MAX_RETRIES);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                Logger.LogDebug("Connection to SD WebUI timed out, attempt {Attempt}/{Max}", 
                    attempt + 1, MAX_RETRIES);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
            
            if (attempt < MAX_RETRIES - 1)
            {
                var delay = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt);
                Logger.LogDebug("Waiting {Delay}ms before retry", delay);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        
        return false;
    }

    /// <summary>
    /// Send HTTP request with retry logic
    /// </summary>
    private async Task<T?> SendRequestWithRetryAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        int timeoutSeconds,
        CancellationToken ct) where T : class
    {
        Exception? lastException = null;
        
        for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                
                var request = new HttpRequestMessage(method, url);
                
                if (body != null)
                {
                    request.Content = new StringContent(
                        JsonSerializer.Serialize(body),
                        Encoding.UTF8,
                        "application/json");
                }
                
                var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning("SD WebUI request failed: {Status} - {Content}", 
                        response.StatusCode, content);
                    
                    // Certain errors should not be retried
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                        response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return null;
                    }
                    
                    lastException = new HttpRequestException($"Request failed with status {response.StatusCode}");
                }
                else
                {
                    return JsonSerializer.Deserialize<T>(content);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogWarning(ex, "HTTP request to SD WebUI failed, attempt {Attempt}/{Max}", 
                    attempt + 1, MAX_RETRIES);
                lastException = ex;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                Logger.LogWarning("Request to SD WebUI timed out after {Timeout}s, attempt {Attempt}/{Max}", 
                    timeoutSeconds, attempt + 1, MAX_RETRIES);
                lastException = new TimeoutException($"Request timed out after {timeoutSeconds}s");
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry on cancellation
            }
            
            if (attempt < MAX_RETRIES - 1)
            {
                var delay = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt);
                Logger.LogDebug("Waiting {Delay}ms before retry", delay);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        
        if (lastException != null)
        {
            Logger.LogError(lastException, "All retry attempts exhausted for SD WebUI request");
        }
        
        return null;
    }

    public override string AdaptPrompt(string prompt, VisualGenerationOptions options)
    {
        var adapted = prompt;

        if (!adapted.Contains("masterpiece") && !adapted.Contains("high quality"))
        {
            adapted = "masterpiece, best quality, " + adapted;
        }

        if (options.Style == "photorealistic" && !adapted.Contains("photorealistic"))
        {
            adapted += ", photorealistic, 8k uhd, dslr";
        }
        else if (options.Style == "anime" && !adapted.Contains("anime"))
        {
            adapted += ", anime style, illustration";
        }

        return adapted;
    }

    public override decimal GetCostEstimate(VisualGenerationOptions options)
    {
        return 0m;
    }

    /// <summary>
    /// Get detailed connection status for diagnostics
    /// </summary>
    public async Task<SDConnectionStatus> GetConnectionStatusAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            
            var response = await _httpClient.GetAsync(
                $"{_webUiUrl}/sdapi/v1/options", 
                cts.Token).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var options = JsonSerializer.Deserialize<JsonElement>(content);
                
                string? currentModel = null;
                if (options.TryGetProperty("sd_model_checkpoint", out var modelEl))
                {
                    currentModel = modelEl.GetString();
                }
                
                return new SDConnectionStatus(
                    IsConnected: true,
                    Url: _webUiUrl,
                    CurrentModel: currentModel,
                    ErrorMessage: null
                );
            }
            
            return new SDConnectionStatus(
                IsConnected: false,
                Url: _webUiUrl,
                CurrentModel: null,
                ErrorMessage: $"Server returned status {response.StatusCode}"
            );
        }
        catch (Exception ex)
        {
            return new SDConnectionStatus(
                IsConnected: false,
                Url: _webUiUrl,
                CurrentModel: null,
                ErrorMessage: ex.Message
            );
        }
    }

    private sealed class SdWebUiResponse
    {
        public List<string>? Images { get; set; }
        public string? Info { get; set; }
    }
}

/// <summary>
/// Connection status for SD WebUI
/// </summary>
public record SDConnectionStatus(
    bool IsConnected,
    string Url,
    string? CurrentModel,
    string? ErrorMessage
);
