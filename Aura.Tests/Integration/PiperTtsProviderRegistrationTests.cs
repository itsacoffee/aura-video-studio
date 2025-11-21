using System;
using System.IO;
using System.Linq;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Aura.Providers;
using Aura.Providers.Tts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for Piper TTS provider registration with path validation.
/// Tests the fix for PR #3 - ensuring Piper is not registered when paths are invalid.
/// </summary>
public class PiperTtsProviderRegistrationTests
{
    private ServiceCollection ConfigureMinimalServices(ProviderSettings? customSettings = null)
    {
        var services = new ServiceCollection();
        
        // Register logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        
        // Register ProviderSettings
        services.AddSingleton(customSettings ?? new ProviderSettings());
        
        // Register audio services required by TTS providers
        services.AddSingleton<Aura.Core.Audio.WavValidator>();
        services.AddSingleton<Aura.Core.Audio.SilentWavGenerator>();
        
        // Register HttpClientFactory for TTS providers that need it
        services.AddHttpClient();
        
        return services;
    }

    [Fact]
    public void PiperProvider_ShouldNotBeRegistered_WhenPathsNotConfigured()
    {
        // Arrange - settings with null/empty paths
        var settings = new ProviderSettings
        {
            PiperExecutablePath = null,
            PiperVoiceModelPath = null
        };
        
        var services = ConfigureMinimalServices(settings);
        services.AddTtsProviders();
        
        // Act
        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ITtsProvider>().Where(p => p != null).ToList();
        
        // Assert - Piper should NOT be in the list
        Assert.DoesNotContain(providers, p => p.GetType() == typeof(PiperTtsProvider));
    }

    [Fact]
    public void PiperProvider_ShouldNotBeRegistered_WhenExecutableDoesNotExist()
    {
        // Arrange - settings with non-existent executable path
        var settings = new ProviderSettings
        {
            PiperExecutablePath = "/nonexistent/path/to/piper.exe",
            PiperVoiceModelPath = Path.GetTempFileName() // Valid file
        };
        
        var services = ConfigureMinimalServices(settings);
        services.AddTtsProviders();
        
        // Act
        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ITtsProvider>().Where(p => p != null).ToList();
        
        // Assert - Piper should NOT be in the list
        Assert.DoesNotContain(providers, p => p.GetType() == typeof(PiperTtsProvider));
        
        // Cleanup
        if (File.Exists(settings.PiperVoiceModelPath))
        {
            File.Delete(settings.PiperVoiceModelPath);
        }
    }

    [Fact]
    public void PiperProvider_ShouldNotBeRegistered_WhenModelDoesNotExist()
    {
        // Arrange - settings with non-existent model path
        var settings = new ProviderSettings
        {
            PiperExecutablePath = Path.GetTempFileName(), // Valid file
            PiperVoiceModelPath = "/nonexistent/path/to/model.onnx"
        };
        
        var services = ConfigureMinimalServices(settings);
        services.AddTtsProviders();
        
        // Act
        var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<ITtsProvider>().Where(p => p != null).ToList();
        
        // Assert - Piper should NOT be in the list
        Assert.DoesNotContain(providers, p => p.GetType() == typeof(PiperTtsProvider));
        
        // Cleanup
        if (File.Exists(settings.PiperExecutablePath))
        {
            File.Delete(settings.PiperExecutablePath);
        }
    }

    [Fact]
    public void PiperProvider_ShouldBeRegistered_WhenBothPathsExist()
    {
        // Arrange - create temporary files to simulate valid Piper installation
        var tempExe = Path.GetTempFileName();
        var tempModel = Path.GetTempFileName();
        
        try
        {
            var settings = new ProviderSettings
            {
                PiperExecutablePath = tempExe,
                PiperVoiceModelPath = tempModel
            };
            
            var services = ConfigureMinimalServices(settings);
            services.AddTtsProviders();
            
            // Act
            var serviceProvider = services.BuildServiceProvider();
            var providers = serviceProvider.GetServices<ITtsProvider>().Where(p => p != null).ToList();
            
            // Assert - Piper SHOULD be in the list
            Assert.Contains(providers, p => p.GetType() == typeof(PiperTtsProvider));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempExe))
            {
                File.Delete(tempExe);
            }
            if (File.Exists(tempModel))
            {
                File.Delete(tempModel);
            }
        }
    }

    [Fact]
    public void TtsProviderFactory_ShouldNotIncludePiper_WhenFilesDoNotExist()
    {
        // Arrange - settings with non-existent paths
        var settings = new ProviderSettings
        {
            PiperExecutablePath = "/nonexistent/piper.exe",
            PiperVoiceModelPath = "/nonexistent/model.onnx"
        };
        
        var services = ConfigureMinimalServices(settings);
        services.AddTtsProviders();
        services.AddSingleton<TtsProviderFactory>();
        
        // Act
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        var availableProviders = factory.CreateAvailableProviders();
        
        // Assert - Piper should NOT be in the available providers dictionary
        Assert.DoesNotContain("Piper", availableProviders.Keys);
    }

    [Fact]
    public void TtsProviderFactory_ShouldIncludePiper_WhenFilesExist()
    {
        // Arrange - create temporary files
        var tempExe = Path.GetTempFileName();
        var tempModel = Path.GetTempFileName();
        
        try
        {
            var settings = new ProviderSettings
            {
                PiperExecutablePath = tempExe,
                PiperVoiceModelPath = tempModel
            };
            
            var services = ConfigureMinimalServices(settings);
            services.AddTtsProviders();
            services.AddSingleton<TtsProviderFactory>();
            
            // Act
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
            var availableProviders = factory.CreateAvailableProviders();
            
            // Assert - Piper SHOULD be in the available providers dictionary
            Assert.Contains("Piper", availableProviders.Keys);
            Assert.IsType<PiperTtsProvider>(availableProviders["Piper"]);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempExe))
            {
                File.Delete(tempExe);
            }
            if (File.Exists(tempModel))
            {
                File.Delete(tempModel);
            }
        }
    }

    [Fact]
    public void FreshInstall_ShouldFallbackToWindowsOrNull_WhenPiperNotConfigured()
    {
        // Arrange - simulate fresh install with no Piper paths
        var settings = new ProviderSettings
        {
            PiperExecutablePath = null,
            PiperVoiceModelPath = null
        };
        
        var services = ConfigureMinimalServices(settings);
        services.AddTtsProviders();
        services.AddSingleton<TtsProviderFactory>();
        
        // Act
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<TtsProviderFactory>();
        var defaultProvider = factory.GetDefaultProvider();
        
        // Assert - should get Windows (on Windows) or Null provider, but NOT crash
        Assert.NotNull(defaultProvider);
        Assert.IsNotType<PiperTtsProvider>(defaultProvider);
        
        // On Windows, should get Windows TTS; on other platforms, should get Null
        if (OperatingSystem.IsWindows())
        {
            Assert.True(
                defaultProvider.GetType().Name == "WindowsTtsProvider" || 
                defaultProvider.GetType().Name == "NullTtsProvider",
                $"Expected Windows or Null provider on fresh install, got {defaultProvider.GetType().Name}");
        }
        else
        {
            Assert.Equal("NullTtsProvider", defaultProvider.GetType().Name);
        }
    }
}
