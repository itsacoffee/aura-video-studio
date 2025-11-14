using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.Render;
using Aura.Core.Services.Resources;
using Aura.Providers.Video;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using ProviderTimeline = Aura.Core.Providers.Timeline;

namespace Aura.Tests.Integration;

/// <summary>
/// End-to-end integration tests for FFmpeg video creation pipeline
/// </summary>
public class FFmpegIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FFmpegIntegrationTests> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _testOutputDir;
    private readonly string _testAssetsDir;

    public FFmpegIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new TestLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger<FFmpegIntegrationTests>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        _testOutputDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString("N"));
        _testAssetsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestAssets");
        
        Directory.CreateDirectory(_testOutputDir);
        Directory.CreateDirectory(_testAssetsDir);
    }

    [Fact(Skip = "Requires FFmpeg installed - run manually in CI/local")]
    public async Task CompleteVideoRenderingPipeline_ShouldSucceed()
    {
        // Arrange
        var ffmpegResolver = new FFmpegResolver(_loggerFactory.CreateLogger<FFmpegResolver>(), _cache);
        var ffmpegLocator = new FfmpegLocator(ffmpegResolver);
        
        // Verify FFmpeg is available
        var resolution = await ffmpegResolver.ResolveAsync(forceRefresh: true);
        if (!resolution.Found || !resolution.IsValid)
        {
            _output.WriteLine($"FFmpeg not found or invalid: {resolution.Error}");
            _output.WriteLine("This test requires FFmpeg to be installed. Skipping.");
            return;
        }

        _output.WriteLine($"Using FFmpeg: {resolution.Path} (Source: {resolution.Source})");

        // Create test audio file (simple sine wave for 5 seconds)
        var audioPath = await CreateTestAudioFileAsync(resolution.Path!);
        Assert.True(File.Exists(audioPath), "Test audio file should be created");

        // Create timeline with test data
        var providerScene = new Scene(
            Index: 0,
            Heading: "Test Scene",
            Script: "This is a test scene",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(5));

        var imagePath = await CreateTestImageAsync();
        var sceneAssets = new Dictionary<int, IReadOnlyList<Asset>>
        {
            { 0, new List<Asset> { new Asset("Image", imagePath, null, null) } }
        };

        var timeline = new ProviderTimeline(
            Scenes: new List<Scene> { providerScene },
            SceneAssets: sceneAssets,
            NarrationPath: audioPath,
            MusicPath: "",
            SubtitlesPath: null);

        // Create render spec
        var spec = new RenderSpec(
            Res: new Resolution(1280, 720), // 720p for faster testing
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "H264",
            QualityLevel: 50, // Medium quality for faster testing
            EnableSceneCut: true);

        var composer = new FfmpegVideoComposer(
            _loggerFactory.CreateLogger<FfmpegVideoComposer>(),
            ffmpegLocator,
            configuredFfmpegPath: resolution.Path,
            outputDirectory: _testOutputDir);

        var progressUpdates = new System.Collections.Generic.List<RenderProgress>();
        var progress = new Progress<RenderProgress>(p =>
        {
            progressUpdates.Add(p);
            _output.WriteLine($"Progress: {p.Percentage:F1}% - {p.CurrentStage}");
        });

        // Act
        var outputPath = await composer.RenderAsync(
            timeline,
            spec,
            progress,
            CancellationToken.None);

        // Assert
        Assert.NotNull(outputPath);
        Assert.True(File.Exists(outputPath), $"Output video should exist at {outputPath}");
        
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 1024, "Output file should be larger than 1KB");
        
        Assert.NotEmpty(progressUpdates);
        Assert.Contains(progressUpdates, p => p.Percentage > 0);
        Assert.Contains(progressUpdates, p => p.Percentage >= 100);

        _output.WriteLine($"✓ Video rendered successfully: {outputPath}");
        _output.WriteLine($"  File size: {fileInfo.Length / 1024.0:F2} KB");
        _output.WriteLine($"  Progress updates: {progressUpdates.Count}");
    }

    [Fact(Skip = "Requires FFmpeg installed - run manually in CI/local")]
    public async Task HardwareAccelerationDetection_ShouldDetectAvailableEncoders()
    {
        // Arrange
        var ffmpegResolver = new FFmpegResolver(_loggerFactory.CreateLogger<FFmpegResolver>(), _cache);
        var resolution = await ffmpegResolver.ResolveAsync(forceRefresh: true);
        
        if (!resolution.Found || resolution.Path == null)
        {
            _output.WriteLine("FFmpeg not available, skipping test");
            return;
        }

        var hardwareEncoder = new HardwareEncoder(
            _loggerFactory.CreateLogger<HardwareEncoder>(),
            resolution.Path!);

        // Act
        var capabilities = await hardwareEncoder.DetectHardwareCapabilitiesAsync();

        // Assert
        Assert.NotNull(capabilities);
        Assert.NotEmpty(capabilities.AvailableEncoders);
        
        _output.WriteLine("Hardware Capabilities:");
        _output.WriteLine($"  NVENC: {capabilities.HasNVENC}");
        _output.WriteLine($"  AMF: {capabilities.HasAMF}");
        _output.WriteLine($"  QuickSync: {capabilities.HasQSV}");
        _output.WriteLine($"  VideoToolbox: {capabilities.HasVideoToolbox}");
        _output.WriteLine($"  Available Encoders: {string.Join(", ", capabilities.AvailableEncoders)}");

        if (capabilities.GpuMemory != null)
        {
            _output.WriteLine($"  GPU: {capabilities.GpuMemory.GpuName}");
            _output.WriteLine($"  GPU Memory: {capabilities.GpuMemory.TotalMemoryBytes / (1024.0 * 1024.0 * 1024.0):F2} GB");
        }
    }

    [Fact]
    public async Task FFmpegCommandBuilder_ShouldBuildValidCommand()
    {
        // Arrange
        var builder = new FFmpegCommandBuilder()
            .AddInput("input.mp4")
            .SetOutput("output.mp4")
            .SetVideoCodec("libx264")
            .SetAudioCodec("aac")
            .SetResolution(1920, 1080)
            .SetFrameRate(30)
            .SetVideoBitrate(5000)
            .SetAudioBitrate(192)
            .SetPixelFormat("yuv420p")
            .SetPreset("medium")
            .SetCRF(23)
            .AddMetadata("title", "Test Video");

        // Act
        var command = builder.Build();

        // Assert
        Assert.NotEmpty(command);
        Assert.Contains("-i \"input.mp4\"", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-s 1920x1080", command);
        Assert.Contains("-r 30", command);
        Assert.Contains("-b:v 5000k", command);
        Assert.Contains("-b:a 192k", command);
        Assert.Contains("-pix_fmt yuv420p", command);
        Assert.Contains("-preset medium", command);
        Assert.Contains("-crf 23", command);
        Assert.Contains("\"output.mp4\"", command);

        _output.WriteLine("Generated FFmpeg command:");
        _output.WriteLine(command);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task FFmpegQualityPresets_ShouldProvideValidPresets()
    {
        // Arrange & Act
        var draftPreset = FFmpegQualityPresets.GetPreset(QualityLevel.Draft);
        var standardPreset = FFmpegQualityPresets.GetPreset(QualityLevel.Good);
        var premiumPreset = FFmpegQualityPresets.GetPreset(QualityLevel.High);
        var maximumPreset = FFmpegQualityPresets.GetPreset(QualityLevel.Maximum);

        // Assert
        Assert.Equal("Draft", draftPreset.Name);
        Assert.Equal("ultrafast", draftPreset.Preset);
        Assert.Equal(28, draftPreset.CRF);
        Assert.False(draftPreset.TwoPass);

        Assert.Equal("Standard", standardPreset.Name);
        Assert.Equal("medium", standardPreset.Preset);
        Assert.Equal(23, standardPreset.CRF);
        Assert.False(standardPreset.TwoPass);

        Assert.Equal("Premium", premiumPreset.Name);
        Assert.Equal("slow", premiumPreset.Preset);
        Assert.Equal(18, premiumPreset.CRF);
        Assert.True(premiumPreset.TwoPass);

        Assert.Equal("Maximum", maximumPreset.Name);
        Assert.Equal("veryslow", maximumPreset.Preset);
        Assert.Equal(15, maximumPreset.CRF);
        Assert.True(maximumPreset.TwoPass);

        _output.WriteLine("Quality Presets:");
        _output.WriteLine($"  Draft: CRF={draftPreset.CRF}, Preset={draftPreset.Preset}, Bitrate={draftPreset.VideoBitrate}k");
        _output.WriteLine($"  Standard: CRF={standardPreset.CRF}, Preset={standardPreset.Preset}, Bitrate={standardPreset.VideoBitrate}k");
        _output.WriteLine($"  Premium: CRF={premiumPreset.CRF}, Preset={premiumPreset.Preset}, Bitrate={premiumPreset.VideoBitrate}k");
        _output.WriteLine($"  Maximum: CRF={maximumPreset.CRF}, Preset={maximumPreset.Preset}, Bitrate={maximumPreset.VideoBitrate}k");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ResourceManagement_ShouldMonitorDiskSpace()
    {
        // Arrange
        var diskSpaceChecker = new DiskSpaceChecker(_loggerFactory.CreateLogger<DiskSpaceChecker>());

        // Act
        var diskInfo = diskSpaceChecker.GetDiskSpaceInfo(_testOutputDir);
        var estimatedSpace = diskSpaceChecker.EstimateVideoSpaceRequired(
            durationSeconds: 60,
            quality: 75);

        // Assert
        Assert.True(diskInfo.IsAvailable);
        Assert.True(diskInfo.TotalBytes > 0);
        Assert.True(diskInfo.AvailableBytes > 0);
        Assert.InRange(diskInfo.PercentUsed, 0, 100);
        
        Assert.True(estimatedSpace > 0);

        _output.WriteLine("Disk Space Information:");
        _output.WriteLine($"  Drive: {diskInfo.DriveName}");
        _output.WriteLine($"  Total: {diskInfo.TotalGigabytes:F2} GB");
        _output.WriteLine($"  Available: {diskInfo.AvailableGigabytes:F2} GB");
        _output.WriteLine($"  Used: {diskInfo.PercentUsed:F1}%");
        _output.WriteLine($"  Estimated space for 60s video: {estimatedSpace / (1024.0 * 1024.0):F2} MB");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessManager_ShouldTrackAndCleanupProcesses()
    {
        // Arrange
        var processManager = new ProcessManager(_loggerFactory.CreateLogger<ProcessManager>());
        var jobId = Guid.NewGuid().ToString("N");
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;

        // Act
        processManager.RegisterProcess(processId, jobId);
        var tracked = processManager.GetTrackedProcesses();
        processManager.UnregisterProcess(processId);
        var trackedAfter = processManager.GetTrackedProcesses();

        // Assert
        Assert.Contains(processId, tracked);
        Assert.DoesNotContain(processId, trackedAfter);

        _output.WriteLine($"Process tracking:");
        _output.WriteLine($"  Registered PID: {processId}");
        _output.WriteLine($"  Tracked count: {tracked.Length}");
        _output.WriteLine($"  After unregister: {trackedAfter.Length}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test audio file using FFmpeg
    /// </summary>
    private async Task<string> CreateTestAudioFileAsync(string ffmpegPath)
    {
        var outputPath = Path.Combine(_testOutputDir, "test_audio.mp3");
        
        // Generate 5 seconds of sine wave at 440Hz (A note)
        var args = $"-f lavfi -i \"sine=frequency=440:duration=5\" -b:a 128k \"{outputPath}\"";
        
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start FFmpeg process");
        }

        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"FFmpeg failed to create test audio: {error}");
        }

        return outputPath;
    }

    /// <summary>
    /// Creates a test image using FFmpeg
    /// </summary>
    private async Task<string> CreateTestImageAsync()
    {
        var outputPath = Path.Combine(_testOutputDir, "test_image.png");
        
        // Create a simple gradient image
        var ffmpegResolver = new FFmpegResolver(_loggerFactory.CreateLogger<FFmpegResolver>(), _cache);
        var resolution = await ffmpegResolver.ResolveAsync();
        
        if (!resolution.Found || string.IsNullOrEmpty(resolution.Path))
        {
            // Fallback: create a simple colored image using System.Drawing if available
            // For now, just create a dummy file
            File.WriteAllText(outputPath, "dummy");
            return outputPath;
        }

        var args = $"-f lavfi -i \"color=c=blue:s=1280x720:d=1\" -frames:v 1 \"{outputPath}\"";
        
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = resolution.Path,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        return outputPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Warning: Failed to cleanup test directory: {ex.Message}");
        }

        _cache?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test logger that writes to xUnit output
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
            if (exception != null)
            {
                _output.WriteLine($"  Exception: {exception}");
            }
        }
        catch
        {
            // Ignore errors writing to test output
        }
    }
}

/// <summary>
/// Test logger factory that writes to xUnit output
/// </summary>
internal class TestLoggerFactory : ILoggerFactory
{
    private readonly ITestOutputHelper _output;

    public TestLoggerFactory(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger<object>(_output);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // Not needed for tests
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public ILogger<T> CreateLogger<T>()
    {
        return new TestLogger<T>(_output);
    }
}

/// <summary>
/// Fake FFmpeg locator for testing
/// </summary>
internal class FfmpegLocator : IFfmpegLocator
{
    private readonly FFmpegResolver _resolver;

    public FfmpegLocator(FFmpegResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task<string> GetEffectiveFfmpegPathAsync(string? configuredPath = null, CancellationToken ct = default)
    {
        var result = await _resolver.ResolveAsync(configuredPath, forceRefresh: false, ct);
        
        if (!result.Found || string.IsNullOrEmpty(result.Path))
        {
            throw new InvalidOperationException($"FFmpeg not found: {result.Error}");
        }

        return result.Path;
    }

    public async Task<FfmpegValidationResult> CheckAllCandidatesAsync(string? configuredPath = null, CancellationToken ct = default)
    {
        var result = await _resolver.ResolveAsync(configuredPath, forceRefresh: false, ct);
        return new FfmpegValidationResult
        {
            Found = result.Found,
            FfmpegPath = result.Path,
            Reason = result.Error,
            VersionString = result.Version
        };
    }

    public async Task<FfmpegValidationResult> ValidatePathAsync(string ffmpegPath, CancellationToken ct = default)
    {
        // For test purposes, just check if the path exists
        return new FfmpegValidationResult
        {
            Found = File.Exists(ffmpegPath),
            FfmpegPath = ffmpegPath,
            Reason = File.Exists(ffmpegPath) ? null : "File not found",
            VersionString = null
        };
    }
}
