using System;
using System.Text.Json;
using Aura.Core.AI.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class SchemaValidationTests
{
    private readonly SchemaValidator _validator;

    public SchemaValidationTests()
    {
        _validator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
    }

    [Fact]
    public void ValidateSceneAnalysis_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var validJson = @"{
            ""importance"": 0.8,
            ""complexity"": 0.6,
            ""emotionalIntensity"": 0.7,
            ""informationDensity"": ""medium"",
            ""optimalDurationSeconds"": 5.5,
            ""transitionType"": ""fade"",
            ""reasoning"": ""This scene is important because it introduces the main concept.""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<SceneAnalysisSchema>(validJson);

        // Assert
        if (!result.IsValid)
        {
            var errorDetails = $"Validation failed with {result.ValidationErrors.Count} errors:\n" + 
                             string.Join("\n", result.ValidationErrors);
            Assert.Fail(errorDetails);
        }
        
        Assert.NotNull(data);
        Assert.Equal(0.8, data.Importance);
        Assert.Equal("medium", data.InformationDensity);
        Assert.Equal("fade", data.TransitionType);
        Assert.True(result.ValidationDuration.TotalMilliseconds < 150, 
            $"Validation took {result.ValidationDuration.TotalMilliseconds}ms, expected < 150ms");
    }

    [Fact]
    public void ValidateSceneAnalysis_InvalidRange_ReturnsErrors()
    {
        // Arrange
        var invalidJson = @"{
            ""importance"": 1.5,
            ""complexity"": -0.1,
            ""emotionalIntensity"": 0.7,
            ""informationDensity"": ""invalid"",
            ""optimalDurationSeconds"": 5.5,
            ""transitionType"": ""fade"",
            ""reasoning"": ""Valid""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<SceneAnalysisSchema>(invalidJson);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(data);
        Assert.Contains("Importance must be between 0.0 and 1.0", result.ErrorMessage);
        Assert.Contains("Complexity must be between 0.0 and 1.0", result.ErrorMessage);
    }

    [Fact]
    public void ValidateVisualPrompt_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var validJson = @"{
            ""detailedDescription"": ""A serene mountain landscape with snow-capped peaks"",
            ""compositionGuidelines"": ""Rule of thirds, leading lines"",
            ""lightingMood"": ""Golden hour warmth"",
            ""lightingDirection"": ""Side lighting"",
            ""lightingQuality"": ""Soft"",
            ""timeOfDay"": ""Sunset"",
            ""colorPalette"": [""#FF6B35"", ""#004E89"", ""#F7B267""],
            ""shotType"": ""Wide shot"",
            ""cameraAngle"": ""Eye level"",
            ""depthOfField"": ""Deep"",
            ""styleKeywords"": [""cinematic"", ""natural""],
            ""negativeElements"": [""people"", ""buildings""],
            ""continuityElements"": [""mountains"", ""sky""],
            ""reasoning"": ""This visual complements the narrative tone.""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<VisualPromptSchema>(validJson);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(data);
        Assert.Contains("mountain landscape", data.DetailedDescription);
        Assert.Equal(3, data.ColorPalette.Length);
        Assert.True(result.ValidationDuration.TotalMilliseconds < 150);
    }

    [Fact]
    public void ValidateVisualPrompt_MissingRequired_ReturnsErrors()
    {
        // Arrange
        var invalidJson = @"{
            ""detailedDescription"": ""Short"",
            ""compositionGuidelines"": ""X"",
            ""lightingMood"": ""OK"",
            ""colorPalette"": [],
            ""shotType"": ""OK"",
            ""reasoning"": ""Short""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<VisualPromptSchema>(invalidJson);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("DetailedDescription must be at least 20 characters", result.ErrorMessage);
        Assert.Contains("CompositionGuidelines must be at least 10 characters", result.ErrorMessage);
        Assert.Contains("ColorPalette must contain at least one color", result.ErrorMessage);
    }

    [Fact]
    public void ValidateContentComplexity_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var validJson = @"{
            ""overallComplexityScore"": 0.7,
            ""conceptDifficulty"": 0.6,
            ""terminologyDensity"": 0.5,
            ""prerequisiteKnowledgeLevel"": 0.4,
            ""multiStepReasoningRequired"": 0.8,
            ""newConceptsIntroduced"": 3,
            ""cognitiveProcessingTimeSeconds"": 15.5,
            ""optimalAttentionWindowSeconds"": 45.0,
            ""detailedBreakdown"": ""The content introduces several new concepts requiring sustained attention.""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<ContentComplexitySchema>(validJson);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(data);
        Assert.Equal(0.7, data.OverallComplexityScore);
        Assert.Equal(3, data.NewConceptsIntroduced);
        Assert.True(result.ValidationDuration.TotalMilliseconds < 150);
    }

    [Fact]
    public void ValidateSceneCoherence_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var validJson = @"{
            ""coherenceScore"": 0.85,
            ""connectionTypes"": [""logical"", ""temporal"", ""causal""],
            ""confidenceScore"": 0.9,
            ""reasoning"": ""Strong logical flow between scenes with clear cause-effect relationships.""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<SceneCoherenceSchema>(validJson);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(data);
        Assert.Equal(0.85, data.CoherenceScore);
        Assert.Equal(3, data.ConnectionTypes.Length);
        Assert.True(result.ValidationDuration.TotalMilliseconds < 150);
    }

    [Fact]
    public void ValidateNarrativeArc_ValidJson_ReturnsSuccess()
    {
        // Arrange
        var validJson = @"{
            ""isValid"": true,
            ""detectedStructure"": ""Three-act structure"",
            ""expectedStructure"": ""Three-act structure"",
            ""structuralIssues"": [],
            ""recommendations"": [""Maintain pacing""],
            ""reasoning"": ""The narrative follows a clear three-act structure with proper setup, confrontation, and resolution.""
        }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<NarrativeArcSchema>(validJson);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(data);
        Assert.True(data.IsValid);
        Assert.Equal("Three-act structure", data.DetectedStructure);
        Assert.True(result.ValidationDuration.TotalMilliseconds < 150);
    }

    [Fact]
    public void ValidateAndDeserialize_EmptyJson_ReturnsError()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<SceneAnalysisSchema>(emptyJson);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(data);
        Assert.Equal("Empty JSON output", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAndDeserialize_InvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var (result, data) = _validator.ValidateAndDeserialize<SceneAnalysisSchema>(invalidJson);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(data);
        Assert.Contains("Invalid JSON format", result.ErrorMessage);
    }

    [Fact]
    public void GenerateRepairPrompt_CreatesValidPrompt()
    {
        // Arrange
        var originalPrompt = "Analyze this scene";
        var failedOutput = "{ invalid }";
        var errors = new System.Collections.Generic.List<string>
        {
            "Importance must be between 0.0 and 1.0",
            "Missing required field: reasoning"
        };
        var schemaDefinition = "{ schema here }";

        // Act
        var repairPrompt = _validator.GenerateRepairPrompt(
            originalPrompt, 
            failedOutput, 
            errors, 
            schemaDefinition);

        // Assert
        Assert.Contains("Analyze this scene", repairPrompt);
        Assert.Contains("failed validation", repairPrompt);
        Assert.Contains("Importance must be between 0.0 and 1.0", repairPrompt);
        Assert.Contains("Missing required field: reasoning", repairPrompt);
        Assert.Contains(schemaDefinition, repairPrompt);
    }

    [Fact]
    public void SchemaValidator_PerformanceUnder5ms()
    {
        // Arrange
        var validJson = @"{
            ""importance"": 0.8,
            ""complexity"": 0.6,
            ""emotionalIntensity"": 0.7,
            ""informationDensity"": ""medium"",
            ""optimalDurationSeconds"": 5.5,
            ""transitionType"": ""fade"",
            ""reasoning"": ""Performance test reasoning that is long enough.""
        }";

        // Act - Run 10 times to get average
        var totalMs = 0.0;
        for (int i = 0; i < 10; i++)
        {
            var (result, _) = _validator.ValidateAndDeserialize<SceneAnalysisSchema>(validJson);
            totalMs += result.ValidationDuration.TotalMilliseconds;
        }

        var averageMs = totalMs / 10.0;

        // Assert
        Assert.True(averageMs < 5.0, $"Average validation time {averageMs}ms exceeds 5ms threshold");
    }
}
