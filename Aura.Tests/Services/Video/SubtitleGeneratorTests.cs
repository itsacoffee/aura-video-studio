using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Services.Video;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.Video;

public class SubtitleGeneratorTests : IDisposable
{
    private readonly Mock<ILogger<SubtitleGenerator>> _mockLogger;
    private readonly SubtitleGenerator _generator;
    private readonly string _tempDirectory;

    public SubtitleGeneratorTests()
    {
        _mockLogger = new Mock<ILogger<SubtitleGenerator>>();
        _generator = new SubtitleGenerator(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SubtitleTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task GenerateAsync_SRT_ShouldCreateValidSubtitleFile()
    {
        // Arrange
        var entries = new List<SubtitleEntry>
        {
            new SubtitleEntry
            {
                Index = 1,
                StartTime = TimeSpan.FromSeconds(0),
                EndTime = TimeSpan.FromSeconds(3),
                Text = "Hello, world!"
            },
            new SubtitleEntry
            {
                Index = 2,
                StartTime = TimeSpan.FromSeconds(3),
                EndTime = TimeSpan.FromSeconds(6),
                Text = "This is a test subtitle."
            }
        };

        var outputPath = Path.Combine(_tempDirectory, "test.srt");

        // Act
        var result = await _generator.GenerateAsync(entries, outputPath, SubtitleFormat.SRT);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("00:00:00,000 --> 00:00:03,000", content);
        Assert.Contains("Hello, world!", content);
        Assert.Contains("00:00:03,000 --> 00:00:06,000", content);
        Assert.Contains("This is a test subtitle.", content);
    }

    [Fact]
    public async Task GenerateAsync_VTT_ShouldCreateValidSubtitleFile()
    {
        // Arrange
        var entries = new List<SubtitleEntry>
        {
            new SubtitleEntry
            {
                Index = 1,
                StartTime = TimeSpan.FromSeconds(0),
                EndTime = TimeSpan.FromSeconds(3),
                Text = "Hello, world!"
            }
        };

        var outputPath = Path.Combine(_tempDirectory, "test.vtt");

        // Act
        var result = await _generator.GenerateAsync(entries, outputPath, SubtitleFormat.VTT);

        // Assert
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("WEBVTT", content);
        Assert.Contains("00:00:00.000 --> 00:00:03.000", content);
        Assert.Contains("Hello, world!", content);
    }

    [Fact]
    public async Task GenerateAsync_ASS_ShouldCreateValidSubtitleFile()
    {
        // Arrange
        var entries = new List<SubtitleEntry>
        {
            new SubtitleEntry
            {
                Index = 1,
                StartTime = TimeSpan.FromSeconds(0),
                EndTime = TimeSpan.FromSeconds(3),
                Text = "Hello, world!",
                SpeakerName = "Speaker1"
            }
        };

        var outputPath = Path.Combine(_tempDirectory, "test.ass");

        // Act
        var result = await _generator.GenerateAsync(entries, outputPath, SubtitleFormat.ASS);

        // Assert
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("[Script Info]", content);
        Assert.Contains("[V4+ Styles]", content);
        Assert.Contains("[Events]", content);
        Assert.Contains("Speaker1", content);
        Assert.Contains("Hello, world!", content);
    }

    [Fact]
    public async Task GenerateEntriesAsync_ShouldSplitLongTextIntoLines()
    {
        // Arrange
        var scriptLines = new List<(string text, TimeSpan startTime, TimeSpan duration)>
        {
            ("This is a very long subtitle text that should be split into multiple lines for better readability on screen", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        // Act
        var entries = await _generator.GenerateEntriesAsync(scriptLines);

        // Assert
        Assert.Single(entries);
        var entry = entries[0];
        Assert.Contains("\n", entry.Text); // Should have line breaks
    }

    [Fact]
    public async Task GenerateEntriesAsync_ShouldSkipEmptyText()
    {
        // Arrange
        var scriptLines = new List<(string text, TimeSpan startTime, TimeSpan duration)>
        {
            ("Valid text", TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            ("", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)),
            ("Another valid text", TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2))
        };

        // Act
        var entries = await _generator.GenerateEntriesAsync(scriptLines);

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("Valid text", entries[0].Text);
        Assert.Equal("Another valid text", entries[1].Text);
    }

    [Fact]
    public async Task ParseAsync_SRT_ShouldParseValidSubtitleFile()
    {
        // Arrange
        var srtContent = @"1
00:00:00,000 --> 00:00:03,000
Hello, world!

2
00:00:03,000 --> 00:00:06,000
This is a test subtitle.

";
        var inputPath = Path.Combine(_tempDirectory, "parse_test.srt");
        await File.WriteAllTextAsync(inputPath, srtContent);

        // Act
        var entries = await _generator.ParseAsync(inputPath, SubtitleFormat.SRT);

        // Assert
        Assert.Equal(2, entries.Count);
        
        Assert.Equal(1, entries[0].Index);
        Assert.Equal(TimeSpan.FromSeconds(0), entries[0].StartTime);
        Assert.Equal(TimeSpan.FromSeconds(3), entries[0].EndTime);
        Assert.Equal("Hello, world!", entries[0].Text);

        Assert.Equal(2, entries[1].Index);
        Assert.Equal(TimeSpan.FromSeconds(3), entries[1].StartTime);
        Assert.Equal(TimeSpan.FromSeconds(6), entries[1].EndTime);
        Assert.Equal("This is a test subtitle.", entries[1].Text);
    }

    [Fact]
    public async Task ConvertAsync_FromSRTToVTT_ShouldConvertSuccessfully()
    {
        // Arrange
        var srtContent = @"1
00:00:00,000 --> 00:00:03,000
Hello, world!

";
        var inputPath = Path.Combine(_tempDirectory, "convert_input.srt");
        var outputPath = Path.Combine(_tempDirectory, "convert_output.vtt");
        await File.WriteAllTextAsync(inputPath, srtContent);

        // Act
        var result = await _generator.ConvertAsync(
            inputPath,
            outputPath,
            SubtitleFormat.SRT,
            SubtitleFormat.VTT
        );

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("WEBVTT", content);
        Assert.Contains("00:00:00.000 --> 00:00:03.000", content);
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyEntries_ShouldThrowArgumentException()
    {
        // Arrange
        var entries = new List<SubtitleEntry>();
        var outputPath = Path.Combine(_tempDirectory, "empty.srt");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _generator.GenerateAsync(entries, outputPath)
        );
    }

    [Fact]
    public async Task ParseAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.srt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _generator.ParseAsync(nonExistentPath)
        );
    }

    [Fact]
    public async Task GenerateAsync_WithMultilineText_ShouldPreserveLineBreaks()
    {
        // Arrange
        var entries = new List<SubtitleEntry>
        {
            new SubtitleEntry
            {
                Index = 1,
                StartTime = TimeSpan.FromSeconds(0),
                EndTime = TimeSpan.FromSeconds(3),
                Text = "First line\nSecond line"
            }
        };

        var outputPath = Path.Combine(_tempDirectory, "multiline.srt");

        // Act
        await _generator.GenerateAsync(entries, outputPath, SubtitleFormat.SRT);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("First line\nSecond line", content);
    }
}
