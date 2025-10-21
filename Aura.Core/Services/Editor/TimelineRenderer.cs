using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Editor;

/// <summary>
/// Renders EditableTimeline to video using FFmpeg
/// </summary>
public class TimelineRenderer
{
    private readonly ILogger<TimelineRenderer> _logger;
    private readonly string _ffmpegPath;

    public TimelineRenderer(ILogger<TimelineRenderer> logger, string ffmpegPath = "ffmpeg")
    {
        _logger = logger;
        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Generate a low-resolution preview video for faster processing
    /// </summary>
    public async Task<string> GeneratePreviewAsync(
        EditableTimeline timeline,
        RenderSpec spec,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Create low-res preview spec
        var previewSpec = spec with
        {
            Res = new Resolution(1280, 720), // 720p
            VideoBitrateK = 2000,
            AudioBitrateK = 128,
            QualityLevel = 50 // Faster encoding
        };

        return await RenderTimelineAsync(timeline, previewSpec, outputPath, progress, cancellationToken);
    }

    /// <summary>
    /// Generate final high-quality video
    /// </summary>
    public async Task<string> GenerateFinalAsync(
        EditableTimeline timeline,
        RenderSpec spec,
        string outputPath,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await RenderTimelineAsync(timeline, spec, outputPath, progress, cancellationToken);
    }

    /// <summary>
    /// Core rendering method that converts timeline to video
    /// </summary>
    private async Task<string> RenderTimelineAsync(
        EditableTimeline timeline,
        RenderSpec spec,
        string outputPath,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        if (timeline.Scenes.Count == 0)
        {
            throw new InvalidOperationException("Timeline has no scenes to render");
        }

        _logger.LogInformation("Starting timeline render with {SceneCount} scenes", timeline.Scenes.Count);

        try
        {
            // Build FFmpeg command
            var filterComplex = BuildFilterComplex(timeline, spec);
            var ffmpegArgs = BuildFFmpegCommand(timeline, spec, filterComplex, outputPath);

            _logger.LogDebug("FFmpeg arguments: {Args}", ffmpegArgs);

            // Execute FFmpeg
            await ExecuteFFmpegAsync(ffmpegArgs, timeline.TotalDuration, progress, cancellationToken);

            _logger.LogInformation("Timeline rendered successfully to {OutputPath}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render timeline");
            throw;
        }
    }

    /// <summary>
    /// Build complex FFmpeg filter chain for compositing scenes
    /// </summary>
    private string BuildFilterComplex(EditableTimeline timeline, RenderSpec spec)
    {
        var filters = new StringBuilder();
        var hasAudio = false;

        // Process each scene
        for (int i = 0; i < timeline.Scenes.Count; i++)
        {
            var scene = timeline.Scenes[i];
            
            // Create blank video for scene duration if no visual assets
            if (scene.VisualAssets.Count == 0)
            {
                filters.AppendFormat(CultureInfo.InvariantCulture,
                    "color=c=black:s={0}x{1}:d={2}:r={3}[v{4}];",
                    spec.Res.Width, spec.Res.Height,
                    scene.Duration.TotalSeconds, spec.Fps, i);
            }
            else
            {
                // Composite visual assets
                var sceneFilter = BuildSceneComposite(scene, spec, i);
                filters.Append(sceneFilter);
            }

            // Add narration audio if present
            if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && File.Exists(scene.NarrationAudioPath))
            {
                hasAudio = true;
            }
        }

        // Concatenate all scene videos
        if (timeline.Scenes.Count > 1)
        {
            filters.Append(string.Join("", timeline.Scenes.Select((_, i) => $"[v{i}]")));
            filters.AppendFormat(CultureInfo.InvariantCulture, "concat=n={0}:v=1:a=0[outv];", timeline.Scenes.Count);
        }
        else
        {
            filters.Append("[v0]copy[outv];");
        }

        // Mix audio tracks if present
        if (hasAudio)
        {
            filters.Append(BuildAudioMix(timeline));
        }

        return filters.ToString();
    }

    /// <summary>
    /// Build composite filter for a single scene with assets
    /// </summary>
    private string BuildSceneComposite(TimelineScene scene, RenderSpec spec, int sceneIndex)
    {
        var filter = new StringBuilder();
        
        // Sort assets by Z-index
        var sortedAssets = scene.VisualAssets
            .Where(a => a.Type == AssetType.Image || a.Type == AssetType.Video)
            .OrderBy(a => a.ZIndex)
            .ToList();

        if (sortedAssets.Count == 0)
        {
            return $"color=c=black:s={spec.Res.Width}x{spec.Res.Height}:d={scene.Duration.TotalSeconds}:r={spec.Fps}[v{sceneIndex}];";
        }

        // Start with black background
        filter.AppendFormat(CultureInfo.InvariantCulture,
            "color=c=black:s={0}x{1}:d={2}:r={3}[bg{4}];",
            spec.Res.Width, spec.Res.Height,
            scene.Duration.TotalSeconds, spec.Fps, sceneIndex);

        string currentOutput = $"bg{sceneIndex}";

        // Overlay each asset
        for (int i = 0; i < sortedAssets.Count; i++)
        {
            var asset = sortedAssets[i];
            var assetInput = $"asset{sceneIndex}_{i}";
            var overlayOutput = i == sortedAssets.Count - 1 ? $"v{sceneIndex}" : $"tmp{sceneIndex}_{i}";

            // Scale and position asset
            var x = (int)(asset.Position.X / 100.0 * spec.Res.Width);
            var y = (int)(asset.Position.Y / 100.0 * spec.Res.Height);
            var w = (int)(asset.Position.Width / 100.0 * spec.Res.Width);
            var h = (int)(asset.Position.Height / 100.0 * spec.Res.Height);

            // Apply effects if present
            var effectsFilter = BuildEffectsFilter(asset.Effects);
            
            filter.AppendFormat(CultureInfo.InvariantCulture,
                "[{0}]scale={1}:{2}{3},format=rgba,colorchannelmixer=aa={4}[scaled{5}_{6}];",
                assetInput, w, h, effectsFilter, asset.Opacity, sceneIndex, i);

            // Overlay on current output
            filter.AppendFormat(CultureInfo.InvariantCulture,
                "[{0}][scaled{1}_{2}]overlay={3}:{4}[{5}];",
                currentOutput, sceneIndex, i, x, y, overlayOutput);

            currentOutput = overlayOutput;
        }

        return filter.ToString();
    }

    /// <summary>
    /// Build effects filter string
    /// </summary>
    private string BuildEffectsFilter(EffectConfig? effects)
    {
        if (effects == null) return string.Empty;

        var filters = new List<string>();

        if (Math.Abs(effects.Brightness - 1.0) > 0.01)
        {
            filters.Add($"eq=brightness={effects.Brightness - 1.0}");
        }

        if (Math.Abs(effects.Contrast - 1.0) > 0.01)
        {
            filters.Add($"eq=contrast={effects.Contrast}");
        }

        if (Math.Abs(effects.Saturation - 1.0) > 0.01)
        {
            filters.Add($"eq=saturation={effects.Saturation}");
        }

        if (!string.IsNullOrEmpty(effects.Filter))
        {
            filters.Add(effects.Filter);
        }

        return filters.Count > 0 ? "," + string.Join(",", filters) : string.Empty;
    }

    /// <summary>
    /// Build audio mixing filter
    /// </summary>
    private string BuildAudioMix(EditableTimeline timeline)
    {
        var audioInputs = new List<string>();

        // Collect narration tracks
        for (int i = 0; i < timeline.Scenes.Count; i++)
        {
            var scene = timeline.Scenes[i];
            if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && File.Exists(scene.NarrationAudioPath))
            {
                audioInputs.Add($"[{i}:a]");
            }
        }

        // Add background music if present
        if (!string.IsNullOrEmpty(timeline.BackgroundMusicPath) && File.Exists(timeline.BackgroundMusicPath))
        {
            audioInputs.Add("[music:a]");
        }

        if (audioInputs.Count == 0)
        {
            return string.Empty;
        }

        // Mix all audio tracks
        return $"{string.Join("", audioInputs)}amix=inputs={audioInputs.Count}:duration=longest:dropout_transition=2[outa];";
    }

    /// <summary>
    /// Build complete FFmpeg command
    /// </summary>
    private string BuildFFmpegCommand(
        EditableTimeline timeline,
        RenderSpec spec,
        string filterComplex,
        string outputPath)
    {
        var args = new StringBuilder();

        // Add input files for all scene assets
        for (int i = 0; i < timeline.Scenes.Count; i++)
        {
            var scene = timeline.Scenes[i];
            
            foreach (var asset in scene.VisualAssets)
            {
                if (File.Exists(asset.FilePath))
                {
                    args.AppendFormat(CultureInfo.InvariantCulture, "-i \"{0}\" ", asset.FilePath);
                }
            }

            if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && File.Exists(scene.NarrationAudioPath))
            {
                args.AppendFormat(CultureInfo.InvariantCulture, "-i \"{0}\" ", scene.NarrationAudioPath);
            }
        }

        // Add background music if present
        if (!string.IsNullOrEmpty(timeline.BackgroundMusicPath) && File.Exists(timeline.BackgroundMusicPath))
        {
            args.AppendFormat(CultureInfo.InvariantCulture, "-i \"{0}\" ", timeline.BackgroundMusicPath);
        }

        // Add filter complex
        if (!string.IsNullOrEmpty(filterComplex))
        {
            args.AppendFormat(CultureInfo.InvariantCulture, "-filter_complex \"{0}\" ", filterComplex);
        }

        // Map output streams
        args.Append("-map \"[outv]\" ");
        
        if (filterComplex.Contains("[outa]"))
        {
            args.Append("-map \"[outa]\" ");
        }

        // Video encoding
        args.AppendFormat(CultureInfo.InvariantCulture,
            "-c:v libx264 -preset {0} -crf {1} -b:v {2}k ",
            spec.QualityLevel > 75 ? "slow" : "medium",
            28 - (spec.QualityLevel / 5), // CRF 18-28
            spec.VideoBitrateK);

        // Audio encoding
        args.AppendFormat(CultureInfo.InvariantCulture,
            "-c:a aac -b:a {0}k ",
            spec.AudioBitrateK);

        // Frame rate
        args.AppendFormat(CultureInfo.InvariantCulture, "-r {0} ", spec.Fps);

        // Pixel format
        args.Append("-pix_fmt yuv420p ");

        // Overwrite output
        args.Append("-y ");

        // Output file
        args.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"", outputPath);

        return args.ToString();
    }

    /// <summary>
    /// Execute FFmpeg command with progress reporting
    /// </summary>
    private async Task ExecuteFFmpegAsync(
        string arguments,
        TimeSpan totalDuration,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var errorOutput = new StringBuilder();

        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            
            errorOutput.AppendLine(e.Data);
            
            // Parse FFmpeg progress
            if (e.Data.Contains("time=") && progress != null && totalDuration.TotalSeconds > 0)
            {
                var match = System.Text.RegularExpressions.Regex.Match(e.Data, @"time=(\d+):(\d+):(\d+\.\d+)");
                if (match.Success)
                {
                    var hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    var minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    
                    var currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                    var percentage = (int)((currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100);
                    
                    progress.Report(Math.Min(100, percentage));
                }
            }
        };

        process.Start();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            _logger.LogError("FFmpeg failed with exit code {ExitCode}. Output: {Output}",
                process.ExitCode, errorOutput.ToString());
            throw new InvalidOperationException($"FFmpeg rendering failed with exit code {process.ExitCode}");
        }
    }
}
