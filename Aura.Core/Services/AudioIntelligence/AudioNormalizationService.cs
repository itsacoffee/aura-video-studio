using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for audio normalization and ducking using FFmpeg
/// Implements EBU R128 loudness normalization and intelligent ducking
/// </summary>
public class AudioNormalizationService
{
    private readonly ILogger<AudioNormalizationService> _logger;
    private readonly IFFmpegService _ffmpegService;

    public AudioNormalizationService(
        ILogger<AudioNormalizationService> logger,
        IFFmpegService ffmpegService)
    {
        _logger = logger;
        _ffmpegService = ffmpegService;
    }

    /// <summary>
    /// Normalize audio to target LUFS using EBU R128 standard
    /// </summary>
    public async Task<string> NormalizeToLUFSAsync(
        string inputPath,
        string outputPath,
        double targetLUFS = -14.0,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Normalizing audio {Input} to {LUFS} LUFS", inputPath, targetLUFS);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input audio file not found: {inputPath}");

        var filter = $"loudnorm=I={targetLUFS}:TP=-1.5:LRA=11:measured_I=-23:measured_LRA=7:measured_TP=-5:measured_thresh=-34:offset=0:linear=true";

        var arguments = $"-i \"{inputPath}\" -af \"{filter}\" -ar 48000 -ac 2 \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Audio normalization failed: {result.ErrorMessage ?? "Unknown error"}");
        }

        _logger.LogInformation("Audio normalized successfully to {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Apply ducking to music when narration plays
    /// </summary>
    public async Task<string> ApplyDuckingAsync(
        string musicPath,
        string narrationPath,
        string outputPath,
        DuckingSettings settings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying ducking to music with narration");

        if (!File.Exists(musicPath))
            throw new FileNotFoundException($"Music file not found: {musicPath}");

        if (!File.Exists(narrationPath))
            throw new FileNotFoundException($"Narration file not found: {narrationPath}");

        var attackMs = settings.AttackTime.TotalMilliseconds;
        var releaseMs = settings.ReleaseTime.TotalMilliseconds;
        var duckDepthLinear = Math.Pow(10, settings.DuckDepthDb / 20.0);

        var sideChainFilter = $"sidechaincompress=threshold={settings.Threshold}:ratio=20:attack={attackMs}:release={releaseMs}:makeup=1:knee=2.828427:link=average:detection=rms:level_sc={1 - duckDepthLinear}";

        var arguments = $"-i \"{musicPath}\" -i \"{narrationPath}\" -filter_complex \"[0:a][1:a]{sideChainFilter}[ducked]\" -map \"[ducked]\" -ac 2 -ar 48000 \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Ducking failed: {result.ErrorMessage ?? "Unknown error"}");
        }

        _logger.LogInformation("Ducking applied successfully to {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Mix multiple audio tracks with volume levels and ducking
    /// </summary>
    public async Task<string> MixAudioTracksAsync(
        List<AudioTrackInput> tracks,
        string outputPath,
        AudioMixing mixingSettings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Mixing {Count} audio tracks", tracks.Count);

        if (tracks.Count == 0)
            throw new ArgumentException("No audio tracks to mix");

        var filterComplex = BuildMixingFilterComplex(tracks, mixingSettings);
        var inputArgs = string.Join(" ", tracks.Select((t, i) => $"-i \"{t.FilePath}\""));

        var arguments = $"{inputArgs} -filter_complex \"{filterComplex}\" -map \"[final]\" -ac 2 -ar 48000 \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Audio mixing failed: {result.ErrorMessage ?? "Unknown error"}");
        }

        _logger.LogInformation("Audio mixed successfully to {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Measure loudness of an audio file (returns LUFS)
    /// </summary>
    public async Task<double> MeasureLoudnessAsync(
        string inputPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Measuring loudness of {Input}", inputPath);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input audio file not found: {inputPath}");

        var arguments = $"-i \"{inputPath}\" -af loudnorm=print_format=json -f null -";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var loudness = ParseLoudnessFromOutput(result.StandardError);

        _logger.LogInformation("Measured loudness: {LUFS} LUFS", loudness);
        return loudness;
    }

    /// <summary>
    /// Apply compression to audio for consistent dynamics
    /// </summary>
    public async Task<string> ApplyCompressionAsync(
        string inputPath,
        string outputPath,
        CompressionSettings settings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying compression to {Input}", inputPath);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input audio file not found: {inputPath}");

        var attackMs = settings.AttackTime.TotalMilliseconds;
        var releaseMs = settings.ReleaseTime.TotalMilliseconds;

        var filter = $"acompressor=threshold={settings.Threshold}dB:ratio={settings.Ratio}:attack={attackMs}:release={releaseMs}:makeup={settings.MakeupGain}dB:knee=2:mix=1:detection=rms:link=average";

        var arguments = $"-i \"{inputPath}\" -af \"{filter}\" -ar 48000 -ac 2 \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Compression failed: {result.ErrorMessage ?? "Unknown error"}");
        }

        _logger.LogInformation("Compression applied successfully to {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Apply EQ for voice clarity
    /// </summary>
    public async Task<string> ApplyVoiceEQAsync(
        string inputPath,
        string outputPath,
        EqualizationSettings settings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying voice EQ to {Input}", inputPath);

        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input audio file not found: {inputPath}");

        var filters = new List<string>
        {
            $"highpass=f={settings.HighPassFrequency}:poles=2"
        };

        if (Math.Abs(settings.PresenceBoost) > 0.1)
        {
            filters.Add($"equalizer=f=4000:width_type=h:width=2000:g={settings.PresenceBoost}");
        }

        if (Math.Abs(settings.DeEsserReduction) > 0.1)
        {
            filters.Add($"equalizer=f=7000:width_type=h:width=2000:g={settings.DeEsserReduction}");
        }

        var filterChain = string.Join(",", filters);
        var arguments = $"-i \"{inputPath}\" -af \"{filterChain}\" -ar 48000 -ac 2 \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"EQ processing failed: {result.ErrorMessage ?? "Unknown error"}");
        }

        _logger.LogInformation("Voice EQ applied successfully to {Output}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Complete audio processing pipeline with normalization, ducking, and EQ
    /// </summary>
    public async Task<string> ProcessCompleteAsync(
        AudioProcessingRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting complete audio processing for {JobId}", request.JobId);

        var tempDir = Path.Combine(Path.GetTempPath(), $"aura_audio_{request.JobId}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var processedTracks = new List<AudioTrackInput>();

            if (request.NarrationPath != null)
            {
                var narrationProcessed = Path.Combine(tempDir, "narration_processed.wav");
                await ApplyVoiceEQAsync(request.NarrationPath, narrationProcessed, request.MixingSettings.EQ, ct).ConfigureAwait(false);
                await ApplyCompressionAsync(narrationProcessed, narrationProcessed, request.MixingSettings.Compression, ct).ConfigureAwait(false);
                
                processedTracks.Add(new AudioTrackInput(narrationProcessed, request.MixingSettings.NarrationVolume));
            }

            if (request.MusicPath != null)
            {
                var musicProcessed = request.MusicPath;

                if (request.NarrationPath != null)
                {
                    musicProcessed = Path.Combine(tempDir, "music_ducked.wav");
                    await ApplyDuckingAsync(request.MusicPath, request.NarrationPath, musicProcessed, 
                        request.MixingSettings.Ducking, ct).ConfigureAwait(false);
                }

                processedTracks.Add(new AudioTrackInput(musicProcessed, request.MixingSettings.MusicVolume));
            }

            if (request.SfxPaths != null && request.SfxPaths.Count > 0)
            {
                foreach (var (sfxPath, index) in request.SfxPaths.Select((p, i) => (p, i)))
                {
                    processedTracks.Add(new AudioTrackInput(sfxPath, request.MixingSettings.SoundEffectsVolume));
                }
            }

            var mixedPath = Path.Combine(tempDir, "mixed.wav");
            await MixAudioTracksAsync(processedTracks, mixedPath, request.MixingSettings, ct).ConfigureAwait(false);

            if (request.MixingSettings.Normalize)
            {
                await NormalizeToLUFSAsync(mixedPath, request.OutputPath, request.MixingSettings.TargetLUFS, ct).ConfigureAwait(false);
            }
            else
            {
                File.Copy(mixedPath, request.OutputPath, overwrite: true);
            }

            _logger.LogInformation("Complete audio processing finished: {Output}", request.OutputPath);
            return request.OutputPath;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp directory: {TempDir}", tempDir);
            }
        }
    }

    private string BuildMixingFilterComplex(List<AudioTrackInput> tracks, AudioMixing mixingSettings)
    {
        var inputs = new List<string>();

        for (int i = 0; i < tracks.Count; i++)
        {
            var volumeLinear = tracks[i].Volume / 100.0;
            inputs.Add($"[{i}:a]volume={volumeLinear}[a{i}]");
        }

        var mixInputs = string.Join("", Enumerable.Range(0, tracks.Count).Select(i => $"[a{i}]"));
        inputs.Add($"{mixInputs}amix=inputs={tracks.Count}:duration=longest:dropout_transition=2[final]");

        return string.Join(";", inputs);
    }

    private double ParseLoudnessFromOutput(string output)
    {
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("input_i") || line.Contains("I:"))
            {
                var parts = line.Split(new[] { ':', '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[1].Trim(), out var lufs))
                {
                    return lufs;
                }
            }
        }

        return -23.0;
    }
}

/// <summary>
/// Audio track input for mixing
/// </summary>
public record AudioTrackInput(
    string FilePath,
    double Volume
);

/// <summary>
/// Complete audio processing request
/// </summary>
public record AudioProcessingRequest(
    string JobId,
    string? NarrationPath,
    string? MusicPath,
    List<string>? SfxPaths,
    AudioMixing MixingSettings,
    string OutputPath
);
