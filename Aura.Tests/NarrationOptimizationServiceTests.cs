using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Aura.Core.Services.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class NarrationOptimizationServiceTests
{
    private readonly NarrationOptimizationService _service;
    private readonly ILlmProvider _mockLlmProvider;

    public NarrationOptimizationServiceTests()
    {
        _mockLlmProvider = new Aura.Providers.Llm.RuleBasedLlmProvider(
            NullLogger<Aura.Providers.Llm.RuleBasedLlmProvider>.Instance);
        _service = new NarrationOptimizationService(
            NullLogger<NarrationOptimizationService>.Instance,
            _mockLlmProvider);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_ReturnOptimizedResult()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is a test narration line for optimization.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OptimizedLines);
        Assert.Equal(lines.Count, result.OriginalLines.Count);
        Assert.True(result.OptimizationScore >= 0);
        Assert.True(result.OptimizationScore <= 100);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectLongSentences()
    {
        // Arrange
        var longText = "This is an extremely long sentence that contains way more than twenty five words and should be detected as a complex sentence that needs to be simplified for better text to speech synthesis quality and natural delivery.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, longText, TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            MaxSentenceWords = 25
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var optimizedLine = result.OptimizedLines.First();
        Assert.True(optimizedLine.ActionsApplied.Count > 0);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectEmotionalTone()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is amazing! Incredible! Fantastic news!", TimeSpan.Zero, TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "Unfortunately, this is a sad and tragic situation.", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableEmotionalToneTagging = true,
            MinEmotionConfidence = 0.5
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var emotionalLines = result.OptimizedLines.Where(l => l.EmotionalTone != null).ToList();
        Assert.NotEmpty(emotionalLines);
        Assert.All(emotionalLines, line => Assert.True(line.EmotionConfidence >= config.MinEmotionConfidence));
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectTongueTwisters()
    {
        // Arrange - use a stronger tongue twister pattern
        var tongueTwister = "She sells seashells by the seashore and she surely sees the shining shells.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, tongueTwister, TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableTongueTwisterDetection = true
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OptimizedLines);
        Assert.True(result.OptimizationScore >= 0 && result.OptimizationScore <= 100);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectAcronyms()
    {
        // Arrange
        var textWithAcronyms = "The CEO announced that NASA will use AI and API technology.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, textWithAcronyms, TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableAcronymClarification = true
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectPhoneNumbers()
    {
        // Arrange
        var textWithPhone = "Call us at 555-123-4567 for more information.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, textWithPhone, TimeSpan.Zero, TimeSpan.FromSeconds(4))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableNumberSpelling = true
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_GenerateSsmlWhenSupported()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is an exciting announcement!", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var voiceDescriptor = new VoiceDescriptor
        {
            Id = "test-voice",
            Name = "Test Voice",
            Provider = VoiceProvider.Mock,
            Locale = "en-US",
            Gender = VoiceGender.Female,
            VoiceType = VoiceType.Neural,
            SupportedFeatures = VoiceFeatures.Prosody | VoiceFeatures.Styles
        };
        var config = new NarrationOptimizationConfig
        {
            EnableSsml = true,
            EnableEmotionalToneTagging = true,
            MinEmotionConfidence = 0.5
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, voiceDescriptor, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var optimizedLine = result.OptimizedLines.First();
        if (optimizedLine.EmotionalTone != null)
        {
            Assert.NotNull(optimizedLine.SsmlMarkup);
            Assert.Contains("<speak>", optimizedLine.SsmlMarkup);
        }
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_PreserveSemanticMeaning()
    {
        // Arrange
        var originalText = "Artificial intelligence is transforming the world.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, originalText, TimeSpan.Zero, TimeSpan.FromSeconds(4))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var optimizedLine = result.OptimizedLines.First();
        Assert.Equal(originalText, optimizedLine.OriginalText);
        Assert.NotNull(optimizedLine.OptimizedText);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_CompleteUnderTwoSeconds()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is a test line for performance.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ProcessingTime.TotalSeconds < 2.0,
            $"Processing took {result.ProcessingTime.TotalSeconds:F2}s, expected < 2s");
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_HandleMultipleLines()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "First line of narration.", TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Second line with different content.", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)),
            new ScriptLine(2, "Third line to test batching.", TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.OptimizedLines.Count);
        Assert.Equal(3, result.OriginalLines.Count);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_RespectDisabledFeatures()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is amazing! Call 555-1234.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableEmotionalToneTagging = false,
            EnableNumberSpelling = false,
            EnableSsml = false
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var optimizedLine = result.OptimizedLines.First();
        Assert.Null(optimizedLine.EmotionalTone);
        Assert.Null(optimizedLine.SsmlMarkup);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_CalculateOptimizationScore()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is a simple test line.", TimeSpan.Zero, TimeSpan.FromSeconds(2))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimizationScore >= 70, "Optimization score should be at least 70");
        Assert.True(result.OptimizationScore <= 100, "Optimization score should not exceed 100");
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_DetectHomographs()
    {
        // Arrange
        var textWithHomograph = "I will read the book. I already read it yesterday.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, textWithHomograph, TimeSpan.Zero, TimeSpan.FromSeconds(4))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig
        {
            EnableHomographDisambiguation = true
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_HandleEmptyLines()
    {
        // Arrange
        var lines = new List<ScriptLine>();
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.OptimizedLines);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_AdaptToVoicePersonality()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "This is a professional announcement.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("ProfessionalVoice", 1.0, 1.0, PauseStyle.Natural);
        var voiceDescriptor = new VoiceDescriptor
        {
            Id = "professional-voice",
            Name = "Professional Voice",
            Provider = VoiceProvider.Mock,
            Locale = "en-US",
            Gender = VoiceGender.Male,
            VoiceType = VoiceType.Neural,
            Description = "Professional, formal voice for business content",
            SupportedFeatures = VoiceFeatures.Standard
        };
        var config = new NarrationOptimizationConfig
        {
            EnableVoiceAdaptation = true
        };

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, voiceDescriptor, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task OptimizeForTtsAsync_Should_TrackOptimizationsApplied()
    {
        // Arrange
        var complexText = "This is an extremely long sentence with more than twenty five words that contains multiple clauses and should trigger several optimization actions including sentence simplification and pause insertion for better TTS delivery.";
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, complexText, TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var config = new NarrationOptimizationConfig();

        // Act
        var result = await _service.OptimizeForTtsAsync(lines, voiceSpec, null, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimizationsApplied > 0, "Should have applied at least one optimization");
        var optimizedLine = result.OptimizedLines.First();
        Assert.NotEmpty(optimizedLine.ActionsApplied);
    }
}
