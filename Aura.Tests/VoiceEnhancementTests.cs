using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Core.Services.VoiceEnhancement;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class VoiceProcessingServiceTests : IDisposable
{
    private readonly VoiceProcessingService _service;
    private readonly NoiseReductionService _noiseReduction;
    private readonly EqualizeService _equalizer;
    private readonly ProsodyAdjustmentService _prosody;
    private readonly EmotionDetectionService _emotionDetection;
    private readonly string _tempDirectory;

    public VoiceProcessingServiceTests()
    {
        _noiseReduction = new NoiseReductionService(NullLogger<NoiseReductionService>.Instance);
        _equalizer = new EqualizeService(NullLogger<EqualizeService>.Instance);
        _prosody = new ProsodyAdjustmentService(NullLogger<ProsodyAdjustmentService>.Instance);
        _emotionDetection = new EmotionDetectionService(NullLogger<EmotionDetectionService>.Instance);
        
        _service = new VoiceProcessingService(
            NullLogger<VoiceProcessingService>.Instance,
            _noiseReduction,
            _equalizer,
            _prosody,
            _emotionDetection);

        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Constructor_Should_CreateInstance()
    {
        // Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public void CreatePipeline_Should_ReturnPipeline()
    {
        // Act
        var pipeline = _service.CreatePipeline();

        // Assert
        Assert.NotNull(pipeline);
        Assert.Equal(0, pipeline.EffectCount);
    }

    [Fact]
    public async Task AnalyzeQualityAsync_Should_ReturnMetrics()
    {
        // Arrange
        var testFile = CreateTestAudioFile();

        // Act
        var metrics = await _service.AnalyzeQualityAsync(testFile, CancellationToken.None);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.SignalToNoiseRatio > 0);
        Assert.True(metrics.ClarityScore >= 0 && metrics.ClarityScore <= 1);
    }

    [Fact]
    public async Task EnhanceVoiceAsync_Should_ThrowOnMissingFile()
    {
        // Arrange
        var config = new VoiceEnhancementConfig
        {
            EnableNoiseReduction = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _service.EnhanceVoiceAsync(
                "nonexistent.wav",
                config,
                CancellationToken.None));
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(true, true, false, false)]
    public async Task EnhanceVoiceAsync_Should_ApplySelectedEnhancements(
        bool enableNoise,
        bool enableEq,
        bool enableProsody,
        bool enableEmotion)
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        var config = new VoiceEnhancementConfig
        {
            EnableNoiseReduction = enableNoise,
            EnableEqualization = enableEq,
            EnableProsodyAdjustment = enableProsody,
            Prosody = enableProsody ? new ProsodySettings { PitchShift = 2 } : null,
            EnableEmotionEnhancement = enableEmotion,
            TargetEmotion = enableEmotion ? new EmotionTarget { Emotion = EmotionType.Happy } : null
        };

        // Act
        var result = await _service.EnhanceVoiceAsync(testFile, config, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.OutputPath);
        Assert.True(result.ProcessingTimeMs >= 0);
        Assert.NotEmpty(result.Messages);
    }

    private string CreateTestAudioFile()
    {
        var filePath = Path.Combine(_tempDirectory, "test_audio.wav");
        
        // Create a minimal valid WAV file header
        using (var writer = new BinaryWriter(File.Create(filePath)))
        {
            // RIFF header
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36); // File size - 8
            writer.Write(new[] { 'W', 'A', 'V', 'E' });

            // fmt chunk
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (PCM)
            writer.Write((short)2); // Num channels
            writer.Write(48000); // Sample rate
            writer.Write(192000); // Byte rate
            writer.Write((short)4); // Block align
            writer.Write((short)16); // Bits per sample

            // data chunk
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(0); // Data size
        }

        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}

public class NoiseReductionServiceTests
{
    private readonly NoiseReductionService _service;

    public NoiseReductionServiceTests()
    {
        _service = new NoiseReductionService(NullLogger<NoiseReductionService>.Instance);
    }

    [Fact]
    public void Constructor_Should_CreateInstance()
    {
        Assert.NotNull(_service);
    }

    [Fact]
    public void Cleanup_Should_NotThrow()
    {
        // Act & Assert
        _service.Cleanup();
    }
}

public class EqualizeServiceTests
{
    private readonly EqualizeService _service;

    public EqualizeServiceTests()
    {
        _service = new EqualizeService(NullLogger<EqualizeService>.Instance);
    }

    [Fact]
    public void Constructor_Should_CreateInstance()
    {
        Assert.NotNull(_service);
    }
}

public class ProsodyAdjustmentServiceTests
{
    private readonly ProsodyAdjustmentService _service;

    public ProsodyAdjustmentServiceTests()
    {
        _service = new ProsodyAdjustmentService(NullLogger<ProsodyAdjustmentService>.Instance);
    }

    [Fact]
    public void Constructor_Should_CreateInstance()
    {
        Assert.NotNull(_service);
    }
}

public class EmotionDetectionServiceTests
{
    private readonly EmotionDetectionService _service;

    public EmotionDetectionServiceTests()
    {
        _service = new EmotionDetectionService(NullLogger<EmotionDetectionService>.Instance);
    }

    [Fact]
    public void Constructor_Should_CreateInstance()
    {
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task DetectEmotionAsync_Should_ReturnResult()
    {
        // Arrange - use any file path, the service will handle missing files gracefully
        var testPath = "test.wav";

        // Act
        var result = await _service.DetectEmotionAsync(testPath, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(Enum.IsDefined(typeof(EmotionType), result.Emotion));
        Assert.True(result.Confidence >= 0 && result.Confidence <= 1);
    }

    [Fact]
    public async Task AnalyzeEmotionalArcAsync_Should_ReturnArc()
    {
        // Arrange
        var paths = new[] { "test1.wav", "test2.wav", "test3.wav" };

        // Act
        var arc = await _service.AnalyzeEmotionalArcAsync(paths, CancellationToken.None);

        // Assert
        Assert.NotNull(arc);
        Assert.Equal(3, arc.Segments.Length);
        Assert.True(Enum.IsDefined(typeof(EmotionType), arc.DominantEmotion));
        Assert.True(arc.EmotionalVariety >= 0 && arc.EmotionalVariety <= 1);
    }
}

public class VoiceProcessingPipelineTests
{
    [Fact]
    public void Pipeline_Should_ChainEffects()
    {
        // Arrange
        var pipeline = new VoiceProcessingPipeline(
            NullLogger.Instance,
            Path.GetTempPath());

        // Act
        pipeline
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_1"; })
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_2"; })
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_3"; });

        // Assert
        Assert.Equal(3, pipeline.EffectCount);
    }

    [Fact]
    public async Task Pipeline_Should_ProcessInOrder()
    {
        // Arrange
        var pipeline = new VoiceProcessingPipeline(
            NullLogger.Instance,
            Path.GetTempPath());

        pipeline
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_A"; })
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_B"; })
            .AddEffect(async (input, ct) => { await Task.Delay(1, ct); return input + "_C"; });

        // Act
        var result = await pipeline.ProcessAsync("test", CancellationToken.None);

        // Assert
        Assert.Equal("test_A_B_C", result);
    }
}
