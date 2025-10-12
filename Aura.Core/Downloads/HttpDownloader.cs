using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Downloads;

public record HttpDownloadProgress(
    long BytesDownloaded,
    long TotalBytes,
    float PercentComplete,
    double SpeedBytesPerSecond,
    string? Message = null,
    string? CurrentUrl = null,
    int? MirrorIndex = null
);

/// <summary>
/// Download error codes for better error handling
/// </summary>
public static class DownloadErrorCodes
{
    public const string E_DL_404 = "E-DL-404";
    public const string E_DL_TIMEOUT = "E-DL-TIMEOUT";
    public const string E_DL_CHECKSUM = "E-DL-CHECKSUM";
    public const string E_DL_NETWORK = "E-DL-NETWORK";
    public const string E_DL_IO = "E-DL-IO";
}

public class DownloadException : Exception
{
    public string ErrorCode { get; }
    public string? Url { get; }

    public DownloadException(string errorCode, string message, string? url = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Url = url;
    }
}

/// <summary>
/// Robust HTTP downloader with resume support, retry logic, and checksum verification
/// </summary>
public class HttpDownloader
{
    private readonly ILogger<HttpDownloader> _logger;
    private readonly HttpClient _httpClient;
    private const int BufferSize = 8192;
    private const int MaxRetries = 3;

    public HttpDownloader(ILogger<HttpDownloader> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Download a file with resume support and optional checksum verification
    /// </summary>
    public async Task<bool> DownloadFileAsync(
        string url,
        string outputPath,
        string? expectedSha256 = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await DownloadFileAsync(new[] { url }, outputPath, expectedSha256, progress, ct);
    }

    /// <summary>
    /// Download a file with mirror fallback support, resume, and checksum verification
    /// </summary>
    public async Task<bool> DownloadFileAsync(
        string[] urls,
        string outputPath,
        string? expectedSha256 = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        Exception? lastException = null;

        for (int urlIndex = 0; urlIndex < urls.Length; urlIndex++)
        {
            string currentUrl = urls[urlIndex];
            _logger.LogInformation("Attempting download from URL {Index}/{Total}: {Url}", 
                urlIndex + 1, urls.Length, currentUrl);

            if (urlIndex > 0)
            {
                progress?.Report(new HttpDownloadProgress(0, 0, 0, 0, 
                    $"Trying mirror {urlIndex + 1}/{urls.Length}...", currentUrl, urlIndex));
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        int delaySeconds = (int)Math.Pow(2, attempt);
                        _logger.LogInformation("Retry attempt {Attempt} of {MaxRetries} after {Delay}s delay", 
                            attempt, MaxRetries, delaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                    }

                    var result = await DownloadFileInternalAsync(
                        currentUrl, outputPath, expectedSha256, progress, ct, urlIndex);
                    
                    _logger.LogInformation("Successfully downloaded from {Url}", currentUrl);
                    return result;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("URL not found (404): {Url}", currentUrl);
                    lastException = new DownloadException(
                        DownloadErrorCodes.E_DL_404, 
                        $"File not found at {currentUrl}", 
                        currentUrl, ex);
                    break; // Don't retry 404s, try next mirror
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Download timeout for {Url}, attempt {Attempt}", currentUrl, attempt + 1);
                    lastException = new DownloadException(
                        DownloadErrorCodes.E_DL_TIMEOUT, 
                        $"Download timeout for {currentUrl}", 
                        currentUrl, ex);
                    
                    if (attempt >= MaxRetries - 1) break; // Try next mirror after all retries
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
                {
                    _logger.LogWarning(ex, "Network error for {Url}, attempt {Attempt}, will retry", 
                        currentUrl, attempt + 1);
                    lastException = new DownloadException(
                        DownloadErrorCodes.E_DL_NETWORK, 
                        $"Network error: {ex.Message}", 
                        currentUrl, ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Download failed: {Url}", currentUrl);
                    lastException = ex;
                    
                    if (ex is DownloadException) throw;
                    break; // Try next mirror
                }
            }
        }

        // All mirrors failed
        if (lastException != null)
        {
            throw lastException;
        }

        return false;
    }

    private async Task<bool> DownloadFileInternalAsync(
        string url,
        string outputPath,
        string? expectedSha256,
        IProgress<HttpDownloadProgress>? progress,
        CancellationToken ct,
        int? mirrorIndex = null)
    {
        var partialPath = outputPath + ".partial";
        var startTime = DateTime.UtcNow;
        long totalBytesRead = 0;

        try
        {
            // Check if partial download exists
            long existingBytes = 0;
            if (File.Exists(partialPath))
            {
                existingBytes = new FileInfo(partialPath).Length;
                _logger.LogInformation("Found partial download: {Bytes} bytes", existingBytes);
                totalBytesRead = existingBytes;
            }

            // Make HTTP request with range header for resume
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingBytes > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = existingBytes + (response.Content.Headers.ContentLength ?? 0);
            _logger.LogInformation("Downloading {Url} ({TotalBytes} bytes)", url, totalBytes);

            // Download to partial file
            {
                // Open file for writing (append if resuming)
                await using var fileStream = new FileStream(
                    partialPath,
                    existingBytes > 0 ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    useAsync: true);

                await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
                
                var buffer = new byte[BufferSize];
                int bytesRead;
                var lastProgressReport = DateTime.UtcNow;

                while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    totalBytesRead += bytesRead;

                    // Report progress every 500ms
                    if ((DateTime.UtcNow - lastProgressReport).TotalMilliseconds >= 500)
                    {
                        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        var speed = elapsed > 0 ? totalBytesRead / elapsed : 0;
                        var percentComplete = totalBytes > 0 ? (float)(totalBytesRead * 100.0 / totalBytes) : 0;

                        progress?.Report(new HttpDownloadProgress(
                            totalBytesRead,
                            totalBytes,
                            percentComplete,
                            speed,
                            null,
                            url,
                            mirrorIndex
                        ));

                        lastProgressReport = DateTime.UtcNow;
                    }
                }

                // Ensure stream is flushed before closing
                await fileStream.FlushAsync(ct);
            } // File stream is now closed

            // Final progress report
            progress?.Report(new HttpDownloadProgress(totalBytesRead, totalBytes, 100, 0, "Download complete", url, mirrorIndex));

            // Move partial file to final location
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(partialPath, outputPath);

            _logger.LogInformation("Download complete: {OutputPath}", outputPath);

            // Verify checksum if provided
            if (!string.IsNullOrEmpty(expectedSha256))
            {
                _logger.LogInformation("Verifying checksum...");
                var actualSha256 = await ComputeSha256Async(outputPath, ct);
                
                if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Checksum mismatch! Expected: {Expected}, Actual: {Actual}", 
                        expectedSha256, actualSha256);
                    
                    // Delete the downloaded file on checksum failure
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                        _logger.LogInformation("Deleted file with mismatched checksum");
                    }
                    
                    throw new DownloadException(
                        DownloadErrorCodes.E_DL_CHECKSUM,
                        $"Checksum mismatch. Expected: {expectedSha256}, Actual: {actualSha256}",
                        url);
                }
                
                _logger.LogInformation("Checksum verified successfully");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed: {Url}", url);
            throw;
        }
    }

    private async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Import a local file and optionally verify its checksum
    /// </summary>
    public async Task<(bool success, string? actualSha256)> ImportLocalFileAsync(
        string localFilePath,
        string outputPath,
        string? expectedSha256 = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Importing local file: {LocalPath} -> {OutputPath}", localFilePath, outputPath);

        if (!File.Exists(localFilePath))
        {
            throw new FileNotFoundException($"Local file not found: {localFilePath}", localFilePath);
        }

        progress?.Report(new HttpDownloadProgress(0, 0, 0, 0, "Verifying local file..."));

        // Compute checksum of local file
        string actualSha256 = await ComputeSha256Async(localFilePath, ct);
        _logger.LogInformation("Local file SHA256: {Sha256}", actualSha256);

        // Verify checksum if provided
        if (!string.IsNullOrEmpty(expectedSha256))
        {
            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Checksum mismatch for local file. Expected: {Expected}, Actual: {Actual}",
                    expectedSha256, actualSha256);
                
                progress?.Report(new HttpDownloadProgress(0, 0, 0, 0, 
                    "⚠️ Checksum mismatch - proceed with caution"));
                
                // Return false but also return the actual checksum so caller can decide
                return (false, actualSha256);
            }
            
            _logger.LogInformation("Checksum verified successfully");
        }

        progress?.Report(new HttpDownloadProgress(0, 100, 50, 0, "Copying file..."));

        // Copy file to output path
        var fileInfo = new FileInfo(localFilePath);
        long totalBytes = fileInfo.Length;
        
        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Invalid output path"));

        // Copy file
        await using (var sourceStream = File.OpenRead(localFilePath))
        await using (var destStream = File.Create(outputPath))
        {
            var buffer = new byte[BufferSize];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, ct);
                totalRead += bytesRead;

                float percent = totalBytes > 0 ? (float)(totalRead * 100.0 / totalBytes) : 100;
                progress?.Report(new HttpDownloadProgress(totalRead, totalBytes, percent, 0, "Copying..."));
            }
        }

        progress?.Report(new HttpDownloadProgress(totalBytes, totalBytes, 100, 0, "Import complete"));
        _logger.LogInformation("Local file imported successfully");

        return (true, actualSha256);
    }
}
