using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VoiceEnhancement;

/// <summary>
/// Service for audio noise reduction and cleanup
/// </summary>
public class NoiseReductionService
{
    private readonly ILogger<NoiseReductionService> _logger;
    private readonly string _tempDirectory;

    public NoiseReductionService(ILogger<NoiseReductionService> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "NoiseReduction");

        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// Reduces background noise from audio file using FFmpeg filters
    /// </summary>
    public async Task<string> ReduceNoiseAsync(
        string inputPath,
        double strength = 0.7,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Reducing noise from: {InputPath} (strength: {Strength})", inputPath, strength);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"nr_{Guid.NewGuid()}.wav");
            var filterChain = BuildNoiseReductionFilter(strength);

            // Use FFmpeg for noise reduction
            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            
            var success = await RunFFmpegAsync(ffmpegArgs, ct);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Noise reduction failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Noise reduction completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during noise reduction");
            return inputPath;
        }
    }

    /// <summary>
    /// Builds FFmpeg filter chain for noise reduction
    /// </summary>
    private string BuildNoiseReductionFilter(double strength)
    {
        // Strength should be between 0.0 and 1.0
        strength = Math.Clamp(strength, 0.0, 1.0);

        var filters = new[]
        {
            // High-pass filter to remove low-frequency rumble
            "highpass=f=80",
            
            // Low-pass filter to remove high-frequency hiss (adjusted based on strength)
            $"lowpass=f={(int)(18000 - (strength * 6000))}",
            
            // Adaptive noise gate
            $"afftdn=nf={(-20 + (strength * 10))}",
            
            // De-click/de-pop filter
            "adeclick",
            
            // Slight compression to even out levels
            "acompressor=threshold=-20dB:ratio=2:attack=5:release=50"
        };

        return string.Join(",", filters);
    }

    /// <summary>
    /// Applies spectral noise gating (more aggressive)
    /// </summary>
    public async Task<string> ApplySpectralGateAsync(
        string inputPath,
        double threshold = -30.0,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying spectral gate to: {InputPath}", inputPath);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"sg_{Guid.NewGuid()}.wav");

            // Spectral noise gate - more aggressive than standard noise reduction
            var filterChain = $"afftdn=nt=w:om=o:tn=1:nf={threshold},adeclick,highpass=f=80";
            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";

            var success = await RunFFmpegAsync(ffmpegArgs, ct);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Spectral gate failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Spectral gate completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during spectral gating");
            return inputPath;
        }
    }

    /// <summary>
    /// Removes clicks and pops from audio
    /// </summary>
    public async Task<string> RemoveClicksAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Removing clicks from: {InputPath}", inputPath);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"dc_{Guid.NewGuid()}.wav");
            var filterChain = "adeclick,adeclip";
            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";

            var success = await RunFFmpegAsync(ffmpegArgs, ct);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Click removal failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Click removal completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing clicks");
            return inputPath;
        }
    }

    /// <summary>
    /// Runs FFmpeg with the specified arguments
    /// </summary>
    private async Task<bool> RunFFmpegAsync(string arguments, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -hide_banner -loglevel error {arguments}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("FFmpeg exited with code {ExitCode}: {Error}", 
                    process.ExitCode, errorOutput);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running FFmpeg");
            return false;
        }
    }

    /// <summary>
    /// Cleans up temporary files
    /// </summary>
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                var files = Directory.GetFiles(_tempDirectory);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {File}", file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }
}
