using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Serialization;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// API-level tests for /api/script endpoint with focus on:
/// - Enum handling and tolerant parsing
/// - ProblemDetails error responses
/// - Provider fallback behavior
/// - DTO round-trip validation
/// </summary>
public class ScriptApiTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ScriptApiTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        EnumJsonConverters.AddToOptions(_jsonOptions);
    }

    #region DTO Round-Trip Tests

    [Fact]
    public void ScriptRequest_Should_DeserializeWithCanonicalEnums()
    {
        // Arrange - JSON with canonical enum values
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""Widescreen16x9"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Balanced"",
            ""style"": ""Standard""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("Test Topic", dto.Topic);
        Assert.Equal(Aspect.Widescreen16x9, dto.Aspect);
        Assert.Equal(Pacing.Conversational, dto.Pacing);
        Assert.Equal(Density.Balanced, dto.Density);
    }

    [Fact]
    public void ScriptRequest_Should_DeserializeWithAliasEnums()
    {
        // Arrange - JSON with legacy alias values (as might come from TypeScript UI)
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""16:9"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Normal"",
            ""style"": ""Standard""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(Aspect.Widescreen16x9, dto.Aspect); // 16:9 -> Widescreen16x9
        Assert.Equal(Density.Balanced, dto.Density); // Normal -> Balanced
    }

    [Fact]
    public void ScriptRequest_Should_HandleCaseInsensitiveEnums()
    {
        // Arrange - JSON with mixed case enum values
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""widescreen16x9"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""conversational"",
            ""density"": ""balanced"",
            ""style"": ""Standard""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(Aspect.Widescreen16x9, dto.Aspect);
        Assert.Equal(Pacing.Conversational, dto.Pacing);
        Assert.Equal(Density.Balanced, dto.Density);
    }

    [Fact]
    public void ScriptRequest_Should_ThrowWithInvalidAspect()
    {
        // Arrange - JSON with invalid aspect value
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""4:3"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Balanced"",
            ""style"": ""Standard""
        }";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions));

        Assert.Contains("Aspect value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
        Assert.Contains("16:9", exception.Message); // Should suggest valid aliases
    }

    [Fact]
    public void ScriptRequest_Should_ThrowWithInvalidDensity()
    {
        // Arrange - JSON with invalid density value
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""Widescreen16x9"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Medium"",
            ""style"": ""Standard""
        }";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions));

        Assert.Contains("Density value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
        Assert.Contains("Normal", exception.Message); // Should suggest valid aliases
    }

    [Fact]
    public void ScriptRequest_Should_RoundTripWithAllEnums()
    {
        // Arrange
        var dto = new ScriptRequestDto(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Vertical9x16,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Fast,
            Density: Density.Dense,
            Style: "Tutorial",
            ProviderTier: "Free"
        );

        // Act - Serialize then deserialize
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var result = JsonSerializer.Deserialize<ScriptRequestDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Topic, result.Topic);
        Assert.Equal(dto.Aspect, result.Aspect);
        Assert.Equal(dto.Pacing, result.Pacing);
        Assert.Equal(dto.Density, result.Density);
    }

    #endregion

    #region Orchestrator Integration Tests

    [Fact]
    public async Task GenerateScript_Should_ReturnValidScript_WithCompleteBriefAndPlan()
    {
        // Arrange - Complete request with valid Brief and Plan
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Introduction to Python Programming",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "Friendly",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Tutorial"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success, "Script generation should succeed");
        Assert.NotNull(result.Script);
        Assert.NotEmpty(result.Script);
        Assert.Contains("Python", result.Script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("##", result.Script); // Should have scene markers
        Assert.Equal("RuleBased", result.ProviderUsed);
        
        // Validate script has reasonable word count for 5 minute video
        var wordCount = result.Script.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(wordCount >= 400, $"5-minute script should have at least 400 words, got {wordCount}");
    }

    [Fact]
    public async Task GenerateScript_Should_FallbackToRuleBased_WhenOllamaUnreachable()
    {
        // Arrange - Ollama provider that fails, RuleBased as fallback
        var mockOllama = new Mock<ILlmProvider>();
        mockOllama
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failed to connect to Ollama at http://127.0.0.1:11434"));

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = mockOllama.Object,
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Students",
            Goal: "Educational",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Lecture"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success, "Should succeed with fallback");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback, "Should indicate fallback was used");
        Assert.NotNull(result.Script);
        Assert.Contains("Machine Learning", result.Script);
        
        // Verify Ollama was attempted before fallback
        mockOllama.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GenerateScript_Should_ReturnErrorCode_WhenNoProvidersAvailable()
    {
        // Arrange - No providers available
        var providers = new Dictionary<string, ILlmProvider>();

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );

        // Act & Assert - Should throw exception when no providers available
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await orchestrator.GenerateScriptAsync(
                brief,
                planSpec,
                "Free",
                offlineOnly: false,
                CancellationToken.None
            )
        );

        Assert.Contains("provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateScript_Should_ReturnE307_WhenProRequestedInOfflineMode()
    {
        // Arrange - Pro provider requested with offline mode
        var mockProProvider = new Mock<ILlmProvider>();
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Cloud Services",
            Audience: "Developers",
            Goal: "Overview",
            Tone: "Technical",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(4),
            Pacing: Pacing.Fast,
            Density: Density.Dense,
            Style: "Technical"
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Pro",
            offlineOnly: true,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E307", result.ErrorCode);
        Assert.Contains("OfflineOnly", result.ErrorMessage);
        Assert.Contains("internet connection", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        
        // Pro provider should never be called
        mockProProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GenerateScript_Should_HandleAllAspectRatios()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider> { ["RuleBased"] = provider };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );

        var aspectRatios = new[] 
        { 
            Aspect.Widescreen16x9, 
            Aspect.Vertical9x16, 
            Aspect.Square1x1 
        };

        // Act & Assert
        foreach (var aspect in aspectRatios)
        {
            var brief = new Brief(
                Topic: $"Test for {aspect}",
                Audience: "General",
                Goal: "Test",
                Tone: "Informative",
                Language: "en-US",
                Aspect: aspect
            );

            var result = await orchestrator.GenerateScriptAsync(
                brief,
                planSpec,
                "Free",
                offlineOnly: false,
                CancellationToken.None
            );

            Assert.True(result.Success, $"Should succeed for aspect {aspect}");
            Assert.NotNull(result.Script);
        }
    }

    [Fact]
    public async Task GenerateScript_Should_HandleAllPacingOptions()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider> { ["RuleBased"] = provider };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var pacingOptions = new[] { Pacing.Chill, Pacing.Conversational, Pacing.Fast };

        // Act & Assert
        var scripts = new Dictionary<Pacing, string>();
        foreach (var pacing in pacingOptions)
        {
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(3),
                Pacing: pacing,
                Density: Density.Balanced,
                Style: "Standard"
            );

            var result = await orchestrator.GenerateScriptAsync(
                brief,
                planSpec,
                "Free",
                offlineOnly: false,
                CancellationToken.None
            );

            Assert.True(result.Success, $"Should succeed for pacing {pacing}");
            Assert.NotNull(result.Script);
            scripts[pacing] = result.Script;
        }

        // Verify that pacing affects script length
        var chillWords = CountWords(scripts[Pacing.Chill]);
        var conversationalWords = CountWords(scripts[Pacing.Conversational]);
        var fastWords = CountWords(scripts[Pacing.Fast]);

        Assert.True(chillWords < conversationalWords, 
            $"Chill ({chillWords}) should have fewer words than Conversational ({conversationalWords})");
        Assert.True(conversationalWords < fastWords, 
            $"Conversational ({conversationalWords}) should have fewer words than Fast ({fastWords})");
    }

    [Fact]
    public async Task GenerateScript_Should_HandleAllDensityOptions()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider> { ["RuleBased"] = provider };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var densityOptions = new[] { Density.Sparse, Density.Balanced, Density.Dense };

        // Act & Assert
        var scripts = new Dictionary<Density, string>();
        foreach (var density in densityOptions)
        {
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(3),
                Pacing: Pacing.Conversational,
                Density: density,
                Style: "Standard"
            );

            var result = await orchestrator.GenerateScriptAsync(
                brief,
                planSpec,
                "Free",
                offlineOnly: false,
                CancellationToken.None
            );

            Assert.True(result.Success, $"Should succeed for density {density}");
            Assert.NotNull(result.Script);
            scripts[density] = result.Script;
        }

        // Verify that density affects script length
        var sparseWords = CountWords(scripts[Density.Sparse]);
        var balancedWords = CountWords(scripts[Density.Balanced]);
        var denseWords = CountWords(scripts[Density.Dense]);

        Assert.True(sparseWords < balancedWords, 
            $"Sparse ({sparseWords}) should have fewer words than Balanced ({balancedWords})");
        Assert.True(balancedWords < denseWords, 
            $"Balanced ({balancedWords}) should have fewer words than Dense ({denseWords})");
    }

    #endregion

    #region Helper Methods

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    #endregion
}
