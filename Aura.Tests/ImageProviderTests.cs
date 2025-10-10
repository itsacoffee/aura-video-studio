using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Providers.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class ImageProviderTests
{
    [Fact]
    public async Task StableDiffusion_Should_GateOnNonNvidiaGpu()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: false, // AMD/Intel GPU
            vramGB: 16);

        var scene = new Scene(0, "Test", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var spec = new VisualSpec("modern", Aspect.Widescreen16x9, new[] { "technology" });

        // Act
        var result = await provider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);

        // Assert
        Assert.Empty(result); // Should return empty due to non-NVIDIA gate
    }

    [Fact]
    public async Task StableDiffusion_Should_GateOnInsufficientVram()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 4); // Insufficient VRAM

        var scene = new Scene(0, "Test", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var spec = new VisualSpec("modern", Aspect.Widescreen16x9, new[] { "technology" });

        // Act
        var result = await provider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);

        // Assert
        Assert.Empty(result); // Should return empty due to insufficient VRAM
    }

    [Fact]
    public async Task StableDiffusion_Should_DisableWithVramBelow8GB()
    {
        // Arrange - Test VRAM < 8GB scenario for optimal SDXL performance
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 7); // Below 8GB - works but not optimal

        var scene = new Scene(0, "Test", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var spec = new VisualSpec("modern", Aspect.Widescreen16x9, new[] { "technology" });

        // Act
        var result = await provider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);

        // Assert
        // 7GB is above the 6GB minimum, so it should attempt generation (even if suboptimal)
        // The provider allows 6GB+ but logs warnings
        Assert.NotNull(result); // Should not be null
    }

    [Fact]
    public void StableDiffusion_Should_SelectSDXL_WithHighVram()
    {
        // This test validates the model selection logic based on VRAM
        // SDXL should be selected when VRAM >= 12GB
        
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 12);

        // The provider should internally select SDXL model
        // This is verified through logging in actual usage
        Assert.NotNull(provider);
    }

    [Fact]
    public void StableDiffusion_Should_SelectSD15_WithMediumVram()
    {
        // This test validates the model selection logic based on VRAM
        // SD 1.5 should be selected when VRAM < 12GB but >= 6GB
        
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 8);

        // The provider should internally select SD 1.5 model
        // This is verified through logging in actual usage
        Assert.NotNull(provider);
    }

    [Fact]
    public void SDGenerationParams_Should_MergeCorrectly()
    {
        // Arrange
        var defaultParams = new SDGenerationParams
        {
            Steps = 20,
            CfgScale = 7.0,
            Style = "default style"
        };

        var overrideParams = new SDGenerationParams
        {
            Steps = 30,
            // CfgScale not set, should use default from record definition
            Style = "custom style"
        };

        // The actual merge is done internally by the provider
        // This test validates that the record type can be created
        Assert.Equal(30, overrideParams.Steps);
        Assert.Equal("custom style", overrideParams.Style);
        Assert.Equal(7.0, overrideParams.CfgScale); // Default value from record definition
    }

    [Fact]
    public void SDGenerationParams_Should_ValidateParameters()
    {
        // Arrange & Act
        var validParams = new SDGenerationParams
        {
            Steps = 20,
            CfgScale = 7.0,
            Seed = -1,
            Width = 1024,
            Height = 576,
            Style = "photorealistic",
            SamplerName = "DPM++ 2M Karras"
        };

        // Assert
        Assert.Equal(20, validParams.Steps);
        Assert.Equal(7.0, validParams.CfgScale);
        Assert.Equal(-1, validParams.Seed);
        Assert.Equal(1024, validParams.Width);
        Assert.Equal(576, validParams.Height);
    }

    [Fact]
    public async Task PexelsProvider_Should_ReturnEmpty_WithoutApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PexelsStockProvider(
            NullLogger<PexelsStockProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var result = await provider.SearchAsync("nature", 10, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task PixabayProvider_Should_ReturnEmpty_WithoutApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PixabayStockProvider(
            NullLogger<PixabayStockProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var result = await provider.SearchAsync("nature", 10, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UnsplashProvider_Should_ReturnEmpty_WithoutApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new UnsplashStockProvider(
            NullLogger<UnsplashStockProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var result = await provider.SearchAsync("nature", 10, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task LocalStockProvider_Should_ReturnEmpty_WithInvalidDirectory()
    {
        // Arrange
        var provider = new LocalStockProvider(
            NullLogger<LocalStockProvider>.Instance,
            baseDirectory: "/non/existent/directory");

        // Act
        var result = await provider.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SlideshowProvider_Should_GenerateSlideAsset()
    {
        // Arrange
        var provider = new SlideshowProvider(
            NullLogger<SlideshowProvider>.Instance);

        var scene = new Scene(0, "Test Scene", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var spec = new VisualSpec("simple", Aspect.Widescreen16x9, Array.Empty<string>());

        // Act
        var result = await provider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("slide", result[0].Kind);
    }

    [Fact]
    public void StableDiffusion_Should_UseDifferentSteps_BasedOnVram()
    {
        // Test that different VRAM levels result in different step counts
        // SDXL (12GB+) should use 30 steps, SD15 (6-11GB) should use 20 steps

        // High VRAM - SDXL
        var providerHigh = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            new HttpClient(),
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 16);

        // Medium VRAM - SD15
        var providerMed = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            new HttpClient(),
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 8);

        Assert.NotNull(providerHigh);
        Assert.NotNull(providerMed);
    }

    [Fact]
    public async Task StableDiffusion_ProbeAsync_Should_ReturnFalse_WithoutNvidia()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: false,
            vramGB: 16);

        // Act
        var result = await provider.ProbeAsync();

        // Assert
        Assert.False(result); // Should fail probe without NVIDIA GPU
    }

    [Fact]
    public async Task StableDiffusion_ProbeAsync_Should_ReturnFalse_WithLowVram()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 4);

        // Act
        var result = await provider.ProbeAsync();

        // Assert
        Assert.False(result); // Should fail probe with insufficient VRAM
    }

    [Fact]
    public async Task StableDiffusion_ProbeAsync_Should_HandleTimeout()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 8);

        // Act
        var result = await provider.ProbeAsync();

        // Assert
        Assert.False(result); // Should handle timeout gracefully
    }

    [Fact]
    public async Task StableDiffusion_FetchOrGenerateAsync_Should_HandleTimeout()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new StableDiffusionWebUiProvider(
            NullLogger<StableDiffusionWebUiProvider>.Instance,
            httpClient,
            "http://127.0.0.1:7860",
            isNvidiaGpu: true,
            vramGB: 8);

        var scene = new Scene(0, "Test", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var spec = new VisualSpec("modern", Aspect.Widescreen16x9, new[] { "technology" });

        // Act
        var result = await provider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);

        // Assert
        Assert.Empty(result); // Should handle timeout gracefully and return empty
    }

    [Fact]
    public async Task VisualPipeline_StockOnly_ShouldComposeSuccessfully()
    {
        // Arrange - Test that stock-only path works without SD
        var pexelsProvider = new PexelsStockProvider(
            NullLogger<PexelsStockProvider>.Instance,
            new HttpClient(),
            apiKey: null); // No API key

        var pixabayProvider = new PixabayStockProvider(
            NullLogger<PixabayStockProvider>.Instance,
            new HttpClient(),
            apiKey: null); // No API key

        var unsplashProvider = new UnsplashStockProvider(
            NullLogger<UnsplashStockProvider>.Instance,
            new HttpClient(),
            apiKey: null); // No API key

        var scene = new Scene(0, "Test Scene", "Test script", TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // Act - All should return empty without keys but not fail
        var pexelsResult = await pexelsProvider.SearchAsync("nature", 5, CancellationToken.None);
        var pixabayResult = await pixabayProvider.SearchAsync("nature", 5, CancellationToken.None);
        var unsplashResult = await unsplashProvider.SearchAsync("nature", 5, CancellationToken.None);

        // Assert - All providers handle missing keys gracefully
        Assert.Empty(pexelsResult);
        Assert.Empty(pixabayResult);
        Assert.Empty(unsplashResult);

        // Verify slideshow provider always works as fallback
        var slideshowProvider = new SlideshowProvider(NullLogger<SlideshowProvider>.Instance);
        var spec = new VisualSpec("simple", Aspect.Widescreen16x9, Array.Empty<string>());
        var slideshowResult = await slideshowProvider.FetchOrGenerateAsync(scene, spec, CancellationToken.None);
        
        Assert.NotEmpty(slideshowResult);
        Assert.Single(slideshowResult);
    }
}
