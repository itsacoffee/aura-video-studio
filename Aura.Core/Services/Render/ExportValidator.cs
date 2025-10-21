using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Validation result for an export
/// </summary>
public record ValidationResult(
    bool IsValid,
    List<string> Issues,
    VideoStreamInfo? VideoInfo = null,
    AudioStreamInfo? AudioInfo = null,
    TimeSpan? Duration = null);

/// <summary>
/// Video stream information
/// </summary>
public record VideoStreamInfo(
    string Codec,
    Resolution Resolution,
    double FrameRate,
    string PixelFormat);

/// <summary>
/// Audio stream information
/// </summary>
public record AudioStreamInfo(
    string Codec,
    int SampleRate,
    int Channels,
    int BitRate);

/// <summary>
/// Validates exported video files
/// </summary>
public class ExportValidator
{
    private readonly ILogger<ExportValidator> _logger;
    private readonly string _ffprobePath;

    public ExportValidator(ILogger<ExportValidator> logger, string ffprobePath = "ffprobe")
    {
        _logger = logger;
        _ffprobePath = ffprobePath;
    }

    /// <summary>
    /// Validates an exported file
    /// </summary>
    public async Task<ValidationResult> ValidateExportAsync(
        string filePath,
        ExportPreset expectedPreset,
        TimeSpan expectedDuration)
    {
        _logger.LogInformation("Validating export: {FilePath}", filePath);

        var issues = new List<string>();

        // Check file exists
        if (!File.Exists(filePath))
        {
            issues.Add("Output file does not exist");
            return new ValidationResult(IsValid: false, Issues: issues);
        }

        // Check file size
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length < 1024 * 1024) // Less than 1MB
        {
            issues.Add($"File size is suspiciously small: {fileInfo.Length / 1024.0:F2} KB");
        }

        _logger.LogDebug("File size: {Size} MB", fileInfo.Length / 1024.0 / 1024.0);

        // Probe video with FFprobe
        try
        {
            var probeResult = await ProbeVideoAsync(filePath);

            if (probeResult == null)
            {
                issues.Add("Failed to probe video file");
                return new ValidationResult(IsValid: false, Issues: issues);
            }

            // Validate video stream
            if (probeResult.VideoInfo == null)
            {
                issues.Add("No video stream found");
            }
            else
            {
                _logger.LogDebug(
                    "Video stream: {Codec} {Width}x{Height} @ {Fps}fps",
                    probeResult.VideoInfo.Codec,
                    probeResult.VideoInfo.Resolution.Width,
                    probeResult.VideoInfo.Resolution.Height,
                    probeResult.VideoInfo.FrameRate
                );

                // Check resolution
                if (probeResult.VideoInfo.Resolution.Width != expectedPreset.Resolution.Width ||
                    probeResult.VideoInfo.Resolution.Height != expectedPreset.Resolution.Height)
                {
                    issues.Add(
                        $"Resolution mismatch: expected {expectedPreset.Resolution.Width}x{expectedPreset.Resolution.Height}, " +
                        $"got {probeResult.VideoInfo.Resolution.Width}x{probeResult.VideoInfo.Resolution.Height}"
                    );
                }
            }

            // Validate audio stream
            if (probeResult.AudioInfo == null)
            {
                issues.Add("No audio stream found");
            }
            else
            {
                _logger.LogDebug(
                    "Audio stream: {Codec} {SampleRate}Hz {Channels}ch @ {BitRate}kbps",
                    probeResult.AudioInfo.Codec,
                    probeResult.AudioInfo.SampleRate,
                    probeResult.AudioInfo.Channels,
                    probeResult.AudioInfo.BitRate / 1000
                );
            }

            // Validate duration
            if (probeResult.Duration.HasValue)
            {
                var durationDiff = Math.Abs((probeResult.Duration.Value - expectedDuration).TotalSeconds);
                
                if (durationDiff > 1.0) // More than 1 second difference
                {
                    issues.Add(
                        $"Duration mismatch: expected {expectedDuration.TotalSeconds:F2}s, " +
                        $"got {probeResult.Duration.Value.TotalSeconds:F2}s"
                    );
                }

                _logger.LogDebug("Duration: {Duration}s", probeResult.Duration.Value.TotalSeconds);
            }

            // Try to decode first 10 frames to ensure file is playable
            var isPlayable = await VerifyPlayableAsync(filePath);
            if (!isPlayable)
            {
                issues.Add("File failed playback verification - may be corrupted");
            }

            var isValid = issues.Count == 0;

            _logger.LogInformation(
                "Validation {Result}: {IssueCount} issues found",
                isValid ? "passed" : "failed",
                issues.Count
            );

            return new ValidationResult(
                IsValid: isValid,
                Issues: issues,
                VideoInfo: probeResult.VideoInfo,
                AudioInfo: probeResult.AudioInfo,
                Duration: probeResult.Duration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating export");
            issues.Add($"Validation error: {ex.Message}");
            return new ValidationResult(IsValid: false, Issues: issues);
        }
    }

    /// <summary>
    /// Probes video file using FFprobe
    /// </summary>
    private async Task<ValidationResult?> ProbeVideoAsync(string filePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = $"-v quiet -print_format json -show_format -show_streams \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return null;
            }

            // Parse FFprobe output
            return ParseProbeOutput(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error probing video file");
            return null;
        }
    }

    /// <summary>
    /// Verifies file is playable by decoding first frames
    /// </summary>
    private async Task<bool> VerifyPlayableAsync(string filePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-v error -i \"{filePath}\" -frames:v 10 -f null -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses FFprobe JSON output
    /// </summary>
    private ValidationResult? ParseProbeOutput(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            VideoStreamInfo? videoInfo = null;
            AudioStreamInfo? audioInfo = null;
            TimeSpan? duration = null;

            // Parse streams
            if (root.TryGetProperty("streams", out var streams))
            {
                foreach (var stream in streams.EnumerateArray())
                {
                    if (!stream.TryGetProperty("codec_type", out var codecType))
                        continue;

                    var type = codecType.GetString();

                    if (type == "video" && videoInfo == null)
                    {
                        var codec = stream.TryGetProperty("codec_name", out var codecName) 
                            ? codecName.GetString() ?? "unknown" 
                            : "unknown";

                        var width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
                        var height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0;

                        var fps = 30.0;
                        if (stream.TryGetProperty("r_frame_rate", out var fpsStr))
                        {
                            var fpsMatch = Regex.Match(fpsStr.GetString() ?? "", @"(\d+)/(\d+)");
                            if (fpsMatch.Success)
                            {
                                var num = double.Parse(fpsMatch.Groups[1].Value);
                                var den = double.Parse(fpsMatch.Groups[2].Value);
                                fps = num / den;
                            }
                        }

                        var pixFmt = stream.TryGetProperty("pix_fmt", out var pf) 
                            ? pf.GetString() ?? "unknown" 
                            : "unknown";

                        videoInfo = new VideoStreamInfo(
                            Codec: codec,
                            Resolution: new Resolution(width, height),
                            FrameRate: fps,
                            PixelFormat: pixFmt
                        );
                    }
                    else if (type == "audio" && audioInfo == null)
                    {
                        var codec = stream.TryGetProperty("codec_name", out var codecName) 
                            ? codecName.GetString() ?? "unknown" 
                            : "unknown";

                        var sampleRate = stream.TryGetProperty("sample_rate", out var sr) 
                            ? int.Parse(sr.GetString() ?? "0") 
                            : 0;

                        var channels = stream.TryGetProperty("channels", out var ch) ? ch.GetInt32() : 0;

                        var bitRate = stream.TryGetProperty("bit_rate", out var br) 
                            ? int.Parse(br.GetString() ?? "0") 
                            : 0;

                        audioInfo = new AudioStreamInfo(
                            Codec: codec,
                            SampleRate: sampleRate,
                            Channels: channels,
                            BitRate: bitRate
                        );
                    }
                }
            }

            // Parse format duration
            if (root.TryGetProperty("format", out var format))
            {
                if (format.TryGetProperty("duration", out var durationStr))
                {
                    if (double.TryParse(durationStr.GetString(), out var durationSeconds))
                    {
                        duration = TimeSpan.FromSeconds(durationSeconds);
                    }
                }
            }

            return new ValidationResult(
                IsValid: true,
                Issues: new List<string>(),
                VideoInfo: videoInfo,
                AudioInfo: audioInfo,
                Duration: duration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FFprobe output");
            return null;
        }
    }
}
