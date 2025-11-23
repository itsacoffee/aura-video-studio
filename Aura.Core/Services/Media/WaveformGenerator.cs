using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Generates audio waveforms using FFmpeg for timeline visualization
/// </summary>
public class WaveformGenerator
{
    private readonly ILogger<WaveformGenerator> _logger;
    private readonly Dictionary<string, string> _waveformCache = new();
    private readonly ConcurrentDictionary<string, float[]> _dataCache = new();
    private readonly string _ffmpegPath;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _generateLock = new(3, 3);

    public WaveformGenerator(
        ILogger<WaveformGenerator> logger,
        string ffmpegPath = "ffmpeg",
        string? cacheDirectory = null)
    {
        _logger = logger;
        _ffmpegPath = ffmpegPath;
        _cacheDirectory = cacheDirectory ?? Path.Combine(Path.GetTempPath(), "aura-waveform-cache");
        Directory.CreateDirectory(_cacheDirectory);
        
        // Load persistent cache asynchronously (fire-and-forget with error handling)
        // This avoids blocking the constructor while still ensuring cache is loaded
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPersistentCacheAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load persistent cache");
            }
        });
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
        if (_waveformCache.TryGetValue(cacheKey, out string? cachedPath) && File.Exists(cachedPath))
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
        var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg failed to generate waveform: {Error}", errorOutput);
            throw new InvalidOperationException($"Failed to generate waveform: {errorOutput}");
        }

        // Cache the result
        _waveformCache[cacheKey] = outputPath;

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

        var fileHash = ComputeFileHash(audioFilePath);
        var cacheKey = $"{fileHash}:{targetSamples}";
        
        if (_dataCache.TryGetValue(cacheKey, out float[]? cachedData))
        {
            _logger.LogInformation("Returning cached waveform data for {AudioFile}", audioFilePath);
            return cachedData;
        }

        var persistentPath = GetPersistentCachePath(fileHash, targetSamples);
        if (File.Exists(persistentPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(persistentPath, cancellationToken).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<float[]>(json);
                if (data != null)
                {
                    _dataCache[cacheKey] = data;
                    _logger.LogInformation("Loaded waveform data from persistent cache for {AudioFile}", audioFilePath);
                    return data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading persistent cache for {AudioFile}", audioFilePath);
            }
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
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

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

            var downsampled = DownsampleAudio(samples.ToArray(), targetSamples);

            _dataCache[cacheKey] = downsampled;

            await SaveToPersistentCacheAsync(fileHash, targetSamples, downsampled).ConfigureAwait(false);

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
    /// Asynchronously generate waveform data with priority for visible range
    /// </summary>
    public async Task<float[]> GenerateWaveformDataAsyncWithPriority(
        string audioFilePath,
        int targetSamples,
        double startTime,
        double endTime,
        CancellationToken cancellationToken = default)
    {
        await _generateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await GenerateWaveformDataAsync(audioFilePath, targetSamples, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _generateLock.Release();
        }
    }

    /// <summary>
    /// Clears the waveform cache
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing waveform cache");
        _waveformCache.Clear();
        _dataCache.Clear();
    }

    /// <summary>
    /// Clear persistent cache
    /// </summary>
    public async Task ClearPersistentCacheAsync()
    {
        try
        {
            if (Directory.Exists(_cacheDirectory))
            {
                Directory.Delete(_cacheDirectory, true);
                Directory.CreateDirectory(_cacheDirectory);
            }
            _logger.LogInformation("Cleared persistent waveform cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing persistent cache");
        }
        
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        var fileInfo = new FileInfo(filePath);
        var hashInput = $"{filePath}:{fileInfo.Length}:{fileInfo.LastWriteTimeUtc}";
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToHexString(hashBytes);
    }

    private string GetPersistentCachePath(string fileHash, int targetSamples)
    {
        return Path.Combine(_cacheDirectory, $"{fileHash}_{targetSamples}.json");
    }

    private async Task SaveToPersistentCacheAsync(string fileHash, int targetSamples, float[] data)
    {
        try
        {
            var path = GetPersistentCachePath(fileHash, targetSamples);
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
            _logger.LogDebug("Saved waveform data to persistent cache: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving to persistent cache");
        }
    }

    private async Task LoadPersistentCacheAsync()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                return;
            }

            var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.json");
            _logger.LogInformation("Found {Count} waveform cache files", cacheFiles.Length);
            
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading persistent cache");
        }
    }
}
