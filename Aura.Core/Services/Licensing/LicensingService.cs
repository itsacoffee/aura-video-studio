using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;
using LicensingModels = Aura.Core.Models.Licensing;

namespace Aura.Core.Services.Licensing;

/// <summary>
/// Service for managing licensing and provenance information
/// </summary>
public class LicensingService : ILicensingService
{
    private readonly ILogger<LicensingService> _logger;
    private readonly Dictionary<string, LicensingModels.ProjectLicensingManifest> _manifestCache = new();
    private readonly Dictionary<string, LicensingModels.LicensingSignOff> _signOffCache = new();

    public LicensingService(ILogger<LicensingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate licensing manifest for a project
    /// </summary>
    public async Task<LicensingModels.ProjectLicensingManifest> GenerateManifestAsync(string projectId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating licensing manifest for project {ProjectId}", projectId);

        if (_manifestCache.TryGetValue(projectId, out var cached))
        {
            _logger.LogDebug("Returning cached manifest for project {ProjectId}", projectId);
            return cached;
        }

        var assets = new List<LicensingModels.AssetLicensingInfo>();
        var warnings = new List<string>();
        var missingInfo = new List<string>();

        var manifest = new LicensingModels.ProjectLicensingManifest
        {
            ProjectId = projectId,
            ProjectName = $"Project {projectId}",
            GeneratedAt = DateTime.UtcNow,
            Assets = assets,
            Warnings = warnings,
            MissingLicensingInfo = missingInfo,
            AllCommercialUseAllowed = true,
            Summary = CalculateSummary(assets)
        };

        _manifestCache[projectId] = manifest;

        _logger.LogInformation("Generated licensing manifest for project {ProjectId} with {AssetCount} assets",
            projectId, assets.Count);

        return await Task.FromResult(manifest).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate manifest from timeline data
    /// </summary>
    public LicensingModels.ProjectLicensingManifest GenerateManifestFromTimeline(string projectId, Models.Timeline.EditableTimeline timeline)
    {
        _logger.LogInformation("Generating licensing manifest from timeline for project {ProjectId}", projectId);

        var assets = new List<LicensingModels.AssetLicensingInfo>();
        var warnings = new List<string>();
        var missingInfo = new List<string>();

        if (timeline?.Scenes != null)
        {
            for (int i = 0; i < timeline.Scenes.Count; i++)
            {
                var scene = timeline.Scenes[i];

                if (scene.VisualAssets != null && scene.VisualAssets.Count > 0)
                {
                    foreach (var visualAsset in scene.VisualAssets)
                    {
                        var asset = new LicensingModels.AssetLicensingInfo
                        {
                            AssetId = visualAsset.Id,
                            AssetType = LicensingModels.AssetType.Visual,
                            SceneIndex = i,
                            Name = $"Visual for scene {i}",
                            Source = "Unknown",
                            FilePath = visualAsset.FilePath,
                            LicenseType = "Unknown",
                            CommercialUseAllowed = false,
                            AttributionRequired = false
                        };

                        warnings.Add($"Scene {i}: Visual source is unknown");
                        missingInfo.Add($"Scene {i}: Visual licensing information");

                        assets.Add(asset);
                    }
                }

                if (!string.IsNullOrEmpty(scene.NarrationAudioPath))
                {
                    var narrationAsset = new LicensingModels.AssetLicensingInfo
                    {
                        AssetId = $"narration-{i}",
                        AssetType = LicensingModels.AssetType.Narration,
                        SceneIndex = i,
                        Name = $"Narration for scene {i}",
                        Source = "TTS",
                        FilePath = scene.NarrationAudioPath,
                        LicenseType = "Generated",
                        CommercialUseAllowed = true,
                        AttributionRequired = false
                    };

                    assets.Add(narrationAsset);
                }

                if (!string.IsNullOrEmpty(scene.Script))
                {
                    var captionAsset = new LicensingModels.AssetLicensingInfo
                    {
                        AssetId = $"caption-{i}",
                        AssetType = LicensingModels.AssetType.Caption,
                        SceneIndex = i,
                        Name = $"Caption for scene {i}",
                        Source = "Script",
                        LicenseType = "Generated",
                        CommercialUseAllowed = true,
                        AttributionRequired = false
                    };

                    assets.Add(captionAsset);
                }
            }
        }

        var allCommercialAllowed = assets.All(a => a.CommercialUseAllowed);

        var manifest = new LicensingModels.ProjectLicensingManifest
        {
            ProjectId = projectId,
            ProjectName = $"Project {projectId}",
            GeneratedAt = DateTime.UtcNow,
            Assets = assets,
            Warnings = warnings,
            MissingLicensingInfo = missingInfo,
            AllCommercialUseAllowed = allCommercialAllowed,
            Summary = CalculateSummary(assets)
        };

        _manifestCache[projectId] = manifest;

        _logger.LogInformation("Generated licensing manifest from timeline for project {ProjectId} with {AssetCount} assets",
            projectId, assets.Count);

        return manifest;
    }

    /// <summary>
    /// Export licensing manifest in specified format
    /// </summary>
    public async Task<string> ExportManifestAsync(LicensingModels.ProjectLicensingManifest manifest, LicensingModels.LicensingExportFormat format, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting manifest for project {ProjectId} in format {Format}",
            manifest.ProjectId, format);

        return format switch
        {
            LicensingModels.LicensingExportFormat.Json => await ExportAsJsonAsync(manifest, cancellationToken).ConfigureAwait(false),
            LicensingModels.LicensingExportFormat.Csv => await ExportAsCsvAsync(manifest, cancellationToken).ConfigureAwait(false),
            LicensingModels.LicensingExportFormat.Html => await ExportAsHtmlAsync(manifest, cancellationToken).ConfigureAwait(false),
            LicensingModels.LicensingExportFormat.Text => await ExportAsTextAsync(manifest, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };
    }

    /// <summary>
    /// Validate manifest and check for issues
    /// </summary>
    public bool ValidateManifest(LicensingModels.ProjectLicensingManifest manifest)
    {
        if (manifest == null)
        {
            _logger.LogWarning("Manifest validation failed: manifest is null");
            return false;
        }

        if (string.IsNullOrEmpty(manifest.ProjectId))
        {
            _logger.LogWarning("Manifest validation failed: missing project ID");
            return false;
        }

        if (manifest.Assets == null || manifest.Assets.Count == 0)
        {
            _logger.LogWarning("Manifest validation warning: no assets in manifest");
        }

        if (manifest.MissingLicensingInfo.Count > 0)
        {
            _logger.LogWarning("Manifest has {Count} assets with missing licensing information",
                manifest.MissingLicensingInfo.Count);
        }

        if (!manifest.AllCommercialUseAllowed)
        {
            _logger.LogWarning("Manifest contains assets with commercial use restrictions");
        }

        return true;
    }

    /// <summary>
    /// Record sign-off for licensing
    /// </summary>
    public async Task RecordSignOffAsync(LicensingModels.LicensingSignOff signOff, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording licensing sign-off for project {ProjectId}", signOff.ProjectId);

        _signOffCache[signOff.ProjectId] = signOff;

        _logger.LogInformation("Sign-off recorded for project {ProjectId} at {Timestamp}",
            signOff.ProjectId, signOff.SignedOffAt);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private LicensingModels.LicensingSummary CalculateSummary(List<LicensingModels.AssetLicensingInfo> assets)
    {
        var assetsByType = new Dictionary<LicensingModels.AssetType, int>();
        var assetsBySource = new Dictionary<string, int>();
        var assetsByLicenseType = new Dictionary<string, int>();

        foreach (var asset in assets)
        {
            assetsByType.TryGetValue(asset.AssetType, out var typeCount);
            assetsByType[asset.AssetType] = typeCount + 1;

            if (!string.IsNullOrEmpty(asset.Source))
            {
                assetsBySource.TryGetValue(asset.Source, out var sourceCount);
                assetsBySource[asset.Source] = sourceCount + 1;
            }

            if (!string.IsNullOrEmpty(asset.LicenseType))
            {
                assetsByLicenseType.TryGetValue(asset.LicenseType, out var licenseCount);
                assetsByLicenseType[asset.LicenseType] = licenseCount + 1;
            }
        }

        return new LicensingModels.LicensingSummary
        {
            TotalAssets = assets.Count,
            AssetsByType = assetsByType,
            AssetsBySource = assetsBySource,
            AssetsByLicenseType = assetsByLicenseType,
            AssetsRequiringAttribution = assets.Count(a => a.AttributionRequired),
            AssetsWithCommercialRestrictions = assets.Count(a => !a.CommercialUseAllowed)
        };
    }

    private async Task<string> ExportAsJsonAsync(LicensingModels.ProjectLicensingManifest manifest, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return await Task.FromResult(JsonSerializer.Serialize(manifest, options)).ConfigureAwait(false);
    }

    private async Task<string> ExportAsCsvAsync(LicensingModels.ProjectLicensingManifest manifest, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Asset ID,Type,Scene,Name,Source,License Type,License URL,Commercial Use,Attribution Required,Attribution Text,Creator,Creator URL,Source URL,File Path");

        foreach (var asset in manifest.Assets)
        {
            sb.AppendLine($"\"{asset.AssetId}\",\"{asset.AssetType}\",{asset.SceneIndex},\"{asset.Name}\",\"{asset.Source}\",\"{asset.LicenseType}\",\"{asset.LicenseUrl ?? ""}\",{asset.CommercialUseAllowed},{asset.AttributionRequired},\"{asset.AttributionText ?? ""}\",\"{asset.Creator ?? ""}\",\"{asset.CreatorUrl ?? ""}\",\"{asset.SourceUrl ?? ""}\",\"{asset.FilePath ?? ""}\"");
        }

        return await Task.FromResult(sb.ToString()).ConfigureAwait(false);
    }

    private async Task<string> ExportAsHtmlAsync(LicensingModels.ProjectLicensingManifest manifest, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine($"<title>Licensing Information - {manifest.ProjectName}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1, h2 { color: #333; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #4CAF50; color: white; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        sb.AppendLine(".warning { color: #ff6b6b; font-weight: bold; }");
        sb.AppendLine(".summary { background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 20px 0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>Licensing Information</h1>");
        sb.AppendLine($"<p><strong>Project:</strong> {manifest.ProjectName}</p>");
        sb.AppendLine($"<p><strong>Generated:</strong> {manifest.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p><strong>Commercial Use Allowed:</strong> {(manifest.AllCommercialUseAllowed ? "Yes" : "No")}</p>");

        if (manifest.Warnings.Count > 0)
        {
            sb.AppendLine("<div class='warning'>");
            sb.AppendLine("<h2>Warnings</h2>");
            sb.AppendLine("<ul>");
            foreach (var warning in manifest.Warnings)
            {
                sb.AppendLine($"<li>{warning}</li>");
            }
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine($"<p><strong>Total Assets:</strong> {manifest.Summary.TotalAssets}</p>");
        sb.AppendLine($"<p><strong>Assets Requiring Attribution:</strong> {manifest.Summary.AssetsRequiringAttribution}</p>");
        sb.AppendLine($"<p><strong>Assets with Commercial Restrictions:</strong> {manifest.Summary.AssetsWithCommercialRestrictions}</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("<h2>Assets</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Type</th><th>Scene</th><th>Name</th><th>Source</th><th>License</th><th>Commercial</th><th>Attribution</th></tr>");

        foreach (var asset in manifest.Assets)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{asset.AssetType}</td>");
            sb.AppendLine($"<td>{asset.SceneIndex}</td>");
            sb.AppendLine($"<td>{asset.Name}</td>");
            sb.AppendLine($"<td>{asset.Source}</td>");
            sb.AppendLine($"<td>{asset.LicenseType}</td>");
            sb.AppendLine($"<td>{(asset.CommercialUseAllowed ? "Yes" : "No")}</td>");
            sb.AppendLine($"<td>{(asset.AttributionRequired ? asset.AttributionText ?? "Required" : "Not Required")}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return await Task.FromResult(sb.ToString()).ConfigureAwait(false);
    }

    private async Task<string> ExportAsTextAsync(LicensingModels.ProjectLicensingManifest manifest, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("==============================================");
        sb.AppendLine($"LICENSING INFORMATION");
        sb.AppendLine("==============================================");
        sb.AppendLine();
        sb.AppendLine($"Project: {manifest.ProjectName}");
        sb.AppendLine($"Generated: {manifest.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Commercial Use Allowed: {(manifest.AllCommercialUseAllowed ? "Yes" : "No")}");
        sb.AppendLine();

        if (manifest.Warnings.Count > 0)
        {
            sb.AppendLine("WARNINGS:");
            sb.AppendLine("----------------------------------------------");
            foreach (var warning in manifest.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("SUMMARY:");
        sb.AppendLine("----------------------------------------------");
        sb.AppendLine($"Total Assets: {manifest.Summary.TotalAssets}");
        sb.AppendLine($"Assets Requiring Attribution: {manifest.Summary.AssetsRequiringAttribution}");
        sb.AppendLine($"Assets with Commercial Restrictions: {manifest.Summary.AssetsWithCommercialRestrictions}");
        sb.AppendLine();

        sb.AppendLine("ASSETS:");
        sb.AppendLine("----------------------------------------------");

        foreach (var asset in manifest.Assets)
        {
            sb.AppendLine();
            sb.AppendLine($"[{asset.AssetType}] Scene {asset.SceneIndex}: {asset.Name}");
            sb.AppendLine($"  Source: {asset.Source}");
            sb.AppendLine($"  License: {asset.LicenseType}");
            sb.AppendLine($"  Commercial Use: {(asset.CommercialUseAllowed ? "Allowed" : "Restricted")}");
            sb.AppendLine($"  Attribution: {(asset.AttributionRequired ? asset.AttributionText ?? "Required" : "Not Required")}");

            if (!string.IsNullOrEmpty(asset.Creator))
            {
                sb.AppendLine($"  Creator: {asset.Creator}");
            }

            if (!string.IsNullOrEmpty(asset.LicenseUrl))
            {
                sb.AppendLine($"  License URL: {asset.LicenseUrl}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("==============================================");

        return await Task.FromResult(sb.ToString()).ConfigureAwait(false);
    }
}
