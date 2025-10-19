using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class TtsFallbackServiceTests
{
    private readonly ILogger<TtsFallbackService> _logger;
    private readonly ILogger<WavFileWriter> _wavLogger;
    private readonly WavFileWriter _wavFileWriter;
    private readonly TtsFallbackService _service;

    public TtsFallbackServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        _logger = loggerFactory.CreateLogger<TtsFallbackService>();
        _wavLogger = loggerFactory.CreateLogger<WavFileWriter>();
        _wavFileWriter = new WavFileWriter(_wavLogger);
        _service = new TtsFallbackService(_logger, _wavFileWriter);
    }

    [Fact]
    public async Task SynthesizeWithFallbackAsync_Should_ReturnPrimaryVoiceOnSuccess()
    {
        // Arrange
        var lines = CreateTestScriptLines(2);
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var mockProvider = new MockSuccessfulTtsProvider(_wavFileWriter);

        // Act
        var result = await _service.SynthesizeWithFallbackAsync(
            mockProvider, lines, voiceSpec, 5.0, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.UsedFallback);
        Assert.Equal("TestVoice", result.UsedVoice);
        Assert.True(File.Exists(result.OutputPath));

        // Cleanup
        if (File.Exists(result.OutputPath))
        {
            File.Delete(result.OutputPath);
        }
    }

    [Fact]
    public async Task SynthesizeWithFallbackAsync_Should_FallbackToAlternateVoice()
    {
        // Arrange
        var lines = CreateTestScriptLines(2);
        var voiceSpec = new VoiceSpec("FailingVoice", 1.0, 1.0, PauseStyle.Natural);
        var mockProvider = new MockPartiallyFailingTtsProvider(_wavFileWriter);

        // Act
        var result = await _service.SynthesizeWithFallbackAsync(
            mockProvider, lines, voiceSpec, 5.0, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UsedFallback);
        Assert.Equal("AlternateVoice", result.UsedVoice);
        Assert.Contains("Primary voice", result.FallbackReason);
        Assert.True(File.Exists(result.OutputPath));

        // Cleanup
        if (File.Exists(result.OutputPath))
        {
            File.Delete(result.OutputPath);
        }
    }

    [Fact]
    public async Task SynthesizeWithFallbackAsync_Should_FallbackToSilence()
    {
        // Arrange
        var lines = CreateTestScriptLines(2);
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var mockProvider = new MockCompletelyFailingTtsProvider();

        // Act
        var result = await _service.SynthesizeWithFallbackAsync(
            mockProvider, lines, voiceSpec, 5.0, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UsedFallback);
        Assert.Equal("Silent Fallback", result.UsedVoice);
        Assert.Contains("failed", result.FallbackReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.OutputPath));
        
        // Verify it's a valid WAV file
        var fileInfo = new FileInfo(result.OutputPath);
        Assert.True(fileInfo.Length > 128, "Silent fallback should be larger than 128 bytes");

        // Cleanup
        if (File.Exists(result.OutputPath))
        {
            File.Delete(result.OutputPath);
        }
    }

    [Fact]
    public async Task SynthesizeWithFallbackAsync_Should_HandleZeroByteFiles()
    {
        // Arrange
        var lines = CreateTestScriptLines(2);
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 1.0, PauseStyle.Natural);
        var mockProvider = new MockZeroByteProvider();

        // Act
        var result = await _service.SynthesizeWithFallbackAsync(
            mockProvider, lines, voiceSpec, 5.0, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Zero-byte file should be detected as invalid and fallback should be used
        Assert.True(File.Exists(result.OutputPath));
        
        var fileInfo = new FileInfo(result.OutputPath);
        Assert.True(fileInfo.Length > 128, "Final output should be larger than 128 bytes");

        // Cleanup
        if (File.Exists(result.OutputPath))
        {
            File.Delete(result.OutputPath);
        }
    }

    [Fact]
    public void CreateDiagnosticMessage_Should_HandleSuccess()
    {
        // Arrange
        var result = new TtsFallbackResult
        {
            UsedFallback = false,
            UsedVoice = "TestVoice"
        };

        // Act
        var message = TtsFallbackService.CreateDiagnosticMessage(result);

        // Assert
        Assert.Contains("successfully", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TestVoice", message);
    }

    [Fact]
    public void CreateDiagnosticMessage_Should_HandleFallback()
    {
        // Arrange
        var result = new TtsFallbackResult
        {
            UsedFallback = true,
            UsedVoice = "AlternateVoice",
            FallbackReason = "Primary failed"
        };

        // Act
        var message = TtsFallbackService.CreateDiagnosticMessage(result);

        // Assert
        Assert.Contains("Primary failed", message);
        Assert.Contains("AlternateVoice", message);
    }

    [Fact]
    public void CreateDiagnosticMessage_Should_HandleSilentFallback()
    {
        // Arrange
        var result = new TtsFallbackResult
        {
            UsedFallback = true,
            UsedVoice = "Silent Fallback",
            FallbackReason = "All voices failed"
        };

        // Act
        var message = TtsFallbackService.CreateDiagnosticMessage(result);

        // Assert
        Assert.Contains("Fix it", message);
    }

    private static IEnumerable<ScriptLine> CreateTestScriptLines(int count)
    {
        var lines = new List<ScriptLine>();
        for (int i = 0; i < count; i++)
        {
            lines.Add(new ScriptLine(
                SceneIndex: i,
                Text: $"Test line {i}",
                Start: TimeSpan.FromSeconds(i * 2),
                Duration: TimeSpan.FromSeconds(2)
            ));
        }
        return lines;
    }

    // Mock providers for testing
    private class MockSuccessfulTtsProvider : ITtsProvider
    {
        private readonly WavFileWriter _writer;

        public MockSuccessfulTtsProvider(WavFileWriter writer)
        {
            _writer = writer;
        }

        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "TestVoice" });
        }

        public async Task<string> SynthesizeAsync(
            IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"mock_success_{Guid.NewGuid()}.wav");
            var duration = lines.Sum(l => l.Duration.TotalSeconds);
            await _writer.GenerateSilenceAsync(outputPath, duration, ct: ct);
            return outputPath;
        }
    }

    private class MockPartiallyFailingTtsProvider : ITtsProvider
    {
        private readonly WavFileWriter _writer;

        public MockPartiallyFailingTtsProvider(WavFileWriter writer)
        {
            _writer = writer;
        }

        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(
                new List<string> { "FailingVoice", "AlternateVoice" });
        }

        public async Task<string> SynthesizeAsync(
            IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            if (spec.VoiceName == "FailingVoice")
            {
                throw new InvalidOperationException("Voice not available");
            }

            var outputPath = Path.Combine(Path.GetTempPath(), $"mock_partial_{Guid.NewGuid()}.wav");
            var duration = lines.Sum(l => l.Duration.TotalSeconds);
            await _writer.GenerateSilenceAsync(outputPath, duration, ct: ct);
            return outputPath;
        }
    }

    private class MockCompletelyFailingTtsProvider : ITtsProvider
    {
        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "Voice1", "Voice2" });
        }

        public Task<string> SynthesizeAsync(
            IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            throw new InvalidOperationException("All voices failing");
        }
    }

    private class MockZeroByteProvider : ITtsProvider
    {
        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "TestVoice" });
        }

        public Task<string> SynthesizeAsync(
            IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"mock_zero_{Guid.NewGuid()}.wav");
            File.WriteAllText(outputPath, ""); // Create zero-byte file
            return Task.FromResult(outputPath);
        }
    }
}
