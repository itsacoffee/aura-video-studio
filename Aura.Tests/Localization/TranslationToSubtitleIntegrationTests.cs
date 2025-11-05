using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Captions;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Localization;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Localization;
using Aura.Providers.Tts.validators;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

/// <summary>
/// Integration tests for the complete translation → SSML → TTS → subtitle pipeline.
/// Tests the full internationalization workflow with RTL support.
/// </summary>
public class TranslationToSubtitleIntegrationTests : IDisposable
{
    private readonly TranslationIntegrationService _integrationService;
    private readonly TranslationService _translationService;
    private readonly SSMLPlannerService _ssmlPlannerService;
    private readonly CaptionBuilder _captionBuilder;
    private readonly SubtitleService _subtitleService;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly string _tempDirectory;

    public TranslationToSubtitleIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"TranslationIntegrationTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        _mockLlmProvider = new Mock<ILlmProvider>();
        
        _translationService = new TranslationService(
            NullLogger<TranslationService>.Instance,
            _mockLlmProvider.Object);
        
        var mappers = new List<ISSMLMapper>
        {
            new ElevenLabsSSMLMapper(),
            new WindowsSSMLMapper(),
            new PlayHTSSMLMapper(),
            new PiperSSMLMapper(),
            new Mimic3SSMLMapper()
        };
        
        _ssmlPlannerService = new SSMLPlannerService(
            NullLogger<SSMLPlannerService>.Instance,
            mappers);
        
        _captionBuilder = new CaptionBuilder(
            NullLogger<CaptionBuilder>.Instance);

        _subtitleService = new SubtitleService(
            NullLogger<SubtitleService>.Instance,
            _captionBuilder);
        
        _integrationService = new TranslationIntegrationService(
            NullLogger<TranslationIntegrationService>.Instance,
            _translationService,
            _ssmlPlannerService,
            _captionBuilder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task TranslateAndPlanSSML_EnglishToSpanish_GeneratesValidSubtitles()
    {
        // Arrange
        SetupMockTranslation("en", "es", 
            "Hello world", 
            "Hola mundo");

        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2))
        };

        var request = new TranslateAndPlanSSMLRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "es",
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            SubtitleFormat = SubtitleFormat.SRT,
            DurationTolerance = 0.02
        };

        // Act
        var result = await _integrationService.TranslateAndPlanSSMLAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("es", result.Translation.TargetLanguage);
        Assert.NotEmpty(result.Translation.TranslatedText);
        Assert.NotNull(result.Subtitles);
        Assert.Equal(SubtitleFormat.SRT, result.Subtitles.Format);
        Assert.NotEmpty(result.Subtitles.Content);
        Assert.Contains("00:00:00,000", result.Subtitles.Content); // Verify SRT format
    }

    [Fact]
    public async Task TranslateAndPlanSSML_EnglishToArabic_HandlesRTL()
    {
        // Arrange
        SetupMockTranslation("en", "ar", 
            "Welcome to our platform", 
            "مرحبا بكم في منصتنا");

        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Welcome to our platform", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };

        var request = new TranslateAndPlanSSMLRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "ar",
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            SubtitleFormat = SubtitleFormat.VTT,
            DurationTolerance = 0.02
        };

        // Act
        var result = await _integrationService.TranslateAndPlanSSMLAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ar", result.Translation.TargetLanguage);
        Assert.NotEmpty(result.Translation.TranslatedText);
        
        // Check subtitles are in VTT format
        Assert.Equal(SubtitleFormat.VTT, result.Subtitles.Format);
        Assert.StartsWith("WEBVTT", result.Subtitles.Content);
        Assert.NotEmpty(result.Subtitles.Content);
    }

    [Fact]
    public async Task TranslateAndPlanSSML_MultipleScenes_GeneratesAllSubtitles()
    {
        // Arrange
        SetupMockBatchTranslation("en", "fr", new[]
        {
            ("Welcome", "Bienvenue"),
            ("Thank you", "Merci"),
            ("Goodbye", "Au revoir")
        });

        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Welcome", TimeSpan.Zero, TimeSpan.FromSeconds(1)),
            new ScriptLine(1, "Thank you", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.5)),
            new ScriptLine(2, "Goodbye", TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(1))
        };

        var request = new TranslateAndPlanSSMLRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "fr",
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            SubtitleFormat = SubtitleFormat.SRT,
            DurationTolerance = 0.02
        };

        // Act
        var result = await _integrationService.TranslateAndPlanSSMLAsync(request);

        // Assert
        Assert.Equal(3, result.TranslatedScriptLines.Count);
        Assert.Equal(3, result.Subtitles.LineCount);
        Assert.NotEmpty(result.Subtitles.Content);
        
        // Verify SSML segments
        Assert.Equal(3, result.SSMLPlanning.Segments.Count);
    }

    [Fact]
    public async Task EndToEnd_TranslationToExportedSubtitles_CreatesValidFile()
    {
        // Arrange
        SetupMockTranslation("en", "ja", 
            "Good morning", 
            "おはようございます");

        var scriptLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Good morning", TimeSpan.Zero, TimeSpan.FromSeconds(2))
        };

        var translationRequest = new TranslateAndPlanSSMLRequest
        {
            SourceLanguage = "en",
            TargetLanguage = "ja",
            ScriptLines = scriptLines,
            TargetProvider = VoiceProvider.ElevenLabs,
            VoiceSpec = CreateTestVoiceSpec(),
            SubtitleFormat = SubtitleFormat.SRT,
            DurationTolerance = 0.02
        };

        // Act - Translation and SSML planning
        var translationResult = await _integrationService.TranslateAndPlanSSMLAsync(translationRequest);
        
        // Act - Export subtitles to file
        var subtitleRequest = new SubtitleGenerationRequest
        {
            ScriptLines = translationResult.TranslatedScriptLines.ToList(),
            TargetLanguage = "ja",
            Format = SubtitleExportFormat.SRT,
            IsRightToLeft = false,
            ExportToFile = true,
            OutputDirectory = _tempDirectory,
            BaseFileName = "japanese_subtitles"
        };

        var subtitleResult = await _subtitleService.GenerateSubtitlesAsync(subtitleRequest);

        // Assert
        Assert.NotNull(subtitleResult.ExportedFilePath);
        Assert.True(File.Exists(subtitleResult.ExportedFilePath));
        
        var fileContent = await File.ReadAllTextAsync(subtitleResult.ExportedFilePath);
        Assert.NotEmpty(fileContent);
        Assert.Contains("00:00:00,000", fileContent); // SRT timing format
        Assert.Contains("-->", fileContent); // SRT arrow separator
    }

    [Fact]
    public void SubtitleService_RTLStyleRecommendation_ReturnsCorrectFont()
    {
        // Act
        var arabicStyle = _subtitleService.GetRecommendedStyle("ar");
        var hebrewStyle = _subtitleService.GetRecommendedStyle("he");
        var englishStyle = _subtitleService.GetRecommendedStyle("en");

        // Assert
        Assert.True(arabicStyle.IsRightToLeft);
        Assert.Equal("Arial Unicode MS", arabicStyle.RtlFontFallback);
        
        Assert.True(hebrewStyle.IsRightToLeft);
        Assert.Equal("Arial Unicode MS", hebrewStyle.RtlFontFallback);
        
        Assert.False(englishStyle.IsRightToLeft);
        Assert.Null(englishStyle.RtlFontFallback);
    }

    [Fact]
    public void SubtitleService_TimingValidation_EnforcesTolerance()
    {
        // Arrange - Create lines that are exactly 2% over target
        var targetDuration = 10.0;
        var actualLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.Zero, TimeSpan.FromSeconds(10.2)) // 2% over
        };

        // Act
        var result = _subtitleService.ValidateTimingAlignment(
            actualLines, targetDuration, 0.02);

        // Assert - Should pass at exactly 2%
        Assert.True(result.IsValid);
        Assert.Equal(10.2, result.ActualDuration);
        Assert.True(result.DeviationPercent <= 2.0);
    }

    [Fact]
    public void VoiceRecommendation_ReturnsAppropriateVoices_ForLanguage()
    {
        // Act
        var spanishVoices = _integrationService.GetRecommendedVoice("es", VoiceProvider.ElevenLabs);
        var arabicVoices = _integrationService.GetRecommendedVoice("ar", VoiceProvider.ElevenLabs);

        // Assert
        Assert.False(spanishVoices.IsRTL);
        Assert.NotEmpty(spanishVoices.RecommendedVoices);
        
        Assert.True(arabicVoices.IsRTL);
        Assert.NotEmpty(arabicVoices.RecommendedVoices);
    }

    private void SetupMockTranslation(string sourceLanguage, string targetLanguage, 
        string sourceText, string translatedText)
    {
        var mockResponse = CreateMockTranslationResponse(sourceLanguage, targetLanguage, 
            sourceText, translatedText);
        
        _mockLlmProvider
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);
    }

    private void SetupMockBatchTranslation(string sourceLanguage, string targetLanguage, 
        (string Source, string Target)[] pairs)
    {
        foreach (var pair in pairs)
        {
            var mockResponse = CreateMockTranslationResponse(sourceLanguage, targetLanguage, 
                pair.Source, pair.Target);
            
            _mockLlmProvider
                .Setup(x => x.CompleteAsync(
                    It.Is<string>(s => s.Contains(pair.Source)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);
        }
    }

    private string CreateMockTranslationResponse(string sourceLanguage, string targetLanguage, 
        string sourceText, string translatedText)
    {
        return $@"{{
  ""translatedText"": ""{translatedText}"",
  ""sourceLanguage"": ""{sourceLanguage}"",
  ""targetLanguage"": ""{targetLanguage}"",
  ""fluencyScore"": 0.95,
  ""accuracyScore"": 0.92,
  ""culturalAppropriatenessScore"": 0.90,
  ""backTranslatedText"": ""{sourceText}"",
  ""culturalAdaptations"": [],
  ""warnings"": []
}}";
    }

    private VoiceSpec CreateTestVoiceSpec()
    {
        return new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
    }
}
