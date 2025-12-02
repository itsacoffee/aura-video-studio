using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Services.Settings;

/// <summary>
/// Service for managing graphics and visual settings
/// </summary>
public interface IGraphicsSettingsService
{
    /// <summary>
    /// Get current graphics settings
    /// </summary>
    Task<GraphicsSettings> GetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Save graphics settings
    /// </summary>
    Task<bool> SaveSettingsAsync(GraphicsSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Apply a performance profile preset
    /// </summary>
    Task<GraphicsSettings> ApplyProfileAsync(PerformanceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Detect optimal settings based on hardware
    /// </summary>
    Task<GraphicsSettings> DetectOptimalSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    Task<bool> ResetToDefaultsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when settings change
    /// </summary>
    event EventHandler<GraphicsSettings>? SettingsChanged;
}
