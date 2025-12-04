using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Service for creating and managing cloned voices.
/// </summary>
public interface IVoiceCloningService
{
    /// <summary>
    /// Creates a cloned voice from audio samples.
    /// </summary>
    /// <param name="name">Name for the cloned voice.</param>
    /// <param name="samplePaths">Paths to audio sample files.</param>
    /// <param name="settings">Voice cloning settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created cloned voice.</returns>
    Task<ClonedVoice> CreateClonedVoiceAsync(
        string name,
        IReadOnlyList<string> samplePaths,
        VoiceCloneSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all available cloned voices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of cloned voices.</returns>
    Task<IReadOnlyList<ClonedVoice>> GetClonedVoicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a cloned voice.
    /// </summary>
    /// <param name="voiceId">ID of the voice to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteClonedVoiceAsync(string voiceId, CancellationToken ct = default);

    /// <summary>
    /// Generates a preview sample using a cloned voice.
    /// </summary>
    /// <param name="voiceId">ID of the cloned voice.</param>
    /// <param name="sampleText">Text to synthesize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the audio file path.</returns>
    Task<VoiceSampleResult> GenerateSampleAsync(
        string voiceId,
        string sampleText,
        CancellationToken ct = default);
}
