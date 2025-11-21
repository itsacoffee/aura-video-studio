using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ImageProviderFactoryTests
{
    [Fact]
    public void GetDefaultStockProvider_WithPexelsApiKey_ReturnsPexelsProvider()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var mockProviderSettings = new Mock<ProviderSettings>(
            NullLogger<ProviderSettings>.Instance,
            null);
        mockProviderSettings.Setup(x => x.GetPexelsApiKey()).Returns("test-api-key-12345");
        
        var factory = new ImageProviderFactory(
            serviceProvider,
            NullLogger<ImageProviderFactory>.Instance,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            mockProviderSettings.Object);
        
        // Act
        var provider = factory.GetDefaultStockProvider();
        
        // Assert
        Assert.NotNull(provider);
        Assert.Equal("PexelsImageProvider", provider.GetType().Name);
    }

    [Fact]
    public void GetDefaultStockProvider_WithoutPexelsApiKey_ReturnsPlaceholderProvider()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var mockProviderSettings = new Mock<ProviderSettings>(
            NullLogger<ProviderSettings>.Instance,
            null);
        mockProviderSettings.Setup(x => x.GetPexelsApiKey()).Returns((string?)null);
        
        var factory = new ImageProviderFactory(
            serviceProvider,
            NullLogger<ImageProviderFactory>.Instance,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            mockProviderSettings.Object);
        
        // Act
        var provider = factory.GetDefaultStockProvider();
        
        // Assert
        Assert.NotNull(provider);
        Assert.Equal("PlaceholderImageProvider", provider.GetType().Name);
    }

    [Fact]
    public void GetDefaultStockProvider_WithEmptyPexelsApiKey_ReturnsPlaceholderProvider()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var mockProviderSettings = new Mock<ProviderSettings>(
            NullLogger<ProviderSettings>.Instance,
            null);
        mockProviderSettings.Setup(x => x.GetPexelsApiKey()).Returns(string.Empty);
        
        var factory = new ImageProviderFactory(
            serviceProvider,
            NullLogger<ImageProviderFactory>.Instance,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            mockProviderSettings.Object);
        
        // Act
        var provider = factory.GetDefaultStockProvider();
        
        // Assert
        Assert.NotNull(provider);
        Assert.Equal("PlaceholderImageProvider", provider.GetType().Name);
    }

    [Fact]
    public async Task PlaceholderProvider_GeneratesImagesWithoutApiKey()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        serviceCollection.AddLogging();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        var mockProviderSettings = new Mock<ProviderSettings>(
            NullLogger<ProviderSettings>.Instance,
            null);
        mockProviderSettings.Setup(x => x.GetPexelsApiKey()).Returns((string?)null);
        
        var factory = new ImageProviderFactory(
            serviceProvider,
            NullLogger<ImageProviderFactory>.Instance,
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            mockProviderSettings.Object);
        
        // Act
        var provider = factory.GetDefaultStockProvider();
        var results = await provider.SearchAsync("test scene", 3, CancellationToken.None);
        
        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.All(results, asset =>
        {
            Assert.Equal("image", asset.Kind);
            Assert.Equal("Generated", asset.License);
            Assert.Contains("Aura Placeholder", asset.Attribution);
        });
    }
}
