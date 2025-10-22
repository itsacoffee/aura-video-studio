using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Render plan for a scene
/// </summary>
public enum SceneRenderStatus
{
    /// <summary>Scene unchanged, use cached render</summary>
    Unmodified,
    
    /// <summary>Scene modified, re-render required</summary>
    Modified,
    
    /// <summary>New scene, needs initial render</summary>
    New
}

/// <summary>
/// Render plan entry for a single scene
/// </summary>
public record SceneRenderPlan(
    int SceneIndex,
    SceneRenderStatus Status,
    string? CachedOutputPath,
    string Checksum);

/// <summary>
/// Complete render plan for a timeline
/// </summary>
public record RenderPlan(
    string JobId,
    List<SceneRenderPlan> Scenes,
    int TotalScenes,
    int UnmodifiedScenes,
    int ModifiedScenes,
    int NewScenes);

/// <summary>
/// Render cache manifest tracking scene checksums
/// </summary>
public record RenderCacheManifest(
    string JobId,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    Dictionary<int, SceneCacheEntry> Scenes);

/// <summary>
/// Cache entry for a single scene
/// </summary>
public record SceneCacheEntry(
    int SceneIndex,
    string Checksum,
    string OutputPath,
    DateTime RenderedAt,
    TimeSpan Duration);

/// <summary>
/// Implements smart rendering with change detection and incremental updates
/// </summary>
public class SmartRenderer
{
    private readonly ILogger<SmartRenderer> _logger;
    private readonly string _cacheDirectory;
    private const int MaxCachedRenders = 3;

    public SmartRenderer(ILogger<SmartRenderer> logger, string cacheDirectory)
    {
        _logger = logger;
        _cacheDirectory = cacheDirectory;
        
        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Generates a render plan by comparing current timeline to cached state
    /// </summary>
    public async Task<RenderPlan> GenerateRenderPlanAsync(
        EditableTimeline timeline,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating render plan for job {JobId}", jobId);

        var manifest = await LoadManifestAsync(jobId);
        var scenePlans = new List<SceneRenderPlan>();

        foreach (var scene in timeline.Scenes)
        {
            var checksum = CalculateSceneChecksum(scene);
            
            if (manifest != null && manifest.Scenes.TryGetValue(scene.Index, out var cachedEntry))
            {
                if (cachedEntry.Checksum == checksum && File.Exists(cachedEntry.OutputPath))
                {
                    // Scene unchanged, can use cached render
                    scenePlans.Add(new SceneRenderPlan(
                        SceneIndex: scene.Index,
                        Status: SceneRenderStatus.Unmodified,
                        CachedOutputPath: cachedEntry.OutputPath,
                        Checksum: checksum
                    ));
                }
                else
                {
                    // Scene modified, needs re-render
                    scenePlans.Add(new SceneRenderPlan(
                        SceneIndex: scene.Index,
                        Status: SceneRenderStatus.Modified,
                        CachedOutputPath: null,
                        Checksum: checksum
                    ));
                }
            }
            else
            {
                // New scene, needs render
                scenePlans.Add(new SceneRenderPlan(
                    SceneIndex: scene.Index,
                    Status: SceneRenderStatus.New,
                    CachedOutputPath: null,
                    Checksum: checksum
                ));
            }
        }

        var unmodified = scenePlans.Count(s => s.Status == SceneRenderStatus.Unmodified);
        var modified = scenePlans.Count(s => s.Status == SceneRenderStatus.Modified);
        var newScenes = scenePlans.Count(s => s.Status == SceneRenderStatus.New);

        _logger.LogInformation(
            "Render plan: {Total} scenes ({Unmodified} unmodified, {Modified} modified, {New} new)",
            scenePlans.Count, unmodified, modified, newScenes
        );

        return new RenderPlan(
            JobId: jobId,
            Scenes: scenePlans,
            TotalScenes: scenePlans.Count,
            UnmodifiedScenes: unmodified,
            ModifiedScenes: modified,
            NewScenes: newScenes
        );
    }

    /// <summary>
    /// Calculates MD5 checksum of scene content and assets
    /// </summary>
    public string CalculateSceneChecksum(TimelineScene scene)
    {
        var content = new StringBuilder();
        
        // Include scene properties
        content.Append($"Heading:{scene.Heading}|");
        content.Append($"Script:{scene.Script}|");
        content.Append($"Duration:{scene.Duration.TotalSeconds}|");
        content.Append($"Transition:{scene.TransitionType}|");
        content.Append($"TransitionDuration:{scene.TransitionDuration?.TotalSeconds}|");
        
        // Include narration audio checksum
        if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && File.Exists(scene.NarrationAudioPath))
        {
            content.Append($"Narration:{CalculateFileChecksum(scene.NarrationAudioPath)}|");
        }
        
        // Include visual assets
        foreach (var asset in scene.VisualAssets.OrderBy(a => a.Id))
        {
            content.Append($"Asset:{asset.Id}|");
            content.Append($"Type:{asset.Type}|");
            content.Append($"File:{CalculateFileChecksum(asset.FilePath)}|");
            content.Append($"Position:{asset.Position.X},{asset.Position.Y},{asset.Position.Width},{asset.Position.Height}|");
            content.Append($"ZIndex:{asset.ZIndex}|");
            content.Append($"Opacity:{asset.Opacity}|");
            
            if (asset.Effects != null)
            {
                content.Append($"Effects:{asset.Effects.Brightness},{asset.Effects.Contrast},{asset.Effects.Saturation},{asset.Effects.Filter}|");
            }
        }
        
        return ComputeMD5Hash(content.ToString());
    }

    /// <summary>
    /// Renders only modified scenes and stitches with cached scenes
    /// </summary>
    public async Task<string> RenderModificationsOnlyAsync(
        EditableTimeline timeline,
        ExportPreset preset,
        RenderPlan plan,
        string outputPath,
        IProgress<RenderProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rendering modifications: {Modified} of {Total} scenes need rendering",
            plan.ModifiedScenes + plan.NewScenes, plan.TotalScenes
        );

        var sceneOutputs = new Dictionary<int, string>();
        var totalScenes = plan.ModifiedScenes + plan.NewScenes;
        var completedScenes = 0;

        // Render modified and new scenes
        foreach (var scenePlan in plan.Scenes)
        {
            if (scenePlan.Status == SceneRenderStatus.Unmodified && scenePlan.CachedOutputPath != null)
            {
                // Use cached render
                sceneOutputs[scenePlan.SceneIndex] = scenePlan.CachedOutputPath;
                _logger.LogInformation(
                    "Scene {Index}: Using cached render from {Path}",
                    scenePlan.SceneIndex, scenePlan.CachedOutputPath
                );
            }
            else
            {
                // Render scene
                var scene = timeline.Scenes[scenePlan.SceneIndex];
                var sceneOutputPath = Path.Combine(
                    _cacheDirectory,
                    plan.JobId,
                    $"scene_{scenePlan.SceneIndex:D3}.mp4"
                );

                Directory.CreateDirectory(Path.GetDirectoryName(sceneOutputPath)!);

                _logger.LogInformation(
                    "Scene {Index}: Rendering ({Status})",
                    scenePlan.SceneIndex, scenePlan.Status
                );

                // TODO: Actual scene rendering would happen here
                // For now, we'll just log the intent
                
                sceneOutputs[scenePlan.SceneIndex] = sceneOutputPath;
                completedScenes++;

                // Report progress
                var overallProgress = (completedScenes * 100) / totalScenes;
                progress?.Report(new RenderProgressInfo(
                    OverallProgress: overallProgress,
                    CurrentScene: scenePlan.SceneIndex + 1,
                    TotalScenes: plan.TotalScenes,
                    SceneProgress: 100,
                    Stage: $"Rendered scene {scenePlan.SceneIndex + 1}",
                    EstimatedTimeRemaining: TimeSpan.Zero
                ));
            }
        }

        // Stitch scenes together
        _logger.LogInformation("Stitching {Count} scenes together", sceneOutputs.Count);
        await StitchScenesAsync(sceneOutputs, outputPath, cancellationToken);

        // Update cache manifest
        await UpdateManifestAsync(plan.JobId, timeline, sceneOutputs);

        // Cleanup old renders
        await CleanupOldRendersAsync(plan.JobId);

        _logger.LogInformation("Smart render complete: {OutputPath}", outputPath);
        return outputPath;
    }

    /// <summary>
    /// Stitches rendered scenes using FFmpeg concat demuxer
    /// </summary>
    private async Task StitchScenesAsync(
        Dictionary<int, string> sceneOutputs,
        string outputPath,
        CancellationToken cancellationToken)
    {
        // Create concat file list
        var concatListPath = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid()}.txt");
        
        try
        {
            var lines = sceneOutputs
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"file '{kvp.Value.Replace("'", "'\\''")}'");
            
            await File.WriteAllLinesAsync(concatListPath, lines, cancellationToken);

            // Use FFmpeg concat demuxer with stream copy (fast)
            // TODO: Actual FFmpeg execution would happen here
            _logger.LogInformation("Stitching scenes with FFmpeg concat demuxer");
            
            // For now, we'll just copy the first scene as a placeholder
            if (sceneOutputs.Count > 0 && File.Exists(sceneOutputs.First().Value))
            {
                File.Copy(sceneOutputs.First().Value, outputPath, overwrite: true);
            }
        }
        finally
        {
            if (File.Exists(concatListPath))
            {
                File.Delete(concatListPath);
            }
        }
    }

    /// <summary>
    /// Loads render cache manifest for a job
    /// </summary>
    private async Task<RenderCacheManifest?> LoadManifestAsync(string jobId)
    {
        var manifestPath = GetManifestPath(jobId);
        
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath);
            return JsonSerializer.Deserialize<RenderCacheManifest>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load manifest for job {JobId}", jobId);
            return null;
        }
    }

    /// <summary>
    /// Updates render cache manifest after successful render
    /// </summary>
    private async Task UpdateManifestAsync(
        string jobId,
        EditableTimeline timeline,
        Dictionary<int, string> sceneOutputs)
    {
        var manifest = await LoadManifestAsync(jobId) ?? new RenderCacheManifest(
            JobId: jobId,
            CreatedAt: DateTime.UtcNow,
            LastModifiedAt: DateTime.UtcNow,
            Scenes: new Dictionary<int, SceneCacheEntry>()
        );

        var updatedScenes = new Dictionary<int, SceneCacheEntry>(manifest.Scenes);

        foreach (var scene in timeline.Scenes)
        {
            if (sceneOutputs.TryGetValue(scene.Index, out var outputPath))
            {
                updatedScenes[scene.Index] = new SceneCacheEntry(
                    SceneIndex: scene.Index,
                    Checksum: CalculateSceneChecksum(scene),
                    OutputPath: outputPath,
                    RenderedAt: DateTime.UtcNow,
                    Duration: scene.Duration
                );
            }
        }

        var updatedManifest = manifest with
        {
            LastModifiedAt = DateTime.UtcNow,
            Scenes = updatedScenes
        };

        var manifestPath = GetManifestPath(jobId);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

        var json = JsonSerializer.Serialize(updatedManifest, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(manifestPath, json);
    }

    /// <summary>
    /// Cleans up old renders keeping only the last N renders per job
    /// </summary>
    private async Task CleanupOldRendersAsync(string jobId)
    {
        var jobCacheDir = Path.Combine(_cacheDirectory, jobId);
        
        if (!Directory.Exists(jobCacheDir))
        {
            return;
        }

        try
        {
            var renderDirs = Directory.GetDirectories(jobCacheDir)
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.CreationTimeUtc)
                .ToList();

            // Keep only the last MaxCachedRenders
            var toDelete = renderDirs.Skip(MaxCachedRenders);

            foreach (var dir in toDelete)
            {
                _logger.LogInformation("Cleaning up old render cache: {Path}", dir.FullName);
                await Task.Run(() => Directory.Delete(dir.FullName, recursive: true));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up old renders for job {JobId}", jobId);
        }
    }

    private string GetManifestPath(string jobId)
    {
        return Path.Combine(_cacheDirectory, jobId, "manifest.json");
    }

    private string CalculateFileChecksum(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return "missing";
        }

        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return "error";
        }
    }

    private string ComputeMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hash = md5.ComputeHash(inputBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

/// <summary>
/// Progress information for rendering
/// </summary>
public record RenderProgressInfo(
    int OverallProgress,
    int CurrentScene,
    int TotalScenes,
    int SceneProgress,
    string Stage,
    TimeSpan EstimatedTimeRemaining);
