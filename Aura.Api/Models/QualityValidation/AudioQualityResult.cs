namespace Aura.Api.Models.QualityValidation;

/// <summary>
/// Result of audio quality analysis
/// </summary>
public record AudioQualityResult : QualityValidationResponse
{
    /// <summary>
    /// Audio loudness in LUFS (Loudness Units relative to Full Scale)
    /// </summary>
    public double LoudnessLUFS { get; init; }

    /// <summary>
    /// Peak audio level in dBFS
    /// </summary>
    public double PeakLevel { get; init; }

    /// <summary>
    /// Estimated noise level (0-100, higher is noisier)
    /// </summary>
    public int NoiseLevel { get; init; }

    /// <summary>
    /// Clarity score (0-100, higher is clearer)
    /// </summary>
    public int ClarityScore { get; init; }

    /// <summary>
    /// Indicates if audio has clipping
    /// </summary>
    public bool HasClipping { get; init; }

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; init; }

    /// <summary>
    /// Bit depth
    /// </summary>
    public int BitDepth { get; init; }

    /// <summary>
    /// Number of audio channels
    /// </summary>
    public int Channels { get; init; }

    /// <summary>
    /// Dynamic range in dB
    /// </summary>
    public double DynamicRange { get; init; }
}
