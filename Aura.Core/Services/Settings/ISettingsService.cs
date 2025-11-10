using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Services.Settings;

/// <summary>
/// Centralized service for managing all application settings
/// Provides unified access to user preferences, provider configurations, and system settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get all user settings
    /// </summary>
    Task<UserSettings> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Update user settings
    /// </summary>
    Task<SettingsUpdateResult> UpdateSettingsAsync(UserSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Reset all settings to defaults
    /// </summary>
    Task<SettingsUpdateResult> ResetToDefaultsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get specific settings section
    /// </summary>
    Task<T> GetSettingsSectionAsync<T>(CancellationToken ct = default) where T : class, new();

    /// <summary>
    /// Update specific settings section
    /// </summary>
    Task<SettingsUpdateResult> UpdateSettingsSectionAsync<T>(T section, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Validate settings
    /// </summary>
    Task<SettingsValidationResult> ValidateSettingsAsync(UserSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Export settings to JSON
    /// </summary>
    Task<string> ExportSettingsAsync(bool includeSecrets = false, CancellationToken ct = default);

    /// <summary>
    /// Import settings from JSON
    /// </summary>
    Task<SettingsUpdateResult> ImportSettingsAsync(string json, bool overwriteExisting = false, CancellationToken ct = default);

    /// <summary>
    /// Get hardware performance settings
    /// </summary>
    Task<HardwarePerformanceSettings> GetHardwareSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Update hardware performance settings
    /// </summary>
    Task<SettingsUpdateResult> UpdateHardwareSettingsAsync(HardwarePerformanceSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Get provider configuration settings
    /// </summary>
    Task<ProviderConfiguration> GetProviderConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Update provider configuration settings
    /// </summary>
    Task<SettingsUpdateResult> UpdateProviderConfigurationAsync(ProviderConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Test provider connection
    /// </summary>
    Task<ProviderTestResult> TestProviderConnectionAsync(string providerName, CancellationToken ct = default);

    /// <summary>
    /// Get available GPU devices for selection
    /// </summary>
    Task<List<GpuDevice>> GetAvailableGpuDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get available hardware encoder options
    /// </summary>
    Task<List<EncoderOption>> GetAvailableEncodersAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of a settings update operation
/// </summary>
public class SettingsUpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of settings validation
/// </summary>
public class SettingsValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new();
}

/// <summary>
/// Validation issue
/// </summary>
public class ValidationIssue
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; }
}

/// <summary>
/// Validation severity
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Result of provider connection test
/// </summary>
public class ProviderTestResult
{
    public bool Success { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// GPU device information
/// </summary>
public class GpuDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public int VramMB { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Encoder option
/// </summary>
public class EncoderOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsHardwareAccelerated { get; set; }
    public bool IsAvailable { get; set; }
    public List<string> RequiredHardware { get; set; } = new();
}
