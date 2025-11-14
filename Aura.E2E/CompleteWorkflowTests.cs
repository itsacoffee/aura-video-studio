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
using Xunit;

namespace Aura.E2E;

/// <summary>
/// Complete end-to-end workflow validation tests
/// Tests the full pipeline from hardware detection through script generation
/// </summary>
public class CompleteWorkflowTests
{
    /// <summary>
    /// Test complete offline workflow with local providers only
    /// Validates: Hardware detection → Provider selection → Validation → Script generation
    /// </summary>
    [Fact]
    public async Task CompleteOfflineWorkflow_Should_GenerateScriptSuccessfully()
    {
        // Step 1: Hardware detection
        var hardwareDetector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);
        var systemProfile = await hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
        
        Assert.NotNull(systemProfile);
        Assert.True(systemProfile.LogicalCores > 0);
        Assert.True(systemProfile.RamGB > 0);

        // Step 2: Setup providers (local only for offline mode)
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

        // Step 3: Pre-generation validation
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);
        var validationResult = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Free",
            "Free",
            "Free",
            offlineOnly: true
        );

        Assert.True(validationResult.IsValid, "Validation should pass for Free tier with local providers");
        Assert.Empty(validationResult.Issues);

        // Step 4: Provider selection
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var llmSelection = mixer.SelectLlmProvider(llmProviders, "Free");
        var ttsSelection = mixer.SelectTtsProvider(ttsProviders, "Free");
        var visualSelection = mixer.SelectVisualProvider(visualProviders, "Free", false, 0);

        Assert.Equal("RuleBased", llmSelection.SelectedProvider);
        Assert.Equal("Windows", ttsSelection.SelectedProvider);
        Assert.Equal("Stock", visualSelection.SelectedProvider);

        // Step 5: Script generation
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "Quick Start with Aura",
            Audience: "New Users",
            Goal: "Tutorial",
            Tone: "Friendly",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(15),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Tutorial"
        );

        var scriptResult = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Step 6: Validate results
        Assert.True(scriptResult.Success, $"Script generation should succeed. Error: {scriptResult.ErrorMessage}");
        Assert.Equal("RuleBased", scriptResult.ProviderUsed);
        Assert.NotNull(scriptResult.Script);
        Assert.InRange(scriptResult.Script!.Length, 50, 3000);
        Assert.Contains("Aura", scriptResult.Script, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test complete workflow with Pro tier and automatic fallback
    /// Validates graceful downgrade when Pro providers unavailable
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_ProWithFallback_Should_DowngradeGracefully()
    {
        // Step 1: Hardware detection
        var hardwareDetector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);
        var systemProfile = await hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
        
        Assert.NotNull(systemProfile);

        // Step 2: Setup providers (Pro not available, only Free)
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Ollama"] = new FailingLlmProvider("Ollama") // Simulates unavailable
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Step 3: Pre-generation validation (ProIfAvailable should pass)
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);
        var validationResult = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "ProIfAvailable",
            "ProIfAvailable",
            "ProIfAvailable",
            offlineOnly: false
        );

        Assert.True(validationResult.IsValid, "ProIfAvailable should validate successfully");

        // Step 4: Provider selection (should fallback to Free)
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var llmSelection = mixer.SelectLlmProvider(llmProviders, "ProIfAvailable");
        
        // Should select a fallback provider when Pro not available
        Assert.NotNull(llmSelection.SelectedProvider);
        Assert.True(
            llmSelection.SelectedProvider == "RuleBased" || llmSelection.SelectedProvider == "Ollama",
            "Should select available provider"
        );

        // Step 5: Script generation with fallback
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "AI-Powered Video Creation",
            Audience: "Content Creators",
            Goal: "Showcase Features",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(15),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Explainer"
        );

        var scriptResult = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "ProIfAvailable",
            offlineOnly: false,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Step 6: Validate graceful fallback
        Assert.True(scriptResult.Success, "Script generation should succeed with fallback");
        Assert.NotNull(scriptResult.Script);
        Assert.InRange(scriptResult.Script!.Length, 50, 3000);
        
        // Should indicate fallback occurred if RuleBased was used
        if (scriptResult.ProviderUsed == "RuleBased")
        {
            Assert.True(scriptResult.IsFallback, "RuleBased usage from ProIfAvailable should be marked as fallback");
        }
    }

    /// <summary>
    /// Test that offline mode blocks Pro providers correctly
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_OfflineMode_Should_BlockProProviders()
    {
        // Step 1: Setup providers
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = new FailingLlmProvider("OpenAI") // Pro provider
        };

        // Step 2: Validation should fail for Pro in offline mode
        var validator = new GenerationValidator(NullLogger<GenerationValidator>.Instance);
        var validationResult = validator.ValidateProviders(
            llmProviders,
            new Dictionary<string, ITtsProvider>(),
            new Dictionary<string, object>(),
            "Pro",
            "Free",
            "Free",
            offlineOnly: true
        );

        Assert.False(validationResult.IsValid, "Pro tier should fail validation in offline mode");
        Assert.Contains(validationResult.Issues, i => i.Contains("Pro LLM") && i.Contains("offline"));

        // Step 3: ScriptOrchestrator should also block
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "Test",
            Audience: "Test",
            Goal: "Test",
            Tone: "Test",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Test"
        );

        var scriptResult = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Pro",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        Assert.False(scriptResult.Success, "Script generation should fail for Pro in offline mode");
        Assert.Equal("E307", scriptResult.ErrorCode);
    }

    /// <summary>
    /// Test that hardware detection works across different system tiers
    /// </summary>
    [Fact]
    public async Task HardwareDetection_Should_DetectSystemTier()
    {
        // Arrange & Act
        var detector = new HardwareDetector(NullLogger<HardwareDetector>.Instance);
        var profile = await detector.DetectSystemAsync().ConfigureAwait(false);

        // Assert
        Assert.NotNull(profile);
        Assert.True(Enum.IsDefined(typeof(HardwareTier), profile.Tier));
        
        // Should detect basic specs
        Assert.True(profile.LogicalCores >= 1);
        Assert.True(profile.RamGB >= 1);
        
        // GPU detection should complete (may be null on non-GPU systems)
        // Just verify the property can be accessed
        var gpu = profile.Gpu;
        _ = gpu; // Suppress unused variable warning
    }
}
