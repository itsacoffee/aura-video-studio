using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VoiceEnhancement;

/// <summary>
/// Service for voice frequency equalization
/// </summary>
public class EqualizeService
{
    private readonly ILogger<EqualizeService> _logger;
    private readonly string _tempDirectory;

    public EqualizeService(ILogger<EqualizeService> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "Equalization");

        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// Applies equalization to voice audio
    /// </summary>
    public async Task<string> ApplyEqualizationAsync(
        string inputPath,
        EqualizationPreset preset,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying equalization to: {InputPath} (preset: {Preset})", 
            inputPath, preset);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"eq_{Guid.NewGuid()}.wav");
            var filterChain = BuildEqualizationFilter(preset);

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Equalization failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Equalization completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during equalization");
            return inputPath;
        }
    }

    /// <summary>
    /// Applies custom EQ settings
    /// </summary>
    public async Task<string> ApplyCustomEqualizationAsync(
        string inputPath,
        CustomEqSettings settings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying custom equalization to: {InputPath}", inputPath);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"eq_custom_{Guid.NewGuid()}.wav");
            var filterChain = BuildCustomEqFilter(settings);

            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";
            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("Custom equalization failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("Custom equalization completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during custom equalization");
            return inputPath;
        }
    }

    /// <summary>
    /// Builds FFmpeg equalization filter based on preset
    /// </summary>
    private string BuildEqualizationFilter(EqualizationPreset preset)
    {
        return preset switch
        {
            EqualizationPreset.Flat => "aformat=channel_layouts=stereo",
            
            EqualizationPreset.Balanced => 
                // Slight bass reduction, presence boost, gentle de-essing
                "equalizer=f=100:t=q:w=1.5:g=-1," +
                "equalizer=f=3000:t=q:w=1.0:g=2," +
                "equalizer=f=6000:t=q:w=2.0:g=-1",
            
            EqualizationPreset.Warm => 
                // Boost low-mids, reduce highs
                "equalizer=f=200:t=q:w=1.5:g=2," +
                "equalizer=f=800:t=q:w=1.0:g=3," +
                "equalizer=f=5000:t=q:w=2.0:g=-2",
            
            EqualizationPreset.Bright => 
                // Cut low frequencies, boost highs
                "equalizer=f=100:t=q:w=1.5:g=-3," +
                "equalizer=f=3000:t=q:w=1.0:g=2," +
                "equalizer=f=8000:t=q:w=2.0:g=3",
            
            EqualizationPreset.Broadcast => 
                // Radio/podcast optimized - presence boost, controlled bass
                "highpass=f=80," +
                "equalizer=f=150:t=q:w=1.0:g=-2," +
                "equalizer=f=2500:t=q:w=1.0:g=4," +
                "equalizer=f=4000:t=q:w=1.5:g=2," +
                "equalizer=f=7000:t=q:w=2.0:g=-2," +
                "lowpass=f=15000",
            
            EqualizationPreset.Telephone => 
                // Narrow bandwidth, emphasize speech frequencies
                "highpass=f=300," +
                "lowpass=f=3400," +
                "equalizer=f=1000:t=q:w=0.5:g=3," +
                "equalizer=f=2000:t=q:w=0.5:g=2",
            
            _ => "aformat=channel_layouts=stereo"
        };
    }

    /// <summary>
    /// Builds custom EQ filter from settings
    /// </summary>
    private string BuildCustomEqFilter(CustomEqSettings settings)
    {
        var filters = new System.Collections.Generic.List<string>();

        foreach (var band in settings.Bands)
        {
            // FFmpeg equalizer: f=frequency, t=type (q for Q factor), w=width/Q, g=gain
            filters.Add($"equalizer=f={band.Frequency}:t=q:w={band.Q}:g={band.Gain}");
        }

        return string.Join(",", filters);
    }

    /// <summary>
    /// Applies de-essing (reduces harsh sibilance)
    /// </summary>
    public async Task<string> ApplyDeEssingAsync(
        string inputPath,
        double intensity = 0.5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying de-essing to: {InputPath}", inputPath);

        try
        {
            var outputPath = Path.Combine(_tempDirectory, $"dees_{Guid.NewGuid()}.wav");
            
            // De-essing typically targets 6-8kHz range
            var centerFreq = 7000;
            var gain = -3 - (intensity * 4); // -3 to -7 dB based on intensity
            var width = 2.0;

            var filterChain = $"equalizer=f={centerFreq}:t=q:w={width}:g={gain}";
            var ffmpegArgs = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";

            var success = await RunFFmpegAsync(ffmpegArgs, ct).ConfigureAwait(false);

            if (!success || !File.Exists(outputPath))
            {
                _logger.LogWarning("De-essing failed, returning original file");
                return inputPath;
            }

            _logger.LogInformation("De-essing completed: {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during de-essing");
            return inputPath;
        }
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

/// <summary>
/// Custom EQ band settings
/// </summary>
public record EqBand
{
    public int Frequency { get; init; }
    public double Gain { get; init; }
    public double Q { get; init; } = 1.0;
}

/// <summary>
/// Custom EQ settings
/// </summary>
public record CustomEqSettings
{
    public EqBand[] Bands { get; init; } = Array.Empty<EqBand>();
}
