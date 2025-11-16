using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class FFmpegDirectCheckServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public FFmpegDirectCheckServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ffmpeg-direct-check-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task CheckAsync_WhenNoCandidates_ReturnsNotInstalled()
    {
        var service = new FFmpegDirectCheckService(
            NullLogger<FFmpegDirectCheckService>.Instance,
            Path.Combine(_tempDirectory, "managed"));

        var result = await service.CheckAsync().ConfigureAwait(false);

        Assert.False(result.Installed);
        Assert.False(result.Valid);
        Assert.Null(result.ChosenPath);
        Assert.NotEmpty(result.Candidates);
    }

    [Fact]
    public async Task CheckAsync_WhenEnvVarCandidateValid_ReturnsInstalled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var mockBinary = Path.Combine(_tempDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.bat" : "ffmpeg.sh");
        await CreateMockFfmpegBinary(mockBinary).ConfigureAwait(false);

        var previousValue = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", mockBinary);

        try
        {
            var service = new FFmpegDirectCheckService(
                NullLogger<FFmpegDirectCheckService>.Instance,
                Path.Combine(_tempDirectory, "managed"));

            var result = await service.CheckAsync().ConfigureAwait(false);

            Assert.True(result.Installed);
            Assert.True(result.Valid);
            Assert.Equal("EnvVar", result.Source);
            Assert.Equal(mockBinary, result.ChosenPath);
            Assert.Equal("6.0-test", result.Version);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", previousValue);
        }
    }

    private static async Task CreateMockFfmpegBinary(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var batchContent = @"@echo off
if ""%1""==""-version"" (
    echo ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers
    exit /b 0
)
exit /b 1";
            await File.WriteAllTextAsync(path, batchContent).ConfigureAwait(false);
        }
        else
        {
            var shellContent = @"#!/bin/bash
if [ ""$1"" = ""-version"" ]; then
    echo ""ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers""
    exit 0
fi
exit 1";
            await File.WriteAllTextAsync(path, shellContent).ConfigureAwait(false);
            try
            {
                System.Diagnostics.Process.Start("chmod", $"+x {path}")?.WaitForExit();
            }
            catch
            {
                // Ignore chmod failures in CI
            }
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }
}

