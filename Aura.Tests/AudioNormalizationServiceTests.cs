using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Services.AudioIntelligence;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class AudioNormalizationServiceTests
{
    private readonly Mock<IFFmpegService> _mockFFmpeg;
    private readonly AudioNormalizationService _service;

    public AudioNormalizationServiceTests()
    {
        _mockFFmpeg = new Mock<IFFmpegService>();
        _service = new AudioNormalizationService(
            NullLogger<AudioNormalizationService>.Instance,
            _mockFFmpeg.Object);
    }

    [Fact]
    public async Task NormalizeToLUFSAsync_Should_CallFFmpegWithCorrectFilter()
    {
        // Arrange
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        var targetLUFS = -14.0;

        try
        {
            File.WriteAllText(inputPath, "dummy audio data");

            _mockFFmpeg.Setup(f => f.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FFmpegResult
                {
                    Success = true,
                    ExitCode = 0,
                    StandardOutput = string.Empty,
                    StandardError = string.Empty
                });

            // Act
            var result = await _service.NormalizeToLUFSAsync(inputPath, outputPath, targetLUFS);

            // Assert
            Assert.Equal(outputPath, result);
            _mockFFmpeg.Verify(f => f.ExecuteAsync(
                It.Is<string>(s => s.Contains($"loudnorm=I={targetLUFS}")),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task NormalizeToLUFSAsync_Should_ThrowWhenInputNotFound()
    {
        // Arrange
        var inputPath = "nonexistent.wav";
        var outputPath = Path.GetTempFileName();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.NormalizeToLUFSAsync(inputPath, outputPath, -14.0));
    }

    [Fact]
    public async Task ApplyDuckingAsync_Should_CallFFmpegWithSidechainFilter()
    {
        // Arrange
        var musicPath = Path.GetTempFileName();
        var narrationPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var settings = new DuckingSettings(
            DuckDepthDb: -12.0,
            AttackTime: TimeSpan.FromMilliseconds(100),
            ReleaseTime: TimeSpan.FromMilliseconds(500),
            Threshold: 0.02);

        try
        {
            File.WriteAllText(musicPath, "dummy music");
            File.WriteAllText(narrationPath, "dummy narration");

            _mockFFmpeg.Setup(f => f.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FFmpegResult
                {
                    Success = true,
                    ExitCode = 0
                });

            // Act
            var result = await _service.ApplyDuckingAsync(musicPath, narrationPath, outputPath, settings);

            // Assert
            Assert.Equal(outputPath, result);
            _mockFFmpeg.Verify(f => f.ExecuteAsync(
                It.Is<string>(s => s.Contains("sidechaincompress")),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(musicPath)) File.Delete(musicPath);
            if (File.Exists(narrationPath)) File.Delete(narrationPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ApplyCompressionAsync_Should_UseCorrectParameters()
    {
        // Arrange
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var settings = new CompressionSettings(
            Threshold: -18.0,
            Ratio: 3.0,
            AttackTime: TimeSpan.FromMilliseconds(20),
            ReleaseTime: TimeSpan.FromMilliseconds(250),
            MakeupGain: 5.0);

        try
        {
            File.WriteAllText(inputPath, "dummy audio");

            _mockFFmpeg.Setup(f => f.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FFmpegResult
                {
                    Success = true,
                    ExitCode = 0
                });

            // Act
            var result = await _service.ApplyCompressionAsync(inputPath, outputPath, settings);

            // Assert
            Assert.Equal(outputPath, result);
            _mockFFmpeg.Verify(f => f.ExecuteAsync(
                It.Is<string>(s =>
                    s.Contains("acompressor") &&
                    s.Contains("threshold=-18") &&
                    s.Contains("ratio=3") &&
                    s.Contains("makeup=5")),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task ApplyVoiceEQAsync_Should_ApplyHighPassAndPresenceBoost()
    {
        // Arrange
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        var settings = new EqualizationSettings(
            HighPassFrequency: 80,
            PresenceBoost: 3.0,
            DeEsserReduction: -4.0);

        try
        {
            File.WriteAllText(inputPath, "dummy audio");

            _mockFFmpeg.Setup(f => f.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FFmpegResult
                {
                    Success = true,
                    ExitCode = 0
                });

            // Act
            var result = await _service.ApplyVoiceEQAsync(inputPath, outputPath, settings);

            // Assert
            Assert.Equal(outputPath, result);
            _mockFFmpeg.Verify(f => f.ExecuteAsync(
                It.Is<string>(s =>
                    s.Contains("highpass=f=80") &&
                    s.Contains("f=4000") &&
                    s.Contains("g=3") &&
                    s.Contains("f=7000") &&
                    s.Contains("g=-4")),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task MixAudioTracksAsync_Should_HandleMultipleTracks()
    {
        // Arrange
        var track1 = Path.GetTempFileName();
        var track2 = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(track1, "track1");
            File.WriteAllText(track2, "track2");

            var tracks = new List<AudioTrackInput>
            {
                new(track1, 100.0),
                new(track2, 50.0)
            };

            var mixingSettings = new AudioMixing(
                MusicVolume: 50,
                NarrationVolume: 100,
                SoundEffectsVolume: 60,
                Ducking: new DuckingSettings(-12, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 0.02),
                EQ: new EqualizationSettings(80, 3, -4),
                Compression: new CompressionSettings(-18, 3, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(250), 5),
                Normalize: true,
                TargetLUFS: -14.0);

            _mockFFmpeg.Setup(f => f.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FFmpegResult
                {
                    Success = true,
                    ExitCode = 0
                });

            // Act
            var result = await _service.MixAudioTracksAsync(tracks, outputPath, mixingSettings);

            // Assert
            Assert.Equal(outputPath, result);
            _mockFFmpeg.Verify(f => f.ExecuteAsync(
                It.Is<string>(s => s.Contains("amix=inputs=2")),
                It.IsAny<Action<FFmpegProgress>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(track1)) File.Delete(track1);
            if (File.Exists(track2)) File.Delete(track2);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task MixAudioTracksAsync_Should_ThrowWhenNoTracks()
    {
        // Arrange
        var tracks = new List<AudioTrackInput>();
        var outputPath = Path.GetTempFileName();

        var mixingSettings = new AudioMixing(
            MusicVolume: 50,
            NarrationVolume: 100,
            SoundEffectsVolume: 60,
            Ducking: new DuckingSettings(-12, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 0.02),
            EQ: new EqualizationSettings(80, 3, -4),
            Compression: new CompressionSettings(-18, 3, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(250), 5),
            Normalize: true,
            TargetLUFS: -14.0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.MixAudioTracksAsync(tracks, outputPath, mixingSettings));
    }
}
