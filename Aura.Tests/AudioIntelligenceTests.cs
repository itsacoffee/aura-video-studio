using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Services.AudioIntelligence;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class MusicRecommendationServiceTests
{
    private readonly MusicRecommendationService _service;
    private readonly ILlmProvider _mockLlmProvider;

    public MusicRecommendationServiceTests()
    {
        _mockLlmProvider = new Aura.Providers.Llm.RuleBasedLlmProvider(
            NullLogger<Aura.Providers.Llm.RuleBasedLlmProvider>.Instance);
        _service = new MusicRecommendationService(
            NullLogger<MusicRecommendationService>.Instance,
            _mockLlmProvider);
    }

    [Fact]
    public async Task RecommendMusicAsync_Should_ReturnRecommendations()
    {
        // Arrange
        var mood = MusicMood.Energetic;
        var energy = EnergyLevel.High;
        var duration = TimeSpan.FromMinutes(3);

        // Act
        var recommendations = await _service.RecommendMusicAsync(
            mood, null, energy, duration, null, 5, CancellationToken.None);

        // Assert
        Assert.NotNull(recommendations);
        Assert.NotEmpty(recommendations);
        Assert.True(recommendations.Count <= 5);
    }

    [Fact]
    public async Task RecommendMusicAsync_Should_FilterByMood()
    {
        // Arrange
        var mood = MusicMood.Calm;
        var energy = EnergyLevel.Low;
        var duration = TimeSpan.FromMinutes(3);

        // Act
        var recommendations = await _service.RecommendMusicAsync(
            mood, null, energy, duration, null, 10, CancellationToken.None);

        // Assert
        Assert.All(recommendations, rec =>
            Assert.True(rec.Track.Mood == mood || rec.Track.Energy == energy));
    }

    [Fact]
    public async Task RecommendMusicAsync_Should_ScoreByRelevance()
    {
        // Arrange
        var mood = MusicMood.Epic;
        var energy = EnergyLevel.VeryHigh;
        var duration = TimeSpan.FromMinutes(3);

        // Act
        var recommendations = await _service.RecommendMusicAsync(
            mood, null, energy, duration, null, 10, CancellationToken.None);

        // Assert
        Assert.All(recommendations, rec =>
        {
            Assert.True(rec.RelevanceScore >= 0);
            Assert.True(rec.RelevanceScore <= 100);
        });

        // Should be ordered by relevance
        var scores = recommendations.Select(r => r.RelevanceScore).ToList();
        Assert.Equal(scores.OrderByDescending(s => s).ToList(), scores);
    }
}

public class BeatDetectionServiceTests
{
    private readonly BeatDetectionService _service;

    public BeatDetectionServiceTests()
    {
        _service = new BeatDetectionService(NullLogger<BeatDetectionService>.Instance);
    }

    [Fact]
    public void CalculateBPM_Should_ReturnCorrectBPM()
    {
        // Arrange - Create beats at 120 BPM (0.5 second intervals)
        var beats = new List<BeatMarker>();
        for (int i = 0; i < 10; i++)
        {
            beats.Add(new BeatMarker(
                Timestamp: i * 0.5,
                Strength: 0.8,
                IsDownbeat: i % 4 == 0,
                MusicalPhrase: i / 16
            ));
        }

        // Act
        var bpm = _service.CalculateBPM(beats);

        // Assert
        Assert.InRange(bpm, 118, 122); // Allow small tolerance
    }

    [Fact]
    public void IdentifyMusicalPhrases_Should_GroupBeats()
    {
        // Arrange - Create downbeats
        var beats = new List<BeatMarker>();
        for (int i = 0; i < 32; i++)
        {
            beats.Add(new BeatMarker(
                Timestamp: i * 0.5,
                Strength: 0.8,
                IsDownbeat: i % 4 == 0,
                MusicalPhrase: i / 32
            ));
        }

        // Act
        var phrases = _service.IdentifyMusicalPhrases(beats);

        // Assert
        Assert.NotEmpty(phrases);
        Assert.All(phrases, phrase =>
        {
            Assert.True(phrase.Start < phrase.End);
        });
    }

    [Fact]
    public void FindClimaxMoments_Should_ReturnStrongestBeats()
    {
        // Arrange
        var beats = new List<BeatMarker>
        {
            new(0.0, 0.5, true, 0),
            new(0.5, 0.9, false, 0),
            new(1.0, 0.7, false, 0),
            new(1.5, 0.95, true, 0),
            new(2.0, 0.6, false, 0),
        };

        // Act
        var climaxMoments = _service.FindClimaxMoments(beats, 2);

        // Assert
        Assert.Equal(2, climaxMoments.Count);
        Assert.Contains(TimeSpan.FromSeconds(1.5), climaxMoments); // 0.95 strength
        Assert.Contains(TimeSpan.FromSeconds(0.5), climaxMoments); // 0.9 strength
    }
}

public class VoiceDirectionServiceTests
{
    private readonly VoiceDirectionService _service;
    private readonly ILlmProvider _mockLlmProvider;

    public VoiceDirectionServiceTests()
    {
        _mockLlmProvider = new Aura.Providers.Llm.RuleBasedLlmProvider(
            NullLogger<Aura.Providers.Llm.RuleBasedLlmProvider>.Instance);
        _service = new VoiceDirectionService(
            NullLogger<VoiceDirectionService>.Instance,
            _mockLlmProvider);
    }

    [Fact]
    public async Task GenerateVoiceDirectionAsync_Should_ReturnDirections()
    {
        // Arrange
        var script = "Welcome to our video! This is amazing content. Are you ready?";

        // Act
        var directions = await _service.GenerateVoiceDirectionAsync(
            script, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(directions);
        Assert.NotEmpty(directions);
    }

    [Fact]
    public async Task GenerateVoiceDirectionAsync_Should_DetectExcitement()
    {
        // Arrange
        var script = "This is incredible! Amazing results! Wow!";

        // Act
        var directions = await _service.GenerateVoiceDirectionAsync(
            script, null, null, null, CancellationToken.None);

        // Assert
        Assert.Contains(directions, d => d.Emotion == EmotionalDelivery.Excited);
    }

    [Fact]
    public async Task GenerateVoiceDirectionAsync_Should_FindEmphasisWords()
    {
        // Arrange
        var script = "This is very IMPORTANT and CRITICAL information.";
        var keyMessages = new List<string> { "important", "critical" };

        // Act
        var directions = await _service.GenerateVoiceDirectionAsync(
            script, null, null, keyMessages, CancellationToken.None);

        // Assert
        var allEmphasisWords = directions.SelectMany(d => d.EmphasisWords).ToList();
        Assert.Contains("IMPORTANT", allEmphasisWords, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("CRITICAL", allEmphasisWords, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateVoiceDirectionAsync_Should_IdentifyPauses()
    {
        // Arrange
        var script = "Hello, world. This is a test, right?";

        // Act
        var directions = await _service.GenerateVoiceDirectionAsync(
            script, null, null, null, CancellationToken.None);

        // Assert
        Assert.All(directions, d =>
        {
            if (d.Pauses.Count > 0)
            {
                Assert.All(d.Pauses, pause =>
                {
                    Assert.True(pause.CharacterPosition >= 0);
                    Assert.True(pause.Duration > TimeSpan.Zero);
                });
            }
        });
    }
}

public class AudioMixingServiceTests
{
    private readonly AudioMixingService _service;

    public AudioMixingServiceTests()
    {
        _service = new AudioMixingService(NullLogger<AudioMixingService>.Instance);
    }

    [Theory]
    [InlineData("educational", true, true, false)]
    [InlineData("corporate", true, true, false)]
    [InlineData("gaming", true, true, true)]
    public async Task GenerateMixingSuggestionsAsync_Should_ReturnValidMixing(
        string contentType, bool hasNarration, bool hasMusic, bool hasSfx)
    {
        // Act
        var mixing = await _service.GenerateMixingSuggestionsAsync(
            contentType, hasNarration, hasMusic, hasSfx, -14.0, CancellationToken.None);

        // Assert
        Assert.NotNull(mixing);
        Assert.True(mixing.TargetLUFS >= -18 && mixing.TargetLUFS <= -10);
    }

    [Fact]
    public async Task GenerateMixingSuggestionsAsync_Educational_Should_PrioritizeVoice()
    {
        // Act
        var mixing = await _service.GenerateMixingSuggestionsAsync(
            "educational", true, true, false, -14.0, CancellationToken.None);

        // Assert
        Assert.True(mixing.NarrationVolume > mixing.MusicVolume);
        Assert.True(Math.Abs(mixing.Ducking.DuckDepthDb) >= 10); // Strong ducking
    }

    [Fact]
    public async Task GenerateMixingSuggestionsAsync_Should_IncludeDuckingForNarrationAndMusic()
    {
        // Act
        var mixing = await _service.GenerateMixingSuggestionsAsync(
            "default", true, true, false, -14.0, CancellationToken.None);

        // Assert
        Assert.NotNull(mixing.Ducking);
        Assert.True(mixing.Ducking.DuckDepthDb < 0); // Negative dB reduction
        Assert.True(mixing.Ducking.AttackTime > TimeSpan.Zero);
        Assert.True(mixing.Ducking.ReleaseTime > TimeSpan.Zero);
    }

    [Fact]
    public void ValidateMixing_Should_DetectInvalidLUFS()
    {
        // Arrange
        var invalidMixing = new AudioMixing(
            MusicVolume: 50,
            NarrationVolume: 100,
            SoundEffectsVolume: 50,
            Ducking: new DuckingSettings(-12, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 0.02),
            EQ: new EqualizationSettings(80, 3, -4),
            Compression: new CompressionSettings(-18, 3, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(250), 5),
            Normalize: true,
            TargetLUFS: -20 // Too low!
        );

        // Act
        var (isValid, issues) = _service.ValidateMixing(invalidMixing);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, issue => issue.Contains("LUFS"));
    }

    [Fact]
    public void GenerateFFmpegMixingFilter_Should_IncludeAllComponents()
    {
        // Arrange
        var mixing = new AudioMixing(
            MusicVolume: 35,
            NarrationVolume: 100,
            SoundEffectsVolume: 50,
            Ducking: new DuckingSettings(-12, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 0.02),
            EQ: new EqualizationSettings(80, 3, -4),
            Compression: new CompressionSettings(-18, 3, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(250), 5),
            Normalize: true,
            TargetLUFS: -14
        );

        // Act
        var filter = _service.GenerateFFmpegMixingFilter(mixing);

        // Assert
        Assert.Contains("highpass", filter);
        Assert.Contains("equalizer", filter);
        Assert.Contains("acompressor", filter);
        Assert.Contains("loudnorm", filter);
    }
}

public class SoundEffectServiceTests
{
    private readonly SoundEffectService _service;
    private readonly ILlmProvider _mockLlmProvider;

    public SoundEffectServiceTests()
    {
        _mockLlmProvider = new Aura.Providers.Llm.RuleBasedLlmProvider(
            NullLogger<Aura.Providers.Llm.RuleBasedLlmProvider>.Instance);
        _service = new SoundEffectService(
            NullLogger<SoundEffectService>.Instance,
            _mockLlmProvider);
    }

    [Fact]
    public async Task SuggestSoundEffectsAsync_Should_ReturnEffects()
    {
        // Arrange
        var script = "Click the button to reveal the results!";

        // Act
        var effects = await _service.SuggestSoundEffectsAsync(
            script, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(effects);
        Assert.NotEmpty(effects);
    }

    [Fact]
    public async Task SuggestSoundEffectsAsync_Should_DetectUIElements()
    {
        // Arrange
        var script = "Click here, tap that button, and select your choice.";

        // Act
        var effects = await _service.SuggestSoundEffectsAsync(
            script, null, null, CancellationToken.None);

        // Assert
        Assert.Contains(effects, e => e.Type == SoundEffectType.UI || e.Type == SoundEffectType.Click);
    }

    [Fact]
    public async Task SuggestSoundEffectsAsync_Should_DetectImpacts()
    {
        // Arrange
        var script = "And then it revealed the amazing transformation!";

        // Act
        var effects = await _service.SuggestSoundEffectsAsync(
            script, null, null, CancellationToken.None);

        // Assert
        Assert.Contains(effects, e => e.Type == SoundEffectType.Impact || e.Type == SoundEffectType.Transition);
    }

    [Fact]
    public void OptimizeTiming_Should_PreventOverlaps()
    {
        // Arrange
        var effects = new List<SoundEffect>
        {
            new("sfx1", SoundEffectType.Click, "Click 1", TimeSpan.FromSeconds(1.0), TimeSpan.FromMilliseconds(100), 50, "Test", null),
            new("sfx2", SoundEffectType.Click, "Click 2", TimeSpan.FromSeconds(1.05), TimeSpan.FromMilliseconds(100), 50, "Test", null),
            new("sfx3", SoundEffectType.Click, "Click 3", TimeSpan.FromSeconds(2.0), TimeSpan.FromMilliseconds(100), 50, "Test", null),
        };

        // Act
        var optimized = _service.OptimizeTiming(effects, TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.Equal(3, optimized.Count);
        
        // Check that effects are properly spaced
        for (int i = 1; i < optimized.Count; i++)
        {
            var prev = optimized[i - 1];
            var curr = optimized[i];
            var gap = curr.Timestamp - (prev.Timestamp + prev.Duration);
            Assert.True(gap >= TimeSpan.FromMilliseconds(500));
        }
    }
}

public class AudioContinuityServiceTests
{
    private readonly AudioContinuityService _service;

    public AudioContinuityServiceTests()
    {
        _service = new AudioContinuityService(NullLogger<AudioContinuityService>.Instance);
    }

    [Fact]
    public async Task CheckContinuityAsync_Should_ReturnContinuityReport()
    {
        // Arrange
        var segments = new List<string> { "segment1.wav", "segment2.wav", "segment3.wav" };

        // Act
        var continuity = await _service.CheckContinuityAsync(
            segments, "professional", CancellationToken.None);

        // Assert
        Assert.NotNull(continuity);
        Assert.True(continuity.StyleConsistencyScore >= 0);
        Assert.True(continuity.StyleConsistencyScore <= 100);
        Assert.True(continuity.VolumeConsistencyScore >= 0);
        Assert.True(continuity.VolumeConsistencyScore <= 100);
    }

    [Fact]
    public async Task AnalyzeSynchronizationAsync_Should_IdentifySyncPoints()
    {
        // Arrange
        var audioBeats = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(1.0),
            TimeSpan.FromSeconds(2.0),
            TimeSpan.FromSeconds(3.0),
        };
        var visualTransitions = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(0.95),
            TimeSpan.FromSeconds(2.1),
            TimeSpan.FromSeconds(3.0),
        };

        // Act
        var analysis = await _service.AnalyzeSynchronizationAsync(
            audioBeats, visualTransitions, TimeSpan.FromSeconds(10), CancellationToken.None);

        // Assert
        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.SyncPoints);
        Assert.True(analysis.OverallSyncScore >= 0);
        Assert.True(analysis.OverallSyncScore <= 100);
    }

    [Fact]
    public void SuggestTransitions_Should_ReturnTransitions()
    {
        // Arrange
        var sceneBoundaries = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(15),
        };
        var sceneMoods = new List<MusicMood>
        {
            MusicMood.Calm,
            MusicMood.Energetic,
            MusicMood.Calm,
        };

        // Act
        var transitions = _service.SuggestTransitions(sceneBoundaries, sceneMoods);

        // Assert
        Assert.Equal(2, transitions.Count); // N-1 transitions for N scenes
        Assert.All(transitions, t =>
        {
            Assert.NotNull(t.TransitionType);
            Assert.True(t.Duration > TimeSpan.Zero);
        });
    }
}
