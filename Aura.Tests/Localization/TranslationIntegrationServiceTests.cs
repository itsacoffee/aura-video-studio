using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Captions;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Localization;

public class TranslationIntegrationServiceTests
{
    private readonly Mock<ILogger<TranslationIntegrationService>> _loggerMock;
    private readonly Mock<TranslationService> _translationServiceMock;
    private readonly Mock<SSMLPlannerService> _ssmlPlannerServiceMock;
    private readonly Mock<CaptionBuilder> _captionBuilderMock;
    private readonly TranslationIntegrationService _service;

    public TranslationIntegrationServiceTests()
    {
        _loggerMock = new Mock<ILogger<TranslationIntegrationService>>();
        
        var translationLogger = new Mock<ILogger<TranslationService>>();
        var llmProvider = new Mock<Core.Providers.ILlmProvider>();
        _translationServiceMock = new Mock<TranslationService>(translationLogger.Object, llmProvider.Object);
        
        var ssmlLogger = new Mock<ILogger<SSMLPlannerService>>();
        _ssmlPlannerServiceMock = new Mock<SSMLPlannerService>(ssmlLogger.Object);
        
        var captionLogger = new Mock<ILogger<CaptionBuilder>>();
        _captionBuilderMock = new Mock<CaptionBuilder>(captionLogger.Object);

        _service = new TranslationIntegrationService(
            _loggerMock.Object,
            _translationServiceMock.Object,
            _ssmlPlannerServiceMock.Object,
            _captionBuilderMock.Object);
    }

    [Fact]
    public void GetRecommendedVoice_ForArabic_ReturnsRTLMarker()
    {
        var recommendation = _service.GetRecommendedVoice(
            "ar",
            VoiceProvider.ElevenLabs);

        Assert.True(recommendation.IsRTL);
        Assert.Equal("ar", recommendation.TargetLanguage);
        Assert.Equal(VoiceProvider.ElevenLabs, recommendation.Provider);
        Assert.NotEmpty(recommendation.RecommendedVoices);
    }

    [Fact]
    public void GetRecommendedVoice_ForEnglish_ReturnsLTRMarker()
    {
        var recommendation = _service.GetRecommendedVoice(
            "en",
            VoiceProvider.ElevenLabs);

        Assert.False(recommendation.IsRTL);
        Assert.Equal("en", recommendation.TargetLanguage);
    }

    [Fact]
    public void GetRecommendedVoice_ForHebrew_ReturnsRTLMarker()
    {
        var recommendation = _service.GetRecommendedVoice(
            "he",
            VoiceProvider.ElevenLabs);

        Assert.True(recommendation.IsRTL);
        Assert.Equal("he", recommendation.TargetLanguage);
    }

    [Fact]
    public void GetRecommendedVoice_WithElevenLabs_ReturnsQualityVoices()
    {
        var recommendation = _service.GetRecommendedVoice(
            "es",
            VoiceProvider.ElevenLabs);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.All(recommendation.RecommendedVoices, voice =>
        {
            Assert.False(string.IsNullOrEmpty(voice.VoiceName));
            Assert.False(string.IsNullOrEmpty(voice.Gender));
            Assert.False(string.IsNullOrEmpty(voice.Style));
            Assert.Equal("Premium", voice.Quality);
        });
    }

    [Fact]
    public void GetRecommendedVoice_WithWindowsSAPI_ReturnsFreeVoice()
    {
        var recommendation = _service.GetRecommendedVoice(
            "en",
            VoiceProvider.WindowsSAPI);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.Equal("Free", recommendation.RecommendedVoices.First().Quality);
    }

    [Fact]
    public void GetRecommendedVoice_WithPiper_ReturnsFreeVoice()
    {
        var recommendation = _service.GetRecommendedVoice(
            "en",
            VoiceProvider.Piper);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.Equal("Free", recommendation.RecommendedVoices.First().Quality);
    }

    [Fact]
    public void GetRecommendedVoice_WithUnsupportedLanguage_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _service.GetRecommendedVoice("invalid-language", VoiceProvider.ElevenLabs));
    }

    [Theory]
    [InlineData("ar")]
    [InlineData("he")]
    [InlineData("fa")]
    [InlineData("ur")]
    public void GetRecommendedVoice_ForRTLLanguages_AlwaysReturnsRTLTrue(string languageCode)
    {
        var recommendation = _service.GetRecommendedVoice(
            languageCode,
            VoiceProvider.ElevenLabs);

        Assert.True(recommendation.IsRTL);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("ja")]
    [InlineData("zh")]
    public void GetRecommendedVoice_ForLTRLanguages_AlwaysReturnsRTLFalse(string languageCode)
    {
        var recommendation = _service.GetRecommendedVoice(
            languageCode,
            VoiceProvider.ElevenLabs);

        Assert.False(recommendation.IsRTL);
    }

    [Fact]
    public void GetRecommendedVoice_ForJapanese_ReturnsAppropriateVoices()
    {
        var recommendation = _service.GetRecommendedVoice(
            "ja",
            VoiceProvider.ElevenLabs);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.Contains(recommendation.RecommendedVoices,
            v => v.VoiceName.Contains("Akira") || v.VoiceName.Contains("Sakura"));
    }

    [Fact]
    public void GetRecommendedVoice_ForChinese_ReturnsAppropriateVoices()
    {
        var recommendation = _service.GetRecommendedVoice(
            "zh",
            VoiceProvider.ElevenLabs);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.Contains(recommendation.RecommendedVoices,
            v => v.VoiceName.Contains("Li") || v.VoiceName.Contains("Mei"));
    }

    [Fact]
    public void GetRecommendedVoice_ForGerman_ReturnsAppropriateVoices()
    {
        var recommendation = _service.GetRecommendedVoice(
            "de",
            VoiceProvider.ElevenLabs);

        Assert.NotEmpty(recommendation.RecommendedVoices);
        Assert.Contains(recommendation.RecommendedVoices,
            v => v.VoiceName.Contains("Hans") || v.VoiceName.Contains("Greta"));
    }
}
