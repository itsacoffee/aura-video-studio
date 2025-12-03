using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

// Use explicit alias to avoid ambiguity with Aura.Core.Providers.FallbackImageProvider
using FallbackImageProvider = Aura.Providers.Images.FallbackImageProvider;
using PlaceholderImageProvider = Aura.Providers.Images.PlaceholderImageProvider;

namespace Aura.Tests;

/// <summary>
/// Tests for FallbackImageProvider fallback chain behavior
/// </summary>
public class FallbackImageProviderTests
{
    private readonly Mock<ILogger<FallbackImageProvider>> _loggerMock;
    private readonly PlaceholderImageProvider _placeholderProvider;
    private readonly Scene _testScene;
    private readonly VisualSpec _testSpec;

    public FallbackImageProviderTests()
    {
        _loggerMock = new Mock<ILogger<FallbackImageProvider>>();
        _placeholderProvider = new PlaceholderImageProvider(
            NullLogger<PlaceholderImageProvider>.Instance);
        _testScene = new Scene(
            Index: 0,
            Heading: "Test Scene",
            Script: "This is a test scene for video generation",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));
        _testSpec = new VisualSpec("modern", Aspect.Widescreen16x9, Array.Empty<string>());
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Use_Primary_Provider_When_Available()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<IStockProvider>();
        mockPrimaryProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Asset>
            {
                new Asset("image", "https://example.com/image.jpg", "Test License", "Test Attribution")
            });

        var primaryProviders = new List<IStockProvider> { mockPrimaryProvider.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        var result = await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("https://example.com/image.jpg", result[0].PathOrUrl);
        mockPrimaryProvider.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Fall_Back_To_Placeholder_When_Primary_Returns_Empty()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<IStockProvider>();
        mockPrimaryProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Asset>());

        var primaryProviders = new List<IStockProvider> { mockPrimaryProvider.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        var result = await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("image", result[0].Kind);
        Assert.Contains("placeholder", result[0].PathOrUrl.ToLowerInvariant());
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Fall_Back_To_Placeholder_When_Primary_Throws()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<IStockProvider>();
        mockPrimaryProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API key not configured"));

        var primaryProviders = new List<IStockProvider> { mockPrimaryProvider.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        var result = await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("image", result[0].Kind);
        // Placeholder provider should have been used
        Assert.Contains("placeholder", result[0].PathOrUrl.ToLowerInvariant());
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Try_Multiple_Providers_Before_Fallback()
    {
        // Arrange
        var mockProvider1 = new Mock<IStockProvider>();
        mockProvider1
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider 1 failed"));

        var mockProvider2 = new Mock<IStockProvider>();
        mockProvider2
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Asset>
            {
                new Asset("image", "https://provider2.com/image.jpg", "License 2", "Attribution 2")
            });

        var primaryProviders = new List<IStockProvider> { mockProvider1.Object, mockProvider2.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        var result = await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("https://provider2.com/image.jpg", result[0].PathOrUrl);
        mockProvider1.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        mockProvider2.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Use_Placeholder_When_No_Primary_Providers()
    {
        // Arrange
        var primaryProviders = new List<IStockProvider>();
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        var result = await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal("image", result[0].Kind);
        Assert.Contains("placeholder", result[0].PathOrUrl.ToLowerInvariant());
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Use_Scene_Heading_As_Query()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<IStockProvider>();
        string? capturedQuery = null;
        mockPrimaryProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((query, count, ct) => capturedQuery = query)
            .ReturnsAsync(new List<Asset>
            {
                new Asset("image", "https://example.com/image.jpg", "License", "Attribution")
            });

        var primaryProviders = new List<IStockProvider> { mockPrimaryProvider.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        await fallbackProvider.FetchOrGenerateAsync(_testScene, _testSpec);

        // Assert
        Assert.Equal("Test Scene", capturedQuery);
    }

    [Fact]
    public async Task FallbackImageProvider_Should_Use_Script_When_Heading_Is_Empty()
    {
        // Arrange
        var sceneWithEmptyHeading = new Scene(
            Index: 0,
            Heading: "",
            Script: "Fallback to script text",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        var mockPrimaryProvider = new Mock<IStockProvider>();
        string? capturedQuery = null;
        mockPrimaryProvider
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((query, count, ct) => capturedQuery = query)
            .ReturnsAsync(new List<Asset>
            {
                new Asset("image", "https://example.com/image.jpg", "License", "Attribution")
            });

        var primaryProviders = new List<IStockProvider> { mockPrimaryProvider.Object };
        var fallbackProvider = new FallbackImageProvider(_loggerMock.Object, primaryProviders, _placeholderProvider);

        // Act
        await fallbackProvider.FetchOrGenerateAsync(sceneWithEmptyHeading, _testSpec);

        // Assert
        Assert.Equal("Fallback to script text", capturedQuery);
    }

    [Fact]
    public void FallbackImageProvider_Constructor_Should_Throw_For_Null_Logger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FallbackImageProvider(null!, new List<IStockProvider>(), _placeholderProvider));
    }

    [Fact]
    public void FallbackImageProvider_Constructor_Should_Throw_For_Null_Placeholder()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FallbackImageProvider(_loggerMock.Object, new List<IStockProvider>(), null!));
    }

    [Fact]
    public void FallbackImageProvider_Constructor_Should_Accept_Null_Primary_Providers()
    {
        // Act - should not throw, null is treated as empty list
        var provider = new FallbackImageProvider(_loggerMock.Object, null!, _placeholderProvider);

        // Assert
        Assert.NotNull(provider);
    }
}
