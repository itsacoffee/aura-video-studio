using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Piper TTS provider - lightweight, offline, fast CLI-based TTS
/// https://github.com/rhasspy/piper
/// Uses atomic file operations and validation for reliability.
/// </summary>
public class PiperTtsProvider : ITtsProvider
{
    private readonly ILogger<PiperTtsProvider> _logger;
    private readonly SilentWavGenerator _silentWavGenerator;
    private readonly WavValidator _wavValidator;
    private readonly string _piperExecutable;
    private readonly string _voiceModelPath;
    private readonly string _outputDirectory;
    private readonly ProcessRegistry? _processRegistry;
    private readonly ManagedProcessRunner? _processRunner;

    public PiperTtsProvider(
        ILogger<PiperTtsProvider> logger,
        SilentWavGenerator silentWavGenerator,
        WavValidator wavValidator,
        string piperExecutable,
        string voiceModelPath,
        ProcessRegistry? processRegistry = null,
        ManagedProcessRunner? processRunner = null)
    {
        _logger = logger;
        _silentWavGenerator = silentWavGenerator;
        _wavValidator = wavValidator;
        _piperExecutable = piperExecutable;
        _voiceModelPath = voiceModelPath;
        _processRegistry = processRegistry;
        _processRunner = processRunner;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS", "Piper");

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        // Validate dependencies on initialization
        if (!File.Exists(_piperExecutable))
        {
            _logger.LogWarning("Piper executable not found at {Path}. Provider will not be functional. Please install Piper and configure the path in settings.", _piperExecutable);
        }
        else if (!File.Exists(_voiceModelPath))
        {
            _logger.LogWarning("Voice model file not found at {Path}. Provider will not be functional. Please download a Piper voice model and configure the path in settings.", _voiceModelPath);
        }
        else
        {
            _logger.LogInformation("Piper TTS provider initialized with executable at {Executable} and voice model at {Model}", 
                _piperExecutable, _voiceModelPath);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        
        // In a full implementation, this would scan the voice models directory
        // For now, return the configured voice
        var voiceName = Path.GetFileNameWithoutExtension(_voiceModelPath);
        return new List<string> { voiceName };
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Synthesizing speech with Piper TTS using voice model {Model}", _voiceModelPath);

        // Health check before synthesis
        if (!await IsHealthyAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "Piper TTS provider is not healthy. " +
                "Please verify that the Piper executable and voice model are correctly configured.");
        }

        var linesList = lines.ToList();
        var segmentPaths = new List<string>();

        try
        {
            // Synthesize each line separately
            for (int i = 0; i < linesList.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var line = linesList[i];
                var outputPath = Path.Combine(_outputDirectory, $"segment_{i:D4}.wav");

                _logger.LogDebug("Synthesizing line {Index}: {Text}", i, line.Text);

                // Run Piper CLI: echo "text" | piper --model voice.onnx --output_file output.wav
                var success = await RunPiperAsync(line.Text, outputPath, ct).ConfigureAwait(false);

                if (!success)
                {
                    _logger.LogWarning("Failed to synthesize line {Index}, creating silence", i);
                    await _silentWavGenerator.GenerateAsync(outputPath, line.Duration, sampleRate: 22050, ct: ct).ConfigureAwait(false);
                }

                segmentPaths.Add(outputPath);
            }

            // Merge all segments into final narration file
            var finalPath = Path.Combine(_outputDirectory, $"narration_{DateTime.Now:yyyyMMddHHmmss}.wav");
            _logger.LogInformation("Merging {Count} segments into {Path}", segmentPaths.Count, finalPath);

            MergeWavFiles(segmentPaths, finalPath);

            // Validate the merged file
            var validationResult = await _wavValidator.ValidateAsync(finalPath, ct).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                _logger.LogError("Merged narration file failed validation: {Error}", validationResult.ErrorMessage);
                throw new InvalidOperationException($"Failed to create valid narration file: {validationResult.ErrorMessage}");
            }
            
            _logger.LogInformation("Narration validation passed: {Path}", finalPath);

            // Clean up segment files
            foreach (var segment in segmentPaths)
            {
                try
                {
                    if (File.Exists(segment))
                    {
                        File.Delete(segment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary segment {Path}", segment);
                }
            }

            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Piper TTS synthesis");
            
            // Clean up on error
            foreach (var segment in segmentPaths)
            {
                try
                {
                    if (File.Exists(segment))
                    {
                        File.Delete(segment);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Check if Piper TTS provider is healthy (executable and model exist)
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        if (!File.Exists(_piperExecutable))
        {
            _logger.LogWarning("Piper executable not found at {Path}", _piperExecutable);
            return false;
        }

        if (!File.Exists(_voiceModelPath))
        {
            _logger.LogWarning("Piper voice model not found at {Path}", _voiceModelPath);
            return false;
        }

        return true;
    }

    private async Task<bool> RunPiperAsync(string text, string outputPath, CancellationToken ct)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _piperExecutable,
                Arguments = $"--model \"{_voiceModelPath}\" --output_file \"{outputPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Use ManagedProcessRunner if available, otherwise fall back to manual process management
            if (_processRunner != null)
            {
                return await RunPiperWithManagedRunnerAsync(processStartInfo, text, outputPath, ct).ConfigureAwait(false);
            }

            // Fallback to manual process management
            return await RunPiperManuallyAsync(processStartInfo, text, outputPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Piper process");
            return false;
        }
    }

    /// <summary>
    /// Run Piper using ManagedProcessRunner (preferred method)
    /// Note: ManagedProcessRunner doesn't support stdin, so we use manual process management
    /// but with improved timeout handling (60s instead of 5 minutes)
    /// </summary>
    private async Task<bool> RunPiperWithManagedRunnerAsync(
        ProcessStartInfo startInfo,
        string text,
        string outputPath,
        CancellationToken ct)
    {
        // Since ManagedProcessRunner doesn't support stdin, we fall back to manual management
        // but use the improved timeout (60s) pattern
        return await RunPiperManuallyAsync(startInfo, text, outputPath, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Run Piper manually (fallback method)
    /// </summary>
    private async Task<bool> RunPiperManuallyAsync(
        ProcessStartInfo startInfo,
        string text,
        string outputPath,
        CancellationToken ct)
    {
        using var process = new Process { StartInfo = startInfo };
        
        process.Start();

        // Register with process registry for tracking if available
        if (_processRegistry != null)
        {
            _processRegistry.Register(process);
        }

        // Write text to stdin
        await process.StandardInput.WriteLineAsync(text).ConfigureAwait(false);
        process.StandardInput.Close();

        // Wait for completion with cancellation support and timeout (60s per synthesis call)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Piper process timed out after 60 seconds");
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            return false;
        }

        if (process.ExitCode == 0 && File.Exists(outputPath))
        {
            return true;
        }

        var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        _logger.LogWarning("Piper process failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
        return false;
    }



    private void MergeWavFiles(List<string> inputFiles, string outputFile)
    {
        if (inputFiles.Count == 0)
        {
            throw new ArgumentException("No input files to merge");
        }

        // Validate all input files exist and have minimum size
        const int MinWavFileSize = 44; // WAV header size
        foreach (var file in inputFiles)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Input file not found: {file}");
            }
            
            var info = new FileInfo(file);
            if (info.Length < MinWavFileSize)
            {
                throw new InvalidDataException(
                    $"Input file is too small to be a valid WAV file: {file} ({info.Length} bytes, minimum {MinWavFileSize} bytes)");
            }
        }

        if (inputFiles.Count == 1)
        {
            // Use atomic copy for single file
            string tempPath = outputFile + ".tmp";
            try
            {
                File.Copy(inputFiles[0], tempPath, overwrite: true);
                File.Move(tempPath, outputFile, overwrite: true);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw;
            }
            return;
        }

        // Simple concatenation for WAV files with same format
        string tempPath2 = outputFile + ".tmp";
        try
        {
            // Read first file to get header
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

            // Write output file to temp location
            using (var output = new FileStream(tempPath2, FileMode.Create, FileAccess.Write))
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
            File.Move(tempPath2, outputFile, overwrite: true);
            
            // Validate output file was created successfully
            var outputInfo = new FileInfo(outputFile);
            if (!outputInfo.Exists || outputInfo.Length < MinWavFileSize)
            {
                throw new InvalidOperationException(
                    $"Failed to create valid merged output file: {outputFile} " +
                    $"(exists: {outputInfo.Exists}, size: {outputInfo.Length} bytes)");
            }
            
            _logger.LogInformation("Successfully merged {Count} files into {Path} ({Size} bytes)", 
                inputFiles.Count, outputFile, outputInfo.Length);
        }
        catch
        {
            // Clean up temp file on error
            if (File.Exists(tempPath2))
            {
                try { File.Delete(tempPath2); } catch { }
            }
            throw;
        }
    }
}
