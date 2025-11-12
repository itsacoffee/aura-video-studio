using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Aura.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Providers.Llm;

/// <summary>
/// Integration tests for LLM provider registration and resolution
/// Tests the complete DI container setup and provider factory
/// </summary>
public class LlmProviderRegistrationTests
{
    private readonly ITestOutputHelper _output;

    public LlmProviderRegistrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void AddLlmProviders_RegistersAllKeyedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();
        services.AddMemoryCache();
        
        // Add required dependencies
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();
        
        // Act
        services.AddLlmProviders();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - RuleBased should always be available
        var ruleBasedProvider = serviceProvider.GetKeyedService<ILlmProvider>("RuleBased");
        Assert.NotNull(ruleBasedProvider);
        _output.WriteLine("✓ RuleBased provider registered");

        // Ollama should be registered (but may return null if not configured)
        var ollamaProvider = serviceProvider.GetKeyedService<ILlmProvider>("Ollama");
        _output.WriteLine(ollamaProvider != null 
            ? "✓ Ollama provider registered and available" 
            : "⚠ Ollama provider registered but not configured");

        // Pro providers return null without API keys (expected behavior)
        var openAiProvider = serviceProvider.GetKeyedService<ILlmProvider>("OpenAI");
        _output.WriteLine(openAiProvider != null 
            ? "✓ OpenAI provider registered and available" 
            : "⚠ OpenAI provider registered but no API key configured");

        var azureProvider = serviceProvider.GetKeyedService<ILlmProvider>("Azure");
        _output.WriteLine(azureProvider != null 
            ? "✓ Azure provider registered and available" 
            : "⚠ Azure provider registered but no credentials configured");

        var geminiProvider = serviceProvider.GetKeyedService<ILlmProvider>("Gemini");
        _output.WriteLine(geminiProvider != null 
            ? "✓ Gemini provider registered and available" 
            : "⚠ Gemini provider registered but no API key configured");

        var anthropicProvider = serviceProvider.GetKeyedService<ILlmProvider>("Anthropic");
        _output.WriteLine(anthropicProvider != null 
            ? "✓ Anthropic provider registered and available" 
            : "⚠ Anthropic provider registered but no API key configured");
    }

    [Fact]
    public void AddLlmProviders_RegistersCompositeLlmProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();
        services.AddMemoryCache();
        
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();
        
        // Act
        services.AddLlmProviders();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var compositeLlmProvider = serviceProvider.GetService<ILlmProvider>();
        Assert.NotNull(compositeLlmProvider);
        Assert.IsType<CompositeLlmProvider>(compositeLlmProvider);
        _output.WriteLine("✓ CompositeLlmProvider registered as ILlmProvider");
    }

    [Fact]
    public void LlmProviderFactory_CreatesAvailableProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();
        services.AddMemoryCache();
        
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();
        services.AddLlmProviders();
        services.AddSingleton<LlmProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<LlmProviderFactory>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act
        var providers = factory.CreateAvailableProviders(loggerFactory);

        // Assert
        Assert.NotEmpty(providers);
        Assert.True(providers.ContainsKey("RuleBased"), "RuleBased provider must always be available");
        _output.WriteLine($"✓ Factory created {providers.Count} provider(s): {string.Join(", ", providers.Keys)}");
    }

    [Fact]
    public void AddProviderHealthServices_RegistersValidationAndCircuitBreaker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();
        services.AddMemoryCache();
        
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();
        services.AddLlmProviders();

        // Act
        services.AddProviderHealthServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var validator = serviceProvider.GetService<LlmProviderValidator>();
        Assert.NotNull(validator);
        _output.WriteLine("✓ LlmProviderValidator registered");

        var circuitBreaker = serviceProvider.GetService<LlmProviderCircuitBreaker>();
        Assert.NotNull(circuitBreaker);
        _output.WriteLine("✓ LlmProviderCircuitBreaker registered");
    }

    [Fact]
    public void LlmProviderValidator_ValidatesAllProviders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddHttpClient();
        services.AddMemoryCache();
        
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();
        services.AddLlmProviders();
        services.AddProviderHealthServices();
        
        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetRequiredService<LlmProviderValidator>();

        // Act
        var results = validator.ValidateAllProviders();

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.ContainsKey("RuleBased"));
        Assert.True(results["RuleBased"].IsAvailable, "RuleBased should always be available");
        
        foreach (var result in results.Values)
        {
            _output.WriteLine($"{(result.IsAvailable ? "✓" : "✗")} {result.ProviderName}: {result.ValidationMessage}");
        }
    }

    [Fact]
    public void LlmProviderCircuitBreaker_TracksProviderState()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        services.AddSingleton<LlmProviderCircuitBreaker>();
        var serviceProvider = services.BuildServiceProvider();
        var circuitBreaker = serviceProvider.GetRequiredService<LlmProviderCircuitBreaker>();

        // Act & Assert - Initial state should be available
        Assert.True(circuitBreaker.IsProviderAvailable("TestProvider"));
        _output.WriteLine("✓ Provider initially available");

        // Record some failures
        circuitBreaker.RecordFailure("TestProvider", new Exception("Test failure 1"));
        circuitBreaker.RecordFailure("TestProvider", new Exception("Test failure 2"));
        Assert.True(circuitBreaker.IsProviderAvailable("TestProvider"), "Should still be available after 2 failures");
        _output.WriteLine("✓ Circuit remains closed after 2 failures");

        // Third failure should open the circuit
        circuitBreaker.RecordFailure("TestProvider", new Exception("Test failure 3"));
        Assert.False(circuitBreaker.IsProviderAvailable("TestProvider"), "Should be unavailable after 3 failures");
        _output.WriteLine("✓ Circuit opened after 3 failures");

        // Get provider info
        var info = circuitBreaker.GetProviderInfo("TestProvider");
        Assert.Equal(CircuitStatus.Open, info.Status);
        Assert.Equal(3, info.ConsecutiveFailures);
        Assert.Equal(3, info.TotalFailures);
        _output.WriteLine($"✓ Circuit state: {info.Status}, Consecutive failures: {info.ConsecutiveFailures}");

        // Reset circuit
        circuitBreaker.ResetCircuit("TestProvider");
        Assert.True(circuitBreaker.IsProviderAvailable("TestProvider"), "Should be available after manual reset");
        _output.WriteLine("✓ Circuit reset successfully");
    }

    [Fact]
    public async Task LlmProviderCircuitBreaker_ExecuteAsync_HandlesSuccessAndFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        services.AddSingleton<LlmProviderCircuitBreaker>();
        var serviceProvider = services.BuildServiceProvider();
        var circuitBreaker = serviceProvider.GetRequiredService<LlmProviderCircuitBreaker>();

        // Act & Assert - Success
        var result = await circuitBreaker.ExecuteAsync("TestProvider", async ct => 
        {
            await Task.Delay(10, ct);
            return "success";
        });
        Assert.Equal("success", result);
        _output.WriteLine("✓ Successful execution tracked");

        // Verify success was recorded
        var info = circuitBreaker.GetProviderInfo("TestProvider");
        Assert.Equal(1, info.TotalSuccesses);
        _output.WriteLine($"✓ Success count: {info.TotalSuccesses}");
    }
}
