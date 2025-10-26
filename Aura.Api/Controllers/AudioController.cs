using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Services.AudioIntelligence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered audio intelligence and music recommendations
/// </summary>
[ApiController]
[Route("api/audio")]
public class AudioController : ControllerBase
{
    private readonly ILogger<AudioController> _logger;
    private readonly MusicRecommendationService _musicService;
    private readonly BeatDetectionService _beatService;
    private readonly VoiceDirectionService _voiceService;
    private readonly SoundEffectService _soundEffectService;
    private readonly AudioMixingService _mixingService;
    private readonly AudioContinuityService _continuityService;

    public AudioController(
        ILogger<AudioController> logger,
        MusicRecommendationService musicService,
        BeatDetectionService beatService,
        VoiceDirectionService voiceService,
        SoundEffectService soundEffectService,
        AudioMixingService mixingService,
        AudioContinuityService continuityService)
    {
        _logger = logger;
        _musicService = musicService;
        _beatService = beatService;
        _voiceService = voiceService;
        _soundEffectService = soundEffectService;
        _mixingService = mixingService;
        _continuityService = continuityService;
    }

    /// <summary>
    /// Analyze script for audio requirements
    /// </summary>
    [HttpPost("analyze-script")]
    public async Task<IActionResult> AnalyzeScript(
        [FromBody] AnalyzeScriptRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new { success = false, error = "Script is required" });
            }

            _logger.LogInformation("Analyzing script for audio requirements");

            // In production, would integrate with ScriptAnalysisService from PR 20
            // For now, provide basic analysis
            var analysis = new ScriptAudioAnalysis(
                EmotionalArc: new List<MusicMood> { MusicMood.Neutral, MusicMood.Energetic },
                EnergyProgression: new List<EnergyLevel> { EnergyLevel.Medium, EnergyLevel.High },
                SoundEffects: new List<SoundEffectSuggestion>(),
                VoiceHints: new List<VoiceDirectionHint>(),
                OverallTone: "Professional and engaging",
                EstimatedDuration: TimeSpan.FromMinutes(2)
            );

            return Ok(new { success = true, analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing script");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Suggest music tracks based on mood and context
    /// </summary>
    [HttpPost("suggest-music")]
    public async Task<IActionResult> SuggestMusic(
        [FromBody] SuggestMusicRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Suggesting music for mood={Mood}, energy={Energy}", 
                request.Mood, request.Energy);

            var recommendations = await _musicService.RecommendMusicAsync(
                request.Mood,
                request.PreferredGenre,
                request.Energy,
                request.Duration,
                request.Context,
                request.MaxResults ?? 10,
                ct);

            return Ok(new { success = true, recommendations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting music");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Detect beats in a music file
    /// </summary>
    [HttpPost("detect-beats")]
    public async Task<IActionResult> DetectBeats(
        [FromBody] DetectBeatsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return BadRequest(new { success = false, error = "File path is required" });
            }

            _logger.LogInformation("Detecting beats in file: {FilePath}", request.FilePath);

            var beats = await _beatService.DetectBeatsAsync(
                request.FilePath,
                request.MinBPM ?? 60,
                request.MaxBPM ?? 200,
                ct);

            var bpm = _beatService.CalculateBPM(beats);
            var phrases = _beatService.IdentifyMusicalPhrases(beats);
            var climaxMoments = _beatService.FindClimaxMoments(beats);

            return Ok(new
            {
                success = true,
                beats,
                bpm,
                phrases,
                climaxMoments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting beats");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Generate voice direction parameters for TTS
    /// </summary>
    [HttpPost("voice-direction")]
    public async Task<IActionResult> VoiceDirection(
        [FromBody] VoiceDirectionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new { success = false, error = "Script is required" });
            }

            _logger.LogInformation("Generating voice direction");

            var directions = await _voiceService.GenerateVoiceDirectionAsync(
                request.Script,
                request.ContentType,
                request.TargetAudience,
                request.KeyMessages,
                ct);

            return Ok(new { success = true, directions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating voice direction");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Suggest sound effects with timing
    /// </summary>
    [HttpPost("sound-effects")]
    public async Task<IActionResult> SoundEffects(
        [FromBody] SoundEffectRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return BadRequest(new { success = false, error = "Script is required" });
            }

            _logger.LogInformation("Suggesting sound effects");

            var effects = await _soundEffectService.SuggestSoundEffectsAsync(
                request.Script,
                request.SceneDurations,
                request.ContentType,
                ct);

            var optimizedEffects = _soundEffectService.OptimizeTiming(effects);

            return Ok(new
            {
                success = true,
                soundEffects = optimizedEffects,
                totalEffects = optimizedEffects.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting sound effects");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Generate mixing recommendations
    /// </summary>
    [HttpPost("mixing-suggestions")]
    public async Task<IActionResult> MixingSuggestions(
        [FromBody] MixingSuggestionsRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Generating mixing suggestions for content type: {ContentType}", 
                request.ContentType);

            var mixing = await _mixingService.GenerateMixingSuggestionsAsync(
                request.ContentType,
                request.HasNarration,
                request.HasMusic,
                request.HasSoundEffects,
                request.TargetLUFS ?? -14.0,
                ct);

            var (isValid, issues) = _mixingService.ValidateMixing(mixing);
            var conflicts = await _mixingService.DetectFrequencyConflictsAsync(
                request.HasNarration,
                request.HasMusic,
                request.HasSoundEffects,
                ct);
            var stereoPlacement = _mixingService.SuggestStereoPlacement(
                request.HasNarration,
                request.HasMusic,
                request.HasSoundEffects);

            return Ok(new
            {
                success = true,
                mixing,
                isValid,
                validationIssues = issues,
                frequencyConflicts = conflicts,
                stereoPlacement,
                ffmpegFilter = _mixingService.GenerateFFmpegMixingFilter(mixing)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mixing suggestions");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Create AI music generation prompts
    /// </summary>
    [HttpPost("music-prompts")]
    public IActionResult MusicPrompts([FromBody] MusicPromptRequest request)
    {
        try
        {
            _logger.LogInformation("Creating music generation prompt");

            var prompt = new MusicPrompt(
                PromptId: Guid.NewGuid().ToString(),
                Mood: request.Mood,
                Genre: request.Genre,
                Energy: request.Energy,
                TargetDuration: request.Duration,
                TargetBPM: null,
                Instrumentation: GenerateInstrumentationSuggestion(request.Genre, request.Mood),
                Style: GenerateStyleDescription(request.Mood, request.Energy),
                ReferenceTrackId: null,
                CreatedAt: DateTime.UtcNow
            );

            var textPrompt = $"Create a {request.Energy} energy {request.Genre} track with a {request.Mood} mood. " +
                           $"Duration should be approximately {request.Duration.TotalSeconds:F0} seconds. " +
                           $"Instrumentation: {prompt.Instrumentation}. " +
                           $"Style: {prompt.Style}.";

            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                textPrompt += $" Context: {request.AdditionalContext}";
            }

            return Ok(new
            {
                success = true,
                prompt,
                textPrompt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating music prompt");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Analyze audio-visual synchronization
    /// </summary>
    [HttpPost("sync-analysis")]
    public async Task<IActionResult> SyncAnalysis(
        [FromBody] SyncAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Analyzing audio-visual synchronization");

            var analysis = await _continuityService.AnalyzeSynchronizationAsync(
                request.AudioBeatTimestamps,
                request.VisualTransitionTimestamps,
                request.VideoDuration,
                ct);

            return Ok(new { success = true, analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing synchronization");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Check audio continuity across segments
    /// </summary>
    [HttpPost("continuity-check")]
    public async Task<IActionResult> ContinuityCheck(
        [FromBody] ContinuityCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.AudioSegmentPaths == null || request.AudioSegmentPaths.Count == 0)
            {
                return BadRequest(new { success = false, error = "Audio segments are required" });
            }

            _logger.LogInformation("Checking audio continuity for {Count} segments", 
                request.AudioSegmentPaths.Count);

            var continuity = await _continuityService.CheckContinuityAsync(
                request.AudioSegmentPaths,
                request.TargetStyle,
                ct);

            return Ok(new { success = true, continuity });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking continuity");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Access categorized music library
    /// </summary>
    [HttpGet("music-library")]
    public IActionResult MusicLibrary([FromQuery] MusicSearchParams? searchParams)
    {
        try
        {
            _logger.LogInformation("Fetching music library");

            // In production, would query actual music database
            var mockLibrary = GetMockMusicLibrary();

            // Apply filters if provided
            var filtered = mockLibrary.AsEnumerable();

            if (searchParams?.Mood != null)
            {
                filtered = filtered.Where(t => t.Mood == searchParams.Mood);
            }

            if (searchParams?.Genre != null)
            {
                filtered = filtered.Where(t => t.Genre == searchParams.Genre);
            }

            if (searchParams?.Energy != null)
            {
                filtered = filtered.Where(t => t.Energy == searchParams.Energy);
            }

            if (searchParams?.MinBPM != null)
            {
                filtered = filtered.Where(t => t.BPM >= searchParams.MinBPM);
            }

            if (searchParams?.MaxBPM != null)
            {
                filtered = filtered.Where(t => t.BPM <= searchParams.MaxBPM);
            }

            if (searchParams?.MinDuration != null)
            {
                filtered = filtered.Where(t => t.Duration >= searchParams.MinDuration);
            }

            if (searchParams?.MaxDuration != null)
            {
                filtered = filtered.Where(t => t.Duration <= searchParams.MaxDuration);
            }

            if (!string.IsNullOrWhiteSpace(searchParams?.SearchQuery))
            {
                var query = searchParams.SearchQuery.ToLowerInvariant();
                filtered = filtered.Where(t =>
                    t.Title.ToLowerInvariant().Contains(query) ||
                    t.Artist?.ToLowerInvariant().Contains(query) == true);
            }

            var results = filtered.Take(searchParams?.Limit ?? 50).ToList();

            return Ok(new
            {
                success = true,
                tracks = results,
                totalCount = results.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching music library");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // Helper methods

    private string GenerateInstrumentationSuggestion(MusicGenre genre, MusicMood mood)
    {
        return genre switch
        {
            MusicGenre.Cinematic => "Orchestra, strings, brass, percussion",
            MusicGenre.Electronic => "Synthesizers, electronic drums, bass",
            MusicGenre.Rock => "Electric guitar, bass, drums",
            MusicGenre.Pop => "Vocals, synths, drums, bass",
            MusicGenre.Ambient => "Pads, atmospheric sounds, light percussion",
            MusicGenre.Classical => "Piano, strings, woodwinds",
            MusicGenre.Jazz => "Saxophone, piano, double bass, drums",
            MusicGenre.Corporate => "Piano, light strings, subtle percussion",
            MusicGenre.Orchestral => "Full orchestra with strings, brass, woodwinds, percussion",
            MusicGenre.LoFi => "Vinyl texture, mellow beats, piano, guitar",
            _ => "Mixed instrumentation"
        };
    }

    private string GenerateStyleDescription(MusicMood mood, EnergyLevel energy)
    {
        var moodDesc = mood switch
        {
            MusicMood.Happy => "upbeat and positive",
            MusicMood.Sad => "melancholic and reflective",
            MusicMood.Energetic => "dynamic and powerful",
            MusicMood.Calm => "peaceful and serene",
            MusicMood.Dramatic => "intense and emotional",
            MusicMood.Tense => "suspenseful and building",
            MusicMood.Uplifting => "inspiring and hopeful",
            MusicMood.Epic => "grand and heroic",
            _ => "balanced"
        };

        var energyDesc = energy switch
        {
            EnergyLevel.VeryLow => "very subtle and minimal",
            EnergyLevel.Low => "gentle and understated",
            EnergyLevel.Medium => "moderate and steady",
            EnergyLevel.High => "vibrant and engaging",
            EnergyLevel.VeryHigh => "intense and driving",
            _ => "moderate"
        };

        return $"{moodDesc} with {energyDesc} energy";
    }

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
        };
    }
}

// Request DTOs for Audio Controller
public record DetectBeatsRequest(string FilePath, int? MinBPM, int? MaxBPM);
