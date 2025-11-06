using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for settings export and import with security-first design
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsExportImportController : ControllerBase
{
    private readonly ILogger<SettingsExportImportController> _logger;
    private readonly SettingsExportImportService _exportImportService;
    private readonly string _userSettingsFilePath;

    public SettingsExportImportController(
        ILogger<SettingsExportImportController> logger,
        SettingsExportImportService exportImportService,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _exportImportService = exportImportService;
        
        var auraDataDir = providerSettings.GetAuraDataDirectory();
        _userSettingsFilePath = System.IO.Path.Combine(auraDataDir, "user-settings.json");
    }

    /// <summary>
    /// Get preview of what would be redacted in export
    /// </summary>
    [HttpGet("export/preview")]
    public async Task<IActionResult> GetExportPreview(CancellationToken ct)
    {
        try
        {
            var preview = await _exportImportService.GetRedactionPreviewAsync(ct);

            return Ok(new RedactionPreviewResponse(
                preview.TotalSecrets,
                preview.AvailableKeys,
                preview.PreviewMasked));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export preview");
            return StatusCode(500, new { error = "Failed to generate export preview" });
        }
    }

    /// <summary>
    /// Export settings with optional secret inclusion (requires explicit opt-in)
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportSettings(
        [FromBody] ExportSettingsRequest request,
        CancellationToken ct)
    {
        try
        {
            var userSettings = await LoadUserSettingsAsync(ct);

            var result = await _exportImportService.ExportSettingsAsync(
                userSettings,
                request.IncludeSecrets,
                request.SelectedSecretKeys,
                request.AcknowledgeWarning,
                ct);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Error });
            }

            if (result.Metadata?.SecretsIncluded == true)
            {
                _logger.LogWarning(
                    "User exported settings WITH secrets. Keys: {Keys}, CorrelationId: {CorrelationId}",
                    string.Join(", ", request.SelectedSecretKeys ?? new List<string>()),
                    HttpContext.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation(
                    "User exported settings WITHOUT secrets (secure default), CorrelationId: {CorrelationId}",
                    HttpContext.TraceIdentifier);
            }

            var response = new ExportSettingsResponse(
                result.Data!.Version,
                result.Data.ExportedAt,
                MapToSettingsExportData(result.Data),
                MapToExportMetadataDto(result.Metadata!));

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting settings, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new { error = "Failed to export settings" });
        }
    }

    /// <summary>
    /// Import settings with dry-run mode (default) for conflict detection
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportSettings(
        [FromBody] ImportSettingsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Version))
            {
                return BadRequest(new { error = "Version is required" });
            }

            var currentSettings = await LoadUserSettingsAsync(ct);
            var importData = MapFromSettingsExportData(request.Settings);

            var result = await _exportImportService.ImportSettingsAsync(
                importData,
                currentSettings,
                request.DryRun,
                request.OverwriteExisting,
                ct);

            if (result.DryRun)
            {
                _logger.LogInformation(
                    "Dry-run import completed. Conflicts: {Count}, CorrelationId: {CorrelationId}",
                    result.Conflicts.Count,
                    HttpContext.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation(
                    "Settings imported. Conflicts resolved: {Count}, CorrelationId: {CorrelationId}",
                    result.Conflicts.Count,
                    HttpContext.TraceIdentifier);

                await SaveUserSettingsAsync(currentSettings, ct);
            }

            var conflicts = result.Conflicts.Count > 0
                ? MapToImportConflictSummary(result.Conflicts)
                : null;

            var response = new ImportSettingsResponse(
                result.Success,
                result.Message,
                conflicts,
                new List<string>(),
                result.RequiresConfirmation ? new List<string> { "Conflicts detected. Review and confirm." } : new List<string>());

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing settings, CorrelationId: {CorrelationId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new { error = "Failed to import settings" });
        }
    }

    private async Task<UserSettings> LoadUserSettingsAsync(CancellationToken ct)
    {
        if (!System.IO.File.Exists(_userSettingsFilePath))
        {
            return new UserSettings();
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(_userSettingsFilePath, ct);
            return System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading user settings file, using defaults");
            return new UserSettings();
        }
    }

    private async Task SaveUserSettingsAsync(UserSettings settings, CancellationToken ct)
    {
        var directory = System.IO.Path.GetDirectoryName(_userSettingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };

        var json = System.Text.Json.JsonSerializer.Serialize(settings, options);
        await System.IO.File.WriteAllTextAsync(_userSettingsFilePath, json, ct);
    }

    private SettingsExportData MapToSettingsExportData(ExportData data)
    {
        return new SettingsExportData(
            new GeneralSettingsDto(
                data.General.DefaultProjectSaveLocation,
                data.General.AutosaveIntervalSeconds,
                data.General.AutosaveEnabled,
                data.General.Language,
                data.General.Theme.ToString(),
                data.General.AdvancedModeEnabled),
            data.ApiKeys,
            new ProviderPathsDto(
                data.Advanced.StableDiffusionUrl,
                data.Advanced.OllamaUrl,
                data.FileLocations.FFmpegPath,
                data.FileLocations.FFprobePath,
                data.FileLocations.OutputDirectory),
            new Dictionary<string, object>
            {
                ["videoDefaults"] = data.VideoDefaults,
                ["editorPreferences"] = data.EditorPreferences,
                ["ui"] = data.UI
            });
    }

    private ExportMetadata MapToExportMetadataDto(ExportMetadataCore metadata)
    {
        return new ExportMetadata(
            metadata.SecretsIncluded,
            metadata.IncludedSecretKeys,
            metadata.RedactedSecretKeys,
            metadata.ExportedBy,
            metadata.MachineName);
    }

    private ExportData MapFromSettingsExportData(SettingsExportData data)
    {
        var exportData = new ExportData
        {
            General = new GeneralSettings
            {
                DefaultProjectSaveLocation = data.General.DefaultProjectSaveLocation,
                AutosaveIntervalSeconds = data.General.AutosaveIntervalSeconds,
                AutosaveEnabled = data.General.AutosaveEnabled,
                Language = data.General.Language,
                Theme = Enum.TryParse<ThemeMode>(data.General.Theme, out var theme) ? theme : ThemeMode.Auto,
                AdvancedModeEnabled = data.General.AdvancedModeEnabled
            },
            ApiKeys = data.ApiKeys,
            FileLocations = new FileLocationsSettings
            {
                FFmpegPath = data.ProviderPaths.FFmpegPath ?? string.Empty,
                FFprobePath = data.ProviderPaths.FFprobePath ?? string.Empty,
                OutputDirectory = data.ProviderPaths.OutputDirectory ?? string.Empty
            },
            Advanced = new AdvancedSettings
            {
                StableDiffusionUrl = data.ProviderPaths.StableDiffusionUrl ?? "http://127.0.0.1:7860",
                OllamaUrl = data.ProviderPaths.OllamaUrl ?? "http://127.0.0.1:11434"
            }
        };

        if (data.AdditionalSettings.TryGetValue("videoDefaults", out var videoDefaults))
        {
            var vd = System.Text.Json.JsonSerializer.Deserialize<VideoDefaultsSettings>(
                System.Text.Json.JsonSerializer.Serialize(videoDefaults));
            if (vd != null) exportData.VideoDefaults = vd;
        }

        if (data.AdditionalSettings.TryGetValue("editorPreferences", out var editorPrefs))
        {
            var ep = System.Text.Json.JsonSerializer.Deserialize<EditorPreferencesSettings>(
                System.Text.Json.JsonSerializer.Serialize(editorPrefs));
            if (ep != null) exportData.EditorPreferences = ep;
        }

        if (data.AdditionalSettings.TryGetValue("ui", out var ui))
        {
            var uiSettings = System.Text.Json.JsonSerializer.Deserialize<UISettings>(
                System.Text.Json.JsonSerializer.Serialize(ui));
            if (uiSettings != null) exportData.UI = uiSettings;
        }

        return exportData;
    }

    private ImportConflictSummary MapToImportConflictSummary(List<ImportConflict> conflicts)
    {
        var general = conflicts.Where(c => c.Category == "General")
            .Select(c => new ConflictItem(c.Key, c.CurrentValue, c.NewValue, ConflictResolution.UseNew))
            .ToList();

        var apiKeys = conflicts.Where(c => c.Category == "ApiKeys")
            .Select(c => new ConflictItem(c.Key, c.CurrentValue, c.NewValue, ConflictResolution.KeepCurrent))
            .ToList();

        var providerPaths = conflicts.Where(c => c.Category == "FileLocations")
            .Select(c => new ConflictItem(c.Key, c.CurrentValue, c.NewValue, ConflictResolution.KeepCurrent))
            .ToList();

        return new ImportConflictSummary(general, apiKeys, providerPaths, conflicts.Count);
    }
}
