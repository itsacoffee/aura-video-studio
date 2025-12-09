using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Aura.Tests.TestUtilities;

internal static class FfmpegTestHelper
{
    internal static string GetRuntimeRidSegment()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        return "linux-x64";
    }

    internal static async Task CreateMockFfmpegBinary(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var batchContent = @"@echo off
if ""%1""==""-version"" (
    echo ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers
    echo built with gcc 12.2.0
    exit /b 0
)
exit /b 1";
            await File.WriteAllTextAsync(path, batchContent);
            return;
        }

        var shellContent = @"#!/bin/bash
if [ ""$1"" = ""-version"" ]; then
    echo ""ffmpeg version 6.0-test Copyright (c) 2000-2024 the FFmpeg developers""
    echo ""built with gcc 12.2.0""
    exit 0
fi
exit 1";
        await File.WriteAllTextAsync(path, shellContent);

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
            // Ignore chmod failures in test helper
        }
    }
}
