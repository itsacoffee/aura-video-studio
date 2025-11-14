using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Video;

/// <summary>
/// Subtitle entry with timing and text
/// </summary>
public record SubtitleEntry
{
    public int Index { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? SpeakerName { get; init; }
}

/// <summary>
/// Subtitle format types
/// </summary>
public enum SubtitleFormat
{
    SRT, // SubRip
    VTT, // WebVTT
    ASS  // Advanced SubStation Alpha
}

/// <summary>
/// Service for generating subtitle files in various formats
/// </summary>
public interface ISubtitleGenerator
{
    /// <summary>
    /// Generate subtitle file from entries
    /// </summary>
    Task<string> GenerateAsync(
        IEnumerable<SubtitleEntry> entries,
        string outputPath,
        SubtitleFormat format = SubtitleFormat.SRT,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate subtitle entries from script lines with timing
    /// </summary>
    Task<List<SubtitleEntry>> GenerateEntriesAsync(
        IEnumerable<(string text, TimeSpan startTime, TimeSpan duration)> scriptLines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse existing subtitle file
    /// </summary>
    Task<List<SubtitleEntry>> ParseAsync(
        string subtitlePath,
        SubtitleFormat format = SubtitleFormat.SRT,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert subtitle file from one format to another
    /// </summary>
    Task<string> ConvertAsync(
        string inputPath,
        string outputPath,
        SubtitleFormat inputFormat,
        SubtitleFormat outputFormat,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of subtitle generator
/// </summary>
public class SubtitleGenerator : ISubtitleGenerator
{
    private readonly ILogger<SubtitleGenerator> _logger;

    public SubtitleGenerator(ILogger<SubtitleGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateAsync(
        IEnumerable<SubtitleEntry> entries,
        string outputPath,
        SubtitleFormat format = SubtitleFormat.SRT,
        CancellationToken cancellationToken = default)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0)
        {
            throw new ArgumentException("At least one subtitle entry must be provided", nameof(entries));
        }

        _logger.LogInformation(
            "Generating {Format} subtitle file with {Count} entries: {Output}",
            format,
            entryList.Count,
            outputPath
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var content = format switch
        {
            SubtitleFormat.SRT => GenerateSRT(entryList),
            SubtitleFormat.VTT => GenerateVTT(entryList),
            SubtitleFormat.ASS => GenerateASS(entryList),
            _ => throw new NotSupportedException($"Subtitle format {format} is not supported")
        };

        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Subtitle file generated: {Output}", outputPath);
        return outputPath;
    }

    public async Task<List<SubtitleEntry>> GenerateEntriesAsync(
        IEnumerable<(string text, TimeSpan startTime, TimeSpan duration)> scriptLines,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<SubtitleEntry>();
        int index = 1;

        foreach (var (text, startTime, duration) in scriptLines)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            // Split long text into multiple lines if needed (max 42 chars per line for readability)
            var lines = SplitTextIntoLines(text, maxCharsPerLine: 42);
            var formattedText = string.Join("\n", lines);

            entries.Add(new SubtitleEntry
            {
                Index = index++,
                StartTime = startTime,
                EndTime = startTime + duration,
                Text = formattedText
            });
        }

        await Task.CompletedTask.ConfigureAwait(false);
        _logger.LogInformation("Generated {Count} subtitle entries from script", entries.Count);
        return entries;
    }

    public async Task<List<SubtitleEntry>> ParseAsync(
        string subtitlePath,
        SubtitleFormat format = SubtitleFormat.SRT,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(subtitlePath))
        {
            throw new FileNotFoundException("Subtitle file not found", subtitlePath);
        }

        _logger.LogInformation("Parsing {Format} subtitle file: {Path}", format, subtitlePath);

        var content = await File.ReadAllTextAsync(subtitlePath, cancellationToken)
            .ConfigureAwait(false);

        var entries = format switch
        {
            SubtitleFormat.SRT => ParseSRT(content),
            SubtitleFormat.VTT => ParseVTT(content),
            SubtitleFormat.ASS => ParseASS(content),
            _ => throw new NotSupportedException($"Subtitle format {format} is not supported")
        };

        _logger.LogInformation("Parsed {Count} subtitle entries", entries.Count);
        return entries;
    }

    public async Task<string> ConvertAsync(
        string inputPath,
        string outputPath,
        SubtitleFormat inputFormat,
        SubtitleFormat outputFormat,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Converting subtitle from {InputFormat} to {OutputFormat}",
            inputFormat,
            outputFormat
        );

        var entries = await ParseAsync(inputPath, inputFormat, cancellationToken)
            .ConfigureAwait(false);

        return await GenerateAsync(entries, outputPath, outputFormat, cancellationToken)
            .ConfigureAwait(false);
    }

    #region SRT Format

    private string GenerateSRT(List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();

        foreach (var entry in entries)
        {
            sb.AppendLine(entry.Index.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine($"{FormatSRTTime(entry.StartTime)} --> {FormatSRTTime(entry.EndTime)}");
            sb.AppendLine(entry.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatSRTTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
    }

    private List<SubtitleEntry> ParseSRT(string content)
    {
        var entries = new List<SubtitleEntry>();
        var blocks = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3)
            {
                continue;
            }

            if (!int.TryParse(lines[0], out int index))
            {
                continue;
            }

            var timeLine = lines[1];
            var timeParts = timeLine.Split(new[] { " --> " }, StringSplitOptions.None);
            if (timeParts.Length != 2)
            {
                continue;
            }

            var startTime = ParseSRTTime(timeParts[0]);
            var endTime = ParseSRTTime(timeParts[1]);
            var text = string.Join("\n", lines.Skip(2));

            entries.Add(new SubtitleEntry
            {
                Index = index,
                StartTime = startTime,
                EndTime = endTime,
                Text = text
            });
        }

        return entries;
    }

    private TimeSpan ParseSRTTime(string timeString)
    {
        // Format: HH:MM:SS,mmm
        var parts = timeString.Split(':');
        if (parts.Length != 3)
        {
            return TimeSpan.Zero;
        }

        var secondsParts = parts[2].Split(',');
        if (secondsParts.Length != 2)
        {
            return TimeSpan.Zero;
        }

        int hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
        int minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int seconds = int.Parse(secondsParts[0], CultureInfo.InvariantCulture);
        int milliseconds = int.Parse(secondsParts[1], CultureInfo.InvariantCulture);

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    #endregion

    #region VTT Format

    private string GenerateVTT(List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.AppendLine($"{FormatVTTTime(entry.StartTime)} --> {FormatVTTTime(entry.EndTime)}");
            sb.AppendLine(entry.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatVTTTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }

    private List<SubtitleEntry> ParseVTT(string content)
    {
        var entries = new List<SubtitleEntry>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // Skip "WEBVTT" header
            .ToList();

        int index = 1;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Contains("-->"))
            {
                var timeParts = line.Split(new[] { " --> " }, StringSplitOptions.None);
                if (timeParts.Length != 2)
                {
                    continue;
                }

                var startTime = ParseVTTTime(timeParts[0]);
                var endTime = ParseVTTTime(timeParts[1]);

                // Collect text lines until empty line
                var textLines = new List<string>();
                for (int j = i + 1; j < lines.Count && !string.IsNullOrWhiteSpace(lines[j]) && !lines[j].Contains("-->"); j++)
                {
                    textLines.Add(lines[j]);
                    i = j;
                }

                entries.Add(new SubtitleEntry
                {
                    Index = index++,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = string.Join("\n", textLines)
                });
            }
        }

        return entries;
    }

    private TimeSpan ParseVTTTime(string timeString)
    {
        // Format: HH:MM:SS.mmm
        var parts = timeString.Split(':');
        if (parts.Length != 3)
        {
            return TimeSpan.Zero;
        }

        var secondsParts = parts[2].Split('.');
        if (secondsParts.Length != 2)
        {
            return TimeSpan.Zero;
        }

        int hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
        int minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int seconds = int.Parse(secondsParts[0], CultureInfo.InvariantCulture);
        int milliseconds = int.Parse(secondsParts[1], CultureInfo.InvariantCulture);

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    #endregion

    #region ASS Format

    private string GenerateASS(List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();
        
        // ASS header
        sb.AppendLine("[Script Info]");
        sb.AppendLine("Title: Generated by Aura");
        sb.AppendLine("ScriptType: v4.00+");
        sb.AppendLine();
        
        // Styles
        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        sb.AppendLine("Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H80000000,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1");
        sb.AppendLine();
        
        // Events
        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var entry in entries)
        {
            var speaker = !string.IsNullOrEmpty(entry.SpeakerName) ? entry.SpeakerName : "";
            var text = entry.Text.Replace("\n", "\\N"); // ASS uses \N for line breaks
            
            sb.AppendLine($"Dialogue: 0,{FormatASSTime(entry.StartTime)},{FormatASSTime(entry.EndTime)},Default,{speaker},0,0,0,,{text}");
        }

        return sb.ToString();
    }

    private string FormatASSTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
    }

    private List<SubtitleEntry> ParseASS(string content)
    {
        var entries = new List<SubtitleEntry>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int index = 1;
        bool inEvents = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("[Events]", StringComparison.OrdinalIgnoreCase))
            {
                inEvents = true;
                continue;
            }

            if (inEvents && line.StartsWith("Dialogue:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Substring(9).Split(',', 10); // Split into 10 parts max
                if (parts.Length < 10)
                {
                    continue;
                }

                var startTime = ParseASSTime(parts[1]);
                var endTime = ParseASSTime(parts[2]);
                var speaker = parts[4];
                var text = parts[9].Replace("\\N", "\n"); // Convert \N to actual line breaks

                entries.Add(new SubtitleEntry
                {
                    Index = index++,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = text,
                    SpeakerName = !string.IsNullOrWhiteSpace(speaker) ? speaker : null
                });
            }
        }

        return entries;
    }

    private TimeSpan ParseASSTime(string timeString)
    {
        // Format: H:MM:SS.cc (centiseconds)
        var parts = timeString.Split(':');
        if (parts.Length != 3)
        {
            return TimeSpan.Zero;
        }

        var secondsParts = parts[2].Split('.');
        if (secondsParts.Length != 2)
        {
            return TimeSpan.Zero;
        }

        int hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
        int minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int seconds = int.Parse(secondsParts[0], CultureInfo.InvariantCulture);
        int centiseconds = int.Parse(secondsParts[1], CultureInfo.InvariantCulture);

        return new TimeSpan(0, hours, minutes, seconds, centiseconds * 10);
    }

    #endregion

    #region Helper Methods

    private List<string> SplitTextIntoLines(string text, int maxCharsPerLine)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxCharsPerLine && currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }

    #endregion
}
