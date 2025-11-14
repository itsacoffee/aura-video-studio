using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VoiceEnhancement;

/// <summary>
/// Service for adjusting voice prosody (pitch, rate, emphasis, rhythm)
/// </summary>
public class ProsodyAdjustmentService
{
    private readonly ILogger<ProsodyAdjustmentService> _logger;
    private readonly string _tempDirectory;

    public ProsodyAdjustmentService(ILogger<ProsodyAdjustmentService> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "Prosody");

        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// Adjusts prosody according to settings
    /// </summary>
    public async Task<string> AdjustProsodyAsync(
        string inputPath,
        ProsodySettings settings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Adjusting prosody for: {InputPath}", inputPath);

        try
        {
            string currentPath = inputPath;

            // Apply pitch shift if needed
            if (Math.Abs(settings.PitchShift) > 0.1)
            {
                currentPath = await AdjustPitchAsync(currentPath, settings.PitchShift, ct).ConfigureAwait(false);
            }

            // Apply tempo/rate change if needed
            if (Math.Abs(settings.RateMultiplier - 1.0) > 0.01)
            {
                currentPath = await AdjustTempoAsync(currentPath, settings.RateMultiplier, ct).ConfigureAwait(false);
            }

            // Apply volume adjustment if needed
            if (Math.Abs(settings.VolumeAdjustment) > 0.1)
            {
                currentPath = await AdjustVolumeAsync(currentPath, settings.VolumeAdjustment, ct).ConfigureAwait(false);
            }

            return currentPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting prosody");
            return inputPath;
        }
    }

    /// <summary>
    /// Adjusts pitch in semitones
    /// </summary>
    public async Task<string> AdjustPitchAsync(
        string inputPath,
        double semitones,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Adjusting pitch by {Semitones} semitones", semitones);

        try
        {
            // Clamp to reasonable range
            semitones = Math.Clamp(semitones, -12, 12);

            var outputPath = Path.Combine(_tempDirectory, $"pitch_{Guid.NewGuid()}.wav");
            
            // Use rubberband or asetrate+atempo for pitch shifting
            // rubberband is better quality but may not be available
            // Using asetrate + atempo as a fallback
            var cents = (int)(semitones * 100);
            var filterChain = $"asetrate=48000*2^({semitones}/12),aresample=48000";

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Pitch adjustment failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Pitch adjustment completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting pitch");
            return inputPath;
        }
    }

    /// <summary>
    /// Adjusts tempo/speed without changing pitch
    /// </summary>
    public async Task<string> AdjustTempoAsync(
        string inputPath,
        double multiplier,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Adjusting tempo by {Multiplier}x", multiplier);

        try
        {
            // Clamp to reasonable range
            multiplier = Math.Clamp(multiplier, 0.5, 2.0);

            var outputPath = Path.Combine(_tempDirectory, $"tempo_{Guid.NewGuid()}.wav");
            
            // Use atempo filter for tempo changes without pitch shift
            // atempo can only handle 0.5 to 2.0 range per filter
            var filterChain = BuildTempoFilterChain(multiplier);

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Tempo adjustment failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Tempo adjustment completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting tempo");
            return inputPath;
        }
    }

    /// <summary>
    /// Adjusts volume in dB
    /// </summary>
    public async Task<string> AdjustVolumeAsync(
        string inputPath,
        double dB,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Adjusting volume by {dB} dB", dB);

        try
        {
            // Clamp to reasonable range
            dB = Math.Clamp(dB, -20, 20);

            var outputPath = Path.Combine(_tempDirectory, $"vol_{Guid.NewGuid()}.wav");
            var filterChain = $"volume={dB}dB";

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Volume adjustment failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Volume adjustment completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting volume");
            return inputPath;
        }
    }

    /// <summary>
    /// Adds emphasis to speech (compression and EQ)
    /// </summary>
    public async Task<string> AddEmphasisAsync(
        string inputPath,
        double level,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Adding emphasis at level {Level}", level);

        try
        {
            level = Math.Clamp(level, 0.0, 1.0);

            var outputPath = Path.Combine(_tempDirectory, $"emph_{Guid.NewGuid()}.wav");
            
            // Emphasis: compression + presence boost
            var threshold = -20 + (level * 10);
            var ratio = 2 + (level * 4);
            var presenceBoost = level * 3;

            var filterChain = 
                $"acompressor=threshold={threshold}dB:ratio={ratio}:attack=5:release=50:makeup=3dB," +
                $"equalizer=f=3000:t=q:w=1.0:g={presenceBoost}";

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Emphasis failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Emphasis completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding emphasis");
            return inputPath;
        }
    }

    /// <summary>
    /// Builds tempo filter chain that can handle any multiplier
    /// </summary>
    private string BuildTempoFilterChain(double multiplier)
    {
        var filters = new System.Collections.Generic.List<string>();
        
        // atempo can only handle 0.5 to 2.0 per filter, so chain them if needed
        while (multiplier > 2.0)
        {
            filters.Add("atempo=2.0");
            multiplier /= 2.0;
        }
        
        while (multiplier < 0.5)
        {
            filters.Add("atempo=0.5");
            multiplier /= 0.5;
        }
        
        if (Math.Abs(multiplier - 1.0) > 0.01)
        {
            filters.Add($"atempo={multiplier:F3}");
        }

        return filters.Count > 0 ? string.Join(",", filters) : "anull";
    }

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

            var errorOutput = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

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
}
