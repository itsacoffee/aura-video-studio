using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

/// <summary>
/// Tests for LLM integration in Localization services.
/// Verifies that services correctly use CompleteAsync for prompt-based requests.
/// </summary>
public class LocalizationLlmIntegrationTests
{
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<ILogger<TranslationService>> _mockTranslationLogger;
    private readonly Mock<ILogger<TranslationQualityValidator>> _mockValidatorLogger;
    private readonly Mock<ILogger<CulturalLocalizationEngine>> _mockCulturalLogger;
    private readonly Mock<ILogger<VisualLocalizationAnalyzer>> _mockVisualLogger;

    public LocalizationLlmIntegrationTests()
    {
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockTranslationLogger = new Mock<ILogger<TranslationService>>();
        _mockValidatorLogger = new Mock<ILogger<TranslationQualityValidator>>();
        _mockCulturalLogger = new Mock<ILogger<CulturalLocalizationEngine>>();
        _mockVisualLogger = new Mock<ILogger<VisualLocalizationAnalyzer>>();
    }

    #region TranslationService Tests

    [Fact]
    public async Task TranslationService_TranslateAsync_UsesCompleteAsyncWithDetailedPrompt()
    {
        // Arrange
        var translationService = new TranslationService(
            _mockTranslationLogger.Object,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{\"translatedText\": \"Hola mundo\"}");

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            ScriptLines = new List<ScriptLine>
            {
                new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2))
            },
            Options = new TranslationOptions { Mode = TranslationMode.Localized }
        };

        // Act
        var result = await translationService.TranslateAsync(request, CancellationToken.None);

        // Assert
        _mockLlmProvider.Verify(
            x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "TranslationService should use CompleteAsync for translation");

        Assert.NotNull(capturedPrompt);
        Assert.Contains("expert professional translator", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("English", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello world", capturedPrompt);
    }

    [Fact]
    public async Task TranslationService_TranslateAsync_IncludesGlossaryInPrompt()
    {
        // Arrange
        var translationService = new TranslationService(
            _mockTranslationLogger.Object,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{\"translatedText\": \"Hola mundo\"}");

        var glossary = new Dictionary<string, string>
        {
            { "product", "producto" },
            { "service", "servicio" }
        };

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            ScriptLines = new List<ScriptLine>
            {
                new ScriptLine(0, "Our product and service", TimeSpan.Zero, TimeSpan.FromSeconds(2))
            },
            Glossary = glossary,
            Options = new TranslationOptions { Mode = TranslationMode.Localized }
        };

        // Act
        await translationService.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("MANDATORY TERMINOLOGY", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("product", capturedPrompt);
        Assert.Contains("producto", capturedPrompt);
        Assert.Contains("service", capturedPrompt);
        Assert.Contains("servicio", capturedPrompt);
    }

    [Fact]
    public async Task TranslationService_TranslateAsync_IncludesTranscreationModeInstructions()
    {
        // Arrange
        var translationService = new TranslationService(
            _mockTranslationLogger.Object,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{\"translatedText\": \"Hola mundo\"}");

        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            ScriptLines = new List<ScriptLine>
            {
                new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2))
            },
            Options = new TranslationOptions { Mode = TranslationMode.Transcreation }
        };

        // Act
        await translationService.TranslateAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("TRANSCREATION", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("creative adaptation", capturedPrompt, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region TranslationQualityValidator Tests

    [Fact]
    public async Task TranslationQualityValidator_ValidateAsync_UsesCompleteAsyncForScoring()
    {
        // Arrange
        var validator = new TranslationQualityValidator(
            _mockValidatorLogger.Object,
            _mockLlmProvider.Object);

        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("85");

        var translatedLines = new List<TranslatedScriptLine>
        {
            new TranslatedScriptLine
            {
                SceneIndex = 0,
                SourceText = "Hello world",
                TranslatedText = "Hola mundo"
            }
        };

        // Act
        var quality = await validator.ValidateAsync(
            translatedLines, "en", "es", new TranslationOptions(), CancellationToken.None);

        // Assert
        _mockLlmProvider.Verify(
            x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "TranslationQualityValidator should use CompleteAsync for scoring");

        Assert.True(quality.OverallScore > 0);
    }

    [Fact]
    public async Task TranslationQualityValidator_ValidateAsync_IncludesDetailedEvaluationCriteria()
    {
        // Arrange
        var validator = new TranslationQualityValidator(
            _mockValidatorLogger.Object,
            _mockLlmProvider.Object);

        var capturedPrompts = new List<string>();
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompts.Add(prompt))
            .ReturnsAsync("85");

        var translatedLines = new List<TranslatedScriptLine>
        {
            new TranslatedScriptLine
            {
                SceneIndex = 0,
                SourceText = "Hello world",
                TranslatedText = "Hola mundo"
            }
        };

        // Act
        await validator.ValidateAsync(
            translatedLines, "en", "es", new TranslationOptions(), CancellationToken.None);

        // Assert
        Assert.True(capturedPrompts.Count > 0, "Validator should call CompleteAsync");
        
        // Check that prompts include evaluation criteria
        var allPrompts = string.Join(" ", capturedPrompts);
        Assert.Contains("EVALUATION CRITERIA", allPrompts, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CulturalLocalizationEngine Tests

    [Fact]
    public async Task CulturalLocalizationEngine_AnalyzeAsync_UsesCompleteAsyncWithStructuredPrompt()
    {
        // Arrange
        var engine = new CulturalLocalizationEngine(
            _mockCulturalLogger.Object,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Cultural Sensitivity Score: 85\nIssues Found:\n- No issues found\nRecommendations:\n- No changes recommended");

        var targetLanguage = new LanguageInfo
        {
            Code = "ja",
            Name = "Japanese",
            CulturalSensitivities = new List<string> { "formal speech levels" }
        };

        // Act
        var result = await engine.AnalyzeCulturalContentAsync(
            "Welcome to our platform",
            targetLanguage,
            "Japan",
            CancellationToken.None);

        // Assert
        _mockLlmProvider.Verify(
            x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "CulturalLocalizationEngine should use CompleteAsync for analysis");

        Assert.NotNull(capturedPrompt);
        Assert.Contains("cultural sensitivity expert", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EVALUATION FRAMEWORK", capturedPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CulturalLocalizationEngine_AnalyzeAsync_IncludesKnownSensitivities()
    {
        // Arrange
        var engine = new CulturalLocalizationEngine(
            _mockCulturalLogger.Object,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Cultural Sensitivity Score: 85");

        var targetLanguage = new LanguageInfo
        {
            Code = "zh",
            Name = "Chinese",
            CulturalSensitivities = new List<string>
            {
                "avoid political references",
                "respect for elders"
            }
        };

        // Act
        await engine.AnalyzeCulturalContentAsync(
            "Test content",
            targetLanguage,
            "China",
            CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("KNOWN CULTURAL SENSITIVITIES", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("avoid political references", capturedPrompt);
        Assert.Contains("respect for elders", capturedPrompt);
    }

    #endregion

    #region VisualLocalizationAnalyzer Tests

    [Fact]
    public async Task VisualLocalizationAnalyzer_AnalyzeAsync_UsesCompleteAsyncWithComprehensivePrompt()
    {
        // Arrange
        var analyzer = new VisualLocalizationAnalyzer(
            NullLogger.Instance,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("No visual localization issues identified.");

        var translatedLines = new List<TranslatedScriptLine>
        {
            new TranslatedScriptLine
            {
                SceneIndex = 0,
                TranslatedText = "A person giving thumbs up"
            }
        };

        var targetLanguage = new LanguageInfo
        {
            Code = "ar",
            Name = "Arabic"
        };

        var culturalContext = new CulturalContext
        {
            TargetRegion = "Middle East",
            Sensitivities = new List<string> { "gestures may have different meanings" }
        };

        // Act
        var recommendations = await analyzer.AnalyzeVisualLocalizationNeedsAsync(
            translatedLines,
            targetLanguage,
            culturalContext,
            CancellationToken.None);

        // Assert
        _mockLlmProvider.Verify(
            x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce(),
            "VisualLocalizationAnalyzer should use CompleteAsync for analysis");

        Assert.NotNull(capturedPrompt);
        Assert.Contains("visual localization expert", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ANALYSIS CATEGORIES", capturedPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VisualLocalizationAnalyzer_AnalyzeAsync_IncludesCulturalSensitivitiesInPrompt()
    {
        // Arrange
        var analyzer = new VisualLocalizationAnalyzer(
            NullLogger.Instance,
            _mockLlmProvider.Object);

        string? capturedPrompt = null;
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync("No visual localization issues identified.");

        var translatedLines = new List<TranslatedScriptLine>
        {
            new TranslatedScriptLine
            {
                SceneIndex = 0,
                TranslatedText = "A red envelope gift"
            }
        };

        var targetLanguage = new LanguageInfo
        {
            Code = "zh",
            Name = "Chinese"
        };

        var culturalContext = new CulturalContext
        {
            TargetRegion = "China",
            Sensitivities = new List<string>
            {
                "red is lucky color",
                "avoid number 4"
            }
        };

        // Act
        await analyzer.AnalyzeVisualLocalizationNeedsAsync(
            translatedLines,
            targetLanguage,
            culturalContext,
            CancellationToken.None);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("KNOWN CULTURAL SENSITIVITIES", capturedPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("red is lucky color", capturedPrompt);
        Assert.Contains("avoid number 4", capturedPrompt);
    }

    #endregion

    #region RAG Configuration Tests

    [Fact]
    public void TranslationRequest_SupportsRagConfiguration()
    {
        // Arrange & Act
        var request = new TranslationRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            RagConfiguration = new RagConfiguration(
                Enabled: true,
                TopK: 5,
                MinimumScore: 0.7f,
                MaxContextTokens: 2000,
                IncludeCitations: true
            ),
            Domain = "medical",
            IndustryGlossaryId = "medical-terms-v1"
        };

        // Assert
        Assert.NotNull(request.RagConfiguration);
        Assert.True(request.RagConfiguration.Enabled);
        Assert.Equal(5, request.RagConfiguration.TopK);
        Assert.Equal(0.7f, request.RagConfiguration.MinimumScore);
        Assert.Equal("medical", request.Domain);
        Assert.Equal("medical-terms-v1", request.IndustryGlossaryId);
    }

    #endregion
}
