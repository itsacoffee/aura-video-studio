using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Normalizes audio levels for consistent volume across TTS providers
/// Uses FFmpeg's loudnorm filter for EBU R128 loudness normalization
/// </summary>
public class AudioNormalizer
{
    private readonly ILogger<AudioNormalizer> _logger;
    private readonly string? _ffmpegPath;
    private readonly string _outputDirectory;

    // Default normalization targets (EBU R128 broadcast standard)
    private const double DefaultTargetLufs = -16.0; // Integrated loudness
    private const double DefaultTruePeak = -1.5;     // True peak level
    private const double DefaultLoudnessRange = 11.0; // Loudness range (LRA)

    public AudioNormalizer(ILogger<AudioNormalizer> logger, string? ffmpegPath = null)
    {
        _logger = logger;
        _ffmpegPath = ffmpegPath;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS", "Normalized");

        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    /// <summary>
    /// Normalizes audio file to target loudness level
    /// </summary>
    /// <param name="inputPath">Path to input audio file</param>
    /// <param name="outputPath">Path for normalized output (optional)</param>
    /// <param name="targetLufs">Target integrated loudness in LUFS (default: -16)</param>
    /// <param name="truePeak">True peak target in dBTP (default: -1.5)</param>
    /// <param name="loudnessRange">Target loudness range (default: 11)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to normalized audio file</returns>
    public async Task<string> NormalizeAsync(
        string inputPath,
        string? outputPath = null,
        double targetLufs = DefaultTargetLufs,
        double truePeak = DefaultTruePeak,
        double loudnessRange = DefaultLoudnessRange,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input audio file not found: {inputPath}");
        }

        // Determine output path
        outputPath ??= GenerateOutputPath(inputPath);

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        _logger.LogInformation(
            "[{CorrelationId}] Normalizing audio: {Input} -> {Output} (target: {Lufs} LUFS)",
            correlationId, Path.GetFileName(inputPath), Path.GetFileName(outputPath), targetLufs);

        // Get FFmpeg path
        var ffmpegExecutable = await GetFfmpegPathAsync(ct);

        // Two-pass normalization for optimal results
        var analysisResult = await AnalyzeAudioAsync(inputPath, ffmpegExecutable, correlationId, ct);

        if (analysisResult == null)
        {
            _logger.LogWarning("[{CorrelationId}] Analysis failed, using single-pass normalization", correlationId);
            await NormalizeSinglePassAsync(inputPath, outputPath, ffmpegExecutable, targetLufs, truePeak, loudnessRange, ct);
        }
        else
        {
            _logger.LogDebug(
                "[{CorrelationId}] Audio analysis: I={Integrated} LUFS, TP={TruePeak} dBTP, LRA={Lra}",
                correlationId, analysisResult.IntegratedLoudness, analysisResult.TruePeak, analysisResult.LoudnessRange);

            await NormalizeTwoPassAsync(
                inputPath,
                outputPath,
                ffmpegExecutable,
                analysisResult,
                targetLufs,
                truePeak,
                loudnessRange,
                ct);
        }

        // Validate output
        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException($"Normalization failed: output file not created at {outputPath}");
        }

        var outputInfo = new FileInfo(outputPath);
        if (outputInfo.Length < 128)
        {
            throw new InvalidOperationException($"Normalized file is too small ({outputInfo.Length} bytes)");
        }

        _logger.LogInformation(
            "[{CorrelationId}] Audio normalization completed: {Size} bytes",
            correlationId, outputInfo.Length);

        return outputPath;
    }

    /// <summary>
    /// Normalizes multiple audio files to consistent levels
    /// </summary>
    public async Task<string[]> NormalizeBatchAsync(
        string[] inputPaths,
        double targetLufs = DefaultTargetLufs,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation(
            "[{CorrelationId}] Batch normalizing {Count} audio files",
            correlationId, inputPaths.Length);

        var outputPaths = new string[inputPaths.Length];
        var tasks = new Task<string>[inputPaths.Length];

        for (int i = 0; i < inputPaths.Length; i++)
        {
            var index = i;
            tasks[i] = NormalizeAsync(inputPaths[index], null, targetLufs, ct: ct);
        }

        var results = await Task.WhenAll(tasks);
        
        _logger.LogInformation(
            "[{CorrelationId}] Batch normalization completed: {Count} files",
            correlationId, results.Length);

        return results;
    }

    /// <summary>
    /// Analyzes audio loudness characteristics (first pass)
    /// </summary>
    private async Task<AudioAnalysisResult?> AnalyzeAudioAsync(
        string inputPath,
        string ffmpegPath,
        string correlationId,
        CancellationToken ct)
    {
        try
        {
            var args = $"-i \"{inputPath}\" -af loudnorm=I=-16:TP=-1.5:LRA=11:print_format=json -f null -";

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
                return null;
            }

            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Parse loudnorm output from stderr
            return ParseLoudnormOutput(stderr);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{CorrelationId}] Audio analysis failed", correlationId);
            return null;
        }
    }

    /// <summary>
    /// Single-pass normalization (faster but less accurate)
    /// </summary>
    private async Task NormalizeSinglePassAsync(
        string inputPath,
        string outputPath,
        string ffmpegPath,
        double targetLufs,
        double truePeak,
        double loudnessRange,
        CancellationToken ct)
    {
        var args = $"-i \"{inputPath}\" " +
                   $"-af \"loudnorm=I={targetLufs}:TP={truePeak}:LRA={loudnessRange}\" " +
                   $"-ar 44100 -y \"{outputPath}\"";

        await RunFfmpegAsync(ffmpegPath, args, ct);
    }

    /// <summary>
    /// Two-pass normalization (slower but more accurate)
    /// </summary>
    private async Task NormalizeTwoPassAsync(
        string inputPath,
        string outputPath,
        string ffmpegPath,
        AudioAnalysisResult analysis,
        double targetLufs,
        double truePeak,
        double loudnessRange,
        CancellationToken ct)
    {
        var args = $"-i \"{inputPath}\" " +
                   $"-af \"loudnorm=I={targetLufs}:TP={truePeak}:LRA={loudnessRange}:" +
                   $"measured_I={analysis.IntegratedLoudness}:" +
                   $"measured_TP={analysis.TruePeak}:" +
                   $"measured_LRA={analysis.LoudnessRange}:" +
                   $"measured_thresh={analysis.Threshold}:" +
                   $"offset={analysis.TargetOffset}:" +
                   $"linear=true:print_format=summary\" " +
                   $"-ar 44100 -y \"{outputPath}\"";

        await RunFfmpegAsync(ffmpegPath, args, ct);
    }

    /// <summary>
    /// Runs FFmpeg process and waits for completion
    /// </summary>
    private async Task RunFfmpegAsync(string ffmpegPath, string args, CancellationToken ct)
    {
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

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"FFmpeg normalization failed with exit code {process.ExitCode}: {error}");
        }
    }

    /// <summary>
    /// Parses loudnorm filter output to extract audio measurements
    /// </summary>
    private AudioAnalysisResult? ParseLoudnormOutput(string output)
    {
        try
        {
            // Look for JSON output in stderr
            var jsonStart = output.IndexOf("{", StringComparison.Ordinal);
            var jsonEnd = output.LastIndexOf("}", StringComparison.Ordinal);

            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
            {
                return null;
            }

            var json = output.Substring(jsonStart, jsonEnd - jsonStart + 1);

            // Parse manually (simple JSON parsing to avoid dependencies)
            var result = new AudioAnalysisResult();

            result.IntegratedLoudness = ExtractJsonValue(json, "input_i");
            result.TruePeak = ExtractJsonValue(json, "input_tp");
            result.LoudnessRange = ExtractJsonValue(json, "input_lra");
            result.Threshold = ExtractJsonValue(json, "input_thresh");
            result.TargetOffset = ExtractJsonValue(json, "target_offset");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse loudnorm output");
            return null;
        }
    }

    /// <summary>
    /// Extracts numeric value from JSON string
    /// </summary>
    private double ExtractJsonValue(string json, string key)
    {
        var searchKey = $"\"{key}\" : \"";
        var startIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
        
        if (startIndex < 0)
        {
            return 0.0;
        }

        startIndex += searchKey.Length;
        var endIndex = json.IndexOf("\"", startIndex, StringComparison.Ordinal);

        if (endIndex < 0)
        {
            return 0.0;
        }

        var valueStr = json.Substring(startIndex, endIndex - startIndex);
        
        if (double.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return 0.0;
    }

    /// <summary>
    /// Gets FFmpeg executable path
    /// </summary>
    private async Task<string> GetFfmpegPathAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_ffmpegPath) && File.Exists(_ffmpegPath))
        {
            return _ffmpegPath;
        }

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
                return ffmpegPath;
            }
        }

        // Check common installation locations
        if (OperatingSystem.IsWindows())
        {
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "ffmpeg.exe")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        throw new FileNotFoundException(
            "FFmpeg not found. Please install FFmpeg or configure its path. " +
            "Download from: https://ffmpeg.org/download.html");
    }

    /// <summary>
    /// Generates output path for normalized file
    /// </summary>
    private string GenerateOutputPath(string inputPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8];

        return Path.Combine(_outputDirectory, $"{fileName}_normalized_{timestamp}_{guid}{extension}");
    }
}

/// <summary>
/// Audio analysis result from first pass
/// </summary>
internal class AudioAnalysisResult
{
    public double IntegratedLoudness { get; set; }
    public double TruePeak { get; set; }
    public double LoudnessRange { get; set; }
    public double Threshold { get; set; }
    public double TargetOffset { get; set; }
}
