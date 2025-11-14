using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Configuration;

/// <summary>
/// Service for exporting and importing settings with security-first design
/// </summary>
public class SettingsExportImportService
{
    private readonly ILogger<SettingsExportImportService> _logger;
    private readonly ISecureStorageService _secureStorage;

    public SettingsExportImportService(
        ILogger<SettingsExportImportService> logger,
        ISecureStorageService secureStorage)
    {
        _logger = logger;
        _secureStorage = secureStorage;
    }

    /// <summary>
    /// Export settings with optional secret inclusion
    /// </summary>
    public async Task<ExportResult> ExportSettingsAsync(
        UserSettings userSettings,
        bool includeSecrets,
        List<string>? selectedSecretKeys,
        bool acknowledgeWarning,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting settings export. IncludeSecrets={IncludeSecrets}", includeSecrets);

        if (includeSecrets && !acknowledgeWarning)
        {
            return new ExportResult
            {
                Success = false,
                Error = "Must acknowledge security warning when exporting secrets"
            };
        }

        var exportData = new ExportData
        {
            Version = "1.0.0",
            ExportedAt = DateTime.UtcNow,
            General = userSettings.General,
            FileLocations = userSettings.FileLocations,
            VideoDefaults = userSettings.VideoDefaults,
            EditorPreferences = userSettings.EditorPreferences,
            UI = userSettings.UI,
            Advanced = userSettings.Advanced
        };

        // Handle API keys based on includeSecrets flag
        if (includeSecrets && selectedSecretKeys?.Count > 0)
        {
            exportData.ApiKeys = await LoadSelectedApiKeysAsync(selectedSecretKeys, ct).ConfigureAwait(false);
            _logger.LogWarning(
                "Exporting settings WITH secrets. Keys included: {Keys}. Warning acknowledged: {Acknowledged}",
                string.Join(", ", selectedSecretKeys),
                acknowledgeWarning);
        }
        else
        {
            exportData.ApiKeys = new Dictionary<string, string>();
            _logger.LogInformation("Exporting settings WITHOUT secrets (default secure behavior)");
        }

        return new ExportResult
        {
            Success = true,
            Data = exportData,
            Metadata = new ExportMetadataCore
            {
                SecretsIncluded = includeSecrets && (selectedSecretKeys?.Count ?? 0) > 0,
                IncludedSecretKeys = includeSecrets ? selectedSecretKeys : new List<string>(),
                RedactedSecretKeys = await GetAllSecretKeysAsync(ct).ConfigureAwait(false),
                ExportedBy = Environment.UserName,
                MachineName = Environment.MachineName
            }
        };
    }

    /// <summary>
    /// Import settings with dry-run and conflict detection
    /// </summary>
    public async Task<ImportResult> ImportSettingsAsync(
        ExportData importData,
        UserSettings currentSettings,
        bool dryRun,
        bool overwriteExisting,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting settings import. DryRun={DryRun}, Overwrite={Overwrite}", 
            dryRun, overwriteExisting);

        var result = new ImportResult
        {
            Success = true,
            DryRun = dryRun,
            Conflicts = new List<ImportConflict>()
        };

        // Detect conflicts
        DetectGeneralSettingsConflicts(importData.General, currentSettings.General, result.Conflicts);
        DetectFileLocationConflicts(importData.FileLocations, currentSettings.FileLocations, result.Conflicts);
        await DetectApiKeyConflictsAsync(importData.ApiKeys, result.Conflicts, ct).ConfigureAwait(false);

        if (result.Conflicts.Count > 0 && !overwriteExisting)
        {
            result.RequiresConfirmation = true;
            result.Message = $"Found {result.Conflicts.Count} conflicts. Please review and confirm.";
            _logger.LogInformation("Import detected {Count} conflicts", result.Conflicts.Count);
        }

        if (dryRun)
        {
            result.Message = "Dry-run completed. No changes applied.";
            _logger.LogInformation("Dry-run completed with {Conflicts} conflicts detected", result.Conflicts.Count);
            return result;
        }

        // Apply changes if not dry-run
        if (!dryRun && (overwriteExisting || result.Conflicts.Count == 0))
        {
            await ApplyImportAsync(importData, currentSettings, ct).ConfigureAwait(false);
            result.Message = "Settings imported successfully";
            _logger.LogInformation("Settings import completed successfully");
        }

        return result;
    }

    /// <summary>
    /// Get preview of what would be redacted
    /// </summary>
    public async Task<RedactionPreview> GetRedactionPreviewAsync(CancellationToken ct)
    {
        var allKeys = await GetAllSecretKeysAsync(ct).ConfigureAwait(false);
        var preview = new Dictionary<string, string>();

        foreach (var key in allKeys)
        {
            var value = await _secureStorage.GetApiKeyAsync(key).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(value))
            {
                preview[key] = MaskSecret(value);
            }
        }

        return new RedactionPreview
        {
            TotalSecrets = allKeys.Count,
            AvailableKeys = allKeys,
            PreviewMasked = preview
        };
    }

    private async Task<Dictionary<string, string>> LoadSelectedApiKeysAsync(
        List<string> selectedKeys, 
        CancellationToken ct)
    {
        var keys = new Dictionary<string, string>();

        foreach (var key in selectedKeys)
        {
            var value = await _secureStorage.GetApiKeyAsync(key).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(value))
            {
                keys[key] = value;
            }
        }

        return keys;
    }

    private async Task<List<string>> GetAllSecretKeysAsync(CancellationToken ct)
    {
        var configuredProviders = await _secureStorage.GetConfiguredProvidersAsync().ConfigureAwait(false);
        return configuredProviders.ToList();
    }

    private void DetectGeneralSettingsConflicts(
        GeneralSettings imported,
        GeneralSettings current,
        List<ImportConflict> conflicts)
    {
        if (imported.Theme != current.Theme)
        {
            conflicts.Add(new ImportConflict
            {
                Category = "General",
                Key = "Theme",
                CurrentValue = current.Theme.ToString(),
                NewValue = imported.Theme.ToString(),
                RecommendedAction = "Use new value"
            });
        }

        if (imported.AdvancedModeEnabled != current.AdvancedModeEnabled)
        {
            conflicts.Add(new ImportConflict
            {
                Category = "General",
                Key = "AdvancedModeEnabled",
                CurrentValue = current.AdvancedModeEnabled.ToString(),
                NewValue = imported.AdvancedModeEnabled.ToString(),
                RecommendedAction = "Use new value"
            });
        }
    }

    private void DetectFileLocationConflicts(
        FileLocationsSettings imported,
        FileLocationsSettings current,
        List<ImportConflict> conflicts)
    {
        if (!string.IsNullOrEmpty(imported.FFmpegPath) && imported.FFmpegPath != current.FFmpegPath)
        {
            conflicts.Add(new ImportConflict
            {
                Category = "FileLocations",
                Key = "FFmpegPath",
                CurrentValue = current.FFmpegPath,
                NewValue = imported.FFmpegPath,
                RecommendedAction = "Keep current (path may be machine-specific)"
            });
        }

        if (!string.IsNullOrEmpty(imported.OutputDirectory) && imported.OutputDirectory != current.OutputDirectory)
        {
            conflicts.Add(new ImportConflict
            {
                Category = "FileLocations",
                Key = "OutputDirectory",
                CurrentValue = current.OutputDirectory,
                NewValue = imported.OutputDirectory,
                RecommendedAction = "Keep current (path may be machine-specific)"
            });
        }
    }

    private async Task DetectApiKeyConflictsAsync(
        Dictionary<string, string>? importedKeys,
        List<ImportConflict> conflicts,
        CancellationToken ct)
    {
        if (importedKeys == null || importedKeys.Count == 0)
        {
            return;
        }

        foreach (var kvp in importedKeys)
        {
            var existingKey = await _secureStorage.GetApiKeyAsync(kvp.Key).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(existingKey) && existingKey != kvp.Value)
            {
                conflicts.Add(new ImportConflict
                {
                    Category = "ApiKeys",
                    Key = kvp.Key,
                    CurrentValue = MaskSecret(existingKey),
                    NewValue = MaskSecret(kvp.Value),
                    RecommendedAction = "Review before overwriting"
                });
            }
        }
    }

    private async Task ApplyImportAsync(
        ExportData importData,
        UserSettings currentSettings,
        CancellationToken ct)
    {
        currentSettings.General = importData.General;
        currentSettings.FileLocations = importData.FileLocations;
        currentSettings.VideoDefaults = importData.VideoDefaults;
        currentSettings.EditorPreferences = importData.EditorPreferences;
        currentSettings.UI = importData.UI;
        currentSettings.Advanced = importData.Advanced;
        currentSettings.LastUpdated = DateTime.UtcNow;

        // Import API keys if present
        if (importData.ApiKeys != null && importData.ApiKeys.Count > 0)
        {
            foreach (var kvp in importData.ApiKeys)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    await _secureStorage.SaveApiKeyAsync(kvp.Key, kvp.Value).ConfigureAwait(false);
                    _logger.LogInformation("Imported API key for provider: {Provider}", kvp.Key);
                }
            }
        }
    }

    private string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return string.Empty;
        }

        if (secret.Length <= 8)
        {
            return "***";
        }

        return $"{secret.Substring(0, 4)}...{secret.Substring(secret.Length - 4)}";
    }
}

/// <summary>
/// Result of export operation
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public ExportData? Data { get; set; }
    public ExportMetadataCore? Metadata { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public bool DryRun { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ImportConflict> Conflicts { get; set; } = new();
    public bool RequiresConfirmation { get; set; }
}

/// <summary>
/// Data being exported/imported
/// </summary>
public class ExportData
{
    public string Version { get; set; } = "1.0.0";
    public DateTime ExportedAt { get; set; }
    public GeneralSettings General { get; set; } = new();
    public Dictionary<string, string>? ApiKeys { get; set; }
    public FileLocationsSettings FileLocations { get; set; } = new();
    public VideoDefaultsSettings VideoDefaults { get; set; } = new();
    public EditorPreferencesSettings EditorPreferences { get; set; } = new();
    public UISettings UI { get; set; } = new();
    public AdvancedSettings Advanced { get; set; } = new();
}

/// <summary>
/// Metadata about export
/// </summary>
public class ExportMetadataCore
{
    public bool SecretsIncluded { get; set; }
    public List<string>? IncludedSecretKeys { get; set; }
    public List<string>? RedactedSecretKeys { get; set; }
    public string ExportedBy { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
}

/// <summary>
/// Conflict detected during import
/// </summary>
public class ImportConflict
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Preview of what will be redacted
/// </summary>
public class RedactionPreview
{
    public int TotalSecrets { get; set; }
    public List<string> AvailableKeys { get; set; } = new();
    public Dictionary<string, string> PreviewMasked { get; set; } = new();
}
