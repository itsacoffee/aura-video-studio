using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Service for assigning voices to characters in dialogue.
/// </summary>
public interface IVoiceAssignmentService
{
    /// <summary>
    /// Assigns voices to characters based on dialogue analysis.
    /// </summary>
    /// <param name="dialogue">The dialogue analysis result.</param>
    /// <param name="settings">Voice assignment settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Complete voice assignments for all lines.</returns>
    Task<VoiceAssignment> AssignVoicesAsync(
        DialogueAnalysis dialogue,
        VoiceAssignmentSettings settings,
        CancellationToken ct = default);
}
