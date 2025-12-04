using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// AI-driven music selection service that uses LLM to analyze content
/// and match appropriate background music for videos.
/// </summary>
public class MusicMatchingService : IMusicMatchingService
{
    private readonly ILogger<MusicMatchingService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly IMusicProvider _musicProvider;
    private readonly IFFmpegService _ffmpegService;

    public MusicMatchingService(
        ILogger<MusicMatchingService> logger,
        ILlmProvider llmProvider,
        IMusicProvider musicProvider,
        IFFmpegService ffmpegService)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _musicProvider = musicProvider;
        _ffmpegService = ffmpegService;
    }

    /// <inheritdoc />
    public async Task<MusicMatchParameters> AnalyzeContentForMusicAsync(
        Brief brief,
        IReadOnlyList<Scene> scenes,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content for music matching. Topic: {Topic}, Scenes: {Count}",
            brief.Topic, scenes.Count);

        try
        {
            var sceneDescriptions = scenes.Select(s => $"Scene {s.Index + 1}: {s.Heading} - {TruncateText(s.Script, 100)}").ToList();
            var prompt = BuildMusicAnalysisPrompt(brief, sceneDescriptions);

            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            var parameters = ParseMusicParameters(response, brief);

            _logger.LogInformation("Music analysis complete. Mood: {Mood}, Genre: {Genre}, Energy: {Energy}",
                parameters.PrimaryMood, parameters.PreferredGenre, parameters.TargetEnergy);

            return parameters;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM analysis failed, using fallback parameters");
            return CreateFallbackParameters(brief);
        }
    }

    /// <inheritdoc />
    public async Task<List<MusicSuggestion>> GetMusicSuggestionsAsync(
        MusicMatchParameters parameters,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for music with parameters: Mood={Mood}, Genre={Genre}, Energy={Energy}",
            parameters.PrimaryMood, parameters.PreferredGenre, parameters.TargetEnergy);

        var criteria = new MusicSearchCriteria(
            Mood: parameters.PrimaryMood,
            Genre: parameters.PreferredGenre,
            Energy: parameters.TargetEnergy,
            MinBPM: parameters.TargetBPMMin,
            MaxBPM: parameters.TargetBPMMax,
            PageSize: maxResults
        );

        var searchResult = await _musicProvider.SearchAsync(criteria, ct).ConfigureAwait(false);

        var suggestions = searchResult.Results.Select(track => new MusicSuggestion(
            Track: track,
            RelevanceScore: CalculateRelevanceScore(track, parameters),
            MatchReasoning: GenerateMatchReasoning(track, parameters),
            MatchingAttributes: GetMatchingAttributes(track, parameters),
            IsRecommended: false
        )).ToList();

        // Mark top suggestion as recommended
        if (suggestions.Count > 0)
        {
            var sorted = suggestions.OrderByDescending(s => s.RelevanceScore).ToList();
            if (sorted.Count > 0)
            {
                sorted[0] = sorted[0] with { IsRecommended = true };
            }
            return sorted;
        }

        return suggestions;
    }

    /// <inheritdoc />
    public async Task<string> PrepareSelectedMusicAsync(
        MusicSuggestion suggestion,
        TimeSpan targetDuration,
        string outputPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Preparing music: {Title} for duration {Duration}",
            suggestion.Track.Title, targetDuration);

        // Download the music file first
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "MusicPrep");
        Directory.CreateDirectory(tempDir);
        var downloadPath = Path.Combine(tempDir, $"music_{Guid.NewGuid()}.mp3");

        await _musicProvider.DownloadAsync(suggestion.Track.AssetId, downloadPath, ct).ConfigureAwait(false);

        if (!File.Exists(downloadPath))
        {
            throw new FileNotFoundException("Failed to download music file");
        }

        try
        {
            var trackDuration = suggestion.Track.Duration;

            // Determine how to handle duration mismatch
            if (trackDuration >= targetDuration)
            {
                // Trim and fade out
                await TrimWithFadeAsync(downloadPath, outputPath, targetDuration, ct).ConfigureAwait(false);
            }
            else
            {
                // Loop to match duration
                await LoopToMatchDurationAsync(downloadPath, outputPath, targetDuration, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Music prepared successfully: {Output}", outputPath);
            return outputPath;
        }
        finally
        {
            // Cleanup temp file
            try
            {
                if (File.Exists(downloadPath))
                    File.Delete(downloadPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp music file");
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<MusicSuggestion>> RankSuggestionsAsync(
        List<MusicSuggestion> suggestions,
        Brief brief,
        IReadOnlyList<Scene> scenes,
        CancellationToken ct = default)
    {
        if (suggestions.Count <= 1)
            return suggestions;

        _logger.LogInformation("Ranking {Count} music suggestions using AI", suggestions.Count);

        try
        {
            var prompt = BuildRankingPrompt(suggestions, brief, scenes);
            var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
            var rankings = ParseRankings(response, suggestions.Count);

            var ranked = suggestions
                .Select((s, i) => s with { RelevanceScore = rankings.TryGetValue(i, out var score) ? score : s.RelevanceScore })
                .OrderByDescending(s => s.RelevanceScore)
                .ToList();

            // Mark top as recommended
            if (ranked.Count > 0)
            {
                ranked[0] = ranked[0] with { IsRecommended = true };
            }

            return ranked;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI ranking failed, using score-based ranking");
            return suggestions.OrderByDescending(s => s.RelevanceScore).ToList();
        }
    }

    private string BuildMusicAnalysisPrompt(Brief brief, List<string> sceneDescriptions)
    {
        return $@"Analyze this video content and recommend background music characteristics.

Video Topic: {brief.Topic}
Target Audience: {brief.Audience ?? "General"}
Goal: {brief.Goal ?? "Engage viewers"}
Tone: {brief.Tone ?? "Professional"}

Scenes:
{string.Join("\n", sceneDescriptions)}

Respond with JSON containing:
{{
    ""mood"": ""one of: Neutral, Happy, Sad, Energetic, Calm, Dramatic, Tense, Uplifting, Melancholic, Mysterious, Playful, Serious, Epic, Ambient"",
    ""secondaryMood"": ""optional secondary mood or null"",
    ""genre"": ""one of: Cinematic, Electronic, Rock, Pop, Ambient, Classical, Jazz, HipHop, Folk, Corporate, Orchestral, Indie, LoFi, Motivational"",
    ""energy"": ""one of: VeryLow, Low, Medium, High, VeryHigh"",
    ""bpmMin"": number or null,
    ""bpmMax"": number or null,
    ""keywords"": [""list"", ""of"", ""relevant"", ""keywords""],
    ""reasoning"": ""Brief explanation of why these parameters fit the content""
}}";
    }

    private MusicMatchParameters ParseMusicParameters(string response, Brief brief)
    {
        try
        {
            // Extract JSON from response
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var moodStr = root.GetProperty("mood").GetString() ?? "Neutral";
                var genreStr = root.GetProperty("genre").GetString() ?? "Corporate";
                var energyStr = root.GetProperty("energy").GetString() ?? "Medium";

                return new MusicMatchParameters(
                    PrimaryMood: Enum.TryParse<MusicMood>(moodStr, true, out var mood) ? mood : MusicMood.Neutral,
                    SecondaryMood: root.TryGetProperty("secondaryMood", out var sm) && sm.ValueKind != JsonValueKind.Null
                        ? Enum.TryParse<MusicMood>(sm.GetString(), true, out var sm2) ? sm2 : null
                        : null,
                    PreferredGenre: Enum.TryParse<MusicGenre>(genreStr, true, out var genre) ? genre : MusicGenre.Corporate,
                    TargetEnergy: Enum.TryParse<EnergyLevel>(energyStr, true, out var energy) ? energy : EnergyLevel.Medium,
                    TargetBPMMin: root.TryGetProperty("bpmMin", out var bpmMin) && bpmMin.ValueKind == JsonValueKind.Number ? bpmMin.GetInt32() : null,
                    TargetBPMMax: root.TryGetProperty("bpmMax", out var bpmMax) && bpmMax.ValueKind == JsonValueKind.Number ? bpmMax.GetInt32() : null,
                    Keywords: root.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array
                        ? kw.EnumerateArray().Select(k => k.GetString() ?? "").Where(k => !string.IsNullOrEmpty(k)).ToList()
                        : new List<string>(),
                    Reasoning: root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : ""
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON");
        }

        return CreateFallbackParameters(brief);
    }

    private MusicMatchParameters CreateFallbackParameters(Brief brief)
    {
        // Infer parameters from brief tone
        var tone = brief.Tone?.ToLowerInvariant() ?? "";
        var mood = tone switch
        {
            var t when t.Contains("professional") || t.Contains("corporate") => MusicMood.Neutral,
            var t when t.Contains("exciting") || t.Contains("energetic") => MusicMood.Energetic,
            var t when t.Contains("calm") || t.Contains("relaxing") => MusicMood.Calm,
            var t when t.Contains("inspiring") || t.Contains("motivational") => MusicMood.Uplifting,
            var t when t.Contains("serious") || t.Contains("dramatic") => MusicMood.Serious,
            _ => MusicMood.Neutral
        };

        var genre = tone switch
        {
            var t when t.Contains("corporate") || t.Contains("business") => MusicGenre.Corporate,
            var t when t.Contains("modern") || t.Contains("tech") => MusicGenre.Electronic,
            var t when t.Contains("cinematic") || t.Contains("epic") => MusicGenre.Cinematic,
            _ => MusicGenre.Corporate
        };

        return new MusicMatchParameters(
            PrimaryMood: mood,
            SecondaryMood: null,
            PreferredGenre: genre,
            TargetEnergy: EnergyLevel.Medium,
            TargetBPMMin: null,
            TargetBPMMax: null,
            Keywords: new List<string> { brief.Topic ?? "background" },
            Reasoning: "Fallback parameters based on video brief tone"
        );
    }

    private double CalculateRelevanceScore(MusicAsset track, MusicMatchParameters parameters)
    {
        double score = 0;

        // Mood match (40 points)
        if (track.Mood == parameters.PrimaryMood)
            score += 40;
        else if (parameters.SecondaryMood.HasValue && track.Mood == parameters.SecondaryMood.Value)
            score += 25;
        else if (IsCompatibleMood(track.Mood, parameters.PrimaryMood))
            score += 15;

        // Genre match (30 points)
        if (track.Genre == parameters.PreferredGenre)
            score += 30;
        else if (IsCompatibleGenre(track.Genre, parameters.PreferredGenre))
            score += 15;

        // Energy match (20 points)
        if (track.Energy == parameters.TargetEnergy)
            score += 20;
        else if (Math.Abs((int)track.Energy - (int)parameters.TargetEnergy) == 1)
            score += 10;

        // BPM match (10 points)
        if (parameters.TargetBPMMin.HasValue || parameters.TargetBPMMax.HasValue)
        {
            var inRange = (!parameters.TargetBPMMin.HasValue || track.BPM >= parameters.TargetBPMMin.Value)
                && (!parameters.TargetBPMMax.HasValue || track.BPM <= parameters.TargetBPMMax.Value);
            if (inRange)
                score += 10;
        }
        else
        {
            score += 5; // No BPM constraint
        }

        return score;
    }

    private bool IsCompatibleMood(MusicMood track, MusicMood target)
    {
        var compatibilityMap = new Dictionary<MusicMood, List<MusicMood>>
        {
            [MusicMood.Happy] = new() { MusicMood.Uplifting, MusicMood.Playful, MusicMood.Energetic },
            [MusicMood.Sad] = new() { MusicMood.Melancholic, MusicMood.Calm },
            [MusicMood.Energetic] = new() { MusicMood.Happy, MusicMood.Uplifting, MusicMood.Epic },
            [MusicMood.Calm] = new() { MusicMood.Ambient, MusicMood.Neutral, MusicMood.Melancholic },
            [MusicMood.Dramatic] = new() { MusicMood.Epic, MusicMood.Tense, MusicMood.Serious },
            [MusicMood.Uplifting] = new() { MusicMood.Happy, MusicMood.Energetic, MusicMood.Epic },
            [MusicMood.Neutral] = new() { MusicMood.Calm, MusicMood.Ambient }
        };

        return compatibilityMap.TryGetValue(target, out var compatible) && compatible.Contains(track);
    }

    private bool IsCompatibleGenre(MusicGenre track, MusicGenre target)
    {
        var compatibilityMap = new Dictionary<MusicGenre, List<MusicGenre>>
        {
            [MusicGenre.Corporate] = new() { MusicGenre.Pop, MusicGenre.Motivational },
            [MusicGenre.Cinematic] = new() { MusicGenre.Orchestral, MusicGenre.Classical },
            [MusicGenre.Electronic] = new() { MusicGenre.Pop, MusicGenre.LoFi },
            [MusicGenre.Ambient] = new() { MusicGenre.LoFi, MusicGenre.Classical }
        };

        return compatibilityMap.TryGetValue(target, out var compatible) && compatible.Contains(track);
    }

    private string GenerateMatchReasoning(MusicAsset track, MusicMatchParameters parameters)
    {
        var reasons = new List<string>();

        if (track.Mood == parameters.PrimaryMood)
            reasons.Add($"mood matches ({track.Mood})");
        if (track.Genre == parameters.PreferredGenre)
            reasons.Add($"genre matches ({track.Genre})");
        if (track.Energy == parameters.TargetEnergy)
            reasons.Add($"energy level matches ({track.Energy})");

        return reasons.Count > 0
            ? $"This track fits well because {string.Join(", ", reasons)}."
            : "This track has compatible characteristics for your content.";
    }

    private List<string> GetMatchingAttributes(MusicAsset track, MusicMatchParameters parameters)
    {
        var attrs = new List<string>();

        if (track.Mood == parameters.PrimaryMood)
            attrs.Add($"Mood: {track.Mood}");
        if (track.Genre == parameters.PreferredGenre)
            attrs.Add($"Genre: {track.Genre}");
        if (track.Energy == parameters.TargetEnergy)
            attrs.Add($"Energy: {track.Energy}");

        attrs.Add($"BPM: {track.BPM}");
        attrs.Add($"Duration: {track.Duration:mm\\:ss}");

        return attrs;
    }

    private string BuildRankingPrompt(List<MusicSuggestion> suggestions, Brief brief, IReadOnlyList<Scene> scenes)
    {
        var trackList = string.Join("\n", suggestions.Select((s, i) =>
            $"{i + 1}. {s.Track.Title} - {s.Track.Genre}, {s.Track.Mood}, {s.Track.Energy} energy, {s.Track.BPM} BPM"));

        return $@"Rank these music tracks for a video about ""{brief.Topic}"" (tone: {brief.Tone ?? "neutral"}).

Tracks:
{trackList}

Number of scenes: {scenes.Count}

Respond with JSON: {{ ""rankings"": {{ ""0"": score, ""1"": score, ... }} }}
where index is 0-based and score is 0-100.";
    }

    private Dictionary<int, double> ParseRankings(string response, int count)
    {
        var rankings = new Dictionary<int, double>();
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("rankings", out var r))
                {
                    foreach (var prop in r.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out var idx) && idx >= 0 && idx < count)
                        {
                            rankings[idx] = prop.Value.GetDouble();
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Return empty rankings on parse failure
        }
        return rankings;
    }

    private async Task TrimWithFadeAsync(string inputPath, string outputPath, TimeSpan targetDuration, CancellationToken ct)
    {
        var fadeOutStart = targetDuration.TotalSeconds - 3; // 3 second fade out
        if (fadeOutStart < 0) fadeOutStart = 0;

        var filter = $"afade=t=out:st={fadeOutStart:F2}:d=3";
        var arguments = $"-i \"{inputPath}\" -t {targetDuration.TotalSeconds:F2} -af \"{filter}\" -y \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException($"FFmpeg trim failed: {result.ErrorMessage}");
        }
    }

    private async Task LoopToMatchDurationAsync(string inputPath, string outputPath, TimeSpan targetDuration, CancellationToken ct)
    {
        // Use aloop filter to loop audio, then add fade out
        var fadeOutStart = targetDuration.TotalSeconds - 3;
        if (fadeOutStart < 0) fadeOutStart = 0;

        // Calculate number of loops needed
        var fadeFilter = $"afade=t=out:st={fadeOutStart:F2}:d=3";
        var arguments = $"-stream_loop -1 -i \"{inputPath}\" -t {targetDuration.TotalSeconds:F2} -af \"{fadeFilter}\" -y \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);
        if (!result.Success)
        {
            throw new InvalidOperationException($"FFmpeg loop failed: {result.ErrorMessage}");
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength) + "...";
    }
}
