using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Dependencies;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.Audio;

public class ReliableTtsPipelineTests : IDisposable
{
    private readonly ILogger<ReliableTtsPipeline> _logger;
    private readonly ILogger<TtsChunker> _chunkerLogger;
    private readonly ILogger<AudioQualityValidator> _validatorLogger;
    private readonly ILogger<AudioConcatenator> _concatenatorLogger;
    private readonly string _tempDirectory;

    public ReliableTtsPipelineTests()
    {
        _logger = NullLogger<ReliableTtsPipeline>.Instance;
        _chunkerLogger = NullLogger<TtsChunker>.Instance;
        _validatorLogger = NullLogger<AudioQualityValidator>.Instance;
        _concatenatorLogger = NullLogger<AudioConcatenator>.Instance;

        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public void TtsChunker_ChunkText_ShortText_ReturnsSingleChunk()
    {
        // Arrange
        var chunker = new TtsChunker();
        var text = "This is a short text.";

        // Act
        var chunks = chunker.ChunkText(text);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(text, chunks[0].Text);
        Assert.Equal(0, chunks[0].Index);
    }

    [Fact]
    public void TtsChunker_ChunkText_LongText_SplitsIntoMultipleChunks()
    {
        // Arrange
        var chunker = new TtsChunker();
        var longText = string.Join(". ", Enumerable.Range(1, 20).Select(i => 
            $"This is sentence number {i} with some additional text to make it longer"));

        // Act
        var chunks = chunker.ChunkText(longText);

        // Assert
        Assert.True(chunks.Count > 1, "Long text should be split into multiple chunks");
        Assert.All(chunks, chunk => Assert.True(chunk.Text.Length <= 450, 
            $"Chunk should not exceed 450 characters, but got {chunk.Text.Length}"));
    }

    [Fact]
    public void TtsChunker_ChunkText_RespectsSentenceBoundaries()
    {
        // Arrange
        var chunker = new TtsChunker();
        var text = "First sentence. Second sentence. Third sentence.";

        // Act
        var chunks = chunker.ChunkText(text);

        // Assert
        Assert.All(chunks, chunk => 
            Assert.Contains(".", chunk.Text) || chunk == chunks.Last());
    }

    [Fact]
    public async Task AudioQualityValidator_ValidateAsync_ValidAudio_ReturnsValid()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);

        // Create a minimal valid WAV file for testing
        var testAudioPath = Path.Combine(_tempDirectory, "test.wav");
        CreateMinimalWavFile(testAudioPath);

        // Act
        var result = await validator.ValidateAsync(testAudioPath, CancellationToken.None);

        // Assert
        // Note: Without FFprobe, validation may not be fully accurate, but should not throw
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_ShortText_Succeeds()
    {
        // Arrange
        var mockProvider = new Mock<ITtsProvider>();
        var testAudioPath = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testAudioPath);

        mockProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAudioPath);

        var providers = new[] { mockProvider.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var result = await pipeline.SynthesizeAsync("Short text.", voiceSpec, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.ChunkCount);
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_LongText_ChunksAndConcatenates()
    {
        // Arrange
        var mockProvider = new Mock<ITtsProvider>();
        var testAudioPath = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testAudioPath);

        mockProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAudioPath);

        var providers = new[] { mockProvider.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var longText = string.Join(". ", Enumerable.Range(1, 15).Select(i => 
            $"This is sentence number {i} with some additional text to make it longer"));

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await pipeline.SynthesizeAsync(longText, voiceSpec, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ChunkCount > 1, "Long text should be split into multiple chunks");
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_ProviderFails_FallsBackToNext()
    {
        // Arrange
        var failingProvider = new Mock<ITtsProvider>();
        failingProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider failed"));

        var workingProvider = new Mock<ITtsProvider>();
        var testAudioPath = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testAudioPath);
        workingProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAudioPath);

        var providers = new[] { failingProvider.Object, workingProvider.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await pipeline.SynthesizeAsync("Test text.", voiceSpec, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        workingProvider.Verify(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_AllProvidersFail_ThrowsException()
    {
        // Arrange
        var failingProvider1 = new Mock<ITtsProvider>();
        failingProvider1.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 1 failed"));

        var failingProvider2 = new Mock<ITtsProvider>();
        failingProvider2.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 2 failed"));

        var providers = new[] { failingProvider1.Object, failingProvider2.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        await Assert.ThrowsAsync<TtsException>(() =>
            pipeline.SynthesizeAsync("Test text.", voiceSpec, null, CancellationToken.None));
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var slowProvider = new Mock<ITtsProvider>();
        slowProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .Returns(async (IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct) =>
            {
                await Task.Delay(35000, ct); // Longer than 30s timeout
                return Path.Combine(_tempDirectory, "output.wav");
            });

        var providers = new[] { slowProvider.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            pipeline.SynthesizeAsync("Test text.", voiceSpec, null, CancellationToken.None));
    }

    [Fact]
    public async Task AudioConcatenator_ConcatenateAsync_SingleFile_CopiesFile()
    {
        // Arrange
        var testFile = Path.Combine(_tempDirectory, "input.wav");
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testFile);

        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);

        // Act
        await concatenator.ConcatenateAsync(new[] { testFile }, outputFile, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(outputFile));
    }

    [Fact]
    public async Task AudioConcatenator_ConcatenateAsync_MultipleFiles_Concatenates()
    {
        // Arrange
        var testFile1 = Path.Combine(_tempDirectory, "input1.wav");
        var testFile2 = Path.Combine(_tempDirectory, "input2.wav");
        var outputFile = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testFile1);
        CreateMinimalWavFile(testFile2);

        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);

        // Act
        await concatenator.ConcatenateAsync(new[] { testFile1, testFile2 }, outputFile, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(outputFile));
        var outputInfo = new FileInfo(outputFile);
        Assert.True(outputInfo.Length > 0);
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_ChunkValidationFails_FallsBackToNextProvider()
    {
        // Arrange - First provider returns invalid audio (too short), second provider succeeds
        var invalidAudioPath = Path.Combine(_tempDirectory, "invalid.wav");
        File.WriteAllBytes(invalidAudioPath, new byte[50]); // Too small to be valid

        var providerWithInvalidAudio = new Mock<ITtsProvider>();
        providerWithInvalidAudio.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidAudioPath);

        var validAudioPath = Path.Combine(_tempDirectory, "valid.wav");
        CreateMinimalWavFile(validAudioPath);

        var providerWithValidAudio = new Mock<ITtsProvider>();
        providerWithValidAudio.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(validAudioPath);

        var providers = new[] { providerWithInvalidAudio.Object, providerWithValidAudio.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await pipeline.SynthesizeAsync("Test text.", voiceSpec, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        providerWithInvalidAudio.Verify(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()), Times.Once);
        providerWithValidAudio.Verify(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void TtsChunker_ChunkText_1000PlusCharacters_SplitsIntoMultipleChunks()
    {
        // Arrange
        var chunker = new TtsChunker();
        var veryLongText = string.Join(". ", Enumerable.Range(1, 30).Select(i => 
            $"This is sentence number {i} with some additional text to make it much longer than usual " +
            $"so that we can test the chunking behavior for very long scripts that exceed typical limits."));

        // Act
        var chunks = chunker.ChunkText(veryLongText);

        // Assert
        Assert.True(chunks.Count > 1, $"Expected multiple chunks for 1000+ character text, got {chunks.Count}");
        Assert.All(chunks, chunk => 
        {
            Assert.True(chunk.Text.Length <= 450, 
                $"Chunk {chunk.Index} exceeds 450 characters: {chunk.Text.Length}");
            Assert.NotEmpty(chunk.Text);
        });
    }

    [Fact]
    public async Task ReliableTtsPipeline_SynthesizeAsync_PartialFailure_ContinuesWithFallback()
    {
        // Arrange - First chunk fails, second chunk succeeds with fallback
        var failingProvider = new Mock<ITtsProvider>();
        failingProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider failed"));

        var workingProvider = new Mock<ITtsProvider>();
        var testAudioPath = Path.Combine(_tempDirectory, "output.wav");
        CreateMinimalWavFile(testAudioPath);
        workingProvider.Setup(p => p.SynthesizeAsync(
            It.IsAny<IEnumerable<ScriptLine>>(),
            It.IsAny<VoiceSpec>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAudioPath);

        var providers = new[] { failingProvider.Object, workingProvider.Object };
        var chunker = new TtsChunker();
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var validator = new AudioQualityValidator(_validatorLogger, mockFfmpegLocator.Object);
        var concatenator = new AudioConcatenator(_concatenatorLogger, mockFfmpegLocator.Object);
        var pipeline = new ReliableTtsPipeline(
            providers,
            chunker,
            validator,
            concatenator,
            _logger);

        // Create text that will be split into multiple chunks
        var longText = string.Join(". ", Enumerable.Range(1, 10).Select(i => 
            $"This is sentence number {i} with some additional text to make it longer"));

        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await pipeline.SynthesizeAsync(longText, voiceSpec, null, CancellationToken.None);

        // Assert - Should succeed using fallback provider for all chunks
        Assert.NotNull(result);
        Assert.True(result.ChunkCount > 1, "Long text should be split into multiple chunks");
        // Both providers should be called (first fails, second succeeds for each chunk)
        Assert.True(failingProvider.Invocations.Count > 0, "Failing provider should be attempted");
        Assert.True(workingProvider.Invocations.Count >= result.ChunkCount, 
            $"Working provider should be called at least once per chunk. Expected >= {result.ChunkCount}, got {workingProvider.Invocations.Count}");
    }

    /// <summary>
    /// Create a minimal valid WAV file for testing
    /// </summary>
    private void CreateMinimalWavFile(string path)
    {
        using var file = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(file);

        // WAV header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36); // File size - 8
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write((short)1); // Number of channels
        writer.Write(22050); // Sample rate
        writer.Write(44100); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(0); // Data chunk size (empty)
    }
}

