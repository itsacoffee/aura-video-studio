using System;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Audio format configuration for TTS output
/// </summary>
public record AudioFormatConfig
{
    /// <summary>
    /// Audio codec (e.g., "pcm_s16le", "mp3", "aac")
    /// </summary>
    public required string Codec { get; init; }

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000, 22050)
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Number of audio channels (1 for mono, 2 for stereo)
    /// </summary>
    public required int Channels { get; init; }

    /// <summary>
    /// Bit depth (8, 16, 24, 32)
    /// </summary>
    public required int BitDepth { get; init; }

    /// <summary>
    /// Bitrate for compressed formats (e.g., 128000 for 128 kbps)
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// File extension (e.g., ".wav", ".mp3", ".ogg")
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// Whether this format supports streaming
    /// </summary>
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// Estimated file size multiplier (bytes per second of audio)
    /// </summary>
    public double BytesPerSecond => (SampleRate * Channels * BitDepth / 8.0) * (Bitrate != null ? Bitrate.Value / (double)(SampleRate * Channels * BitDepth) : 1.0);

    // Common presets
    public static readonly AudioFormatConfig Wav44100Stereo = new()
    {
        Codec = "pcm_s16le",
        SampleRate = 44100,
        Channels = 2,
        BitDepth = 16,
        FileExtension = ".wav",
        SupportsStreaming = false
    };

    public static readonly AudioFormatConfig Wav44100Mono = new()
    {
        Codec = "pcm_s16le",
        SampleRate = 44100,
        Channels = 1,
        BitDepth = 16,
        FileExtension = ".wav",
        SupportsStreaming = false
    };

    public static readonly AudioFormatConfig Mp3320Stereo = new()
    {
        Codec = "mp3",
        SampleRate = 44100,
        Channels = 2,
        BitDepth = 16,
        Bitrate = 320000,
        FileExtension = ".mp3",
        SupportsStreaming = true
    };

    public static readonly AudioFormatConfig Mp3128Stereo = new()
    {
        Codec = "mp3",
        SampleRate = 44100,
        Channels = 2,
        BitDepth = 16,
        Bitrate = 128000,
        FileExtension = ".mp3",
        SupportsStreaming = true
    };

    public static readonly AudioFormatConfig OggVorbis = new()
    {
        Codec = "libvorbis",
        SampleRate = 44100,
        Channels = 2,
        BitDepth = 16,
        Bitrate = 192000,
        FileExtension = ".ogg",
        SupportsStreaming = true
    };
}

/// <summary>
/// Audio quality preset
/// </summary>
public enum AudioQuality
{
    /// <summary>
    /// Low quality, small file size (good for drafts)
    /// </summary>
    Low,

    /// <summary>
    /// Medium quality, balanced (good for most use cases)
    /// </summary>
    Medium,

    /// <summary>
    /// High quality, larger file size (good for final production)
    /// </summary>
    High,

    /// <summary>
    /// Lossless quality, maximum file size (archival/mastering)
    /// </summary>
    Lossless
}

/// <summary>
/// Audio processing options
/// </summary>
public record AudioProcessingOptions
{
    /// <summary>
    /// Whether to normalize audio volume
    /// </summary>
    public bool NormalizeVolume { get; init; } = true;

    /// <summary>
    /// Target loudness in LUFS (default: -16 for broadcast standard)
    /// </summary>
    public double TargetLufs { get; init; } = -16.0;

    /// <summary>
    /// Whether to remove silence at start and end
    /// </summary>
    public bool TrimSilence { get; init; } = true;

    /// <summary>
    /// Silence threshold in dB (for trimming)
    /// </summary>
    public double SilenceThresholdDb { get; init; } = -40.0;

    /// <summary>
    /// Whether to apply noise reduction
    /// </summary>
    public bool ReduceNoise { get; init; } = false;

    /// <summary>
    /// Whether to apply compression/limiting
    /// </summary>
    public bool ApplyCompression { get; init; } = false;

    /// <summary>
    /// Fade in duration in milliseconds
    /// </summary>
    public int FadeInMs { get; init; } = 0;

    /// <summary>
    /// Fade out duration in milliseconds
    /// </summary>
    public int FadeOutMs { get; init; } = 0;

    /// <summary>
    /// Default processing options for TTS
    /// </summary>
    public static readonly AudioProcessingOptions TtsDefault = new()
    {
        NormalizeVolume = true,
        TargetLufs = -16.0,
        TrimSilence = true,
        SilenceThresholdDb = -40.0,
        ReduceNoise = false,
        ApplyCompression = false,
        FadeInMs = 50,
        FadeOutMs = 100
    };
}

/// <summary>
/// Audio metadata information
/// </summary>
public record AudioMetadata
{
    /// <summary>
    /// Duration of audio
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Number of channels
    /// </summary>
    public required int Channels { get; init; }

    /// <summary>
    /// Bit depth
    /// </summary>
    public required int BitDepth { get; init; }

    /// <summary>
    /// Codec name
    /// </summary>
    public required string Codec { get; init; }

    /// <summary>
    /// Bitrate in bits per second (for compressed formats)
    /// </summary>
    public int? Bitrate { get; init; }

    /// <summary>
    /// Peak amplitude level in dB
    /// </summary>
    public double? PeakDb { get; init; }

    /// <summary>
    /// RMS level in dB
    /// </summary>
    public double? RmsDb { get; init; }

    /// <summary>
    /// Loudness in LUFS (if measured)
    /// </summary>
    public double? LoudnessLufs { get; init; }

    /// <summary>
    /// File path
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// When the audio was generated
    /// </summary>
    public DateTime? GeneratedAt { get; init; }

    /// <summary>
    /// TTS provider that generated this audio
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Voice name used
    /// </summary>
    public string? VoiceName { get; init; }
}

/// <summary>
/// Audio validation result
/// </summary>
public record AudioValidationResult
{
    /// <summary>
    /// Whether the audio is valid
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation issues
    /// </summary>
    public required string[] Issues { get; init; }

    /// <summary>
    /// List of warnings (non-critical)
    /// </summary>
    public string[] Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Audio metadata
    /// </summary>
    public AudioMetadata? Metadata { get; init; }

    /// <summary>
    /// Success result
    /// </summary>
    public static AudioValidationResult Success(AudioMetadata metadata) => new()
    {
        IsValid = true,
        Issues = Array.Empty<string>(),
        Metadata = metadata
    };

    /// <summary>
    /// Failure result
    /// </summary>
    public static AudioValidationResult Failure(params string[] issues) => new()
    {
        IsValid = false,
        Issues = issues
    };
}
