using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Aura.Core.Services.AudioIntelligence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for music and sound effects library operations
/// </summary>
[ApiController]
[Route("api/music-library")]
public class MusicLibraryController : ControllerBase
{
    private readonly ILogger<MusicLibraryController> _logger;
    private readonly IEnumerable<IMusicProvider> _musicProviders;
    private readonly IEnumerable<ISfxProvider> _sfxProviders;
    private readonly LicensingService _licensingService;

    public MusicLibraryController(
        ILogger<MusicLibraryController> logger,
        IEnumerable<IMusicProvider> musicProviders,
        IEnumerable<ISfxProvider> sfxProviders,
        LicensingService licensingService)
    {
        _logger = logger;
        _musicProviders = musicProviders;
        _sfxProviders = sfxProviders;
        _licensingService = licensingService;
    }

    /// <summary>
    /// Search for music tracks
    /// </summary>
    [HttpPost("music/search")]
    public async Task<IActionResult> SearchMusic(
        [FromBody] MusicSearchCriteria criteria,
        [FromQuery] string? provider = null,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[{CorrelationId}] Searching music with criteria", correlationId);

            var targetProvider = provider != null
                ? GetMusicProvider(provider)
                : await GetFirstAvailableMusicProviderAsync(ct).ConfigureAwait(false);

            if (targetProvider == null)
                return Problem("No music provider available", statusCode: 503);

            var results = await targetProvider.SearchAsync(criteria, ct).ConfigureAwait(false);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching music");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Get a specific music track by ID
    /// </summary>
    [HttpGet("music/{provider}/{assetId}")]
    public async Task<IActionResult> GetMusicTrack(
        string provider,
        string assetId,
        CancellationToken ct = default)
    {
        try
        {
            var musicProvider = GetMusicProvider(provider);
            if (musicProvider == null)
                return NotFound($"Music provider '{provider}' not found");

            var asset = await musicProvider.GetByIdAsync(assetId, ct).ConfigureAwait(false);
            if (asset == null)
                return NotFound($"Music track '{assetId}' not found");

            return Ok(asset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting music track");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Get preview URL for music track
    /// </summary>
    [HttpGet("music/{provider}/{assetId}/preview")]
    public async Task<IActionResult> GetMusicPreview(
        string provider,
        string assetId,
        CancellationToken ct = default)
    {
        try
        {
            var musicProvider = GetMusicProvider(provider);
            if (musicProvider == null)
                return NotFound($"Music provider '{provider}' not found");

            var previewUrl = await musicProvider.GetPreviewUrlAsync(assetId, ct).ConfigureAwait(false);
            if (previewUrl == null)
                return NotFound($"Preview not available for track '{assetId}'");

            return Ok(new { PreviewUrl = previewUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting music preview");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Search for sound effects
    /// </summary>
    [HttpPost("sfx/search")]
    public async Task<IActionResult> SearchSfx(
        [FromBody] SfxSearchCriteria criteria,
        [FromQuery] string? provider = null,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation("[{CorrelationId}] Searching SFX with criteria", correlationId);

            var targetProvider = provider != null
                ? GetSfxProvider(provider)
                : await GetFirstAvailableSfxProviderAsync(ct).ConfigureAwait(false);

            if (targetProvider == null)
                return Problem("No SFX provider available", statusCode: 503);

            var results = await targetProvider.SearchAsync(criteria, ct).ConfigureAwait(false);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching SFX");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Find sound effects by tags
    /// </summary>
    [HttpPost("sfx/find-by-tags")]
    public async Task<IActionResult> FindSfxByTags(
        [FromBody] List<string> tags,
        [FromQuery] string? provider = null,
        [FromQuery] int maxResults = 20,
        CancellationToken ct = default)
    {
        try
        {
            var targetProvider = provider != null
                ? GetSfxProvider(provider)
                : await GetFirstAvailableSfxProviderAsync(ct).ConfigureAwait(false);

            if (targetProvider == null)
                return Problem("No SFX provider available", statusCode: 503);

            var results = await targetProvider.FindByTagsAsync(tags, maxResults, ct).ConfigureAwait(false);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding SFX by tags");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Get SFX preview URL
    /// </summary>
    [HttpGet("sfx/{provider}/{assetId}/preview")]
    public async Task<IActionResult> GetSfxPreview(
        string provider,
        string assetId,
        CancellationToken ct = default)
    {
        try
        {
            var sfxProvider = GetSfxProvider(provider);
            if (sfxProvider == null)
                return NotFound($"SFX provider '{provider}' not found");

            var previewUrl = await sfxProvider.GetPreviewUrlAsync(assetId, ct).ConfigureAwait(false);
            if (previewUrl == null)
                return NotFound($"Preview not available for SFX '{assetId}'");

            return Ok(new { PreviewUrl = previewUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SFX preview");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Get licensing summary for a job
    /// </summary>
    [HttpGet("licensing/{jobId}")]
    public async Task<IActionResult> GetLicensingSummary(
        string jobId,
        CancellationToken ct = default)
    {
        try
        {
            var summary = await _licensingService.GetLicensingSummaryAsync(jobId, ct).ConfigureAwait(false);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting licensing summary");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Export licensing information
    /// </summary>
    [HttpPost("licensing/export")]
    public async Task<IActionResult> ExportLicensing(
        [FromBody] LicenseExportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var content = request.Format switch
            {
                LicenseExportFormat.CSV => await _licensingService.ExportToCsvAsync(
                    request.JobId, request.IncludeUnused, ct).ConfigureAwait(false),
                LicenseExportFormat.JSON => await _licensingService.ExportToJsonAsync(
                    request.JobId, request.IncludeUnused, ct).ConfigureAwait(false),
                LicenseExportFormat.Text => await _licensingService.ExportToTextAsync(
                    request.JobId, ct).ConfigureAwait(false),
                LicenseExportFormat.HTML => await _licensingService.ExportToHtmlAsync(
                    request.JobId, ct).ConfigureAwait(false),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };

            var contentType = request.Format switch
            {
                LicenseExportFormat.CSV => "text/csv",
                LicenseExportFormat.JSON => "application/json",
                LicenseExportFormat.Text => "text/plain",
                LicenseExportFormat.HTML => "text/html",
                _ => "text/plain"
            };

            var fileName = $"licensing-{request.JobId}.{request.Format.ToString().ToLowerInvariant()}";

            return File(System.Text.Encoding.UTF8.GetBytes(content), contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting licensing");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Validate licensing for commercial use
    /// </summary>
    [HttpGet("licensing/{jobId}/validate")]
    public async Task<IActionResult> ValidateLicensing(
        string jobId,
        CancellationToken ct = default)
    {
        try
        {
            var (isValid, issues) = await _licensingService.ValidateForCommercialUseAsync(jobId, ct).ConfigureAwait(false);

            return Ok(new
            {
                IsValid = isValid,
                Issues = issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating licensing");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Track asset usage for licensing
    /// </summary>
    [HttpPost("licensing/{jobId}/track")]
    public IActionResult TrackAssetUsage(
        string jobId,
        [FromBody] TrackAssetRequest request)
    {
        try
        {
            _licensingService.TrackAssetUsage(
                jobId,
                request.Asset,
                request.SceneIndex,
                request.StartTime,
                request.Duration,
                request.IsSelected);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking asset usage");
            return Problem(ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// List available music providers
    /// </summary>
    [HttpGet("providers/music")]
    public async Task<IActionResult> ListMusicProviders()
    {
        var providers = new List<object>();
        
        foreach (var provider in _musicProviders)
        {
            var isAvailable = await provider.IsAvailableAsync().ConfigureAwait(false);
            providers.Add(new
            {
                Name = provider.Name,
                IsAvailable = isAvailable
            });
        }

        return Ok(providers);
    }

    /// <summary>
    /// List available SFX providers
    /// </summary>
    [HttpGet("providers/sfx")]
    public async Task<IActionResult> ListSfxProviders()
    {
        var providers = new List<object>();
        
        foreach (var provider in _sfxProviders)
        {
            var isAvailable = await provider.IsAvailableAsync().ConfigureAwait(false);
            providers.Add(new
            {
                Name = provider.Name,
                IsAvailable = isAvailable
            });
        }

        return Ok(providers);
    }

    private IMusicProvider? GetMusicProvider(string name)
    {
        foreach (var provider in _musicProviders)
        {
            if (provider.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return provider;
        }
        return null;
    }

    private async Task<IMusicProvider?> GetFirstAvailableMusicProviderAsync(CancellationToken ct)
    {
        foreach (var provider in _musicProviders)
        {
            var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
            if (isAvailable)
                return provider;
        }
        return null;
    }

    private ISfxProvider? GetSfxProvider(string name)
    {
        foreach (var provider in _sfxProviders)
        {
            if (provider.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return provider;
        }
        return null;
    }

    private async Task<ISfxProvider?> GetFirstAvailableSfxProviderAsync(CancellationToken ct)
    {
        foreach (var provider in _sfxProviders)
        {
            var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
            if (isAvailable)
                return provider;
        }
        return null;
    }
}

/// <summary>
/// Request to track asset usage
/// </summary>
public record TrackAssetRequest(
    AudioAsset Asset,
    int SceneIndex,
    TimeSpan StartTime,
    TimeSpan Duration,
    bool IsSelected
);
