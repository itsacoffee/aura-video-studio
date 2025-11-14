using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Helper class for TTS providers to perform atomic file operations with validation.
/// Implements write-to-temp then rename pattern and WAV validation.
/// </summary>
public class TtsFileHelper
{
    private readonly WavValidator _wavValidator;
    private readonly ILogger _logger;

    public TtsFileHelper(WavValidator wavValidator, ILogger logger)
    {
        _wavValidator = wavValidator;
        _logger = logger;
    }

    /// <summary>
    /// Writes WAV data atomically with validation.
    /// Writes to a temporary file, validates it, then renames to final path.
    /// </summary>
    /// <param name="finalPath">Final destination path for the WAV file</param>
    /// <param name="writeAction">Action that writes the WAV content to the provided stream</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Final path on success</returns>
    public async Task<string> WriteWavAtomicallyAsync(
        string finalPath,
        Func<FileStream, Task> writeAction,
        CancellationToken ct = default)
    {
        string tempPath = finalPath + ".tmp";

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logger.LogDebug("Writing WAV file to temporary path: {TempPath}", tempPath);

            // Write to temporary file with exclusive access
            await using (var fileStream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await writeAction(fileStream).ConfigureAwait(false);
                await fileStream.FlushAsync(ct).ConfigureAwait(false);
            }

            _logger.LogDebug("Validating WAV file: {TempPath}", tempPath);

            // Validate the WAV file
            var validationResult = await _wavValidator.ValidateAsync(tempPath, ct).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                _logger.LogError("WAV validation failed: {Error}", validationResult.ErrorMessage);
                throw new InvalidDataException($"Generated WAV file failed validation: {validationResult.ErrorMessage}");
            }

            _logger.LogInformation("WAV validation successful: {Format}, {SampleRate}Hz, {Duration}s",
                validationResult.Format, validationResult.SampleRate, validationResult.Duration);

            // Atomic rename - this is the commit point
            File.Move(tempPath, finalPath, overwrite: true);

            _logger.LogInformation("Successfully wrote WAV file: {Path}", finalPath);

            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write WAV file atomically");

            // Clean up temporary file on error
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    _logger.LogDebug("Cleaned up temporary file: {TempPath}", tempPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temporary file: {TempPath}", tempPath);
            }

            throw;
        }
    }

    /// <summary>
    /// Copies an existing file atomically with validation.
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="finalPath">Final destination path</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Final path on success</returns>
    public async Task<string> CopyWavAtomicallyAsync(
        string sourcePath,
        string finalPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        return await WriteWavAtomicallyAsync(finalPath, async stream =>
        {
            await using var sourceStream = File.OpenRead(sourcePath);
            await sourceStream.CopyToAsync(stream, 81920, ct).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates an existing WAV file without moving it.
    /// </summary>
    /// <param name="wavPath">Path to WAV file to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    public async Task<WavValidationResult> ValidateWavAsync(string wavPath, CancellationToken ct = default)
    {
        return await _wavValidator.ValidateAsync(wavPath, ct).ConfigureAwait(false);
    }
}
