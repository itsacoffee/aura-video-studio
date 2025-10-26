using Xunit;
using FluentValidation.TestHelper;
using Aura.Api.Validators;
using Aura.Api.Models.ApiModels.V1;
using System.Collections.Generic;

namespace Aura.Tests;

/// <summary>
/// Tests for FluentValidation validators
/// </summary>
public class ValidationTests
{
    [Fact]
    public void ScriptRequestValidator_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: "How to make great videos",
            Audience: "Content creators",
            Goal: "Educate and inspire",
            Tone: "Professional and friendly",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Standard,
            Density: Density.Balanced,
            Style: "Educational with examples",
            ProviderTier: null,
            ProviderSelection: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ScriptRequestValidator_WithEmptyTopic_ShouldHaveError()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: "",
            Audience: "Content creators",
            Goal: "Educate and inspire",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Standard,
            Density: Density.Balanced,
            Style: "Educational",
            ProviderTier: null,
            ProviderSelection: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Topic);
    }

    [Fact]
    public void ScriptRequestValidator_WithTooLongTopic_ShouldHaveError()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: new string('a', 501), // 501 characters, exceeds max of 500
            Audience: "Content creators",
            Goal: "Educate and inspire",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Standard,
            Density: Density.Balanced,
            Style: "Educational",
            ProviderTier: null,
            ProviderSelection: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Topic);
    }

    [Fact]
    public void ScriptRequestValidator_WithInvalidDuration_ShouldHaveError()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: "Valid topic",
            Audience: "Content creators",
            Goal: "Educate and inspire",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 0, // Invalid - must be > 0
            Pacing: Pacing.Standard,
            Density: Density.Balanced,
            Style: "Educational",
            ProviderTier: null,
            ProviderSelection: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.TargetDurationMinutes);
    }

    [Fact]
    public void TtsRequestValidator_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new TtsRequestValidator();
        var request = new TtsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "Hello world", 0.0, 2.0)
            },
            VoiceName: "en-US-JennyNeural",
            Rate: 1.0,
            Pitch: 0.0,
            PauseStyle: PauseStyle.Balanced
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TtsRequestValidator_WithEmptyLines_ShouldHaveError()
    {
        // Arrange
        var validator = new TtsRequestValidator();
        var request = new TtsRequest(
            Lines: new List<LineDto>(),
            VoiceName: "en-US-JennyNeural",
            Rate: 1.0,
            Pitch: 0.0,
            PauseStyle: PauseStyle.Balanced
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Lines);
    }

    [Fact]
    public void TtsRequestValidator_WithInvalidRate_ShouldHaveError()
    {
        // Arrange
        var validator = new TtsRequestValidator();
        var request = new TtsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "Hello world", 0.0, 2.0)
            },
            VoiceName: "en-US-JennyNeural",
            Rate: 3.0, // Invalid - must be between 0.5 and 2.0
            Pitch: 0.0,
            PauseStyle: PauseStyle.Balanced
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Rate);
    }

    [Fact]
    public void AssetSearchRequestValidator_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new AssetSearchRequestValidator();
        var request = new AssetSearchRequest(
            Provider: "pexels",
            Query: "nature landscape",
            Count: 10,
            ApiKey: "test-api-key-12345"
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AssetSearchRequestValidator_WithInvalidProvider_ShouldHaveError()
    {
        // Arrange
        var validator = new AssetSearchRequestValidator();
        var request = new AssetSearchRequest(
            Provider: "invalid-provider",
            Query: "nature landscape",
            Count: 10,
            ApiKey: "test-api-key"
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Provider);
    }

    [Fact]
    public void AssetSearchRequestValidator_WithMissingApiKeyForNonLocal_ShouldHaveError()
    {
        // Arrange
        var validator = new AssetSearchRequestValidator();
        var request = new AssetSearchRequest(
            Provider: "pexels",
            Query: "nature landscape",
            Count: 10,
            ApiKey: null // Missing API key for non-local provider
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.ApiKey);
    }

    [Fact]
    public void AssetSearchRequestValidator_WithInvalidCount_ShouldHaveError()
    {
        // Arrange
        var validator = new AssetSearchRequestValidator();
        var request = new AssetSearchRequest(
            Provider: "pexels",
            Query: "nature landscape",
            Count: 100, // Invalid - must be between 1 and 50
            ApiKey: "test-api-key"
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Count);
    }

    [Fact]
    public void RenderRequestValidator_WithValidRequest_ShouldNotHaveErrors()
    {
        // Arrange
        var validator = new RenderRequestValidator();
        var request = new RenderRequest(
            TimelineJson: "{\"scenes\": []}",
            PresetName: "1080p",
            Settings: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RenderRequestValidator_WithInvalidJson_ShouldHaveError()
    {
        // Arrange
        var validator = new RenderRequestValidator();
        var request = new RenderRequest(
            TimelineJson: "not-valid-json",
            PresetName: "1080p",
            Settings: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.TimelineJson);
    }
}
