using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// Component manifest entry from components.json
/// </summary>
public class ComponentManifestEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("githubRepo")]
    public string? GitHubRepo { get; set; }
    
    [JsonPropertyName("assetPattern")]
    public JsonElement AssetPattern { get; set; }
    
    [JsonPropertyName("mirrors")]
    public List<string> Mirrors { get; set; } = new();
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("extractPath")]
    public string? ExtractPath { get; set; }
    
    [JsonPropertyName("isGitRepo")]
    public bool IsGitRepo { get; set; }
    
    /// <summary>
    /// Get asset pattern for current platform
    /// </summary>
    public string? GetAssetPatternForPlatform()
    {
        if (AssetPattern.ValueKind == JsonValueKind.String)
        {
            return AssetPattern.GetString();
        }
        else if (AssetPattern.ValueKind == JsonValueKind.Object)
        {
            var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
            if (AssetPattern.TryGetProperty(platform, out var patternElement))
            {
                return patternElement.GetString();
            }
        }
        return null;
    }
}

/// <summary>
/// Components manifest root
/// </summary>
public class ComponentsManifest
{
    [JsonPropertyName("components")]
    public List<ComponentManifestEntry> Components { get; set; } = new();
}

/// <summary>
/// Downloads components using GitHub Releases API with mirror fallback
/// </summary>
public class ComponentDownloader
{
    private readonly ILogger<ComponentDownloader> _logger;
    private readonly GitHubReleaseResolver _releaseResolver;
    private readonly HttpDownloader _downloader;
    private readonly string _componentsManifestPath;
    private ComponentsManifest? _cachedManifest;

    public ComponentDownloader(
        ILogger<ComponentDownloader> logger,
        GitHubReleaseResolver releaseResolver,
        HttpDownloader downloader,
        string componentsManifestPath)
    {
        _logger = logger;
        _releaseResolver = releaseResolver;
        _downloader = downloader;
        _componentsManifestPath = componentsManifestPath;
    }

    /// <summary>
    /// Load components manifest
    /// </summary>
    public async Task<ComponentsManifest> LoadManifestAsync(CancellationToken ct = default)
    {
        if (_cachedManifest != null)
        {
            return _cachedManifest;
        }

        if (!File.Exists(_componentsManifestPath))
        {
            throw new FileNotFoundException($"Components manifest not found: {_componentsManifestPath}");
        }

        var json = await File.ReadAllTextAsync(_componentsManifestPath, ct);
        _cachedManifest = JsonSerializer.Deserialize<ComponentsManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize components manifest");

        return _cachedManifest;
    }

    /// <summary>
    /// Download a component, resolving URLs via GitHub API and falling back to mirrors
    /// </summary>
    /// <param name="componentId">Component ID from manifest</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="customUrl">Optional custom URL to try first</param>
    /// <param name="localFilePath">Optional local file to import instead of downloading</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<DownloadResult> DownloadComponentAsync(
        string componentId,
        string outputPath,
        string? customUrl = null,
        string? localFilePath = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        var manifest = await LoadManifestAsync(ct);
        var component = manifest.Components.FirstOrDefault(c => c.Id == componentId);
        
        if (component == null)
        {
            throw new ArgumentException($"Component '{componentId}' not found in manifest");
        }

        // Handle local file import
        if (!string.IsNullOrEmpty(localFilePath))
        {
            _logger.LogInformation("Importing component {Component} from local file: {LocalPath}", 
                componentId, localFilePath);
            
            var (success, sha256) = await _downloader.ImportLocalFileAsync(
                localFilePath, outputPath, null, progress, ct);
            
            return new DownloadResult
            {
                Success = success,
                DownloadedUrl = localFilePath,
                IsLocalFile = true,
                Sha256 = sha256
            };
        }

        // Build candidate URLs
        var candidateUrls = new List<string>();
        var urlSources = new List<string>();

        // 1. Try custom URL if provided
        if (!string.IsNullOrEmpty(customUrl))
        {
            candidateUrls.Add(customUrl);
            urlSources.Add("custom");
            _logger.LogInformation("Adding custom URL: {Url}", customUrl);
        }

        // 2. Try to resolve from GitHub Releases API
        if (!string.IsNullOrEmpty(component.GitHubRepo) && !component.IsGitRepo)
        {
            var assetPattern = component.GetAssetPatternForPlatform();
            if (!string.IsNullOrEmpty(assetPattern))
            {
                var resolvedUrl = await _releaseResolver.ResolveLatestAssetUrlAsync(
                    component.GitHubRepo, assetPattern, ct);
                
                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    candidateUrls.Add(resolvedUrl);
                    urlSources.Add("github-api");
                    _logger.LogInformation("Resolved GitHub release URL: {Url}", resolvedUrl);
                }
            }
        }

        // 3. Add mirrors
        if (component.Mirrors.Count > 0)
        {
            candidateUrls.AddRange(component.Mirrors);
            urlSources.AddRange(Enumerable.Repeat("mirror", component.Mirrors.Count));
            _logger.LogInformation("Added {Count} mirror URLs", component.Mirrors.Count);
        }

        if (candidateUrls.Count == 0)
        {
            throw new InvalidOperationException(
                $"No download URLs available for component '{componentId}'. " +
                "GitHub API resolution failed and no mirrors configured.");
        }

        // Try downloading from each URL
        Exception? lastException = null;
        var attemptedUrls = new List<(string Url, string Source, Exception? Error)>();

        for (int i = 0; i < candidateUrls.Count; i++)
        {
            var url = candidateUrls[i];
            var source = urlSources[i];
            
            try
            {
                _logger.LogInformation("Attempting download from {Source}: {Url}", source, url);
                
                var success = await _downloader.DownloadFileAsync(
                    url, outputPath, null, progress, ct);
                
                if (success)
                {
                    _logger.LogInformation("Successfully downloaded component {Component} from {Source}", 
                        componentId, source);
                    
                    return new DownloadResult
                    {
                        Success = true,
                        DownloadedUrl = url,
                        Source = source,
                        AttemptedUrls = attemptedUrls
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Download failed from {Source} ({Url})", source, url);
                lastException = ex;
                attemptedUrls.Add((url, source, ex));
            }
        }

        // All URLs failed
        _logger.LogError("All download attempts failed for component {Component}", componentId);
        
        return new DownloadResult
        {
            Success = false,
            Error = lastException,
            AttemptedUrls = attemptedUrls
        };
    }

    /// <summary>
    /// Get the primary resolved URL for a component (for display in UI)
    /// </summary>
    public async Task<ResolvedComponentUrl> ResolveComponentUrlAsync(
        string componentId,
        CancellationToken ct = default)
    {
        var manifest = await LoadManifestAsync(ct);
        var component = manifest.Components.FirstOrDefault(c => c.Id == componentId);
        
        if (component == null)
        {
            throw new ArgumentException($"Component '{componentId}' not found in manifest");
        }

        string? resolvedUrl = null;
        string source = "none";

        // Try GitHub API first
        if (!string.IsNullOrEmpty(component.GitHubRepo) && !component.IsGitRepo)
        {
            var assetPattern = component.GetAssetPatternForPlatform();
            if (!string.IsNullOrEmpty(assetPattern))
            {
                resolvedUrl = await _releaseResolver.ResolveLatestAssetUrlAsync(
                    component.GitHubRepo, assetPattern, ct);
                
                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    source = "github-api";
                }
            }
        }

        // Fallback to first mirror
        if (string.IsNullOrEmpty(resolvedUrl) && component.Mirrors.Count > 0)
        {
            resolvedUrl = component.Mirrors[0];
            source = "mirror";
        }

        return new ResolvedComponentUrl
        {
            ComponentId = componentId,
            Url = resolvedUrl,
            Source = source,
            Mirrors = component.Mirrors,
            GitHubRepo = component.GitHubRepo
        };
    }
}

/// <summary>
/// Result of a download operation
/// </summary>
public class DownloadResult
{
    public bool Success { get; set; }
    public string? DownloadedUrl { get; set; }
    public string? Source { get; set; }
    public bool IsLocalFile { get; set; }
    public string? Sha256 { get; set; }
    public Exception? Error { get; set; }
    public List<(string Url, string Source, Exception? Error)> AttemptedUrls { get; set; } = new();
}

/// <summary>
/// Resolved URL information for a component
/// </summary>
public class ResolvedComponentUrl
{
    public string ComponentId { get; set; } = "";
    public string? Url { get; set; }
    public string Source { get; set; } = "";
    public List<string> Mirrors { get; set; } = new();
    public string? GitHubRepo { get; set; }
}
