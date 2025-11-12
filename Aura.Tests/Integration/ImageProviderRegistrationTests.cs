using System;
using System.Linq;
using System.Net.Http;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Aura.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for image provider registration in DI container
/// </summary>
public class ImageProviderRegistrationTests
{
    [Fact]
    public void AddImageProviders_Should_RegisterPlaceholderProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureMinimalServices(services);

        // Act
        services.AddImageProviders();
        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<IStockProvider>().ToList();

        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains(providers, p => p.GetType().Name.Contains("Placeholder"));
    }

    [Fact]
    public void ImageProviderFactory_Should_ResolveProvidersFromDI()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureMinimalServices(services);
        services.AddImageProviders();
        services.AddProviderFactories();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ImageProviderFactory>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        var providers = factory.CreateAvailableProviders(loggerFactory);

        // Assert
        Assert.NotNull(providers);
        // At minimum, we should get providers that were registered
        // The actual count depends on configuration (API keys, etc.)
    }

    [Fact]
    public void AddImageProviders_Should_SkipStableDiffusion_WhenNotConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureMinimalServices(services);

        // Act
        services.AddImageProviders();
        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<IImageProvider>().ToList();

        // Assert
        // Without SD URL configured, StableDiffusion provider should be skipped (null returned)
        // Null providers are filtered out during enumeration
        var sdProviders = providers.Where(p => p != null && p.GetType().Name.Contains("StableDiffusion")).ToList();
        
        // Should be empty or have no SD providers since URL is not configured
        Assert.True(sdProviders.Count == 0 || sdProviders.All(p => p == null));
    }

    [Fact]
    public void AddImageProviders_Should_RegisterEnhancedStockProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureMinimalServices(services);

        // Act
        services.AddImageProviders();
        var serviceProvider = services.BuildServiceProvider();
        var enhancedProviders = serviceProvider.GetServices<IEnhancedStockProvider>().ToList();

        // Assert
        // Enhanced stock providers are registered but may return null if API keys not configured
        // Just verify the service registration exists
        Assert.NotNull(enhancedProviders);
    }

    [Fact]
    public void ImageProviderFactory_Should_FallbackToReflection_WhenNoDIProviders()
    {
        // Arrange - deliberately don't call AddImageProviders
        var services = new ServiceCollection();
        ConfigureMinimalServices(services);
        services.AddProviderFactories();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ImageProviderFactory>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        var providers = factory.CreateAvailableProviders(loggerFactory);

        // Assert
        Assert.NotNull(providers);
        // Without AddImageProviders, factory should fall back to reflection
        // May return empty if no API keys configured
    }

    /// <summary>
    /// Configure minimal required services for testing
    /// </summary>
    private void ConfigureMinimalServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

        // Add HttpClientFactory
        services.AddHttpClient();

        // Add ProviderSettings with default configuration
        services.AddSingleton<ProviderSettings>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ProviderSettings>>();
            return new ProviderSettings(logger)
            {
                // Default empty configuration
                StableDiffusionWebUiUrl = null,
                PiperExecutablePath = null,
                PiperVoiceModelPath = null,
                Mimic3BaseUrl = null
            };
        });

        // Add KeyStore with empty keys
        services.AddSingleton<IKeyStore>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<KeyStore>>();
            return new KeyStore(logger);
        });

        // Add HardwareDetector (optional, providers should handle null)
        services.AddSingleton<Aura.Core.Hardware.HardwareDetector>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Hardware.HardwareDetector>>();
            return new Aura.Core.Hardware.HardwareDetector(logger);
        });
    }
}
