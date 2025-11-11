using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Security;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for LLM provider implementations on Windows
/// Tests credential storage, network connectivity, and provider-specific features
/// </summary>
[Collection("Windows Integration Tests")]
public class WindowsLlmProviderIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<WindowsLlmProviderIntegrationTests> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HttpClient _httpClient;
    private readonly WindowsCredentialManager? _credentialManager;

    public WindowsLlmProviderIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        _logger = _loggerFactory.CreateLogger<WindowsLlmProviderIntegrationTests>();
        _httpClient = new HttpClient();
        
        // Initialize Windows Credential Manager only on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var credLogger = _loggerFactory.CreateLogger<WindowsCredentialManager>();
            _credentialManager = new WindowsCredentialManager(credLogger);
        }
    }

    [SkippableFact(Skip = "Manual test - requires Windows environment")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    public void WindowsCredentialManager_ShouldStoreAndRetrieveApiKeys()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), 
            "This test requires Windows OS");
        
        Assert.NotNull(_credentialManager);
        
        // Arrange
        const string testProvider = "TestProvider";
        const string testApiKey = "test-api-key-12345";
        
        try
        {
            // Act - Store API key
            var storeResult = _credentialManager.StoreApiKey(testProvider, testApiKey);
            Assert.True(storeResult, "Failed to store API key in Windows Credential Manager");
            
            // Act - Retrieve API key
            var retrievedKey = _credentialManager.RetrieveApiKey(testProvider);
            
            // Assert
            Assert.NotNull(retrievedKey);
            Assert.Equal(testApiKey, retrievedKey);
            
            // Act - Check if key exists
            var exists = _credentialManager.HasApiKey(testProvider);
            Assert.True(exists, "API key should exist in Credential Manager");
            
            _output.WriteLine($"✓ Successfully stored and retrieved API key for {testProvider}");
        }
        finally
        {
            // Cleanup
            _credentialManager.DeleteApiKey(testProvider);
        }
    }

    [SkippableFact(Skip = "Manual test - requires Windows and Ollama installation")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    [Trait("Category", "Ollama")]
    public async Task OllamaDetection_ShouldDetectLocalInstallation()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange
        var logger = _loggerFactory.CreateLogger<Core.Services.Providers.OllamaDetectionService>();
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        
        var detectionService = new Core.Services.Providers.OllamaDetectionService(
            logger,
            _httpClient,
            memoryCache);
        
        // Act
        var status = await detectionService.DetectOllamaAsync(CancellationToken.None);
        
        // Assert - Log results (may or may not be installed)
        _output.WriteLine($"Ollama Detection Results:");
        _output.WriteLine($"  IsRunning: {status.IsRunning}");
        _output.WriteLine($"  IsInstalled: {status.IsInstalled}");
        _output.WriteLine($"  Version: {status.Version ?? "N/A"}");
        _output.WriteLine($"  BaseUrl: {status.BaseUrl}");
        _output.WriteLine($"  Error: {status.ErrorMessage ?? "None"}");
        
        if (status.IsRunning)
        {
            // Test model detection
            var models = await detectionService.ListModelsAsync(CancellationToken.None);
            _output.WriteLine($"\n  Available Models: {models.Count}");
            foreach (var model in models)
            {
                _output.WriteLine($"    - {model.Name} ({FormatBytes(model.Size)})");
            }
            
            Assert.NotEmpty(models);
        }
        else
        {
            _output.WriteLine("\n⚠ Ollama is not running. Start Ollama with 'ollama serve' to test model detection.");
        }
    }

    [SkippableFact(Skip = "Manual test - requires OpenAI API key")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    [Trait("Category", "OpenAI")]
    public async Task OpenAI_ShouldHandleNetworkRequestsOnWindows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange - Get API key from environment or skip
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "OPENAI_API_KEY environment variable not set");
        
        var logger = _loggerFactory.CreateLogger<OpenAiLlmProvider>();
        var provider = new OpenAiLlmProvider(logger, _httpClient, apiKey!);
        
        // Act - Validate API key (tests network connectivity)
        var validation = await provider.ValidateApiKeyAsync(CancellationToken.None);
        
        // Assert
        _output.WriteLine($"OpenAI API Key Validation:");
        _output.WriteLine($"  IsValid: {validation.IsValid}");
        _output.WriteLine($"  Message: {validation.Message}");
        _output.WriteLine($"  Available Models: {validation.AvailableModels.Count}");
        
        Assert.True(validation.IsValid, $"API key validation failed: {validation.Message}");
        Assert.NotEmpty(validation.AvailableModels);
        
        foreach (var model in validation.AvailableModels.Take(5))
        {
            _output.WriteLine($"    - {model.Name}");
        }
    }

    [SkippableFact(Skip = "Manual test - requires Anthropic API key")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    [Trait("Category", "Anthropic")]
    public async Task Anthropic_ShouldHandleNetworkRequestsOnWindows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange - Get API key from environment or skip
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "ANTHROPIC_API_KEY environment variable not set");
        
        var logger = _loggerFactory.CreateLogger<AnthropicLlmProvider>();
        var provider = new AnthropicLlmProvider(logger, _httpClient, apiKey!);
        
        // Act - Test simple completion (tests network connectivity)
        try
        {
            var result = await provider.CompleteAsync("Say 'Hello'", CancellationToken.None);
            
            // Assert
            _output.WriteLine($"Anthropic API Test:");
            _output.WriteLine($"  Response: {result}");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        catch (InvalidOperationException ex)
        {
            _output.WriteLine($"Anthropic API Error: {ex.Message}");
            throw;
        }
    }

    [SkippableFact(Skip = "Manual test - requires Gemini API key")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    [Trait("Category", "Gemini")]
    public async Task Gemini_ShouldHandleNetworkRequestsOnWindows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange - Get API key from environment or skip
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        Skip.If(string.IsNullOrEmpty(apiKey), "GEMINI_API_KEY environment variable not set");
        
        var logger = _loggerFactory.CreateLogger<GeminiLlmProvider>();
        var provider = new GeminiLlmProvider(logger, _httpClient, apiKey!);
        
        // Act - Test simple completion (tests network connectivity)
        try
        {
            var result = await provider.CompleteAsync("Say 'Hello'", CancellationToken.None);
            
            // Assert
            _output.WriteLine($"Gemini API Test:");
            _output.WriteLine($"  Response: {result}");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        catch (InvalidOperationException ex)
        {
            _output.WriteLine($"Gemini API Error: {ex.Message}");
            throw;
        }
    }

    [SkippableFact(Skip = "Manual test - requires network connectivity")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    public async Task NetworkFailure_ShouldHandleGracefully()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange - Use invalid API key to simulate auth failure
        var logger = _loggerFactory.CreateLogger<OpenAiLlmProvider>();
        var provider = new OpenAiLlmProvider(logger, _httpClient, "sk-invalid-key-for-testing-12345678901234567890");
        
        // Act & Assert - Should handle error gracefully
        var validation = await provider.ValidateApiKeyAsync(CancellationToken.None);
        
        _output.WriteLine($"Network Error Handling Test:");
        _output.WriteLine($"  IsValid: {validation.IsValid}");
        _output.WriteLine($"  Message: {validation.Message}");
        
        Assert.False(validation.IsValid);
        Assert.Contains("Invalid", validation.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact(Skip = "Manual test - requires timeout simulation")]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    public async Task Timeout_ShouldHandleGracefully()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange - Create provider with very short timeout
        var logger = _loggerFactory.CreateLogger<OllamaLlmProvider>();
        var provider = new OllamaLlmProvider(
            logger, 
            _httpClient, 
            "http://localhost:11434",
            "llama3.1:8b",
            maxRetries: 0,
            timeoutSeconds: 1); // 1 second timeout
        
        // Act - Try to check if service is available
        var isAvailable = await provider.IsServiceAvailableAsync(CancellationToken.None);
        
        // Assert - Should handle timeout gracefully (not crash)
        _output.WriteLine($"Timeout Handling Test:");
        _output.WriteLine($"  Service Available: {isAvailable}");
        _output.WriteLine($"  ✓ Timeout handled gracefully without exception");
    }

    [SkippableFact]
    [Trait("Category", "Windows")]
    [Trait("Category", "Integration")]
    public void WindowsHttpClient_ShouldUseSystemProxy()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            "This test requires Windows OS");
        
        // Arrange & Act
        using var handler = new HttpClientHandler();
        
        // Assert - Verify Windows HTTP stack configuration
        _output.WriteLine($"HttpClient Windows Configuration:");
        _output.WriteLine($"  UseProxy: {handler.UseProxy}");
        _output.WriteLine($"  UseDefaultCredentials: {handler.UseDefaultCredentials}");
        _output.WriteLine($"  Supports Automatic Decompression: {handler.SupportsAutomaticDecompression}");
        
        // Windows HttpClient should respect system proxy settings by default
        Assert.True(handler.UseProxy, "HttpClient should use system proxy on Windows");
        
        _output.WriteLine($"  ✓ HttpClient configured to use Windows system proxy");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _loggerFactory?.Dispose();
    }
}
