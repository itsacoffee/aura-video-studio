using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.AudioIntelligence;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Audio;

public class VoiceEnhancementServiceTests2 : IDisposable
{
    private readonly Mock<IFFmpegService> _ffmpegServiceMock;
    private readonly VoiceEnhancementService _service;
    private readonly string _tempDirectory;

    public VoiceEnhancementServiceTests2()
    {
        _ffmpegServiceMock = new Mock<IFFmpegService>();
        _service = new VoiceEnhancementService(
            NullLogger<VoiceEnhancementService>.Instance,
            _ffmpegServiceMock.Object);

        _tempDirectory = Path.Combine(Path.GetTempPath(), "VoiceEnhancementTests2", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData(VoiceEnhancementPreset.Light)]
    [InlineData(VoiceEnhancementPreset.Standard)]
    [InlineData(VoiceEnhancementPreset.Broadcast)]
    [InlineData(VoiceEnhancementPreset.Podcast)]
    [InlineData(VoiceEnhancementPreset.VideoNarration)]
    public void GetPreset_ReturnsValidOptions(VoiceEnhancementPreset preset)
    {
        // Act
        var options = _service.GetPreset(preset);

        // Assert
        Assert.NotNull(options);
        Assert.True(options.HighPassFrequency >= 70 && options.HighPassFrequency <= 120);
        Assert.True(options.TargetLUFS >= -18 && options.TargetLUFS <= -12);
    }

    [Fact]
    public void GetPreset_LightHasMinimalProcessing()
    {
        // Act
        var options = _service.GetPreset(VoiceEnhancementPreset.Light);

        // Assert
        Assert.True(options.EnableNoiseReduction);
        Assert.True(options.NoiseReductionStrength < 0.5);
        Assert.False(options.EnableCompression);
        Assert.False(options.EnableEQ);
    }

    [Fact]
    public void GetPreset_BroadcastHasAggressiveProcessing()
    {
        // Act
        var options = _service.GetPreset(VoiceEnhancementPreset.Broadcast);

        // Assert
        Assert.True(options.EnableNoiseReduction);
        Assert.True(options.NoiseReductionStrength >= 0.6);
        Assert.True(options.EnableCompression);
        Assert.True(options.EnableEQ);
        Assert.True(options.CompressionRatio >= 3);
    }

    [Fact]
    public void BuildFilterChain_IncludesNoiseReduction()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act
        var filter = _service.BuildFilterChain(options);

        // Assert
        Assert.Contains("afftdn", filter);
    }

    [Fact]
    public void BuildFilterChain_IncludesHighPass()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act
        var filter = _service.BuildFilterChain(options);

        // Assert
        Assert.Contains("highpass", filter);
    }

    [Fact]
    public void BuildFilterChain_IncludesCompression()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act
        var filter = _service.BuildFilterChain(options);

        // Assert
        Assert.Contains("acompressor", filter);
    }

    [Fact]
    public void BuildFilterChain_IncludesLoudnorm()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act
        var filter = _service.BuildFilterChain(options);

        // Assert
        Assert.Contains("loudnorm", filter);
    }

    [Fact]
    public void BuildFilterChain_IncludesDeClick()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act
        var filter = _service.BuildFilterChain(options);

        // Assert
        Assert.Contains("adeclick", filter);
    }

    [Fact]
    public async Task AnalyzeVoiceAsync_ReturnsAnalysis()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        SetupMockFFmpegAnalysis();

        // Act
        var result = await _service.AnalyzeVoiceAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Duration > TimeSpan.Zero);
        Assert.NotEmpty(result.Issues);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task AnalyzeVoiceAsync_DetectsClipping()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-f null")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Duration: 00:00:10.00" });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("loudnorm")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Input Integrated: -14.0 LUFS\nInput True Peak: 0.0 dBTP\nInput LRA: 8.0 LU" });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("silencedetect")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "" });

        // Act
        var result = await _service.AnalyzeVoiceAsync(testFile);

        // Assert
        Assert.True(result.HasClipping);
        Assert.Contains(result.Issues, i => i.Contains("clipping", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeVoiceAsync_ThrowsOnMissingFile()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.AnalyzeVoiceAsync("/nonexistent/file.wav"));
    }

    [Fact]
    public async Task EnhanceVoiceAsync_ReturnsSuccessResult()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        SetupMockFFmpegAnalysis();
        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-af")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true });

        // Create output file to simulate FFmpeg success
        File.WriteAllBytes(outputFile, new byte[100]);

        // Act
        var result = await _service.EnhanceVoiceAsync(testFile, outputFile, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(outputFile, result.OutputPath);
        Assert.NotEmpty(result.AppliedEnhancements);
        Assert.True(result.ProcessingTime.TotalMilliseconds >= 0);
    }

    [Fact]
    public async Task EnhanceVoiceAsync_IncludesAppliedEnhancements()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        var options = _service.GetPreset(VoiceEnhancementPreset.Broadcast);

        SetupMockFFmpegAnalysis();
        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-af")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true });

        File.WriteAllBytes(outputFile, new byte[100]);

        // Act
        var result = await _service.EnhanceVoiceAsync(testFile, outputFile, options);

        // Assert
        Assert.Contains(result.AppliedEnhancements, e => e.Contains("Noise reduction"));
        Assert.Contains(result.AppliedEnhancements, e => e.Contains("Compression"));
        Assert.Contains(result.AppliedEnhancements, e => e.Contains("Loudness normalization"));
    }

    [Fact]
    public async Task EnhanceVoiceAsync_ThrowsOnMissingFile()
    {
        // Arrange
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.EnhanceVoiceAsync("/nonexistent.wav", "/output.wav", options));
    }

    [Fact]
    public async Task EnhanceVoiceAsync_ReturnsFailureOnFFmpegError()
    {
        // Arrange
        var testFile = CreateTestAudioFile();
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        var options = _service.GetPreset(VoiceEnhancementPreset.Standard);

        SetupMockFFmpegAnalysis();
        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-af")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = false, ErrorMessage = "FFmpeg error" });

        // Act
        var result = await _service.EnhanceVoiceAsync(testFile, outputFile, options);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    private void SetupMockFFmpegAnalysis()
    {
        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("-f null")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Duration: 00:00:10.00" });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("loudnorm")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "Input Integrated: -18.0 LUFS\nInput True Peak: -2.0 dBTP\nInput LRA: 8.0 LU" });

        _ffmpegServiceMock.Setup(x => x.ExecuteAsync(It.Is<string>(s => s.Contains("silencedetect")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardError = "silence_start: 1.0\nsilence_start: 2.0\nsilence_start: 3.0" });
    }

    private string CreateTestAudioFile()
    {
        var filePath = Path.Combine(_tempDirectory, $"test_{Guid.NewGuid()}.wav");

        using (var writer = new BinaryWriter(File.Create(filePath)))
        {
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
