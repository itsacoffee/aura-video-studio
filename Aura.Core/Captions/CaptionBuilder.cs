using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Builds FFmpeg subtitle filter for burn-in with styling options and RTL support.
    /// </summary>
    public string BuildBurnInFilter(
        string subtitlePath,
        CaptionRenderStyle style)
    {
        _logger.LogDebug("Building burn-in filter for {Path} with style: {Style}, RTL: {IsRTL}", 
            subtitlePath, style.FontName, style.IsRightToLeft);

        // Escape path for FFmpeg
        string escapedPath = subtitlePath
            .Replace("\\", "\\\\")
            .Replace(":", "\\:")
            .Replace("'", "\\'");

        // Build ASS style string for subtitles filter
        var styleElements = new List<string>
        {
            $"FontName={GetFontName(style)}",
            $"FontSize={style.FontSize}",
            $"PrimaryColour=&H{style.PrimaryColor}&",
            $"OutlineColour=&H{style.OutlineColor}&",
            $"Outline={style.OutlineWidth}",
            $"BorderStyle={style.BorderStyle}",
            $"Alignment={style.Alignment}"
        };

        string forceStyle = string.Join(",", styleElements);
        string filter = $"subtitles='{escapedPath}':force_style='{forceStyle}'";
        
        _logger.LogDebug("Burn-in filter: {Filter}", filter);

        return filter;
    }

    /// <summary>
    /// Gets appropriate font name with RTL fallback support.
    /// </summary>
    private string GetFontName(CaptionRenderStyle style)
    {
        if (style.IsRightToLeft && !string.IsNullOrEmpty(style.RtlFontFallback))
        {
            _logger.LogDebug("Using RTL font fallback: {Font}", style.RtlFontFallback);
            return style.RtlFontFallback;
        }

        return style.FontName;
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
    /// Export subtitles to file with appropriate format and RTL handling.
    /// </summary>
    public async Task<string> ExportSubtitlesToFileAsync(
        IEnumerable<ScriptLine> lines,
        SubtitleExportFormat format,
        string outputDirectory,
        string baseFileName,
        bool isRightToLeft = false)
    {
        var linesList = lines.ToList();
        _logger.LogInformation(
            "Exporting {Format} subtitles to {Directory}/{FileName}, RTL: {IsRTL}", 
            format, outputDirectory, baseFileName, isRightToLeft);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
            _logger.LogDebug("Created output directory: {Directory}", outputDirectory);
        }

        var content = format switch
        {
            SubtitleExportFormat.SRT => GenerateSrt(linesList),
            SubtitleExportFormat.VTT => GenerateVtt(linesList),
            _ => throw new ArgumentException($"Unsupported subtitle format: {format}")
        };

        var extension = format == SubtitleExportFormat.SRT ? "srt" : "vtt";
        var fileName = $"{baseFileName}.{extension}";
        var outputPath = Path.Combine(outputDirectory, fileName);

        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8);
        _logger.LogInformation("Subtitles exported to: {Path}", outputPath);

        return outputPath;
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
/// Subtitle export format
/// </summary>
public enum SubtitleExportFormat
{
    SRT,
    VTT
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
    int Alignment = 2,   // 2 = bottom center
    bool IsRightToLeft = false,
    string? RtlFontFallback = null);  // Font fallback for RTL languages like Arabic/Hebrew
