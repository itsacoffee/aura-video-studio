using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Aura.Core.Providers;
using Aura.Core.Services.Narrative;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for NarrativeFlowAnalyzer service
/// </summary>
public class NarrativeFlowAnalyzerTests
{
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly NarrativeFlowAnalyzer _analyzer;

    public NarrativeFlowAnalyzerTests()
    {
        _mockLlmProvider = new Mock<ILlmProvider>();
        var logger = NullLogger<NarrativeFlowAnalyzer>.Instance;
        _analyzer = new NarrativeFlowAnalyzer(logger, _mockLlmProvider.Object);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithTwoScenes_ReturnsOnePairwiseCoherence()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Welcome to AI tutorial", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Main", "AI uses machine learning", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 85,
                ConnectionTypes: new[] { ConnectionType.Thematic, ConnectionType.Sequential },
                ConfidenceScore: 0.9,
                Reasoning: "Strong thematic connection about AI"
            ));

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "introduction → body",
                ExpectedStructure: "introduction → body → conclusion",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: Array.Empty<string>(),
                Reasoning: "Good basic structure"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Teach AI basics", "tutorial");

        Assert.Single(result.PairwiseCoherence);
        Assert.Equal(85, result.PairwiseCoherence[0].CoherenceScore);
        Assert.Equal(85, result.OverallCoherenceScore);
        Assert.NotNull(result.ArcValidation);
        Assert.True(result.ArcValidation.IsValid);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithLowCoherence_DetectsCriticalIssue()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "AI", "AI is transforming technology", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Cooking", "Let's make pasta with tomato sauce", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 25,
                ConnectionTypes: Array.Empty<string>(),
                ConfidenceScore: 0.8,
                Reasoning: "No thematic connection between AI and cooking"
            ));

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: false,
                DetectedStructure: "disconnected topics",
                ExpectedStructure: "introduction → body → conclusion",
                StructuralIssues: new[] { "Abrupt topic change" },
                Recommendations: new[] { "Add transition or restructure" },
                Reasoning: "No coherent narrative arc"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "General video", "general");

        Assert.NotEmpty(result.ContinuityIssues);
        Assert.Contains(result.ContinuityIssues, i => i.Severity == IssueSeverity.Critical);
        Assert.Contains(result.ContinuityIssues, i => i.IssueType == "abrupt_transition");
        Assert.True(result.PairwiseCoherence[0].RequiresBridging);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithLowCoherence_GeneratesBridgingSuggestion()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Theory", "Machine learning theory is important", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Practice", "Now let's implement a neural network", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 65,
                ConnectionTypes: new[] { ConnectionType.Sequential },
                ConfidenceScore: 0.7,
                Reasoning: "Weak connection, needs bridging"
            ));

        _mockLlmProvider.Setup(p => p.GenerateTransitionTextAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("With this theoretical foundation in place, we can now move to practical implementation.");

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "theory → practice",
                ExpectedStructure: "overview → steps → summary",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: Array.Empty<string>(),
                Reasoning: "Good tutorial structure"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Teach ML", "tutorial");

        Assert.NotEmpty(result.BridgingSuggestions);
        var suggestion = result.BridgingSuggestions[0];
        Assert.Equal(0, suggestion.FromSceneIndex);
        Assert.Equal(1, suggestion.ToSceneIndex);
        Assert.Contains("theoretical foundation", suggestion.BridgingText);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WhenLlmFails_UsesFallbackAnalysis()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "AI Intro", "AI technology is amazing", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "AI Details", "AI technology enables automation", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SceneCoherenceResult?)null);

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NarrativeArcResult?)null);

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Learn AI", "educational");

        Assert.Single(result.PairwiseCoherence);
        Assert.NotNull(result.ArcValidation);
        Assert.Contains("Fallback", result.PairwiseCoherence[0].Reasoning);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithEducationalType_ValidatesCorrectArc()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Problem", "Why is climate change happening?", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Explanation", "Carbon emissions trap heat", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Solution", "We need renewable energy", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 90,
                ConnectionTypes: new[] { ConnectionType.Causal, ConnectionType.Thematic },
                ConfidenceScore: 0.95,
                Reasoning: "Strong causal connection"
            ));

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "problem → explanation → solution",
                ExpectedStructure: "problem → explanation → solution",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: Array.Empty<string>(),
                Reasoning: "Perfect educational structure"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Explain climate change", "educational");

        Assert.NotNull(result.ArcValidation);
        Assert.True(result.ArcValidation.IsValid);
        Assert.Equal("problem → explanation → solution", result.ArcValidation.ExpectedStructure);
        Assert.True(result.OverallCoherenceScore >= 85);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_CompletesWithinPerformanceTarget()
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < 10; i++)
        {
            scenes.Add(new Scene(i, $"Scene {i}", $"Content for scene {i}", 
                TimeSpan.FromSeconds(i * 5), TimeSpan.FromSeconds(5)));
        }

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 80,
                ConnectionTypes: new[] { ConnectionType.Sequential },
                ConfidenceScore: 0.8,
                Reasoning: "Sequential flow"
            ));

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "sequential",
                ExpectedStructure: "introduction → body → conclusion",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: Array.Empty<string>(),
                Reasoning: "Good structure"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Test video", "general");

        Assert.True(result.AnalysisDuration.TotalSeconds < 8, 
            $"Analysis took {result.AnalysisDuration.TotalSeconds:F2}s, expected < 8s");
        Assert.Equal(9, result.PairwiseCoherence.Count);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithSingleScene_ReturnsEmptyPairwiseCoherence()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Only", "Just one scene", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "single scene",
                ExpectedStructure: "introduction → body → conclusion",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: new[] { "Consider adding more scenes" },
                Reasoning: "Single scene video"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Brief video", "general");

        Assert.Empty(result.PairwiseCoherence);
        Assert.Equal(100, result.OverallCoherenceScore);
        Assert.Empty(result.BridgingSuggestions);
    }

    [Fact]
    public async Task AnalyzeNarrativeFlowAsync_WithWarningLevelCoherence_DetectsWarningIssue()
    {
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Introduction to topic", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Jump", "Advanced details without setup", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        _mockLlmProvider.Setup(p => p.AnalyzeSceneCoherenceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SceneCoherenceResult(
                CoherenceScore: 55,
                ConnectionTypes: new[] { ConnectionType.Sequential },
                ConfidenceScore: 0.7,
                Reasoning: "Some connection but weak"
            ));

        _mockLlmProvider.Setup(p => p.ValidateNarrativeArcAsync(
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NarrativeArcResult(
                IsValid: true,
                DetectedStructure: "intro → content",
                ExpectedStructure: "introduction → body → conclusion",
                StructuralIssues: Array.Empty<string>(),
                Recommendations: Array.Empty<string>(),
                Reasoning: "Acceptable structure"
            ));

        var result = await _analyzer.AnalyzeNarrativeFlowAsync(scenes, "Test", "general");

        Assert.NotEmpty(result.ContinuityIssues);
        Assert.Contains(result.ContinuityIssues, i => i.Severity == IssueSeverity.Warning);
        Assert.Contains(result.ContinuityIssues, i => i.IssueType == "weak_transition");
    }
}
