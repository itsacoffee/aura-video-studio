using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for managing audio licensing information and exports
/// </summary>
public class LicensingService
{
    private readonly ILogger<LicensingService> _logger;
    private readonly Dictionary<string, List<UsedAsset>> _jobAssets = new();

    public LicensingService(ILogger<LicensingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Track an asset used in a job
    /// </summary>
    public void TrackAssetUsage(
        string jobId,
        AudioAsset asset,
        int sceneIndex,
        TimeSpan startTime,
        TimeSpan duration,
        bool isSelected = true)
    {
        _logger.LogInformation("Tracking asset usage: {JobId} - {AssetId} - Scene {Scene}",
            jobId, asset.AssetId, sceneIndex);

        if (!_jobAssets.TryGetValue(jobId, out var value))
        {
            value = new List<UsedAsset>();
            _jobAssets[jobId] = value;
        }

        var usedAsset = new UsedAsset(asset, sceneIndex, startTime, duration, isSelected);
        value.Add(usedAsset);
    }

    /// <summary>
    /// Get licensing summary for a job
    /// </summary>
    public Task<LicensingSummary> GetLicensingSummaryAsync(
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating licensing summary for {JobId}", jobId);

        if (!_jobAssets.TryGetValue(jobId, out var assets))
        {
            assets = new List<UsedAsset>();
        }

        var selectedAssets = assets.Where(a => a.IsSelected).ToList();

        var allCommercialUse = selectedAssets.All(a => a.Asset.CommercialUseAllowed);

        var requiredAttributions = selectedAssets
            .Where(a => a.Asset.AttributionRequired)
            .Select(a => a.Asset.AttributionText ?? $"{a.Asset.Title} by {a.Asset.Artist}")
            .Distinct()
            .ToList();

        var licenseUrls = selectedAssets
            .Select(a => a.Asset.LicenseUrl)
            .Distinct()
            .ToList();

        var summary = new LicensingSummary(
            UsedAssets: selectedAssets,
            AllCommercialUseAllowed: allCommercialUse,
            RequiredAttributions: requiredAttributions,
            LicenseUrls: licenseUrls,
            GeneratedAt: DateTime.UtcNow,
            GeneratedFor: jobId
        );

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Export licensing information to CSV
    /// </summary>
    public async Task<string> ExportToCsvAsync(
        string jobId,
        bool includeUnused = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to CSV for {JobId}", jobId);

        var summary = await GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);
        var assets = includeUnused
            ? _jobAssets.GetValueOrDefault(jobId, new List<UsedAsset>())
            : summary.UsedAssets;

        var csv = new StringBuilder();
        csv.AppendLine("Asset ID,Title,Artist,Scene,Start Time,Duration,License Type,Commercial Use,Attribution Required,Attribution Text,Source,License URL");

        foreach (var used in assets)
        {
            var asset = used.Asset;
            csv.AppendLine(
                $"\"{asset.AssetId}\"," +
                $"\"{asset.Title}\"," +
                $"\"{asset.Artist ?? ""}\"," +
                $"{used.SceneIndex}," +
                $"{used.StartTime.TotalSeconds:F2}," +
                $"{used.Duration.TotalSeconds:F2}," +
                $"\"{asset.LicenseType}\"," +
                $"{asset.CommercialUseAllowed}," +
                $"{asset.AttributionRequired}," +
                $"\"{asset.AttributionText ?? ""}\"," +
                $"\"{asset.SourcePlatform}\"," +
                $"\"{asset.LicenseUrl}\""
            );
        }

        return csv.ToString();
    }

    /// <summary>
    /// Export licensing information to JSON
    /// </summary>
    public async Task<string> ExportToJsonAsync(
        string jobId,
        bool includeUnused = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to JSON for {JobId}", jobId);

        var summary = await GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);
        var assets = includeUnused
            ? _jobAssets.GetValueOrDefault(jobId, new List<UsedAsset>())
            : summary.UsedAssets;

        var exportData = new
        {
            JobId = jobId,
            GeneratedAt = summary.GeneratedAt,
            AllCommercialUseAllowed = summary.AllCommercialUseAllowed,
            RequiredAttributions = summary.RequiredAttributions,
            LicenseUrls = summary.LicenseUrls,
            Assets = assets.Select(used => new
            {
                AssetId = used.Asset.AssetId,
                Title = used.Asset.Title,
                Artist = used.Asset.Artist,
                SceneIndex = used.SceneIndex,
                StartTime = used.StartTime.TotalSeconds,
                Duration = used.Duration.TotalSeconds,
                LicenseType = used.Asset.LicenseType.ToString(),
                CommercialUseAllowed = used.Asset.CommercialUseAllowed,
                AttributionRequired = used.Asset.AttributionRequired,
                AttributionText = used.Asset.AttributionText,
                SourcePlatform = used.Asset.SourcePlatform,
                LicenseUrl = used.Asset.LicenseUrl,
                IsSelected = used.IsSelected
            }).ToList()
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Export licensing information as plain text attribution list
    /// </summary>
    public async Task<string> ExportToTextAsync(
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to text for {JobId}", jobId);

        var summary = await GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);

        var text = new StringBuilder();
        text.AppendLine("AUDIO LICENSING INFORMATION");
        text.AppendLine($"Generated: {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        text.AppendLine($"Project: {jobId}");
        text.AppendLine();

        if (!summary.AllCommercialUseAllowed)
        {
            text.AppendLine("⚠️  WARNING: Not all assets allow commercial use!");
            text.AppendLine();
        }

        if (summary.RequiredAttributions.Count > 0)
        {
            text.AppendLine("REQUIRED ATTRIBUTIONS:");
            text.AppendLine();
            foreach (var attribution in summary.RequiredAttributions)
            {
                text.AppendLine($"  • {attribution}");
            }
            text.AppendLine();
        }

        text.AppendLine("ASSET DETAILS:");
        text.AppendLine();

        var assetsByScene = summary.UsedAssets.GroupBy(a => a.SceneIndex).OrderBy(g => g.Key);

        foreach (var sceneGroup in assetsByScene)
        {
            text.AppendLine($"Scene {sceneGroup.Key}:");
            foreach (var used in sceneGroup)
            {
                var asset = used.Asset;
                text.AppendLine($"  {asset.Title} by {asset.Artist ?? "Unknown"}");
                text.AppendLine($"    License: {asset.LicenseType}");
                text.AppendLine($"    Commercial Use: {(asset.CommercialUseAllowed ? "Yes" : "No")}");
                text.AppendLine($"    Attribution: {(asset.AttributionRequired ? "Required" : "Not required")}");
                text.AppendLine($"    Source: {asset.SourcePlatform}");
                text.AppendLine($"    License URL: {asset.LicenseUrl}");
                text.AppendLine();
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// Export as HTML with formatted attributions for video end credits
    /// </summary>
    public async Task<string> ExportToHtmlAsync(
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to HTML for {JobId}", jobId);

        var summary = await GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"UTF-8\">");
        html.AppendLine("  <title>Audio Licensing - " + jobId + "</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Arial, sans-serif; margin: 40px; }");
        html.AppendLine("    h1 { color: #333; }");
        html.AppendLine("    .warning { background: #fff3cd; border: 1px solid #ffc107; padding: 10px; margin: 20px 0; }");
        html.AppendLine("    .attribution { background: #f8f9fa; padding: 15px; margin: 10px 0; border-left: 4px solid #007bff; }");
        html.AppendLine("    .asset { margin: 20px 0; padding: 15px; border: 1px solid #ddd; }");
        html.AppendLine("    .license-badge { display: inline-block; padding: 4px 8px; margin: 5px 0; background: #28a745; color: white; border-radius: 4px; font-size: 12px; }");
        html.AppendLine("    .license-badge.restricted { background: #dc3545; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        
        html.AppendLine($"  <h1>Audio Licensing Information</h1>");
        html.AppendLine($"  <p><strong>Project:</strong> {jobId}</p>");
        html.AppendLine($"  <p><strong>Generated:</strong> {summary.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");

        if (!summary.AllCommercialUseAllowed)
        {
            html.AppendLine("  <div class=\"warning\">");
            html.AppendLine("    <strong>⚠️  WARNING:</strong> Not all assets allow commercial use!");
            html.AppendLine("  </div>");
        }

        if (summary.RequiredAttributions.Count > 0)
        {
            html.AppendLine("  <h2>Required Attributions</h2>");
            foreach (var attribution in summary.RequiredAttributions)
            {
                html.AppendLine($"  <div class=\"attribution\">{attribution}</div>");
            }
        }

        html.AppendLine("  <h2>Asset Details</h2>");

        var assetsByScene = summary.UsedAssets.GroupBy(a => a.SceneIndex).OrderBy(g => g.Key);

        foreach (var sceneGroup in assetsByScene)
        {
            html.AppendLine($"  <h3>Scene {sceneGroup.Key}</h3>");
            foreach (var used in sceneGroup)
            {
                var asset = used.Asset;
                var badgeClass = asset.CommercialUseAllowed ? "" : " restricted";
                
                html.AppendLine("  <div class=\"asset\">");
                html.AppendLine($"    <h4>{asset.Title}</h4>");
                html.AppendLine($"    <p><strong>Artist:</strong> {asset.Artist ?? "Unknown"}</p>");
                html.AppendLine($"    <p><strong>Source:</strong> {asset.SourcePlatform}</p>");
                html.AppendLine($"    <span class=\"license-badge{badgeClass}\">{asset.LicenseType}</span>");
                html.AppendLine($"    <p><strong>Commercial Use:</strong> {(asset.CommercialUseAllowed ? "✓ Allowed" : "✗ Not Allowed")}</p>");
                html.AppendLine($"    <p><strong>Attribution:</strong> {(asset.AttributionRequired ? "✓ Required" : "Not required")}</p>");
                html.AppendLine($"    <p><strong>License:</strong> <a href=\"{asset.LicenseUrl}\" target=\"_blank\">{asset.LicenseUrl}</a></p>");
                html.AppendLine("  </div>");
            }
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Validate licensing for commercial use
    /// </summary>
    public async Task<(bool IsValid, List<string> Issues)> ValidateForCommercialUseAsync(
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating licensing for commercial use: {JobId}", jobId);

        var summary = await GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);
        var issues = new List<string>();

        if (!summary.AllCommercialUseAllowed)
        {
            var restrictedAssets = summary.UsedAssets
                .Where(a => !a.Asset.CommercialUseAllowed)
                .Select(a => $"Scene {a.SceneIndex}: {a.Asset.Title} ({a.Asset.LicenseType})")
                .ToList();

            issues.Add($"The following assets do not allow commercial use:");
            issues.AddRange(restrictedAssets);
        }

        if (summary.RequiredAttributions.Count > 0)
        {
            issues.Add($"Attribution is required for {summary.RequiredAttributions.Count} asset(s).");
            issues.Add("Make sure to include attributions in your video credits.");
        }

        var isValid = !summary.UsedAssets.Any(a => !a.Asset.CommercialUseAllowed);

        return (isValid, issues);
    }

    /// <summary>
    /// Clear tracking data for a job
    /// </summary>
    public void ClearJobData(string jobId)
    {
        _logger.LogInformation("Clearing licensing data for {JobId}", jobId);
        _jobAssets.Remove(jobId);
    }
}
