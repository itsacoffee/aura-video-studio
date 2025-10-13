using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Aura.Core.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for FFmpeg Detection API endpoints
/// </summary>
public class FfmpegDetectionApiTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _manifestPath;
    private readonly string _configPath;
    private readonly string _logDirectory;
    private readonly HttpClient _httpClient;
    private readonly DownloadsController _controller;

    public FfmpegDetectionApiTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-ffmpeg-api-tests-" + Guid.NewGuid().ToString());
        _toolsDirectory = Path.Combine(_testDirectory, "Tools");
        _manifestPath = Path.Combine(_testDirectory, "manifest.json");
        _configPath = Path.Combine(_testDirectory, "engines-config.json");
        _logDirectory = Path.Combine(_testDirectory, "logs");
        _httpClient = new HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_toolsDirectory);
        Directory.CreateDirectory(_logDirectory);

        // Setup controller
        var downloader = new HttpDownloader(
            NullLogger<HttpDownloader>.Instance,
            _httpClient);
        
        var ffmpegInstaller = new FfmpegInstaller(
            NullLogger<FfmpegInstaller>.Instance,
            downloader,
            _toolsDirectory,
            null); // No resolver for tests
        
        var ffmpegLocator = new FfmpegLocator(
            NullLogger<FfmpegLocator>.Instance,
            _toolsDirectory);
        
        var processManager = new ExternalProcessManager(
            NullLogger<ExternalProcessManager>.Instance,
            _httpClient,
            _logDirectory);
        
        var enginesRegistry = new LocalEnginesRegistry(
            NullLogger<LocalEnginesRegistry>.Instance,
            processManager,
            _configPath);
        
        var manifestLoader = new EngineManifestLoader(
            NullLogger<EngineManifestLoader>.Instance,
            _httpClient,
            _manifestPath);
        
        _controller = new DownloadsController(
            NullLogger<DownloadsController>.Instance,
            ffmpegInstaller,
            ffmpegLocator,
            enginesRegistry,
            manifestLoader);
        
        // Set HttpContext for controller
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public void Dispose()
    {
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
        _httpClient.Dispose();
    }

    [Fact]
    public async Task RescanFFmpeg_WithNoFFmpeg_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RescanFFmpeg(default);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Use reflection to get properties
        var foundProperty = response?.GetType().GetProperty("found");
        Assert.NotNull(foundProperty);
        var found = (bool?)foundProperty.GetValue(response);
        Assert.False(found);
        
        var attemptedPathsProperty = response?.GetType().GetProperty("attemptedPaths");
        Assert.NotNull(attemptedPathsProperty);
        var attemptedPaths = attemptedPathsProperty.GetValue(response);
        Assert.NotNull(attemptedPaths);
    }

    [Fact]
    public async Task RescanFFmpeg_WithValidFFmpeg_FindsAndRegisters()
    {
        // Arrange - Create mock FFmpeg in dependencies folder
        var depsDir = Path.Combine(Path.GetDirectoryName(_toolsDirectory)!, "dependencies", "bin");
        Directory.CreateDirectory(depsDir);
        
        var mockFfmpegPath = Path.Combine(depsDir, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        // Act
        var result = await _controller.RescanFFmpeg(default);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Check found property
        var foundProperty = response?.GetType().GetProperty("found");
        Assert.NotNull(foundProperty);
        var found = (bool?)foundProperty.GetValue(response);
        Assert.True(found, "Expected FFmpeg to be found during rescan");
        
        // Check ffmpegPath property
        var pathProperty = response?.GetType().GetProperty("ffmpegPath");
        Assert.NotNull(pathProperty);
        var ffmpegPath = (string?)pathProperty.GetValue(response);
        Assert.NotNull(ffmpegPath);
        Assert.Equal(mockFfmpegPath, ffmpegPath);
    }

    [Fact]
    public async Task AttachFFmpeg_WithValidPath_Succeeds()
    {
        // Arrange
        var mockDir = Path.Combine(_testDirectory, "external-ffmpeg");
        Directory.CreateDirectory(mockDir);
        
        var mockFfmpegPath = Path.Combine(mockDir, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        var request = new AttachFFmpegRequest
        {
            Path = mockFfmpegPath
        };
        
        // Act
        var result = await _controller.AttachFFmpeg(request, default);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        // Check success property
        var successProperty = response?.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        var success = (bool?)successProperty.GetValue(response);
        Assert.True(success);
        
        // Check ffmpegPath property
        var pathProperty = response?.GetType().GetProperty("ffmpegPath");
        Assert.NotNull(pathProperty);
        var ffmpegPath = (string?)pathProperty.GetValue(response);
        Assert.NotNull(ffmpegPath);
        
        // Verify metadata file was created
        var metadataPath = Path.Combine(mockDir, "install.json");
        Assert.True(File.Exists(metadataPath), "install.json should be created");
    }

    [Fact]
    public async Task AttachFFmpeg_WithDirectory_FindsExecutable()
    {
        // Arrange
        var mockDir = Path.Combine(_testDirectory, "external-ffmpeg-dir");
        var binDir = Path.Combine(mockDir, "bin");
        Directory.CreateDirectory(binDir);
        
        var mockFfmpegPath = Path.Combine(binDir, "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
        await CreateMockFfmpegBinary(mockFfmpegPath);
        
        var request = new AttachFFmpegRequest
        {
            Path = mockDir  // Pass directory, not executable
        };
        
        // Act
        var result = await _controller.AttachFFmpeg(request, default);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        var successProperty = response?.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        var success = (bool?)successProperty.GetValue(response);
        Assert.True(success);
        
        var pathProperty = response?.GetType().GetProperty("ffmpegPath");
        Assert.NotNull(pathProperty);
        var ffmpegPath = (string?)pathProperty.GetValue(response);
        Assert.Equal(mockFfmpegPath, ffmpegPath);
    }

    [Fact]
    public async Task AttachFFmpeg_WithInvalidPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new AttachFFmpegRequest
        {
            Path = "/nonexistent/path/ffmpeg"
        };
        
        // Act
        var result = await _controller.AttachFFmpeg(request, default);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        
        var errorProperty = response?.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        var error = (string?)errorProperty.GetValue(response);
        Assert.NotNull(error);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AttachFFmpeg_WithEmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new AttachFFmpegRequest
        {
            Path = ""
        };
        
        // Act
        var result = await _controller.AttachFFmpeg(request, default);
        
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        
        var errorProperty = response?.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        var error = (string?)errorProperty.GetValue(response);
        Assert.Equal("Path is required", error);
    }

    /// <summary>
    /// Create a mock ffmpeg binary that responds to -version
    /// </summary>
    private async Task CreateMockFfmpegBinary(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Create a batch script that handles both -version and smoke test
            var batchContent = @"@echo off
if ""%1""==""-version"" (
    echo ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers
    echo built with gcc 12.2.0
    exit /b 0
)
if ""%1""==""-hide_banner"" (
    REM Smoke test command - create output file with enough content (>100 bytes)
    for %%a in (%*) do set ""lastarg=%%~a""
    echo RIFF....WAVEfmt ................data................................ > %lastarg%
    echo Mock WAV file content for testing purposes only. >> %lastarg%
    echo This ensures the file is large enough for validation. >> %lastarg%
    exit /b 0
)
exit /b 1";
            await File.WriteAllTextAsync(path, batchContent);
        }
        else
        {
            // Create a shell script that handles both -version and smoke test
            var shellContent = @"#!/bin/bash
if [ ""$1"" = ""-version"" ]; then
    echo ""ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers""
    echo ""built with gcc 12.2.0""
    exit 0
fi
if [ ""$1"" = ""-hide_banner"" ]; then
    # Smoke test - create output file with enough content (>100 bytes)
    output=""${!#}""
    {
        printf ""RIFF....WAVEfmt ................data................................""
        echo ""Mock WAV file content for testing purposes only.""
        echo ""This ensures the file is large enough for validation.""
    } > ""$output""
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
