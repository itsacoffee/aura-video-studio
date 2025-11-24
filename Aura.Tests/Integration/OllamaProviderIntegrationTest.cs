using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Services.AI;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration test for OllamaLlmProvider to diagnose issues with Ollama integration
/// </summary>
public class OllamaProviderIntegrationTest : IDisposable
{
    private readonly ILogger<OllamaLlmProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;

    public OllamaProviderIntegrationTest(ITestOutputHelper output)
    {
        _output = output;
        
        // Create logger that writes to test output
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new XunitLoggerProvider(output));
        });
        
        _logger = loggerFactory.CreateLogger<OllamaLlmProvider>();
        
        // Create HttpClient with proper configuration
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(300)
        };
    }

    [Fact(Skip = "Manual test - requires Ollama to be running")]
    public async Task TestOllamaServiceAvailability()
    {
        // Arrange
        var baseUrl = "http://127.0.0.1:11434";
        var model = "llama3.1:8b-q4_k_m";
        var provider = new OllamaLlmProvider(_logger, _httpClient, baseUrl, model);

        // Act
        var isAvailable = await provider.IsServiceAvailableAsync(CancellationToken.None);

        // Assert
        _output.WriteLine($"Ollama service available: {isAvailable}");
        Assert.True(isAvailable, "Ollama should be available at http://127.0.0.1:11434");
    }

    [Fact(Skip = "Manual test - requires Ollama to be running")]
    public async Task TestOllamaScriptGeneration()
    {
        // Arrange
        var baseUrl = "http://127.0.0.1:11434";
        var model = "llama3.1:8b-q4_k_m";
        var provider = new OllamaLlmProvider(_logger, _httpClient, baseUrl, model);

        var brief = new Brief(
            Topic: "Introduction to AI",
            Audience: "Students",
            Goal: "Educate",
            Tone: "Friendly",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            RagConfiguration: null,
            LlmParameters: null,
            PromptModifiers: null);

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Medium,
            Density: Density.Medium,
            Style: "Educational");

        // Act & Assert
        _output.WriteLine("Testing Ollama script generation...");
        
        try
        {
            var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);
            
            _output.WriteLine($"Script generated successfully:");
            _output.WriteLine($"Length: {script.Length} characters");
            _output.WriteLine($"Content preview: {script.Substring(0, Math.Min(200, script.Length))}...");
            
            Assert.NotNull(script);
            Assert.NotEmpty(script);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Script generation FAILED:");
            _output.WriteLine($"Exception type: {ex.GetType().Name}");
            _output.WriteLine($"Message: {ex.Message}");
            _output.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                _output.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Xunit logger provider for writing to test output
/// </summary>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, categoryName);
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// Xunit logger implementation
/// </summary>
public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = $"[{logLevel}] {_categoryName}: {formatter(state, exception)}";
            if (exception != null)
            {
                message += $"\n{exception}";
            }
            _output.WriteLine(message);
        }
        catch
        {
            // Ignore exceptions from test output (happens if test is already completed)
        }
    }

    private class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
