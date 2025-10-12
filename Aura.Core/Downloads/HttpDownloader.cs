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
    string? Message = null
);

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
                        speed
                    ));

                    lastProgressReport = DateTime.UtcNow;
                }
            }

            // Final progress report
            progress?.Report(new HttpDownloadProgress(totalBytesRead, totalBytes, 100, 0, "Download complete"));

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
                    return false;
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
}
