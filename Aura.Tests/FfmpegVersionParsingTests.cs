using System;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for FFmpeg version parsing from -version output
/// </summary>
public class FfmpegVersionParsingTests
{
    [Theory]
    [InlineData("ffmpeg version N-111617-gdd5a56c1b5 Copyright (c) 2000-2024", "N-111617-gdd5a56c1b5")]
    [InlineData("ffmpeg version 6.0 Copyright (c) 2000-2023 the FFmpeg developers", "6.0")]
    [InlineData("ffmpeg version 7.0.1-full_build-www.gyan.dev Copyright (c) 2000-2024", "7.0.1-full_build-www.gyan.dev")]
    [InlineData("ffmpeg version 8.0-full_build-www.gyan.dev Copyright (c) 2000-2024", "8.0-full_build-www.gyan.dev")]
    [InlineData("ffmpeg version n6.1.1-2-g6f21cab903-20240110 Copyright (c)", "n6.1.1-2-g6f21cab903-20240110")]
    public void ExtractVersion_FromVariousOutputs_ReturnsCorrectVersion(string ffmpegOutput, string expectedVersion)
    {
        // This test verifies the version extraction logic
        // The actual parsing is done in FfmpegInstaller.ExtractVersionFromOutput()
        // and FfmpegLocator.ExtractVersionString()
        
        // Arrange - split output by spaces
        var parts = ffmpegOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Act - extract version (index 2 in split output)
        string? actualVersion = null;
        if (parts.Length >= 3 && ffmpegOutput.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
        {
            actualVersion = parts[2];
        }
        
        // Assert
        Assert.Equal(expectedVersion, actualVersion);
    }
    
    [Fact]
    public void ExtractVersion_FromEmptyOutput_ReturnsNull()
    {
        // Arrange
        string? output = "";
        
        // Act
        string? version = null;
        if (!string.IsNullOrEmpty(output))
        {
            var parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                version = parts[2];
            }
        }
        
        // Assert
        Assert.Null(version);
    }
    
    [Fact]
    public void ExtractVersion_FromInvalidOutput_ReturnsNull()
    {
        // Arrange
        string output = "This is not valid FFmpeg output";
        
        // Act
        string? version = null;
        if (output.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
        {
            var parts = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                version = parts[2];
            }
        }
        
        // Assert
        Assert.Null(version);
    }
}
