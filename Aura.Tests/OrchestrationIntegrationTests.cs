using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for the full LLM orchestration pipeline with schema validation
/// </summary>
public class OrchestrationIntegrationTests
{
    private readonly LlmOrchestrationService _orchestration;
    private readonly Mock<ILlmProvider> _mockProvider;
    private readonly StructuredLlmProviderAdapter _adapter;

    public OrchestrationIntegrationTests()
    {
        var orchestrationLogger = new Mock<ILogger<LlmOrchestrationService>>();
        var validatorLogger = new Mock<ILogger<SchemaValidator>>();
        var adapterLogger = new Mock<ILogger<StructuredLlmProviderAdapter>>();
        
        var validator = new SchemaValidator(validatorLogger.Object);
        _orchestration = new LlmOrchestrationService(orchestrationLogger.Object, validator);
        
        _mockProvider = new Mock<ILlmProvider>();
        _adapter = new StructuredLlmProviderAdapter(
            adapterLogger.Object,
            _orchestration,
            _mockProvider.Object
        );
    }

    [Fact]
    public async Task FullPipeline_BriefToPlan_WithValidation_Succeeds()
    {
        // Arrange
        var brief = new Brief(
            "AI and Machine Learning",
            "Software Engineers",
            "Educate about ML basics",
            "Professional",
            "English",
            Aspect.Widescreen16x9
        );
        
        var planSpec = new PlanSpec(
            TimeSpan.FromMinutes(2),
            Pacing.Conversational,
            Density.Balanced,
            "Educational"
        );

        var validPlanJson = @"{
            ""outline"": ""Introduction to AI and Machine Learning: covering fundamentals, applications, and future directions in a clear, accessible manner"",
            ""sceneCount"": 5,
            ""estimatedDurationSeconds"": 120.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-act structure: hook, education, call-to-action"",
            ""keyMessages"": [
                ""AI is transforming industries"",
                ""ML enables computers to learn from data"",
                ""Understanding basics is crucial""
            ],
            ""schema_version"": ""1.0""
        }";

        _mockProvider
            .Setup(p => p.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validPlanJson);

        // Act
        var result = await _adapter.GeneratePlanAsync(brief, planSpec);

        // Assert
        _mockProvider.Verify(p => p.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.SceneCount);
        Assert.Equal("moderate", result.Data.TargetPacing.ToLowerInvariant());
        Assert.Equal(3, result.Data.KeyMessages.Length);
        Assert.NotNull(result.Data.Metadata);
        Assert.Equal("1.0", result.Data.SchemaVersion);
    }

    [Fact]
    public async Task FullPipeline_BriefToPlanToScenes_WithValidation_Succeeds()
    {
        // Arrange - First generate plan
        var brief = new Brief(
            "Quantum Computing",
            "Tech Enthusiasts",
            "Introduce quantum concepts",
            "Engaging",
            "English",
            Aspect.Widescreen16x9
        );
        
        var planSpec = new PlanSpec(
            TimeSpan.FromMinutes(3),
            Pacing.Fast,
            Density.Dense,
            "Tech Explainer"
        );

        var validPlanJson = @"{
            ""outline"": ""Quantum Computing Explained: from superposition to real-world applications, demystifying the technology"",
            ""sceneCount"": 4,
            ""estimatedDurationSeconds"": 180.0,
            ""targetPacing"": ""fast"",
            ""contentDensity"": ""dense"",
            ""narrativeStructure"": ""Progressive complexity: start simple, build understanding"",
            ""keyMessages"": [
                ""Quantum computing uses qubits"",
                ""Superposition enables parallel processing""
            ],
            ""schema_version"": ""1.0""
        }";

        var validScenesJson = @"{
            ""scenes"": [
                {
                    ""index"": 0,
                    ""heading"": ""Introduction"",
                    ""script"": ""Welcome to our journey into quantum computing"",
                    ""durationSeconds"": 45.0,
                    ""purpose"": ""Hook the viewer and set expectations"",
                    ""transitionType"": ""fade""
                },
                {
                    ""index"": 1,
                    ""heading"": ""What is a Qubit?"",
                    ""script"": ""Unlike classical bits, qubits can exist in superposition"",
                    ""durationSeconds"": 60.0,
                    ""purpose"": ""Explain fundamental concept of qubits""
                },
                {
                    ""index"": 2,
                    ""heading"": ""Real Applications"",
                    ""script"": ""Quantum computers are solving real problems today"",
                    ""durationSeconds"": 50.0,
                    ""purpose"": ""Show practical relevance and impact""
                },
                {
                    ""index"": 3,
                    ""heading"": ""The Future"",
                    ""script"": ""The quantum revolution has just begun"",
                    ""durationSeconds"": 25.0,
                    ""purpose"": ""Inspire and provide call to action"",
                    ""transitionType"": ""dissolve""
                }
            ],
            ""schema_version"": ""1.0""
        }";

        _mockProvider
            .Setup(p => p.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validPlanJson);

        // Act - Generate plan
        var planResult = await _adapter.GeneratePlanAsync(brief, planSpec);
        Assert.True(planResult.Success);

        // Setup for scene generation
        _mockProvider.Reset();
        _mockProvider
            .Setup(p => p.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validScenesJson);

        // Act - Generate scenes
        var scenesResult = await _adapter.GenerateScenesAsync(planResult.Data!, brief, planSpec);

        // Assert
        Assert.True(scenesResult.Success, scenesResult.ErrorMessage);
        Assert.NotNull(scenesResult.Data);
        Assert.Equal(4, scenesResult.Data.Scenes.Length);
        
        // Verify scene structure
        var firstScene = scenesResult.Data.Scenes[0];
        Assert.Equal(0, firstScene.Index);
        Assert.Equal("Introduction", firstScene.Heading);
        Assert.Equal(45.0, firstScene.DurationSeconds);
        Assert.Equal("fade", firstScene.TransitionType);
        
        // Verify metadata
        Assert.NotNull(scenesResult.Data.Metadata);
        Assert.Equal("1.0", scenesResult.Data.SchemaVersion);
    }

    [Fact]
    public async Task FullPipeline_InvalidOutputWithAutoRepair_RetriesAndSucceeds()
    {
        // Arrange
        var brief = new Brief("Test", "General", "Test", "Neutral", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Test");

        var invalidJson = @"{
            ""outline"": ""Too short"",
            ""sceneCount"": 100,
            ""estimatedDurationSeconds"": 60.0,
            ""targetPacing"": ""invalid_pacing"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Short"",
            ""keyMessages"": [],
            ""schema_version"": ""1.0""
        }";

        var validJson = @"{
            ""outline"": ""This is a proper outline that meets the minimum length requirement of fifty characters"",
            ""sceneCount"": 3,
            ""estimatedDurationSeconds"": 60.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Simple three-part structure"",
            ""keyMessages"": [""Message one"", ""Message two""],
            ""schema_version"": ""1.0""
        }";

        var attempt = 0;
        _mockProvider
            .Setup(p => p.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => 
            {
                attempt++;
                return attempt == 1 ? invalidJson : validJson;
            });

        // Act
        var result = await _adapter.GeneratePlanAsync(
            brief, 
            planSpec,
            new OrchestrationConfig(MaxRetries: 3, EnableAutoRepair: true)
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.SceneCount);
    }

    [Fact]
    public async Task FullPipeline_MaxRetriesExceeded_ReturnsFailure()
    {
        // Arrange
        var brief = new Brief("Test", "General", "Test", "Neutral", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Test");

        var invalidJson = @"{
            ""outline"": ""Short"",
            ""sceneCount"": 1,
            ""estimatedDurationSeconds"": 60.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Short"",
            ""keyMessages"": [""msg""],
            ""schema_version"": ""1.0""
        }";

        _mockProvider
            .Setup(p => p.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidJson);

        // Act
        var result = await _adapter.GeneratePlanAsync(
            brief,
            planSpec,
            new OrchestrationConfig(MaxRetries: 2, EnableAutoRepair: true)
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal(2, result.AttemptsUsed);
        Assert.NotEmpty(result.ValidationErrors);
        Assert.Contains("Outline", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task FullPipeline_WithMetadata_TracksProviderAndModel()
    {
        // Arrange
        var brief = new Brief("Test", "General", "Test", "Neutral", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Test");

        var validJson = @"{
            ""outline"": ""A comprehensive outline that provides detailed information about the topic"",
            ""sceneCount"": 3,
            ""estimatedDurationSeconds"": 60.0,
            ""targetPacing"": ""moderate"",
            ""contentDensity"": ""moderate"",
            ""narrativeStructure"": ""Three-part narrative structure"",
            ""keyMessages"": [""Key message one"", ""Key message two""],
            ""schema_version"": ""1.0""
        }";

        _mockProvider
            .Setup(p => p.CompleteAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validJson);

        // Act
        var result = await _adapter.GeneratePlanAsync(brief, planSpec);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Metadata);
        Assert.NotNull(result.Data.Metadata.Provider);
        Assert.NotNull(result.Data.Metadata.Model);
        Assert.True(result.Data.Metadata.Timestamp <= DateTime.UtcNow);
        Assert.True(result.Data.Metadata.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task OrchestrationService_RegisterCustomStep_CanExecute()
    {
        // Arrange
        _orchestration.RegisterStep("custom_test", "Input", "Output");
        
        var validJson = @"{
            ""outline"": ""Custom step output that meets all requirements for validation"",
            ""sceneCount"": 2,
            ""estimatedDurationSeconds"": 30.0,
            ""targetPacing"": ""fast"",
            ""contentDensity"": ""sparse"",
            ""narrativeStructure"": ""Simple structure"",
            ""keyMessages"": [""Message""],
            ""schema_version"": ""1.0""
        }";

        // Act
        var result = await _orchestration.ExecuteStepAsync<PlanSchema>(
            "custom_test",
            (prompt, ct) => Task.FromResult(validJson),
            new OrchestrationConfig()
        );

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public void OrchestrationService_HasDefaultSteps_Registered()
    {
        // Act
        var steps = _orchestration.GetAllSteps();

        // Assert
        Assert.Contains("brief_to_plan", steps.Keys);
        Assert.Contains("plan_to_scenes", steps.Keys);
        Assert.Contains("scenes_to_voice", steps.Keys);
        Assert.Contains("scenes_to_visuals", steps.Keys);
        Assert.Contains("voice_to_ssml", steps.Keys);
        Assert.Contains("assets_to_timeline", steps.Keys);
    }
}
