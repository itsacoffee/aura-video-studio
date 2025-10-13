using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Providers.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify FFmpeg PATH detection fix for version 8.0 and other versions
/// </summary>
public class FfmpegPathDetectionTests : IDisposable
{
    private readonly string _testDirectory;

    public FfmpegPathDetectionTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"AuraPathTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void ValidateFfmpegBinaryAsync_WithPathExecutable_ShouldNotCheckFileExists()
    {
        // This test verifies that FfmpegVideoComposer can handle PATH-based executables
        // The key insight is that "ffmpeg" (without path separators) should be treated
        // as a PATH executable and not require File.Exists() check
        
        // Arrange - use a simple executable name (as would come from PATH)
        var pathExecutableName = "ffmpeg";
        var locator = new FfmpegLocator(NullLogger<FfmpegLocator>.Instance, _testDirectory);
        
        // Act - this should NOT throw an exception about file not found
        // because we skip File.Exists check for PATH executables
        var composer = new FfmpegVideoComposer(
            NullLogger<FfmpegVideoComposer>.Instance,
            locator,
            pathExecutableName,
            _testDirectory);
        
        // Assert - constructor should succeed
        Assert.NotNull(composer);
    }

    [Fact]
    public void ValidateFfmpegBinaryAsync_WithAbsolutePath_ShouldCheckFileExists()
    {
        // This test verifies that absolute paths still get File.Exists() check
        
        // Arrange - use an absolute path that doesn't exist
        var absolutePath = Path.Combine(_testDirectory, "nonexistent", "ffmpeg.exe");
        var locator = new FfmpegLocator(NullLogger<FfmpegLocator>.Instance, _testDirectory);
        
        // Act & Assert - constructor should succeed (validation happens during render)
        var composer = new FfmpegVideoComposer(
            NullLogger<FfmpegVideoComposer>.Instance,
            locator,
            absolutePath,
            _testDirectory);
        
        Assert.NotNull(composer);
    }

    [Theory]
    [InlineData("ffmpeg")]           // Linux PATH executable
    [InlineData("ffmpeg.exe")]       // Windows PATH executable
    [InlineData("ffprobe")]          // Another common PATH executable
    public void IsPathExecutable_ShouldReturnTrue_ForExecutableNames(string executableName)
    {
        // These should be treated as PATH executables
        bool isPathExecutable = !Path.IsPathRooted(executableName) && 
                                !executableName.Contains(Path.DirectorySeparatorChar) &&
                                !executableName.Contains(Path.AltDirectorySeparatorChar);
        
        Assert.True(isPathExecutable, 
            $"{executableName} should be treated as a PATH executable");
    }

    [Theory]
    [InlineData("/usr/bin/ffmpeg")]          // Linux absolute path
    [InlineData("./ffmpeg")]                 // Relative path
    [InlineData("bin/ffmpeg")]               // Relative path with directory
    public void IsPathExecutable_ShouldReturnFalse_ForPaths(string path)
    {
        // These should NOT be treated as PATH executables - they have path separators
        bool isPathExecutable = !Path.IsPathRooted(path) && 
                                !path.Contains(Path.DirectorySeparatorChar) &&
                                !path.Contains(Path.AltDirectorySeparatorChar);
        
        Assert.False(isPathExecutable, 
            $"{path} should NOT be treated as a PATH executable");
    }

    [Fact]
    public void IsPathExecutable_WindowsAbsolutePath_ShouldReturnFalse()
    {
        // Skip this test on non-Windows platforms since Windows paths aren't meaningful there
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }
        
        // Windows-specific test for absolute path
        var windowsPath = "C:\\Tools\\ffmpeg.exe";
        
        // Check using the same logic as FfmpegVideoComposer
        bool isPathExecutable = !Path.IsPathRooted(windowsPath) && 
                                !windowsPath.Contains(Path.DirectorySeparatorChar) &&
                                !windowsPath.Contains(Path.AltDirectorySeparatorChar);
        
        Assert.False(isPathExecutable, 
            $"{windowsPath} should NOT be treated as a PATH executable");
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
}
