using System;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for storing user settings with support for encryption
/// </summary>
public class SettingsEntity : IAuditableEntity
{
    /// <summary>
    /// Unique identifier for the settings record
    /// Primary key - typically "user-settings" for single-user mode
    /// </summary>
    public string Id { get; set; } = "user-settings";

    /// <summary>
    /// Settings data stored as JSON
    /// Contains all user preferences and configuration
    /// </summary>
    public string SettingsJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether the settings JSON is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Version of the settings schema
    /// Used for migration and compatibility
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// When the settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the settings
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified the settings
    /// </summary>
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Database entity for storing provider configuration separately
/// Allows for encrypted storage of API keys and sensitive data
/// </summary>
public class ProviderConfigurationEntity : IAuditableEntity
{
    /// <summary>
    /// Unique identifier for the provider config record
    /// </summary>
    public string Id { get; set; } = "provider-config";

    /// <summary>
    /// Provider configuration stored as JSON
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether the config JSON is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; } = true;

    /// <summary>
    /// Version of the provider config schema
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// When the config was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the config was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the config
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified the config
    /// </summary>
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Database entity for storing hardware performance settings
/// </summary>
public class HardwareSettingsEntity : IAuditableEntity
{
    /// <summary>
    /// Unique identifier for the hardware settings record
    /// </summary>
    public string Id { get; set; } = "hardware-settings";

    /// <summary>
    /// Hardware settings stored as JSON
    /// </summary>
    public string SettingsJson { get; set; } = string.Empty;

    /// <summary>
    /// Version of the hardware settings schema
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// When the settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the settings
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified the settings
    /// </summary>
    public string? ModifiedBy { get; set; }
}
