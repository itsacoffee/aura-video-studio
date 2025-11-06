using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class SSMLSubtitleSynchronizerTests
{
    private readonly SSMLSubtitleSynchronizer _synchronizer;

    public SSMLSubtitleSynchronizerTests()
    {
        _synchronizer = new SSMLSubtitleSynchronizer(
            NullLogger<SSMLSubtitleSynchronizer>.Instance);
    }

    [Fact]
    public void SynchronizeWithSSML_WithinTolerance_ReturnsValid()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2.0)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(3.0))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "Hello world", 2000, 2000, 0.5),
            CreateSegment(1, "This is a test", 3000, 3000, 0.5)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines, 0.02);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.SynchronizedLines.Count);
        Assert.True(result.OverallDeviation <= 0.02);
        Assert.Equal(2, result.WithinToleranceCount);
    }

    [Fact]
    public void SynchronizeWithSSML_ExceedsTolerance_ReturnsInvalid()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.Zero, TimeSpan.FromSeconds(2.0))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "Hello world", 2000, 2500, 25.0)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines, 0.02);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Equal(0, result.WithinToleranceCount);
    }

    [Fact]
    public void SynchronizeWithSSML_AdjustsStartTimes_Sequential()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "First line", TimeSpan.Zero, TimeSpan.FromSeconds(2.0)),
            new ScriptLine(1, "Second line", TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(3.0)),
            new ScriptLine(2, "Third line", TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(2.5))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "First line", 2000, 2000, 0.0),
            CreateSegment(1, "Second line", 3000, 3000, 0.0),
            CreateSegment(2, "Third line", 2500, 2500, 0.0)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines);

        Assert.Equal(3, result.SynchronizedLines.Count);
        
        Assert.Equal(TimeSpan.Zero, result.SynchronizedLines[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(2.0), result.SynchronizedLines[0].Duration);
        
        Assert.Equal(TimeSpan.FromSeconds(2.0), result.SynchronizedLines[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(3.0), result.SynchronizedLines[1].Duration);
        
        Assert.Equal(TimeSpan.FromSeconds(5.0), result.SynchronizedLines[2].Start);
        Assert.Equal(TimeSpan.FromSeconds(2.5), result.SynchronizedLines[2].Duration);
    }

    [Fact]
    public void SynchronizeWithSSML_CalculatesDeviationCorrectly()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10.0))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "Test", 10000, 10100, 1.0)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines, 0.02);

        Assert.Single(result.Adjustments);
        var adjustment = result.Adjustments[0];
        
        Assert.Equal(0.01, adjustment.DeviationFromTarget, 3);
        Assert.True(adjustment.WithinTolerance);
    }

    [Fact]
    public void SynchronizeWithSSML_StrictTolerance_DetectsSmallDeviations()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(2.0))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "Test", 2000, 2050, 2.5)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines, 0.02);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains("exceeds tolerance", result.Warnings[0]);
    }

    [Fact]
    public void SynchronizeWithSSML_CalculatesWithinTolerancePercent()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.Zero, TimeSpan.FromSeconds(2.0)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0)),
            new ScriptLine(2, "Line 3", TimeSpan.FromSeconds(4.0), TimeSpan.FromSeconds(2.0)),
            new ScriptLine(3, "Line 4", TimeSpan.FromSeconds(6.0), TimeSpan.FromSeconds(2.0))
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegment(0, "Line 1", 2000, 2010, 0.5),
            CreateSegment(1, "Line 2", 2000, 2020, 1.0),
            CreateSegment(2, "Line 3", 2000, 2200, 10.0),
            CreateSegment(3, "Line 4", 2000, 2015, 0.75)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines, 0.02);

        Assert.Equal(3, result.WithinToleranceCount);
        Assert.Equal(75.0, result.WithinTolerancePercent);
    }

    [Fact]
    public void ApplyTimingMarkers_SplitsLineByMarkers()
    {
        var line = new ScriptLine(0, "This is a long sentence with multiple words", TimeSpan.Zero, TimeSpan.FromSeconds(5.0));
        
        var markers = new List<TimingMarker>
        {
            new TimingMarker(2000, "marker1", null),
            new TimingMarker(3500, "marker2", null)
        };

        var segments = _synchronizer.ApplyTimingMarkers(line, markers);

        Assert.Equal(3, segments.Count);
        
        Assert.Equal(TimeSpan.Zero, segments[0].Start);
        Assert.True(segments[0].Duration > TimeSpan.Zero);
        
        Assert.True(segments[1].Start > TimeSpan.Zero);
        Assert.True(segments[1].Duration > TimeSpan.Zero);
        
        Assert.True(segments[2].Start > segments[1].Start);
    }

    [Fact]
    public void ApplyTimingMarkers_NoMarkers_ReturnsSingleLine()
    {
        var line = new ScriptLine(0, "Single line", TimeSpan.Zero, TimeSpan.FromSeconds(2.0));
        var markers = new List<TimingMarker>();

        var segments = _synchronizer.ApplyTimingMarkers(line, markers);

        Assert.Single(segments);
        Assert.Equal(line.Text, segments[0].Text);
        Assert.Equal(line.Duration, segments[0].Duration);
    }

    [Fact]
    public void SynchronizeWithSSML_IncludesTimingMarkers_InAdjustments()
    {
        var originalLines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test line with markers", TimeSpan.Zero, TimeSpan.FromSeconds(3.0))
        };

        var markers = new List<TimingMarker>
        {
            new TimingMarker(1000, "mid-point", null)
        };

        var ssmlSegments = new List<SSMLSegmentResult>
        {
            CreateSegmentWithMarkers(0, "Test line with markers", 3000, 3000, 0.0, markers)
        };

        var result = _synchronizer.SynchronizeWithSSML(ssmlSegments, originalLines);

        Assert.Single(result.Adjustments);
        Assert.NotEmpty(result.Adjustments[0].TimingMarkers);
        Assert.Contains(result.Adjustments[0].TimingMarkers, m => m.Name == "mid-point");
    }

    private SSMLSegmentResult CreateSegment(
        int sceneIndex,
        string text,
        int targetDurationMs,
        int estimatedDurationMs,
        double deviationPercent)
    {
        return new SSMLSegmentResult
        {
            SceneIndex = sceneIndex,
            OriginalText = text,
            SsmlMarkup = $"<speak>{text}</speak>",
            EstimatedDurationMs = estimatedDurationMs,
            TargetDurationMs = targetDurationMs,
            DeviationPercent = deviationPercent,
            Adjustments = new ProsodyAdjustments(),
            TimingMarkers = Array.Empty<TimingMarker>()
        };
    }

    private SSMLSegmentResult CreateSegmentWithMarkers(
        int sceneIndex,
        string text,
        int targetDurationMs,
        int estimatedDurationMs,
        double deviationPercent,
        IEnumerable<TimingMarker> markers)
    {
        return new SSMLSegmentResult
        {
            SceneIndex = sceneIndex,
            OriginalText = text,
            SsmlMarkup = $"<speak>{text}</speak>",
            EstimatedDurationMs = estimatedDurationMs,
            TargetDurationMs = targetDurationMs,
            DeviationPercent = deviationPercent,
            Adjustments = new ProsodyAdjustments(),
            TimingMarkers = markers.ToList()
        };
    }
}
