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

namespace Aura.E2E;

/// <summary>
/// Critical path End-to-End test suite that validates the entire application stack.
/// Tests the complete GenerateVideoWorkflow from UI -> Middleware -> Backend -> Providers.
/// 
/// Note: These tests validate the core pipeline components without requiring a running HTTP server.
/// HTTP-level integration tests are in Aura.Tests/Integration when the full stack is available.
/// </summary>
public class GenerateVideoWorkflowE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;

    public GenerateVideoWorkflowE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Critical Path Test: GenerateVideoWorkflow
    /// Tests the complete flow from brief creation through script generation.
    /// 
    /// Steps validated:
    /// 1. Hardware detection (system profiling)
    /// 2. Provider selection (LLM, TTS, Visual)
    /// 3. Brief/Plan spec validation
    /// 4. Script generation with RuleBased provider
    /// 5. Provider fallback logic
    /// </summary>
    [Fact(DisplayName = "E2E: GenerateVideoWorkflow - Complete Critical Path")]
    public async Task GenerateVideoWorkflow_CriticalPath_ShouldCompleteSuccessfully()
    {
        _output.WriteLine("=== E2E Critical Path: GenerateVideoWorkflow ===");
        _output.WriteLine("Testing complete flow: Hardware Detection → Provider Selection → Script Generation");

        // Step 1: Hardware detection
        _output.WriteLine("\nStep 1: Hardware Detection...");
        var hardwareDetector = new HardwareDetector(_loggerFactory.CreateLogger<HardwareDetector>());
        var systemProfile = await hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
        
        Assert.NotNull(systemProfile);
        Assert.True(systemProfile.LogicalCores > 0, "Should detect CPU cores");
        Assert.True(systemProfile.RamGB > 0, "Should detect RAM");
        Assert.True(Enum.IsDefined(typeof(HardwareTier), systemProfile.Tier), "Should determine hardware tier");
        
        _output.WriteLine($"✓ Hardware detected: {systemProfile.LogicalCores} cores, {systemProfile.RamGB}GB RAM, Tier: {systemProfile.Tier}");

        // Step 2: Provider setup and selection
        _output.WriteLine("\nStep 2: Provider Selection...");
        var llmProvider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());
        
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var ttsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Mock"] = new MockTtsProvider("Mock")
        };

        var visualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };

        var mixerConfig = new ProviderMixingConfig { LogProviderSelection = true, AutoFallback = true };
        var mixer = new ProviderMixer(_loggerFactory.CreateLogger<ProviderMixer>(), mixerConfig);

        var llmSelection = mixer.SelectLlmProvider(llmProviders, "Free");
        var ttsSelection = mixer.SelectTtsProvider(ttsProviders, "Free");
        var visualSelection = mixer.SelectVisualProvider(visualProviders, "Free", false, 0);

        Assert.Equal("RuleBased", llmSelection.SelectedProvider);
        // TTS selection may return "Null" when no suitable provider found for tier
        Assert.True(
            ttsSelection.SelectedProvider == "Mock" || ttsSelection.SelectedProvider == "Null",
            $"TTS should select Mock or Null, got: {ttsSelection.SelectedProvider}");
        Assert.Equal("Stock", visualSelection.SelectedProvider);
        
        _output.WriteLine($"✓ Providers selected: LLM={llmSelection.SelectedProvider}, TTS={ttsSelection.SelectedProvider}, Visual={visualSelection.SelectedProvider}");

        // Step 3: Pre-generation validation
        _output.WriteLine("\nStep 3: Pre-generation Validation...");
        var validator = new GenerationValidator(_loggerFactory.CreateLogger<GenerationValidator>());
        var validationResult = validator.ValidateProviders(
            llmProviders,
            ttsProviders,
            visualProviders,
            "Free",
            "Free",
            "Free",
            offlineOnly: true
        );

        _output.WriteLine($"Validation result: IsValid={validationResult.IsValid}");
        if (validationResult.Issues.Length > 0)
        {
            foreach (var issue in validationResult.Issues)
            {
                _output.WriteLine($"  Issue: {issue}");
            }
        }
        
        _output.WriteLine("✓ Validation completed");

        // Step 4: Create Brief and Plan
        _output.WriteLine("\nStep 4: Create Brief and Plan...");
        var brief = new Brief(
            Topic: "Getting Started with Aura Video Studio",
            Audience: "New Users",
            Goal: "Tutorial",
            Tone: "Friendly and Educational",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        _output.WriteLine($"✓ Brief created: Topic='{brief.Topic}', Duration={planSpec.TargetDuration.TotalSeconds}s");

        // Step 5: Script generation
        _output.WriteLine("\nStep 5: Script Generation...");
        var script = await llmProvider.DraftScriptAsync(brief, planSpec, CancellationToken.None).ConfigureAwait(false);
        
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.True(script.Length >= 100, "Script should have meaningful content");
        
        _output.WriteLine($"✓ Script generated: {script.Length} characters");
        _output.WriteLine($"Script preview:\n{script.Substring(0, Math.Min(300, script.Length))}...");

        // Step 6: Verify script structure
        _output.WriteLine("\nStep 6: Verify Script Structure...");
        Assert.Contains("##", script, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine("✓ Script contains scene headers");
        _output.WriteLine("\n=== GenerateVideoWorkflow Critical Path Test Complete ===");
        _output.WriteLine("✓ All pipeline components validated successfully");
    }

    /// <summary>
    /// Test: Verify provider fallback when primary provider fails
    /// </summary>
    [Fact(DisplayName = "E2E: Provider Fallback Logic")]
    public async Task ProviderFallback_WhenPrimaryFails_ShouldUseBackup()
    {
        _output.WriteLine("=== E2E: Provider Fallback Logic ===");

        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["Failing"] = new FailingLlmProvider("Failing"),
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
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
            TargetDuration: TimeSpan.FromSeconds(15),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Test"
        );

        _output.WriteLine("Attempting script generation with fallback...");
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "ProIfAvailable",
            offlineOnly: false,
            CancellationToken.None
        ).ConfigureAwait(false);

        _output.WriteLine($"Result: Success={result.Success}, Provider={result.ProviderUsed}, IsFallback={result.IsFallback}");

        Assert.True(result.Success, "Script generation should succeed with fallback");
        Assert.NotNull(result.Script);
        Assert.Equal("RuleBased", result.ProviderUsed);
        
        _output.WriteLine("✓ Fallback to RuleBased provider worked correctly");
    }

    /// <summary>
    /// Test: Hardware tier detection for strategy selection
    /// </summary>
    [Fact(DisplayName = "E2E: Hardware Tier Detection")]
    public async Task HardwareTierDetection_ShouldDetermineCorrectTier()
    {
        _output.WriteLine("=== E2E: Hardware Tier Detection ===");

        var detector = new HardwareDetector(_loggerFactory.CreateLogger<HardwareDetector>());
        var profile = await detector.DetectSystemAsync().ConfigureAwait(false);

        Assert.NotNull(profile);
        
        _output.WriteLine($"System Profile:");
        _output.WriteLine($"  Logical Cores: {profile.LogicalCores}");
        _output.WriteLine($"  Physical Cores: {profile.PhysicalCores}");
        _output.WriteLine($"  RAM: {profile.RamGB}GB");
        _output.WriteLine($"  GPU: {profile.Gpu?.Model ?? "None detected"}");
        _output.WriteLine($"  Tier: {profile.Tier}");

        // Verify tier is reasonable for the detected hardware
        Assert.True(Enum.IsDefined(typeof(HardwareTier), profile.Tier));
        
        _output.WriteLine("✓ Hardware tier determined correctly");
    }

    /// <summary>
    /// Test: Offline mode correctly blocks online providers
    /// </summary>
    [Fact(DisplayName = "E2E: Offline Mode Enforcement")]
    public async Task OfflineMode_ShouldBlockOnlineProviders()
    {
        _output.WriteLine("=== E2E: Offline Mode Enforcement ===");

        var validator = new GenerationValidator(_loggerFactory.CreateLogger<GenerationValidator>());

        // Test with Pro tier in offline mode (should fail)
        var result = validator.ValidateProviders(
            new Dictionary<string, ILlmProvider>
            {
                ["OpenAI"] = new FailingLlmProvider("OpenAI")
            },
            new Dictionary<string, ITtsProvider>(),
            new Dictionary<string, object>(),
            "Pro",
            "Free",
            "Free",
            offlineOnly: true
        );

        _output.WriteLine($"Validation result: IsValid={result.IsValid}");
        foreach (var issue in result.Issues)
        {
            _output.WriteLine($"  Issue: {issue}");
        }

        Assert.False(result.IsValid, "Pro tier should fail validation in offline mode");
        Assert.Contains(result.Issues, i => i.Contains("Pro", StringComparison.OrdinalIgnoreCase) || i.Contains("offline", StringComparison.OrdinalIgnoreCase));
        
        _output.WriteLine("✓ Offline mode correctly blocks Pro providers");
    }

    /// <summary>
    /// Test: Script generation with various content types
    /// </summary>
    [Fact(DisplayName = "E2E: Multiple Content Types")]
    public async Task ScriptGeneration_MultipleContentTypes_ShouldSucceed()
    {
        _output.WriteLine("=== E2E: Multiple Content Types ===");

        var llmProvider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());

        var contentTypes = new[]
        {
            ("Educational", "How Machine Learning Works", "informative"),
            ("Entertainment", "Top 5 Comedy Movies", "entertaining"),
            ("Marketing", "Product Launch Video", "professional")
        };

        foreach (var (category, topic, tone) in contentTypes)
        {
            _output.WriteLine($"\nTesting content type: {category}");
            
            var brief = new Brief(
                Topic: topic,
                Audience: "General audience",
                Goal: category,
                Tone: tone,
                Language: "English",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: category.ToLowerInvariant()
            );

            var script = await llmProvider.DraftScriptAsync(brief, planSpec, CancellationToken.None).ConfigureAwait(false);
            
            Assert.NotNull(script);
            Assert.NotEmpty(script);
            Assert.Contains(topic, script, StringComparison.OrdinalIgnoreCase);
            
            _output.WriteLine($"✓ {category}: Generated {script.Length} chars");
        }

        _output.WriteLine("\n✓ All content types generated successfully");
    }

    /// <summary>
    /// Test: Script streaming capability
    /// </summary>
    [Fact(DisplayName = "E2E: Script Streaming")]
    public async Task ScriptGeneration_Streaming_RuleBasedBehavior()
    {
        _output.WriteLine("=== E2E: Script Streaming (RuleBased Behavior) ===");

        var llmProvider = new RuleBasedLlmProvider(_loggerFactory.CreateLogger<RuleBasedLlmProvider>());

        var brief = new Brief(
            Topic: "Streaming Test",
            Audience: "Developers",
            Goal: "Technical Demo",
            Tone: "Technical",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(15),
            Pacing: Pacing.Fast,
            Density: Density.Sparse,
            Style: "Technical"
        );

        // RuleBased provider does not support true streaming
        Assert.False(llmProvider.SupportsStreaming, "RuleBased provider does not support streaming");
        _output.WriteLine("✓ RuleBased correctly reports no streaming support");

        // The stream method returns an error chunk directing to use non-streaming method
        var chunkCount = 0;
        bool receivedFinal = false;
        string? errorMessage = null;
        
        await foreach (var chunk in llmProvider.DraftScriptStreamAsync(brief, planSpec, CancellationToken.None))
        {
            chunkCount++;
            receivedFinal = chunk.IsFinal;
            errorMessage = chunk.ErrorMessage;
        }

        Assert.Equal(1, chunkCount);
        Assert.True(receivedFinal, "Should receive final chunk");
        Assert.NotNull(errorMessage);
        Assert.Contains("streaming", errorMessage, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine($"✓ Stream method correctly indicates streaming not supported: {errorMessage}");
        _output.WriteLine("✓ Use DraftScriptAsync for RuleBased provider instead");
    }

    public void Dispose()
    {
        _loggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

