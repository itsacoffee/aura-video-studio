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

    public PiperTtsProvider(
        ILogger<PiperTtsProvider> logger,
        SilentWavGenerator silentWavGenerator,
        WavValidator wavValidator,
        string piperExecutable,
        string voiceModelPath)
    {
        _logger = logger;
        _silentWavGenerator = silentWavGenerator;
        _wavValidator = wavValidator;
        _piperExecutable = piperExecutable;
        _voiceModelPath = voiceModelPath;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS", "Piper");

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        await Task.CompletedTask;
        
        // In a full implementation, this would scan the voice models directory
        // For now, return the configured voice
        var voiceName = Path.GetFileNameWithoutExtension(_voiceModelPath);
        return new List<string> { voiceName };
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Synthesizing speech with Piper TTS using voice model {Model}", _voiceModelPath);

        if (!File.Exists(_piperExecutable))
        {
            throw new FileNotFoundException($"Piper executable not found at {_piperExecutable}");
        }

        if (!File.Exists(_voiceModelPath))
        {
            throw new FileNotFoundException($"Voice model not found at {_voiceModelPath}");
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
                var success = await RunPiperAsync(line.Text, outputPath, ct);

                if (!success)
                {
                    _logger.LogWarning("Failed to synthesize line {Index}, creating silence", i);
                    await _silentWavGenerator.GenerateAsync(outputPath, line.Duration, sampleRate: 22050, ct: ct);
                }

                segmentPaths.Add(outputPath);
            }

            // Merge all segments into final narration file
            var finalPath = Path.Combine(_outputDirectory, $"narration_{DateTime.Now:yyyyMMddHHmmss}.wav");
            _logger.LogInformation("Merging {Count} segments into {Path}", segmentPaths.Count, finalPath);

            MergeWavFiles(segmentPaths, finalPath);

            // Validate the merged file
            var validationResult = await _wavValidator.ValidateAsync(finalPath, ct);
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

            using var process = new Process { StartInfo = processStartInfo };
            
            process.Start();

            // Write text to stdin
            await process.StandardInput.WriteLineAsync(text);
            process.StandardInput.Close();

            // Wait for completion with cancellation support
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0 && File.Exists(outputPath))
            {
                return true;
            }

            var error = await process.StandardError.ReadToEndAsync(ct);
            _logger.LogWarning("Piper process failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Piper process");
            return false;
        }
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
