using System;
using System.Collections.Generic;

namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// DTOs for settings export/import functionality
/// </summary>

/// <summary>
/// Request to export settings with optional secret inclusion
/// </summary>
public record ExportSettingsRequest(
    bool IncludeSecrets = false,
    List<string>? SelectedSecretKeys = null,
    bool AcknowledgeWarning = false);

/// <summary>
/// Response containing exported settings with metadata
/// </summary>
public record ExportSettingsResponse(
    string Version,
    DateTime ExportedAt,
    SettingsExportData Settings,
    ExportMetadata Metadata);

/// <summary>
/// The actual settings data being exported
/// </summary>
public record SettingsExportData(
    GeneralSettingsDto General,
    Dictionary<string, string>? ApiKeys,
    ProviderPathsDto ProviderPaths,
    Dictionary<string, object> AdditionalSettings);

/// <summary>
/// Metadata about the export
/// </summary>
public record ExportMetadata(
    bool SecretsIncluded,
    List<string>? IncludedSecretKeys,
    List<string>? RedactedSecretKeys,
    string ExportedBy,
    string MachineName);

/// <summary>
/// Request to import settings with dry-run option
/// </summary>
public record ImportSettingsRequest(
    string Version,
    SettingsExportData Settings,
    bool DryRun = true,
    bool OverwriteExisting = false);

/// <summary>
/// Response from import operation showing what would be changed
/// </summary>
public record ImportSettingsResponse(
    bool Success,
    string Message,
    ImportConflictSummary? Conflicts,
    List<string>? Errors,
    List<string>? Warnings);

/// <summary>
/// Summary of conflicts found during import
/// </summary>
public record ImportConflictSummary(
    List<ConflictItem> GeneralSettings,
    List<ConflictItem> ApiKeys,
    List<ConflictItem> ProviderPaths,
    int TotalConflicts);

/// <summary>
/// Individual conflict item
/// </summary>
public record ConflictItem(
    string Key,
    string CurrentValue,
    string NewValue,
    ConflictResolution RecommendedResolution);

/// <summary>
/// How to resolve a conflict
/// </summary>
public enum ConflictResolution
{
    KeepCurrent,
    UseNew,
    Merge
}

/// <summary>
/// General settings DTO for export/import
/// </summary>
public record GeneralSettingsDto(
    string DefaultProjectSaveLocation,
    int AutosaveIntervalSeconds,
    bool AutosaveEnabled,
    string Language,
    string Theme,
    bool AdvancedModeEnabled);

/// <summary>
/// Provider paths DTO for export/import
/// </summary>
public record ProviderPathsDto(
    string? StableDiffusionUrl,
    string? OllamaUrl,
    string? FFmpegPath,
    string? FFprobePath,
    string? OutputDirectory);

/// <summary>
/// Preview of what will be redacted in export
/// </summary>
public record RedactionPreviewResponse(
    int TotalSecrets,
    List<string> AvailableSecretKeys,
    Dictionary<string, string> RedactionPreview);
