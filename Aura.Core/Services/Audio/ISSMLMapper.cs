using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.Audio;

/// <summary>
/// Interface for provider-specific SSML mapping
/// </summary>
public interface ISSMLMapper
{
    /// <summary>
    /// Provider this mapper supports
    /// </summary>
    VoiceProvider Provider { get; }

    /// <summary>
    /// Get provider-specific SSML constraints
    /// </summary>
    ProviderSSMLConstraints GetConstraints();

    /// <summary>
    /// Map generic prosody to provider-specific SSML
    /// </summary>
    string MapToSSML(
        string text,
        ProsodyAdjustments adjustments,
        VoiceSpec voiceSpec);

    /// <summary>
    /// Validate SSML for provider compatibility
    /// </summary>
    Models.Audio.SSMLValidationResult Validate(string ssml);

    /// <summary>
    /// Attempt to auto-repair invalid SSML
    /// </summary>
    string AutoRepair(string ssml);

    /// <summary>
    /// Estimate duration for SSML segment in milliseconds
    /// </summary>
    Task<int> EstimateDurationAsync(
        string ssml,
        VoiceSpec voiceSpec,
        CancellationToken ct = default);
}
