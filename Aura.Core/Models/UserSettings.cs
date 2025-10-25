using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Comprehensive user settings model for all application configuration
/// </summary>
public class UserSettings
{
    // General Settings
    public GeneralSettings General { get; set; } = new();
    
    // API Keys
    public ApiKeysSettings ApiKeys { get; set; } = new();
    
    // File Locations
    public FileLocationsSettings FileLocations { get; set; } = new();
    
    // Video Defaults
    public VideoDefaultsSettings VideoDefaults { get; set; } = new();
    
    // Editor Preferences
    public EditorPreferencesSettings EditorPreferences { get; set; } = new();
    
    // UI Settings
    public UISettings UI { get; set; } = new();
    
    // Advanced Settings (legacy)
    public AdvancedSettings Advanced { get; set; } = new();
    
    // Metadata
    public string Version { get; set; } = "1.0.0";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// General application settings
/// </summary>
public class GeneralSettings
{
    public string DefaultProjectSaveLocation { get; set; } = "";
    public int AutosaveIntervalSeconds { get; set; } = 300; // 5 minutes default
    public bool AutosaveEnabled { get; set; } = true;
    public string Language { get; set; } = "en-US";
    public string Locale { get; set; } = "en-US";
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;
    public StartupBehavior StartupBehavior { get; set; } = StartupBehavior.ShowDashboard;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
}

/// <summary>
/// API keys for external services
/// Note: These should be encrypted when stored
/// </summary>
public class ApiKeysSettings
{
    public string OpenAI { get; set; } = "";
    public string Anthropic { get; set; } = "";
    public string StabilityAI { get; set; } = "";
    public string ElevenLabs { get; set; } = "";
    public string Pexels { get; set; } = "";
    public string Pixabay { get; set; } = "";
    public string Unsplash { get; set; } = "";
    public string Google { get; set; } = "";
    public string Azure { get; set; } = "";
}

/// <summary>
/// File and directory locations
/// </summary>
public class FileLocationsSettings
{
    public string FFmpegPath { get; set; } = "";
    public string FFprobePath { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string TempDirectory { get; set; } = "";
    public string MediaLibraryLocation { get; set; } = "";
    public string ProjectsDirectory { get; set; } = "";
}

/// <summary>
/// Default video generation settings
/// </summary>
public class VideoDefaultsSettings
{
    public string DefaultResolution { get; set; } = "1920x1080";
    public int DefaultFrameRate { get; set; } = 30;
    public string DefaultCodec { get; set; } = "libx264";
    public string DefaultBitrate { get; set; } = "5M";
    public string DefaultAudioCodec { get; set; } = "aac";
    public string DefaultAudioBitrate { get; set; } = "192k";
    public int DefaultAudioSampleRate { get; set; } = 44100;
}

/// <summary>
/// Editor and timeline preferences
/// </summary>
public class EditorPreferencesSettings
{
    public bool TimelineSnapEnabled { get; set; } = true;
    public double TimelineSnapInterval { get; set; } = 1.0; // seconds
    public string PlaybackQuality { get; set; } = "high";
    public bool GenerateThumbnails { get; set; } = true;
    public int ThumbnailInterval { get; set; } = 5; // seconds
    public Dictionary<string, string> KeyboardShortcuts { get; set; } = new();
    public bool ShowWaveforms { get; set; } = true;
    public bool ShowTimecode { get; set; } = true;
}

/// <summary>
/// UI customization settings
/// </summary>
public class UISettings
{
    public int Scale { get; set; } = 100;
    public bool CompactMode { get; set; } = false;
    public string ColorScheme { get; set; } = "default";
}

/// <summary>
/// Advanced/legacy settings
/// </summary>
public class AdvancedSettings
{
    public bool OfflineMode { get; set; } = false;
    public string StableDiffusionUrl { get; set; } = "http://127.0.0.1:7860";
    public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";
    public bool EnableTelemetry { get; set; } = false;
    public bool EnableCrashReports { get; set; } = false;
}

/// <summary>
/// Theme mode enumeration
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    Auto
}

/// <summary>
/// Startup behavior options
/// </summary>
public enum StartupBehavior
{
    ShowDashboard,
    ShowLastProject,
    ShowNewProjectDialog
}
