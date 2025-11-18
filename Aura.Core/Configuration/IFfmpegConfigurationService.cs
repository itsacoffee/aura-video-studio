using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Configuration;

/// <summary>
/// High-level facade for FFmpeg configuration, combining:
/// - FFmpegOptions (appsettings)
/// - Environment hints (e.g. AURA_FFMPEG_PATH)
/// - Persistent configuration (FFmpegConfigurationStore)
/// This is the single authoritative access point for FFmpeg config at runtime.
/// </summary>
public interface IFfmpegConfigurationService
{
    /// <summary>
    /// Resolve the effective FFmpeg configuration, applying all sources and validation.
    /// </summary>
    Task<FFmpegConfiguration> GetEffectiveConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Update the FFmpeg configuration (e.g. from UI) and persist to disk.
    /// </summary>
    Task UpdateConfigurationAsync(FFmpegConfiguration configuration, CancellationToken ct = default);
}
