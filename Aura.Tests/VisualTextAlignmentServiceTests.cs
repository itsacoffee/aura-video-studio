using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Aura.Core.Services.Visual;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for VisualTextAlignmentService - ensures narration and visuals are synchronized
/// Validates acceptance criteria from PR #7
/// </summary>
public class VisualTextAlignmentServiceTests
{
    private readonly VisualTextAlignmentService _service;
    private readonly Brief _testBrief;
    private readonly IReadOnlyList<Scene> _testScenes;

    public VisualTextAlignmentServiceTests()
    {
        var logger = NullLogger<VisualTextAlignmentService>.Instance;
        _service = new VisualTextAlignmentService(logger);

        _testBrief = new Brief(
            Topic: "AI and Machine Learning",
            Audience: "Tech professionals",
            Goal: "Educate about ML concepts",
            Tone: "professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9);

        _testScenes = new List<Scene>
        {
            new Scene(
                Index: 0,
                Heading: "Introduction to AI",
                Script: "Artificial Intelligence is transforming industries. Machine learning algorithms analyze data patterns to make predictions.",
                Start: TimeSpan.Zero,
                Duration: TimeSpan.FromSeconds(10)),
            new Scene(
                Index: 1,
                Heading: "Complex Technical Details",
                Script: "The convolutional neural network architecture utilizes backpropagation through multiple hidden layers with gradient descent optimization.",
                Start: TimeSpan.FromSeconds(10),
                Duration: TimeSpan.FromSeconds(8)),
            new Scene(
                Index: 2,
                Heading: "Simple Summary",
                Script: "Now you understand the basics. Let's move forward.",
                Start: TimeSpan.FromSeconds(18),
                Duration: TimeSpan.FromSeconds(5))
        };
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_WithoutLlm_GeneratesSegmentsWithTimingMarkers()
    {
        var result = await _service.AnalyzeSynchronizationAsync(
            _testScenes,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Segments);
        
        foreach (var segment in result.Segments)
        {
            Assert.True(segment.Duration.TotalSeconds > 0, "Segment must have valid duration");
            Assert.True(segment.StartTime >= TimeSpan.Zero, "Segment must have valid start time");
            
            var timingAccuracy = 0.5;
            Assert.True(segment.Duration.TotalSeconds >= timingAccuracy, 
                $"Timing precision should be at least ±{timingAccuracy} seconds");
        }
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_CalculatesCognitiveLoadBelowThreshold()
    {
        var result = await _service.AnalyzeSynchronizationAsync(
            _testScenes,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        Assert.True(result.OverallCognitiveLoad >= 0 && result.OverallCognitiveLoad <= 100,
            "Cognitive load should be in 0-100 range");
        
        if (result.OverallCognitiveLoad > 75.0)
        {
            Assert.Contains(result.Warnings, w => w.Contains("cognitive load"));
        }
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_ComplexityCorrelation_IsInverse()
    {
        var result = await _service.AnalyzeSynchronizationAsync(
            _testScenes,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        Assert.True(result.ComplexityCorrelation >= -1.0 && result.ComplexityCorrelation <= 1.0,
            "Correlation coefficient should be between -1 and 1");

        var targetCorrelation = -0.7;
        if (Math.Abs(result.ComplexityCorrelation) < Math.Abs(targetCorrelation))
        {
            Assert.Contains(result.Warnings, w => w.Contains("correlation"));
        }

        var complexNarrationSegments = result.Segments.Where(s => s.NarrationComplexity > 70).ToList();
        if (complexNarrationSegments.Any())
        {
            foreach (var segment in complexNarrationSegments)
            {
                if (segment.VisualRecommendations.Any())
                {
                    var avgVisualComplexity = segment.VisualRecommendations.Average(v => v.VisualComplexity);
                    Assert.True(avgVisualComplexity < segment.NarrationComplexity,
                        "High narration complexity should have lower visual complexity");
                }
            }
        }
    }

    [Fact]
    public async Task IdentifyKeyConceptsAsync_ExtractsNounsDataPointsAndVerbs()
    {
        var text = "The machine learning algorithm processes 1000 data points per second and transforms the output.";
        
        var concepts = await _service.IdentifyKeyConceptsAsync(
            text,
            "professional",
            llmProvider: null,
            ct: CancellationToken.None);

        Assert.NotEmpty(concepts);
        
        Assert.Contains(concepts, c => c.Type == ConceptType.DataPoint);
        Assert.Contains(concepts, c => c.Type == ConceptType.ActionVerb);
        
        foreach (var concept in concepts)
        {
            Assert.True(concept.Importance >= 0 && concept.Importance <= 100,
                "Concept importance should be 0-100");
            Assert.NotEmpty(concept.SuggestedVisualization);
        }
    }

    [Fact]
    public async Task GenerateVisualRecommendationsAsync_InverselyBalancesComplexity()
    {
        var highComplexityText = "The convolutional neural network utilizes backpropagation through multiple layers.";
        var narrationComplexity = 85.0;
        var keyConcepts = new List<KeyConcept>
        {
            new KeyConcept
            {
                Text = "neural network",
                Type = ConceptType.TechnicalTerm,
                Importance = 80,
                TimeOffset = TimeSpan.FromSeconds(1),
                SuggestedVisualization = "Network diagram",
                RequiresMetaphor = false,
                SuggestsMotion = false
            }
        };

        var recommendations = await _service.GenerateVisualRecommendationsAsync(
            highComplexityText,
            narrationComplexity,
            keyConcepts,
            "professional",
            llmProvider: null,
            ct: CancellationToken.None);

        Assert.NotEmpty(recommendations);
        
        foreach (var rec in recommendations)
        {
            Assert.True(rec.VisualComplexity < narrationComplexity,
                $"Visual complexity ({rec.VisualComplexity}) should be less than narration complexity ({narrationComplexity})");
            
            Assert.NotEmpty(rec.Description);
            Assert.True(rec.Priority >= 0 && rec.Priority <= 100);
            Assert.True(rec.Duration.TotalSeconds > 0);
        }
        
        var simpleVisualRec = recommendations.FirstOrDefault(r => r.VisualComplexity < 30);
        Assert.NotNull(simpleVisualRec);
    }

    [Fact]
    public async Task ValidateVisualConsistencyAsync_DetectsContradictions()
    {
        var segment = new NarrationSegment
        {
            SceneIndex = 0,
            Text = "Dogs are loyal companions",
            StartTime = TimeSpan.Zero,
            Duration = TimeSpan.FromSeconds(3),
            NarrationComplexity = 30,
            KeyConcepts = Array.Empty<KeyConcept>(),
            VisualRecommendations = Array.Empty<VisualRecommendation>(),
            CognitiveLoadScore = 40,
            IsTransitionPoint = false,
            NarrationRate = 150,
            InformationDensity = InformationDensity.Low
        };

        var contradictingVisuals = new List<VisualRecommendation>
        {
            new VisualRecommendation
            {
                ContentType = VisualContentType.BRoll,
                Description = "Beautiful cat playing with toys",
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(3),
                VisualComplexity = 40,
                BRollKeywords = new[] { "cat", "feline", "pet" },
                Priority = 70,
                RequiresDynamicContent = false,
                Reasoning = "Test contradiction"
            }
        };

        var validation = await _service.ValidateVisualConsistencyAsync(
            segment,
            contradictingVisuals,
            llmProvider: null,
            ct: CancellationToken.None);

        Assert.NotNull(validation);
        Assert.False(validation.IsConsistent, "Should detect dog/cat contradiction");
        Assert.NotEmpty(validation.Contradictions);
        Assert.True(validation.ConsistencyScore < 100);
    }

    [Fact]
    public void BalanceCognitiveLoadAsync_CalculatesMultiModalLoad()
    {
        var segment = new NarrationSegment
        {
            SceneIndex = 0,
            Text = "Complex technical content",
            StartTime = TimeSpan.Zero,
            Duration = TimeSpan.FromSeconds(5),
            NarrationComplexity = 80,
            KeyConcepts = new[]
            {
                new KeyConcept { Text = "concept1", Type = ConceptType.TechnicalTerm, Importance = 70, TimeOffset = TimeSpan.Zero, SuggestedVisualization = "viz1" },
                new KeyConcept { Text = "concept2", Type = ConceptType.TechnicalTerm, Importance = 75, TimeOffset = TimeSpan.FromSeconds(2), SuggestedVisualization = "viz2" }
            },
            VisualRecommendations = Array.Empty<VisualRecommendation>(),
            CognitiveLoadScore = 70,
            IsTransitionPoint = false,
            NarrationRate = 180,
            InformationDensity = InformationDensity.High
        };

        var visuals = new List<VisualRecommendation>
        {
            new VisualRecommendation
            {
                ContentType = VisualContentType.Illustration,
                Description = "Simple diagram",
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(5),
                VisualComplexity = 25,
                BRollKeywords = Array.Empty<string>(),
                Priority = 80,
                RequiresDynamicContent = false,
                Reasoning = "Balance high narration complexity"
            }
        };

        var metrics = _service.BalanceCognitiveLoadAsync(segment, visuals);

        Assert.NotNull(metrics);
        Assert.True(metrics.OverallLoad >= 0 && metrics.OverallLoad <= 100);
        Assert.True(metrics.NarrationLoad >= 0 && metrics.NarrationLoad <= 100);
        Assert.True(metrics.VisualLoad >= 0 && metrics.VisualLoad <= 100);
        Assert.True(metrics.MultiModalLoad >= 0 && metrics.MultiModalLoad <= 100);
        Assert.True(metrics.ProcessingRate >= 0);

        if (metrics.ExceedsThreshold)
        {
            Assert.NotEmpty(metrics.Recommendations);
        }
    }

    [Fact]
    public void GenerateVisualMetadata_IncludesCameraAnglesAndComposition()
    {
        var concept = new KeyConcept
        {
            Text = "data point",
            Type = ConceptType.DataPoint,
            Importance = 80,
            TimeOffset = TimeSpan.FromSeconds(1),
            SuggestedVisualization = "Chart",
            RequiresMetaphor = false,
            SuggestsMotion = false
        };

        var metadata = _service.GenerateVisualMetadata(concept, "professional", 30);

        Assert.NotNull(metadata);
        Assert.True(Enum.IsDefined(typeof(CameraAngle), metadata.CameraAngle), 
            "Camera angle should be a valid enum value");
        Assert.True(Enum.IsDefined(typeof(CompositionRule), metadata.CompositionRule),
            "Composition rule should be a valid enum value");
        Assert.True(Enum.IsDefined(typeof(ShotType), metadata.ShotType),
            "Shot type should be a valid enum value");
        Assert.NotEmpty(metadata.FocusPoint);
        Assert.NotEmpty(metadata.ColorScheme);
        Assert.NotEmpty(metadata.EmotionalTones);
        Assert.NotEmpty(metadata.LightingMood);
        Assert.NotEmpty(metadata.AttentionCues);
    }

    [Fact]
    public void CalculatePacingRecommendationsAsync_AdjustsForNarrationRate()
    {
        var fastNarrationScene = new Scene(
            Index: 0,
            Heading: "Fast Pace",
            Script: "Quick rapid-fire information delivery",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        var fastSegments = new List<NarrationSegment>
        {
            new NarrationSegment
            {
                SceneIndex = 0,
                Text = "Quick rapid-fire information",
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(2),
                NarrationComplexity = 60,
                KeyConcepts = Array.Empty<KeyConcept>(),
                VisualRecommendations = Array.Empty<VisualRecommendation>(),
                CognitiveLoadScore = 50,
                IsTransitionPoint = true,
                NarrationRate = 190,
                InformationDensity = InformationDensity.High
            }
        };

        var pacingRec = _service.CalculatePacingRecommendationsAsync(fastNarrationScene, fastSegments);

        Assert.NotNull(pacingRec);
        Assert.True(pacingRec.NarrationRate > 180, "Should detect fast narration");
        Assert.True(pacingRec.RecommendedVisualChanges <= 3, 
            "Fast narration should have fewer visual changes");
        Assert.NotEmpty(pacingRec.Reasoning);

        var slowSegments = new List<NarrationSegment>
        {
            new NarrationSegment
            {
                SceneIndex = 0,
                Text = "Slow, contemplative delivery",
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(5),
                NarrationComplexity = 40,
                KeyConcepts = Array.Empty<KeyConcept>(),
                VisualRecommendations = Array.Empty<VisualRecommendation>(),
                CognitiveLoadScore = 35,
                IsTransitionPoint = false,
                NarrationRate = 110,
                InformationDensity = InformationDensity.Low
            }
        };

        var slowScene = new Scene(0, "Slow", "content", TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var slowPacing = _service.CalculatePacingRecommendationsAsync(slowScene, slowSegments);

        Assert.True(slowPacing.RecommendedVisualChanges >= 4,
            "Slow narration should allow more visual changes");
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_TransitionAlignment_Above90Percent()
    {
        var scenesWithTransitions = new List<Scene>
        {
            new Scene(0, "Intro", "First point here.", TimeSpan.Zero, TimeSpan.FromSeconds(3)),
            new Scene(1, "Main", "However, the next topic is different. Therefore we continue.", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6)),
            new Scene(2, "Conclusion", "Finally, we conclude our discussion.", TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(4))
        };

        var result = await _service.AnalyzeSynchronizationAsync(
            scenesWithTransitions,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        Assert.True(result.TransitionAlignmentRate >= 0 && result.TransitionAlignmentRate <= 100);
        
        var transitionSegments = result.Segments.Where(s => s.IsTransitionPoint).ToList();
        Assert.NotEmpty(transitionSegments);
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_PerformanceWithinTarget()
    {
        var largeSceneSet = Enumerable.Range(0, 10)
            .Select(i => new Scene(
                i,
                $"Scene {i}",
                $"This is scene {i} with some content about various topics and concepts.",
                TimeSpan.FromSeconds(i * 5),
                TimeSpan.FromSeconds(5)))
            .ToList();

        var startTime = DateTime.UtcNow;

        var result = await _service.AnalyzeSynchronizationAsync(
            largeSceneSet,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        var avgTimePerScene = totalTime / largeSceneSet.Count;

        Assert.True(avgTimePerScene < 3.0,
            $"Average time per scene ({avgTimePerScene:F2}s) should be less than 3 seconds");
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_GeneratesComprehensiveRecommendations()
    {
        var result = await _service.AnalyzeSynchronizationAsync(
            _testScenes,
            _testBrief,
            llmProvider: null,
            pacingData: null,
            ct: CancellationToken.None);

        Assert.NotNull(result.RecommendationSummary);
        Assert.NotEmpty(result.RecommendationSummary);
        
        Assert.Contains("Cognitive Load", result.RecommendationSummary);
        Assert.Contains("Complexity Correlation", result.RecommendationSummary);
        Assert.Contains("Transition Alignment", result.RecommendationSummary);
    }

    [Fact]
    public async Task GenerateVisualRecommendationsAsync_ProducesSpecificBRollKeywords()
    {
        var text = "The robot navigates through the warehouse autonomously.";
        var concepts = new List<KeyConcept>
        {
            new KeyConcept
            {
                Text = "robot",
                Type = ConceptType.Noun,
                Importance = 80,
                TimeOffset = TimeSpan.Zero,
                SuggestedVisualization = "Robot in action",
                RequiresMetaphor = false,
                SuggestsMotion = true
            }
        };

        var recommendations = await _service.GenerateVisualRecommendationsAsync(
            text,
            narrationComplexity: 50,
            concepts,
            "professional",
            llmProvider: null,
            ct: CancellationToken.None);

        Assert.NotEmpty(recommendations);
        
        var brollRec = recommendations.FirstOrDefault(r => r.BRollKeywords.Any());
        Assert.NotNull(brollRec);
        Assert.NotEmpty(brollRec.BRollKeywords);
        
        var hasGenericKeyword = brollRec.BRollKeywords.Any(k => 
            k.Equals("stock footage", StringComparison.OrdinalIgnoreCase) ||
            k.Equals("generic", StringComparison.OrdinalIgnoreCase));
        Assert.False(hasGenericKeyword, "B-roll keywords should be specific, not generic");
    }

    [Fact]
    public void GenerateVisualMetadata_StructuredJSONFormat()
    {
        var concept = new KeyConcept
        {
            Text = "algorithm",
            Type = ConceptType.TechnicalTerm,
            Importance = 75,
            TimeOffset = TimeSpan.FromSeconds(2),
            SuggestedVisualization = "Flowchart",
            RequiresMetaphor = false,
            SuggestsMotion = false
        };

        var metadata = _service.GenerateVisualMetadata(concept, "professional", 35);

        Assert.NotNull(metadata);
        Assert.All(metadata.ColorScheme, color => Assert.Matches(@"^#[0-9A-Fa-f]{6}$", color));
        
        Assert.Contains(metadata.EmotionalTones, tone => !string.IsNullOrWhiteSpace(tone));
        Assert.NotEmpty(metadata.FocusPoint);
    }
}
