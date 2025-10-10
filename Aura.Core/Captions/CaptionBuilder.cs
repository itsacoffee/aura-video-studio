using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Captions;

/// <summary>
/// Builds caption/subtitle files (SRT and VTT) from script lines with accurate timecodes.
/// Supports styling options for burn-in rendering.
/// </summary>
public class CaptionBuilder
{
    private readonly ILogger<CaptionBuilder> _logger;

    public CaptionBuilder(ILogger<CaptionBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates SRT subtitle file content from script lines.
    /// SRT format: index, timecode (HH:MM:SS,mmm --> HH:MM:SS,mmm), text, blank line.
    /// </summary>
    public string GenerateSrt(IEnumerable<ScriptLine> lines)
    {
        var linesList = lines.ToList();
        _logger.LogInformation("Generating SRT captions for {Count} lines", linesList.Count);

        var srt = new StringBuilder();
        int index = 1;

        foreach (var line in linesList)
        {
            var start = line.Start;
            var end = line.Start + line.Duration;

            // SRT format: HH:MM:SS,mmm (note the comma separator)
            string startTime = FormatSrtTimecode(start);
            string endTime = FormatSrtTimecode(end);

            srt.AppendLine(index.ToString());
            srt.AppendLine($"{startTime} --> {endTime}");
            srt.AppendLine(line.Text);
            srt.AppendLine();

            index++;
        }

        _logger.LogDebug("Generated SRT with {Count} entries", linesList.Count);
        return srt.ToString();
    }

    /// <summary>
    /// Generates VTT subtitle file content from script lines.
    /// VTT format: WEBVTT header, timecode (HH:MM:SS.mmm --> HH:MM:SS.mmm), text, blank line.
    /// </summary>
    public string GenerateVtt(IEnumerable<ScriptLine> lines)
    {
        var linesList = lines.ToList();
        _logger.LogInformation("Generating VTT captions for {Count} lines", linesList.Count);

        var vtt = new StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();

        foreach (var line in linesList)
        {
            var start = line.Start;
            var end = line.Start + line.Duration;

            // VTT format: HH:MM:SS.mmm (note the dot separator)
            string startTime = FormatVttTimecode(start);
            string endTime = FormatVttTimecode(end);

            vtt.AppendLine($"{startTime} --> {endTime}");
            vtt.AppendLine(line.Text);
            vtt.AppendLine();
        }

        _logger.LogDebug("Generated VTT with {Count} entries", linesList.Count);
        return vtt.ToString();
    }

    /// <summary>
    /// Builds FFmpeg subtitle filter for burn-in with styling options.
    /// </summary>
    public string BuildBurnInFilter(
        string subtitlePath,
        CaptionRenderStyle style)
    {
        _logger.LogDebug("Building burn-in filter for {Path} with style: {Style}", 
            subtitlePath, style.FontName);

        // Escape path for FFmpeg
        string escapedPath = subtitlePath
            .Replace("\\", "\\\\")
            .Replace(":", "\\:")
            .Replace("'", "\\'");

        // Build ASS style string for subtitles filter
        string forceStyle = $"FontName={style.FontName}," +
                           $"FontSize={style.FontSize}," +
                           $"PrimaryColour=&H{style.PrimaryColor}&," +
                           $"OutlineColour=&H{style.OutlineColor}&," +
                           $"Outline={style.OutlineWidth}," +
                           $"BorderStyle={style.BorderStyle}," +
                           $"Alignment={style.Alignment}";

        string filter = $"subtitles='{escapedPath}':force_style='{forceStyle}'";
        _logger.LogDebug("Burn-in filter: {Filter}", filter);

        return filter;
    }

    /// <summary>
    /// Validates caption timecodes align with script line durations.
    /// </summary>
    public bool ValidateTimecodes(
        IEnumerable<ScriptLine> lines,
        out string? validationMessage)
    {
        var linesList = lines.ToList();
        _logger.LogDebug("Validating timecodes for {Count} lines", linesList.Count);

        for (int i = 0; i < linesList.Count - 1; i++)
        {
            var current = linesList[i];
            var next = linesList[i + 1];

            var currentEnd = current.Start + current.Duration;

            // Check for overlapping timecodes
            if (currentEnd > next.Start)
            {
                validationMessage = $"Line {i} ends at {currentEnd} but line {i + 1} starts at {next.Start} (overlap detected).";
                _logger.LogWarning(validationMessage);
                return false;
            }

            // Check for negative durations
            if (current.Duration <= TimeSpan.Zero)
            {
                validationMessage = $"Line {i} has non-positive duration: {current.Duration}.";
                _logger.LogWarning(validationMessage);
                return false;
            }
        }

        // Check last line
        if (linesList.Count > 0)
        {
            var last = linesList[linesList.Count - 1];
            if (last.Duration <= TimeSpan.Zero)
            {
                validationMessage = $"Last line has non-positive duration: {last.Duration}.";
                _logger.LogWarning(validationMessage);
                return false;
            }
        }

        validationMessage = null;
        _logger.LogInformation("Timecode validation passed for {Count} lines", linesList.Count);
        return true;
    }

    /// <summary>
    /// Formats TimeSpan to SRT timecode format (HH:MM:SS,mmm).
    /// </summary>
    private string FormatSrtTimecode(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
    }

    /// <summary>
    /// Formats TimeSpan to VTT timecode format (HH:MM:SS.mmm).
    /// </summary>
    private string FormatVttTimecode(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }
}

/// <summary>
/// Caption rendering style options for burn-in with FFmpeg.
/// </summary>
public record CaptionRenderStyle(
    string FontName = "Arial",
    int FontSize = 24,
    string PrimaryColor = "FFFFFF",
    string OutlineColor = "000000",
    int OutlineWidth = 2,
    int BorderStyle = 3,  // 3 = opaque box
    int Alignment = 2);   // 2 = bottom center
