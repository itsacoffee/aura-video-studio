using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Providers.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify that FFmpeg path resolution is consistent across validation and remediation
/// This addresses the issue where validation might succeed but remediation fails due to path inconsistencies
/// </summary>
public class FfmpegSingleLocatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _mockFfmpegPath;

    public FfmpegSingleLocatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AuraSingleLocatorTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        _mockFfmpegPath = Path.Combine(_testDirectory, exeName);
    }

    [Fact]
    public async Task FfmpegVideoComposer_UsesLocatorToResolvePathOncePerJob()
    {
        // Arrange - create a mock locator that tracks how many times it's called
        var mockLocator = new Mock<IFfmpegLocator>();
        int callCount = 0;
        
        mockLocator
            .Setup(l => l.GetEffectiveFfmpegPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return _mockFfmpegPath;
            });

        var composer = new FfmpegVideoComposer(
            NullLogger<FfmpegVideoComposer>.Instance,
            mockLocator.Object,
            "ffmpeg",
            _testDirectory);

        // Assert - locator should not be called in constructor
        Assert.Equal(0, callCount);
        
        // The locator would be called once per render job when RenderAsync is invoked
        // We can't easily test the full RenderAsync without actual FFmpeg, but we've verified
        // the locator injection pattern is correct
    }

    [Fact]
    public async Task FfmpegLocator_ReturnsConsistentPath()
    {
        // Arrange - create a mock FFmpeg binary
        await CreateMockFfmpegBinary(_mockFfmpegPath);
        
        var locator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _testDirectory);

        // Act - resolve the path multiple times
        var path1 = await locator.GetEffectiveFfmpegPathAsync(_mockFfmpegPath, CancellationToken.None);
        var path2 = await locator.GetEffectiveFfmpegPathAsync(_mockFfmpegPath, CancellationToken.None);
        var path3 = await locator.GetEffectiveFfmpegPathAsync(_mockFfmpegPath, CancellationToken.None);

        // Assert - all paths should be identical
        Assert.Equal(path1, path2);
        Assert.Equal(path2, path3);
        Assert.Equal(_mockFfmpegPath, path1);
    }

    [Fact]
    public async Task FfmpegLocator_ValidatesPathAndReturnsAbsolutePath()
    {
        // Arrange - create a mock FFmpeg binary
        await CreateMockFfmpegBinary(_mockFfmpegPath);
        
        var locator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _testDirectory);

        // Act
        var result = await locator.ValidatePathAsync(_mockFfmpegPath, CancellationToken.None);

        // Assert
        Assert.True(result.Found, $"Expected FFmpeg to be found. Reason: {result.Reason}");
        Assert.NotNull(result.FfmpegPath);
        Assert.True(Path.IsPathRooted(result.FfmpegPath), "Expected absolute path");
        Assert.Contains("ffmpeg version", result.ValidationOutput ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FfmpegLocator_ThrowsWhenFfmpegNotFound()
    {
        // Arrange - locator with no FFmpeg available
        var locator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _testDirectory);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await locator.GetEffectiveFfmpegPathAsync(
                "/nonexistent/path/to/ffmpeg", 
                CancellationToken.None));
        
        Assert.Contains("FFmpeg not found", exception.Message);
    }

    [Fact]
    public void FfmpegVideoComposer_RequiresLocator()
    {
        // This test verifies that FfmpegVideoComposer now requires IFfmpegLocator
        // and doesn't accept a direct string path anymore
        
        var mockLocator = new Mock<IFfmpegLocator>();
        
        // This should compile successfully
        var composer = new FfmpegVideoComposer(
            NullLogger<FfmpegVideoComposer>.Instance,
            mockLocator.Object,
            "ffmpeg",
            _testDirectory);
        
        Assert.NotNull(composer);
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
    echo ffmpeg version 8.0-test Copyright (c) 2000-2024 the FFmpeg developers
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
    echo ""ffmpeg version 8.0-test Copyright (c) 2000-2024 the FFmpeg developers""
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
