using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.Setup;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.FFmpeg;

/// <summary>
/// Windows-specific integration tests for FFmpeg
/// </summary>
public class FFmpegWindowsIntegrationTests
{
    private readonly ILogger<FFmpegDetectionService> _detectionLogger;
    private readonly ILogger<FFmpegResolver> _resolverLogger;
    private readonly IMemoryCache _cache;

    public FFmpegWindowsIntegrationTests()
    {
        _detectionLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<FFmpegDetectionService>();
        _resolverLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<FFmpegResolver>();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact(Skip = "Windows-only test, requires manual execution on Windows")]
    public async Task DetectFFmpeg_OnWindows_FindsViaPATHOrRegistry()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var service = new FFmpegDetectionService(_detectionLogger, _cache);
        var result = await service.DetectFFmpegAsync();

        // On a properly configured Windows system, FFmpeg should be found
        // either via PATH, Registry, or common installation paths
        Assert.True(result.IsInstalled || result.Error != null);
        
        if (result.IsInstalled)
        {
            Assert.NotNull(result.Path);
            Assert.True(File.Exists(result.Path), $"FFmpeg path should exist: {result.Path}");
            Assert.NotNull(result.Version);
        }
    }

    [Fact]
    public void EscapePath_HandlesWindowsPathsWithSpaces()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var builder = new FFmpegCommandBuilder();
        builder.AddInput(@"C:\Program Files\Test Video\input.mp4");
        builder.SetOutput(@"C:\Users\Test User\Documents\output.mp4");

        var command = builder.Build();

        // Should contain quoted paths with forward slashes
        Assert.Contains("\"C:/Program Files/Test Video/input.mp4\"", command);
        Assert.Contains("\"C:/Users/Test User/Documents/output.mp4\"", command);
    }

    [Fact]
    public void EscapePath_HandlesWindowsBackslashes()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var builder = new FFmpegCommandBuilder();
        builder.AddInput(@"C:\Videos\input.mp4");
        builder.SetOutput(@"D:\Output\result.mp4");

        var command = builder.Build();

        // Backslashes should be converted to forward slashes
        Assert.Contains("C:/Videos/input.mp4", command);
        Assert.Contains("D:/Output/result.mp4", command);
        Assert.DoesNotContain("C:\\", command);
        Assert.DoesNotContain("D:\\", command);
    }

    [Fact]
    public void EscapePath_HandlesLongWindowsPaths()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Create a very long path
        var longPath = @"C:\Users\TestUser\Documents\Videos\Projects\2024\January\WeekOne\DayOne\Morning\Session\Recording\Final\Export\Version2\Draft\video_with_very_long_name_that_exceeds_normal_limits.mp4";

        var builder = new FFmpegCommandBuilder();
        builder.AddInput(longPath);
        builder.SetOutput(@"C:\Output\short.mp4");

        var command = builder.Build();

        // Should handle long paths correctly
        Assert.Contains("\"C:/Users/TestUser", command);
    }

    [Fact]
    public void EscapePath_HandlesSpecialCharacters()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var builder = new FFmpegCommandBuilder();
        builder.AddInput(@"C:\Videos\Test (2024) - Final [HD].mp4");
        builder.SetOutput(@"C:\Output\Result & Review.mp4");

        var command = builder.Build();

        // Should preserve special characters within quotes
        Assert.Contains("Test (2024) - Final [HD].mp4", command);
        Assert.Contains("Result & Review.mp4", command);
    }

    [Fact(Skip = "Windows-only test, requires FFmpeg installed")]
    public async Task FFmpegProcessSpawn_OnWindows_ExecutesSuccessfully()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var resolver = new FFmpegResolver(_resolverLogger, _cache);
        var result = await resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Skip if FFmpeg not available
        if (!result.Found || !result.IsValid)
        {
            return;
        }

        Assert.NotNull(result.Path);
        Assert.True(File.Exists(result.Path));

        // Test simple version command
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = result.Path,
            Arguments = "-version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        Assert.NotNull(process);

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("ffmpeg version", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Windows-only test, requires FFmpeg installed")]
    public async Task FFmpegProcessSpawn_WithWindowsPaths_HandlesQuotedArguments()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var resolver = new FFmpegResolver(_resolverLogger, _cache);
        var result = await resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Skip if FFmpeg not available
        if (!result.Found || !result.IsValid)
        {
            return;
        }

        // Create temporary input file with spaces in path
        var tempDir = Path.Combine(Path.GetTempPath(), "Test Directory With Spaces");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputFile = Path.Combine(tempDir, "test input.txt");
            var outputFile = Path.Combine(tempDir, "test output.mp4");

            // Create dummy input file
            await File.WriteAllTextAsync(inputFile, "test");

            // Build command with paths containing spaces
            var builder = new FFmpegCommandBuilder();
            builder.AddInput(inputFile);
            builder.SetOutput(outputFile);
            builder.SetVideoCodec("libx264");
            builder.SetDuration(TimeSpan.FromSeconds(1));

            var command = builder.Build();

            // Verify paths are properly quoted
            Assert.Contains($"\"", command);
            Assert.DoesNotContain("\" \"", command); // No double quoting
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact(Skip = "Windows-only test, requires NVIDIA GPU")]
    public async Task HardwareAcceleration_OnWindows_DetectsNVENC()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var resolver = new FFmpegResolver(_resolverLogger, _cache);
        var result = await resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Skip if FFmpeg not available
        if (!result.Found || !result.IsValid || result.Path == null)
        {
            return;
        }

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<Aura.Core.Services.Render.HardwareEncoder>();
        
        var hardwareEncoder = new Aura.Core.Services.Render.HardwareEncoder(logger, result.Path);
        var capabilities = await hardwareEncoder.DetectHardwareCapabilitiesAsync();

        // If NVIDIA GPU is present, NVENC should be detected
        // This is informational - not all test machines will have NVIDIA GPUs
        if (capabilities.HasNVENC)
        {
            Assert.True(capabilities.AvailableEncoders.Count > 0);
            Assert.Contains(capabilities.AvailableEncoders, e => e.Contains("nvenc"));
        }
    }

    [Fact(Skip = "Windows-only test, requires AMD GPU")]
    public async Task HardwareAcceleration_OnWindows_DetectsAMF()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var resolver = new FFmpegResolver(_resolverLogger, _cache);
        var result = await resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Skip if FFmpeg not available
        if (!result.Found || !result.IsValid || result.Path == null)
        {
            return;
        }

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<Aura.Core.Services.Render.HardwareEncoder>();
        
        var hardwareEncoder = new Aura.Core.Services.Render.HardwareEncoder(logger, result.Path);
        var capabilities = await hardwareEncoder.DetectHardwareCapabilitiesAsync();

        // If AMD GPU is present, AMF should be detected
        if (capabilities.HasAMF)
        {
            Assert.True(capabilities.AvailableEncoders.Count > 0);
            Assert.Contains(capabilities.AvailableEncoders, e => e.Contains("amf"));
        }
    }

    [Fact(Skip = "Windows-only test, requires Intel GPU")]
    public async Task HardwareAcceleration_OnWindows_DetectsQuickSync()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var resolver = new FFmpegResolver(_resolverLogger, _cache);
        var result = await resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Skip if FFmpeg not available
        if (!result.Found || !result.IsValid || result.Path == null)
        {
            return;
        }

        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<Aura.Core.Services.Render.HardwareEncoder>();
        
        var hardwareEncoder = new Aura.Core.Services.Render.HardwareEncoder(logger, result.Path);
        var capabilities = await hardwareEncoder.DetectHardwareCapabilitiesAsync();

        // If Intel GPU is present, QuickSync (QSV) should be detected
        if (capabilities.HasQSV)
        {
            Assert.True(capabilities.AvailableEncoders.Count > 0);
            Assert.Contains(capabilities.AvailableEncoders, e => e.Contains("qsv"));
        }
    }

    [Fact]
    public void FFmpegResolver_OnWindows_ChecksRegistryPaths()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var service = new FFmpegDetectionService(_detectionLogger, _cache);
        
        // This test verifies that the registry checking code doesn't throw
        // The actual registry may or may not have FFmpeg entries
        var task = service.DetectFFmpegAsync();
        task.Wait(TimeSpan.FromSeconds(5));

        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void CommandBuilder_OnWindows_ProducesValidFFmpegCommand()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var builder = new FFmpegCommandBuilder()
            .AddInput(@"C:\Videos\input.mp4")
            .SetOutput(@"C:\Output\result.mp4")
            .SetVideoCodec("libx264")
            .SetAudioCodec("aac")
            .SetVideoBitrate(5000)
            .SetAudioBitrate(192)
            .SetResolution(1920, 1080)
            .SetFrameRate(30);

        var command = builder.Build();

        // Verify all essential components are present
        Assert.Contains("-i", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:v 5000k", command);
        Assert.Contains("-b:a 192k", command);
        Assert.Contains("-s 1920x1080", command);
        Assert.Contains("-r 30", command);

        // Verify Windows paths are properly escaped
        Assert.Contains("C:/Videos/input.mp4", command);
        Assert.Contains("C:/Output/result.mp4", command);
    }

    [Fact]
    public async Task FfmpegLocator_RespectsElectronEnvironmentVariable()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Set mock environment variable
        var testPath = @"C:\TestApp\resources\ffmpeg\win-x64\bin";
        Environment.SetEnvironmentVariable("FFMPEG_PATH", testPath);

        try
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<FfmpegLocator>();
            var locator = new FfmpegLocator(logger);

            var result = await locator.CheckAllCandidatesAsync();

            // Should include the Electron path in attempted paths
            var attemptedElectronPath = Path.Combine(testPath, "ffmpeg.exe");
            Assert.Contains(attemptedElectronPath, result.AttemptedPaths);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("FFMPEG_PATH", null);
        }
    }

    [Fact]
    public async Task FfmpegLocator_ChecksMultipleElectronEnvironmentVariables()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Set both environment variables
        var ffmpegPath = @"C:\TestApp\resources\ffmpeg";
        var binariesPath = @"C:\TestApp\resources\binaries";
        Environment.SetEnvironmentVariable("FFMPEG_PATH", ffmpegPath);
        Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", binariesPath);

        try
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<FfmpegLocator>();
            var locator = new FfmpegLocator(logger);

            var result = await locator.CheckAllCandidatesAsync();

            // Should check both paths
            var ffmpegExe = Path.Combine(ffmpegPath, "ffmpeg.exe");
            var binariesExe = Path.Combine(binariesPath, "ffmpeg.exe");
            
            Assert.Contains(ffmpegExe, result.AttemptedPaths);
            Assert.Contains(binariesExe, result.AttemptedPaths);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("FFMPEG_PATH", null);
            Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", null);
        }
    }

    [Fact]
    public void FfmpegLocator_WindowsRegistry_DoesNotThrowException()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Test that registry checking doesn't throw even if keys don't exist
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<FfmpegLocator>();
        var locator = new FfmpegLocator(logger);

        // This should not throw an exception even if registry keys are missing
        var exception = Record.Exception(async () => 
        {
            var result = await locator.CheckAllCandidatesAsync();
            // We don't care if FFmpeg is found, just that no exception is thrown
        });

        Assert.Null(exception);
    }

    [Fact]
    public void EscapePath_HandlesUNCPaths()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var builder = new FFmpegCommandBuilder();
        builder.AddInput(@"\\server\share\Videos\input.mp4");
        builder.SetOutput(@"\\backup\videos\output.mp4");

        var command = builder.Build();

        // UNC paths should be preserved but with forward slashes
        Assert.Contains("//server/share/Videos/input.mp4", command);
        Assert.Contains("//backup/videos/output.mp4", command);
    }

    [Fact(Skip = "Requires actual FFmpeg installation")]
    public async Task Integration_EndToEnd_FFmpegPathDetectionAndExecution()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // 1. Detect FFmpeg
        var locatorLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<FfmpegLocator>();
        var locator = new FfmpegLocator(locatorLogger);
        var result = await locator.GetEffectiveFfmpegPathAsync();

        Assert.NotNull(result);
        Assert.True(File.Exists(result), $"FFmpeg should exist at: {result}");

        // 2. Execute version command
        var ffmpegLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<FFmpegService>();
        var ffmpegService = new FFmpegService(locator, ffmpegLogger);

        var version = await ffmpegService.GetVersionAsync();
        Assert.NotNull(version);
        Assert.Contains("ffmpeg", version, StringComparison.OrdinalIgnoreCase);

        // 3. Check hardware acceleration
        var hwLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<HardwareAccelerationDetector>();
        var hwDetector = new HardwareAccelerationDetector(hwLogger);
        var capabilities = await hwDetector.DetectCapabilitiesAsync(result);

        Assert.NotNull(capabilities);
        // At minimum, software encoding should be available
        Assert.True(capabilities.AvailableEncoders.Count > 0);
    }
}
