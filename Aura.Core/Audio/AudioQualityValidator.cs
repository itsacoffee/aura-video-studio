using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Audio quality validation result
/// </summary>
public record AudioQualityValidationResult(
    bool IsValid,
    TimeSpan Duration,
    int SampleRate,
    double PeakAmplitude,
    List<string> Issues
);

/// <summary>
/// Validates audio quality including duration, sample rate, and audibility
/// </summary>
public class AudioQualityValidator
{
    private readonly ILogger<AudioQualityValidator> _logger;
    private readonly IFfmpegLocator? _ffmpegLocator;
    private readonly string? _ffprobePath;

    public AudioQualityValidator(
        ILogger<AudioQualityValidator> logger,
        IFfmpegLocator? ffmpegLocator = null,
        string? ffprobePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegLocator = ffmpegLocator;
        _ffprobePath = ffprobePath;
    }

    /// <summary>
    /// Validate audio file for quality metrics
    /// </summary>
    public async Task<AudioQualityValidationResult> ValidateAsync(string audioPath, CancellationToken ct = default)
    {
        var issues = new List<string>();

        // Basic file existence check
        if (!File.Exists(audioPath))
        {
            return new AudioQualityValidationResult(
                IsValid: false,
                Duration: TimeSpan.Zero,
                SampleRate: 0,
                PeakAmplitude: 0.0,
                Issues: new List<string> { "Audio file does not exist" });
        }

        // Get audio metadata using FFprobe
        var metadata = await GetAudioMetadataAsync(audioPath, ct).ConfigureAwait(false);

        if (metadata == null)
        {
            return new AudioQualityValidationResult(
                IsValid: false,
                Duration: TimeSpan.Zero,
                SampleRate: 0,
                PeakAmplitude: 0.0,
                Issues: new List<string> { "Failed to extract audio metadata" });
        }

        // Validate duration (must be at least 100ms)
        if (metadata.Duration < TimeSpan.FromMilliseconds(100))
        {
            issues.Add($"Audio too short: {metadata.Duration.TotalMilliseconds:F0}ms (minimum 100ms)");
        }

        // Validate sample rate (must be at least 16kHz for reasonable quality)
        if (metadata.SampleRate < 16000)
        {
            issues.Add($"Sample rate too low: {metadata.SampleRate}Hz (minimum 16000Hz)");
        }

        // Check for silence/corruption by analyzing peak amplitude
        var peakAmplitude = await AnalyzePeakAmplitudeAsync(audioPath, ct).ConfigureAwait(false);
        if (peakAmplitude < 0.01)
        {
            issues.Add($"Audio appears to be silent or corrupted (peak amplitude: {peakAmplitude:F4})");
        }

        var isValid = issues.Count == 0;

        if (!isValid)
        {
            _logger.LogWarning("Audio quality validation failed for {Path}: {Issues}",
                audioPath, string.Join("; ", issues));
        }
        else
        {
            _logger.LogDebug("Audio quality validation passed: duration={Duration}s, sampleRate={SampleRate}Hz, peak={Peak:F4}",
                metadata.Duration.TotalSeconds, metadata.SampleRate, peakAmplitude);
        }

        return new AudioQualityValidationResult(
            IsValid: isValid,
            Duration: metadata.Duration,
            SampleRate: metadata.SampleRate,
            PeakAmplitude: peakAmplitude,
            Issues: issues);
    }

    /// <summary>
    /// Get audio metadata using FFprobe
    /// </summary>
    private async Task<AudioMetadata?> GetAudioMetadataAsync(string audioPath, CancellationToken ct)
    {
        var ffprobePath = await GetFfprobePathAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(ffprobePath))
        {
            _logger.LogWarning("FFprobe not available for metadata extraction");
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = $"-v error -show_entries format=duration:stream=sample_rate -of json \"{audioPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var error = stderr.ToString();
                _logger.LogWarning("FFprobe failed for {Path}: {Error}", audioPath, error);
                return null;
            }

            // Parse JSON output
            var jsonText = stdout.ToString();
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                _logger.LogWarning("FFprobe returned empty output for {Path}", audioPath);
                return null;
            }

            using var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            TimeSpan duration = TimeSpan.Zero;
            int sampleRate = 0;

            // Extract duration from format
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var durationElement))
                {
                    if (durationElement.ValueKind == JsonValueKind.String)
                    {
                        if (double.TryParse(durationElement.GetString(), out var durationSeconds))
                        {
                            duration = TimeSpan.FromSeconds(durationSeconds);
                        }
                    }
                    else if (durationElement.ValueKind == JsonValueKind.Number)
                    {
                        duration = TimeSpan.FromSeconds(durationElement.GetDouble());
                    }
                }
            }

            // Extract sample rate from stream
            if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (stream.TryGetProperty("codec_type", out var codecType) &&
                        codecType.GetString() == "audio")
                    {
                        if (stream.TryGetProperty("sample_rate", out var sampleRateElement))
                        {
                            if (sampleRateElement.ValueKind == JsonValueKind.String)
                            {
                                int.TryParse(sampleRateElement.GetString(), out sampleRate);
                            }
                            else if (sampleRateElement.ValueKind == JsonValueKind.Number)
                            {
                                sampleRate = sampleRateElement.GetInt32();
                            }
                        }
                        break; // Use first audio stream
                    }
                }
            }

            return new AudioMetadata(duration, sampleRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception extracting audio metadata for {Path}", audioPath);
            return null;
        }
    }

    /// <summary>
    /// Analyze peak amplitude to detect silence or corruption
    /// </summary>
    private async Task<double> AnalyzePeakAmplitudeAsync(string audioPath, CancellationToken ct)
    {
        var ffmpegPath = await GetFfmpegPathAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            _logger.LogWarning("FFmpeg not available for peak amplitude analysis");
            return 0.0;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{audioPath}\" -af \"volumedetect\" -f null -",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            var stderr = new StringBuilder();

            process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            // FFmpeg outputs volume detection info to stderr
            var output = stderr.ToString();

            // Parse peak amplitude from output like: "mean_volume: -20.1 dB" or "max_volume: -5.2 dB"
            // Look for max_volume line
            var maxVolumeMatch = System.Text.RegularExpressions.Regex.Match(
                output,
                @"max_volume:\s*([-\d.]+)\s*dB");

            if (maxVolumeMatch.Success)
            {
                if (double.TryParse(maxVolumeMatch.Groups[1].Value, out var maxVolumeDb))
                {
                    // Convert dB to linear amplitude (0-1 range)
                    // dB = 20 * log10(amplitude), so amplitude = 10^(dB/20)
                    var amplitude = Math.Pow(10, maxVolumeDb / 20.0);
                    return Math.Min(1.0, Math.Max(0.0, amplitude));
                }
            }

            // Fallback: if we can't parse, assume it's valid if FFmpeg didn't error
            if (process.ExitCode == 0)
            {
                _logger.LogDebug("Could not parse peak amplitude from FFmpeg output, assuming valid");
                return 0.5; // Assume moderate amplitude
            }

            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception analyzing peak amplitude for {Path}", audioPath);
            return 0.0;
        }
    }

    /// <summary>
    /// Get FFprobe path
    /// </summary>
    private async Task<string?> GetFfprobePathAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_ffprobePath) && File.Exists(_ffprobePath))
        {
            return _ffprobePath;
        }

        if (_ffmpegLocator != null)
        {
            try
            {
                var ffmpegPath = await _ffmpegLocator.GetEffectiveFfmpegPathAsync(ct: ct).ConfigureAwait(false);
                var ffprobePath = Path.Combine(Path.GetDirectoryName(ffmpegPath) ?? string.Empty, "ffprobe");
                if (File.Exists(ffprobePath))
                {
                    return ffprobePath;
                }

                // Try with .exe extension on Windows
                var ffprobeExe = ffprobePath + ".exe";
                if (File.Exists(ffprobeExe))
                {
                    return ffprobeExe;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not locate FFprobe via FFmpeg locator");
            }
        }

        return null;
    }

    /// <summary>
    /// Get FFmpeg path
    /// </summary>
    private async Task<string?> GetFfmpegPathAsync(CancellationToken ct)
    {
        if (_ffmpegLocator != null)
        {
            try
            {
                return await _ffmpegLocator.GetEffectiveFfmpegPathAsync(ct: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not locate FFmpeg");
            }
        }

        return null;
    }

    /// <summary>
    /// Audio metadata extracted from file
    /// </summary>
    private record AudioMetadata(TimeSpan Duration, int SampleRate);
}

