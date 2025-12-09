using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Services.Director;

/// <summary>
/// AI Director service that automatically applies professional cinematographic decisions
/// to video generation, including Ken Burns motion, transition selection, and director-style presets.
/// </summary>
public interface IAIDirectorService
{
    /// <summary>
    /// Analyzes scenes and applies director decisions based on emotional arc and preset style.
    /// </summary>
    /// <param name="scenes">The scenes to analyze and direct</param>
    /// <param name="brief">The video brief with topic, tone, and audience information</param>
    /// <param name="preset">The director preset style to apply</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Director decisions for all scenes including motion, transitions, and timing</returns>
    Task<DirectorDecisions> AnalyzeAndDirectAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        DirectorPreset preset,
        CancellationToken ct = default);

    /// <summary>
    /// Smooths transitions between scenes to prevent jarring sequences.
    /// </summary>
    /// <param name="directions">The scene directions to smooth</param>
    /// <returns>Smoothed scene directions with balanced transitions</returns>
    IReadOnlyList<SceneDirection> SmoothTransitions(IReadOnlyList<SceneDirection> directions);
}
