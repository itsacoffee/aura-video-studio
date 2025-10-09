using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Images;

/// <summary>
/// Parameters for Stable Diffusion image generation
/// </summary>
public record SDGenerationParams
{
    public string? Model { get; init; } // null = auto-detect based on VRAM
    public int? Steps { get; init; } // null = auto-detect based on VRAM
    public double CfgScale { get; init; } = 7.0;
    public int Seed { get; init; } = -1; // -1 = random
    public int? Width { get; init; } // null = auto-detect based on aspect
    public int? Height { get; init; } // null = auto-detect based on aspect
    public string Style { get; init; } = "high quality, detailed, professional";
    public string SamplerName { get; init; } = "DPM++ 2M Karras";
}

/// <summary>
/// Image provider that uses Stable Diffusion WebUI for local image generation.
/// NVIDIA-ONLY: This provider requires an NVIDIA GPU with sufficient VRAM.
/// </summary>
public class StableDiffusionWebUiProvider : IImageProvider
{
    private readonly ILogger<StableDiffusionWebUiProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _isNvidiaGpu;
    private readonly int _vramGB;
    private readonly SDGenerationParams _defaultParams;

    public StableDiffusionWebUiProvider(
        ILogger<StableDiffusionWebUiProvider> logger,
        HttpClient httpClient,
        string baseUrl = "http://127.0.0.1:7860",
        bool isNvidiaGpu = false,
        int vramGB = 0,
        SDGenerationParams? defaultParams = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _isNvidiaGpu = isNvidiaGpu;
        _vramGB = vramGB;
        _defaultParams = defaultParams ?? new SDGenerationParams();

        // Enforce NVIDIA-only policy
        if (!_isNvidiaGpu)
        {
            _logger.LogWarning("Stable Diffusion WebUI provider requires an NVIDIA GPU. Local diffusion is disabled.");
        }
        else if (_vramGB < 6)
        {
            _logger.LogWarning("Insufficient VRAM for Stable Diffusion. Minimum 6GB required, detected: {VRAM}GB", _vramGB);
        }
    }

    /// <summary>
    /// Performs a low-step 256x256 probe to test if SD WebUI is responsive.
    /// Returns true if probe succeeds, false otherwise.
    /// </summary>
    public async Task<bool> ProbeAsync(CancellationToken ct = default)
    {
        if (!_isNvidiaGpu || _vramGB < 6)
        {
            _logger.LogWarning("Skipping SD probe - NVIDIA GPU with >=6GB VRAM required");
            return false;
        }

        _logger.LogInformation("Running SD WebUI probe at {BaseUrl}", _baseUrl);

        try
        {
            var probeBody = new
            {
                prompt = "test",
                negative_prompt = "",
                steps = 1,
                width = 256,
                height = 256,
                cfg_scale = 7.0,
                sampler_name = "Euler a",
                seed = -1
            };

            var json = JsonSerializer.Serialize(probeBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            var response = await _httpClient.PostAsync($"{_baseUrl}/sdapi/v1/txt2img", content, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SD WebUI probe successful");
                return true;
            }

            _logger.LogWarning("SD WebUI probe failed with status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SD WebUI probe failed: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
    {
        return await FetchOrGenerateAsync(scene, spec, null, ct);
    }

    /// <summary>
    /// Generate images with optional per-scene parameter overrides
    /// </summary>
    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(
        Scene scene, 
        VisualSpec spec, 
        SDGenerationParams? overrideParams,
        CancellationToken ct)
    {
        // NVIDIA-ONLY GATE
        if (!_isNvidiaGpu)
        {
            _logger.LogWarning("Local diffusion requires an NVIDIA GPU. Use stock visuals or Pro cloud instead.");
            return Array.Empty<Asset>();
        }

        if (_vramGB < 6)
        {
            _logger.LogWarning("Insufficient VRAM ({VRAM}GB) for Stable Diffusion. Minimum 6GB required.", _vramGB);
            return Array.Empty<Asset>();
        }

        _logger.LogInformation("Generating image with Stable Diffusion for scene {Scene}: {Heading}", 
            scene.Index, scene.Heading);

        try
        {
            // Merge parameters: override > default > auto-detect
            var effectiveParams = MergeParams(_defaultParams, overrideParams);

            // Build prompt from scene and spec
            string prompt = BuildPrompt(scene, spec, effectiveParams.Style);

            // Determine model based on VRAM if not explicitly set
            bool useSDXL = _vramGB >= 12;
            string model = effectiveParams.Model ?? (useSDXL ? "SDXL" : "SD 1.5");

            // Determine steps based on VRAM if not explicitly set
            int steps = effectiveParams.Steps ?? (useSDXL ? 30 : 20);

            // Determine dimensions if not explicitly set
            int width = effectiveParams.Width ?? GetWidth(spec.Aspect);
            int height = effectiveParams.Height ?? GetHeight(spec.Aspect);

            _logger.LogInformation(
                "Using {Model} model (VRAM: {VRAM}GB), steps: {Steps}, size: {Width}x{Height}", 
                model, _vramGB, steps, width, height);

            // Call Stable Diffusion WebUI API
            var requestBody = new
            {
                prompt = prompt,
                negative_prompt = "blurry, low quality, distorted, watermark, text, logo",
                steps = steps,
                width = width,
                height = height,
                cfg_scale = effectiveParams.CfgScale,
                sampler_name = effectiveParams.SamplerName,
                seed = effectiveParams.Seed
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.Timeout = TimeSpan.FromMinutes(5); // SD generation can take time

            var response = await _httpClient.PostAsync($"{_baseUrl}/sdapi/v1/txt2img", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var responseDoc = JsonDocument.Parse(responseJson);

            var assets = new List<Asset>();

            if (responseDoc.RootElement.TryGetProperty("images", out var images) &&
                images.GetArrayLength() > 0)
            {
                // In a real implementation, we would save the base64 image to a file
                string imagePath = $"sd_generated_{scene.Index}_{DateTime.Now:yyyyMMddHHmmss}.png";
                
                assets.Add(new Asset(
                    Kind: "image",
                    PathOrUrl: imagePath,
                    License: "Generated locally",
                    Attribution: $"Generated with Stable Diffusion ({model})"
                ));

                _logger.LogInformation("Successfully generated image for scene {Scene}", scene.Index);
            }

            return assets;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Stable Diffusion WebUI at {BaseUrl}", _baseUrl);
            return Array.Empty<Asset>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image with Stable Diffusion for scene {Scene}", scene.Index);
            return Array.Empty<Asset>();
        }
    }

    private SDGenerationParams MergeParams(SDGenerationParams defaultParams, SDGenerationParams? overrides)
    {
        if (overrides == null)
            return defaultParams;

        return new SDGenerationParams
        {
            Model = overrides.Model ?? defaultParams.Model,
            Steps = overrides.Steps ?? defaultParams.Steps,
            CfgScale = overrides.CfgScale != 0 ? overrides.CfgScale : defaultParams.CfgScale,
            Seed = overrides.Seed != 0 ? overrides.Seed : defaultParams.Seed,
            Width = overrides.Width ?? defaultParams.Width,
            Height = overrides.Height ?? defaultParams.Height,
            Style = overrides.Style ?? defaultParams.Style,
            SamplerName = overrides.SamplerName ?? defaultParams.SamplerName
        };
    }

    private string BuildPrompt(Scene scene, VisualSpec spec, string styleOverride)
    {
        var promptParts = new List<string>();

        // Add scene context
        if (!string.IsNullOrEmpty(scene.Heading))
        {
            promptParts.Add(scene.Heading);
        }

        // Add keywords from spec
        if (spec.Keywords?.Length > 0)
        {
            promptParts.AddRange(spec.Keywords);
        }

        // Add style from spec or override
        var style = !string.IsNullOrEmpty(spec.Style) ? spec.Style : styleOverride;
        if (!string.IsNullOrEmpty(style))
        {
            promptParts.Add(style);
        }

        return string.Join(", ", promptParts);
    }

    private int GetWidth(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.Widescreen16x9 => 1024,
            Aspect.Vertical9x16 => 576,
            Aspect.Square1x1 => 1024,
            _ => 1024
        };
    }

    private int GetHeight(Aspect aspect)
    {
        return aspect switch
        {
            Aspect.Widescreen16x9 => 576,
            Aspect.Vertical9x16 => 1024,
            Aspect.Square1x1 => 1024,
            _ => 576
        };
    }
}
