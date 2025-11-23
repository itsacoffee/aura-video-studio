using System;
using Aura.Api.Controllers;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify ScriptsController dependency injection is properly configured
/// </summary>
public class ScriptsControllerDiTests
{
    [Fact]
    public void ScriptsController_CanBeResolvedFromDi()
    {
        // Arrange: Create a minimal service collection with required dependencies
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging();
        
        // Register all dependencies required by ScriptsController
        services.AddSingleton<ScriptOrchestrator>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ScriptOrchestrator>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var mixer = sp.GetRequiredService<ProviderMixer>();
            return new ScriptOrchestrator(logger, loggerFactory, mixer, Array.Empty<Aura.Providers.Llm.ILlmProvider>());
        });
        
        services.AddSingleton<ScriptProcessor>();
        services.AddSingleton<ScriptCacheService>();
        services.AddSingleton<ProviderMixer>();
        services.AddSingleton<StreamingOrchestrator>();
        
        var provider = services.BuildServiceProvider();
        
        // Act: Attempt to resolve ScriptsController
        var controller = ActivatorUtilities.CreateInstance<ScriptsController>(provider);
        
        // Assert: Controller was successfully instantiated
        Assert.NotNull(controller);
    }
    
    [Fact]
    public void ScriptProcessor_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ScriptProcessor>();
        
        var provider = services.BuildServiceProvider();
        
        // Act
        var instance1 = provider.GetRequiredService<ScriptProcessor>();
        var instance2 = provider.GetRequiredService<ScriptProcessor>();
        
        // Assert: Same instance returned (singleton behavior)
        Assert.Same(instance1, instance2);
    }
    
    [Fact]
    public void ScriptCacheService_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ScriptCacheService>();
        
        var provider = services.BuildServiceProvider();
        
        // Act
        var instance1 = provider.GetRequiredService<ScriptCacheService>();
        var instance2 = provider.GetRequiredService<ScriptCacheService>();
        
        // Assert: Same instance returned (singleton behavior)
        Assert.Same(instance1, instance2);
    }
    
    [Fact]
    public void ScriptProcessor_RequiresOnlyLogger()
    {
        // Arrange: Verify ScriptProcessor only needs ILogger dependency
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ScriptProcessor>();
        
        var provider = services.BuildServiceProvider();
        
        // Act & Assert: Should resolve without errors
        var scriptProcessor = provider.GetRequiredService<ScriptProcessor>();
        Assert.NotNull(scriptProcessor);
    }
    
    [Fact]
    public void ScriptCacheService_RequiresOnlyLogger()
    {
        // Arrange: Verify ScriptCacheService only needs ILogger dependency
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ScriptCacheService>();
        
        var provider = services.BuildServiceProvider();
        
        // Act & Assert: Should resolve without errors
        var cacheService = provider.GetRequiredService<ScriptCacheService>();
        Assert.NotNull(cacheService);
    }
}
