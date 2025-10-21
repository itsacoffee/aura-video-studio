using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for intelligent music recommendation based on emotion and content
/// </summary>
public class MusicRecommendationService
{
    private readonly ILogger<MusicRecommendationService> _logger;
    private readonly ILlmProvider _llmProvider;

    public MusicRecommendationService(
        ILogger<MusicRecommendationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Recommends music tracks based on mood, genre, and context
    /// </summary>
    public async Task<List<MusicRecommendation>> RecommendMusicAsync(
        MusicMood mood,
        MusicGenre? preferredGenre,
        EnergyLevel energy,
        TimeSpan duration,
        string? context = null,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Recommending music for mood={Mood}, genre={Genre}, energy={Energy}, duration={Duration}",
            mood, preferredGenre, energy, duration);

        try
        {
            // Get available tracks from mock library (in production, this would query a real database)
            var availableTracks = GetMockMusicLibrary();

            // Filter by basic criteria
            var candidateTracks = availableTracks
                .Where(t => t.Mood == mood || IsCompatibleMood(t.Mood, mood))
                .Where(t => preferredGenre == null || t.Genre == preferredGenre)
                .Where(t => IsCompatibleEnergy(t.Energy, energy))
                .Where(t => t.Duration >= duration.Subtract(TimeSpan.FromSeconds(10)))
                .ToList();

            if (candidateTracks.Count == 0)
            {
                _logger.LogWarning("No tracks found matching criteria, relaxing filters");
                candidateTracks = availableTracks.Where(t => IsCompatibleEnergy(t.Energy, energy)).ToList();
            }

            // Use AI to rank and explain recommendations
            var recommendations = await RankTracksWithAIAsync(candidateTracks, mood, energy, context, ct);

            return recommendations.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending music");
            throw;
        }
    }

    /// <summary>
    /// Analyzes script emotional arc and suggests music for each segment
    /// </summary>
    public async Task<List<(TimeSpan Start, TimeSpan Duration, MusicRecommendation Recommendation)>> 
        RecommendMusicForScriptAsync(
            List<EmotionalPoint> emotionalArc,
            List<TimeSpan> sceneDurations,
            string? contentType = null,
            CancellationToken ct = default)
    {
        _logger.LogInformation("Recommending music for {Count} scenes in emotional arc", emotionalArc.Count);

        var recommendations = new List<(TimeSpan, TimeSpan, MusicRecommendation)>();
        var currentTime = TimeSpan.Zero;

        for (int i = 0; i < emotionalArc.Count && i < sceneDurations.Count; i++)
        {
            var point = emotionalArc[i];
            var duration = sceneDurations[i];

            // Map emotional tone to music mood
            var mood = MapEmotionalToneToMood(point.Tone);
            var energy = MapIntensityToEnergy(point.Intensity);

            var musicRecs = await RecommendMusicAsync(mood, null, energy, duration, point.Context, 1, ct);
            
            if (musicRecs.Count > 0)
            {
                recommendations.Add((currentTime, duration, musicRecs[0]));
            }

            currentTime += duration;
        }

        return recommendations;
    }

    /// <summary>
    /// Uses AI to rank tracks and provide reasoning
    /// </summary>
    private async Task<List<MusicRecommendation>> RankTracksWithAIAsync(
        List<MusicTrack> tracks,
        MusicMood targetMood,
        EnergyLevel targetEnergy,
        string? context,
        CancellationToken ct)
    {
        if (tracks.Count == 0)
        {
            return new List<MusicRecommendation>();
        }

        // For now, use simple scoring (in production, would use AI for more sophisticated ranking)
        var recommendations = tracks.Select(track =>
        {
            var moodScore = track.Mood == targetMood ? 100 : IsCompatibleMood(track.Mood, targetMood) ? 70 : 30;
            var energyScore = track.Energy == targetEnergy ? 100 : IsCompatibleEnergy(track.Energy, targetEnergy) ? 70 : 30;
            var overallScore = (moodScore + energyScore) / 2.0;

            var attributes = new List<string>();
            if (track.Mood == targetMood) attributes.Add($"Perfect mood match: {targetMood}");
            if (track.Energy == targetEnergy) attributes.Add($"Perfect energy match: {targetEnergy}");
            attributes.Add($"{track.BPM} BPM");
            attributes.Add($"{track.Genre} genre");

            var reasoning = $"This {track.Genre} track provides {track.Mood} mood with {track.Energy} energy, " +
                          $"matching your {targetMood} mood requirement at {targetEnergy} energy level.";

            if (!string.IsNullOrEmpty(context))
            {
                reasoning += $" Suitable for: {context}";
            }

            return new MusicRecommendation(
                Track: track,
                RelevanceScore: overallScore,
                Reasoning: reasoning,
                MatchingAttributes: attributes,
                SuggestedStartTime: TimeSpan.Zero,
                SuggestedDuration: track.Duration
            );
        })
        .OrderByDescending(r => r.RelevanceScore)
        .ToList();

        return recommendations;
    }

    /// <summary>
    /// Maps emotional tone to music mood
    /// </summary>
    private MusicMood MapEmotionalToneToMood(EmotionalTone tone)
    {
        return tone switch
        {
            EmotionalTone.Excited => MusicMood.Energetic,
            EmotionalTone.Curious => MusicMood.Mysterious,
            EmotionalTone.Concerned => MusicMood.Tense,
            EmotionalTone.Hopeful => MusicMood.Uplifting,
            EmotionalTone.Satisfied => MusicMood.Happy,
            EmotionalTone.Inspired => MusicMood.Epic,
            EmotionalTone.Empowered => MusicMood.Uplifting,
            EmotionalTone.Entertained => MusicMood.Playful,
            EmotionalTone.Thoughtful => MusicMood.Calm,
            EmotionalTone.Urgent => MusicMood.Tense,
            EmotionalTone.Relieved => MusicMood.Calm,
            _ => MusicMood.Neutral
        };
    }

    /// <summary>
    /// Maps intensity to energy level
    /// </summary>
    private EnergyLevel MapIntensityToEnergy(double intensity)
    {
        return intensity switch
        {
            < 20 => EnergyLevel.VeryLow,
            < 40 => EnergyLevel.Low,
            < 60 => EnergyLevel.Medium,
            < 80 => EnergyLevel.High,
            _ => EnergyLevel.VeryHigh
        };
    }

    /// <summary>
    /// Checks if two moods are compatible
    /// </summary>
    private bool IsCompatibleMood(MusicMood track, MusicMood target)
    {
        var compatibilityMap = new Dictionary<MusicMood, List<MusicMood>>
        {
            [MusicMood.Happy] = new() { MusicMood.Uplifting, MusicMood.Playful, MusicMood.Energetic },
            [MusicMood.Sad] = new() { MusicMood.Melancholic, MusicMood.Calm, MusicMood.Serious },
            [MusicMood.Energetic] = new() { MusicMood.Happy, MusicMood.Uplifting, MusicMood.Epic },
            [MusicMood.Calm] = new() { MusicMood.Ambient, MusicMood.Neutral, MusicMood.Melancholic },
            [MusicMood.Dramatic] = new() { MusicMood.Epic, MusicMood.Tense, MusicMood.Serious },
            [MusicMood.Tense] = new() { MusicMood.Dramatic, MusicMood.Mysterious, MusicMood.Serious },
            [MusicMood.Uplifting] = new() { MusicMood.Happy, MusicMood.Energetic, MusicMood.Epic },
            [MusicMood.Mysterious] = new() { MusicMood.Ambient, MusicMood.Tense, MusicMood.Serious },
        };

        return compatibilityMap.TryGetValue(target, out var compatible) && compatible.Contains(track);
    }

    /// <summary>
    /// Checks if energy levels are compatible (within 1 level)
    /// </summary>
    private bool IsCompatibleEnergy(EnergyLevel track, EnergyLevel target)
    {
        var diff = Math.Abs((int)track - (int)target);
        return diff <= 1;
    }

    /// <summary>
    /// Mock music library (in production, this would come from a database or API)
    /// </summary>
    private List<MusicTrack> GetMockMusicLibrary()
    {
        return new List<MusicTrack>
        {
            new("track_001", "Upbeat Corporate", "AudioLib", MusicGenre.Corporate, MusicMood.Uplifting, 
                EnergyLevel.High, 128, TimeSpan.FromMinutes(3), "/music/upbeat_corporate.mp3", null, null),
            new("track_002", "Calm Ambient", "AudioLib", MusicGenre.Ambient, MusicMood.Calm, 
                EnergyLevel.Low, 80, TimeSpan.FromMinutes(4), "/music/calm_ambient.mp3", null, null),
            new("track_003", "Energetic Electronic", "AudioLib", MusicGenre.Electronic, MusicMood.Energetic, 
                EnergyLevel.VeryHigh, 140, TimeSpan.FromMinutes(2.5), "/music/energetic_electronic.mp3", null, null),
            new("track_004", "Epic Orchestral", "AudioLib", MusicGenre.Orchestral, MusicMood.Epic, 
                EnergyLevel.High, 110, TimeSpan.FromMinutes(3.5), "/music/epic_orchestral.mp3", null, null),
            new("track_005", "Playful Indie", "AudioLib", MusicGenre.Indie, MusicMood.Playful, 
                EnergyLevel.Medium, 115, TimeSpan.FromMinutes(2.8), "/music/playful_indie.mp3", null, null),
            new("track_006", "Serious Cinematic", "AudioLib", MusicGenre.Cinematic, MusicMood.Serious, 
                EnergyLevel.Medium, 95, TimeSpan.FromMinutes(4.2), "/music/serious_cinematic.mp3", null, null),
            new("track_007", "Tense Drama", "AudioLib", MusicGenre.Cinematic, MusicMood.Tense, 
                EnergyLevel.High, 120, TimeSpan.FromMinutes(3), "/music/tense_drama.mp3", null, null),
            new("track_008", "Happy Pop", "AudioLib", MusicGenre.Pop, MusicMood.Happy, 
                EnergyLevel.High, 125, TimeSpan.FromMinutes(3.2), "/music/happy_pop.mp3", null, null),
            new("track_009", "Melancholic Piano", "AudioLib", MusicGenre.Classical, MusicMood.Melancholic, 
                EnergyLevel.Low, 70, TimeSpan.FromMinutes(5), "/music/melancholic_piano.mp3", null, null),
            new("track_010", "Motivational Rock", "AudioLib", MusicGenre.Rock, MusicMood.Uplifting, 
                EnergyLevel.VeryHigh, 135, TimeSpan.FromMinutes(2.5), "/music/motivational_rock.mp3", null, null),
        };
    }
}
