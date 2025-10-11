using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Service for one-click safe video generation with guaranteed-success settings.
/// Forces Free-only providers: RuleBased LLM + Windows TTS + Stock visuals.
/// Locks render to 1080p30 H.264 for maximum compatibility.
/// </summary>
public class QuickService
{
    private readonly ILogger<QuickService> _logger;
    private readonly JobRunner _jobRunner;

    public QuickService(ILogger<QuickService> logger, JobRunner jobRunner)
    {
        _logger = logger;
        _jobRunner = jobRunner;
    }

    /// <summary>
    /// Creates and starts a quick demo video job with safe defaults.
    /// Generates a 10-15 second video with captions.
    /// </summary>
    public async Task<QuickDemoResult> CreateQuickDemoAsync(
        string? topic = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting Quick Demo generation with safe defaults");

            // Force safe brief settings
            var brief = new Brief(
                Topic: topic ?? "Welcome to Aura Video Studio",
                Audience: "General",
                Goal: "Demonstrate",
                Tone: "Informative",
                Language: "en-US",
                Aspect: Aspect.Widescreen16x9
            );

            // Target 10-15 seconds duration (0.25 minutes)
            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(12),
                Pacing: Pacing.Fast,
                Density: Density.Sparse,
                Style: "Demo"
            );

            // Safe voice settings - uses Windows TTS default
            var voiceSpec = new VoiceSpec(
                VoiceName: "en-US-Standard-A",
                Rate: 1.0,
                Pitch: 0.0,
                Pause: PauseStyle.Short
            );

            // Lock to 1080p30 H.264 for maximum compatibility
            var renderSpec = new RenderSpec(
                Res: new Resolution(1920, 1080),
                Container: "mp4",
                VideoBitrateK: 5000,
                AudioBitrateK: 192,
                Fps: 30,
                Codec: "H264",
                QualityLevel: 75,
                EnableSceneCut: true
            );

            var job = await _jobRunner.CreateAndStartJobAsync(
                brief,
                planSpec,
                voiceSpec,
                renderSpec,
                correlationId: $"quick-demo-{DateTime.UtcNow:yyyyMMddHHmmss}",
                ct
            );

            _logger.LogInformation("Quick Demo job created: {JobId}", job.Id);

            return new QuickDemoResult(
                Success: true,
                JobId: job.Id,
                Message: "Quick demo started successfully"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Quick Demo");
            return new QuickDemoResult(
                Success: false,
                JobId: null,
                Message: $"Failed to create quick demo: {ex.Message}"
            );
        }
    }
}

/// <summary>
/// Result of a quick demo generation request
/// </summary>
public record QuickDemoResult(
    bool Success,
    string? JobId,
    string Message
);
