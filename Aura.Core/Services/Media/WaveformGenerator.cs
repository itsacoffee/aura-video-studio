using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Generates audio waveforms using FFmpeg for timeline visualization
/// </summary>
public class WaveformGenerator
{
    private readonly ILogger<WaveformGenerator> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _ffmpegPath;

    public WaveformGenerator(
        ILogger<WaveformGenerator> logger,
        IMemoryCache cache,
        string ffmpegPath = "ffmpeg")
    {
        _logger = logger;
        _cache = cache;
        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Generates a waveform PNG image with transparent background
    /// </summary>
    public async Task<string> GenerateWaveformAsync(
        string audioFilePath,
        int width,
        int height,
        string trackType = "narration",
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        // Check cache
        var cacheKey = $"waveform:{audioFilePath}:{width}:{height}:{trackType}";
        if (_cache.TryGetValue(cacheKey, out string? cachedPath) && File.Exists(cachedPath))
        {
            _logger.LogInformation("Returning cached waveform for {AudioFile}", audioFilePath);
            return cachedPath;
        }

        // Generate unique output filename
        var outputDir = Path.Combine(Path.GetTempPath(), "aura-waveforms");
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, $"{Guid.NewGuid()}.png");

        // Determine color based on track type
        var color = trackType.ToLowerInvariant() switch
        {
            "narration" => "0x4472C4",  // Blue
            "music" => "0x70AD47",      // Green
            "sfx" => "0xED7D31",        // Orange
            _ => "0x4472C4"             // Default blue
        };

        // Build FFmpeg command
        // Use showwavespic filter to generate waveform image
        var arguments = $"-i \"{audioFilePath}\" " +
                       $"-filter_complex \"[0:a]showwavespic=s={width}x{height}:colors={color}:scale=lin\" " +
                       $"-frames:v 1 " +
                       $"-y \"{outputPath}\"";

        _logger.LogInformation("Generating waveform: {Arguments}", arguments);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg failed to generate waveform: {Error}", errorOutput);
            throw new InvalidOperationException($"Failed to generate waveform: {errorOutput}");
        }

        // Cache the result
        _cache.Set(cacheKey, outputPath, TimeSpan.FromHours(24));

        _logger.LogInformation("Waveform generated successfully: {OutputPath}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Extracts raw audio sample data as JSON array for client-side rendering
    /// </summary>
    public async Task<float[]> GenerateWaveformDataAsync(
        string audioFilePath,
        int targetSamples,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        // Check cache
        var cacheKey = $"waveform-data:{audioFilePath}:{targetSamples}";
        if (_cache.TryGetValue(cacheKey, out float[]? cachedData))
        {
            _logger.LogInformation("Returning cached waveform data for {AudioFile}", audioFilePath);
            return cachedData;
        }

        // Extract audio samples using FFmpeg
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.raw");

        try
        {
            // Convert to raw PCM data
            var arguments = $"-i \"{audioFilePath}\" " +
                           $"-f f32le " +
                           $"-acodec pcm_f32le " +
                           $"-ac 1 " + // Mono by averaging channels
                           $"-ar 44100 " +
                           $"\"{tempFile}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to extract audio samples");
            }

            // Read raw samples
            var samples = new List<float>();
            using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                while (fs.Position < fs.Length)
                {
                    samples.Add(reader.ReadSingle());
                }
            }

            // Downsample to target resolution
            var downsampled = DownsampleAudio(samples.ToArray(), targetSamples);

            // Cache the result
            _cache.Set(cacheKey, downsampled, TimeSpan.FromHours(24));

            return downsampled;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* Ignore */ }
            }
        }
    }

    /// <summary>
    /// Downsamples audio data to target number of samples
    /// </summary>
    private static float[] DownsampleAudio(float[] samples, int targetSamples)
    {
        if (samples.Length <= targetSamples)
        {
            return samples;
        }

        var result = new float[targetSamples];
        var step = (float)samples.Length / targetSamples;

        for (int i = 0; i < targetSamples; i++)
        {
            var startIdx = (int)(i * step);
            var endIdx = Math.Min((int)((i + 1) * step), samples.Length);

            // Calculate RMS (root mean square) for this window
            float sum = 0;
            int count = endIdx - startIdx;
            for (int j = startIdx; j < endIdx; j++)
            {
                sum += samples[j] * samples[j];
            }

            result[i] = count > 0 ? (float)Math.Sqrt(sum / count) : 0;
        }

        return result;
    }

    /// <summary>
    /// Clears the waveform cache
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing waveform cache");
        // Note: IMemoryCache doesn't have a clear all method, 
        // but entries will expire based on their TTL
    }
}
