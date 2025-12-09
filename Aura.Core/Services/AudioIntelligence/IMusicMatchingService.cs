using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Interface for AI-driven music selection and matching service.
/// Uses LLM to analyze content and match appropriate background music.
/// </summary>
public interface IMusicMatchingService
{
    /// <summary>
    /// Analyzes the brief and scenes using LLM to generate music search parameters.
    /// </summary>
    /// <param name="brief">The video brief with topic, audience, and tone</param>
    /// <param name="scenes">List of scenes with their content and duration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>AI-generated music search parameters</returns>
    Task<MusicMatchParameters> AnalyzeContentForMusicAsync(
        Brief brief,
        IReadOnlyList<Scene> scenes,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for music tracks matching the AI-generated parameters.
    /// </summary>
    /// <param name="parameters">Music search parameters from AI analysis</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching music suggestions</returns>
    Task<List<MusicSuggestion>> GetMusicSuggestionsAsync(
        MusicMatchParameters parameters,
        int maxResults = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads and prepares music for video (trimming, looping, fading).
    /// </summary>
    /// <param name="suggestion">The selected music suggestion</param>
    /// <param name="targetDuration">Target duration for the music</param>
    /// <param name="outputPath">Path to save the prepared music file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to the prepared music file</returns>
    Task<string> PrepareSelectedMusicAsync(
        MusicSuggestion suggestion,
        System.TimeSpan targetDuration,
        string outputPath,
        CancellationToken ct = default);

    /// <summary>
    /// Ranks music suggestions based on relevance to the content.
    /// </summary>
    /// <param name="suggestions">List of music suggestions to rank</param>
    /// <param name="brief">The video brief for context</param>
    /// <param name="scenes">Scenes for content matching</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Ranked list of suggestions with scores</returns>
    Task<List<MusicSuggestion>> RankSuggestionsAsync(
        List<MusicSuggestion> suggestions,
        Brief brief,
        IReadOnlyList<Scene> scenes,
        CancellationToken ct = default);
}

/// <summary>
/// Parameters for music matching derived from AI analysis.
/// </summary>
public record MusicMatchParameters(
    MusicMood PrimaryMood,
    MusicMood? SecondaryMood,
    MusicGenre PreferredGenre,
    EnergyLevel TargetEnergy,
    int? TargetBPMMin,
    int? TargetBPMMax,
    List<string> Keywords,
    string Reasoning
);

/// <summary>
/// A music suggestion with ranking information.
/// </summary>
public record MusicSuggestion(
    MusicAsset Track,
    double RelevanceScore,
    string MatchReasoning,
    List<string> MatchingAttributes,
    bool IsRecommended
);
