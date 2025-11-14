using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentVerification;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentVerification;

/// <summary>
/// Manages persistence of verification results to disk
/// </summary>
public class VerificationPersistence
{
    private readonly ILogger<VerificationPersistence> _logger;
    private readonly string _verificationDirectory;
    private readonly string _historyDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public VerificationPersistence(ILogger<VerificationPersistence> logger, string baseDirectory)
    {
        _logger = logger;
        _verificationDirectory = Path.Combine(baseDirectory, "Verification");
        _historyDirectory = Path.Combine(_verificationDirectory, "History");
        
        // Ensure directories exist
        Directory.CreateDirectory(_verificationDirectory);
        Directory.CreateDirectory(_historyDirectory);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Save verification result to disk
    /// </summary>
    public async Task SaveVerificationResultAsync(
        VerificationResult result,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetVerificationFilePath(result.ContentId);
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            
            // Write to temp file first, then rename for atomic operation
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            
            _logger.LogDebug("Saved verification result for content {ContentId}", result.ContentId);

            // Also save to history
            await SaveToHistoryAsync(result, ct).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Load verification result from disk
    /// </summary>
    public async Task<VerificationResult?> LoadVerificationResultAsync(
        string contentId,
        CancellationToken ct = default)
    {
        var filePath = GetVerificationFilePath(contentId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No verification result found for content {ContentId}", contentId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<VerificationResult>(json, _jsonOptions);
            _logger.LogDebug("Loaded verification result for content {ContentId}", contentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load verification result for content {ContentId}", contentId);
            return null;
        }
    }

    /// <summary>
    /// Load verification history for content
    /// </summary>
    public async Task<List<VerificationResult>> LoadVerificationHistoryAsync(
        string contentId,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var historyFiles = Directory.GetFiles(_historyDirectory, $"{contentId}_*.json")
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .Take(maxResults)
            .ToList();

        var results = new List<VerificationResult>();

        foreach (var filePath in historyFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<VerificationResult>(json, _jsonOptions);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load history file {FilePath}", filePath);
            }
        }

        return results;
    }

    /// <summary>
    /// Delete verification result
    /// </summary>
    public async Task DeleteVerificationResultAsync(
        string contentId,
        CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var filePath = GetVerificationFilePath(contentId);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted verification result for content {ContentId}", contentId);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// List all verified content IDs
    /// </summary>
    public Task<List<string>> ListVerifiedContentAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_verificationDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();

        _logger.LogDebug("Found {Count} verified content items", files.Count);
        return Task.FromResult(files);
    }

    /// <summary>
    /// Get verification statistics
    /// </summary>
    public async Task<VerificationStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var contentIds = await ListVerifiedContentAsync(ct).ConfigureAwait(false);
        var results = new List<VerificationResult>();

        foreach (var contentId in contentIds)
        {
            var result = await LoadVerificationResultAsync(contentId, ct).ConfigureAwait(false);
            if (result != null)
            {
                results.Add(result);
            }
        }

        var totalVerified = results.Count;
        var verified = results.Count(r => r.OverallStatus == VerificationStatus.Verified);
        var partiallyVerified = results.Count(r => r.OverallStatus == VerificationStatus.PartiallyVerified);
        var unverified = results.Count(r => r.OverallStatus == VerificationStatus.Unverified);
        var disputed = results.Count(r => r.OverallStatus == VerificationStatus.Disputed);
        var falseContent = results.Count(r => r.OverallStatus == VerificationStatus.False);

        var avgConfidence = results.Count != 0 ? results.Average(r => r.OverallConfidence) : 0.0;

        return new VerificationStatistics(
            TotalVerified: totalVerified,
            Verified: verified,
            PartiallyVerified: partiallyVerified,
            Unverified: unverified,
            Disputed: disputed,
            False: falseContent,
            AverageConfidence: avgConfidence,
            LastUpdated: DateTime.UtcNow
        );
    }

    private string GetVerificationFilePath(string contentId)
    {
        return Path.Combine(_verificationDirectory, $"{contentId}.json");
    }

    private async Task SaveToHistoryAsync(VerificationResult result, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var historyFilePath = Path.Combine(_historyDirectory, 
            $"{result.ContentId}_{timestamp}.json");
        
        var json = JsonSerializer.Serialize(result, _jsonOptions);
        await File.WriteAllTextAsync(historyFilePath, json, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Verification statistics
/// </summary>
public record VerificationStatistics(
    int TotalVerified,
    int Verified,
    int PartiallyVerified,
    int Unverified,
    int Disputed,
    int False,
    double AverageConfidence,
    DateTime LastUpdated
);
