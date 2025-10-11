using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// End-to-end tests for the /script endpoint behavior with different provider configurations
/// </summary>
public class ScriptEndpointE2ETests
{
    [Fact]
    public async Task ScriptEndpoint_Should_UseRuleBased_WhenNoOtherProviders()
    {
        // Arrange - Free tier with only RuleBased provider
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Introduction to Python",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "Friendly",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Tutorial"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(brief, spec, "Free", offlineOnly: false, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.False(result.IsFallback);
        Assert.NotNull(result.Script);
        Assert.Contains("Python", result.Script);
        Assert.Contains("##", result.Script); // Has scene markers
    }

    [Fact]
    public async Task ScriptEndpoint_Should_FallbackToRuleBased_WhenProFails()
    {
        // Arrange - Pro tier that falls back to RuleBased
        var mockProProvider = new Mock<ILlmProvider>();
        mockProProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API key invalid"));

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Students",
            Goal: "Educational",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Lecture"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(brief, spec, "Pro", offlineOnly: false, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback, "Should have fallen back from Pro to RuleBased");
        Assert.NotNull(result.Script);
        Assert.Contains("Machine Learning", result.Script);
    }

    [Fact]
    public async Task ScriptEndpoint_Should_ReturnE307_WhenProInOfflineMode()
    {
        // Arrange - Pro tier with OfflineOnly mode
        var mockProProvider = new Mock<ILlmProvider>();
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(brief, spec, "Pro", offlineOnly: true, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E307", result.ErrorCode);
        Assert.Contains("OfflineOnly", result.ErrorMessage);
        mockProProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Pro provider should not be called in offline mode"
        );
    }

    [Fact]
    public async Task ScriptEndpoint_Should_UseRuleBased_WhenProIfAvailableInOfflineMode()
    {
        // Arrange - ProIfAvailable tier with OfflineOnly mode - should gracefully downgrade
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Web Development",
            Audience: "Developers",
            Goal: "Tutorial",
            Tone: "Professional",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(4),
            Pacing: Pacing.Fast,
            Density: Density.Dense,
            Style: "Technical"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(brief, spec, "ProIfAvailable", offlineOnly: true, CancellationToken.None);

        // Assert
        Assert.True(result.Success, "ProIfAvailable should gracefully downgrade in offline mode");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.NotNull(result.Script);
        Assert.Contains("Web Development", result.Script);
    }

    [Fact]
    public async Task ScriptEndpoint_Should_RespectPacingAndDensity()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider> { ["RuleBased"] = provider };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Neutral",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        // Test with Fast pacing and Dense density
        var fastDenseSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Fast,
            Density: Density.Dense,
            Style: "Standard"
        );

        // Test with Chill pacing and Sparse density
        var chillSparseSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Chill,
            Density: Density.Sparse,
            Style: "Standard"
        );

        // Act
        var fastDenseResult = await orchestrator.GenerateScriptAsync(brief, fastDenseSpec, "Free", offlineOnly: false, CancellationToken.None);
        var chillSparseResult = await orchestrator.GenerateScriptAsync(brief, chillSparseSpec, "Free", offlineOnly: false, CancellationToken.None);

        // Assert
        Assert.True(fastDenseResult.Success);
        Assert.True(chillSparseResult.Success);

        var fastDenseWords = CountWords(fastDenseResult.Script!);
        var chillSparseWords = CountWords(chillSparseResult.Script!);

        Assert.True(fastDenseWords > chillSparseWords,
            $"Fast/Dense ({fastDenseWords} words) should have more content than Chill/Sparse ({chillSparseWords} words)");
    }

    [Fact]
    public async Task ScriptEndpoint_Should_HandleMultipleFallbacks()
    {
        // Arrange - Chain of providers that fail until RuleBased succeeds
        var mockOpenAI = new Mock<ILlmProvider>();
        mockOpenAI
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("OpenAI unavailable"));

        var mockOllama = new Mock<ILlmProvider>();
        mockOllama
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Ollama not running"));

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockOpenAI.Object,
            ["Ollama"] = mockOllama.Object,
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        var brief = new Brief(
            Topic: "Cloud Computing",
            Audience: "IT Professionals",
            Goal: "Overview",
            Tone: "Technical",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(6),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Professional"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(brief, spec, "Pro", offlineOnly: false, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.NotNull(result.Script);
        Assert.Contains("Cloud Computing", result.Script);

        // Verify all providers in the chain were attempted
        mockOpenAI.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        mockOllama.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
