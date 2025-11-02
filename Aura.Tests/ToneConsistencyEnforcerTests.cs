using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Quality;
using Aura.Core.Providers;
using Aura.Core.Services.Quality;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ToneConsistencyEnforcerTests
{
    private readonly Mock<ILogger<ToneConsistencyEnforcer>> _loggerMock;
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly ToneConsistencyEnforcer _enforcer;

    public ToneConsistencyEnforcerTests()
    {
        _loggerMock = new Mock<ILogger<ToneConsistencyEnforcer>>();
        _llmProviderMock = new Mock<ILlmProvider>();
        _enforcer = new ToneConsistencyEnforcer(_loggerMock.Object, _llmProviderMock.Object);
    }

    [Fact]
    public async Task ExpandToneProfileAsync_ReturnsToneProfile()
    {
        // Arrange
        var tone = "professional";
        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""vocabularyLevel"": ""College"",
                ""formality"": ""Professional"",
                ""humor"": ""None"",
                ""energy"": ""Moderate"",
                ""perspective"": ""ThirdPersonAuthority"",
                ""guidelines"": ""Professional tone with clear, authoritative language"",
                ""examplePhrases"": [""Let's examine"", ""Research indicates""],
                ""phrasesToAvoid"": [""Yo"", ""Sup""],
                ""targetWordsPerMinute"": 150,
                ""ttsRateAdjustment"": -5,
                ""ttsPitchAdjustment"": 0,
                ""visualStyleKeywords"": [""clean"", ""corporate"", ""professional""]
            }");

        // Act
        var profile = await _enforcer.ExpandToneProfileAsync(tone);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(tone, profile.OriginalTone);
        Assert.Equal(VocabularyLevel.College, profile.VocabularyLevel);
        Assert.Equal(FormalityLevel.Professional, profile.Formality);
        Assert.Equal(HumorStyle.None, profile.Humor);
        Assert.Equal(EnergyLevel.Moderate, profile.Energy);
        Assert.Equal(NarrativePerspective.ThirdPersonAuthority, profile.Perspective);
    }

    [Fact]
    public async Task ExpandToneProfileAsync_HandlesLlmFailure_ReturnsFallback()
    {
        // Arrange
        var tone = "casual";
        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM unavailable"));

        // Act
        var profile = await _enforcer.ExpandToneProfileAsync(tone);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(tone, profile.OriginalTone);
        Assert.NotEmpty(profile.Guidelines);
    }

    [Fact]
    public async Task ValidateScriptToneAsync_ReturnsConsistencyScore()
    {
        // Arrange
        var scriptText = "Let's explore the fascinating world of machine learning.";
        var toneProfile = new ToneProfile
        {
            OriginalTone = "educational",
            VocabularyLevel = VocabularyLevel.College,
            Formality = FormalityLevel.Conversational,
            Energy = EnergyLevel.Moderate,
            Perspective = NarrativePerspective.SecondPerson
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""vocabularyScore"": 90,
                ""formalityScore"": 88,
                ""energyScore"": 85,
                ""perspectiveScore"": 92,
                ""overallScore"": 89,
                ""reasoning"": ""Script maintains consistent educational tone""
            }");

        // Act
        var score = await _enforcer.ValidateScriptToneAsync(scriptText, toneProfile, 0);

        // Assert
        Assert.NotNull(score);
        Assert.True(score.OverallScore > 0);
        Assert.Equal(0, score.SceneIndex);
        Assert.True(score.Passes); // Should be > 85
    }

    [Fact]
    public async Task ValidateVisualStyleToneAsync_ReturnsAlignmentScore()
    {
        // Arrange
        var visualDescription = "Clean, modern office space with natural lighting";
        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            Energy = EnergyLevel.Moderate,
            Formality = FormalityLevel.Professional,
            VisualStyleKeywords = new[] { "clean", "modern", "professional" }
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""visualAlignmentScore"": 92,
                ""reasoning"": ""Visual style aligns well with professional tone""
            }");

        // Act
        var score = await _enforcer.ValidateVisualStyleToneAsync(visualDescription, toneProfile, 1);

        // Assert
        Assert.NotNull(score);
        Assert.True(score.VisualAlignmentScore > 0);
        Assert.Equal(1, score.SceneIndex);
    }

    [Fact]
    public async Task ValidatePacingToneAsync_CalculatesCorrelation()
    {
        // Arrange
        var cutFrequency = 2.0;
        var averageSceneDuration = 30.0;
        var toneProfile = new ToneProfile
        {
            OriginalTone = "energetic",
            Energy = EnergyLevel.Energetic
        };

        // Act
        var score = await _enforcer.ValidatePacingToneAsync(cutFrequency, averageSceneDuration, toneProfile);

        // Assert
        Assert.NotNull(score);
        Assert.True(score.PacingAlignmentScore >= 0);
        Assert.True(score.PacingAlignmentScore <= 100);
        Assert.Contains("correlation", score.Reasoning.ToLowerInvariant());
    }

    [Fact]
    public async Task ValidateAudioToneAsync_CalculatesRateAndPitchAlignment()
    {
        // Arrange
        var voiceSpec = new VoiceSpec("TestVoice", Rate: 1.0, Pitch: 1.0, PauseStyle.Natural);
        var toneProfile = new ToneProfile
        {
            OriginalTone = "moderate",
            Energy = EnergyLevel.Moderate
        };

        // Act
        var score = await _enforcer.ValidateAudioToneAsync(voiceSpec, toneProfile);

        // Assert
        Assert.NotNull(score);
        Assert.True(score.OverallScore >= 0);
        Assert.True(score.OverallScore <= 100);
        Assert.Contains("rate", score.Reasoning.ToLowerInvariant());
        Assert.Contains("pitch", score.Reasoning.ToLowerInvariant());
    }

    [Fact]
    public async Task DetectStyleViolationsAsync_ReturnsViolations()
    {
        // Arrange
        var sceneTexts = new[]
        {
            "This is a professional presentation about business metrics.",
            "Yo, this stuff is totally rad, bro!",
            "As we proceed with the analysis..."
        };

        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            VocabularyLevel = VocabularyLevel.College,
            Formality = FormalityLevel.Professional
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"[
                {
                    ""severity"": ""High"",
                    ""category"": ""FormalityShift"",
                    ""sceneIndex"": 1,
                    ""description"": ""Sudden shift to very informal language"",
                    ""example"": ""Yo, this stuff is totally rad, bro!"",
                    ""expected"": ""Professional language"",
                    ""actual"": ""Casual slang"",
                    ""impactScore"": 85
                }
            ]");

        // Act
        var violations = await _enforcer.DetectStyleViolationsAsync(sceneTexts, toneProfile);

        // Assert
        Assert.NotNull(violations);
        Assert.NotEmpty(violations);
        var violation = violations.First();
        Assert.Equal(ViolationSeverity.High, violation.Severity);
        Assert.Equal(1, violation.SceneIndex);
        Assert.True(violation.ImpactScore > 0);
    }

    [Fact]
    public async Task DetectToneDriftAsync_DetectsDrift()
    {
        // Arrange
        var sceneTexts = new[]
        {
            "Welcome to our professional analysis.",
            "Let's look at the data carefully.",
            "Things are getting interesting now.",
            "Wow, this is super cool stuff!",
            "OMG, you won't believe this!"
        };

        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            Formality = FormalityLevel.Professional
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""driftDetected"": true,
                ""driftedCharacteristics"": [""formality"", ""vocabulary""],
                ""violations"": []
            }");

        // Act
        var result = await _enforcer.DetectToneDriftAsync(sceneTexts, toneProfile, windowSize: 3);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DetectToneDriftAsync_InsufficientScenes_ReturnsNoDrift()
    {
        // Arrange
        var sceneTexts = new[] { "Scene 1", "Scene 2" };
        var toneProfile = new ToneProfile { OriginalTone = "professional" };

        // Act
        var result = await _enforcer.DetectToneDriftAsync(sceneTexts, toneProfile, windowSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.DriftDetected);
        Assert.Contains("Insufficient", result.Analysis);
    }

    [Fact]
    public async Task GenerateCorrectionSuggestionsAsync_GeneratesSuggestions()
    {
        // Arrange
        var sceneTexts = new[]
        {
            "This is fine",
            "Yo, this is totally rad!",
            "More content here"
        };

        var violations = new[]
        {
            new StyleViolation
            {
                Severity = ViolationSeverity.High,
                Category = ViolationCategory.FormalityShift,
                SceneIndex = 1,
                Description = "Informal language",
                Example = "Yo, this is totally rad!",
                Expected = "Professional",
                Actual = "Casual slang"
            }
        };

        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            Formality = FormalityLevel.Professional
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""correctedText"": ""This presents an excellent opportunity."",
                ""explanation"": ""Replaced casual slang with professional language"",
                ""specificChanges"": [""Removed 'Yo'"", ""Changed 'rad' to 'excellent'""],
                ""scoreAfter"": 92
            }");

        // Act
        var suggestions = await _enforcer.GenerateCorrectionSuggestionsAsync(sceneTexts, toneProfile, violations);

        // Assert
        Assert.NotNull(suggestions);
    }

    [Fact]
    public async Task GenerateCorrectionSuggestionsAsync_SkipsLowSeverityViolations()
    {
        // Arrange
        var sceneTexts = new[] { "Scene text" };
        var violations = new[]
        {
            new StyleViolation
            {
                Severity = ViolationSeverity.Low,
                SceneIndex = 0
            }
        };
        var toneProfile = new ToneProfile { OriginalTone = "professional" };

        // Act
        var suggestions = await _enforcer.GenerateCorrectionSuggestionsAsync(sceneTexts, toneProfile, violations);

        // Assert
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ValidateCrossModalToneAsync_ValidatesAllModalities()
    {
        // Arrange
        var sceneTexts = new[]
        {
            "Professional analysis of business trends.",
            "Examining the data reveals key insights."
        };

        var visualDescriptions = new[]
        {
            "Clean corporate office environment",
            "Data visualization charts and graphs"
        };

        var sceneDurations = new[] { 5.0, 5.0 };

        var voiceSpec = new VoiceSpec("Professional", Rate: 1.0, Pitch: 1.0, PauseStyle.Natural);

        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            VocabularyLevel = VocabularyLevel.College,
            Formality = FormalityLevel.Professional,
            Energy = EnergyLevel.Moderate,
            VisualStyleKeywords = new[] { "clean", "corporate" }
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""vocabularyScore"": 90,
                ""formalityScore"": 88,
                ""energyScore"": 85,
                ""perspectiveScore"": 92,
                ""overallScore"": 89,
                ""visualAlignmentScore"": 90,
                ""reasoning"": ""Consistent professional tone""
            }");

        // Act
        var validation = await _enforcer.ValidateCrossModalToneAsync(
            sceneTexts, 
            visualDescriptions, 
            sceneDurations, 
            voiceSpec, 
            toneProfile);

        // Assert
        Assert.NotNull(validation);
        Assert.True(validation.ScriptScore >= 0);
        Assert.True(validation.VisualScore >= 0);
        Assert.True(validation.PacingScore >= 0);
        Assert.True(validation.AudioScore >= 0);
        Assert.True(validation.OverallScore >= 0);
        Assert.NotEmpty(validation.Analysis);
    }

    [Fact]
    public async Task ValidateCrossModalToneAsync_DetectsAlignment()
    {
        // Arrange
        var sceneTexts = new[] { "Scene 1" };
        var visualDescriptions = new[] { "Visual 1" };
        var sceneDurations = new[] { 5.0 };
        var voiceSpec = new VoiceSpec("Voice", 1.0, 1.0, PauseStyle.Natural);
        var toneProfile = new ToneProfile
        {
            OriginalTone = "professional",
            Energy = EnergyLevel.Moderate
        };

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""overallScore"": 90,
                ""vocabularyScore"": 90,
                ""formalityScore"": 90,
                ""energyScore"": 90,
                ""perspectiveScore"": 90,
                ""visualAlignmentScore"": 90,
                ""reasoning"": ""Good alignment""
            }");

        // Act
        var validation = await _enforcer.ValidateCrossModalToneAsync(
            sceneTexts, visualDescriptions, sceneDurations, voiceSpec, toneProfile);

        // Assert
        Assert.True(validation.OverallScore > 80);
        Assert.True(validation.IsAligned);
    }

    [Theory]
    [InlineData(EnergyLevel.Calm, 0.5)]
    [InlineData(EnergyLevel.Moderate, 1.0)]
    [InlineData(EnergyLevel.Energetic, 2.0)]
    [InlineData(EnergyLevel.High, 3.0)]
    public async Task ValidatePacingToneAsync_MatchesExpectedCutFrequency(EnergyLevel energy, double expectedFrequency)
    {
        // Arrange
        var toneProfile = new ToneProfile
        {
            OriginalTone = "test",
            Energy = energy
        };

        // Act
        var score = await _enforcer.ValidatePacingToneAsync(expectedFrequency, 30.0, toneProfile);

        // Assert
        Assert.True(score.PacingAlignmentScore >= 90); // Should be very high when matching expected
    }

    [Fact]
    public async Task ExpandToneProfileAsync_CompletesInUnderTwoSeconds()
    {
        // Arrange
        var tone = "conversational";
        var startTime = DateTime.UtcNow;

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{
                ""vocabularyLevel"": ""Grade9to12"",
                ""formality"": ""Conversational"",
                ""humor"": ""Light"",
                ""energy"": ""Moderate"",
                ""perspective"": ""SecondPerson"",
                ""guidelines"": ""Conversational and friendly"",
                ""examplePhrases"": [""Let's talk about""],
                ""phrasesToAvoid"": [""Henceforth""],
                ""targetWordsPerMinute"": 160,
                ""ttsRateAdjustment"": 0,
                ""ttsPitchAdjustment"": 0,
                ""visualStyleKeywords"": [""friendly""]
            }");

        // Act
        var profile = await _enforcer.ExpandToneProfileAsync(tone);
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;

        // Assert
        Assert.True(duration < 2.0, $"Expected < 2s, but took {duration:F2}s");
    }

    [Fact]
    public async Task ValidateScriptToneAsync_CompletesInUnderTwoSeconds()
    {
        // Arrange
        var scriptText = "Test script with moderate length for performance testing.";
        var toneProfile = new ToneProfile { OriginalTone = "test" };
        var startTime = DateTime.UtcNow;

        _llmProviderMock
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(@"{""overallScore"": 85, ""reasoning"": ""Good""}");

        // Act
        var score = await _enforcer.ValidateScriptToneAsync(scriptText, toneProfile);
        var duration = (DateTime.UtcNow - startTime).TotalSeconds;

        // Assert
        Assert.True(duration < 2.0, $"Expected < 2s, but took {duration:F2}s");
    }

    [Fact]
    public void ToneConsistencyScore_PassesWhenAbove85()
    {
        // Arrange & Act
        var score = new ToneConsistencyScore { OverallScore = 86 };

        // Assert
        Assert.True(score.Passes);
    }

    [Fact]
    public void ToneConsistencyScore_FailsWhenAt85OrBelow()
    {
        // Arrange & Act
        var score = new ToneConsistencyScore { OverallScore = 85 };

        // Assert
        Assert.False(score.Passes);
    }

    [Fact]
    public void CrossModalToneValidation_IsAlignedWhenAbove80()
    {
        // Arrange & Act
        var validation = new CrossModalToneValidation { OverallScore = 81 };

        // Assert
        Assert.True(validation.IsAligned);
    }

    [Fact]
    public void CrossModalToneValidation_NotAlignedWhenAt80OrBelow()
    {
        // Arrange & Act
        var validation = new CrossModalToneValidation { OverallScore = 80 };

        // Assert
        Assert.False(validation.IsAligned);
    }
}
