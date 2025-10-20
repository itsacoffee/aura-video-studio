using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Null TTS provider that returns silence - used as final fallback when no other TTS is available.
/// Uses SilentWavGenerator for reliable silent audio generation.
/// </summary>
public class NullTtsProvider : ITtsProvider
{
    private readonly ILogger<NullTtsProvider> _logger;
    private readonly SilentWavGenerator _silentWavGenerator;
    private readonly string _outputDir;

    public NullTtsProvider(ILogger<NullTtsProvider> logger, SilentWavGenerator silentWavGenerator)
    {
        _logger = logger;
        _silentWavGenerator = silentWavGenerator;
        _outputDir = Path.Combine(Path.GetTempPath(), "aura-null-tts");
        Directory.CreateDirectory(_outputDir);
    }

    public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        // Return a single "silent" voice
        var voices = new List<string> { "Null (Silent)" };
        return Task.FromResult<IReadOnlyList<string>>(voices);
    }

    public async Task<string> SynthesizeAsync(
        IEnumerable<ScriptLine> lines,
        VoiceSpec spec,
        CancellationToken ct = default)
    {
        _logger.LogWarning("NullTtsProvider: Generating silent audio placeholder");
        
        // Calculate total duration
        var totalDuration = TimeSpan.Zero;
        foreach (var line in lines)
        {
            totalDuration += line.Duration;
        }

        var outputPath = Path.Combine(_outputDir, $"silent-{Guid.NewGuid()}.wav");

        // Use SilentWavGenerator for reliable, validated silent audio
        await _silentWavGenerator.GenerateAsync(outputPath, totalDuration, ct: ct);

        _logger.LogInformation("Generated silent audio: {Path}, Duration: {Duration}s", 
            outputPath, totalDuration.TotalSeconds);
        
        return outputPath;
    }
}
