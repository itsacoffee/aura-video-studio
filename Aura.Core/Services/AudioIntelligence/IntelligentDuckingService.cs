using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Intelligent audio ducking service that analyzes narration to detect silence
/// and applies context-aware ducking profiles using FFmpeg sidechain compression.
/// </summary>
public class IntelligentDuckingService : IIntelligentDuckingService
{
    private readonly ILogger<IntelligentDuckingService> _logger;
    private readonly IFFmpegService _ffmpegService;

    private static readonly Dictionary<DuckingProfileType, DuckingProfile> DefaultProfiles = new()
    {
        [DuckingProfileType.Aggressive] = new DuckingProfile(
            Type: DuckingProfileType.Aggressive,
            DuckDepthDb: -20,
            AttackTime: TimeSpan.FromMilliseconds(50),
            ReleaseTime: TimeSpan.FromMilliseconds(300),
            Threshold: 0.02,
            Ratio: 20,
            MusicBaseVolume: 0.15
        ),
        [DuckingProfileType.Balanced] = new DuckingProfile(
            Type: DuckingProfileType.Balanced,
            DuckDepthDb: -12,
            AttackTime: TimeSpan.FromMilliseconds(100),
            ReleaseTime: TimeSpan.FromMilliseconds(500),
            Threshold: 0.03,
            Ratio: 10,
            MusicBaseVolume: 0.25
        ),
        [DuckingProfileType.Gentle] = new DuckingProfile(
            Type: DuckingProfileType.Gentle,
            DuckDepthDb: -6,
            AttackTime: TimeSpan.FromMilliseconds(200),
            ReleaseTime: TimeSpan.FromMilliseconds(800),
            Threshold: 0.05,
            Ratio: 4,
            MusicBaseVolume: 0.35
        ),
        [DuckingProfileType.Dynamic] = new DuckingProfile(
            Type: DuckingProfileType.Dynamic,
            DuckDepthDb: -15,
            AttackTime: TimeSpan.FromMilliseconds(80),
            ReleaseTime: TimeSpan.FromMilliseconds(400),
            Threshold: 0.025,
            Ratio: 15,
            MusicBaseVolume: 0.20
        )
    };

    public IntelligentDuckingService(
        ILogger<IntelligentDuckingService> logger,
        IFFmpegService ffmpegService)
    {
        _logger = logger;
        _ffmpegService = ffmpegService;
    }

    /// <inheritdoc />
    public async Task<NarrationAnalysis> AnalyzeNarrationAsync(
        string narrationPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing narration for ducking: {Path}", narrationPath);

        if (!File.Exists(narrationPath))
            throw new FileNotFoundException("Narration file not found", narrationPath);

        // Get audio duration and detect silence segments using FFmpeg
        var duration = await GetAudioDurationAsync(narrationPath, ct).ConfigureAwait(false);
        var silenceSegments = await DetectSilenceAsync(narrationPath, ct).ConfigureAwait(false);
        var loudnessInfo = await AnalyzeLoudnessAsync(narrationPath, ct).ConfigureAwait(false);

        // Derive speech segments from silence segments
        var speechSegments = DeriveSpeechSegments(silenceSegments, duration, loudnessInfo.averageLoudness);

        var speechDuration = speechSegments.Sum(s => s.Duration.TotalSeconds);
        var silenceDuration = silenceSegments.Sum(s => s.Duration.TotalSeconds);
        var ratio = silenceDuration > 0 ? speechDuration / silenceDuration : speechDuration;

        _logger.LogInformation(
            "Narration analysis complete. Duration: {Duration}, Silence segments: {Count}, Speech/Silence ratio: {Ratio:F2}",
            duration, silenceSegments.Count, ratio);

        return new NarrationAnalysis(
            TotalDuration: duration,
            SilenceSegments: silenceSegments,
            SpeechSegments: speechSegments,
            AverageLoudness: loudnessInfo.averageLoudness,
            NoiseFloor: loudnessInfo.noiseFloor,
            HasClipping: loudnessInfo.hasClipping,
            SpeechToSilenceRatio: ratio
        );
    }

    /// <inheritdoc />
    public DuckingPlan PlanDucking(NarrationAnalysis analysis, string? contentType = null)
    {
        _logger.LogInformation("Planning ducking for content type: {ContentType}", contentType ?? "default");

        // Select profile based on content type and analysis
        var profileType = SelectProfileType(analysis, contentType);
        var profile = GetDefaultProfile(profileType);

        // Adjust profile based on analysis
        profile = AdjustProfileForAnalysis(profile, analysis);

        // Build segments
        var segments = BuildDuckingSegments(analysis, profile);

        // Build FFmpeg filter
        var filter = BuildSidechainFilter(profile);

        var reasoning = $"Selected {profileType} profile based on content type '{contentType ?? "default"}' " +
            $"with speech/silence ratio of {analysis.SpeechToSilenceRatio:F2}. " +
            $"Music will be reduced by {Math.Abs(profile.DuckDepthDb)}dB during speech.";

        return new DuckingPlan(
            Profile: profile,
            Segments: segments,
            FFmpegFilter: filter,
            Reasoning: reasoning
        );
    }

    /// <inheritdoc />
    public async Task<string> ApplyDuckingAsync(
        string narrationPath,
        string musicPath,
        DuckingPlan plan,
        string outputPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Applying {Profile} ducking to mix narration and music",
            plan.Profile.Type);

        if (!File.Exists(narrationPath))
            throw new FileNotFoundException("Narration file not found", narrationPath);
        if (!File.Exists(musicPath))
            throw new FileNotFoundException("Music file not found", musicPath);

        // Build the filter complex for sidechain compression
        var musicVol = plan.Profile.MusicBaseVolume;
        var filterComplex = $"[1:a]volume={musicVol}[music];" +
            $"[music][0:a]{plan.FFmpegFilter}[ducked];" +
            $"[ducked][0:a]amix=inputs=2:duration=first:dropout_transition=2[final]";

        var arguments = $"-i \"{narrationPath}\" -i \"{musicPath}\" " +
            $"-filter_complex \"{filterComplex}\" " +
            $"-map \"[final]\" -ac 2 -ar 48000 -y \"{outputPath}\"";

        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Ducking failed: {result.ErrorMessage}");
        }

        _logger.LogInformation("Ducking applied successfully: {Output}", outputPath);
        return outputPath;
    }

    /// <inheritdoc />
    public DuckingProfile GetDefaultProfile(DuckingProfileType profileType)
    {
        return DefaultProfiles.TryGetValue(profileType, out var profile)
            ? profile
            : DefaultProfiles[DuckingProfileType.Balanced];
    }

    private async Task<TimeSpan> GetAudioDurationAsync(string path, CancellationToken ct)
    {
        var arguments = $"-i \"{path}\" -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        // Parse duration from stderr output
        var match = Regex.Match(result.StandardError ?? "", @"Duration:\s*(\d+):(\d+):(\d+\.\d+)");
        if (match.Success)
        {
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = double.Parse(match.Groups[3].Value);
            return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.FromMinutes(1); // Fallback
    }

    private async Task<List<SilenceSegment>> DetectSilenceAsync(string path, CancellationToken ct)
    {
        var segments = new List<SilenceSegment>();

        // Use silencedetect filter
        var arguments = $"-i \"{path}\" -af silencedetect=noise=-30dB:d=0.5 -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var output = result.StandardError ?? "";

        // Parse silence_start and silence_end pairs
        var startMatches = Regex.Matches(output, @"silence_start:\s*([\d.]+)");
        var endMatches = Regex.Matches(output, @"silence_end:\s*([\d.]+)");

        for (int i = 0; i < Math.Min(startMatches.Count, endMatches.Count); i++)
        {
            var start = double.Parse(startMatches[i].Groups[1].Value);
            var end = double.Parse(endMatches[i].Groups[1].Value);

            segments.Add(new SilenceSegment(
                Start: TimeSpan.FromSeconds(start),
                End: TimeSpan.FromSeconds(end),
                NoiseLevel: -30 // Default noise threshold used
            ));
        }

        return segments;
    }

    private async Task<(double averageLoudness, double noiseFloor, bool hasClipping)> AnalyzeLoudnessAsync(
        string path, CancellationToken ct)
    {
        var arguments = $"-i \"{path}\" -af loudnorm=print_format=summary -f null -";
        var result = await _ffmpegService.ExecuteAsync(arguments, null, ct).ConfigureAwait(false);

        var output = result.StandardError ?? "";

        double avgLoudness = -23;
        double noiseFloor = -60;
        bool hasClipping = false;

        // Parse Input Integrated loudness
        var iMatch = Regex.Match(output, @"Input Integrated:\s*([-\d.]+)\s*LUFS");
        if (iMatch.Success)
            avgLoudness = double.Parse(iMatch.Groups[1].Value);

        // Parse True Peak
        var tpMatch = Regex.Match(output, @"Input True Peak:\s*([-\d.]+)\s*dBTP");
        if (tpMatch.Success)
        {
            var truePeak = double.Parse(tpMatch.Groups[1].Value);
            hasClipping = truePeak > -0.5;
        }

        // Estimate noise floor from threshold if available
        var threshMatch = Regex.Match(output, @"Input Threshold:\s*([-\d.]+)\s*LUFS");
        if (threshMatch.Success)
            noiseFloor = double.Parse(threshMatch.Groups[1].Value);

        return (avgLoudness, noiseFloor, hasClipping);
    }

    private List<SpeechSegment> DeriveSpeechSegments(
        List<SilenceSegment> silenceSegments,
        TimeSpan totalDuration,
        double averageLoudness)
    {
        var speechSegments = new List<SpeechSegment>();
        var currentStart = TimeSpan.Zero;

        foreach (var silence in silenceSegments.OrderBy(s => s.Start))
        {
            if (silence.Start > currentStart)
            {
                speechSegments.Add(new SpeechSegment(
                    Start: currentStart,
                    End: silence.Start,
                    AverageLoudness: averageLoudness,
                    PeakLoudness: averageLoudness + 6
                ));
            }
            currentStart = silence.End;
        }

        // Add final speech segment if there's audio after last silence
        if (currentStart < totalDuration)
        {
            speechSegments.Add(new SpeechSegment(
                Start: currentStart,
                End: totalDuration,
                AverageLoudness: averageLoudness,
                PeakLoudness: averageLoudness + 6
            ));
        }

        return speechSegments;
    }

    private DuckingProfileType SelectProfileType(NarrationAnalysis analysis, string? contentType)
    {
        var ct = contentType?.ToLowerInvariant() ?? "";

        // Content-type based selection
        if (ct.Contains("educational") || ct.Contains("tutorial") || ct.Contains("training"))
            return DuckingProfileType.Aggressive;

        if (ct.Contains("ambient") || ct.Contains("relaxation") || ct.Contains("meditation"))
            return DuckingProfileType.Gentle;

        if (ct.Contains("documentary") || ct.Contains("narrative"))
            return DuckingProfileType.Dynamic;

        // Analysis-based fallback
        if (analysis.SpeechToSilenceRatio > 3)
            return DuckingProfileType.Balanced;

        if (analysis.SpeechToSilenceRatio < 1)
            return DuckingProfileType.Gentle;

        return DuckingProfileType.Balanced;
    }

    private DuckingProfile AdjustProfileForAnalysis(DuckingProfile profile, NarrationAnalysis analysis)
    {
        // Adjust based on loudness
        var duckDepthAdjustment = 0.0;

        if (analysis.AverageLoudness < -20)
        {
            // Quiet narration - less aggressive ducking needed
            duckDepthAdjustment = 3;
        }
        else if (analysis.AverageLoudness > -14)
        {
            // Loud narration - more ducking may help
            duckDepthAdjustment = -2;
        }

        // Adjust based on clipping
        if (analysis.HasClipping)
        {
            // If narration is clipping, reduce music more
            duckDepthAdjustment -= 3;
        }

        return profile with
        {
            DuckDepthDb = profile.DuckDepthDb + duckDepthAdjustment
        };
    }

    private List<DuckingSegment> BuildDuckingSegments(NarrationAnalysis analysis, DuckingProfile profile)
    {
        var segments = new List<DuckingSegment>();

        foreach (var speech in analysis.SpeechSegments)
        {
            segments.Add(new DuckingSegment(
                Start: speech.Start,
                End: speech.End,
                MusicVolume: profile.MusicBaseVolume,
                Reason: "Speech detected - music ducked"
            ));
        }

        foreach (var silence in analysis.SilenceSegments)
        {
            // Only add if silence is long enough for music to rise
            if (silence.Duration.TotalSeconds >= 0.5)
            {
                segments.Add(new DuckingSegment(
                    Start: silence.Start,
                    End: silence.End,
                    MusicVolume: 1.0, // Full volume during silence
                    Reason: "Silence detected - music at full volume"
                ));
            }
        }

        return segments.OrderBy(s => s.Start).ToList();
    }

    private string BuildSidechainFilter(DuckingProfile profile)
    {
        var attackMs = profile.AttackTime.TotalMilliseconds;
        var releaseMs = profile.ReleaseTime.TotalMilliseconds;

        // Calculate level_sc for sidechain compression
        // This determines how much the music is reduced when narration is present
        var levelSc = Math.Pow(10, profile.DuckDepthDb / 20.0);

        return $"sidechaincompress=threshold={profile.Threshold}:" +
            $"ratio={profile.Ratio}:" +
            $"attack={attackMs}:" +
            $"release={releaseMs}:" +
            $"makeup=1:" +
            $"knee=2.828427:" +
            $"link=average:" +
            $"detection=rms:" +
            $"level_sc={levelSc:F4}";
    }
}
