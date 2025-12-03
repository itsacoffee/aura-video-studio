using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Synthesizes multi-voice audio from dialogue with different voices for each character.
/// </summary>
public class MultiVoiceSynthesizer : IMultiVoiceSynthesizer
{
    private readonly TtsProviderFactory _ttsFactory;
    private readonly ILogger<MultiVoiceSynthesizer> _logger;
    private readonly string _outputDirectory;

    public MultiVoiceSynthesizer(
        TtsProviderFactory ttsFactory,
        ILogger<MultiVoiceSynthesizer> logger)
    {
        _ttsFactory = ttsFactory ?? throw new ArgumentNullException(nameof(ttsFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "MultiVoice");
    }

    /// <inheritdoc />
    public async Task<string> SynthesizeMultiVoiceAsync(
        VoiceAssignment assignment,
        IProgress<SynthesisProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (assignment == null || assignment.VoicedLines.Count == 0)
        {
            throw new ArgumentException("Voice assignment must contain at least one line", nameof(assignment));
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var tempDir = Path.Combine(_outputDirectory, sessionId);
        Directory.CreateDirectory(tempDir);

        _logger.LogInformation(
            "Starting multi-voice synthesis for {LineCount} lines with {VoiceCount} voices",
            assignment.VoicedLines.Count,
            assignment.CharacterVoices.Count);

        var segments = new List<AudioSegment>();
        var total = assignment.VoicedLines.Count;

        try
        {
            for (int i = 0; i < assignment.VoicedLines.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var voicedLine = assignment.VoicedLines[i];
                var provider = GetProviderForVoice(voicedLine.AssignedVoice);

                var segmentPath = Path.Combine(tempDir, $"segment_{i:D4}.wav");

                _logger.LogDebug(
                    "Synthesizing segment {Index}/{Total}: {VoiceName}",
                    i + 1,
                    total,
                    voicedLine.AssignedVoice.Name);

                var scriptLine = new ScriptLine(
                    SceneIndex: i,
                    Text: voicedLine.Line.Text,
                    Start: TimeSpan.Zero,
                    Duration: TimeSpan.FromSeconds(5)); // Default duration estimate

                var audioPath = await provider.SynthesizeAsync(
                    new[] { scriptLine },
                    voicedLine.SynthesisSpec,
                    ct).ConfigureAwait(false);

                // Copy to segment file if different format
                if (audioPath != segmentPath)
                {
                    await ConvertToWavAsync(audioPath, segmentPath, ct).ConfigureAwait(false);
                }

                segments.Add(new AudioSegment(segmentPath, voicedLine.Line.Text));

                progress?.Report(new SynthesisProgress(
                    (i + 1.0) / total * 100,
                    $"Synthesized: {voicedLine.AssignedVoice.Name}"));
            }

            // Concatenate all segments
            var outputPath = Path.Combine(tempDir, "final_narration.wav");
            await ConcatenateSegmentsAsync(segments, outputPath, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Multi-voice synthesis complete: {OutputPath}",
                outputPath);

            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-voice synthesis failed");
            
            // Cleanup on failure
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup temporary directory");
            }

            throw;
        }
    }

    private ITtsProvider GetProviderForVoice(VoiceDescriptor voice)
    {
        // Map VoiceProvider enum to provider name
        var providerName = voice.Provider switch
        {
            VoiceProvider.ElevenLabs => "ElevenLabs",
            VoiceProvider.Azure => "Azure",
            VoiceProvider.PlayHT => "PlayHT",
            VoiceProvider.Piper => "Piper",
            VoiceProvider.Mimic3 => "Mimic3",
            VoiceProvider.WindowsSAPI => "Windows",
            _ => null
        };

        if (!string.IsNullOrEmpty(providerName))
        {
            var provider = _ttsFactory.TryCreateProvider(providerName);
            if (provider != null)
            {
                return provider;
            }
        }

        // Fallback to any available provider
        var providers = _ttsFactory.CreateAvailableProviders();
        if (providers.Count > 0)
        {
            return providers.Values.First();
        }

        throw new InvalidOperationException("No TTS providers available for voice synthesis");
    }

    private async Task ConvertToWavAsync(string inputPath, string outputPath, CancellationToken ct)
    {
        if (inputPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(inputPath, outputPath, true);
            return;
        }

        var ffmpegPath = await GetFfmpegPathAsync().ConfigureAwait(false);
        var args = $"-i \"{inputPath}\" -acodec pcm_s16le -ar 48000 -ac 2 -y \"{outputPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start FFmpeg process");
        }

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            _logger.LogError("FFmpeg conversion failed: {Error}", error);
            throw new InvalidOperationException($"Failed to convert audio file: {error}");
        }
    }

    private async Task ConcatenateSegmentsAsync(
        IReadOnlyList<AudioSegment> segments,
        string outputPath,
        CancellationToken ct)
    {
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("No segments to concatenate");
        }

        if (segments.Count == 1)
        {
            File.Copy(segments[0].Path, outputPath, true);
            return;
        }

        var ffmpegPath = await GetFfmpegPathAsync().ConfigureAwait(false);

        // Create concat file list
        var tempDir = Path.GetDirectoryName(outputPath) ?? Path.GetTempPath();
        var listFile = Path.Combine(tempDir, "segments.txt");
        var listContent = string.Join("\n", segments.Select(s => $"file '{s.Path}'"));
        await File.WriteAllTextAsync(listFile, listContent, ct).ConfigureAwait(false);

        try
        {
            var args = $"-f concat -safe 0 -i \"{listFile}\" -acodec pcm_s16le -ar 48000 -ac 2 -y \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start FFmpeg process");
            }

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
                _logger.LogError("FFmpeg concatenation failed: {Error}", error);
                throw new InvalidOperationException($"Failed to concatenate audio files: {error}");
            }

            _logger.LogDebug("Successfully concatenated {Count} audio segments", segments.Count);
        }
        finally
        {
            // Clean up list file
            if (File.Exists(listFile))
            {
                try { File.Delete(listFile); }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    private static Task<string> GetFfmpegPathAsync()
    {
        // Try to find ffmpeg in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();

        foreach (var path in paths)
        {
            var ffmpegPath = Path.Combine(path, "ffmpeg");
            if (OperatingSystem.IsWindows())
            {
                ffmpegPath += ".exe";
            }

            if (File.Exists(ffmpegPath))
            {
                return Task.FromResult(ffmpegPath);
            }
        }

        // Default fallback
        var defaultPath = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return Task.FromResult(defaultPath);
    }

    private record AudioSegment(string Path, string Text);
}
