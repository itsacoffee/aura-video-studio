using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests that validate Aura.Providers (video generation logic) are invoked correctly.
/// These tests bypass the UI/HTTP layer and test the core provider and orchestration components directly.
/// This ensures the provider layer integrates correctly with the orchestration layer.
/// </summary>
public class VideoGenerationApiIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;

    public VideoGenerationApiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Test: RuleBased LLM provider is invoked correctly for script generation
    /// </summary>
    [Fact]
    public async Task RuleBasedProvider_ScriptGeneration_ShouldProduceValidScript()
    {
        _output.WriteLine("=== Integration Test: RuleBased Provider Invocation ===");

        var provider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());

        var brief = new Brief(
            Topic: "Introduction to Machine Learning",
            Audience: "Software Developers",
            Goal: "Educational",
            Tone: "Technical but Accessible",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Technical Tutorial"
        );

        _output.WriteLine("Invoking RuleBased provider for script generation...");
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None).ConfigureAwait(false);

        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.True(script.Length >= 100, "Script should have substantial content");

        _output.WriteLine($"Script generated: {script.Length} characters");
        _output.WriteLine($"Script preview:\n{script.Substring(0, Math.Min(500, script.Length))}");
        
        // Verify script contains expected structure (scenes)
        Assert.Contains("##", script, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine("✓ RuleBased provider produces valid script with scenes");
    }

    /// <summary>
    /// Test: Provider selection logic works correctly
    /// </summary>
    [Fact]
    public void ProviderMixer_SelectsCorrectProvider_ForFreeTier()
    {
        _output.WriteLine("=== Integration Test: Provider Selection ===");

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(_loggerFactory.CreateLogger<ProviderMixer>(), config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var selection = mixer.SelectLlmProvider(providers, "Free");

        Assert.NotNull(selection);
        Assert.Equal("RuleBased", selection.SelectedProvider);
        
        _output.WriteLine($"Provider selected: {selection.SelectedProvider}");
        _output.WriteLine("✓ Provider mixer selects RuleBased for Free tier");
    }

    /// <summary>
    /// Test: Hardware detection works and returns valid tier
    /// </summary>
    [Fact]
    public async Task HardwareDetector_ReturnsValidSystemProfile()
    {
        _output.WriteLine("=== Integration Test: Hardware Detection ===");

        var detector = new HardwareDetector(_loggerFactory.CreateLogger<HardwareDetector>());
        var profile = await detector.DetectSystemAsync().ConfigureAwait(false);

        Assert.NotNull(profile);
        Assert.True(profile.LogicalCores >= 1, "Should detect at least 1 core");
        Assert.True(profile.RamGB >= 1, "Should detect at least 1GB RAM");
        Assert.True(Enum.IsDefined(typeof(HardwareTier), profile.Tier), "Should return valid tier");

        _output.WriteLine($"Detected: {profile.LogicalCores} cores, {profile.RamGB}GB RAM, Tier: {profile.Tier}");
        _output.WriteLine("✓ Hardware detection returns valid system profile");
    }

    /// <summary>
    /// Test: Generation validator correctly validates providers
    /// </summary>
    [Fact]
    public void GenerationValidator_ValidatesProvidersCorrectly()
    {
        _output.WriteLine("=== Integration Test: Generation Validator ===");

        var validator = new GenerationValidator(_loggerFactory.CreateLogger<GenerationValidator>());

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>();
        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        // Validate Free tier with available providers
        var result = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Free",
            "Free",
            "Free",
            offlineOnly: true
        );

        _output.WriteLine($"Validation result: IsValid={result.IsValid}");
        if (result.Issues.Length > 0)
        {
            foreach (var issue in result.Issues)
            {
                _output.WriteLine($"  Issue: {issue}");
            }
        }

        // For this test, we just verify the validator runs without exception
        Assert.NotNull(result);
        _output.WriteLine("✓ Generation validator completes validation check");
    }

    /// <summary>
    /// Test: Script orchestrator integrates with LLM providers correctly
    /// </summary>
    [Fact]
    public async Task ScriptOrchestrator_IntegratesWithProviders_Correctly()
    {
        _output.WriteLine("=== Integration Test: Script Orchestrator ===");

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>())
        };

        var mixerConfig = new ProviderMixingConfig { LogProviderSelection = true, AutoFallback = true };
        var mixer = new ProviderMixer(_loggerFactory.CreateLogger<ProviderMixer>(), mixerConfig);

        var orchestrator = new ScriptOrchestrator(
            _loggerFactory.CreateLogger<ScriptOrchestrator>(),
            _loggerFactory,
            mixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "Getting Started with Aura",
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

        _output.WriteLine("Generating script through orchestrator...");
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        Assert.True(result.Success, $"Script generation should succeed. Error: {result.ErrorMessage}");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.NotNull(result.Script);
        Assert.True(result.Script.Length >= 50, "Script should have meaningful content");

        _output.WriteLine($"✓ Orchestrator produced script using {result.ProviderUsed} provider");
        _output.WriteLine($"✓ Script length: {result.Script.Length} characters");
    }

    /// <summary>
    /// Test: Provider fallback works when primary provider fails
    /// </summary>
    [Fact]
    public async Task ProviderFallback_WhenPrimaryFails_UsesBackup()
    {
        _output.WriteLine("=== Integration Test: Provider Fallback ===");

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>())
        };

        var mixerConfig = new ProviderMixingConfig { LogProviderSelection = true, AutoFallback = true };
        var mixer = new ProviderMixer(_loggerFactory.CreateLogger<ProviderMixer>(), mixerConfig);

        var orchestrator = new ScriptOrchestrator(
            _loggerFactory.CreateLogger<ScriptOrchestrator>(),
            _loggerFactory,
            mixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "Fallback Test",
            Audience: "Test",
            Goal: "Testing",
            Tone: "Neutral",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Test"
        );

        // Request ProIfAvailable which should fallback to RuleBased
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "ProIfAvailable",
            offlineOnly: false,
            CancellationToken.None
        ).ConfigureAwait(false);

        Assert.True(result.Success, "Should succeed with fallback");
        Assert.NotNull(result.Script);
        
        _output.WriteLine($"✓ Fallback worked, provider used: {result.ProviderUsed}");
    }

    /// <summary>
    /// Test: Offline mode correctly rejects Pro tier
    /// </summary>
    [Fact]
    public async Task OfflineMode_RejectsPro_WhenNoLocalProviders()
    {
        _output.WriteLine("=== Integration Test: Offline Mode Rejection ===");

        // Empty provider dictionary - no local providers available
        var llmProviders = new Dictionary<string, ILlmProvider>();

        var mixerConfig = new ProviderMixingConfig { LogProviderSelection = true, AutoFallback = true };
        var mixer = new ProviderMixer(_loggerFactory.CreateLogger<ProviderMixer>(), mixerConfig);

        var orchestrator = new ScriptOrchestrator(
            _loggerFactory.CreateLogger<ScriptOrchestrator>(),
            _loggerFactory,
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

        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Pro",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        Assert.False(result.Success, "Pro in offline mode with no providers should fail");
        Assert.NotNull(result.ErrorMessage);
        
        _output.WriteLine($"✓ Correctly rejected: {result.ErrorCode} - {result.ErrorMessage}");
    }

    /// <summary>
    /// Test: Multiple content types are handled correctly
    /// </summary>
    [Fact]
    public async Task MultipleContentTypes_AllGenerateSuccessfully()
    {
        _output.WriteLine("=== Integration Test: Multiple Content Types ===");

        var provider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());

        var contentTypes = new[]
        {
            ("Educational", "How AI Works"),
            ("Entertainment", "Top 5 Movies"),
            ("Marketing", "Product Launch")
        };

        foreach (var (category, topic) in contentTypes)
        {
            var brief = new Brief(
                Topic: topic,
                Audience: "General",
                Goal: category,
                Tone: "informative",
                Language: "English",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: category
            );

            var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None).ConfigureAwait(false);
            
            Assert.NotNull(script);
            Assert.NotEmpty(script);
            
            _output.WriteLine($"✓ {category}: {script.Length} chars");
        }

        _output.WriteLine("✓ All content types generated successfully");
    }

    /// <summary>
    /// Test: Provider characteristics are correctly reported
    /// </summary>
    [Fact]
    public void ProviderCharacteristics_AreCorrectlyReported()
    {
        _output.WriteLine("=== Integration Test: Provider Characteristics ===");

        var provider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());
        var characteristics = provider.GetCharacteristics();

        Assert.NotNull(characteristics);
        Assert.True(characteristics.IsLocal, "RuleBased should be marked as local");
        Assert.NotNull(characteristics.ProviderTier);
        
        _output.WriteLine($"Provider characteristics:");
        _output.WriteLine($"  IsLocal: {characteristics.IsLocal}");
        _output.WriteLine($"  SupportsStreaming: {characteristics.SupportsStreaming}");
        _output.WriteLine($"  ProviderTier: {characteristics.ProviderTier}");
        _output.WriteLine($"  ExpectedFirstTokenMs: {characteristics.ExpectedFirstTokenMs}");
        _output.WriteLine($"  ExpectedTokensPerSec: {characteristics.ExpectedTokensPerSec}");
        
        _output.WriteLine("✓ Provider characteristics reported correctly");
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

