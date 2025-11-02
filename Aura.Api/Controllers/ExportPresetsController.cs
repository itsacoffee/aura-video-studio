using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing export presets and format-specific settings
/// </summary>
[ApiController]
[Route("api/export-presets")]
public class ExportPresetsController : ControllerBase
{
    private readonly ILogger<ExportPresetsController> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly string _presetsPath;

    public ExportPresetsController(
        ILogger<ExportPresetsController> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;

        var auraDataDir = _providerSettings.GetAuraDataDirectory();
        _presetsPath = Path.Combine(auraDataDir, "export-presets.json");
    }

    /// <summary>
    /// Get all built-in and custom export presets
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetExportPresets(CancellationToken ct)
    {
        try
        {
            var builtInPresets = GetBuiltInPresets();
            var customPresets = await LoadCustomPresetsAsync(ct);

            return Ok(new
            {
                builtIn = builtInPresets,
                custom = customPresets,
                totalCount = builtInPresets.Count + customPresets.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export presets");
            return StatusCode(500, new { error = "Failed to get export presets" });
        }
    }

    /// <summary>
    /// Get a specific export preset by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExportPreset(string id, CancellationToken ct)
    {
        try
        {
            var builtIn = GetBuiltInPresets().FirstOrDefault(p => p.Id == id);
            if (builtIn != null)
            {
                return Ok(builtIn);
            }

            var custom = (await LoadCustomPresetsAsync(ct)).FirstOrDefault(p => p.Id == id);
            if (custom != null)
            {
                return Ok(custom);
            }

            return NotFound(new { error = $"Preset {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export preset: {PresetId}", id);
            return StatusCode(500, new { error = "Failed to get export preset" });
        }
    }

    /// <summary>
    /// Create a new custom export preset
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateExportPreset(
        [FromBody] ExportPresetModel preset,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                return BadRequest(new { error = "Preset name is required" });
            }

            if (GetBuiltInPresets().Any(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { error = "Cannot override built-in presets" });
            }

            preset.Id = Guid.NewGuid().ToString();
            preset.CreatedAt = DateTime.UtcNow;
            preset.IsBuiltIn = false;

            var customPresets = await LoadCustomPresetsAsync(ct);
            customPresets.Add(preset);
            await SaveCustomPresetsAsync(customPresets, ct);

            _logger.LogInformation("Created export preset: {PresetName}", preset.Name);

            return CreatedAtAction(nameof(GetExportPreset), new { id = preset.Id }, preset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating export preset");
            return StatusCode(500, new { error = "Failed to create export preset" });
        }
    }

    /// <summary>
    /// Update an existing custom export preset
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExportPreset(
        string id,
        [FromBody] ExportPresetModel preset,
        CancellationToken ct)
    {
        try
        {
            if (GetBuiltInPresets().Any(p => p.Id == id))
            {
                return BadRequest(new { error = "Cannot modify built-in presets" });
            }

            var customPresets = await LoadCustomPresetsAsync(ct);
            var existing = customPresets.FirstOrDefault(p => p.Id == id);

            if (existing == null)
            {
                return NotFound(new { error = $"Preset {id} not found" });
            }

            preset.Id = id;
            preset.CreatedAt = existing.CreatedAt;
            preset.UpdatedAt = DateTime.UtcNow;
            preset.IsBuiltIn = false;

            var index = customPresets.IndexOf(existing);
            customPresets[index] = preset;

            await SaveCustomPresetsAsync(customPresets, ct);

            _logger.LogInformation("Updated export preset: {PresetId}", id);

            return Ok(preset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating export preset: {PresetId}", id);
            return StatusCode(500, new { error = "Failed to update export preset" });
        }
    }

    /// <summary>
    /// Delete a custom export preset
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExportPreset(string id, CancellationToken ct)
    {
        try
        {
            if (GetBuiltInPresets().Any(p => p.Id == id))
            {
                return BadRequest(new { error = "Cannot delete built-in presets" });
            }

            var customPresets = await LoadCustomPresetsAsync(ct);
            var existing = customPresets.FirstOrDefault(p => p.Id == id);

            if (existing == null)
            {
                return NotFound(new { error = $"Preset {id} not found" });
            }

            customPresets.Remove(existing);
            await SaveCustomPresetsAsync(customPresets, ct);

            _logger.LogInformation("Deleted export preset: {PresetId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting export preset: {PresetId}", id);
            return StatusCode(500, new { error = "Failed to delete export preset" });
        }
    }

    /// <summary>
    /// Get format-specific recommendations
    /// </summary>
    [HttpGet("format-recommendations/{format}")]
    public IActionResult GetFormatRecommendations(string format)
    {
        try
        {
            var recommendations = format.ToLowerInvariant() switch
            {
                "youtube" => new
                {
                    resolution = "1920x1080",
                    frameRate = 30,
                    codec = "libx264",
                    bitrate = "8M",
                    audioCodec = "aac",
                    audioBitrate = "192k",
                    notes = "Optimized for YouTube: 1080p30 with high bitrate for best quality"
                },
                "instagram" => new
                {
                    resolution = "1080x1920",
                    frameRate = 30,
                    codec = "libx264",
                    bitrate = "5M",
                    audioCodec = "aac",
                    audioBitrate = "128k",
                    notes = "Optimized for Instagram Stories/Reels: Vertical 9:16 format"
                },
                "tiktok" => new
                {
                    resolution = "1080x1920",
                    frameRate = 30,
                    codec = "libx264",
                    bitrate = "5M",
                    audioCodec = "aac",
                    audioBitrate = "128k",
                    notes = "Optimized for TikTok: Vertical format with good compression"
                },
                "twitter" => new
                {
                    resolution = "1280x720",
                    frameRate = 30,
                    codec = "libx264",
                    bitrate = "5M",
                    audioCodec = "aac",
                    audioBitrate = "128k",
                    notes = "Optimized for Twitter: 720p with 512MB file size limit in mind"
                },
                "web" => new
                {
                    resolution = "1280x720",
                    frameRate = 30,
                    codec = "libx264",
                    bitrate = "3M",
                    audioCodec = "aac",
                    audioBitrate = "128k",
                    notes = "Optimized for web playback: Good balance of quality and file size"
                },
                "4k" => new
                {
                    resolution = "3840x2160",
                    frameRate = 30,
                    codec = "libx265",
                    bitrate = "25M",
                    audioCodec = "aac",
                    audioBitrate = "256k",
                    notes = "4K Ultra HD with HEVC for efficient compression"
                },
                _ => null
            };

            if (recommendations == null)
            {
                return NotFound(new { error = $"No recommendations available for format: {format}" });
            }

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting format recommendations: {Format}", format);
            return StatusCode(500, new { error = "Failed to get format recommendations" });
        }
    }

    private List<ExportPresetModel> GetBuiltInPresets()
    {
        return new List<ExportPresetModel>
        {
            new()
            {
                Id = "youtube-1080p",
                Name = "YouTube 1080p",
                Description = "Optimized for YouTube uploads at 1080p",
                Resolution = "1920x1080",
                FrameRate = 30,
                Codec = "libx264",
                Bitrate = "8M",
                AudioCodec = "aac",
                AudioBitrate = "192k",
                AudioSampleRate = 48000,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "social-media"
            },
            new()
            {
                Id = "youtube-4k",
                Name = "YouTube 4K",
                Description = "Optimized for YouTube uploads at 4K",
                Resolution = "3840x2160",
                FrameRate = 30,
                Codec = "libx265",
                Bitrate = "25M",
                AudioCodec = "aac",
                AudioBitrate = "256k",
                AudioSampleRate = 48000,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "social-media"
            },
            new()
            {
                Id = "instagram-story",
                Name = "Instagram Story",
                Description = "Vertical format for Instagram Stories",
                Resolution = "1080x1920",
                FrameRate = 30,
                Codec = "libx264",
                Bitrate = "5M",
                AudioCodec = "aac",
                AudioBitrate = "128k",
                AudioSampleRate = 44100,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "social-media"
            },
            new()
            {
                Id = "tiktok",
                Name = "TikTok",
                Description = "Vertical format for TikTok",
                Resolution = "1080x1920",
                FrameRate = 30,
                Codec = "libx264",
                Bitrate = "5M",
                AudioCodec = "aac",
                AudioBitrate = "128k",
                AudioSampleRate = 44100,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "social-media"
            },
            new()
            {
                Id = "twitter",
                Name = "Twitter",
                Description = "Optimized for Twitter with file size limits",
                Resolution = "1280x720",
                FrameRate = 30,
                Codec = "libx264",
                Bitrate = "5M",
                AudioCodec = "aac",
                AudioBitrate = "128k",
                AudioSampleRate = 44100,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "social-media"
            },
            new()
            {
                Id = "web-720p",
                Name = "Web 720p",
                Description = "Optimized for web playback",
                Resolution = "1280x720",
                FrameRate = 30,
                Codec = "libx264",
                Bitrate = "3M",
                AudioCodec = "aac",
                AudioBitrate = "128k",
                AudioSampleRate = 44100,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "web"
            },
            new()
            {
                Id = "archival-4k",
                Name = "Archival 4K",
                Description = "Maximum quality for archival purposes",
                Resolution = "3840x2160",
                FrameRate = 30,
                Codec = "libx265",
                Bitrate = "40M",
                AudioCodec = "flac",
                AudioBitrate = "0",
                AudioSampleRate = 48000,
                Format = "mkv",
                IsBuiltIn = true,
                Category = "archival"
            },
            new()
            {
                Id = "draft-preview",
                Name = "Draft Preview",
                Description = "Fast preview for review purposes",
                Resolution = "1280x720",
                FrameRate = 30,
                Codec = "libx264",
                Preset = "ultrafast",
                Bitrate = "2M",
                AudioCodec = "aac",
                AudioBitrate = "96k",
                AudioSampleRate = 44100,
                Format = "mp4",
                IsBuiltIn = true,
                Category = "preview"
            }
        };
    }

    private async Task<List<ExportPresetModel>> LoadCustomPresetsAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_presetsPath))
        {
            return new List<ExportPresetModel>();
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_presetsPath, ct);
            return JsonSerializer.Deserialize<List<ExportPresetModel>>(json) ?? new List<ExportPresetModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading custom presets");
            return new List<ExportPresetModel>();
        }
    }

    private async Task SaveCustomPresetsAsync(List<ExportPresetModel> presets, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_presetsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(presets, options);
        await System.IO.File.WriteAllTextAsync(_presetsPath, json, ct);
    }
}

/// <summary>
/// Export preset model
/// </summary>
public class ExportPresetModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resolution { get; set; } = "1920x1080";
    public int FrameRate { get; set; } = 30;
    public string Codec { get; set; } = "libx264";
    public string? Preset { get; set; }
    public string Bitrate { get; set; } = "5M";
    public string AudioCodec { get; set; } = "aac";
    public string AudioBitrate { get; set; } = "192k";
    public int AudioSampleRate { get; set; } = 44100;
    public string Format { get; set; } = "mp4";
    public bool IsBuiltIn { get; set; }
    public string Category { get; set; } = "custom";
    public Dictionary<string, string> CustomSettings { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
