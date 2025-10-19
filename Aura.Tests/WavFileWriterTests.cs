using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class WavFileWriterTests
{
    private readonly ILogger<WavFileWriter> _logger;
    private readonly WavFileWriter _writer;

    public WavFileWriterTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _logger = loggerFactory.CreateLogger<WavFileWriter>();
        _writer = new WavFileWriter(_logger);
    }

    [Fact]
    public async Task WriteAsync_Should_CreateValidWavFile()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wav");
        var audioData = new short[1000]; // 1000 samples of silence
        
        try
        {
            // Act
            await _writer.WriteAsync(outputPath, audioData);

            // Assert
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 128, "File should be larger than 128 bytes");
            Assert.True(_writer.ValidateWavFile(outputPath), "File should have valid WAV structure");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_Should_UseAtomicWrites()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wav");
        var partPath = outputPath + ".part";
        var audioData = new short[1000];

        try
        {
            // Act
            await _writer.WriteAsync(outputPath, audioData);

            // Assert
            Assert.True(File.Exists(outputPath), "Output file should exist");
            Assert.False(File.Exists(partPath), "Partial file should be cleaned up");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            if (File.Exists(partPath))
            {
                File.Delete(partPath);
            }
        }
    }

    [Fact]
    public async Task GenerateSilenceAsync_Should_CreateValidSilentWav()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"silence_{Guid.NewGuid()}.wav");
        const double duration = 1.0; // 1 second

        try
        {
            // Act
            await _writer.GenerateSilenceAsync(outputPath, duration);

            // Assert
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 128, "Silent file should be larger than 128 bytes");
            Assert.True(_writer.ValidateWavFile(outputPath), "Silent file should have valid WAV structure");

            // Verify RIFF header
            using var stream = File.OpenRead(outputPath);
            using var reader = new BinaryReader(stream);
            var riff = new string(reader.ReadChars(4));
            Assert.Equal("RIFF", riff);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task GenerateMinimalSilenceAsync_Should_CreateValidMinimalWav()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"minimal_silence_{Guid.NewGuid()}.wav");

        try
        {
            // Act
            await _writer.GenerateMinimalSilenceAsync(outputPath);

            // Assert
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 128, "Minimal file should be larger than 128 bytes");
            Assert.True(_writer.ValidateWavFile(outputPath), "Minimal file should have valid WAV structure");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_Should_OverwriteExistingFile()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"overwrite_{Guid.NewGuid()}.wav");
        var audioData1 = new short[500];
        var audioData2 = new short[1000];

        try
        {
            // Act
            await _writer.WriteAsync(outputPath, audioData1);
            var size1 = new FileInfo(outputPath).Length;

            await _writer.WriteAsync(outputPath, audioData2);
            var size2 = new FileInfo(outputPath).Length;

            // Assert
            Assert.True(size2 > size1, "Second write should create larger file");
            Assert.True(_writer.ValidateWavFile(outputPath), "Overwritten file should be valid");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ValidateWavFile_Should_RejectInvalidFiles()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}.wav");

        try
        {
            // Create invalid file (too small)
            File.WriteAllText(tempPath, "invalid");

            // Act
            var isValid = _writer.ValidateWavFile(tempPath);

            // Assert
            Assert.False(isValid, "Invalid file should be rejected");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public void ValidateWavFile_Should_RejectNonExistentFiles()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.wav");

        // Act
        var isValid = _writer.ValidateWavFile(nonExistentPath);

        // Assert
        Assert.False(isValid, "Non-existent file should be rejected");
    }

    [Fact]
    public async Task WriteAsync_Should_ThrowOnEmptyData()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid()}.wav");
        var emptyData = Array.Empty<short>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _writer.WriteAsync(outputPath, emptyData);
        });
    }

    [Fact]
    public async Task GenerateSilenceAsync_Should_ThrowOnInvalidDuration()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"invalid_duration_{Guid.NewGuid()}.wav");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _writer.GenerateSilenceAsync(outputPath, -1.0);
        });
    }

    [Fact]
    public async Task WriteAsync_Should_CreateDirectoryIfNotExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"wavtest_{Guid.NewGuid()}");
        var outputPath = Path.Combine(tempDir, "test.wav");
        var audioData = new short[1000];

        try
        {
            // Act
            await _writer.WriteAsync(outputPath, audioData);

            // Assert
            Assert.True(Directory.Exists(tempDir), "Directory should be created");
            Assert.True(File.Exists(outputPath), "File should be created in new directory");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_Should_SupportCancellation()
    {
        // Arrange
        var outputPath = Path.Combine(Path.GetTempPath(), $"cancel_{Guid.NewGuid()}.wav");
        var audioData = new short[10_000_000]; // Large data
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _writer.WriteAsync(outputPath, audioData, ct: cts.Token);
            });

            // Partial file should not exist
            Assert.False(File.Exists(outputPath), "Output file should not exist after cancellation");
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            var partPath = outputPath + ".part";
            if (File.Exists(partPath))
            {
                File.Delete(partPath);
            }
        }
    }
}
