using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Models.Timeline;
using Aura.Core.Orchestrator;
using Aura.Core.Services.Editor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for timeline editing and preview rendering
/// </summary>
[ApiController]
[Route("api/editor")]
public class EditorController : ControllerBase
{
    private readonly ILogger<EditorController> _logger;
    private readonly ArtifactManager _artifactManager;
    private readonly JobRunner _jobRunner;
    private readonly TimelineRenderer _timelineRenderer;

    public EditorController(
        ILogger<EditorController> logger,
        ArtifactManager artifactManager,
        JobRunner jobRunner,
        TimelineRenderer timelineRenderer)
    {
        _logger = logger;
        _artifactManager = artifactManager;
        _jobRunner = jobRunner;
        _timelineRenderer = timelineRenderer;
    }

    /// <summary>
    /// Load timeline for a job
    /// </summary>
    [HttpGet("timeline/{jobId}")]
    public async Task<IActionResult> GetTimeline(string jobId)
    {
        try
        {
            _logger.LogInformation("Loading timeline for job {JobId}", jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Check if timeline already exists
            var timelinePath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "timeline.json");
            
            EditableTimeline timeline;
            
            if (System.IO.File.Exists(timelinePath))
            {
                // Load existing timeline
                var json = await System.IO.File.ReadAllTextAsync(timelinePath).ConfigureAwait(false);
                timeline = JsonSerializer.Deserialize<EditableTimeline>(json) ?? new EditableTimeline();
            }
            else
            {
                // Create timeline from job artifacts
                timeline = await CreateTimelineFromJobAsync(job).ConfigureAwait(false);
                
                // Save the initial timeline
                var json = JsonSerializer.Serialize(timeline, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(timelinePath, json).ConfigureAwait(false);
            }

            return Ok(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load timeline for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to load timeline", details = ex.Message });
        }
    }

    /// <summary>
    /// Save timeline changes
    /// </summary>
    [HttpPut("timeline/{jobId}")]
    public async Task<IActionResult> SaveTimeline(string jobId, [FromBody] EditableTimeline timeline)
    {
        try
        {
            _logger.LogInformation("Saving timeline for job {JobId}", jobId);

            // Validate timeline
            if (!ValidateTimeline(timeline, out var validationError))
            {
                return BadRequest(new { error = validationError });
            }

            // Save timeline to job directory
            var timelinePath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "timeline.json");
            var json = JsonSerializer.Serialize(timeline, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(timelinePath, json).ConfigureAwait(false);

            _logger.LogInformation("Timeline saved successfully for job {JobId}", jobId);
            return Ok(new { success = true, message = "Timeline saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save timeline for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to save timeline", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate preview video from timeline
    /// </summary>
    [HttpPost("timeline/{jobId}/render-preview")]
    public async Task<IActionResult> RenderPreview(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Rendering preview for job {JobId}", jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Load timeline
            var timelinePath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "timeline.json");
            if (!System.IO.File.Exists(timelinePath))
            {
                return BadRequest(new { error = "Timeline not found. Please save the timeline first." });
            }

            var json = await System.IO.File.ReadAllTextAsync(timelinePath, cancellationToken).ConfigureAwait(false);
            var timeline = JsonSerializer.Deserialize<EditableTimeline>(json);
            
            if (timeline == null || timeline.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Timeline is empty" });
            }

            // Use render spec from job or create default
            var renderSpec = job.RenderSpec ?? new RenderSpec(
                Res: new Resolution(1920, 1080),
                Container: "mp4",
                VideoBitrateK: 5000,
                AudioBitrateK: 192,
                Fps: 30);

            // Generate preview
            var previewPath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "preview.mp4");
            
            var progress = new Progress<int>(percentage =>
            {
                _logger.LogDebug("Preview rendering progress: {Percentage}%", percentage);
            });

            await _timelineRenderer.GeneratePreviewAsync(
                timeline, 
                renderSpec, 
                previewPath, 
                progress, 
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Preview rendered successfully for job {JobId}", jobId);
            
            return Ok(new 
            { 
                success = true, 
                previewPath = $"/api/editor/preview/{jobId}",
                message = "Preview generated successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render preview for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to render preview", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate final high-quality video from timeline
    /// </summary>
    [HttpPost("timeline/{jobId}/render-final")]
    public async Task<IActionResult> RenderFinal(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Rendering final video for job {JobId}", jobId);

            var job = _jobRunner.GetJob(jobId);
            if (job == null)
            {
                return NotFound(new { error = "Job not found" });
            }

            // Load timeline
            var timelinePath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "timeline.json");
            if (!System.IO.File.Exists(timelinePath))
            {
                return BadRequest(new { error = "Timeline not found. Please save the timeline first." });
            }

            var json = await System.IO.File.ReadAllTextAsync(timelinePath, cancellationToken).ConfigureAwait(false);
            var timeline = JsonSerializer.Deserialize<EditableTimeline>(json);
            
            if (timeline == null || timeline.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Timeline is empty" });
            }

            var renderSpec = job.RenderSpec ?? new RenderSpec(
                Res: new Resolution(1920, 1080),
                Container: "mp4",
                VideoBitrateK: 5000,
                AudioBitrateK: 192,
                Fps: 30);

            // Generate final video
            var finalPath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "final_edited.mp4");
            
            var progress = new Progress<int>(percentage =>
            {
                _logger.LogDebug("Final rendering progress: {Percentage}%", percentage);
            });

            await _timelineRenderer.GenerateFinalAsync(
                timeline, 
                renderSpec, 
                finalPath, 
                progress, 
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Final video rendered successfully for job {JobId}", jobId);
            
            return Ok(new 
            { 
                success = true, 
                videoPath = $"/api/editor/video/{jobId}",
                message = "Final video generated successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render final video for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to render final video", details = ex.Message });
        }
    }

    /// <summary>
    /// Upload asset file for timeline
    /// </summary>
    [HttpPost("timeline/{jobId}/assets/upload")]
    public async Task<IActionResult> UploadAsset(string jobId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            _logger.LogInformation("Uploading asset for job {JobId}: {FileName}", jobId, file.FileName);

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi", ".webm" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Invalid file type. Only images and videos are allowed." });
            }

            // Create assets directory
            var assetsDir = Path.Combine(_artifactManager.GetJobDirectory(jobId), "assets");
            Directory.CreateDirectory(assetsDir);

            // Generate unique filename
            var assetId = Guid.NewGuid().ToString();
            var fileName = $"{assetId}{extension}";
            var filePath = Path.Combine(assetsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream).ConfigureAwait(false);
            }

            _logger.LogInformation("Asset uploaded successfully: {FilePath}", filePath);

            return Ok(new
            {
                assetId,
                fileName,
                filePath,
                fileSize = file.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload asset for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to upload asset", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete asset from timeline
    /// </summary>
    [HttpDelete("timeline/{jobId}/assets/{assetId}")]
    public IActionResult DeleteAsset(string jobId, string assetId)
    {
        try
        {
            _logger.LogInformation("Deleting asset {AssetId} from job {JobId}", assetId, jobId);

            var assetsDir = Path.Combine(_artifactManager.GetJobDirectory(jobId), "assets");
            var assetFiles = Directory.GetFiles(assetsDir, $"{assetId}.*");

            if (assetFiles.Length == 0)
            {
                return NotFound(new { error = "Asset not found" });
            }

            foreach (var file in assetFiles)
            {
                System.IO.File.Delete(file);
            }

            _logger.LogInformation("Asset {AssetId} deleted successfully", assetId);
            return Ok(new { success = true, message = "Asset deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete asset {AssetId} from job {JobId}", assetId, jobId);
            return StatusCode(500, new { error = "Failed to delete asset", details = ex.Message });
        }
    }

    /// <summary>
    /// Get preview video file
    /// </summary>
    [HttpGet("preview/{jobId}")]
    public IActionResult GetPreview(string jobId)
    {
        var previewPath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "preview.mp4");
        
        if (!System.IO.File.Exists(previewPath))
        {
            return NotFound(new { error = "Preview not found" });
        }

        return PhysicalFile(previewPath, "video/mp4", enableRangeProcessing: true);
    }

    /// <summary>
    /// Get final video file
    /// </summary>
    [HttpGet("video/{jobId}")]
    public IActionResult GetVideo(string jobId)
    {
        var videoPath = Path.Combine(_artifactManager.GetJobDirectory(jobId), "final_edited.mp4");
        
        if (!System.IO.File.Exists(videoPath))
        {
            return NotFound(new { error = "Video not found" });
        }

        return PhysicalFile(videoPath, "video/mp4", enableRangeProcessing: true);
    }

    /// <summary>
    /// Create timeline from job artifacts
    /// </summary>
    private async Task<EditableTimeline> CreateTimelineFromJobAsync(Job job)
    {
        var timeline = new EditableTimeline();

        // Extract scenes from job artifacts
        var jobDir = _artifactManager.GetJobDirectory(job.Id);
        var scenesPath = Path.Combine(jobDir, "scenes.json");

        if (System.IO.File.Exists(scenesPath))
        {
            var json = await System.IO.File.ReadAllTextAsync(scenesPath).ConfigureAwait(false);
            var scenes = JsonSerializer.Deserialize<List<Scene>>(json);

            if (scenes != null)
            {
                foreach (var scene in scenes)
                {
                    var timelineScene = new TimelineScene(
                        Index: scene.Index,
                        Heading: scene.Heading,
                        Script: scene.Script,
                        Start: scene.Start,
                        Duration: scene.Duration,
                        NarrationAudioPath: null, // Will be populated from audio artifacts
                        VisualAssets: new List<TimelineAsset>(),
                        TransitionType: "Fade",
                        TransitionDuration: TimeSpan.FromSeconds(0.5)
                    );

                    timeline.AddScene(timelineScene);
                }
            }
        }

        // Try to find narration audio files
        var audioDir = Path.Combine(jobDir, "audio");
        if (Directory.Exists(audioDir))
        {
            var audioFiles = Directory.GetFiles(audioDir, "*.wav").OrderBy(f => f).ToArray();
            
            for (int i = 0; i < Math.Min(timeline.Scenes.Count, audioFiles.Length); i++)
            {
                var scene = timeline.Scenes[i];
                timeline.Scenes[i] = scene with { NarrationAudioPath = audioFiles[i] };
            }
        }

        // Try to find background music
        var musicPath = Path.Combine(jobDir, "music.mp3");
        if (System.IO.File.Exists(musicPath))
        {
            timeline.BackgroundMusicPath = musicPath;
        }

        return timeline;
    }

    /// <summary>
    /// Validate timeline structure
    /// </summary>
    private bool ValidateTimeline(EditableTimeline timeline, out string error)
    {
        error = string.Empty;

        if (timeline.Scenes.Count == 0)
        {
            error = "Timeline must have at least one scene";
            return false;
        }

        // Validate all asset files exist
        foreach (var scene in timeline.Scenes)
        {
            if (!string.IsNullOrEmpty(scene.NarrationAudioPath) && !System.IO.File.Exists(scene.NarrationAudioPath))
            {
                error = $"Narration audio file not found for scene {scene.Index}: {scene.NarrationAudioPath}";
                return false;
            }

            foreach (var asset in scene.VisualAssets)
            {
                if (!System.IO.File.Exists(asset.FilePath))
                {
                    error = $"Asset file not found: {asset.FilePath}";
                    return false;
                }
            }
        }

        if (!string.IsNullOrEmpty(timeline.BackgroundMusicPath) && !System.IO.File.Exists(timeline.BackgroundMusicPath))
        {
            error = $"Background music file not found: {timeline.BackgroundMusicPath}";
            return false;
        }

        return true;
    }
}
