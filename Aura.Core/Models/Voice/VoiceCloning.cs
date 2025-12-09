using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Voice;

/// <summary>
/// Represents a cloned voice created from audio samples.
/// </summary>
public record ClonedVoice(
    string Id,
    string Name,
    string ProviderId,
    VoiceProvider Provider,
    DateTime CreatedAt,
    VoiceCloneQuality Quality,
    IReadOnlyList<string> SamplePaths);

/// <summary>
/// Settings for voice cloning operations.
/// </summary>
public record VoiceCloneSettings(
    string Description,
    VoiceCloneQuality Quality = VoiceCloneQuality.Standard,
    string? AccentHint = null,
    string? GenderHint = null);

/// <summary>
/// Quality levels for voice cloning.
/// </summary>
public enum VoiceCloneQuality
{
    /// <summary>Fast, lower quality instant voice clone.</summary>
    Instant,
    /// <summary>Balanced quality and processing time.</summary>
    Standard,
    /// <summary>Highest quality, requires more samples and processing time.</summary>
    Professional
}
