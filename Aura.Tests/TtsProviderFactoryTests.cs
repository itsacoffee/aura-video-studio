using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Aura.Providers.Tts;
using Aura.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for TtsProviderFactory to ensure proper DI resolution and fallback behavior
/// </summary>
public class TtsProviderFactoryTests
{
    /// <summary>
    /// Helper method to create a service collection with common registrations
    /// </summary>
    private ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        
        // Register common loggers
        services.AddSingleton<ILogger<TtsProviderFactory>>(NullLogger<TtsProviderFactory>.Instance);
        services.AddSingleton<ILogger<NullTtsProvider>>(NullLogger<NullTtsProvider>.Instance);
        services.AddSingleton<ILogger<WindowsTtsProvider>>(NullLogger<WindowsTtsProvider>.Instance);
        services.AddSingleton<ILogger<MockTtsProvider>>(NullLogger<MockTtsProvider>.Instance);
        services.AddSingleton<ILogger<ProviderSettings>>(NullLogger<ProviderSettings>.Instance);
        services.AddSingleton<ILogger<Aura.Core.Audio.WavValidator>>(NullLogger<Aura.Core.Audio.WavValidator>.Instance);
        services.AddSingleton<ILogger<Aura.Core.Audio.SilentWavGenerator>>(NullLogger<Aura.Core.Audio.SilentWavGenerator>.Instance);
        
        // Register ProviderSettings
        services.AddSingleton<ProviderSettings>();
        
        // Register Audio services required by TTS providers
        services.AddSingleton<Aura.Core.Audio.WavValidator>();
        services.AddSingleton<Aura.Core.Audio.SilentWavGenerator>();
        
        // Register factory
        services.AddSingleton<TtsProviderFactory>();
        
        return services;
    }
    [Fact]
    public void Factory_Should_ResolveNullProviderWhenNoOthersRegistered()
    {
        // Arrange - register only NullTtsProvider
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var providers = factory.CreateAvailableProviders();
        
        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains("Null", providers.Keys);
        Assert.IsType<NullTtsProvider>(providers["Null"]);
    }
    
    [Fact]
    public void Factory_Should_ReturnNullProviderAsDefaultWhenNoOthersAvailable()
    {
        // Arrange - register only NullTtsProvider
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var defaultProvider = factory.GetDefaultProvider();
        
        // Assert
        Assert.NotNull(defaultProvider);
        Assert.IsType<NullTtsProvider>(defaultProvider);
    }
    
    [Fact]
    public void Factory_Should_ResolveWindowsProviderWhenRegistered()
    {
        // Arrange - register NullTtsProvider and WindowsTtsProvider
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        
        // Only register Windows provider if on Windows platform
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ITtsProvider, WindowsTtsProvider>();
        }
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var providers = factory.CreateAvailableProviders();
        
        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains("Null", providers.Keys);
        
        if (OperatingSystem.IsWindows())
        {
            // Windows provider should be available on Windows
            Assert.Contains("Windows", providers.Keys);
            Assert.IsType<WindowsTtsProvider>(providers["Windows"]);
        }
    }
    
    [Fact]
    public void Factory_Should_PreferWindowsOverNullForDefault()
    {
        // Skip this test if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        
        // Arrange - register NullTtsProvider and WindowsTtsProvider
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        services.AddSingleton<ITtsProvider, WindowsTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var defaultProvider = factory.GetDefaultProvider();
        
        // Assert
        Assert.NotNull(defaultProvider);
        Assert.IsType<WindowsTtsProvider>(defaultProvider);
    }
    
    [Fact]
    public void Factory_Should_EnumerateMultipleProviders()
    {
        // Arrange - register multiple providers
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        services.AddSingleton<ITtsProvider, MockTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var providers = factory.CreateAvailableProviders();
        
        // Assert
        Assert.NotEmpty(providers);
        Assert.True(providers.Count >= 2);
        Assert.Contains("Null", providers.Keys);
        Assert.Contains("Mock", providers.Keys);
    }
    
    [Fact]
    public void Factory_Should_NeverThrowWhenCreatingProviders()
    {
        // Arrange - register only factory, no providers
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act & Assert - should not throw
        var exception = Record.Exception(() => factory.CreateAvailableProviders());
        Assert.Null(exception);
        
        var providers = factory.CreateAvailableProviders();
        Assert.NotNull(providers);
    }
    
    [Fact]
    public void Factory_Should_NeverThrowWhenGettingDefaultProvider()
    {
        // Arrange - register factory with minimal setup
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act & Assert - should not throw
        var exception = Record.Exception(() => factory.GetDefaultProvider());
        Assert.Null(exception);
        
        var provider = factory.GetDefaultProvider();
        Assert.NotNull(provider);
    }
    
    [Fact]
    public void Factory_Should_MapProviderTypesToFriendlyNames()
    {
        // Arrange - register providers with type names ending in "TtsProvider"
        var services = CreateServiceCollection();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        services.AddSingleton<ITtsProvider, MockTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var providers = factory.CreateAvailableProviders();
        
        // Assert - names should not include "TtsProvider" suffix
        Assert.Contains("Null", providers.Keys);
        Assert.Contains("Mock", providers.Keys);
        Assert.DoesNotContain("NullTtsProvider", providers.Keys);
        Assert.DoesNotContain("MockTtsProvider", providers.Keys);
    }
    
    [Fact]
    public void Factory_Should_CreateNullProviderWhenNoProvidersRegistered()
    {
        // Arrange - register factory but NO TTS providers at all (edge case)
        var services = CreateServiceCollection();
        // Intentionally not registering any ITtsProvider
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var defaultProvider = factory.GetDefaultProvider();
        
        // Assert - should create a NullTtsProvider as emergency fallback
        Assert.NotNull(defaultProvider);
        Assert.Equal("NullTtsProvider", defaultProvider.GetType().Name);
    }
    
    [Fact]
    public void Factory_Should_ReturnEmptyDictionaryWhenNoProvidersRegistered()
    {
        // Arrange - register factory but NO TTS providers at all
        var services = CreateServiceCollection();
        // Intentionally not registering any ITtsProvider
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        
        // Act
        var providers = factory.CreateAvailableProviders();
        
        // Assert - should return empty dictionary, not throw
        Assert.NotNull(providers);
        Assert.Empty(providers);
    }
}
