using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Assets;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Aura.Providers.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aura.Tests;

public class UnifiedStockProviderTests
{
    #region Fallback Chain Tests

    [Fact]
    public async Task UnifiedStockProvider_Should_Return_Results_From_First_Available_Provider()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, returnsResults: true);
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: true);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object, mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" });

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 5);

        // Assert
        Assert.False(result.IsPlaceholder);
        Assert.Equal("Pexels", result.SourceProvider);
        Assert.NotEmpty(result.Assets);
        
        // Verify Pexels was called but Pixabay was not
        mockPexels.Verify(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        mockPixabay.Verify(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnifiedStockProvider_Should_Fallback_When_First_Provider_Returns_Empty()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, returnsResults: false);
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: true);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object, mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" });

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 5);

        // Assert
        Assert.False(result.IsPlaceholder);
        Assert.Equal("Pixabay", result.SourceProvider);
        Assert.Contains("Pexels", result.ProvidersTried);
        Assert.Contains("Pixabay", result.ProvidersTried);
    }

    [Fact]
    public async Task UnifiedStockProvider_Should_Fallback_When_First_Provider_Throws()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, throws: true);
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: true);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object, mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" });

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 5);

        // Assert
        Assert.False(result.IsPlaceholder);
        Assert.Equal("Pixabay", result.SourceProvider);
        Assert.Contains("Pexels", result.ProviderErrors.Keys);
    }

    [Fact]
    public async Task UnifiedStockProvider_Should_Use_Placeholder_When_All_Providers_Fail()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, returnsResults: false);
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: false);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object, mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" },
            enablePlaceholder: true);

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 3);

        // Assert
        Assert.True(result.IsPlaceholder);
        Assert.Equal("Placeholder", result.SourceProvider);
        Assert.Equal(3, result.Assets.Count);
    }

    [Fact]
    public async Task UnifiedStockProvider_Should_Return_Empty_When_Placeholder_Disabled_And_All_Fail()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, returnsResults: false);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object },
            fallbackOrder: new[] { "Pexels" },
            enablePlaceholder: false);

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 3);

        // Assert
        Assert.False(result.IsPlaceholder);
        Assert.Equal("None", result.SourceProvider);
        Assert.Empty(result.Assets);
    }

    [Fact]
    public async Task UnifiedStockProvider_Should_Skip_Unavailable_Providers()
    {
        // Arrange
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: true);

        // Note: Only Pixabay is available, but we configure Pexels first in fallback order
        var service = CreateService(
            enhancedProviders: new[] { mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" });

        // Act
        var result = await service.SearchWithFallbackAsync("test query", 5);

        // Assert
        Assert.False(result.IsPlaceholder);
        Assert.Equal("Pixabay", result.SourceProvider);
    }

    #endregion

    #region GetFallbackOrder Tests

    [Fact]
    public void UnifiedStockProvider_Should_Return_Correct_Fallback_Order()
    {
        // Arrange
        var service = CreateService(
            fallbackOrder: new[] { "Pexels", "Pixabay", "Unsplash" },
            enablePlaceholder: true);

        // Act
        var order = service.GetFallbackOrder();

        // Assert
        Assert.Equal(4, order.Count);
        Assert.Equal("Pexels", order[0]);
        Assert.Equal("Pixabay", order[1]);
        Assert.Equal("Unsplash", order[2]);
        Assert.Equal("Placeholder", order[3]);
    }

    [Fact]
    public void UnifiedStockProvider_Should_Exclude_Placeholder_When_Disabled()
    {
        // Arrange
        var service = CreateService(
            fallbackOrder: new[] { "Pexels", "Pixabay" },
            enablePlaceholder: false);

        // Act
        var order = service.GetFallbackOrder();

        // Assert
        Assert.Equal(2, order.Count);
        Assert.DoesNotContain("Placeholder", order);
    }

    #endregion

    #region GetProviderStatusAsync Tests

    [Fact]
    public async Task UnifiedStockProvider_Should_Return_Status_For_All_Providers()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, returnsResults: true, isValid: true);
        var mockPixabay = CreateMockEnhancedProvider(StockMediaProvider.Pixabay, returnsResults: true, isValid: false);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object, mockPixabay.Object },
            fallbackOrder: new[] { "Pexels", "Pixabay" },
            enablePlaceholder: true);

        // Act
        var status = await service.GetProviderStatusAsync();

        // Assert
        Assert.True(status["Pexels"].IsAvailable);
        Assert.True(status["Pexels"].IsConfigured);
        Assert.False(status["Pixabay"].IsAvailable);
        Assert.True(status["Pixabay"].IsConfigured);
        Assert.True(status["Placeholder"].IsAvailable);
    }

    #endregion

    #region IsAvailableAsync Tests

    [Fact]
    public async Task UnifiedStockProvider_IsAvailable_Should_Return_True_When_Any_Provider_Valid()
    {
        // Arrange
        var mockPexels = CreateMockEnhancedProvider(StockMediaProvider.Pexels, isValid: true);

        var service = CreateService(
            enhancedProviders: new[] { mockPexels.Object },
            fallbackOrder: new[] { "Pexels" },
            enablePlaceholder: false);

        // Act
        var isAvailable = await service.IsAvailableAsync();

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task UnifiedStockProvider_IsAvailable_Should_Return_True_When_Placeholder_Enabled()
    {
        // Arrange - No real providers, but placeholder is enabled
        var service = CreateService(
            enhancedProviders: Array.Empty<IEnhancedStockProvider>(),
            fallbackOrder: new[] { "Pexels" },
            enablePlaceholder: true);

        // Act
        var isAvailable = await service.IsAvailableAsync();

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task UnifiedStockProvider_IsAvailable_Should_Return_False_When_No_Providers_And_Placeholder_Disabled()
    {
        // Arrange
        var service = CreateService(
            enhancedProviders: Array.Empty<IEnhancedStockProvider>(),
            fallbackOrder: new[] { "Pexels" },
            enablePlaceholder: false);

        // Act
        var isAvailable = await service.IsAvailableAsync();

        // Assert
        Assert.False(isAvailable);
    }

    #endregion

    #region Helper Methods

    private static UnifiedStockProviderService CreateService(
        IEnumerable<IEnhancedStockProvider>? enhancedProviders = null,
        IEnumerable<IStockProvider>? basicProviders = null,
        string[]? fallbackOrder = null,
        bool enablePlaceholder = true)
    {
        var options = new UnifiedStockMediaOptions
        {
            FallbackOrder = fallbackOrder?.ToList() ?? new List<string> { "Pexels", "Pixabay", "Unsplash" },
            EnablePlaceholderFallback = enablePlaceholder
        };

        var placeholderGenerator = new PlaceholderColorGenerator(
            NullLogger<PlaceholderColorGenerator>.Instance,
            System.IO.Path.GetTempPath());

        return new UnifiedStockProviderService(
            NullLogger<UnifiedStockProviderService>.Instance,
            enhancedProviders ?? Array.Empty<IEnhancedStockProvider>(),
            basicProviders ?? Array.Empty<IStockProvider>(),
            placeholderGenerator,
            Options.Create(options));
    }

    private static Mock<IEnhancedStockProvider> CreateMockEnhancedProvider(
        StockMediaProvider provider,
        bool returnsResults = false,
        bool throws = false,
        bool isValid = true)
    {
        var mock = new Mock<IEnhancedStockProvider>();
        
        mock.Setup(p => p.ProviderName).Returns(provider);
        mock.Setup(p => p.SupportsVideo).Returns(provider == StockMediaProvider.Pexels || provider == StockMediaProvider.Pixabay);
        mock.Setup(p => p.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isValid);
        mock.Setup(p => p.GetRateLimitStatus()).Returns(new RateLimitStatus
        {
            Provider = provider,
            RequestsRemaining = 100,
            RequestsLimit = 200,
            IsLimited = false
        });

        if (throws)
        {
            mock.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("API error"));
        }
        else if (returnsResults)
        {
            mock.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StockMediaResult>
                {
                    new StockMediaResult
                    {
                        Id = "test-1",
                        Type = StockMediaType.Image,
                        Provider = provider,
                        ThumbnailUrl = "https://example.com/thumb.jpg",
                        PreviewUrl = "https://example.com/preview.jpg",
                        FullSizeUrl = "https://example.com/full.jpg",
                        Width = 1920,
                        Height = 1080,
                        Licensing = new AssetLicensingInfo
                        {
                            LicenseType = "Test License",
                            Attribution = "Test Attribution",
                            CommercialUseAllowed = true
                        }
                    }
                });
        }
        else
        {
            mock.Setup(p => p.SearchAsync(It.IsAny<StockMediaSearchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<StockMediaResult>());
        }

        return mock;
    }

    #endregion
}
