using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for detecting beats in music tracks for synchronization
/// </summary>
public class BeatDetectionService
{
    private readonly ILogger<BeatDetectionService> _logger;

    public BeatDetectionService(ILogger<BeatDetectionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects beats in a music file
    /// </summary>
    public async Task<List<BeatMarker>> DetectBeatsAsync(
        string filePath,
        int minBPM = 60,
        int maxBPM = 200,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Detecting beats in file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Audio file not found: {filePath}");
        }

        try
        {
            // In production, this would use a real beat detection library like aubio
            // For now, simulate beat detection based on typical BPM patterns
            var beats = await SimulateBeatDetectionAsync(filePath, minBPM, maxBPM, ct);

            _logger.LogInformation("Detected {Count} beats in file", beats.Count);
            return beats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting beats");
            throw;
        }
    }

    /// <summary>
    /// Calculates BPM from beat markers
    /// </summary>
    public int CalculateBPM(List<BeatMarker> beats)
    {
        if (beats.Count < 2)
        {
            return 120; // Default BPM
        }

        // Calculate average interval between beats
        var intervals = new List<double>();
        for (int i = 1; i < beats.Count; i++)
        {
            intervals.Add(beats[i].Timestamp - beats[i - 1].Timestamp);
        }

        var avgInterval = intervals.Average();
        var bpm = 60.0 / avgInterval;

        return (int)Math.Round(bpm);
    }

    /// <summary>
    /// Aligns visual transitions with musical beats
    /// </summary>
    public List<(TimeSpan Beat, TimeSpan Transition, TimeSpan Offset)> AlignBeatsToTransitions(
        List<BeatMarker> beats,
        List<TimeSpan> visualTransitions,
        TimeSpan maxOffset = default)
    {
        if (maxOffset == default)
        {
            maxOffset = TimeSpan.FromMilliseconds(200); // Default 200ms tolerance
        }

        var alignments = new List<(TimeSpan, TimeSpan, TimeSpan)>();

        foreach (var transition in visualTransitions)
        {
            // Find closest beat
            var closestBeat = beats
                .Select(b => (
                    Beat: b,
                    Offset: Math.Abs(TimeSpan.FromSeconds(b.Timestamp).TotalMilliseconds - transition.TotalMilliseconds)
                ))
                .OrderBy(x => x.Offset)
                .FirstOrDefault();

            if (closestBeat.Beat != null && closestBeat.Offset <= maxOffset.TotalMilliseconds)
            {
                var beatTime = TimeSpan.FromSeconds(closestBeat.Beat.Timestamp);
                var offset = transition - beatTime;
                alignments.Add((beatTime, transition, offset));
            }
        }

        return alignments;
    }

    /// <summary>
    /// Identifies musical phrases from beat patterns
    /// </summary>
    public List<(int PhraseNumber, TimeSpan Start, TimeSpan End)> IdentifyMusicalPhrases(
        List<BeatMarker> beats,
        int beatsPerBar = 4)
    {
        var phrases = new List<(int, TimeSpan, TimeSpan)>();
        
        var downbeats = beats.Where(b => b.IsDownbeat).ToList();
        if (downbeats.Count < 2)
        {
            return phrases;
        }

        // Group into phrases (typically 8 or 16 bars)
        int barsPerPhrase = 8;
        int phraseNumber = 1;

        for (int i = 0; i < downbeats.Count - barsPerPhrase; i += barsPerPhrase)
        {
            var phraseStart = TimeSpan.FromSeconds(downbeats[i].Timestamp);
            var phraseEnd = i + barsPerPhrase < downbeats.Count 
                ? TimeSpan.FromSeconds(downbeats[i + barsPerPhrase].Timestamp)
                : TimeSpan.FromSeconds(beats.Last().Timestamp);

            phrases.Add((phraseNumber++, phraseStart, phraseEnd));
        }

        return phrases;
    }

    /// <summary>
    /// Finds climax moments in music (highest energy beats)
    /// </summary>
    public List<TimeSpan> FindClimaxMoments(List<BeatMarker> beats, int topN = 5)
    {
        return beats
            .OrderByDescending(b => b.Strength)
            .Take(topN)
            .Select(b => TimeSpan.FromSeconds(b.Timestamp))
            .OrderBy(t => t)
            .ToList();
    }

    /// <summary>
    /// Simulates beat detection (in production, would use real audio analysis)
    /// </summary>
    private async Task<List<BeatMarker>> SimulateBeatDetectionAsync(
        string filePath,
        int minBPM,
        int maxBPM,
        CancellationToken ct)
    {
        await Task.Delay(100, ct); // Simulate processing time

        // Generate mock beat data based on typical music structure
        var bpm = 120; // Simulate detected BPM
        var beatInterval = 60.0 / bpm;
        var duration = 180.0; // Simulate 3 minute track

        var beats = new List<BeatMarker>();
        var currentTime = 0.0;
        var beatCount = 0;
        var phraseCount = 0;

        while (currentTime < duration)
        {
            var isDownbeat = beatCount % 4 == 0;
            var strength = isDownbeat ? 0.9 : 0.6;

            // Increase strength at phrase boundaries
            if (beatCount % 16 == 0)
            {
                strength = 1.0;
                phraseCount++;
            }

            // Add some variation to make it realistic
            strength += (Random.Shared.NextDouble() - 0.5) * 0.2;
            strength = Math.Clamp(strength, 0.3, 1.0);

            beats.Add(new BeatMarker(
                Timestamp: currentTime,
                Strength: strength,
                IsDownbeat: isDownbeat,
                MusicalPhrase: phraseCount
            ));

            currentTime += beatInterval;
            beatCount++;
        }

        return beats;
    }

    /// <summary>
    /// Suggests optimal scene duration based on musical phrases
    /// </summary>
    public TimeSpan SuggestSceneDuration(
        List<BeatMarker> beats,
        TimeSpan desiredDuration,
        int beatsPerBar = 4)
    {
        var bpm = CalculateBPM(beats);
        var secondsPerBeat = 60.0 / bpm;
        var secondsPerBar = secondsPerBeat * beatsPerBar;

        // Round to nearest bar
        var desiredBars = Math.Round(desiredDuration.TotalSeconds / secondsPerBar);
        
        // Prefer phrase boundaries (8 or 16 bars)
        var preferredBars = 8;
        if (Math.Abs(desiredBars - 16) < Math.Abs(desiredBars - 8))
        {
            preferredBars = 16;
        }
        else if (desiredBars < 4)
        {
            preferredBars = 4;
        }

        return TimeSpan.FromSeconds(preferredBars * secondsPerBar);
    }
}
