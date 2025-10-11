using System.Collections.Generic;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// Tests for generation validation before running pipeline
/// </summary>
public class GenerationValidatorTests
{
    /// <summary>
    /// Test validation passes with all providers available
    /// </summary>
    [Fact]
    public void Validation_Should_PassWithAllProvidersAvailable()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["OpenAI"] = new FailingLlmProvider("OpenAI")
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Act
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Free",
            "Free",
            "Free",
            offlineOnly: false
        );

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
        Assert.Empty(result.Warnings);
    }

    /// <summary>
    /// Test validation fails when no LLM providers registered
    /// </summary>
    [Fact]
    public void Validation_Should_FailWithNoLlmProviders()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        // Act
        var result = validator.ValidateProviders(
            null,
            new Dictionary<string, ITtsProvider>(),
            new Dictionary<string, object>(),
            "Free",
            "Free",
            "Free",
            offlineOnly: false
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("No LLM providers"));
    }

    /// <summary>
    /// Test validation fails when Pro tier requested in offline mode
    /// </summary>
    [Fact]
    public void Validation_Should_FailWhenProRequestedInOfflineMode()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Act
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Pro",
            "Pro",
            "Pro",
            offlineOnly: true
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Pro LLM") && i.Contains("offline"));
        Assert.Contains(result.Issues, i => i.Contains("Pro TTS") && i.Contains("offline"));
        Assert.Contains(result.Issues, i => i.Contains("Pro Visual") && i.Contains("offline"));
    }

    /// <summary>
    /// Test validation warns when Pro tier requested but not available
    /// </summary>
    [Fact]
    public void Validation_Should_FailWhenProTierRequestedButNotAvailable()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
            // No OpenAI/Azure/Gemini
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
            // No ElevenLabs/PlayHT
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Act
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Pro",
            "Pro",
            "Free",
            offlineOnly: false
        );

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Pro LLM") && i.Contains("no Pro providers"));
        Assert.Contains(result.Warnings, w => w.Contains("Pro TTS") && w.Contains("no Pro providers"));
    }

    /// <summary>
    /// Test validation warns when fallback providers not available
    /// </summary>
    [Fact]
    public void Validation_Should_WarnWhenFallbackProvidersNotAvailable()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = new FailingLlmProvider("OpenAI")
            // No RuleBased fallback
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stability"] = new object()
            // No Stock fallback
        };

        // Act
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Pro",
            "Free",
            "Pro",
            offlineOnly: false
        );

        // Assert
        Assert.True(result.IsValid); // Still valid but has warnings
        Assert.Contains(result.Warnings, w => w.Contains("RuleBased"));
        Assert.Contains(result.Warnings, w => w.Contains("Stock"));
    }

    /// <summary>
    /// Test validation passes with ProIfAvailable when Pro not available
    /// </summary>
    [Fact]
    public void Validation_Should_PassWithProIfAvailableWhenProNotAvailable()
    {
        // Arrange
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
            // No Pro providers
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Act - ProIfAvailable gracefully downgrades, so validation should pass
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "ProIfAvailable",
            "ProIfAvailable",
            "ProIfAvailable",
            offlineOnly: false
        );

        // Assert
        Assert.True(result.IsValid);
    }
}
