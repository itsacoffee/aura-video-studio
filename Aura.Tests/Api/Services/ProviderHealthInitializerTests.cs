using System;
using System.Linq;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Api.Services;

public class ProviderHealthInitializerTests
{
    [Fact]
    public void RegisterAllProviders_WithLlmProviders_RegistersWithHealthMonitor()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add required services
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ProviderSettings>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ProviderSettings>>();
            return new ProviderSettings(logger);
        });
        services.AddSingleton<IKeyStore, KeyStore>();
        services.AddHttpClient();
        
        // Add health monitor
        var circuitBreakerSettings = new CircuitBreakerSettings();
        services.AddSingleton(circuitBreakerSettings);
        services.AddSingleton<ProviderHealthMonitor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ProviderHealthMonitor>>();
            return new ProviderHealthMonitor(logger, circuitBreakerSettings);
        });
        
        // Add LlmProviderFactory
        services.AddSingleton<LlmProviderFactory>();
        
        // Register RuleBased provider (always available)
        services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Providers.Llm.RuleBasedLlmProvider>>();
            return new Aura.Providers.Llm.RuleBasedLlmProvider(logger);
        });
        
        // Add initializer
        services.AddSingleton<ProviderHealthInitializer>();
        
        var serviceProvider = services.BuildServiceProvider();
        var healthMonitor = serviceProvider.GetRequiredService<ProviderHealthMonitor>();
        var initializer = serviceProvider.GetRequiredService<ProviderHealthInitializer>();
        
        // Verify no providers registered initially
        var initialMetrics = healthMonitor.GetAllProviderHealth();
        Assert.Empty(initialMetrics);
        
        // Act
        initializer.RegisterAllProviders();
        
        // Assert
        var metrics = healthMonitor.GetAllProviderHealth();
        Assert.NotEmpty(metrics);
        
        // At minimum, RuleBased should be registered
        Assert.Contains("RuleBased", metrics.Keys);
        
        var ruleBasedMetrics = metrics["RuleBased"];
        Assert.Equal("RuleBased", ruleBasedMetrics.ProviderName);
    }
    
    [Fact]
    public void RegisterAllProviders_DoesNotThrow_WhenNoProvidersAvailable()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add minimal required services
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        
        // Add health monitor
        var circuitBreakerSettings = new CircuitBreakerSettings();
        services.AddSingleton(circuitBreakerSettings);
        services.AddSingleton<ProviderHealthMonitor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ProviderHealthMonitor>>();
            return new ProviderHealthMonitor(logger, circuitBreakerSettings);
        });
        
        // Add initializer without any providers
        services.AddSingleton<ProviderHealthInitializer>();
        
        var serviceProvider = services.BuildServiceProvider();
        var initializer = serviceProvider.GetRequiredService<ProviderHealthInitializer>();
        
        // Act & Assert - should not throw
        var exception = Record.Exception(() => initializer.RegisterAllProviders());
        Assert.Null(exception);
    }
}
