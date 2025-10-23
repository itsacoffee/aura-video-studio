using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Enhanced TTS provider interface with voice enhancement support
/// </summary>
public interface IEnhancedTtsProvider
{
    /// <summary>
    /// Gets available voices from the provider
    /// </summary>
    Task<IReadOnlyList<VoiceDescriptor>> GetVoiceDescriptorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Synthesizes speech with optional enhancement
    /// </summary>
    Task<TtsSynthesisResult> SynthesizeWithEnhancementAsync(
        IEnumerable<ScriptLine> lines,
        VoiceSpec spec,
        VoiceEnhancementConfig? enhancementConfig = null,
        CancellationToken ct = default);

    /// <summary>
    /// Synthesizes speech with prosody controls
    /// </summary>
    Task<TtsSynthesisResult> SynthesizeWithProsodyAsync(
        IEnumerable<ScriptLine> lines,
        VoiceSpec spec,
        ProsodySettings prosody,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a sample of a voice
    /// </summary>
    Task<VoiceSampleResult?> GetVoiceSampleAsync(
        string voiceId,
        string sampleText = "Hello, this is a sample of my voice.",
        CancellationToken ct = default);

    /// <summary>
    /// Validates that the provider is properly configured
    /// </summary>
    Task<ProviderHealthStatus> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>
/// TTS synthesis result with metadata
/// </summary>
public record TtsSynthesisResult
{
    /// <summary>
    /// Path to the synthesized audio file
    /// </summary>
    public required string AudioPath { get; init; }

    /// <summary>
    /// Voice used for synthesis
    /// </summary>
    public required VoiceDescriptor Voice { get; init; }

    /// <summary>
    /// Total duration of synthesized audio
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of characters synthesized
    /// </summary>
    public int CharacterCount { get; init; }

    /// <summary>
    /// Provider-specific metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Any warnings or messages
    /// </summary>
    public string[] Messages { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Provider health status
/// </summary>
public record ProviderHealthStatus
{
    /// <summary>
    /// Is the provider available and configured?
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Provider name
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Available voices count
    /// </summary>
    public int AvailableVoicesCount { get; init; }

    /// <summary>
    /// Any issues or messages
    /// </summary>
    public string[] Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Provider capabilities
    /// </summary>
    public TtsCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// TTS provider capabilities
/// </summary>
public record TtsCapabilities
{
    /// <summary>
    /// Supports SSML markup
    /// </summary>
    public bool SupportsSsml { get; init; }

    /// <summary>
    /// Supports prosody controls
    /// </summary>
    public bool SupportsProsody { get; init; }

    /// <summary>
    /// Supports emotion/style controls
    /// </summary>
    public bool SupportsStyles { get; init; }

    /// <summary>
    /// Supports voice cloning
    /// </summary>
    public bool SupportsVoiceCloning { get; init; }

    /// <summary>
    /// Maximum characters per request (-1 for unlimited)
    /// </summary>
    public int MaxCharactersPerRequest { get; init; } = -1;

    /// <summary>
    /// Supports streaming synthesis
    /// </summary>
    public bool SupportsStreaming { get; init; }

    /// <summary>
    /// Available audio formats
    /// </summary>
    public string[] SupportedFormats { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Voice sample result
/// </summary>
public record VoiceSampleResult
{
    /// <summary>
    /// Path to the sample audio file
    /// </summary>
    public required string AudioPath { get; init; }

    /// <summary>
    /// Duration of the sample
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Sample text that was synthesized
    /// </summary>
    public string? SampleText { get; init; }

    /// <summary>
    /// Sample format (e.g., "wav", "mp3")
    /// </summary>
    public string AudioFormat { get; init; } = "wav";
}

/// <summary>
/// TTS provider factory for creating appropriate provider instances
/// </summary>
public interface ITtsProviderFactory
{
    /// <summary>
    /// Creates a TTS provider for the specified provider type
    /// </summary>
    IEnhancedTtsProvider CreateProvider(VoiceProvider providerType);

    /// <summary>
    /// Gets all available providers
    /// </summary>
    IReadOnlyList<IEnhancedTtsProvider> GetAvailableProviders();

    /// <summary>
    /// Gets the best provider for the given voice
    /// </summary>
    IEnhancedTtsProvider GetProviderForVoice(VoiceDescriptor voice);
}
