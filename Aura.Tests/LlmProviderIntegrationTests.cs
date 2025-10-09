using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for LLM providers
/// These test the actual provider implementations (without real API calls)
/// </summary>
public class LlmProviderIntegrationTests
{
    private readonly Brief _testBrief = new Brief(
        Topic: "Introduction to Machine Learning",
        Audience: "Beginners",
        Goal: "Educational",
        Tone: "Friendly",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(3),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    [Fact]
    public async Task RuleBasedProvider_Should_GenerateValidScript()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        // Act
        var script = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Machine Learning", script);
        Assert.Contains("##", script); // Should have scene markers
        Assert.Contains("Introduction", script);
        Assert.Contains("Conclusion", script);
    }

    [Fact]
    public async Task RuleBasedProvider_Should_GenerateDifferentScripts_ForDifferentTopics()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief1 = _testBrief with { Topic = "Topic A" };
        var brief2 = _testBrief with { Topic = "Topic B" };

        // Act
        var script1 = await provider.DraftScriptAsync(brief1, _testSpec, CancellationToken.None);
        var script2 = await provider.DraftScriptAsync(brief2, _testSpec, CancellationToken.None);

        // Assert
        Assert.Contains("Topic A", script1);
        Assert.Contains("Topic B", script2);
        Assert.DoesNotContain("Topic B", script1);
        Assert.DoesNotContain("Topic A", script2);
    }

    [Fact]
    public async Task RuleBasedProvider_Should_ScaleContent_ByDuration()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var shortSpec = _testSpec with { TargetDuration = TimeSpan.FromMinutes(1) };
        var longSpec = _testSpec with { TargetDuration = TimeSpan.FromMinutes(10) };

        // Act
        var shortScript = await provider.DraftScriptAsync(_testBrief, shortSpec, CancellationToken.None);
        var longScript = await provider.DraftScriptAsync(_testBrief, longSpec, CancellationToken.None);

        // Assert
        var shortWordCount = CountWords(shortScript);
        var longWordCount = CountWords(longScript);
        
        Assert.True(longWordCount > shortWordCount * 2, 
            $"Long script ({longWordCount} words) should be significantly longer than short script ({shortWordCount} words)");
    }

    [Fact]
    public void OllamaProvider_Should_ConfigureWithCustomUrl()
    {
        // Arrange & Act
        var provider = new OllamaLlmProvider(
            NullLogger<OllamaLlmProvider>.Instance,
            new HttpClient(),
            baseUrl: "http://192.168.1.100:11434",
            model: "llama2"
        );

        // Assert - should not throw
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task OllamaProvider_Should_ThrowException_WhenServerNotAvailable()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(2);
        
        var provider = new OllamaLlmProvider(
            NullLogger<OllamaLlmProvider>.Instance,
            httpClient,
            baseUrl: "http://localhost:19999", // Non-existent port
            model: "test-model",
            maxRetries: 0,
            timeoutSeconds: 2
        );

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        });
        
        // Should fail with connection or timeout error
        Assert.NotNull(exception);
    }

    [Fact]
    public void OpenAiProvider_Should_RequireApiKey()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new OpenAiLlmProvider(
                NullLogger<OpenAiLlmProvider>.Instance,
                new HttpClient(),
                apiKey: "" // Empty key should throw
            );
        });
    }

    [Fact]
    public void AzureOpenAiProvider_Should_RequireApiKeyAndEndpoint()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                apiKey: "", // Empty key should throw
                endpoint: "https://test.openai.azure.com"
            );
        });

        Assert.Throws<ArgumentException>(() =>
        {
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                apiKey: "test-key",
                endpoint: "" // Empty endpoint should throw
            );
        });
    }

    [Fact]
    public void GeminiProvider_Should_RequireApiKey()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
        {
            new GeminiLlmProvider(
                NullLogger<GeminiLlmProvider>.Instance,
                new HttpClient(),
                apiKey: "" // Empty key should throw
            );
        });
    }

    [Fact]
    public void AzureOpenAiProvider_Should_ConfigureWithCustomDeployment()
    {
        // Arrange & Act
        var provider = new AzureOpenAiLlmProvider(
            NullLogger<AzureOpenAiLlmProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            endpoint: "https://test.openai.azure.com",
            deploymentName: "my-custom-gpt4"
        );

        // Assert - should not throw
        Assert.NotNull(provider);
    }

    [Fact]
    public void GeminiProvider_Should_ConfigureWithCustomModel()
    {
        // Arrange & Act
        var provider = new GeminiLlmProvider(
            NullLogger<GeminiLlmProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            model: "gemini-pro-vision"
        );

        // Assert - should not throw
        Assert.NotNull(provider);
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
