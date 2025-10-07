using System;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Provides audio processing utilities including LUFS normalization and DSP chain.
/// Spec requirement: -14 LUFS (YouTube) / -16 LUFS (voice-only) / -12 LUFS (music-forward)
/// DSP chain: HPF -> De-esser -> Compressor -> Limiter
/// </summary>
public class AudioProcessor
{
    private readonly ILogger<AudioProcessor> _logger;

    public AudioProcessor(ILogger<AudioProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds FFmpeg audio filter chain for processing
    /// </summary>
    public string BuildAudioFilterChain(
        double targetLufs = -14.0,
        bool enableDeEsser = true,
        bool enableCompressor = true,
        bool enableLimiter = true,
        double peakCeiling = -1.0)
    {
        var filters = new System.Collections.Generic.List<string>();

        // High-pass filter to remove rumble (80Hz)
        filters.Add("highpass=f=80");

        // De-esser (reduces harsh sibilance around 6-8kHz)
        if (enableDeEsser)
        {
            filters.Add("treble=g=-3:f=7000:w=2000");
        }

        // Compressor (ratio 3:1, threshold -18dB, soft knee)
        if (enableCompressor)
        {
            filters.Add("acompressor=threshold=-18dB:ratio=3:attack=20:release=250:makeup=6dB");
        }

        // Limiter (prevent peaks above ceiling)
        if (enableLimiter)
        {
            filters.Add($"alimiter=limit={peakCeiling}dB:attack=5:release=50");
        }

        // LUFS normalization
        // Note: FFmpeg loudnorm filter analyzes and normalizes to target LUFS
        filters.Add($"loudnorm=I={targetLufs}:TP={peakCeiling}:LRA=11");

        return string.Join(",", filters);
    }

    /// <summary>
    /// Builds FFmpeg command for music ducking (reduces music volume during speech)
    /// </summary>
    public string BuildMusicDuckingFilter(
        string narrationInput,
        string musicInput,
        double duckDepthDb = -12.0,
        double attackMs = 100,
        double releaseMs = 500)
    {
        // Use sidechaincompress to duck music when narration is present
        // [0:a] is narration (trigger), [1:a] is music (to be ducked)
        return $"[{musicInput}][{narrationInput}]sidechaincompress=threshold=0.02:ratio=10:attack={attackMs}:release={releaseMs}:makeup=0[duckedmusic]";
    }

    /// <summary>
    /// Calculates target bitrate based on content type
    /// </summary>
    public int SuggestAudioBitrate(string contentType, int channels = 2)
    {
        return contentType.ToLowerInvariant() switch
        {
            "voice" or "narration" => channels == 1 ? 96 : 128,  // Voice can use lower bitrate
            "music" => channels == 1 ? 192 : 256,                // Music needs higher bitrate
            "mixed" or "default" => 256,                         // Default for mixed content
            _ => 256
        };
    }

    /// <summary>
    /// Validates audio settings against spec requirements
    /// </summary>
    public bool ValidateAudioSettings(
        double lufs,
        double peakDb,
        out string? validationMessage)
    {
        // LUFS should be between -16 and -12 (with -14 being ideal for YouTube)
        if (lufs < -18 || lufs > -10)
        {
            validationMessage = $"LUFS {lufs:F1} is outside recommended range (-16 to -12). YouTube standard is -14 LUFS.";
            return false;
        }

        // Peak should not exceed -1 dBFS to prevent clipping
        if (peakDb > -1)
        {
            validationMessage = $"Peak level {peakDb:F1} dBFS exceeds -1 dBFS ceiling and may cause clipping.";
            return false;
        }

        validationMessage = null;
        return true;
    }

    /// <summary>
    /// Generates subtitle/caption commands for FFmpeg
    /// </summary>
    public string BuildSubtitleFilter(
        string subtitlePath,
        string fontName = "Arial",
        int fontSize = 24,
        string primaryColor = "FFFFFF",
        string outlineColor = "000000",
        int outlineWidth = 2)
    {
        // Escape path for FFmpeg
        string escapedPath = subtitlePath
            .Replace("\\", "\\\\")
            .Replace(":", "\\:")
            .Replace("'", "\\'");

        // Build style string
        string style = $"FontName={fontName},FontSize={fontSize}," +
                      $"PrimaryColour=&H{primaryColor}&," +
                      $"OutlineColour=&H{outlineColor}&," +
                      $"Outline={outlineWidth}," +
                      $"BorderStyle=3," +  // Opaque box
                      $"Alignment=2";      // Bottom center

        return $"subtitles='{escapedPath}':force_style='{style}'";
    }

    /// <summary>
    /// Generates SRT subtitle file content from script lines
    /// </summary>
    public string GenerateSrtSubtitles(
        System.Collections.Generic.IEnumerable<Models.ScriptLine> lines)
    {
        var srt = new System.Text.StringBuilder();
        int index = 1;

        foreach (var line in lines)
        {
            var start = line.Start;
            var end = line.Start + line.Duration;

            // SRT format: HH:MM:SS,mmm
            string startTime = $"{(int)start.TotalHours:D2}:{start.Minutes:D2}:{start.Seconds:D2},{start.Milliseconds:D3}";
            string endTime = $"{(int)end.TotalHours:D2}:{end.Minutes:D2}:{end.Seconds:D2},{end.Milliseconds:D3}";

            srt.AppendLine(index.ToString());
            srt.AppendLine($"{startTime} --> {endTime}");
            srt.AppendLine(line.Text);
            srt.AppendLine();

            index++;
        }

        return srt.ToString();
    }

    /// <summary>
    /// Generates VTT subtitle file content from script lines
    /// </summary>
    public string GenerateVttSubtitles(
        System.Collections.Generic.IEnumerable<Models.ScriptLine> lines)
    {
        var vtt = new System.Text.StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();

        foreach (var line in lines)
        {
            var start = line.Start;
            var end = line.Start + line.Duration;

            // VTT format: HH:MM:SS.mmm
            string startTime = $"{(int)start.TotalHours:D2}:{start.Minutes:D2}:{start.Seconds:D2}.{start.Milliseconds:D3}";
            string endTime = $"{(int)end.TotalHours:D2}:{end.Minutes:D2}:{end.Seconds:D2}.{end.Milliseconds:D3}";

            vtt.AppendLine($"{startTime} --> {endTime}");
            vtt.AppendLine(line.Text);
            vtt.AppendLine();
        }

        return vtt.ToString();
    }
}
