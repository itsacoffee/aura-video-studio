using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Tests.TestUtilities;
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
        await FfmpegTestHelper.CreateMockFfmpegBinary(mockFfmpegPath);
        
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
        await FfmpegTestHelper.CreateMockFfmpegBinary(mockFfmpegPath);
        
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
        await FfmpegTestHelper.CreateMockFfmpegBinary(mockFfmpegPath);
        
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
        await FfmpegTestHelper.CreateMockFfmpegBinary(mockFfmpegPath);
        
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

    [Fact]
    public async Task CheckAllCandidatesAsync_FindsBundledResourcePath()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
        var rid = FfmpegTestHelper.GetRuntimeRidSegment();

        // Simulate packaged backend base directory
        var backendBase = Path.Combine(_testDirectory, "resources", "backend", rid);
        Directory.CreateDirectory(backendBase);

        // Place ffmpeg in resources/ffmpeg/<rid>/bin
        var bundledPath = Path.Combine(_testDirectory, "resources", "ffmpeg", rid, "bin", exeName);
        Directory.CreateDirectory(Path.GetDirectoryName(bundledPath)!);
        await FfmpegTestHelper.CreateMockFfmpegBinary(bundledPath);

        var locator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _testDirectory,
            null,
            backendBase);

        var result = await locator.CheckAllCandidatesAsync(null, CancellationToken.None);

        Assert.True(result.Found);
        Assert.Equal(bundledPath, result.FfmpegPath);
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
