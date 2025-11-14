using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Audio validation result with diagnostics
/// </summary>
public class AudioValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public double? Duration { get; set; }
    public long? BitRate { get; set; }
    public string? Format { get; set; }
    public string? CodecName { get; set; }
    public bool IsCorrupted { get; set; }
    public string? DiagnosticOutput { get; set; }
}

/// <summary>
/// Validates and repairs audio files for rendering
/// </summary>
public class AudioValidator
{
    private readonly ILogger<AudioValidator> _logger;
    private readonly string? _ffmpegPath;
    private readonly string? _ffprobePath;

    public AudioValidator(ILogger<AudioValidator> logger, string? ffmpegPath = null, string? ffprobePath = null)
    {
        _logger = logger;
        _ffmpegPath = ffmpegPath;
        _ffprobePath = ffprobePath;
    }

    /// <summary>
    /// Validate an audio file for use in rendering
    /// </summary>
    /// <param name="audioPath">Path to audio file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with diagnostics</returns>
    public async Task<AudioValidationResult> ValidateAsync(string audioPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Validating audio file: {Path}", audioPath);

        // Check file exists
        if (!File.Exists(audioPath))
        {
            return new AudioValidationResult
            {
                IsValid = false,
                ErrorMessage = "File does not exist",
                IsCorrupted = false
            };
        }

        // Check file size (must be > 128 bytes)
        var fileInfo = new FileInfo(audioPath);
        if (fileInfo.Length <= 128)
        {
            _logger.LogWarning("Audio file too small (only {Size} bytes): {Path}", fileInfo.Length, audioPath);
            return new AudioValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File too small ({fileInfo.Length} bytes). Expected > 128 bytes.",
                IsCorrupted = true
            };
        }

        // If this is a WAV file, use specialized WAV validator first (only if we have validation tools)
        if (audioPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && 
            (!string.IsNullOrEmpty(_ffmpegPath) || !string.IsNullOrEmpty(_ffprobePath)))
        {
            var wavValidator = new WavValidator(_logger as ILogger<WavValidator> ?? 
                Microsoft.Extensions.Logging.Abstractions.NullLogger<WavValidator>.Instance);
            
            // Do a quick check first - if it doesn't even have valid headers, mark as corrupted
            if (await wavValidator.QuickValidateAsync(audioPath, ct).ConfigureAwait(false))
            {
                var wavResult = await wavValidator.ValidateAsync(audioPath, ct).ConfigureAwait(false);
                if (!wavResult.IsValid)
                {
                    return new AudioValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"WAV validation failed: {wavResult.ErrorMessage}",
                        IsCorrupted = true,
                        Duration = wavResult.Duration,
                        Format = wavResult.Format,
                        DiagnosticOutput = $"Sample Rate: {wavResult.SampleRate}Hz, Channels: {wavResult.Channels}, Bits: {wavResult.BitsPerSample}"
                    };
                }
                
                // WAV validation passed - still run ffprobe/ffmpeg for additional checks if available
                _logger.LogInformation("WAV header validation passed, performing additional checks");
            }
        }

        // Try ffprobe first if available (more detailed)
        if (!string.IsNullOrEmpty(_ffprobePath) && File.Exists(_ffprobePath))
        {
            return await ValidateWithFfprobeAsync(audioPath, ct).ConfigureAwait(false);
        }

        // Fall back to ffmpeg validation
        if (!string.IsNullOrEmpty(_ffmpegPath) && File.Exists(_ffmpegPath))
        {
            return await ValidateWithFfmpegAsync(audioPath, ct).ConfigureAwait(false);
        }

        // No validation tools available - basic check only
        _logger.LogWarning("No ffprobe/ffmpeg available for validation, performing basic file check only");
        return new AudioValidationResult
        {
            IsValid = true,
            ErrorMessage = "Warning: No ffprobe/ffmpeg available for deep validation"
        };
    }

    /// <summary>
    /// Validate using ffprobe (preferred method)
    /// </summary>
    private async Task<AudioValidationResult> ValidateWithFfprobeAsync(string audioPath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath!,
                Arguments = $"-v error -show_entries format=duration,bit_rate -of json \"{audioPath}\"",
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

            var stderrText = stderr.ToString();
            var stdoutText = stdout.ToString();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(stderrText))
            {
                _logger.LogWarning("ffprobe validation failed for {Path}: {Error}", audioPath, stderrText);
                return new AudioValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"ffprobe error: {stderrText}",
                    IsCorrupted = stderrText.Contains("Invalid data") || 
                                  stderrText.Contains("moov atom") ||
                                  stderrText.Contains("could not find codec"),
                    DiagnosticOutput = stderrText
                };
            }

            // Parse JSON output
            try
            {
                var json = JsonDocument.Parse(stdoutText);
                var format = json.RootElement.GetProperty("format");

                double? duration = null;
                long? bitRate = null;

                if (format.TryGetProperty("duration", out var durationElement))
                {
                    if (durationElement.TryGetDouble(out var dur))
                    {
                        duration = dur;
                    }
                }

                if (format.TryGetProperty("bit_rate", out var bitRateElement))
                {
                    if (bitRateElement.TryGetInt64(out var br))
                    {
                        bitRate = br;
                    }
                }

                _logger.LogInformation("Audio validation successful: duration={Duration}s, bitrate={BitRate}", 
                    duration, bitRate);

                return new AudioValidationResult
                {
                    IsValid = true,
                    Duration = duration,
                    BitRate = bitRate,
                    DiagnosticOutput = stdoutText
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ffprobe JSON output");
                // Treat as valid if we got no errors, just couldn't parse metadata
                return new AudioValidationResult
                {
                    IsValid = true,
                    ErrorMessage = "Metadata parsing failed but file appears valid"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during ffprobe validation");
            return new AudioValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation exception: {ex.Message}",
                IsCorrupted = false
            };
        }
    }

    /// <summary>
    /// Validate using ffmpeg (fallback method)
    /// </summary>
    private async Task<AudioValidationResult> ValidateWithFfmpegAsync(string audioPath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath!,
                Arguments = $"-hide_banner -v error -i \"{audioPath}\" -f null -",
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

            var stderrText = stderr.ToString();

            if (process.ExitCode != 0 || !string.IsNullOrEmpty(stderrText))
            {
                _logger.LogWarning("ffmpeg validation failed for {Path}: {Error}", audioPath, stderrText);
                return new AudioValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"ffmpeg error: {stderrText}",
                    IsCorrupted = stderrText.Contains("Invalid data") || 
                                  stderrText.Contains("moov atom") ||
                                  stderrText.Contains("could not find codec"),
                    DiagnosticOutput = stderrText
                };
            }

            _logger.LogInformation("Audio validation successful (ffmpeg null output test passed)");
            return new AudioValidationResult
            {
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during ffmpeg validation");
            return new AudioValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation exception: {ex.Message}",
                IsCorrupted = false
            };
        }
    }

    /// <summary>
    /// Re-encode a corrupted audio file to a clean WAV PCM format
    /// </summary>
    /// <param name="inputPath">Path to corrupted audio file</param>
    /// <param name="outputPath">Path for re-encoded output</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if re-encoding succeeded</returns>
    public async Task<(bool success, string? errorMessage)> ReencodeAsync(
        string inputPath, 
        string outputPath, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
        {
            return (false, "FFmpeg not available for re-encoding");
        }

        _logger.LogInformation("Attempting to re-encode audio: {Input} -> {Output}", inputPath, outputPath);

        try
        {
            // Conservative re-encode command: 48kHz stereo PCM 16-bit WAV
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-v error -y -i \"{inputPath}\" -ar 48000 -ac 2 -acodec pcm_s16le \"{outputPath}\"",
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

            var stderrText = stderr.ToString();

            if (process.ExitCode != 0)
            {
                _logger.LogError("Re-encoding failed with exit code {ExitCode}: {Error}", 
                    process.ExitCode, stderrText);
                return (false, $"Re-encoding failed: {stderrText}");
            }

            // Validate the re-encoded file
            var validation = await ValidateAsync(outputPath, ct).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                _logger.LogError("Re-encoded file failed validation: {Error}", validation.ErrorMessage);
                return (false, $"Re-encoded file invalid: {validation.ErrorMessage}");
            }

            _logger.LogInformation("Successfully re-encoded audio to: {Output}", outputPath);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during audio re-encoding");
            return (false, $"Re-encoding exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a silent WAV file of specified duration as fallback
    /// </summary>
    /// <param name="outputPath">Output path for silent WAV</param>
    /// <param name="durationSeconds">Duration in seconds</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if generation succeeded</returns>
    public async Task<(bool success, string? errorMessage)> GenerateSilentWavAsync(
        string outputPath,
        double durationSeconds,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_ffmpegPath) || !File.Exists(_ffmpegPath))
        {
            return (false, "FFmpeg not available for silent WAV generation");
        }

        _logger.LogInformation("Generating silent WAV: {Duration}s -> {Output}", durationSeconds, outputPath);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-f lavfi -i anullsrc=cl=stereo:r=48000 -t {durationSeconds:F2} -y \"{outputPath}\"",
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

            if (process.ExitCode != 0)
            {
                var stderrText = stderr.ToString();
                _logger.LogError("Silent WAV generation failed: {Error}", stderrText);
                return (false, $"Silent WAV generation failed: {stderrText}");
            }

            _logger.LogInformation("Successfully generated silent WAV: {Output}", outputPath);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during silent WAV generation");
            return (false, $"Silent WAV generation exception: {ex.Message}");
        }
    }
}
