using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests that use real provider API keys to validate provider functionality.
/// These tests are skipped if API keys are not available.
/// </summary>
public class ProviderApiKeysIntegrationTests
{
    private const string OpenAiApiKey = "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";
    private const string PexelsApiKey = "sFxx0egxRq0mRYu1VFBNossHd6zTSWryLHSroEjVvjEbEWHtnSj2BF2E";

    private readonly Brief _testBrief = new Brief(
        Topic: "Introduction to Machine Learning",
        Audience: "Beginners",
        Goal: "Educational",
        Tone: "Friendly",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(1),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    [Fact]
    public async Task OpenAI_Provider_ShouldGenerateValidScript()
    {
        // Arrange
        var logger = NullLogger<Aura.Providers.Llm.OpenAiLlmProvider>.Instance;
        var httpClient = new HttpClient();
        var provider = new Aura.Providers.Llm.OpenAiLlmProvider(logger, httpClient, OpenAiApiKey);

        // Act
        var script = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Machine Learning", script, StringComparison.OrdinalIgnoreCase);
        
        // Verify script has proper structure
        Assert.Contains("##", script);
        
        // Verify it's not just template text
        Assert.True(script.Length > 100, "Script should be substantial");
    }

    [Fact]
    public async Task OpenAI_Provider_ShouldHandleDifferentTopics()
    {
        // Arrange
        var logger = NullLogger<Aura.Providers.Llm.OpenAiLlmProvider>.Instance;
        var httpClient = new HttpClient();
        var provider = new Aura.Providers.Llm.OpenAiLlmProvider(logger, httpClient, OpenAiApiKey);
        
        var brief1 = _testBrief with { Topic = "Introduction to Python Programming" };
        var brief2 = _testBrief with { Topic = "History of Ancient Rome" };

        // Act
        var script1 = await provider.DraftScriptAsync(brief1, _testSpec, CancellationToken.None);
        var script2 = await provider.DraftScriptAsync(brief2, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script1);
        Assert.NotNull(script2);
        Assert.Contains("Python", script1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Rome", script2, StringComparison.OrdinalIgnoreCase);
        
        // Scripts should be different
        Assert.NotEqual(script1, script2);
    }

    [Fact]
    public async Task OpenAI_Provider_ShouldRespectTargetDuration()
    {
        // Arrange
        var logger = NullLogger<Aura.Providers.Llm.OpenAiLlmProvider>.Instance;
        var httpClient = new HttpClient();
        var provider = new Aura.Providers.Llm.OpenAiLlmProvider(logger, httpClient, OpenAiApiKey);
        
        var shortSpec = _testSpec with { TargetDuration = TimeSpan.FromSeconds(30) };
        var longSpec = _testSpec with { TargetDuration = TimeSpan.FromMinutes(3) };

        // Act
        var shortScript = await provider.DraftScriptAsync(_testBrief, shortSpec, CancellationToken.None);
        var longScript = await provider.DraftScriptAsync(_testBrief, longSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(shortScript);
        Assert.NotNull(longScript);
        
        var shortWordCount = CountWords(shortScript);
        var longWordCount = CountWords(longScript);
        
        Assert.True(longWordCount > shortWordCount, 
            $"Long script ({longWordCount} words) should be longer than short script ({shortWordCount} words)");
    }

    [Fact]
    public async Task OpenAI_Provider_ShouldHandleDifferentTones()
    {
        // Arrange
        var logger = NullLogger<Aura.Providers.Llm.OpenAiLlmProvider>.Instance;
        var httpClient = new HttpClient();
        var provider = new Aura.Providers.Llm.OpenAiLlmProvider(logger, httpClient, OpenAiApiKey);
        
        var professionalBrief = _testBrief with { Tone = "Professional" };
        var casualBrief = _testBrief with { Tone = "Casual" };

        // Act
        var professionalScript = await provider.DraftScriptAsync(professionalBrief, _testSpec, CancellationToken.None);
        var casualScript = await provider.DraftScriptAsync(casualBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(professionalScript);
        Assert.NotNull(casualScript);
        Assert.NotEqual(professionalScript, casualScript);
    }

    [Fact]
    public async Task OpenAI_Provider_ShouldHandleInvalidApiKey()
    {
        // Arrange
        var logger = NullLogger<Aura.Providers.Llm.OpenAiLlmProvider>.Instance;
        var httpClient = new HttpClient();
        var provider = new Aura.Providers.Llm.OpenAiLlmProvider(logger, httpClient, "invalid-api-key");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Pexels_Provider_ShouldReturnValidImages()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", PexelsApiKey);

        // Act
        var response = await httpClient.GetAsync("https://api.pexels.com/v1/search?query=nature&per_page=5");

        // Assert
        Assert.True(response.IsSuccessStatusCode, "Pexels API should return success");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
        Assert.Contains("photos", content);
    }

    [Fact]
    public async Task Pexels_Provider_ShouldHandleDifferentQueries()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", PexelsApiKey);

        // Act
        var response1 = await httpClient.GetAsync("https://api.pexels.com/v1/search?query=technology&per_page=3");
        var response2 = await httpClient.GetAsync("https://api.pexels.com/v1/search?query=nature&per_page=3");

        // Assert
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        
        Assert.NotEqual(content1, content2);
    }

    [Fact]
    public async Task Pexels_Provider_ShouldRespectPerPageLimit()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", PexelsApiKey);

        // Act
        var response = await httpClient.GetAsync("https://api.pexels.com/v1/search?query=sunset&per_page=10");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("photos", content);
        Assert.Contains("per_page", content);
    }

    [Fact]
    public async Task Pexels_Provider_ShouldHandleInvalidApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "invalid-key");

        // Act
        var response = await httpClient.GetAsync("https://api.pexels.com/v1/search?query=nature&per_page=5");

        // Assert
        Assert.False(response.IsSuccessStatusCode, "Should fail with invalid API key");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
