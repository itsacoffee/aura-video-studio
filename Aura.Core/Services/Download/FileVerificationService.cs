using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Download;

/// <summary>
/// Service for verifying file integrity using checksums
/// </summary>
public class FileVerificationService
{
    private readonly ILogger<FileVerificationService> _logger;

    public FileVerificationService(ILogger<FileVerificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Verify a file's SHA-256 checksum
    /// </summary>
    /// <param name="filePath">Path to the file to verify</param>
    /// <param name="expectedSha256">Expected SHA-256 hash (case-insensitive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Verification result</returns>
    public async Task<FileVerificationResult> VerifyFileAsync(
        string filePath,
        string expectedSha256,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            throw new ArgumentException("Expected SHA-256 cannot be null or empty", nameof(expectedSha256));
        }

        if (!File.Exists(filePath))
        {
            _logger.LogError("File not found: {FilePath}", filePath);
            return new FileVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"File not found: {filePath}"
            };
        }

        _logger.LogInformation("Verifying file: {FilePath}", filePath);

        try
        {
            var actualSha256 = await ComputeSha256Async(filePath, ct).ConfigureAwait(false);

            var isValid = string.Equals(
                actualSha256,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);

            if (isValid)
            {
                _logger.LogInformation("File verification successful: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning(
                    "File verification failed: {FilePath}. Expected: {Expected}, Actual: {Actual}",
                    filePath, expectedSha256, actualSha256);
            }

            return new FileVerificationResult
            {
                IsValid = isValid,
                ActualSha256 = actualSha256,
                ExpectedSha256 = expectedSha256,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying file: {FilePath}", filePath);
            return new FileVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Verification error: {ex.Message}",
                FilePath = filePath
            };
        }
    }

    /// <summary>
    /// Compute SHA-256 hash of a file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>SHA-256 hash as lowercase hex string</returns>
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        _logger.LogDebug("Computing SHA-256 for: {FilePath}", filePath);

        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        _logger.LogDebug("SHA-256 computed: {Hash}", hash);
        return hash;
    }

    /// <summary>
    /// Verify multiple files in a batch
    /// </summary>
    /// <param name="files">Dictionary of file paths to expected checksums</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dictionary of file paths to verification results</returns>
    public async Task<Dictionary<string, FileVerificationResult>> VerifyFilesAsync(
        Dictionary<string, string> files,
        CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
        {
            throw new ArgumentException("Files dictionary cannot be null or empty", nameof(files));
        }

        var results = new Dictionary<string, FileVerificationResult>();

        foreach (var (filePath, expectedSha256) in files)
        {
            var result = await VerifyFileAsync(filePath, expectedSha256, ct).ConfigureAwait(false);
            results[filePath] = result;
        }

        return results;
    }

    /// <summary>
    /// Verify a file with retry logic
    /// </summary>
    /// <param name="filePath">Path to the file to verify</param>
    /// <param name="expectedSha256">Expected SHA-256 hash</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Verification result</returns>
    public async Task<FileVerificationResult> VerifyFileWithRetryAsync(
        string filePath,
        string expectedSha256,
        int maxRetries = 3,
        CancellationToken ct = default)
    {
        if (maxRetries < 1)
        {
            throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));
        }

        FileVerificationResult? lastResult = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            _logger.LogDebug("Verification attempt {Attempt} of {MaxRetries}", attempt, maxRetries);

            lastResult = await VerifyFileAsync(filePath, expectedSha256, ct).ConfigureAwait(false);

            if (lastResult.IsValid)
            {
                return lastResult;
            }

            if (attempt < maxRetries)
            {
                var delayMs = (int)Math.Pow(2, attempt) * 100; // Exponential backoff
                _logger.LogDebug("Verification failed, retrying in {Delay}ms", delayMs);
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
        }

        _logger.LogWarning("File verification failed after {MaxRetries} attempts: {FilePath}",
            maxRetries, filePath);

        return lastResult ?? new FileVerificationResult
        {
            IsValid = false,
            ErrorMessage = "Verification failed",
            FilePath = filePath
        };
    }
}

/// <summary>
/// Result of a file verification operation
/// </summary>
public class FileVerificationResult
{
    /// <summary>
    /// Whether the file passed verification
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Actual SHA-256 hash computed from the file
    /// </summary>
    public string? ActualSha256 { get; set; }

    /// <summary>
    /// Expected SHA-256 hash
    /// </summary>
    public string? ExpectedSha256 { get; set; }

    /// <summary>
    /// Path to the verified file
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Error message if verification failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
