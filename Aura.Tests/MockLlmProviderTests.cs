using Xunit;
using Aura.Providers.Llm;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

/// <summary>
/// Tests for MockLlmProvider to ensure it behaves correctly in testing scenarios
/// </summary>
public class MockLlmProviderTests
{
    private readonly Brief _testBrief = new(
        Topic: "Test Video Topic",
        Audience: "Developers",
        Goal: "Educate",
        Tone: "Professional",
        Language: "English",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new(
        TargetDuration: TimeSpan.FromMinutes(2),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Modern"
    );

    [Fact]
    public async Task DraftScriptAsync_WithSuccessBehavior_ReturnsScript()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(_testBrief.Topic, result);
        Assert.Contains("Hook", result);
        Assert.Contains("Introduction", result);
        Assert.Contains("Conclusion", result);
    }

    [Fact]
    public async Task DraftScriptAsync_WithFailureBehavior_ThrowsException()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Failure
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
        
        Assert.Contains("configured to fail", exception.Message);
    }

    [Fact]
    public async Task DraftScriptAsync_WithTimeoutBehavior_ThrowsTaskCanceledException()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Timeout
        );

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DraftScriptAsync_WithEmptyResponseBehavior_ReturnsEmptyString()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.EmptyResponse
        );

        // Act
        var result = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DraftScriptAsync_WithSimulatedLatency_DelaysExecution()
    {
        // Arrange
        var latency = TimeSpan.FromMilliseconds(100);
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        )
        {
            SimulatedLatency = latency
        };

        // Act
        var startTime = DateTime.UtcNow;
        await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(duration >= latency, $"Expected at least {latency.TotalMilliseconds}ms delay, but only waited {duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task CompleteAsync_WithSuccessBehavior_ReturnsJsonResponse()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );
        var prompt = "Generate a test response";

        // Act
        var result = await provider.CompleteAsync(prompt, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("response", result);
        Assert.Contains("confidence", result);
        Assert.Contains("reasoning", result);
    }

    [Fact]
    public async Task AnalyzeSceneImportanceAsync_WithSuccessBehavior_ReturnsAnalysis()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.AnalyzeSceneImportanceAsync(
            "Scene text",
            "Previous scene text",
            "Video goal",
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75.0, result.Importance);
        Assert.Equal(60.0, result.Complexity);
        Assert.Equal(50.0, result.EmotionalIntensity);
        Assert.Equal("medium", result.InformationDensity);
        Assert.Equal(10.0, result.OptimalDurationSeconds);
        Assert.NotEmpty(result.Reasoning);
    }

    [Fact]
    public async Task AnalyzeSceneImportanceAsync_WithNullResponseBehavior_ReturnsNull()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.NullResponse
        );

        // Act
        var result = await provider.AnalyzeSceneImportanceAsync(
            "Scene text",
            null,
            "Video goal",
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateVisualPromptAsync_WithSuccessBehavior_ReturnsVisualPrompt()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.GenerateVisualPromptAsync(
            "Scene text about technology",
            null,
            "Professional",
            VisualStyle.Modern,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.DetailedDescription);
        Assert.NotEmpty(result.CompositionGuidelines);
        Assert.NotEmpty(result.ColorPalette);
        Assert.NotEmpty(result.StyleKeywords);
        Assert.Equal("medium shot", result.ShotType);
        Assert.Equal("eye level", result.CameraAngle);
    }

    [Fact]
    public async Task AnalyzeContentComplexityAsync_WithSuccessBehavior_ReturnsComplexityAnalysis()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.AnalyzeContentComplexityAsync(
            "Complex scene text",
            null,
            "Educate viewers",
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(65.0, result.OverallComplexityScore);
        Assert.Equal(55.0, result.ConceptDifficulty);
        Assert.Equal(3, result.NewConceptsIntroduced);
        Assert.Equal(8.0, result.CognitiveProcessingTimeSeconds);
        Assert.NotEmpty(result.DetailedBreakdown);
    }

    [Fact]
    public async Task AnalyzeSceneCoherenceAsync_WithSuccessBehavior_ReturnsCoherenceAnalysis()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.AnalyzeSceneCoherenceAsync(
            "From scene",
            "To scene",
            "Video goal",
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(80.0, result.CoherenceScore);
        Assert.Contains("sequential", result.ConnectionTypes);
        Assert.Equal(0.85, result.ConfidenceScore);
        Assert.NotEmpty(result.Reasoning);
    }

    [Fact]
    public async Task ValidateNarrativeArcAsync_WithSuccessBehavior_ReturnsValidation()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );
        var scenes = new List<string>
        {
            "Scene 1",
            "Scene 2",
            "Scene 3"
        };

        // Act
        var result = await provider.ValidateNarrativeArcAsync(
            scenes,
            "Educational video",
            "tutorial",
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.DetectedStructure);
        Assert.NotEmpty(result.ExpectedStructure);
        Assert.NotNull(result.Recommendations);
    }

    [Fact]
    public async Task GenerateTransitionTextAsync_WithSuccessBehavior_ReturnsTransition()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        var result = await provider.GenerateTransitionTextAsync(
            "From scene",
            "To scene",
            "Video goal",
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CallHistory_TracksMethodInvocations()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        await provider.CompleteAsync("test", CancellationToken.None);
        await provider.AnalyzeSceneImportanceAsync("scene", null, "goal", CancellationToken.None);

        // Assert
        Assert.Equal(3, provider.CallHistory.Count);
        Assert.Contains(provider.CallHistory, call => call.Contains("DraftScriptAsync"));
        Assert.Contains(provider.CallHistory, call => call.Contains("CompleteAsync"));
        Assert.Contains(provider.CallHistory, call => call.Contains("AnalyzeSceneImportanceAsync"));
    }

    [Fact]
    public async Task CallCounts_TracksMethodCallCounts()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );

        // Act
        await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);
        await provider.CompleteAsync("test", CancellationToken.None);

        // Assert
        Assert.Equal(2, provider.CallCounts["DraftScriptAsync"]);
        Assert.Equal(1, provider.CallCounts["CompleteAsync"]);
    }

    [Fact]
    public void ResetCallTracking_ClearsHistoryAndCounts()
    {
        // Arrange
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            MockBehavior.Success
        );
        provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None).Wait();

        // Act
        provider.ResetCallTracking();

        // Assert
        Assert.Empty(provider.CallHistory);
        Assert.Empty(provider.CallCounts);
    }

    [Theory]
    [InlineData(MockBehavior.Success)]
    [InlineData(MockBehavior.Failure)]
    [InlineData(MockBehavior.Timeout)]
    [InlineData(MockBehavior.NullResponse)]
    [InlineData(MockBehavior.EmptyResponse)]
    public void Constructor_WithDifferentBehaviors_InitializesCorrectly(MockBehavior behavior)
    {
        // Act
        var provider = new MockLlmProvider(
            NullLogger<MockLlmProvider>.Instance,
            behavior
        );

        // Assert
        Assert.NotNull(provider);
        Assert.Empty(provider.CallHistory);
        Assert.Empty(provider.CallCounts);
    }
}
