using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.Configuration;

/// <summary>
/// Implementation that unifies FFmpegOptions, environment hints, and FFmpegConfigurationStore.
/// </summary>
public sealed class FfmpegConfigurationService : IFfmpegConfigurationService
{
    private readonly ILogger<FfmpegConfigurationService> _logger;
    private readonly IOptions<FFmpegOptions> _options;
    private readonly FFmpegConfigurationStore _store;

    public FfmpegConfigurationService(
        ILogger<FfmpegConfigurationService> logger,
        IOptions<FFmpegOptions> options,
        FFmpegConfigurationStore store)
    {
        _logger = logger;
        _options = options;
        _store = store;
    }

    public async Task<FFmpegConfiguration> GetEffectiveConfigurationAsync(CancellationToken ct = default)
    {
        // 1. Load persisted config
        var persisted = await _store.LoadAsync(ct).ConfigureAwait(false);

        // 2. Apply environment hint from Electron, if present
        var envPath = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && string.IsNullOrWhiteSpace(persisted.Path))
        {
            _logger.LogInformation("Applying AURA_FFMPEG_PATH hint: {Path}", envPath);
            persisted.Path = envPath;
            persisted.Mode = FFmpegMode.Custom;
            persisted.Source = "Environment";
        }

        // 3. Apply appsettings defaults if still not configured
        var opts = _options.Value;
        if (string.IsNullOrWhiteSpace(persisted.Path) &&
            !string.IsNullOrWhiteSpace(opts.ExecutablePath))
        {
            _logger.LogInformation("Applying FFmpegOptions.ExecutablePath: {Path}", opts.ExecutablePath);
            persisted.Path = opts.ExecutablePath;
            persisted.Mode = FFmpegMode.Custom;
            persisted.Source = "Configured";
        }

        return persisted;
    }

    public async Task UpdateConfigurationAsync(FFmpegConfiguration configuration, CancellationToken ct = default)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        _logger.LogInformation(
            "Updating FFmpeg configuration: Mode={Mode}, Path={Path}",
            configuration.Mode,
            configuration.Path ?? "null");

        await _store.SaveAsync(configuration, ct).ConfigureAwait(false);
    }
}
