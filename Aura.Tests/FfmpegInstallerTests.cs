using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class FfmpegInstallerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FfmpegInstaller _installer;
    private readonly HttpDownloader _downloader;
    
    public FfmpegInstallerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "AuraFFmpegInstallerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        
        var httpClient = new System.Net.Http.HttpClient();
        _downloader = new HttpDownloader(
            NullLogger<HttpDownloader>.Instance,
            httpClient);
        
        _installer = new FfmpegInstaller(
            NullLogger<FfmpegInstaller>.Instance,
            _downloader,
            _testDirectory,
            null); // No resolver for tests
    }
    
    [Fact]
    public async Task AttachExisting_WithValidFfmpeg_Succeeds()
    {
        // Arrange
        var mockFfmpegDir = Path.Combine(_testDirectory, "mock-ffmpeg");
        Directory.CreateDirectory(mockFfmpegDir);
        
        // Create a mock ffmpeg.exe (simple executable that responds to -version)
        var mockFfmpegPath = Path.Combine(mockFfmpegDir, "ffmpeg.exe");
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _installer.AttachExistingAsync(mockFfmpegPath, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.NotNull(result.FfmpegPath);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
        Assert.NotNull(result.ValidationOutput);
        
        // Verify metadata file was created
        var metadataPath = Path.Combine(mockFfmpegDir, "install.json");
        Assert.True(File.Exists(metadataPath));
    }
    
    [Fact]
    public async Task AttachExisting_WithDirectory_FindsFfmpeg()
    {
        // Arrange
        var mockFfmpegDir = Path.Combine(_testDirectory, "mock-ffmpeg-dir");
        Directory.CreateDirectory(mockFfmpegDir);
        
        var mockFfmpegPath = Path.Combine(mockFfmpegDir, "ffmpeg.exe");
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act - pass directory instead of exe path
        var result = await _installer.AttachExistingAsync(mockFfmpegDir, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
    }
    
    [Fact]
    public async Task AttachExisting_WithNestedBinDirectory_FindsFfmpeg()
    {
        // Arrange
        var mockFfmpegDir = Path.Combine(_testDirectory, "mock-ffmpeg-nested");
        var binDir = Path.Combine(mockFfmpegDir, "bin");
        Directory.CreateDirectory(binDir);
        
        var mockFfmpegPath = Path.Combine(binDir, "ffmpeg.exe");
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act - pass parent directory
        var result = await _installer.AttachExistingAsync(mockFfmpegDir, CancellationToken.None);
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal(mockFfmpegPath, result.FfmpegPath);
    }
    
    [Fact]
    public async Task AttachExisting_WithNonExistentPath_Fails()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "does-not-exist.exe");
        
        // Act
        var result = await _installer.AttachExistingAsync(nonExistentPath, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task AttachExisting_WithDirectoryWithoutFfmpeg_Fails()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDirectory, "empty-dir");
        Directory.CreateDirectory(emptyDir);
        
        // Act
        var result = await _installer.AttachExistingAsync(emptyDir, CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Could not find ffmpeg.exe", result.ErrorMessage);
    }
    
    [Fact]
    public async Task InstallFromLocalArchive_WithValidZip_Succeeds()
    {
        // Arrange
        var zipPath = Path.Combine(_testDirectory, "ffmpeg-test.zip");
        await CreateMockFfmpegZip(zipPath);
        
        // Act
        var result = await _installer.InstallFromLocalArchiveAsync(
            zipPath,
            "test-version",
            null,
            null,
            CancellationToken.None);
        
        // Assert
        Assert.True(result.Success, $"Expected success but got error: {result.ErrorMessage}");
        Assert.NotNull(result.FfmpegPath);
        Assert.True(File.Exists(result.FfmpegPath));
        Assert.NotNull(result.InstallPath);
        
        // Verify metadata
        var metadataPath = Path.Combine(result.InstallPath, "install.json");
        Assert.True(File.Exists(metadataPath));
    }
    
    [Fact]
    public async Task InstallFromLocalArchive_WithNonExistentFile_Fails()
    {
        // Arrange
        var nonExistentZip = Path.Combine(_testDirectory, "does-not-exist.zip");
        
        // Act
        var result = await _installer.InstallFromLocalArchiveAsync(
            nonExistentZip,
            "test-version",
            null,
            null,
            CancellationToken.None);
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task GetInstallMetadata_WithValidMetadata_ReturnsData()
    {
        // Arrange
        var mockFfmpegDir = Path.Combine(_testDirectory, "mock-with-metadata");
        Directory.CreateDirectory(mockFfmpegDir);
        
        var mockFfmpegPath = Path.Combine(mockFfmpegDir, "ffmpeg.exe");
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Attach to create metadata
        var attachResult = await _installer.AttachExistingAsync(mockFfmpegPath, CancellationToken.None);
        Assert.True(attachResult.Success);
        
        // Act
        var metadata = await _installer.GetInstallMetadataAsync(mockFfmpegDir);
        
        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("ffmpeg", metadata.Id);
        Assert.Equal(mockFfmpegPath, metadata.FfmpegPath);
        Assert.True(metadata.Validated);
        Assert.Equal("AttachExisting", metadata.SourceType);
    }
    
    [Fact]
    public async Task GetInstallMetadata_WithoutMetadataFile_ReturnsNull()
    {
        // Arrange
        var dirWithoutMetadata = Path.Combine(_testDirectory, "no-metadata");
        Directory.CreateDirectory(dirWithoutMetadata);
        
        // Act
        var metadata = await _installer.GetInstallMetadataAsync(dirWithoutMetadata);
        
        // Assert
        Assert.Null(metadata);
    }
    
    /// <summary>
    /// Create a mock ffmpeg.exe that responds to -version
    /// On Windows, creates a batch file wrapper; on Linux, creates a shell script
    /// </summary>
    private async Task CreateMockFfmpegBinary(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            // Create a batch file that echoes version info
            var batchContent = @"@echo off
if ""%1""==""-version"" (
    echo ffmpeg version test-mock-1.0 Copyright (c) 2000-2024 the FFmpeg developers
    exit /b 0
) else (
    exit /b 1
)
";
            await File.WriteAllTextAsync(path, batchContent);
        }
        else
        {
            // Create a shell script
            var shellContent = @"#!/bin/bash
if [ ""$1"" = ""-version"" ]; then
    echo ""ffmpeg version test-mock-1.0 Copyright (c) 2000-2024 the FFmpeg developers""
    exit 0
else
    exit 1
fi
";
            await File.WriteAllTextAsync(path, shellContent);
            
            // Make executable on Unix
            try
            {
                var process = System.Diagnostics.Process.Start("chmod", $"+x {path}");
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            catch
            {
                // Ignore chmod errors in test environment
            }
        }
    }
    
    /// <summary>
    /// Create a mock FFmpeg zip with a fake binary inside
    /// </summary>
    private async Task CreateMockFfmpegZip(string zipPath)
    {
        var tempExtractDir = Path.Combine(_testDirectory, "zip-contents");
        Directory.CreateDirectory(tempExtractDir);
        
        // Create nested structure like real FFmpeg builds
        var ffmpegDir = Path.Combine(tempExtractDir, "ffmpeg-n6.0-test");
        var binDir = Path.Combine(ffmpegDir, "bin");
        Directory.CreateDirectory(binDir);
        
        var ffmpegExePath = Path.Combine(binDir, "ffmpeg.exe");
        await CreateMockFfmpegBinary(ffmpegExePath);
        
        // Create zip
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }
        ZipFile.CreateFromDirectory(tempExtractDir, zipPath);
        
        // Cleanup temp directory
        Directory.Delete(tempExtractDir, true);
    }
    
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
