using Aura.Api.Models.ApiModels.V1;
using Aura.Api.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Aura.Tests;

public class RequestValidatorsSecurityTests
{
    [Fact]
    public void ScriptRequestValidator_RejectsXssInTopic()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: "<script>alert('xss')</script>",
            Audience: "General",
            Goal: "Inform",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern",
            ProviderTier: null,
            ProviderSelection: null,
            PromptModifiers: null,
            RefinementConfig: null,
            AudienceProfileId: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Topic)
            .WithErrorMessage("Topic contains potentially dangerous content");
    }

    [Fact]
    public void ScriptRequestValidator_RejectsInvalidGuidForAudienceProfileId()
    {
        // Arrange
        var validator = new ScriptRequestValidator();
        var request = new ScriptRequest(
            Topic: "Valid Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9,
            TargetDurationMinutes: 5.0,
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern",
            ProviderTier: null,
            ProviderSelection: null,
            PromptModifiers: null,
            RefinementConfig: null,
            AudienceProfileId: "not-a-guid"
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AudienceProfileId)
            .WithErrorMessage("Audience profile ID must be a valid GUID");
    }

    [Fact]
    public void ApiKeysRequestValidator_ValidatesOpenAiKeyFormat()
    {
        // Arrange
        var validator = new ApiKeysRequestValidator();
        var request = new ApiKeysRequest(
            OpenAiKey: "invalid-key",
            ElevenLabsKey: null,
            PexelsKey: null,
            PixabayKey: null,
            UnsplashKey: null,
            StabilityAiKey: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OpenAiKey)
            .WithErrorMessage("OpenAI API key must start with 'sk-' and be at least 20 characters");
    }

    [Fact]
    public void ApiKeysRequestValidator_AllowsValidOpenAiKey()
    {
        // Arrange
        var validator = new ApiKeysRequestValidator();
        var request = new ApiKeysRequest(
            OpenAiKey: "sk-1234567890123456789012",
            ElevenLabsKey: null,
            PexelsKey: null,
            PixabayKey: null,
            UnsplashKey: null,
            StabilityAiKey: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OpenAiKey);
    }

    [Fact]
    public void ProviderPathsRequestValidator_RejectsPathTraversal()
    {
        // Arrange
        var validator = new ProviderPathsRequestValidator();
        var request = new ProviderPathsRequest(
            StableDiffusionUrl: null,
            OllamaUrl: null,
            FfmpegPath: "../../etc/passwd",
            FfprobePath: null,
            OutputDirectory: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FfmpegPath)
            .WithErrorMessage("FFmpeg path contains directory traversal attempt");
    }

    [Fact]
    public void ProviderPathsRequestValidator_ValidatesUrlFormat()
    {
        // Arrange
        var validator = new ProviderPathsRequestValidator();
        var request = new ProviderPathsRequest(
            StableDiffusionUrl: "not-a-valid-url",
            OllamaUrl: null,
            FfmpegPath: null,
            FfprobePath: null,
            OutputDirectory: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StableDiffusionUrl)
            .WithErrorMessage("Stable Diffusion URL must be a valid HTTP or HTTPS URL");
    }

    [Fact]
    public void TtsRequestValidator_RejectsXssInVoiceName()
    {
        // Arrange
        var validator = new TtsRequestValidator();
        var request = new TtsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "Test", 0, 1)
            },
            VoiceName: "<script>alert('xss')</script>",
            Rate: 1.0,
            Pitch: 0,
            PauseStyle: PauseStyle.Natural
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VoiceName)
            .WithErrorMessage("Voice name contains potentially dangerous content");
    }

    [Fact]
    public void TtsRequestValidator_RejectsXssInLineText()
    {
        // Arrange
        var validator = new TtsRequestValidator();
        var request = new TtsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "<script>alert('xss')</script>", 0, 1)
            },
            VoiceName: "ValidVoice",
            Rate: 1.0,
            Pitch: 0,
            PauseStyle: PauseStyle.Natural
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("Lines[0].Text")
            .WithErrorMessage("Line text contains potentially dangerous content");
    }

    [Fact]
    public void PromptModifiersDtoValidator_EnforcesMaxLength()
    {
        // Arrange
        var validator = new PromptModifiersDtoValidator();
        var request = new PromptModifiersDto(
            AdditionalInstructions: new string('a', 6000),
            ExampleStyle: null,
            EnableChainOfThought: false,
            PromptVersion: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdditionalInstructions)
            .WithErrorMessage("Additional instructions must not exceed 5000 characters");
    }

    [Fact]
    public void ScriptRefinementConfigDtoValidator_ValidatesRanges()
    {
        // Arrange
        var validator = new ScriptRefinementConfigDtoValidator();
        var request = new ScriptRefinementConfigDto(
            MaxRefinementPasses: 10, // Invalid: max is 5
            QualityThreshold: 85.0,
            MinimumImprovement: 5.0,
            EnableAdvisorValidation: true,
            PassTimeoutMinutes: 2
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxRefinementPasses)
            .WithErrorMessage("Max refinement passes must be between 1 and 5");
    }

    [Fact]
    public void CaptionsRequestValidator_ValidatesFormat()
    {
        // Arrange
        var validator = new CaptionsRequestValidator();
        var request = new CaptionsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "Test", 0, 1)
            },
            Format: "INVALID_FORMAT",
            OutputPath: null
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Format)
            .WithErrorMessage("Format must be one of: SRT, VTT, ASS, SSA");
    }

    [Fact]
    public void CaptionsRequestValidator_RejectsPathTraversal()
    {
        // Arrange
        var validator = new CaptionsRequestValidator();
        var request = new CaptionsRequest(
            Lines: new List<LineDto>
            {
                new LineDto(0, "Test", 0, 1)
            },
            Format: "SRT",
            OutputPath: "../../etc/passwd"
        );

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OutputPath)
            .WithErrorMessage("Output path contains directory traversal attempt");
    }
}
