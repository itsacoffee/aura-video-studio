using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Download;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Download;

/// <summary>
/// Service for managing downloads with multi-mirror support, progress tracking, and verification
/// </summary>
public class DownloadService
{
    private readonly ILogger<DownloadService> _logger;
    private readonly HttpClient _httpClient;
    private readonly FileVerificationService _verificationService;
    private readonly Dictionary<string, DownloadMirror> _mirrors;
    private const int DefaultMaxRetries = 3;
    private const int MaxConsecutiveFailuresBeforeDisable = 5;

    public DownloadService(
        ILogger<DownloadService> logger,
        HttpClient httpClient,
        FileVerificationService verificationService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _verificationService = verificationService;
        _mirrors = new Dictionary<string, DownloadMirror>();
    }

    /// <summary>
    /// Register a download mirror
    /// </summary>
    public void RegisterMirror(DownloadMirror mirror)
    {
        if (mirror == null)
        {
            throw new ArgumentNullException(nameof(mirror));
        }

        if (string.IsNullOrWhiteSpace(mirror.Id))
        {
            throw new ArgumentException("Mirror ID cannot be null or empty", nameof(mirror));
        }

        _mirrors[mirror.Id] = mirror;
        _logger.LogInformation("Registered mirror: {MirrorId} ({MirrorName}) with priority {Priority}",
            mirror.Id, mirror.Name, mirror.Priority);
    }

    /// <summary>
    /// Register multiple mirrors
    /// </summary>
    public void RegisterMirrors(IEnumerable<DownloadMirror> mirrors)
    {
        if (mirrors == null)
        {
            throw new ArgumentNullException(nameof(mirrors));
        }

        foreach (var mirror in mirrors)
        {
            RegisterMirror(mirror);
        }
    }

    /// <summary>
    /// Get all registered mirrors
    /// </summary>
    public IReadOnlyList<DownloadMirror> GetMirrors()
    {
        return _mirrors.Values.ToList();
    }

    /// <summary>
    /// Check health of a specific mirror
    /// </summary>
    public async Task<bool> CheckMirrorHealthAsync(
        string mirrorId,
        CancellationToken ct = default)
    {
        if (!_mirrors.TryGetValue(mirrorId, out var mirror))
        {
            throw new ArgumentException($"Mirror not found: {mirrorId}", nameof(mirrorId));
        }

        return await CheckMirrorHealthInternalAsync(mirror, ct);
    }

    /// <summary>
    /// Check health of all registered mirrors
    /// </summary>
    public async Task CheckAllMirrorsHealthAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking health of all mirrors");

        var tasks = _mirrors.Values
            .Where(m => m.IsEnabled)
            .Select(m => CheckMirrorHealthInternalAsync(m, ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation("Mirror health check complete");
    }

    /// <summary>
    /// Download a file with multi-mirror support and progress tracking
    /// </summary>
    public async Task<DownloadResult> DownloadFileAsync(
        string outputPath,
        string? expectedSha256 = null,
        IProgress<DownloadProgressEventArgs>? progress = null,
        CancellationToken ct = default,
        int maxRetries = DefaultMaxRetries)
    {
        var downloadId = Guid.NewGuid().ToString("N");
        
        _logger.LogInformation("Starting download: {DownloadId} -> {OutputPath}", downloadId, outputPath);

        ReportProgress(progress, new DownloadProgressEventArgs
        {
            DownloadId = downloadId,
            Stage = DownloadStage.Initializing,
            Message = "Initializing download...",
            FilePath = outputPath
        });

        var result = new DownloadResult
        {
            DownloadId = downloadId,
            OutputPath = outputPath
        };

        try
        {
            // Select mirrors
            var mirrors = SelectMirrors();
            if (mirrors.Count == 0)
            {
                throw new InvalidOperationException("No mirrors available for download");
            }

            _logger.LogInformation("Selected {Count} mirrors for download", mirrors.Count);

            ReportProgress(progress, new DownloadProgressEventArgs
            {
                DownloadId = downloadId,
                Stage = DownloadStage.CheckingMirrors,
                Message = $"Checking {mirrors.Count} mirrors...",
                FilePath = outputPath
            });

            // Try each mirror
            Exception? lastException = null;
            for (int mirrorIndex = 0; mirrorIndex < mirrors.Count; mirrorIndex++)
            {
                var mirror = mirrors[mirrorIndex];

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                _logger.LogInformation("Attempting download from mirror {Index}/{Total}: {MirrorName}",
                    mirrorIndex + 1, mirrors.Count, mirror.Name);

                ReportProgress(progress, new DownloadProgressEventArgs
                {
                    DownloadId = downloadId,
                    Stage = DownloadStage.Downloading,
                    Message = $"Downloading from {mirror.Name}...",
                    CurrentUrl = mirror.Url,
                    MirrorIndex = mirrorIndex,
                    FilePath = outputPath
                });

                try
                {
                    // Attempt download with retries
                    var downloadSuccess = await AttemptDownloadWithRetriesAsync(
                        mirror,
                        outputPath,
                        progress,
                        downloadId,
                        mirrorIndex,
                        maxRetries,
                        ct);

                    if (!downloadSuccess)
                    {
                        continue;
                    }

                    // Verify checksum if provided
                    if (!string.IsNullOrEmpty(expectedSha256))
                    {
                        ReportProgress(progress, new DownloadProgressEventArgs
                        {
                            DownloadId = downloadId,
                            Stage = DownloadStage.Verifying,
                            Message = "Verifying file integrity...",
                            PercentComplete = 100,
                            FilePath = outputPath
                        });

                        var verificationResult = await _verificationService.VerifyFileAsync(
                            outputPath,
                            expectedSha256,
                            ct);

                        if (!verificationResult.IsValid)
                        {
                            _logger.LogWarning("Checksum verification failed for download from {Mirror}",
                                mirror.Name);
                            
                            // Try next mirror
                            continue;
                        }

                        result.Sha256 = verificationResult.ActualSha256;
                    }

                    // Success!
                    UpdateMirrorSuccess(mirror);
                    
                    result.Success = true;
                    result.MirrorUsed = mirror.Name;
                    result.MirrorUrl = mirror.Url;

                    ReportProgress(progress, new DownloadProgressEventArgs
                    {
                        DownloadId = downloadId,
                        Stage = DownloadStage.Completed,
                        Message = "Download completed successfully",
                        PercentComplete = 100,
                        IsComplete = true,
                        FilePath = outputPath
                    });

                    _logger.LogInformation("Download completed successfully from {Mirror}", mirror.Name);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Download failed from mirror {Mirror}", mirror.Name);
                    UpdateMirrorFailure(mirror);
                    lastException = ex;
                }
            }

            // All mirrors failed
            throw lastException ?? new InvalidOperationException("All mirrors failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed: {DownloadId}", downloadId);
            
            result.Success = false;
            result.ErrorMessage = ex.Message;

            ReportProgress(progress, new DownloadProgressEventArgs
            {
                DownloadId = downloadId,
                Stage = DownloadStage.Failed,
                Message = $"Download failed: {ex.Message}",
                IsComplete = true,
                HasError = true,
                ErrorMessage = ex.Message,
                FilePath = outputPath
            });

            return result;
        }
    }

    /// <summary>
    /// Attempt download with retry logic
    /// </summary>
    private async Task<bool> AttemptDownloadWithRetriesAsync(
        DownloadMirror mirror,
        string outputPath,
        IProgress<DownloadProgressEventArgs>? progress,
        string downloadId,
        int mirrorIndex,
        int maxRetries,
        CancellationToken ct)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    var delaySeconds = (int)Math.Pow(2, attempt - 1);
                    _logger.LogInformation("Retry attempt {Attempt} of {MaxRetries} after {Delay}s delay",
                        attempt, maxRetries, delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                }

                _logger.LogInformation("Download attempt {Attempt} from {Url}", attempt, mirror.Url);

                // Perform the actual download
                using var request = new HttpRequestMessage(HttpMethod.Get, mirror.Url);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                long bytesDownloaded = 0;

                var tempPath = outputPath + ".partial";
                await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                await using (var httpStream = await response.Content.ReadAsStreamAsync(ct))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    var lastProgressReport = DateTime.UtcNow;

                    while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                        bytesDownloaded += bytesRead;

                        // Report progress every 500ms
                        if ((DateTime.UtcNow - lastProgressReport).TotalMilliseconds >= 500 || bytesDownloaded == totalBytes)
                        {
                            var percentComplete = totalBytes > 0 ? (int)((bytesDownloaded * 100.0) / totalBytes) : 0;
                            
                            ReportProgress(progress, new DownloadProgressEventArgs
                            {
                                DownloadId = downloadId,
                                Stage = DownloadStage.Downloading,
                                PercentComplete = percentComplete,
                                Message = $"Downloading... {percentComplete}% ({bytesDownloaded}/{totalBytes} bytes)",
                                CurrentUrl = mirror.Url,
                                MirrorIndex = mirrorIndex,
                                FilePath = outputPath
                            });

                            lastProgressReport = DateTime.UtcNow;
                        }
                    }

                    await fileStream.FlushAsync(ct);
                }

                // Move temp file to final location
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.Move(tempPath, outputPath);

                // Final progress report
                ReportProgress(progress, new DownloadProgressEventArgs
                {
                    DownloadId = downloadId,
                    Stage = DownloadStage.Downloading,
                    PercentComplete = 100,
                    Message = "Download complete",
                    CurrentUrl = mirror.Url,
                    MirrorIndex = mirrorIndex,
                    FilePath = outputPath
                });

                _logger.LogInformation("Successfully downloaded {Bytes} bytes from {Url}", bytesDownloaded, mirror.Url);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("URL not found (404): {Url}", mirror.Url);
                return false; // Don't retry 404s
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Download timeout from {Url}, attempt {Attempt}", mirror.Url, attempt);
                
                if (attempt >= maxRetries)
                {
                    return false;
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Download error from {Url}, attempt {Attempt}, will retry",
                    mirror.Url, attempt);
            }
        }

        return false;
    }

    /// <summary>
    /// Select mirrors for download based on priority and health
    /// </summary>
    private List<DownloadMirror> SelectMirrors()
    {
        return _mirrors.Values
            .Where(m => m.IsEnabled && m.HealthStatus != MirrorHealthStatus.Disabled)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.ConsecutiveFailures)
            .ThenByDescending(m => m.LastSuccess)
            .ToList();
    }

    /// <summary>
    /// Check mirror health internally
    /// </summary>
    private async Task<bool> CheckMirrorHealthInternalAsync(
        DownloadMirror mirror,
        CancellationToken ct)
    {
        _logger.LogDebug("Checking health of mirror: {MirrorId} ({MirrorName})", mirror.Id, mirror.Name);

        mirror.LastChecked = DateTime.UtcNow;

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Simple HEAD request to check if mirror is responding
            using var request = new HttpRequestMessage(HttpMethod.Head, mirror.Url);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            
            var response = await _httpClient.SendAsync(request, linkedCts.Token);
            
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            mirror.AverageResponseTimeMs = responseTime;

            if (response.IsSuccessStatusCode)
            {
                mirror.HealthStatus = MirrorHealthStatus.Healthy;
                mirror.ConsecutiveFailures = 0;
                _logger.LogDebug("Mirror {MirrorId} is healthy (response time: {ResponseTime}ms)",
                    mirror.Id, responseTime);
                return true;
            }
            else
            {
                mirror.HealthStatus = MirrorHealthStatus.Degraded;
                _logger.LogWarning("Mirror {MirrorId} returned status code: {StatusCode}",
                    mirror.Id, response.StatusCode);
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            mirror.HealthStatus = MirrorHealthStatus.Unhealthy;
            _logger.LogWarning("Mirror {MirrorId} health check timed out", mirror.Id);
            return false;
        }
        catch (Exception ex)
        {
            mirror.HealthStatus = MirrorHealthStatus.Unhealthy;
            _logger.LogWarning(ex, "Mirror {MirrorId} health check failed", mirror.Id);
            return false;
        }
    }

    /// <summary>
    /// Update mirror state after successful download
    /// </summary>
    private void UpdateMirrorSuccess(DownloadMirror mirror)
    {
        mirror.LastSuccess = DateTime.UtcNow;
        mirror.ConsecutiveFailures = 0;
        mirror.HealthStatus = MirrorHealthStatus.Healthy;
        _logger.LogDebug("Updated mirror {MirrorId} after successful download", mirror.Id);
    }

    /// <summary>
    /// Update mirror state after failed download
    /// </summary>
    private void UpdateMirrorFailure(DownloadMirror mirror)
    {
        mirror.ConsecutiveFailures++;
        
        if (mirror.ConsecutiveFailures >= MaxConsecutiveFailuresBeforeDisable)
        {
            mirror.IsEnabled = false;
            mirror.HealthStatus = MirrorHealthStatus.Disabled;
            _logger.LogWarning("Mirror {MirrorId} disabled after {Failures} consecutive failures",
                mirror.Id, mirror.ConsecutiveFailures);
        }
        else
        {
            mirror.HealthStatus = MirrorHealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Report progress to listeners
    /// </summary>
    private void ReportProgress(
        IProgress<DownloadProgressEventArgs>? progress,
        DownloadProgressEventArgs args)
    {
        progress?.Report(args);
    }
}

/// <summary>
/// Result of a download operation
/// </summary>
public class DownloadResult
{
    public string DownloadId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? MirrorUsed { get; set; }
    public string? MirrorUrl { get; set; }
    public string? Sha256 { get; set; }
    public string? ErrorMessage { get; set; }
}
