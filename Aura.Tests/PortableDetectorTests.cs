using System;
using System.IO;
using Aura.Core.Services.Setup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for the PortableDetector service
/// </summary>
public class PortableDetectorTests : IDisposable
{
    private readonly ILogger<PortableDetector> _logger;
    private readonly string _testDirectory;
    private readonly string? _originalEnvPortableMode;
    private readonly string? _originalEnvPortableRoot;

    public PortableDetectorTests()
    {
        _logger = NullLogger<PortableDetector>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-portable-detector-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Save original environment variables
        _originalEnvPortableMode = Environment.GetEnvironmentVariable("AURA_PORTABLE_MODE");
        _originalEnvPortableRoot = Environment.GetEnvironmentVariable("AURA_PORTABLE_ROOT");

        // Clear environment variables
        Environment.SetEnvironmentVariable("AURA_PORTABLE_MODE", null);
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", null);
    }

    public void Dispose()
    {
        // Restore environment variables
        Environment.SetEnvironmentVariable("AURA_PORTABLE_MODE", _originalEnvPortableMode);
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _originalEnvPortableRoot);

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void GetPortableStatus_ReturnsCorrectPaths()
    {
        // Arrange
        var detector = new PortableDetector(_logger);

        // Act
        var status = detector.GetPortableStatus();

        // Assert
        Assert.NotNull(status);
        Assert.NotNull(status.PortableRoot);
        Assert.NotNull(status.ToolsDirectory);
        Assert.NotNull(status.DataDirectory);
        Assert.NotNull(status.CacheDirectory);
        Assert.NotNull(status.LogsDirectory);

        // Verify the directory structure
        Assert.Contains("Tools", status.ToolsDirectory);
        Assert.Contains("Data", status.DataDirectory);
        Assert.Contains("cache", status.CacheDirectory);
        Assert.Contains("logs", status.LogsDirectory);
    }

    [Fact]
    public void GetDependencySummary_ReturnsStatus()
    {
        // Arrange
        var detector = new PortableDetector(_logger);

        // Act
        var summary = detector.GetDependencySummary();

        // Assert
        Assert.NotNull(summary);
        // The values depend on system state, so we just verify the object is populated
    }

    [Fact]
    public void EnsureDirectoriesExist_CreatesDirectories()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        Environment.SetEnvironmentVariable("AURA_PORTABLE_MODE", "true");
        var detector = new PortableDetector(_logger);

        // Act
        detector.EnsureDirectoriesExist();

        // Assert
        Assert.True(Directory.Exists(detector.ToolsDirectory), "Tools directory should exist");
        Assert.True(Directory.Exists(detector.DataDirectory), "Data directory should exist");
        Assert.True(Directory.Exists(detector.CacheDirectory), "Cache directory should exist");
        Assert.True(Directory.Exists(detector.LogsDirectory), "Logs directory should exist");
    }

    [Fact]
    public void CreatePortableMarker_CreatesMarkerFile()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);
        var expectedMarkerPath = Path.Combine(_testDirectory, ".portable");

        // Act
        detector.CreatePortableMarker();

        // Assert
        Assert.True(File.Exists(expectedMarkerPath), ".portable marker file should be created");
    }

    [Fact]
    public void ToRelativePath_ConvertsPortablePaths()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        Environment.SetEnvironmentVariable("AURA_PORTABLE_MODE", "true");
        var detector = new PortableDetector(_logger);
        var absolutePath = Path.Combine(_testDirectory, "Tools", "ffmpeg", "bin", "ffmpeg.exe");

        // Act
        var relativePath = detector.ToRelativePath(absolutePath);

        // Assert
        Assert.NotNull(relativePath);
        Assert.StartsWith(".", relativePath);
        Assert.Contains("Tools", relativePath);
    }

    [Fact]
    public void ToAbsolutePath_ConvertsRelativePaths()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);
        var relativePath = "./Tools/ffmpeg/bin/ffmpeg.exe";

        // Act
        var absolutePath = detector.ToAbsolutePath(relativePath);

        // Assert
        Assert.NotNull(absolutePath);
        Assert.True(Path.IsPathRooted(absolutePath));
        Assert.Contains("Tools", absolutePath);
    }

    [Fact]
    public void ValidateAndRepairPath_ValidatesExistingPaths()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);
        
        // Create a test file
        var toolsDir = Path.Combine(_testDirectory, "Tools", "test");
        Directory.CreateDirectory(toolsDir);
        var testFile = Path.Combine(toolsDir, "test.exe");
        File.WriteAllText(testFile, "test");

        // Act
        var (isValid, repairedPath) = detector.ValidateAndRepairPath(testFile, "Tools/test/test.exe");

        // Assert
        Assert.True(isValid);
        Assert.Equal(testFile, repairedPath);
    }

    [Fact]
    public void ValidateAndRepairPath_RepairsInvalidPaths()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);
        
        // Create the default file but not the configured path
        var defaultDir = Path.Combine(_testDirectory, "Tools", "default");
        Directory.CreateDirectory(defaultDir);
        var defaultFile = Path.Combine(defaultDir, "tool.exe");
        File.WriteAllText(defaultFile, "test");

        var nonExistentPath = Path.Combine(_testDirectory, "NonExistent", "tool.exe");

        // Act
        var (isValid, repairedPath) = detector.ValidateAndRepairPath(nonExistentPath, "Tools/default/tool.exe");

        // Assert
        Assert.True(isValid);
        Assert.Equal(defaultFile, repairedPath);
    }

    [Fact]
    public void EnvironmentVariable_PortableMode_EnablesPortableMode()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        Environment.SetEnvironmentVariable("AURA_PORTABLE_MODE", "true");

        // Act
        var detector = new PortableDetector(_logger);

        // Assert
        Assert.True(detector.IsPortableMode);
    }

    [Fact]
    public void PortableMarkerFile_EnablesPortableMode()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var markerPath = Path.Combine(_testDirectory, ".portable");
        File.WriteAllText(markerPath, "test");

        // Act
        var detector = new PortableDetector(_logger);

        // Assert
        Assert.True(detector.IsPortableMode);
    }

    [Fact]
    public void FFmpegDirectory_ReturnsCorrectPath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);

        // Act
        var ffmpegDir = detector.FFmpegDirectory;

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "Tools", "ffmpeg"), ffmpegDir);
    }

    [Fact]
    public void PiperDirectory_ReturnsCorrectPath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("AURA_PORTABLE_ROOT", _testDirectory);
        var detector = new PortableDetector(_logger);

        // Act
        var piperDir = detector.PiperDirectory;

        // Assert
        Assert.Equal(Path.Combine(_testDirectory, "Tools", "piper"), piperDir);
    }
}
