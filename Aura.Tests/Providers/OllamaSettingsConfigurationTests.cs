using System;
using Aura.Core.Providers;
using Aura.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Tests to verify OllamaSettings configuration via ProviderSettings service using 
/// dependency injection factory pattern with proper default values
/// </summary>
public class OllamaSettingsConfigurationTests
{
    /// <summary>
    /// Creates a service collection with OllamaSettings configured via ProviderSettings factory
    /// </summary>
    private static ServiceCollection CreateServiceCollectionWithOllamaSettings()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Add HTTP client factory
        services.AddHttpClient();
        
        // Add memory cache (required for some services)
        services.AddMemoryCache();
        
        // Register ProviderSettings (same as in Program.cs)
        services.AddSingleton<ProviderSettings>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ProviderSettings>>();
            return new ProviderSettings(logger, null);
        });
        
        // Register OllamaSettings configuration using factory (this is what we're testing)
        services.AddSingleton<IConfigureOptions<OllamaSettings>>(sp =>
        {
            return new ConfigureNamedOptions<OllamaSettings>(
                Options.DefaultName, 
                options =>
                {
                    var providerSettings = sp.GetRequiredService<ProviderSettings>();
                    options.BaseUrl = providerSettings.GetOllamaUrl();
                    options.Timeout = TimeSpan.FromMinutes(3);
                    options.MaxRetries = 3;
                    options.GpuEnabled = providerSettings.GetOllamaGpuEnabled();
                    options.NumGpu = providerSettings.GetOllamaNumGpu();
                    options.NumCtx = providerSettings.GetOllamaNumCtx();
                });
        });
        
        return services;
    }

    [Fact]
    public void OllamaSettings_ConfiguresFromProviderSettings_WithCorrectDefaults()
    {
        // Arrange - Build a minimal service collection that mimics Program.cs
        var services = CreateServiceCollectionWithOllamaSettings();
        
        // Register OllamaDirectClient with HttpClient
        services.AddHttpClient<IOllamaDirectClient, OllamaDirectClient>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
        });
        
        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve OllamaDirectClient (which requires OllamaSettings)
        var ollamaClient = serviceProvider.GetService<IOllamaDirectClient>();
        
        // Also verify OllamaSettings directly
        var ollamaSettings = serviceProvider.GetRequiredService<IOptions<OllamaSettings>>().Value;

        // Assert
        Assert.NotNull(ollamaClient);
        Assert.NotNull(ollamaSettings);
        
        // Verify settings have correct defaults from ProviderSettings
        Assert.Equal("http://127.0.0.1:11434", ollamaSettings.BaseUrl);
        Assert.Equal(TimeSpan.FromMinutes(3), ollamaSettings.Timeout);
        Assert.Equal(3, ollamaSettings.MaxRetries);
        Assert.True(ollamaSettings.GpuEnabled); // Default is true
        Assert.Equal(-1, ollamaSettings.NumGpu); // Default is -1 (all GPUs)
        Assert.Equal(4096, ollamaSettings.NumCtx); // Default context size
    }
    
    [Fact]
    public void OllamaDirectClient_CanBeResolved_WithConfiguredSettings()
    {
        // Arrange - Build service collection
        var services = CreateServiceCollectionWithOllamaSettings();
        
        // Register OllamaDirectClient
        services.AddHttpClient<IOllamaDirectClient, OllamaDirectClient>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve OllamaDirectClient
        var exception = Record.Exception(() =>
        {
            var client = serviceProvider.GetRequiredService<IOllamaDirectClient>();
            Assert.NotNull(client);
        });

        // Assert - Should not throw any exceptions
        Assert.Null(exception);
    }
}
