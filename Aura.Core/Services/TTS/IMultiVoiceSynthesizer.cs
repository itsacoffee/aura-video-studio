using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Service for synthesizing multi-voice audio from dialogue.
/// </summary>
public interface IMultiVoiceSynthesizer
{
    /// <summary>
    /// Synthesizes audio with multiple voices for different characters.
    /// </summary>
    /// <param name="assignment">Voice assignment with lines and assigned voices.</param>
    /// <param name="progress">Progress reporter for synthesis updates.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to the combined audio file.</returns>
    Task<string> SynthesizeMultiVoiceAsync(
        VoiceAssignment assignment,
        IProgress<SynthesisProgress>? progress = null,
        CancellationToken ct = default);
}
