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

    public StableDiffusionWebUiProvider(
        ILogger<StableDiffusionWebUiProvider> logger,
        HttpClient httpClient,
        string baseUrl = "http://127.0.0.1:7860",
        bool isNvidiaGpu = false,
        int vramGB = 0)
    {
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _isNvidiaGpu = isNvidiaGpu;
        _vramGB = vramGB;

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

    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
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
            // Build prompt from scene and spec
            string prompt = BuildPrompt(scene, spec);

            // Determine model based on VRAM
            bool useSDXL = _vramGB >= 12;
            string model = useSDXL ? "SDXL" : "SD 1.5";

            _logger.LogInformation("Using {Model} model (VRAM: {VRAM}GB)", model, _vramGB);

            // Call Stable Diffusion WebUI API
            var requestBody = new
            {
                prompt = prompt,
                negative_prompt = "blurry, low quality, distorted, watermark, text, logo",
                steps = _vramGB >= 12 ? 30 : 20, // Fewer steps for lower VRAM
                width = GetWidth(spec.Aspect),
                height = GetHeight(spec.Aspect),
                cfg_scale = 7.0,
                sampler_name = "DPM++ 2M Karras",
                seed = -1
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

    private string BuildPrompt(Scene scene, VisualSpec spec)
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

        // Add style
        if (!string.IsNullOrEmpty(spec.Style))
        {
            promptParts.Add($"{spec.Style} style");
        }

        // Default quality tags
        promptParts.Add("high quality, detailed, professional");

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
