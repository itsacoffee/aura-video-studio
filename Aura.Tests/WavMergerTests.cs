using System;
using System.Collections.Generic;
using System.IO;
using Aura.Providers.Audio;
using Xunit;

namespace Aura.Tests;

public class WavMergerTests
{
    [Fact]
    public void MergeWavFiles_Should_CreateValidOutput()
    {
        // Arrange - Create test WAV files
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "test1.wav");
            var file2 = Path.Combine(tempDir, "test2.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            CreateTestWav(file1, TimeSpan.FromSeconds(1));
            CreateTestWav(file2, TimeSpan.FromSeconds(1));

            var segments = new List<WavSegment>
            {
                new WavSegment(file1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1)),
                new WavSegment(file2, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1))
            };

            // Act
            WavMerger.MergeWavFiles(segments, outputFile);

            // Assert
            Assert.True(File.Exists(outputFile));
            
            // Verify it's a valid WAV file
            using var stream = File.OpenRead(outputFile);
            using var reader = new BinaryReader(stream);
            
            var riff = new string(reader.ReadChars(4));
            Assert.Equal("RIFF", riff);
            
            reader.ReadInt32(); // File size
            
            var wave = new string(reader.ReadChars(4));
            Assert.Equal("WAVE", wave);
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
    public void MergeWavFiles_Should_HandleTimingGaps()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "test1.wav");
            var file2 = Path.Combine(tempDir, "test2.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            CreateTestWav(file1, TimeSpan.FromSeconds(0.5));
            CreateTestWav(file2, TimeSpan.FromSeconds(0.5));

            var segments = new List<WavSegment>
            {
                new WavSegment(file1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0.5)),
                new WavSegment(file2, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(0.5))
            };

            // Act
            WavMerger.MergeWavFiles(segments, outputFile);

            // Assert
            Assert.True(File.Exists(outputFile));
            
            // Read and verify duration includes gaps
            using var stream = File.OpenRead(outputFile);
            using var reader = new BinaryReader(stream);
            
            // Skip to sample rate in fmt chunk
            reader.BaseStream.Seek(24, SeekOrigin.Begin);
            int sampleRate = reader.ReadInt32();
            
            // Calculate duration from file size
            int dataSize = (int)(stream.Length - 44);
            double durationSeconds = (double)dataSize / (sampleRate * 2);
            
            // Should be at least 2 seconds (includes gap)
            Assert.InRange(durationSeconds, 1.8, 2.2);
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
    public void MergeWavFiles_Should_HandleWavWithExtraChunks()
    {
        // Arrange - Test WAV files with LIST and other metadata chunks (like Windows TTS generates)
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "test1_metadata.wav");
            var file2 = Path.Combine(tempDir, "test2_metadata.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            // Create WAV files with LIST chunk before data chunk
            CreateTestWavWithMetadata(file1, TimeSpan.FromSeconds(0.5));
            CreateTestWavWithMetadata(file2, TimeSpan.FromSeconds(0.5));

            var segments = new List<WavSegment>
            {
                new WavSegment(file1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0.5)),
                new WavSegment(file2, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.5))
            };

            // Act
            WavMerger.MergeWavFiles(segments, outputFile);

            // Assert
            Assert.True(File.Exists(outputFile));
            
            // Verify it's a valid WAV file
            using var stream = File.OpenRead(outputFile);
            using var reader = new BinaryReader(stream);
            
            var riff = new string(reader.ReadChars(4));
            Assert.Equal("RIFF", riff);
            
            reader.ReadInt32(); // File size
            
            var wave = new string(reader.ReadChars(4));
            Assert.Equal("WAVE", wave);
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
    public void MergeWavFiles_Should_ThrowOnEmptyFile()
    {
        // Arrange - Create an empty file (0 bytes)
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var emptyFile = Path.Combine(tempDir, "empty.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            // Create a 0-byte file
            File.WriteAllBytes(emptyFile, Array.Empty<byte>());

            var segments = new List<WavSegment>
            {
                new WavSegment(emptyFile, TimeSpan.Zero, TimeSpan.FromSeconds(1))
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidDataException>(() =>
                WavMerger.MergeWavFiles(segments, outputFile));
            
            Assert.Contains("too small", exception.Message);
            Assert.Contains("0 bytes", exception.Message);
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
    public void MergeWavFiles_Should_ThrowOnTooSmallFile()
    {
        // Arrange - Create a file smaller than WAV header (< 44 bytes)
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var tooSmallFile = Path.Combine(tempDir, "small.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            // Create a 20-byte file (less than 44-byte WAV header)
            File.WriteAllBytes(tooSmallFile, new byte[20]);

            var segments = new List<WavSegment>
            {
                new WavSegment(tooSmallFile, TimeSpan.Zero, TimeSpan.FromSeconds(1))
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidDataException>(() =>
                WavMerger.MergeWavFiles(segments, outputFile));
            
            Assert.Contains("too small", exception.Message);
            Assert.Contains("20 bytes", exception.Message);
            Assert.Contains("minimum 44 bytes", exception.Message);
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
    public void MergeWavFiles_Should_ThrowOnMissingFile()
    {
        // Arrange - Reference a file that doesn't exist
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var missingFile = Path.Combine(tempDir, "does_not_exist.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            var segments = new List<WavSegment>
            {
                new WavSegment(missingFile, TimeSpan.Zero, TimeSpan.FromSeconds(1))
            };

            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() =>
                WavMerger.MergeWavFiles(segments, outputFile));
            
            Assert.Contains("not found", exception.Message);
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
    public void MergeWavFiles_Should_ValidateOutputFileCreated()
    {
        // Arrange - Create valid input files
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "test1.wav");
            var outputFile = Path.Combine(tempDir, "merged.wav");

            CreateTestWav(file1, TimeSpan.FromSeconds(0.5));

            var segments = new List<WavSegment>
            {
                new WavSegment(file1, TimeSpan.Zero, TimeSpan.FromSeconds(0.5))
            };

            // Act
            WavMerger.MergeWavFiles(segments, outputFile);

            // Assert - Output file exists and has valid size
            Assert.True(File.Exists(outputFile));
            var outputInfo = new FileInfo(outputFile);
            Assert.True(outputInfo.Length >= 44, "Output file should be at least 44 bytes (WAV header)");
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

    private void CreateTestWav(string path, TimeSpan duration)
    {
        const int sampleRate = 44100;
        const short bitsPerSample = 16;
        const short numChannels = 1;

        int numSamples = (int)(duration.TotalSeconds * sampleRate);
        int dataSize = numSamples * numChannels * (bitsPerSample / 8);

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fileStream);

        // Write WAV header
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);

        // Write simple test data (silence with a tone)
        for (int i = 0; i < numSamples; i++)
        {
            double time = (double)i / sampleRate;
            short sample = (short)(0.3 * short.MaxValue * Math.Sin(2.0 * Math.PI * 440.0 * time));
            writer.Write(sample);
        }
    }

    private void CreateTestWavWithMetadata(string path, TimeSpan duration)
    {
        const int sampleRate = 44100;
        const short bitsPerSample = 16;
        const short numChannels = 1;

        int numSamples = (int)(duration.TotalSeconds * sampleRate);
        int dataSize = numSamples * numChannels * (bitsPerSample / 8);

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fileStream);

        // Write WAV header
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));

        // LIST chunk (metadata) - 20 bytes
        string listChunkData = "INFO";
        int listChunkSize = 4; // Just the INFO identifier for simplicity
        
        // Calculate total file size (RIFF header includes everything except first 8 bytes)
        int totalSize = 4 + 8 + 16 + 8 + listChunkSize + 8 + dataSize; // WAVE + fmt chunk + LIST chunk + data chunk
        
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(totalSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        
        // Write fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        
        // Write LIST chunk (metadata)
        writer.Write(new[] { 'L', 'I', 'S', 'T' });
        writer.Write(listChunkSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes(listChunkData));
        
        // Write data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);

        // Write simple test data (silence with a tone)
        for (int i = 0; i < numSamples; i++)
        {
            double time = (double)i / sampleRate;
            short sample = (short)(0.3 * short.MaxValue * Math.Sin(2.0 * Math.PI * 440.0 * time));
            writer.Write(sample);
        }
    }
}
