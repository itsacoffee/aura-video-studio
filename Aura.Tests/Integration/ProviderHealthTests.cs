using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aura.Core.Providers;
using Aura.Core.Services.Health;
using Aura.Providers.Llm;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for provider health monitoring registration
/// Validates that providers are correctly registered with ProviderHealthMonitor
/// </summary>
public class ProviderHealthTests
{
    [Fact]
    public void ProviderHealthMonitor_CanBeCreated_Successfully()
    {
        // Arrange - create minimal service collection with provider health services
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Register a basic LLM provider
        services.AddSingleton<ILlmProvider, RuleBasedLlmProvider>();
        
        // Register provider health monitor
        services.AddSingleton<ProviderHealthMonitor>();
        
        var sp = services.BuildServiceProvider();
        
        // Act
        var monitor = sp.GetRequiredService<ProviderHealthMonitor>();
        
        // Assert
        Assert.NotNull(monitor);
    }
    
    [Fact]
    public void ProviderHealthMonitor_GetAllProviderHealth_ReturnsNonNull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ProviderHealthMonitor>();
        
        var sp = services.BuildServiceProvider();
        var monitor = sp.GetRequiredService<ProviderHealthMonitor>();
        
        // Act
        var providers = monitor.GetAllProviderHealth();
        
        // Assert
        Assert.NotNull(providers);
    }
    
    [Fact]
    public void ProviderHealthMonitor_RegisterProvider_CanBeQueried()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ProviderHealthMonitor>();
        
        var sp = services.BuildServiceProvider();
        var monitor = sp.GetRequiredService<ProviderHealthMonitor>();
        
        // Act - Register a test provider
        monitor.RegisterHealthCheck("TestProvider", async ct => await Task.FromResult(true));
        var providers = monitor.GetAllProviderHealth();
        
        // Assert
        Assert.True(providers.ContainsKey("TestProvider"), 
            "Provider should be registered in health monitor");
    }
}
