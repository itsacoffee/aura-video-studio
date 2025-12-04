using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Aura.Core.Services.AudioIntelligence;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class MusicMatchingServiceTests
{
    private readonly Mock<ILlmProvider> _llmProviderMock;
    private readonly Mock<IMusicProvider> _musicProviderMock;
    private readonly Mock<IFFmpegService> _ffmpegServiceMock;
    private readonly MusicMatchingService _service;

    public MusicMatchingServiceTests()
    {
        _llmProviderMock = new Mock<ILlmProvider>();
        _musicProviderMock = new Mock<IMusicProvider>();
        _ffmpegServiceMock = new Mock<IFFmpegService>();

        _service = new MusicMatchingService(
            NullLogger<MusicMatchingService>.Instance,
            _llmProviderMock.Object,
            _musicProviderMock.Object,
            _ffmpegServiceMock.Object);
    }

    [Fact]
    public async Task AnalyzeContentForMusicAsync_ReturnsParametersFromLlm()
    {
        // Arrange
        var brief = new Brief("Test Topic", "General", "Engage", "Professional", "en", Aspect.Widescreen16x9);
        var scenes = new List<Scene>
        {
            new Scene(0, "Introduction", "Welcome to our video", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var llmResponse = @"{
            ""mood"": ""Uplifting"",
            ""genre"": ""Corporate"",
            ""energy"": ""Medium"",
            ""keywords"": [""professional"", ""modern""],
            ""reasoning"": ""Corporate content needs professional music""
        }";

        _llmProviderMock.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.AnalyzeContentForMusicAsync(brief, scenes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MusicMood.Uplifting, result.PrimaryMood);
        Assert.Equal(MusicGenre.Corporate, result.PreferredGenre);
        Assert.Equal(EnergyLevel.Medium, result.TargetEnergy);
        Assert.Contains("professional", result.Keywords);
    }

    [Fact]
    public async Task AnalyzeContentForMusicAsync_UsesFallbackOnLlmFailure()
    {
        // Arrange
        var brief = new Brief("Test Topic", "General", "Engage", "Professional", "en", Aspect.Widescreen16x9);
        var scenes = new List<Scene>
        {
            new Scene(0, "Introduction", "Welcome", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        _llmProviderMock.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM error"));

        // Act
        var result = await _service.AnalyzeContentForMusicAsync(brief, scenes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fallback parameters based on video brief tone", result.Reasoning);
    }

    [Fact]
    public async Task GetMusicSuggestionsAsync_ReturnsRankedSuggestions()
    {
        // Arrange
        var parameters = new MusicMatchParameters(
            PrimaryMood: MusicMood.Uplifting,
            SecondaryMood: null,
            PreferredGenre: MusicGenre.Corporate,
            TargetEnergy: EnergyLevel.Medium,
            TargetBPMMin: 100,
            TargetBPMMax: 130,
            Keywords: new List<string> { "professional" },
            Reasoning: "Test"
        );

        var mockTracks = new List<MusicAsset>
        {
            CreateMockMusicAsset("1", "Track 1", MusicGenre.Corporate, MusicMood.Uplifting, EnergyLevel.Medium, 120),
            CreateMockMusicAsset("2", "Track 2", MusicGenre.Corporate, MusicMood.Calm, EnergyLevel.Low, 90)
        };

        _musicProviderMock.Setup(x => x.SearchAsync(It.IsAny<MusicSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult<MusicAsset>(mockTracks, 2, 1, 10, 1));

        // Act
        var result = await _service.GetMusicSuggestionsAsync(parameters, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].RelevanceScore >= result[1].RelevanceScore);
        Assert.True(result[0].IsRecommended);
    }

    [Fact]
    public async Task GetMusicSuggestionsAsync_EmptyResultReturnsEmptyList()
    {
        // Arrange
        var parameters = new MusicMatchParameters(
            PrimaryMood: MusicMood.Epic,
            SecondaryMood: null,
            PreferredGenre: MusicGenre.Orchestral,
            TargetEnergy: EnergyLevel.VeryHigh,
            TargetBPMMin: null,
            TargetBPMMax: null,
            Keywords: new List<string>(),
            Reasoning: "Test"
        );

        _musicProviderMock.Setup(x => x.SearchAsync(It.IsAny<MusicSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult<MusicAsset>(new List<MusicAsset>(), 0, 1, 10, 0));

        // Act
        var result = await _service.GetMusicSuggestionsAsync(parameters, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RankSuggestionsAsync_ReordersBasedOnAiRanking()
    {
        // Arrange
        var brief = new Brief("Test Topic", "General", "Engage", "Professional", "en", Aspect.Widescreen16x9);
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Text", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var suggestions = new List<MusicSuggestion>
        {
            new MusicSuggestion(
                CreateMockMusicAsset("1", "Track 1", MusicGenre.Corporate, MusicMood.Uplifting, EnergyLevel.Medium, 120),
                50, "Initial", new List<string>(), false),
            new MusicSuggestion(
                CreateMockMusicAsset("2", "Track 2", MusicGenre.Corporate, MusicMood.Calm, EnergyLevel.Low, 90),
                30, "Initial", new List<string>(), false)
        };

        var llmResponse = @"{ ""rankings"": { ""0"": 60, ""1"": 90 } }";
        _llmProviderMock.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.RankSuggestionsAsync(suggestions, brief, scenes);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].RelevanceScore >= result[1].RelevanceScore);
        Assert.True(result[0].IsRecommended);
    }

    private MusicAsset CreateMockMusicAsset(
        string id, string title, MusicGenre genre, MusicMood mood, EnergyLevel energy, int bpm)
    {
        return new MusicAsset(
            AssetId: id,
            Title: title,
            Artist: "Test Artist",
            Album: null,
            FilePath: $"/test/{id}.mp3",
            PreviewUrl: null,
            Duration: TimeSpan.FromMinutes(3),
            LicenseType: LicenseType.PublicDomain,
            LicenseUrl: "https://example.com/license",
            CommercialUseAllowed: true,
            AttributionRequired: false,
            AttributionText: null,
            SourcePlatform: "Test",
            CreatorProfileUrl: null,
            Genre: genre,
            Mood: mood,
            Energy: energy,
            BPM: bpm,
            Tags: new List<string>(),
            Metadata: null
        );
    }
}
