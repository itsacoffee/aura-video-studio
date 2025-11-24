using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Result of TTS synthesis operation
/// </summary>
public record TtsResult(string AudioPath, int ChunkCount);

/// <summary>
/// Exception thrown when TTS synthesis fails
/// </summary>
public class TtsException : Exception
{
    public TtsException(string message) : base(message) { }
    public TtsException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Reliable TTS pipeline with chunking, validation, and provider fallback
/// </summary>
public class ReliableTtsPipeline
{
    private readonly IEnumerable<ITtsProvider> _providers;
    private readonly TtsChunker _chunker;
    private readonly AudioQualityValidator _validator;
    private readonly AudioConcatenator _concatenator;
    private readonly ManagedProcessRunner? _processRunner;
    private readonly ILogger<ReliableTtsPipeline> _logger;
    private readonly string _tempDirectory;

    public ReliableTtsPipeline(
        IEnumerable<ITtsProvider> providers,
        TtsChunker chunker,
        AudioQualityValidator validator,
        AudioConcatenator concatenator,
        ILogger<ReliableTtsPipeline> logger,
        ManagedProcessRunner? processRunner = null)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _chunker = chunker ?? throw new ArgumentNullException(nameof(chunker));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _concatenator = concatenator ?? throw new ArgumentNullException(nameof(concatenator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processRunner = processRunner;

        _tempDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS", "Chunks");
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    /// <summary>
    /// Synthesize speech with chunking, validation, and provider fallback
    /// </summary>
    public async Task<TtsResult> SynthesizeAsync(
        string text,
        VoiceSpec voiceSpec,
        string? jobId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        _logger.LogInformation("Starting reliable TTS synthesis for text length {Length} characters", text.Length);

        // Chunk the text
        var chunks = _chunker.ChunkText(text);
        _logger.LogInformation("Text chunked into {Count} segments", chunks.Count);

        var audioFiles = new List<string>();
        var providersList = _providers.ToList();

        if (providersList.Count == 0)
        {
            throw new InvalidOperationException("No TTS providers available");
        }

        // Process each chunk
        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();

            var success = false;
            var currentProviderIndex = 0;
            string? chunkAudioPath = null;

            // Try each provider until one succeeds
            while (!success && currentProviderIndex < providersList.Count)
            {
                var provider = providersList[currentProviderIndex];
                var providerName = provider.GetType().Name;

                try
                {
                    _logger.LogDebug("Attempting chunk {ChunkIndex} with provider {Provider}",
                        chunk.Index, providerName);

                    // Create a ScriptLine for this chunk
                    var scriptLine = new ScriptLine(
                        SceneIndex: chunk.Index,
                        Text: chunk.Text,
                        Start: TimeSpan.Zero,
                        Duration: TimeSpan.FromSeconds(1)); // Placeholder duration

                    chunkAudioPath = await SynthesizeChunkAsync(
                        provider,
                        scriptLine,
                        voiceSpec,
                        jobId,
                        ct).ConfigureAwait(false);

                    // Validate the generated audio
                    var validation = await _validator.ValidateAsync(chunkAudioPath, ct).ConfigureAwait(false);

                    if (validation.IsValid)
                    {
                        _logger.LogInformation("Chunk {ChunkIndex} synthesized and validated successfully with {Provider}",
                            chunk.Index, providerName);
                        audioFiles.Add(chunkAudioPath);
                        success = true;
                    }
                    else
                    {
                        _logger.LogWarning("Chunk {ChunkIndex} validation failed with {Provider}: {Issues}",
                            chunk.Index, providerName, string.Join("; ", validation.Issues));

                        // Clean up invalid audio file
                        try
                        {
                            if (File.Exists(chunkAudioPath))
                            {
                                File.Delete(chunkAudioPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete invalid chunk audio file");
                        }

                        chunkAudioPath = null;
                        currentProviderIndex++; // Try next provider
                    }
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(ex, "TTS provider {Provider} timed out for chunk {ChunkIndex}",
                        providerName, chunk.Index);

                    // Clean up on timeout
                    if (chunkAudioPath != null && File.Exists(chunkAudioPath))
                    {
                        try { File.Delete(chunkAudioPath); } catch { }
                    }

                    currentProviderIndex++;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("TTS synthesis cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TTS provider {Provider} failed for chunk {ChunkIndex}: {Message}",
                        providerName, chunk.Index, ex.Message);

                    // Clean up on error
                    if (chunkAudioPath != null && File.Exists(chunkAudioPath))
                    {
                        try { File.Delete(chunkAudioPath); } catch { }
                    }

                    currentProviderIndex++;
                }
            }

            if (!success)
            {
                var errorMessage = $"All TTS providers failed for chunk {chunk.Index}";
                _logger.LogError(errorMessage);
                throw new TtsException(errorMessage);
            }
        }

        // Concatenate all chunks
        if (audioFiles.Count == 0)
        {
            throw new TtsException("No audio chunks were successfully generated");
        }

        _logger.LogInformation("Concatenating {Count} audio chunks", audioFiles.Count);

        var finalPath = Path.Combine(
            Path.GetDirectoryName(audioFiles[0]) ?? _tempDirectory,
            $"narration_{DateTime.Now:yyyyMMddHHmmss}.wav");

        try
        {
            await _concatenator.ConcatenateAsync(audioFiles, finalPath, ct).ConfigureAwait(false);

            // Validate final concatenated audio
            var finalValidation = await _validator.ValidateAsync(finalPath, ct).ConfigureAwait(false);
            if (!finalValidation.IsValid)
            {
                _logger.LogWarning("Final concatenated audio validation failed: {Issues}",
                    string.Join("; ", finalValidation.Issues));
                // Continue anyway - at least we have audio
            }

            _logger.LogInformation("TTS synthesis completed successfully: {Path} ({Chunks} chunks)",
                finalPath, chunks.Count);

            return new TtsResult(finalPath, chunks.Count);
        }
        finally
        {
            // Clean up chunk files after concatenation
            foreach (var chunkFile in audioFiles)
            {
                try
                {
                    if (File.Exists(chunkFile))
                    {
                        File.Delete(chunkFile);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete chunk file {Path}", chunkFile);
                }
            }
        }
    }

    /// <summary>
    /// Synthesize a single chunk using the specified provider
    /// </summary>
    private async Task<string> SynthesizeChunkAsync(
        ITtsProvider provider,
        ScriptLine scriptLine,
        VoiceSpec voiceSpec,
        string? jobId,
        CancellationToken ct)
    {
        var providerName = provider.GetType().Name;

        // Create timeout for chunk synthesis (30 seconds per chunk)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            // Call provider's SynthesizeAsync with single line
            var audioPath = await provider.SynthesizeAsync(
                new[] { scriptLine },
                voiceSpec,
                timeoutCts.Token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
            {
                throw new TtsException($"Provider {providerName} returned invalid audio path");
            }

            return audioPath;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"TTS provider {providerName} exceeded 30 second timeout");
        }
    }
}

/// <summary>
/// Simple audio concatenator for WAV files
/// </summary>
public class AudioConcatenator
{
    private readonly ILogger<AudioConcatenator> _logger;
    private readonly IFfmpegLocator? _ffmpegLocator;

    public AudioConcatenator(
        ILogger<AudioConcatenator> logger,
        IFfmpegLocator? ffmpegLocator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ffmpegLocator = ffmpegLocator;
    }

    /// <summary>
    /// Concatenate multiple audio files into one
    /// </summary>
    public async Task ConcatenateAsync(
        IEnumerable<string> audioFiles,
        string outputPath,
        CancellationToken ct)
    {
        var filesList = audioFiles.ToList();
        if (filesList.Count == 0)
        {
            throw new ArgumentException("No audio files provided", nameof(audioFiles));
        }

        if (filesList.Count == 1)
        {
            // Single file - just copy it
            File.Copy(filesList[0], outputPath, overwrite: true);
            _logger.LogDebug("Single audio file copied to {Path}", outputPath);
            return;
        }

        // Validate all files exist
        foreach (var file in filesList)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Audio file not found: {file}");
            }
        }

        // Try using FFmpeg if available (more reliable)
        if (_ffmpegLocator != null)
        {
            try
            {
                await ConcatenateWithFfmpegAsync(filesList, outputPath, ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FFmpeg concatenation failed, falling back to simple WAV merge");
            }
        }

        // Fallback: simple WAV file concatenation
        ConcatenateWavFiles(filesList, outputPath);
    }

    /// <summary>
    /// Concatenate using FFmpeg (preferred method)
    /// </summary>
    private async Task ConcatenateWithFfmpegAsync(
        List<string> inputFiles,
        string outputPath,
        CancellationToken ct)
    {
        var ffmpegPath = await _ffmpegLocator!.GetEffectiveFfmpegPathAsync(ct: ct).ConfigureAwait(false);

        // Create concat file list
        var concatListPath = Path.Combine(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(),
            $"concat_{Guid.NewGuid():N}.txt");

        try
        {
            // Write file list for ffmpeg concat demuxer
            var fileList = inputFiles.Select(f => $"file '{f.Replace("'", "'\\''")}'");
            await File.WriteAllLinesAsync(concatListPath, fileList, ct).ConfigureAwait(false);

            // Concatenate using ffmpeg
            var args = $"-f concat -safe 0 -i \"{concatListPath}\" -c copy -y \"{outputPath}\"";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
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

            _logger.LogDebug("Successfully concatenated {Count} audio files using FFmpeg", inputFiles.Count);
        }
        finally
        {
            // Clean up concat list file
            try
            {
                if (File.Exists(concatListPath))
                {
                    File.Delete(concatListPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete concat list file");
            }
        }
    }

    /// <summary>
    /// Simple WAV file concatenation (fallback)
    /// </summary>
    private void ConcatenateWavFiles(List<string> inputFiles, string outputPath)
    {
        // This is a simplified version - assumes all WAV files have the same format
        // For production, should use proper WAV header parsing and merging
        const int MinWavFileSize = 44; // WAV header size

        // Validate all files
        foreach (var file in inputFiles)
        {
            var info = new FileInfo(file);
            if (info.Length < MinWavFileSize)
            {
                throw new InvalidDataException($"Invalid WAV file: {file}");
            }
        }

        // Read first file header
        using var firstFile = new FileStream(inputFiles[0], FileMode.Open, FileAccess.Read);
        byte[] header = new byte[44];
        int headerBytesRead = firstFile.Read(header, 0, 44);
        if (headerBytesRead < 44)
        {
            throw new InvalidDataException($"Failed to read WAV header from {inputFiles[0]}");
        }

        // Calculate total data size
        long totalDataSize = 0;
        foreach (var file in inputFiles)
        {
            var info = new FileInfo(file);
            totalDataSize += info.Length - 44; // Subtract WAV header
        }

        // Write output file
        string tempPath = outputPath + ".tmp";
        try
        {
            using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(output))
            {
                // Write RIFF header with updated size
                writer.Write(header, 0, 4); // "RIFF"
                writer.Write((int)(totalDataSize + 36)); // File size - 8
                writer.Write(header, 8, 36); // Rest of header

                // Copy data from all input files
                byte[] buffer = new byte[8192];
                foreach (var file in inputFiles)
                {
                    using var input = new FileStream(file, FileMode.Open, FileAccess.Read);
                    input.Seek(44, SeekOrigin.Begin); // Skip header

                    int bytesRead;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }

            // Atomic rename
            File.Move(tempPath, outputPath, overwrite: true);
            _logger.LogDebug("Successfully concatenated {Count} WAV files", inputFiles.Count);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }
}

