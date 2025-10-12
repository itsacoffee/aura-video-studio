using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class FfmpegLocatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FfmpegLocator _locator;
    
    public FfmpegLocatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "AuraFfmpegLocatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        
        _locator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _testDirectory);
    }
    
    [Fact]
    public async Task ValidatePathAsync_WithValidFfmpegExecutable_Succeeds()
    {
        // Arrange
        var mockFfmpegPath = Path.Combine(_testDirectory, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _locator.ValidatePathAsync(mockFfmpegPath, CancellationToken.None);
        
        // Assert
        Assert.True(result.Found, $"Expected FFmpeg to be found. Reason: {result.Reason}");
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
        Assert.NotNull(result.ValidationOutput);
        Assert.Contains("ffmpeg version", result.ValidationOutput, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.VersionString);
    }
    
    [Fact]
    public async Task ValidatePathAsync_WithDirectory_FindsFfmpegInDirectory()
    {
        // Arrange
        var mockDir = Path.Combine(_testDirectory, "ffmpeg-dir");
        Directory.CreateDirectory(mockDir);
        
        var mockFfmpegPath = Path.Combine(mockDir, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _locator.ValidatePathAsync(mockDir, CancellationToken.None);
        
        // Assert
        Assert.True(result.Found);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
    }
    
    [Fact]
    public async Task ValidatePathAsync_WithBinSubdirectory_FindsFfmpeg()
    {
        // Arrange
        var mockDir = Path.Combine(_testDirectory, "ffmpeg-with-bin");
        var binDir = Path.Combine(mockDir, "bin");
        Directory.CreateDirectory(binDir);
        
        var mockFfmpegPath = Path.Combine(binDir, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _locator.ValidatePathAsync(mockDir, CancellationToken.None);
        
        // Assert
        Assert.True(result.Found);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
    }
    
    [Fact]
    public async Task ValidatePathAsync_WithNonExistentPath_ReturnsFalse()
    {
        // Act
        var result = await _locator.ValidatePathAsync("/nonexistent/path/ffmpeg", CancellationToken.None);
        
        // Assert
        Assert.False(result.Found);
        Assert.NotNull(result.Reason);
        Assert.Contains("not found", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task ValidatePathAsync_WithInvalidBinary_ReturnsFalse()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDirectory, "invalid-ffmpeg.exe");
        await File.WriteAllTextAsync(invalidPath, "This is not a valid executable");
        
        // Act
        var result = await _locator.ValidatePathAsync(invalidPath, CancellationToken.None);
        
        // Assert
        Assert.False(result.Found);
        Assert.NotNull(result.Reason);
    }
    
    [Fact]
    public async Task CheckAllCandidatesAsync_FindsFfmpegInConfiguredPath()
    {
        // Arrange
        var mockFfmpegPath = Path.Combine(_testDirectory, "configured", "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        Directory.CreateDirectory(Path.GetDirectoryName(mockFfmpegPath)!);
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _locator.CheckAllCandidatesAsync(mockFfmpegPath, CancellationToken.None);
        
        // Assert
        Assert.True(result.Found);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
        Assert.True(result.AttemptedPaths.Count > 0);
        Assert.Contains(mockFfmpegPath, result.AttemptedPaths);
    }
    
    [Fact]
    public async Task CheckAllCandidatesAsync_WithNoFFmpeg_ReturnsNotFound()
    {
        // Act
        var result = await _locator.CheckAllCandidatesAsync(null, CancellationToken.None);
        
        // Assert
        Assert.False(result.Found);
        Assert.NotNull(result.Reason);
        Assert.True(result.AttemptedPaths.Count > 0, "Should have attempted multiple paths");
    }
    
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
    
    /// <summary>
    /// Create a mock ffmpeg binary that responds to -version
    /// </summary>
    private async Task CreateMockFfmpegBinary(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Create a simple batch script for Windows
            var batchContent = @"@echo off
if ""%1""==""-version"" (
    echo ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers
    echo built with gcc 12.2.0
    exit /b 0
)
exit /b 1";
            // Change extension to .bat for testing (since we can't create real .exe)
            var batPath = Path.ChangeExtension(path, ".bat");
            await File.WriteAllTextAsync(batPath, batchContent);
            
            // Also write a .exe that's actually a batch redirect
            // For test purposes, just copy the same content
            await File.WriteAllTextAsync(path, batchContent);
        }
        else
        {
            // Create a shell script for Unix
            var shellContent = @"#!/bin/bash
if [ ""$1"" = ""-version"" ]; then
    echo ""ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers""
    echo ""built with gcc 12.2.0""
    exit 0
fi
exit 1";
            await File.WriteAllTextAsync(path, shellContent);
            
            // Make executable on Unix
            try
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x {path}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch
            {
                // Ignore if chmod fails
            }
        }
    }
}
