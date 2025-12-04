using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.AudioIntelligence;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Audio;

public class IntelligentDuckingServiceTests : IDisposable
{
    private readonly Mock<IFFmpegService> _ffmpegServiceMock;
    private readonly IntelligentDuckingService _service;
    private readonly string _tempDirectory;

    public IntelligentDuckingServiceTests()
    {
        _ffmpegServiceMock = new Mock<IFFmpegService>();
        _service = new IntelligentDuckingService(
            NullLogger<IntelligentDuckingService>.Instance,
            _ffmpegServiceMock.Object);

        _tempDirectory = Path.Combine(Path.GetTempPath(), "IntelligentDuckingTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData(DuckingProfileType.Aggressive, -20)]
    [InlineData(DuckingProfileType.Balanced, -12)]
    [InlineData(DuckingProfileType.Gentle, -6)]
    [InlineData(DuckingProfileType.Dynamic, -15)]
    public void GetDefaultProfile_ReturnsCorrectDepth(DuckingProfileType type, double expectedDepth)
    {
        // Act
        var profile = _service.GetDefaultProfile(type);

        // Assert
        Assert.Equal(type, profile.Type);
        Assert.Equal(expectedDepth, profile.DuckDepthDb);
    }

    [Fact]
    public void GetDefaultProfile_AggressiveHasFastAttack()
    {
        // Act
        var profile = _service.GetDefaultProfile(DuckingProfileType.Aggressive);

        // Assert
        Assert.True(profile.AttackTime.TotalMilliseconds < 100);
        Assert.True(profile.ReleaseTime.TotalMilliseconds < 500);
    }

    [Fact]
    public void GetDefaultProfile_GentleHasSlowAttack()
    {
        // Act
        var profile = _service.GetDefaultProfile(DuckingProfileType.Gentle);

        // Assert
        Assert.True(profile.AttackTime.TotalMilliseconds >= 100);
        Assert.True(profile.ReleaseTime.TotalMilliseconds >= 500);
    }

    [Fact]
    public async Task AnalyzeNarrationAsync_DetectsSilenceSegments()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        var ffmpegOutput = @"[silencedetect @ 0x1234] silence_start: 1.5
[silencedetect @ 0x1234] silence_end: 2.5 | silence_duration: 1.0
[silencedetect @ 0x1234] silence_start: 5.0
[silencedetect @ 0x1234] silence_end: 5.8 | silence_duration: 0.8";

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-f null")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Duration: 00:00:10.00" });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("silencedetect")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = ffmpegOutput });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("loudnorm")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Input Integrated: -18.0 LUFS\nInput True Peak: -2.0 dBTP\nInput Threshold: -28.0 LUFS" });

        // Act
        var result = await _service.AnalyzeNarrationAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SilenceSegments.Count);
        Assert.Equal(TimeSpan.FromSeconds(1.5), result.SilenceSegments[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(2.5), result.SilenceSegments[0].End);
    }

    [Fact]
    public async Task AnalyzeNarrationAsync_ThrowsOnMissingFile()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.AnalyzeNarrationAsync("/nonexistent/file.wav"));
    }

    [Theory]
    [InlineData("educational", DuckingProfileType.Aggressive)]
    [InlineData("tutorial", DuckingProfileType.Aggressive)]
    [InlineData("ambient", DuckingProfileType.Gentle)]
    [InlineData("documentary", DuckingProfileType.Dynamic)]
    [InlineData("default", DuckingProfileType.Balanced)]
    public void PlanDucking_SelectsCorrectProfile(string contentType, DuckingProfileType expectedProfile)
    {
        // Arrange
        var analysis = CreateMockAnalysis();

        // Act
        var plan = _service.PlanDucking(analysis, contentType);

        // Assert
        Assert.Equal(expectedProfile, plan.Profile.Type);
    }

    [Fact]
    public void PlanDucking_GeneratesValidFFmpegFilter()
    {
        // Arrange
        var analysis = CreateMockAnalysis();

        // Act
        var plan = _service.PlanDucking(analysis, "default");

        // Assert
        Assert.NotEmpty(plan.FFmpegFilter);
        Assert.Contains("sidechaincompress", plan.FFmpegFilter);
        Assert.Contains("threshold", plan.FFmpegFilter);
        Assert.Contains("attack", plan.FFmpegFilter);
        Assert.Contains("release", plan.FFmpegFilter);
    }

    [Fact]
    public void PlanDucking_IncludesReasoning()
    {
        // Arrange
        var analysis = CreateMockAnalysis();

        // Act
        var plan = _service.PlanDucking(analysis, "educational");

        // Assert
        Assert.NotEmpty(plan.Reasoning);
        Assert.Contains("Aggressive", plan.Reasoning);
    }

    [Fact]
    public async Task ApplyDuckingAsync_CallsFFmpegWithCorrectFilter()
    {
        // Arrange
        var narrationFile = CreateTestAudioFile();
        var musicFile = CreateTestAudioFile("music");
        var outputFile = Path.Combine(_tempDirectory, "output.wav");

        var analysis = CreateMockAnalysis();
        var plan = _service.PlanDucking(analysis, "default");

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true });

        // Act
        var result = await _service.ApplyDuckingAsync(narrationFile, musicFile, plan, outputFile);

        // Assert
        Assert.Equal(outputFile, result);
        _ffmpegServiceMock.Verify(
            x => x.ExecuteAsync(It.Is<string>(s => s.Contains("sidechaincompress")), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyDuckingAsync_ThrowsOnMissingNarration()
    {
        // Arrange
        var musicFile = CreateTestAudioFile("music");
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        var analysis = CreateMockAnalysis();
        var plan = _service.PlanDucking(analysis, "default");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ApplyDuckingAsync("/nonexistent.wav", musicFile, plan, outputFile));
    }

    [Fact]
    public async Task ApplyDuckingAsync_ThrowsOnMissingMusic()
    {
        // Arrange
        var narrationFile = CreateTestAudioFile();
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        var analysis = CreateMockAnalysis();
        var plan = _service.PlanDucking(analysis, "default");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.ApplyDuckingAsync(narrationFile, "/nonexistent.wav", plan, outputFile));
    }

    private NarrationAnalysis CreateMockAnalysis()
    {
        return new NarrationAnalysis(
            TotalDuration: TimeSpan.FromSeconds(60),
            SilenceSegments: new List<SilenceSegment>
            {
                new(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(6), -50),
                new(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(16), -48)
            },
            SpeechSegments: new List<SpeechSegment>
            {
                new(TimeSpan.Zero, TimeSpan.FromSeconds(5), -18, -10),
                new(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(15), -16, -8)
            },
            AverageLoudness: -18,
            NoiseFloor: -55,
            HasClipping: false,
            SpeechToSilenceRatio: 2.5
        );
    }

    private string CreateTestAudioFile(string prefix = "test")
    {
        var filePath = Path.Combine(_tempDirectory, $"{prefix}_{Guid.NewGuid()}.wav");

        using (var writer = new BinaryWriter(File.Create(filePath)))
        {
            // Minimal WAV header
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(44100);
            writer.Write(88200);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(0);
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
            catch { }
        }
    }
}
