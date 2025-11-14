using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// Resolves GitHub release asset URLs using the GitHub Releases API
/// </summary>
public class GitHubReleaseResolver
{
    private readonly ILogger<GitHubReleaseResolver> _logger;
    private readonly HttpClient _httpClient;
    private const string GitHubApiBaseUrl = "https://api.github.com";

    public GitHubReleaseResolver(ILogger<GitHubReleaseResolver> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Set User-Agent header required by GitHub API
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Aura-Video-Studio");
        }
    }

    /// <summary>
    /// Resolve the download URL for a component's latest release asset
    /// </summary>
    /// <param name="githubRepo">GitHub repository in format "owner/repo"</param>
    /// <param name="assetPattern">Pattern to match against asset names (supports wildcards)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Resolved asset URL or null if not found</returns>
    public async Task<string?> ResolveLatestAssetUrlAsync(
        string githubRepo, 
        string assetPattern, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Resolving latest release asset for {Repo} matching pattern {Pattern}", 
                githubRepo, assetPattern);

            var apiUrl = $"{GitHubApiBaseUrl}/repos/{githubRepo}/releases/latest";
            
            var response = await _httpClient.GetAsync(apiUrl, ct).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {StatusCode} for {Repo}", 
                    response.StatusCode, githubRepo);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var release = JsonSerializer.Deserialize<GitHubRelease>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (release?.Assets == null || release.Assets.Length == 0)
            {
                _logger.LogWarning("No assets found in latest release for {Repo}", githubRepo);
                return null;
            }

            // Convert wildcard pattern to regex
            var regexPattern = ConvertWildcardToRegex(assetPattern);
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            // Find matching asset
            var matchingAsset = release.Assets.FirstOrDefault(a => regex.IsMatch(a.Name));

            if (matchingAsset == null)
            {
                _logger.LogWarning("No asset matching pattern {Pattern} found in release {Tag} for {Repo}", 
                    assetPattern, release.TagName, githubRepo);
                _logger.LogDebug("Available assets: {Assets}", 
                    string.Join(", ", release.Assets.Select(a => a.Name)));
                return null;
            }

            _logger.LogInformation("Resolved asset URL: {Url} (size: {Size} bytes)", 
                matchingAsset.BrowserDownloadUrl, matchingAsset.Size);

            return matchingAsset.BrowserDownloadUrl;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while resolving GitHub release for {Repo}", githubRepo);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse GitHub API response for {Repo}", githubRepo);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resolving GitHub release for {Repo}", githubRepo);
            return null;
        }
    }

    /// <summary>
    /// Get release information including all assets
    /// </summary>
    public async Task<GitHubRelease?> GetLatestReleaseAsync(
        string githubRepo, 
        CancellationToken ct = default)
    {
        try
        {
            var apiUrl = $"{GitHubApiBaseUrl}/repos/{githubRepo}/releases/latest";
            var response = await _httpClient.GetAsync(apiUrl, ct).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<GitHubRelease>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest release for {Repo}", githubRepo);
            return null;
        }
    }

    /// <summary>
    /// Convert wildcard pattern (with * and ?) to regex pattern
    /// </summary>
    private static string ConvertWildcardToRegex(string wildcardPattern)
    {
        // Escape special regex characters except * and ?
        var escaped = Regex.Escape(wildcardPattern);
        
        // Convert * to .* (match any characters)
        escaped = escaped.Replace("\\*", ".*");
        
        // Convert ? to . (match single character)
        escaped = escaped.Replace("\\?", ".");
        
        // Anchor pattern to start and end
        return $"^{escaped}$";
    }
}

/// <summary>
/// GitHub Release API response model
/// </summary>
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
    
    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
}

/// <summary>
/// GitHub Release Asset model
/// </summary>
public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";
    
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "";
}
