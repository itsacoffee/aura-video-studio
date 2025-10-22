using System.Collections.Generic;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Core TTS request with provider-specific options support
/// </summary>
public class TtsRequest
{
    /// <summary>
    /// Text to synthesize
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Voice identifier
    /// </summary>
    public required string VoiceId { get; set; }

    /// <summary>
    /// TTS provider to use
    /// </summary>
    public VoiceProvider Provider { get; set; }

    /// <summary>
    /// Common options (normalized 0.0 to 2.0) - used when provider-specific options not set
    /// </summary>
    public CommonTtsOptions? CommonOptions { get; set; }

    /// <summary>
    /// Azure-specific options
    /// </summary>
    public AzureTtsOptions? AzureOptions { get; set; }

    /// <summary>
    /// Output audio format
    /// </summary>
    public AudioFormat OutputFormat { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 24000;
}

/// <summary>
/// Common TTS options that work across all providers
/// </summary>
public class CommonTtsOptions
{
    /// <summary>
    /// Speaking rate (0.0 to 2.0, default 1.0)
    /// </summary>
    public double Rate { get; set; } = 1.0;

    /// <summary>
    /// Voice pitch (0.0 to 2.0, default 1.0)
    /// </summary>
    public double Pitch { get; set; } = 1.0;

    /// <summary>
    /// Volume level (0.0 to 2.0, default 1.0)
    /// </summary>
    public double Volume { get; set; } = 1.0;
}

/// <summary>
/// Supported audio output formats
/// </summary>
public enum AudioFormat
{
    Wav,
    Mp3,
    Ogg
}
