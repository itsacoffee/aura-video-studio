using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;

namespace Aura.Core.Services.TTS;

/// <summary>
/// Service for detecting dialogue and characters in scripts.
/// </summary>
public interface IDialogueDetectionService
{
    /// <summary>
    /// Analyzes a script to detect dialogue, characters, and emotions.
    /// </summary>
    /// <param name="script">The script text to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analysis result with detected lines and characters.</returns>
    Task<DialogueAnalysis> AnalyzeScriptAsync(
        string script,
        CancellationToken ct = default);
}
