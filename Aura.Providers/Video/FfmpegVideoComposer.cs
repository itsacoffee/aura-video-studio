using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Video;

public class FfmpegVideoComposer : IVideoComposer
{
    private readonly ILogger<FfmpegVideoComposer> _logger;
    private readonly string _ffmpegPath;
    private readonly string _workingDirectory;
    private string _outputDirectory;

    public FfmpegVideoComposer(ILogger<FfmpegVideoComposer> logger, string ffmpegPath, string? outputDirectory = null)
    {
        _logger = logger;
        _ffmpegPath = ffmpegPath;
        _workingDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "Render");
        _outputDirectory = outputDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "AuraVideoStudio");
        
        // Ensure working directory exists
        if (!Directory.Exists(_workingDirectory))
        {
            Directory.CreateDirectory(_workingDirectory);
        }
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<string> RenderAsync(Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct)
    {
        _logger.LogInformation("Starting FFmpeg render at {Resolution}p", spec.Res.Height);
        
        // Create output file path using configured output directory
        string outputFilePath = Path.Combine(
            _outputDirectory,
            $"AuraVideoStudio_{DateTime.Now:yyyyMMddHHmmss}.{spec.Container}");
        
        // Build the FFmpeg command
        string ffmpegCommand = BuildFfmpegCommand(timeline, spec, outputFilePath);
        
        _logger.LogDebug("FFmpeg command: {Command}", ffmpegCommand);
        
        // Create process to run FFmpeg
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = ffmpegCommand,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };
        
        // Track progress
        var totalDuration = timeline.Scenes.Count > 0 
            ? timeline.Scenes[^1].Start + timeline.Scenes[^1].Duration 
            : TimeSpan.FromMinutes(1);
        
        var startTime = DateTime.Now;
        var lastReportTime = DateTime.Now;
        
        // Set up output handler to parse progress
        process.ErrorDataReceived += (sender, args) =>
        {
            if (string.IsNullOrEmpty(args.Data)) return;
            
            // Log the output
            _logger.LogTrace("FFmpeg: {Output}", args.Data);
            
            // Parse progress if it contains time information
            if (args.Data.Contains("time="))
            {
                // Parse the time value (format: time=00:00:12.34)
                var timeMatch = System.Text.RegularExpressions.Regex.Match(args.Data, @"time=(\d{2}:\d{2}:\d{2}\.\d{2})");
                if (timeMatch.Success)
                {
                    var timeStr = timeMatch.Groups[1].Value;
                    if (TimeSpan.TryParse(timeStr, out var currentTime))
                    {
                        var now = DateTime.Now;
                        
                        // Report progress at most once per second
                        if ((now - lastReportTime).TotalSeconds >= 1)
                        {
                            lastReportTime = now;
                            
                            // Calculate progress percentage
                            float percentage = (float)(currentTime.TotalSeconds / totalDuration.TotalSeconds * 100);
                            percentage = Math.Clamp(percentage, 0, 100);
                            
                            // Calculate time remaining
                            var elapsed = now - startTime;
                            var estimatedTotal = TimeSpan.FromSeconds(
                                elapsed.TotalSeconds / (percentage / 100));
                            var remaining = estimatedTotal - elapsed;
                            
                            // Report progress
                            progress.Report(new RenderProgress(
                                percentage,
                                elapsed,
                                remaining,
                                "Rendering video"));
                        }
                    }
                }
            }
        };
        
        // Set up task to wait for process completion
        var tcs = new TaskCompletionSource<bool>();
        
        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0)
            {
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetException(new Exception($"FFmpeg exited with code {process.ExitCode}"));
            }
        };
        
        // Start the process
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        
        // Register cancellation
        ct.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill FFmpeg process during cancellation");
            }
        });
        
        // Wait for completion or cancellation
        await tcs.Task;
        
        _logger.LogInformation("Render completed successfully: {OutputPath}", outputFilePath);
        
        // Report 100% completion
        progress.Report(new RenderProgress(
            100,
            DateTime.Now - startTime,
            TimeSpan.Zero,
            "Render complete"));
        
        return outputFilePath;
    }
    
    private string BuildFfmpegCommand(Timeline timeline, RenderSpec spec, string outputPath)
    {
        var args = new List<string>();
        
        // Input file(s)
        args.Add("-i"); // Narration
        args.Add($"\"{timeline.NarrationPath}\"");
        
        if (!string.IsNullOrEmpty(timeline.MusicPath))
        {
            args.Add("-i"); // Music
            args.Add($"\"{timeline.MusicPath}\"");
        }
        
        // Scene assets would be added here in a more complete implementation
        // For simplicity, we'll assume a basic slideshow with narration and music
        
        // Output file format settings
        args.Add("-c:v");
        args.Add("libx264"); // Default to h264 for compatibility
        
        args.Add("-preset");
        args.Add("medium"); // Balance quality and speed
        
        args.Add("-crf");
        args.Add("23"); // Good quality
        
        // Resolution
        args.Add("-s");
        args.Add($"{spec.Res.Width}x{spec.Res.Height}");
        
        // Audio settings
        args.Add("-c:a");
        args.Add("aac");
        
        args.Add("-b:a");
        args.Add($"{spec.AudioBitrateK}k");
        
        // Video bitrate
        args.Add("-b:v");
        args.Add($"{spec.VideoBitrateK}k");
        
        // Framerate
        args.Add("-r");
        args.Add("30");
        
        // Pixel format
        args.Add("-pix_fmt");
        args.Add("yuv420p");
        
        // Overwrite output file if it exists
        args.Add("-y");
        
        // Output file path
        args.Add($"\"{outputPath}\"");
        
        return string.Join(" ", args);
    }
}