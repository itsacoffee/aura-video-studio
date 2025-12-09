using System;

namespace Aura.Core.Models;

/// <summary>
/// Performance profile presets for quick configuration
/// </summary>
public enum PerformanceProfile
{
    /// <summary>All effects enabled, highest quality</summary>
    Maximum,
    /// <summary>Moderate effects, good performance</summary>
    Balanced,
    /// <summary>Minimal effects, best battery life</summary>
    PowerSaver,
    /// <summary>User-defined settings</summary>
    Custom
}

/// <summary>
/// DPI scaling mode options
/// </summary>
public enum ScalingMode
{
    /// <summary>Follow Windows display settings</summary>
    System,
    /// <summary>User-specified scale factor</summary>
    Manual
}

/// <summary>
/// Comprehensive graphics and visual settings
/// </summary>
public class GraphicsSettings
{
    /// <summary>
    /// Performance profile preset
    /// </summary>
    public PerformanceProfile Profile { get; set; } = PerformanceProfile.Maximum;

    /// <summary>
    /// Whether GPU acceleration is enabled
    /// </summary>
    public bool GpuAccelerationEnabled { get; set; } = true;

    /// <summary>
    /// Name of the detected GPU
    /// </summary>
    public string? DetectedGpuName { get; set; }

    /// <summary>
    /// Vendor of the detected GPU (NVIDIA, AMD, Intel)
    /// </summary>
    public string? DetectedGpuVendor { get; set; }

    /// <summary>
    /// Detected VRAM in megabytes
    /// </summary>
    public int DetectedVramMB { get; set; }

    /// <summary>
    /// Individual visual effect toggles
    /// </summary>
    public VisualEffectsSettings Effects { get; set; } = new();

    /// <summary>
    /// Display scaling configuration
    /// </summary>
    public DisplayScalingSettings Scaling { get; set; } = new();

    /// <summary>
    /// Accessibility-related settings
    /// </summary>
    public AccessibilitySettings Accessibility { get; set; } = new();

    /// <summary>
    /// When settings were last modified
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Settings schema version for migration support
    /// </summary>
    public int SettingsVersion { get; set; } = 1;
}

/// <summary>
/// Individual visual effect toggles for "beauty" features
/// </summary>
public class VisualEffectsSettings
{
    /// <summary>Enable UI animations</summary>
    public bool Animations { get; set; } = true;

    /// <summary>Enable blur/frosted glass effects</summary>
    public bool BlurEffects { get; set; } = true;

    /// <summary>Enable drop shadows</summary>
    public bool Shadows { get; set; } = true;

    /// <summary>Enable transparency effects</summary>
    public bool Transparency { get; set; } = true;

    /// <summary>Enable smooth scrolling</summary>
    public bool SmoothScrolling { get; set; } = true;

    /// <summary>Enable spring physics animations</summary>
    public bool SpringPhysics { get; set; } = true;

    /// <summary>Enable parallax scrolling effects</summary>
    public bool ParallaxEffects { get; set; } = true;

    /// <summary>Enable glow/highlight effects</summary>
    public bool GlowEffects { get; set; } = true;

    /// <summary>Enable micro-interaction animations</summary>
    public bool MicroInteractions { get; set; } = true;

    /// <summary>Enable staggered list animations</summary>
    public bool StaggeredAnimations { get; set; } = true;
}

/// <summary>
/// DPI and display scaling configuration
/// </summary>
public class DisplayScalingSettings
{
    /// <summary>
    /// Scaling mode (System or Manual)
    /// </summary>
    public ScalingMode Mode { get; set; } = ScalingMode.System;

    /// <summary>
    /// Manual scale factor (1.0 = 100%, 1.5 = 150%, etc.)
    /// </summary>
    public double ManualScaleFactor { get; set; } = 1.0;

    /// <summary>
    /// Enable per-monitor DPI awareness
    /// </summary>
    public bool PerMonitorDpiAware { get; set; } = true;

    /// <summary>
    /// Enable subpixel text rendering
    /// </summary>
    public bool SubpixelRendering { get; set; } = true;
}

/// <summary>
/// Accessibility-related visual settings
/// </summary>
public class AccessibilitySettings
{
    /// <summary>
    /// Reduce motion effects (respects OS preference by default)
    /// </summary>
    public bool ReducedMotion { get; set; }

    /// <summary>
    /// Enable high contrast mode
    /// </summary>
    public bool HighContrast { get; set; }

    /// <summary>
    /// Enable larger text
    /// </summary>
    public bool LargeText { get; set; }

    /// <summary>
    /// Show focus indicators for keyboard navigation
    /// </summary>
    public bool FocusIndicators { get; set; } = true;
}
